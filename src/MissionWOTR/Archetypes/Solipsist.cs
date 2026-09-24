using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Solipsist (homebrew cleric archetype, user design: "a cleric focused on
  /// himself, with all spells that affect allies instead being only cast on
  /// himself at 2x effectiveness; for effects like haste, spawn a second
  /// version of the same buff with a different buff type, probably untyped,
  /// so it doesn't get overridden by anything").
  ///
  /// Design:
  /// - Solipsism: every cleric spell that could target an ally can now only
  ///   target the solipsist himself (enemy targets are unaffected - cure
  ///   spells still sear the undead). Every blessing such a spell applies to
  ///   him is laid down a SECOND time as an untyped echo copy that stacks
  ///   with the original and with everything else; healing spells roll their
  ///   healing twice. Channel energy is traded away entirely - a congregation
  ///   of one has no flock to heal.
  ///
  /// Engine notes:
  /// - The lock is a custom IAbilityTargetRestriction component ADDED to the
  ///   shared vanilla spell abilities (the pplus AbilityAnkouShadow recipe).
  ///   The component is inert for every caster without the Solipsism fact,
  ///   so the shared blueprints stay safe for all other classes.
  /// - Selection: the scan walks the cleric spellbook's spell list
  ///   (book.SpellList.SpellsByLevel, the pplus MadScientistPrep idiom) plus
  ///   ability variants, and picks spells that CanTargetFriends and are
  ///   Helpful on allies (or carry a heal action).
  /// - The 2x is realized exactly as requested: for every
  ///   ContextActionApplyBuff in the spell's action tree a CLONE of the buff
  ///   blueprint is created (CopyFrom), every bonus descriptor inside the
  ///   clone is rewritten to None (untyped - so it stacks with the original's
  ///   typed bonus and cannot be overridden by same-type effects), and a copy
  ///   of the apply-buff element with the buff reference swapped to the clone
  ///   is appended to the spell via an extra AbilityEffectRunAction that runs
  ///   after the spell's own actions. ContextActionHealTarget elements are
  ///   copied verbatim: the second run re-rolls the heal in the same context.
  /// - Clone blueprints derive their guids deterministically from the
  ///   original buff's guid (XOR a fixed mask) so they are save-stable.
  /// - Actions nested under a Conditional gate are NOT doubled (never guess
  ///   a variant); actions nested in plain wrappers are.
  /// - Ground-point-targeted spells (the targeting lock and the echo both key
  ///   on the spell's main target being the caster) are untouched; utility
  ///   ally-spells with no buffs or heals (e.g. remove curse) get the lock
  ///   but no echo. Domain spells are not part of the scanned list.
  /// Log prefix: [solipsist].
  /// </summary>
  internal static class Solipsist
  {
    internal const string ArchetypeName = "SolipsistArchetype";
    internal const string SolipsismName = "SolipsistSolipsism";

    private static readonly HashSet<string> ProcessedAbilities = new();
    private static readonly Dictionary<string, BlueprintBuff> EchoClones = new();

    /// <summary>Stable XOR mask: original buff guid ^ mask = echo buff guid.</summary>
    private static readonly byte[] EchoMask =
    {
      0x5A, 0x3C, 0x71, 0xE4, 0x19, 0x8B, 0xD6, 0x02,
      0xF3, 0x47, 0x9A, 0x1D, 0x68, 0xB5, 0xC9, 0x30,
    };

    public static void Configure()
    {
      var cleric = CharacterClassRefs.ClericClass.Reference.Get();

      // ----- Solipsism (the whole kit rides one feature) -----
      var solipsism = FeatureConfigurator.New(SolipsismName, Guids.SolipsistFeature)
        .SetDisplayName("SolipsistSolipsism.Name")
        .SetDescription("SolipsistSolipsism.Description")
        .SetIcon(FeatureRefs.ChannelEnergyFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.SolipsistArchetype, CharacterClassRefs.ClericClass)
          .SetLocalizedName("Solipsist.Name")
          .SetLocalizedDescription("Solipsist.Description");

      // The selfish priest has no flock: channel energy is traded away.
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, cleric, "ChannelEnergyFeature", "ChannelEnergySelection");
      var channelLevel = ArchetypeRemovals.FindLevel(
        cleric.Progression, "ChannelEnergyFeature", "ChannelEnergySelection");

      archetype
        .AddToAddFeatures(LevelPlan.L(channelLevel), solipsism)
        .Configure();

      ApplyToClericSpells(solipsism);

      MissionFeats.Logger.Info("Solipsist: configured.");
    }

    // ------------------------------------------------------------------
    // Spell scan: lock + echo every ally-affecting cleric spell.
    // ------------------------------------------------------------------

    private static void ApplyToClericSpells(BlueprintFeature fact)
    {
      var book = SpellbookRefs.ClericSpellbook.Reference.Get();
      var spellList = book?.SpellList;
      if (spellList?.SpellsByLevel is null)
      {
        MissionFeats.Logger.Warn("[solipsist] cleric spell list unavailable - spells untouched!");
        return;
      }

      var abilities = new List<BlueprintAbility>();
      foreach (var levelList in spellList.SpellsByLevel)
      {
        if (levelList?.Spells is null)
        {
          continue;
        }
        abilities.AddRange(levelList.Spells.Where(spell => spell != null));
      }
      // Variant sub-abilities (alignment circles and the like) are the real
      // castable spells - they must carry the lock and the echo too.
      for (int i = 0; i < abilities.Count; i++)
      {
        var variants = abilities[i].GetComponent<AbilityVariants>();
        if (variants?.Variants != null)
        {
          abilities.AddRange(variants.Variants.Where(variant => variant != null));
        }
      }

      int locked = 0, doubled = 0, clones = 0;
      foreach (var ability in abilities)
      {
        ProcessAbility(ability, fact, ref locked, ref doubled, ref clones);
      }
      MissionFeats.Logger.Info(
        $"[solipsist] spell scan: {abilities.Count} abilities seen, {locked} locked to self, " +
        $"{doubled} with the doubled echo, {clones} echo buffs created.");
    }

    private static void ProcessAbility(
      BlueprintAbility ability, BlueprintFeature fact, ref int locked, ref int doubled, ref int clones)
    {
      var key = ability.AssetGuid.ToString();
      if (ProcessedAbilities.Contains(key))
      {
        return;
      }
      ProcessedAbilities.Add(key);
      if (ability.GetComponent<SolipsistTargetLock>() != null)
      {
        return; // already converted (idempotency guard)
      }

      if (!ability.CanTargetFriends)
      {
        return;
      }
      var (buffs, heals) = CollectEchoTargets(ability);
      if (ability.EffectOnAlly != AbilityEffectOnUnit.Helpful && heals.Count == 0)
      {
        return;
      }

      var echoes = new List<GameAction>();
      foreach (var applyBuff in buffs)
      {
        var original = BuffOf(applyBuff);
        if (original is null)
        {
          continue;
        }
        var clone = GetOrCreateEchoClone(original, ref clones);
        if (clone is null)
        {
          continue;
        }
        echoes.Add(CopyWithBuff(applyBuff, clone));
      }
      foreach (var heal in heals)
      {
        echoes.Add(CopyHeal(heal));
      }

      var builder = AbilityConfigurator.For(ability.ToReference<BlueprintAbilityReference>())
        .AddComponent(new SolipsistTargetLock { Fact = fact });
      if (echoes.Count > 0)
      {
        builder = builder.AddAbilityEffectRunAction(ActionsBuilder.New().Add(
          new SolipsistEchoAction
          {
            Fact = fact,
            Echoes = new ActionList { Actions = echoes.ToArray() },
          }));
      }
      builder.Configure();

      locked++;
      if (echoes.Count > 0)
      {
        doubled++;
      }
      MissionFeats.Logger.Info(
        $"[solipsist] {ability.name}: locked to self" +
        (echoes.Count > 0 ? $" (+{echoes.Count} echo effect(s))." : "."));
    }

    /// <summary>
    /// Every buff-apply and heal action reachable from the spell's run-action
    /// components, WITHOUT descending into Conditional gates (never guess a
    /// variant - a doubled mis-variant is worse than an undoubled one).
    /// </summary>
    private static (List<ContextActionApplyBuff> Buffs, List<ContextActionHealTarget> Heals)
      CollectEchoTargets(BlueprintAbility ability)
    {
      var buffs = new List<ContextActionApplyBuff>();
      var heals = new List<ContextActionHealTarget>();
      foreach (var runAction in ability.GetComponents<AbilityEffectRunAction>())
      {
        if (runAction?.Actions?.Actions is null)
        {
          continue;
        }
        Collect(runAction.Actions.Actions, buffs, heals);
      }
      return (buffs, heals);
    }

    /// <summary>
    /// Reads the buff reference of an apply-buff element without depending
    /// on a deref property name (the serialized field is m_Buff).
    /// </summary>
    private static BlueprintBuff BuffOf(ContextActionApplyBuff applyBuff)
    {
      var field = typeof(ContextActionApplyBuff).GetField(
        "m_Buff", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      var reference = field?.GetValue(applyBuff) as BlueprintBuffReference;
      return reference?.Get();
    }

    private static void Collect(
      GameAction[] actions, List<ContextActionApplyBuff> buffs, List<ContextActionHealTarget> heals)
    {
      foreach (var action in actions)
      {
        if (action is null)
        {
          continue;
        }
        if (action is ContextActionApplyBuff applyBuff)
        {
          buffs.Add(applyBuff);
          continue;
        }
        if (action is ContextActionHealTarget heal)
        {
          heals.Add(heal);
          continue;
        }
        if (action.GetType().Name == "Conditional")
        {
          continue; // gated variants are never doubled blindly
        }
        var type = action.GetType();
        foreach (var field in type.GetFields(
          BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
          if (field.FieldType == typeof(ActionList))
          {
            var nested = (ActionList)field.GetValue(action);
            if (nested?.Actions != null)
            {
              Collect(nested.Actions, buffs, heals);
            }
          }
          else if (field.FieldType.IsArray &&
            typeof(GameAction).IsAssignableFrom(field.FieldType.GetElementType()))
          {
            var nested = (GameAction[])field.GetValue(action);
            if (nested != null)
            {
              Collect(nested, buffs, heals);
            }
          }
        }
      }
    }

    // ------------------------------------------------------------------
    // Echo buffs: untyped clones of the originals.
    // ------------------------------------------------------------------

    private static BlueprintBuff GetOrCreateEchoClone(BlueprintBuff original, ref int created)
    {
      var key = NormalizeGuid(original.AssetGuid.ToString());
      if (EchoClones.TryGetValue(key, out var cached))
      {
        return cached;
      }

      var derived = DeriveEchoGuid(key);
      BlueprintBuff clone = null;
      try
      {
        clone = BlueprintTool.Get<BlueprintBuff>(derived);
      }
      catch (Exception)
      {
        // Not created yet - fall through and create it.
      }
      if (clone is null)
      {
        clone = BuffConfigurator.New("SolipsistEcho" + original.name, derived)
          .CopyFrom(original)
          .Configure();
        // The echo is untyped: every bonus descriptor inside becomes None, so
        // it stacks with the original's typed bonus and with everything else
        // (the user's "different buff type, probably untyped" requirement).
        UntypeDescriptors(clone);
        // Visual identity: the echo reads exactly like its original.
        clone.m_DisplayName = original.m_DisplayName;
        clone.m_Description = original.m_Description;
        clone.m_Icon = original.m_Icon;
        created++;
        MissionFeats.Logger.Info(
          $"[solipsist] echo buff: {original.name} -> {clone.name} ({derived}).");
      }
      EchoClones[key] = clone;
      return clone;
    }

    private static void UntypeDescriptors(BlueprintBuff clone)
    {
      foreach (var component in clone.ComponentsArray ?? Array.Empty<BlueprintComponent>())
      {
        if (component is null)
        {
          continue;
        }
        var type = component.GetType();
        foreach (var field in type.GetFields(
          BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
          if (field.FieldType == typeof(ModifierDescriptor) && !field.IsInitOnly)
          {
            field.SetValue(component, ModifierDescriptor.None);
          }
        }
        foreach (var property in type.GetProperties(
          BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
          if (property.PropertyType == typeof(ModifierDescriptor) &&
            property.CanWrite && property.GetSetMethod(true) != null)
          {
            property.SetValue(component, ModifierDescriptor.None, null);
          }
        }
      }
    }

    private static ContextActionApplyBuff CopyWithBuff(
      ContextActionApplyBuff source, BlueprintBuff clone)
    {
      var copy = new ContextActionApplyBuff();
      var fields = typeof(ContextActionApplyBuff).GetFields(
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      foreach (var field in fields)
      {
        if (field.IsInitOnly || field.FieldType == typeof(BlueprintBuffReference))
        {
          continue;
        }
        field.SetValue(copy, field.GetValue(source));
      }
      var buffField = fields.FirstOrDefault(
        field => field.FieldType == typeof(BlueprintBuffReference));
      if (buffField != null)
      {
        buffField.SetValue(copy, clone.ToReference<BlueprintBuffReference>());
      }
      return copy;
    }

    private static ContextActionHealTarget CopyHeal(ContextActionHealTarget source)
    {
      var copy = new ContextActionHealTarget();
      foreach (var field in typeof(ContextActionHealTarget).GetFields(
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
      {
        if (field.IsInitOnly)
        {
          continue;
        }
        field.SetValue(copy, field.GetValue(source));
      }
      return copy;
    }

    private static string DeriveEchoGuid(string normalizedGuid)
    {
      var hex = normalizedGuid.Replace("-", "").Replace("{", "").Replace("}", "").Trim();
      var dashed = hex.Length == 32
        ? $"{hex.Substring(0, 8)}-{hex.Substring(8, 4)}-{hex.Substring(12, 4)}-" +
          $"{hex.Substring(16, 4)}-{hex.Substring(20, 12)}"
        : normalizedGuid;
      var bytes = Guid.Parse(dashed).ToByteArray();
      for (int i = 0; i < bytes.Length && i < EchoMask.Length; i++)
      {
        bytes[i] ^= EchoMask[i];
      }
      return new Guid(bytes).ToString("D").ToUpperInvariant();
    }

    private static string NormalizeGuid(string guid)
    {
      return (guid ?? string.Empty).Replace("-", "").Replace("{", "").Replace("}", "")
        .Trim().ToLowerInvariant();
    }
  }

  /// <summary>
  /// The targeting lock: while on the ability, friendly unit targets other
  /// than the caster fail the check - unless the caster lacks the Solipsism
  /// fact, in which case the component is inert (the shared vanilla spell
  /// blueprints must stay untouched for every other class). Enemy targets
  /// (cure spells searing undead) and ground points always pass.
  /// </summary>
  [TypeId(Guids.SolipsistTargetLock)]
  internal class SolipsistTargetLock : BlueprintComponent, IAbilityTargetRestriction
  {
    public BlueprintFeature Fact;

    public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target)
    {
      try
      {
        if (Fact is null || caster is null || !caster.HasFact(Fact))
        {
          return true; // inert for everyone but the solipsist
        }
        var unit = target?.Unit;
        if (unit is null || unit == caster)
        {
          return true; // ground points and self pass
        }
        return !unit.IsAlly(caster); // friendly targets other than himself fail
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[solipsist] target check failed.", e);
        return true;
      }
    }

    public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target)
    {
      return "Solipsist: can only target himself with this spell";
    }
  }

  /// <summary>
  /// Runs after a selfish spell's own actions: if the caster carries the
  /// Solipsism fact and aimed the spell at himself, every recorded echo runs
  /// in the same context - same target, same duration evaluation, same
  /// metamagic. Echoes are element copies of the spell's own apply-buff
  /// actions (buff reference swapped to the untyped clone) and heal actions
  /// (a second, independent heal roll).
  /// </summary>
  [TypeId(Guids.SolipsistEchoAction)]
  internal class SolipsistEchoAction : ContextAction
  {
    public BlueprintFeature Fact;
    public ActionList Echoes;

    public override string GetCaption()
    {
      return "SolipsistEcho";
    }

    public override void RunAction()
    {
      try
      {
        var caster = Context?.Caster;
        if (caster is null || Fact is null || Echoes?.Actions is null ||
          Echoes.Actions.Length == 0)
        {
          return;
        }
        if (!caster.HasFact(Fact))
        {
          return; // inert for everyone but the solipsist
        }
        if (Context.MainTarget?.Unit != caster)
        {
          return; // only a self-aimed cast echoes
        }
        foreach (var echo in Echoes.Actions)
        {
          echo?.RunAction();
        }
        MissionFeats.Logger.Info(
          $"[solipsist] echo: blessing doubled on {caster.CharacterName}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[solipsist] echo action failed.", e);
      }
    }
  }
}

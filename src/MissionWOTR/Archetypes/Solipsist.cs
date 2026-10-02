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
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
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
  /// Solipsist (homebrew cleric archetype, user design - v2 after feedback:
  /// "if AoE buffs still work for the whole party the class has no real
  /// downside late-game, and it should be more martially usable").
  ///
  /// Design:
  /// - Solipsism: every cleric spell that could target an ally can now only
  ///   target the solipsist himself (enemy targets are unaffected - cure
  ///   spells still sear the undead). The COMMUNAL / ground-aimed versions
  ///   of such spells are denied to him outright: a congregation of one has
  ///   no one to bless but himself, and late-game party buffing rides almost
  ///   entirely on communals - leaving them open would leave the archetype
  ///   with no real downside. Every blessing such a spell applies to him is
  ///   laid down a SECOND time as an untyped echo copy that stacks with the
  ///   original and with everything else; healing spells roll their healing
  ///   twice; and his personal-range battle blessings (divine power,
  ///   righteous might, frightful aspect) echo as well. Channel energy is
  ///   traded away entirely.
  /// - Martial Devotion: a church of one must be its own church militant.
  ///   Full (fighter) base attack bonus - the engine only offers the three
  ///   tables, and the step up from the cleric's 3/4 is the fighter's - plus
  ///   martial weapon proficiency and one bonus combat feat from the
  ///   fighter's list (the vanilla Crusader bonus-feat pattern).
  ///
  /// Engine notes:
  /// - The lock is a custom IAbilityTargetRestriction component and the
  ///   communal denial a custom IAbilityCasterRestriction component, both
  ///   ADDED to the shared vanilla spell abilities; both are inert for every
  ///   caster without the Solipsism fact, so the shared blueprints stay safe
  ///   for all other classes.
  /// - Selection: the scan walks the cleric spellbook's spell list
  ///   (book.SpellList.SpellsByLevel, the pplus MadScientistPrep idiom) plus
  ///   ability variants. Point-targeted spells with a Helpful effect on
  ///   allies are denied (communal); personal-range spells with buffs echo
  ///   (no lock needed - they are already self-only); unit-targeted spells
  ///   that are Helpful on allies or carry heals get the lock, and the echo
  ///   when they carry buffs or heals.
  /// - The 2x is realized exactly as requested: for every
  ///   ContextActionApplyBuff in the spell's action tree a CLONE of the buff
  ///   blueprint is created (CopyFrom), every bonus descriptor inside the
  ///   clone is rewritten to None (untyped - so it stacks with the original's
  ///   typed bonus and cannot be overridden by same-type effects), and a copy
  ///   of the apply-buff element with the buff reference swapped to the clone
  ///   is appended to the spell's EXISTING action list (no second
  ///   run-action component - ordering after the originals is guaranteed).
  ///   ContextActionHealTarget elements are copied verbatim: the second run
  ///   re-rolls the heal in the same context.
  /// - Conditional-gated tiers (spells that scale by caster level inside a
  ///   Conditional branch) are deep-copied gate and all: the echo re-evaluates
  ///   the same conditions at cast time, so the correct tier's echo applies -
  ///   never both tiers, never the wrong one.
  /// - Clone blueprints derive their guids deterministically from the
  ///   original buff's guid (XOR a fixed mask) so they are save-stable.
  /// - Domain spells are not part of the scanned list; harmful point-target
  ///   spells (selective fireballs and the like) are untouched - the
  ///   solipsist is selfish, not harmless.
  /// Log prefix: [solipsist].
  /// </summary>
  internal static class Solipsist
  {
    internal const string ArchetypeName = "SolipsistArchetype";
    internal const string SolipsismName = "SolipsistSolipsism";
    internal const string MartialDevotionName = "SolipsistMartialDevotion";

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

      // ----- Solipsism (the casting half of the kit) -----
      var solipsism = FeatureConfigurator.New(SolipsismName, Guids.SolipsistFeature)
        .SetDisplayName("SolipsistSolipsism.Name")
        .SetDescription("SolipsistSolipsism.Description")
        .SetIcon(FeatureRefs.ChannelEnergyFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      // 0.53.0 — the doubling used to ride on the Solipsism feature, and that
      // feature lands at the cleric's channel-energy level, which is 1st. So
      // one level of cleric bought permanent buff-doubling: pay nothing,
      // double divine power and righteous might for the rest of the campaign.
      // The echo is now its own feature and comes online later.
      //
      // The *downside* still arrives at 1st, deliberately: no channel energy
      // and no flock to bless is the archetype's cost and must not be
      // avoidable by dipping. Only the payoff waits.
      var echo = FeatureConfigurator.New("SolipsistEcho", Guids.SolipsistEchoFeature)
        .SetDisplayName("SolipsistEcho.Name")
        .SetDescription("SolipsistEcho.Description")
        .SetIcon(FeatureRefs.ChannelEnergyFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      // ----- Martial Devotion (the steel half) -----
      var martialDevotion = FeatureConfigurator.New(MartialDevotionName, Guids.SolipsistMartialDevotion)
        .SetDisplayName("SolipsistMartialDevotion.Name")
        .SetDescription("SolipsistMartialDevotion.Description")
        .SetIcon(FeatureRefs.MartialWeaponProficiency.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { FeatureRefs.MartialWeaponProficiency.Reference.Get() })
        .Configure();

      // ----- Focused Faith / Toughened Faith (flat martial bonuses) -----
      // The engine offers only low/medium/full BAB tables - "slightly higher
      // than the cleric's 3/4" cannot be expressed as a table, so the BAB
      // bump is replaced (per user feedback) with flat untyped bonuses:
      // +1 attack at 5th and +2 at 15th; +5 hit points at 10th and +10 at 20th.
      var focusedFaith1 = FaithBonusFeature(
        "SolipsistFocusedFaith1", Guids.SolipsistFocusedFaith1, "SolipsistFocusedFaith", attack: 1);
      var focusedFaith2 = FaithBonusFeature(
        "SolipsistFocusedFaith2", Guids.SolipsistFocusedFaith2, "SolipsistFocusedFaith", attack: 2);
      var toughenedFaith1 = FaithBonusFeature(
        "SolipsistToughenedFaith1", Guids.SolipsistToughenedFaith1, "SolipsistToughenedFaith", hitPoints: 5);
      var toughenedFaith2 = FaithBonusFeature(
        "SolipsistToughenedFaith2", Guids.SolipsistToughenedFaith2, "SolipsistToughenedFaith", hitPoints: 10);

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.SolipsistArchetype, CharacterClassRefs.ClericClass)
          .SetLocalizedName("Solipsist.Name")
          .SetLocalizedDescription("Solipsist.Description")
          // Martial Devotion: martial weapons and one bonus combat feat from
          // the fighter's list (the vanilla Crusader bonus-feat pattern).
          .AddToAddFeatures(LevelPlan.L(1), martialDevotion)
          .AddToAddFeatures(LevelPlan.L(1), FeatureSelectionRefs.FighterFeatSelection.Reference.Get())
          // Focused Faith: flat untyped attack, +1 at 5th and +2 at 15th.
          .AddToAddFeatures(LevelPlan.L(5), focusedFaith1)
          .AddToAddFeatures(LevelPlan.L(15), focusedFaith2)
          // Toughened Faith: flat untyped hit points, +5 at 10th and +10 at 20th.
          .AddToAddFeatures(LevelPlan.L(10), toughenedFaith1)
          .AddToAddFeatures(LevelPlan.L(20), toughenedFaith2);

      // The selfish priest has no flock: channel energy is traded away.
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, cleric, "ChannelEnergyFeature", "ChannelEnergySelection");
      var channelLevel = ArchetypeRemovals.FindLevel(
        cleric.Progression, "ChannelEnergyFeature", "ChannelEnergySelection");

      archetype
        .AddToAddFeatures(LevelPlan.L(channelLevel), solipsism)
        // The echo comes online at 8th — deep enough that nobody dips for it,
        // and it lines up with the archetype's other mid-tier spike.
        .AddToAddFeatures(LevelPlan.L(8), echo)
        .Configure();

      ApplyToClericSpells(solipsism, echo);

      MissionFeats.Logger.Info("Solipsist: configured.");
    }

    private static BlueprintFeature FaithBonusFeature(
      string name, string guid, string displayBase, int attack = 0, int hitPoints = 0)
    {
      return FeatureConfigurator.New(name, guid)
        .SetDisplayName(displayBase + ".Name")
        .SetDescription(displayBase + ".Description")
        .SetIcon(attack > 0
          ? FeatureRefs.Dodge.Reference.Get().Icon
          : FeatureRefs.IronWill.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new SolipsistFlatBonus { AttackBonus = attack, HitPoints = hitPoints })
        .Configure();
    }

    // ------------------------------------------------------------------
    // Spell scan: lock, deny, and echo every ally-affecting cleric spell.
    // ------------------------------------------------------------------

    private static void ApplyToClericSpells(
      BlueprintFeature lockFact, BlueprintFeature echoFact)
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
      // Variant sub-abilities (alignment circles, communal versions and the
      // like) are the real castable spells - they must carry the lock, the
      // denial, and the echo too.
      for (int i = 0; i < abilities.Count; i++)
      {
        var variants = abilities[i].GetComponent<AbilityVariants>();
        if (variants?.Variants != null)
        {
          abilities.AddRange(variants.Variants.Where(variant => variant != null));
        }
      }

      int locked = 0, doubled = 0, denied = 0, clones = 0;
      foreach (var ability in abilities)
      {
        ProcessAbility(ability, lockFact, echoFact,
          ref locked, ref doubled, ref denied, ref clones);
      }
      MissionFeats.Logger.Info(
        $"[solipsist] spell scan: {abilities.Count} abilities seen, {locked} locked to self, " +
        $"{doubled} with the doubled echo, {denied} communal versions denied, " +
        $"{clones} echo buffs created.");
    }

    private static void ProcessAbility(
      BlueprintAbility ability,
      BlueprintFeature fact,
      BlueprintFeature echoFact,
      ref int locked,
      ref int doubled,
      ref int denied,
      ref int clones)
    {
      var key = ability.AssetGuid.ToString();
      if (ProcessedAbilities.Contains(key))
      {
        return;
      }
      ProcessedAbilities.Add(key);
      if (HasConversion(ability))
      {
        return; // already converted (idempotency guard)
      }

      bool pointTargeted = ability.CanTargetPoint;
      bool personal = ability.Range == AbilityRange.Personal;
      if (!ability.CanTargetFriends && !pointTargeted && !personal)
      {
        return;
      }

      var (hasBuffs, hasHeals) = ScanTargets(ability);
      bool helpful = ability.EffectOnAlly == AbilityEffectOnUnit.Helpful;

      // Communal / ground-aimed blessings: denied outright. Late-game party
      // buffing rides on these - leaving them open would leave the archetype
      // without a real downside.
      if (pointTargeted && helpful)
      {
        AbilityConfigurator.For(ability.ToReference<BlueprintAbilityReference>())
          .AddComponent(new SolipsistCommunalBlock { Fact = fact })
          .Configure();
        denied++;
        MissionFeats.Logger.Info(
          $"[solipsist] {ability.name}: communal version DENIED (a congregation of one).");
        return;
      }

      // Personal-range blessings: already self-only, so no lock - but the
      // echo applies (divine power, righteous might and friends are where a
      // martial cleric's late-game money is).
      if (personal)
      {
        if (!hasBuffs)
        {
          return;
        }
        AttachEcho(ability, echoFact, ref clones);
        doubled++;
        MissionFeats.Logger.Info($"[solipsist] {ability.name}: personal blessing echoes (doubled).");
        return;
      }

      // Unit-targeted ally spells: locked to self, echoed when they carry
      // buffs or heals.
      if (!helpful && !hasHeals)
      {
        return;
      }
      AbilityConfigurator.For(ability.ToReference<BlueprintAbilityReference>())
        .AddComponent(new SolipsistTargetLock { Fact = fact })
        .Configure();
      locked++;
      if (hasBuffs || hasHeals)
      {
        AttachEcho(ability, echoFact, ref clones);
        doubled++;
      }
      MissionFeats.Logger.Info(
        $"[solipsist] {ability.name}: locked to self" +
        (hasBuffs || hasHeals ? " (+echo)." : "."));
    }

    /// <summary>True when this ability already carries any solipsist conversion.</summary>
    private static bool HasConversion(BlueprintAbility ability)
    {
      if (ability.GetComponent<SolipsistTargetLock>() != null ||
        ability.GetComponent<SolipsistCommunalBlock>() != null)
      {
        return true;
      }
      foreach (var runAction in ability.GetComponents<AbilityEffectRunAction>())
      {
        if (runAction?.Actions?.Actions is null)
        {
          continue;
        }
        foreach (var action in runAction.Actions.Actions)
        {
          if (action is SolipsistEchoAction)
          {
            return true;
          }
        }
      }
      return false;
    }

    // ------------------------------------------------------------------
    // Detection: does the spell carry buffs or heals anywhere in its tree?
    // ------------------------------------------------------------------

    private static (bool HasBuffs, bool HasHeals) ScanTargets(BlueprintAbility ability)
    {
      bool buffs = false, heals = false;
      foreach (var runAction in ability.GetComponents<AbilityEffectRunAction>())
      {
        if (runAction?.Actions?.Actions != null)
        {
          Scan(runAction.Actions.Actions, ref buffs, ref heals);
        }
      }
      return (buffs, heals);
    }

    private static void Scan(GameAction[] actions, ref bool buffs, ref bool heals)
    {
      foreach (var action in actions)
      {
        if (action is null)
        {
          continue;
        }
        if (action is ContextActionApplyBuff)
        {
          buffs = true;
          continue;
        }
        if (action is ContextActionHealTarget)
        {
          heals = true;
          continue;
        }
        foreach (var nested in NestedActionLists(action))
        {
          Scan(nested, ref buffs, ref heals);
        }
      }
    }

    private static bool HasTargets(GameAction action)
    {
      if (action is ContextActionApplyBuff || action is ContextActionHealTarget)
      {
        return true;
      }
      foreach (var nested in NestedActionLists(action))
      {
        foreach (var nestedAction in nested)
        {
          if (nestedAction != null && HasTargets(nestedAction))
          {
            return true;
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Action lists nested inside an action (fields and GameAction arrays) -
    /// includes the branches of Conditional gates.
    /// </summary>
    private static IEnumerable<GameAction[]> NestedActionLists(GameAction action)
    {
      var type = action.GetType();
      foreach (var field in type.GetFields(
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
      {
        if (field.FieldType == typeof(ActionList))
        {
          var list = (ActionList)field.GetValue(action);
          if (list?.Actions != null && list.Actions.Length > 0)
          {
            yield return list.Actions;
          }
        }
        else if (field.FieldType.IsArray &&
          typeof(GameAction).IsAssignableFrom(field.FieldType.GetElementType()))
        {
          var nested = (GameAction[])field.GetValue(action);
          if (nested != null && nested.Length > 0)
          {
            yield return nested;
          }
        }
      }
    }

    // ------------------------------------------------------------------
    // Echo construction.
    // ------------------------------------------------------------------

    /// <summary>
    /// Appends one echo action to each of the spell's run-action lists that
    /// carry echoable content. Appending to the EXISTING list (instead of a
    /// second run-action component) guarantees the echo runs right after the
    /// spell's own actions.
    /// </summary>
    private static void AttachEcho(BlueprintAbility ability, BlueprintFeature fact, ref int clones)
    {
      foreach (var runAction in ability.GetComponents<AbilityEffectRunAction>())
      {
        if (runAction?.Actions?.Actions is null || runAction.Actions.Actions.Length == 0)
        {
          continue;
        }
        var echoes = new List<GameAction>();
        BuildEchoes(runAction.Actions.Actions, echoes, ref clones);
        if (echoes.Count == 0)
        {
          continue;
        }
        var echo = new SolipsistEchoAction
        {
          Fact = fact,
          Echoes = new ActionList { Actions = echoes.ToArray() },
        };
        var merged = new List<GameAction>(runAction.Actions.Actions) { echo };
        runAction.Actions = new ActionList { Actions = merged.ToArray() };
      }
    }

    /// <summary>
    /// Builds the echo units for one action list: apply-buff and heal leaves
    /// are substituted copies; a Conditional gate containing targets is
    /// deep-copied whole (its conditions re-evaluate at echo time, so the
    /// correct tier echoes); plain wrappers are flattened into their
    /// payloads.
    /// </summary>
    private static void BuildEchoes(
      GameAction[] actions, List<GameAction> echoes, ref int clones)
    {
      foreach (var action in actions)
      {
        if (action is null)
        {
          continue;
        }
        if (TryEchoLeaf(action, echoes, ref clones))
        {
          continue;
        }
        if (action.GetType().Name == "Conditional")
        {
          if (HasTargets(action))
          {
            var copy = CopyDeep(action, ref clones);
            if (copy != null)
            {
              echoes.Add(copy);
            }
          }
          continue;
        }
        foreach (var nested in NestedActionLists(action))
        {
          BuildEchoes(nested, echoes, ref clones);
        }
      }
    }

    private static bool TryEchoLeaf(GameAction action, List<GameAction> echoes, ref int clones)
    {
      if (action is ContextActionApplyBuff applyBuff)
      {
        var original = BuffOf(applyBuff);
        if (original != null)
        {
          var clone = GetOrCreateEchoClone(original, ref clones);
          if (clone != null)
          {
            echoes.Add(CopyWithBuff(applyBuff, clone));
          }
        }
        return true;
      }
      if (action is ContextActionHealTarget heal)
      {
        echoes.Add(CopyHeal(heal));
        return true;
      }
      return false;
    }

    /// <summary>
    /// Deep copy of an action with buff substitution: apply-buff and heal
    /// leaves become echo copies; every other action is copied structurally,
    /// recursing into nested action lists. Shared sub-objects (conditions,
    /// context values) are referenced, not cloned - they are read-only
    /// evaluators.
    /// </summary>
    private static GameAction CopyDeep(GameAction action, ref int clones)
    {
      if (action is ContextActionApplyBuff applyBuff)
      {
        var original = BuffOf(applyBuff);
        if (original is null)
        {
          return null;
        }
        var clone = GetOrCreateEchoClone(original, ref clones);
        return clone != null ? CopyWithBuff(applyBuff, clone) : null;
      }
      if (action is ContextActionHealTarget heal)
      {
        return CopyHeal(heal);
      }

      var copy = (GameAction)Activator.CreateInstance(action.GetType());
      var type = action.GetType();
      foreach (var field in type.GetFields(
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
      {
        if (field.IsInitOnly)
        {
          continue;
        }
        var value = field.GetValue(action);
        if (field.FieldType == typeof(ActionList))
        {
          var list = (ActionList)value;
          field.SetValue(
            copy,
            list?.Actions == null
              ? new ActionList()
              : new ActionList { Actions = CopyDeepAll(list.Actions, ref clones) });
        }
        else if (field.FieldType.IsArray &&
          typeof(GameAction).IsAssignableFrom(field.FieldType.GetElementType()))
        {
          var nested = (GameAction[])value;
          field.SetValue(
            copy, nested == null ? null : CopyDeepAll(nested, ref clones));
        }
        else
        {
          field.SetValue(copy, value);
        }
      }
      return copy;
    }

    private static GameAction[] CopyDeepAll(GameAction[] actions, ref int clones)
    {
      var result = new List<GameAction>();
      foreach (var action in actions)
      {
        if (action is null)
        {
          continue;
        }
        var copy = CopyDeep(action, ref clones);
        if (copy != null)
        {
          result.Add(copy);
        }
      }
      return result.ToArray();
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
        // Visual identity rides CopyFrom: the base fact configurator copies
        // the original's localized name, description and icon wholesale, so
        // the echo reads exactly like its original in the buff bar.
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
  /// The communal denial: ground-aimed ally blessings (Bless Communal, Resist
  /// Energy Communal and their whole family) are simply uncastable for the
  /// solipsist - his gifts bend inward, and a congregation of one has no use
  /// for the wide versions. Inert for every caster without the Solipsism
  /// fact, so other classes are untouched.
  /// </summary>
  [TypeId(Guids.SolipsistCommunalBlock)]
  internal class SolipsistCommunalBlock : BlueprintComponent, IAbilityCasterRestriction
  {
    public BlueprintFeature Fact;

    public bool IsCasterRestrictionPassed(UnitEntityData caster)
    {
      try
      {
        // Everyone except the solipsist casts communal spells normally.
        return Fact is null || caster is null || !caster.HasFact(Fact);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[solipsist] caster check failed.", e);
        return true;
      }
    }

    public string GetAbilityCasterRestrictionUIText()
    {
      return "Solipsist: a congregation of one has no communal blessings";
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
        var caster = Context?.MaybeCaster;
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

  /// <summary>
  /// The Solipsist's flat martial bonuses: an untyped attack bonus on every
  /// attack roll (the darkcodex AddAttackBonus pattern) and flat untyped hit
  /// points (the pplus ShadowDancerSpawn HitPoints-modifier pattern).
  /// </summary>
  [TypeId(Guids.SolipsistFlatBonus)]
  internal class SolipsistFlatBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonus>,
    IRulebookHandler<RuleCalculateAttackBonus>, ISubscriber, IInitiatorRulebookSubscriber
  {
    public int AttackBonus;
    public int HitPoints;

    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
    {
      if (AttackBonus != 0)
      {
        evt.AddModifier(AttackBonus, Fact, ModifierDescriptor.UntypedStackable);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt)
    {
    }

    protected override void OnTurnOn()
    {
      if (HitPoints != 0)
      {
        Owner.Stats.HitPoints.RemoveModifiersFrom(Runtime);
        Owner.Stats.HitPoints.AddModifier(HitPoints, Runtime, ModifierDescriptor.UntypedStackable);
      }
    }

    protected override void OnTurnOff()
    {
      Owner.Stats.HitPoints.RemoveModifiersFrom(Runtime);
    }
  }
}

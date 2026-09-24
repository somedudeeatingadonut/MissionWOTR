using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Arcane Artillerist (homebrew, user-commissioned: a DPS-focused arcanist to
  /// contrast the class's usual support/debuff builds).
  ///
  /// The kit trades the arcanist exploits gained at 1st, 3rd, 7th and 11th level
  /// for a raw spell-damage engine:
  /// - Weaponized Magic (1st): damaging spells you cast deal bonus damage equal to
  ///   half your arcanist level (minimum 1).
  /// - Overcharge (3rd): swift action, spend 1 arcane reservoir point - your next
  ///   damaging spell deals an additional 1d6 per two arcanist levels.
  /// - Detonation (7th): enemies killed by your spells detonate - other enemies
  ///   within 10 feet take 1d6 per two arcanist levels.
  /// - Annihilating Surge (11th): while you have at least 1 arcane reservoir point
  ///   remaining, Weaponized Magic bonus damage is doubled.
  ///
  /// Implementation notes: all riders are separate direct-damage instances
  /// triggered from outgoing spell damage (the TabletopTweaks Elemental Barrage
  /// detection pattern: evt.Reason.Ability ?? evt.Reason.Context.SourceAbility,
  /// skipping persistent-area ticks and the riders' own damage); flat values use
  /// DirectDamage with DiceFormula.Zero. Numeric tuning happens in playtest.
  /// </summary>
  internal static class ArcaneArtillerist
  {
    internal const string ArchetypeName = "ArcaneArtilleristArchetype";
    internal const string DisplayName = "ArcaneArtillerist.Name";
    internal const string Description = "ArcaneArtillerist.Description";

    internal const string WeaponizedName = "ArcaneArtilleristWeaponizedMagic";
    internal const string OverchargeFeatureName = "ArcaneArtilleristOvercharge";
    internal const string OverchargeAbilityName = "ArcaneArtilleristOverchargeAbility";
    internal const string OverchargeBuffName = "ArcaneArtilleristOverchargeBuff";
    internal const string DetonationName = "ArcaneArtilleristDetonation";
    internal const string SurgeName = "ArcaneArtilleristAnnihilatingSurge";

    // Vanilla: the arcane reservoir (TabletopTweaks-verified GUID).
    private const string ArcaneReservoirResourceGuid = "cac948cbbe79b55459459dd6a8fe44ce";
    // Vanilla: the exploit selection granted at every odd arcanist level.
    private const string ArcanistExploitSelectionGuid = "b8bf3d5023f2d8c428fdf6438cecaea7";

    public static void Configure()
    {
      var arcanist = CharacterClassRefs.ArcanistClass.Reference.Get();
      var reservoir = BlueprintTool.Get<BlueprintAbilityResource>(ArcaneReservoirResourceGuid);

      // ----- Overcharged: consumed by the next damaging spell -----
      var overchargeBuff = BuffConfigurator.New(OverchargeBuffName, Guids.ArtilleristOverchargeBuff)
        .SetDisplayName("ArcaneArtilleristOvercharge.Name")
        .SetDescription("ArcaneArtilleristOverchargeBuff.Description")
        .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
        .AddComponent(new ArtilleristOverchargeRider
        {
          CharacterClass = arcanist,
        })
        .Configure();

      // ----- Overcharge: swift action, spend 1 reservoir point -----
      var overchargeAbility = AbilityConfigurator.New(OverchargeAbilityName, Guids.ArtilleristOverchargeAbility)
        .SetDisplayName("ArcaneArtilleristOvercharge.Name")
        .SetDescription("ArcaneArtilleristOvercharge.Description")
        .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Swift)
        .SetCanTargetSelf()
        .AddAbilityResourceLogic(requiredResource: reservoir, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().ApplyBuff(OverchargeBuffName, ContextDuration.Fixed(60)))
        .Configure();

      // ----- Weaponized Magic (1st): flat rider on spell damage -----
      var weaponized = FeatureConfigurator.New(WeaponizedName, Guids.ArtilleristWeaponizedFeature)
        .SetDisplayName("ArcaneArtilleristWeaponized.Name")
        .SetDescription("ArcaneArtilleristWeaponized.Description")
        .SetIcon(AbilityRefs.BombStandart.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new ArtilleristSpellRider
        {
          CharacterClass = arcanist,
          Reservoir = reservoir,
          DoubleWithReservoir = true,
        })
        .Configure();

      // ----- Overcharge feature (3rd) -----
      var overchargeFeature = FeatureConfigurator.New(OverchargeFeatureName, Guids.ArtilleristOverchargeFeature)
        .SetDisplayName("ArcaneArtilleristOvercharge.Name")
        .SetDescription("ArcaneArtilleristOvercharge.Description")
        .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { overchargeAbility })
        .Configure();

      // ----- Detonation (7th): kills explode -----
      var detonation = FeatureConfigurator.New(DetonationName, Guids.ArtilleristDetonationFeature)
        .SetDisplayName("ArcaneArtilleristDetonation.Name")
        .SetDescription("ArcaneArtilleristDetonation.Description")
        .SetIcon(AbilityRefs.BloodragerInfernalHellfireStrikeAbility.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new ArtilleristDetonation { CharacterClass = arcanist })
        .Configure();

      // ----- Annihilating Surge (11th): passive - the doubling lives in the
      // Weaponized Magic rider (DoubleWithReservoir), this feature is the marker -----
      var surge = FeatureConfigurator.New(SurgeName, Guids.ArtilleristSurgeFeature)
        .SetDisplayName("ArcaneArtilleristSurge.Name")
        .SetDescription("ArcaneArtilleristSurge.Description")
        .SetIcon(FeatureRefs.RagingBrutality.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.ArtilleristArchetype, CharacterClassRefs.ArcanistClass)
          .SetLocalizedName(DisplayName)
          .SetLocalizedDescription(Description);

      // Trades the exploits gained at 1st, 3rd, 7th and 11th level (level-specific:
      // the selection appears at every odd level).
      archetype = archetype
        .AddToRemoveFeatures(1, ArcanistExploitSelectionGuid)
        .AddToRemoveFeatures(3, ArcanistExploitSelectionGuid)
        .AddToRemoveFeatures(7, ArcanistExploitSelectionGuid)
        .AddToRemoveFeatures(11, ArcanistExploitSelectionGuid);

      archetype = archetype
        .AddToAddFeatures(LevelPlan.L(1), WeaponizedName)
        .AddToAddFeatures(LevelPlan.L(3), OverchargeFeatureName)
        .AddToAddFeatures(LevelPlan.L(7), DetonationName)
        .AddToAddFeatures(LevelPlan.L(11), SurgeName);

      archetype.Configure();

      MissionFeats.Logger.Info("ArcaneArtillerist: configured.");
    }

    /// <summary>
    /// True when the damage event is spell damage dealt by this unit - not a
    /// persistent area tick, not this fact's own rider (Elemental Barrage pattern).
    /// </summary>
    internal static bool IsOwnSpellDamage(RuleDealDamage evt, UnitEntityData owner, EntityFact selfFact)
    {
      if (evt.Initiator != owner || evt.Target is null || !evt.Target.IsEnemy(owner))
      {
        return false;
      }
      if (evt.Reason.Fact == selfFact)
      {
        return false;
      }
      if (evt.SourceArea)
      {
        return false;
      }
      var ability = evt.Reason.Ability?.Blueprint ?? evt.Reason.Context?.SourceAbility;
      return ability?.Type == AbilityType.Spell;
    }

    /// <summary>Deals direct bonus damage to the target as a separate instance.</summary>
    internal static void DealRider(UnitEntityData caster, UnitEntityData target, EntityFact reason,
      DiceFormula dice, int flat)
    {
      var bundle = new DamageBundle();
      bundle.Add(new DirectDamage(dice, flat));
      Rulebook.Trigger(new RuleDealDamage(caster, target, bundle) { Reason = reason });
    }

    /// <summary>1d6 per two levels, minimum one die.</summary>
    internal static DiceFormula SurgeDice(int level)
    {
      return new DiceFormula(Math.Max(1, level / 2), DiceType.D6);
    }
  }

  /// <summary>
  /// Weaponized Magic (+ Annihilating Surge): your damaging spells deal bonus
  /// damage equal to half your arcanist level (min 1); doubled while at least one
  /// arcane reservoir point remains (DoubleWithReservoir).
  /// </summary>
  [TypeId(Guids.ArtilleristSpellRider)]
  internal class ArtilleristSpellRider : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass CharacterClass;
    public BlueprintAbilityResource Reservoir;
    public bool DoubleWithReservoir;

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        if (!ArcaneArtillerist.IsOwnSpellDamage(evt, Owner, Fact))
        {
          return;
        }
        int level = Owner.Descriptor.Progression.GetClassLevel(CharacterClass);
        int bonus = Math.Max(1, level / 2);
        if (DoubleWithReservoir && Reservoir != null
          && Owner.Resources.GetResourceAmount(Reservoir) >= 1)
        {
          bonus *= 2;
        }
        ArcaneArtillerist.DealRider(Owner, evt.Target, Fact, DiceFormula.Zero, bonus);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ArcaneArtillerist: Weaponized Magic rider failed.", e);
      }
    }
  }

  /// <summary>
  /// Overcharged: the next damaging spell deals an additional 1d6 per two arcanist
  /// levels, then the buff is consumed.
  /// </summary>
  [TypeId(Guids.ArtilleristOverchargeRider)]
  internal class ArtilleristOverchargeRider : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass CharacterClass;
    public BlueprintBuff Buff;

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        if (!ArcaneArtillerist.IsOwnSpellDamage(evt, Owner, Fact))
        {
          return;
        }
        int level = Owner.Descriptor.Progression.GetClassLevel(CharacterClass);
        ArcaneArtillerist.DealRider(Owner, evt.Target, Fact, ArcaneArtillerist.SurgeDice(level), 0);
        Owner.Buffs.RemoveFact(Buff);
        MissionFeats.Logger.Info(
          $"[artillerist] Overcharge consumed: +{Math.Max(1, level / 2)}d6 on {evt.Target.CharacterName}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ArcaneArtillerist: Overcharge rider failed.", e);
      }
    }
  }

  /// <summary>
  /// Detonation: enemies brought to 0 HP by your spells detonate - other enemies
  /// within 10 feet take 1d6 per two arcanist levels. Each dying enemy detonates
  /// once.
  /// </summary>
  [TypeId(Guids.ArtilleristDetonation)]
  internal class ArtilleristDetonation : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass CharacterClass;

    private static readonly HashSet<string> Detonated = new();

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        if (!ArcaneArtillerist.IsOwnSpellDamage(evt, Owner, Fact))
        {
          return;
        }
        var victim = evt.Target;
        if (victim.HPLeft > 0 || Detonated.Contains(victim.UniqueId))
        {
          return;
        }
        Detonated.Add(victim.UniqueId);
        if (Detonated.Count > 64)
        {
          Detonated.Clear();
        }
        int level = Owner.Descriptor.Progression.GetClassLevel(CharacterClass);
        int hits = 0;
        foreach (var u in Game.Instance.State.LoadedAreaState.MainState.AllEntityData
          .OfType<UnitEntityData>())
        {
          if (u != victim && u.HPLeft > 0 && u.IsEnemy(Owner)
            && Vector3.Distance(u.Position, victim.Position) <= 3.5f)
          {
            ArcaneArtillerist.DealRider(Owner, u, Fact, ArcaneArtillerist.SurgeDice(level), 0);
            hits++;
          }
        }
        if (hits > 0)
        {
          MissionFeats.Logger.Info(
            $"[artillerist] Detonation: {victim.CharacterName}'s death hit {hits} enemies.");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ArcaneArtillerist: Detonation failed.", e);
      }
    }
  }
}

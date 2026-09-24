using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Blueprints.Configurators;
using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Breaker (faithful port of the APG barbarian archetype; WOTR adaptations noted
  /// in docs/ARCHETYPES.md).
  ///
  /// Tabletop:
  /// - Destructive: +1/2 barbarian level (min 1) damage on sunder combat maneuvers
  ///   and melee attacks against unattended objects. Replaces fast movement.
  /// - Battle Scavenger (3rd): no attack penalty with improvised or broken weapons,
  ///   +1 damage with them per three levels beyond 3rd. Replaces trap sense.
  ///
  /// WOTR adaptations (engine gaps):
  /// - No sunder maneuver, no unattended-object targets, no improvised or broken
  ///   weapon states exist in Wrath. Destructive therefore adds its bonus damage on
  ///   weapon attacks against CONSTRUCTS (the game's construct detector is the
  ///   ConstructType feature) - the closest living translation of "smash the
  ///   lifeless thing".
  /// - Battle Scavenger keeps the fantasy with "scavenged arms" - the simple
  ///   weapons a battlefield looter would grab (club, greatclub, quarterstaff,
  ///   spear, dagger, sickle, maces) - granting scaling bonus damage with them.
  ///   Trap sense does not exist on the Wrath barbarian, so that half of the trade
  ///   is void and the feature is simply additive.
  /// </summary>
  internal static class Breaker
  {
    internal const string ArchetypeName = "BreakerArchetype";
    internal const string DisplayName = "Breaker.Name";
    internal const string Description = "Breaker.Description";

    internal const string DestructiveName = "BreakerDestructive";
    internal const string ScavengerName = "BreakerBattleScavenger";

    public static void Configure()
    {
      try
      {
        ConfigureFeatures();
        ConfigureArchetype();
        Main.Logger.Info("[CC-free] Breaker configured.");
      }
      catch (Exception e)
      {
        Main.Logger.Error("Breaker: configuration failed.", e);
      }
    }

    private static void ConfigureFeatures()
    {
      var constructType = FeatureRefs.ConstructType.Reference.Get();

      FeatureConfigurator.New(DestructiveName, Guids.BreakerDestructiveFeature)
        .SetDisplayName("BreakerDestructive.Name")
        .SetDescription("BreakerDestructive.Description")
        .SetIcon(FeatureRefs.ImprovedSunder.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new BreakerDestructiveDamage { ConstructType = constructType })
        .Configure();

      FeatureConfigurator.New(ScavengerName, Guids.BreakerScavengerFeature)
        .SetDisplayName("BreakerScavenger.Name")
        .SetDescription("BreakerScavenger.Description")
        .SetIcon(FeatureRefs.ImprovedUnarmedStrike.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new BreakerScavengerDamage())
        .Configure();
    }

    private static void ConfigureArchetype()
    {
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.BreakerArchetype, CharacterClassRefs.BarbarianClass)
          .SetLocalizedName(DisplayName)
          .SetLocalizedDescription(Description);

      // Destructive replaces fast movement (granted at barbarian 3 in Wrath).
      archetype = ArchetypeRemovals.AddRemovals(
        archetype,
        CharacterClassRefs.BarbarianClass.Reference.Get(),
        FeatureRefs.FastMovement.ToString());

      // Both features arrive with the level-3 trade (tabletop: Destructive is the
      // fast-movement replacement, Battle Scavenger lands at 3rd).
      archetype = archetype
        .AddToAddFeatures(LevelPlan.L(3), DestructiveName, ScavengerName);

      if (LevelPlan.AllAtLevelOne)
      {
        // TEST MODE: everything at level 1.
        archetype = archetype.AddToAddFeatures(1, DestructiveName, ScavengerName);
      }
      archetype.Configure();
    }
  }

  /// <summary>
  /// Destructive: bonus damage equal to half the barbarian level (minimum 1) on
  /// weapon attacks that hit a construct. Delivered as a direct damage rider on
  /// the same target (the ConstructSonicBoom pattern).
  /// </summary>
  [TypeId(Guids.BreakerDestructiveDamage)]
  internal class BreakerDestructiveDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>
  {
    public BlueprintFeature ConstructType;

    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt)
    {
      try
      {
        if (evt.AttackRoll is null || !evt.AttackRoll.IsHit)
        {
          return;
        }
        var target = evt.Target;
        if (target is null || target.HPLeft <= 0)
        {
          return;
        }
        if (ConstructType is null || !target.HasFact(ConstructType))
        {
          return;
        }
        var barbarian = CharacterClassRefs.BarbarianClass.Reference.Get();
        var level = Owner.Descriptor.Progression.GetClassLevel(barbarian);
        var bonus = Math.Max(1, level / 2);
        if (bonus <= 0)
        {
          return;
        }
        var bundle = new DamageBundle();
        bundle.Add(new DirectDamage(new DiceFormula(0, DiceType.Zero), bonus));
        Rulebook.Trigger(new RuleDealDamage(Owner, target, bundle) { Reason = Fact });
        MissionFeats.Logger.Info(
          $"[breaker] Destructive rider: {bonus} damage vs construct {target.CharacterName}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Breaker: Destructive rider failed.", e);
      }
    }
  }

  /// <summary>
  /// Battle Scavenger (WOTR adaptation): +1 bonus damage with scavenged arms
  /// (club, greatclub, quarterstaff, spear, dagger, sickle, maces) per three
  /// barbarian levels beyond 3rd.
  /// </summary>
  [TypeId(Guids.BreakerScavengerDamage)]
  internal class BreakerScavengerDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>
  {
    private static readonly HashSet<string> ScavengedArms = new(StringComparer.OrdinalIgnoreCase)
    {
      "Club", "Greatclub", "Quarterstaff", "Spear", "Dagger", "Sickle",
      "HeavyMace", "LightMace", "HeavyFlail", "LightFlail",
    };

    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt)
    {
      try
      {
        if (evt.AttackRoll is null || !evt.AttackRoll.IsHit)
        {
          return;
        }
        var target = evt.Target;
        if (target is null || target.HPLeft <= 0)
        {
          return;
        }
        var weapon = evt.Weapon?.Blueprint?.name;
        if (weapon is null || !ScavengedArms.Contains(weapon))
        {
          return;
        }
        var barbarian = CharacterClassRefs.BarbarianClass.Reference.Get();
        var level = Owner.Descriptor.Progression.GetClassLevel(barbarian);
        var bonus = level > 3 ? 1 + (level - 4) / 3 : 0;
        if (bonus <= 0)
        {
          return;
        }
        var bundle = new DamageBundle();
        bundle.Add(new DirectDamage(new DiceFormula(0, DiceType.Zero), bonus));
        Rulebook.Trigger(new RuleDealDamage(Owner, target, bundle) { Reason = Fact });
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Breaker: Scavenger rider failed.", e);
      }
    }
  }
}

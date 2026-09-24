using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
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
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Commands.Base;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Bloodstorm (homebrew barbarian archetype, user-approved pitch).
  ///
  /// Rage turns wounds into weather. While raging, the bloodstorm's weapon attacks
  /// open Bleeding Wounds that stack (each application adds a rank, up to five);
  /// she gains fast healing while enemies within 30 feet are bleeding (scaling with
  /// how many), her critical hits against bleeding targets spray blood and fear
  /// onto nearby foes, and Open the Floodgate lets her consume every bleed stack on
  /// the battlefield for a burst of self-healing.
  ///
  /// Tradeoffs: uncanny dodge (2), improved uncanny dodge (5) and the damage
  /// reduction chain (7/10/13/16/19) - all offense, no safety net.
  ///
  /// REAL LEVEL PLAN: Bloodstorm 2, Bloodspout 5, Floodgate 7.
  /// TEST MODE: everything at 1.
  /// </summary>
  internal static class Bloodstorm
  {
    internal const string ArchetypeName = "BloodstormArchetype";
    internal const string DisplayName = "Bloodstorm.Name";
    internal const string Description = "Bloodstorm.Description";

    internal const string FeatureName = "BloodstormFeature";
    internal const string BleedBuffName = "BloodstormBleedingWound";
    internal const string BloodspoutName = "BloodstormBloodspout";
    internal const string FloodgateFeatureName = "BloodstormFloodgateFeature";
    internal const string FloodgateAbilityName = "BloodstormFloodgateAbility";

    /// <summary>Maximum Bleeding Wound ranks on one target.</summary>
    internal const int MaxBleedRanks = 5;

    internal static BlueprintBuff BleedBuff;
    internal static BlueprintBuff[] RageBuffs;

    public static void Configure()
    {
      try
      {
        ConfigureBleedBuff();
        ConfigureFeatures();
        ConfigureArchetype();
        Main.Logger.Info("Bloodstorm configured.");
      }
      catch (Exception e)
      {
        Main.Logger.Error("Bloodstorm: configuration failed.", e);
      }
    }

    private static void ConfigureBleedBuff()
    {
      BleedBuff = BuffConfigurator.New(BleedBuffName, Guids.BloodstormBleedBuff)
        .SetDisplayName("BloodstormBleed.Name")
        .SetDescription("BloodstormBleed.Description")
        .SetIcon(BuffRefs.Bleed1d4Buff.Reference.Get().Icon)
        .AddComponent(new BloodstormBleedTick { MaxRanks = MaxBleedRanks })
        .Configure();
    }

    private static void ConfigureFeatures()
    {
      RageBuffs = new[]
      {
        BuffRefs.RageBuff.Reference.Get(),
        BuffRefs.RageBuffNoFX.Reference.Get(),
      };

      // The L2 package: Crimson Edge (stacking bleed on hit) + Feed the Storm
      // (fast healing while nearby enemies bleed).
      FeatureConfigurator.New(FeatureName, Guids.BloodstormFeature)
        .SetDisplayName("BloodstormFeature.Name")
        .SetDescription("BloodstormFeature.Description")
        .SetIcon(FeatureRefs.RagingBrutality.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new BloodstormOnHit { Bleed = BleedBuff, Rage = RageBuffs, MaxRanks = MaxBleedRanks })
        .AddComponent(new BloodstormFeed { Rage = RageBuffs })
        .Configure();

      // L5: critical hits against bleeding targets spray blood and fear.
      FeatureConfigurator.New(BloodspoutName, Guids.BloodspoutFeature)
        .SetDisplayName("Bloodspout.Name")
        .SetDescription("Bloodspout.Description")
        .SetIcon(FeatureRefs.CriticalFocus.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new BloodstormCritSpray { Bleed = BleedBuff, Rage = RageBuffs, MaxRanks = MaxBleedRanks })
        .Configure();

      // L7: Open the Floodgate - consume all bleed stacks nearby for self-healing.
      var floodgateAction = ElementTool.Create<ContextActionBloodstormFloodgate>();
      floodgateAction.Bleed = BleedBuff;
      floodgateAction.Rage = RageBuffs;

      var ability = AbilityConfigurator.New(FloodgateAbilityName, Guids.BloodstormFloodgateAbility)
        .SetDisplayName("BloodstormFloodgate.Name")
        .SetDescription("BloodstormFloodgate.Description")
        .SetIcon(FeatureRefs.Toughness.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetSelf()
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(floodgateAction))
        .Configure();

      FeatureConfigurator.New(FloodgateFeatureName, Guids.BloodstormFloodgateFeature)
        .SetDisplayName("BloodstormFloodgate.Name")
        .SetDescription("BloodstormFloodgate.Description")
        .SetIcon(FeatureRefs.Toughness.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new[] { ability })
        .Configure();
    }

    private static void ConfigureArchetype()
    {
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.BloodstormArchetype, CharacterClassRefs.BarbarianClass)
          .SetLocalizedName(DisplayName)
          .SetLocalizedDescription(Description);

      // Trades: uncanny dodge (2), improved uncanny dodge (5), damage reduction
      // (7/10/13/16/19). Both uncanny-dodge spellings are passed because the
      // class progression's exact entry is matched live (unmatched ones warn).
      archetype = ArchetypeRemovals.AddRemovals(
        archetype,
        CharacterClassRefs.BarbarianClass.Reference.Get(),
        FeatureRefs.BarbarianUncannyDodge.ToString(),
        FeatureRefs.UncannyDodge.ToString(),
        FeatureRefs.ImprovedUncannyDodge.ToString(),
        FeatureRefs.DamageReduction.ToString());

      archetype = archetype
        .AddToAddFeatures(LevelPlan.L(2), FeatureName)
        .AddToAddFeatures(LevelPlan.L(5), BloodspoutName)
        .AddToAddFeatures(LevelPlan.L(7), FloodgateFeatureName);

      if (LevelPlan.AllAtLevelOne)
      {
        // TEST MODE: everything at level 1.
        archetype = archetype.AddToAddFeatures(1, FeatureName, BloodspoutName, FloodgateFeatureName);
      }
      archetype.Configure();
    }

    /// <summary>True while the unit is under either stock rage buff.</summary>
    internal static bool IsRaging(UnitEntityData unit, BlueprintBuff[] rage)
    {
      if (unit is null || rage is null)
      {
        return false;
      }
      foreach (var buff in rage)
      {
        if (buff != null && unit.Buffs.GetBuff(buff) != null)
        {
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Adds one Bleeding Wound rank to the target (first application creates the
    /// buff, later ones add ranks, capped at MaxRanks). The context comes from the
    /// calling component - class features carry a working context (the same
    /// mechanism ContextRankConfig on class features relies on).
    /// </summary>
    internal static void ApplyBleedRank(
      UnitEntityData target, MechanicsContext context, BlueprintBuff bleed, int maxRanks)
    {
      var existing = target.Buffs.GetBuff(bleed);
      if (existing != null)
      {
        if (existing.Ranks >= maxRanks)
        {
          return;
        }
        existing.AddRank();
        return;
      }
      target.AddBuff(bleed, context);
    }
  }

  /// <summary>
  /// Bleeding Wound: 1d4 damage per rank each round, dealt directly (the
  /// PeriodicSelfDamage pattern, inverted onto the victim).
  /// </summary>
  [TypeId(Guids.BloodstormBleedTick)]
  internal class BloodstormBleedTick : UnitFactComponentDelegate, ITickEachRound
  {
    public int MaxRanks;

    public void OnNewRound()
    {
      try
      {
        if (Owner.HPLeft <= 0)
        {
          return;
        }
        var ranks = Math.Max(1, Fact.Ranks);
        var source = Fact.Context?.MaybeCaster ?? Owner;
        var bundle = new DamageBundle();
        bundle.Add(new DirectDamage(new DiceFormula(ranks, DiceType.D4), 0));
        Rulebook.Trigger(new RuleDealDamage(source, Owner, bundle) { Reason = Fact });
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Bloodstorm: bleed tick failed.", e);
      }
    }
  }

  /// <summary>
  /// Crimson Edge: while raging, weapon hits apply one Bleeding Wound rank.
  /// </summary>
  [TypeId(Guids.BloodstormOnHit)]
  internal class BloodstormOnHit : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>
  {
    public BlueprintBuff Bleed;
    public BlueprintBuff[] Rage;
    public int MaxRanks;

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
        if (!Bloodstorm.IsRaging(Owner, Rage))
        {
          return;
        }
        Bloodstorm.ApplyBleedRank(target, Context, Bleed, MaxRanks);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Bloodstorm: Crimson Edge failed.", e);
      }
    }
  }

  /// <summary>
  /// Feed the Storm: while raging, fast healing that scales with the number of
  /// bleeding enemies within 30 feet (1 enemy: 1, 3+: 2, 5+: 3). Any bleed counts
  /// - ours or another source's.
  /// </summary>
  [TypeId(Guids.BloodstormFeed)]
  internal class BloodstormFeed : UnitFactComponentDelegate, ITickEachRound
  {
    public BlueprintBuff[] Rage;

    public void OnNewRound()
    {
      try
      {
        if (Owner.HPLeft <= 0 || !Bloodstorm.IsRaging(Owner, Rage))
        {
          return;
        }
        if (Owner.Descriptor.Damage <= 0)
        {
          return;
        }
        var bleeding = Game.Instance.State.LoadedAreaState.MainState.AllEntityData
          .OfType<UnitEntityData>()
          .Count(u => u.HPLeft > 0 && u.IsEnemy(Owner)
            && Vector3.Distance(u.Position, Owner.Position) <= 9.2f
            && u.Buffs.Any(b => b?.Blueprint?.name?.Contains("Bleed") == true));
        var heal = bleeding >= 5 ? 3 : bleeding >= 3 ? 2 : bleeding >= 1 ? 1 : 0;
        if (heal > 0)
        {
          Owner.Descriptor.Damage = Math.Max(0, Owner.Descriptor.Damage - heal);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Bloodstorm: Feed the Storm failed.", e);
      }
    }
  }

  /// <summary>
  /// Bloodspout: while raging, a critical hit against a bleeding target sprays
  /// blood - every enemy within 10 feet of the target (except the target) gains a
  /// Bleeding Wound rank and is shaken for a round.
  /// </summary>
  [TypeId(Guids.BloodstormCritSpray)]
  internal class BloodstormCritSpray : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>
  {
    public BlueprintBuff Bleed;
    public BlueprintBuff[] Rage;
    public int MaxRanks;

    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt)
    {
      try
      {
        if (evt.AttackRoll is null || !evt.AttackRoll.IsHit || !evt.AttackRoll.IsCritical)
        {
          return;
        }
        var target = evt.Target;
        if (target is null || target.HPLeft <= 0)
        {
          return;
        }
        if (!Bloodstorm.IsRaging(Owner, Rage))
        {
          return;
        }
        if (target.Buffs.GetBuff(Bleed) is null)
        {
          return;
        }
        var victims = Game.Instance.State.LoadedAreaState.MainState.AllEntityData
          .OfType<UnitEntityData>()
          .Where(u => u != target && u.HPLeft > 0 && u.IsEnemy(Owner)
            && Vector3.Distance(u.Position, target.Position) <= 3.5f)
          .ToList();
        var shaken = BuffRefs.Shaken.Reference.Get();
        foreach (var victim in victims)
        {
          Bloodstorm.ApplyBleedRank(victim, Context, Bleed, MaxRanks);
          if (victim.Buffs.GetBuff(shaken) is null)
          {
            victim.AddBuff(
              shaken, Context,
              duration: ContextDuration.Fixed(1).Calculate(Context).Seconds);
          }
        }
        if (victims.Count > 0)
        {
          MissionFeats.Logger.Info(
            $"[bloodstorm] Bloodspout: sprayed {victims.Count} enemies near {target.CharacterName}.");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Bloodstorm: Bloodspout failed.", e);
      }
    }
  }

  /// <summary>
  /// Open the Floodgate: consumes every Bleeding Wound stack on enemies within 30
  /// feet (the bleeds are removed) and heals the barbarian 1d6 per rank consumed.
  /// Only usable while raging.
  /// </summary>
  [TypeId(Guids.BloodstormFloodgateAction)]
  internal class ContextActionBloodstormFloodgate : ContextAction
  {
    public BlueprintBuff Bleed;
    public BlueprintBuff[] Rage;

    public override string GetCaption() => "Open the Floodgate";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        if (caster is null || Bleed is null)
        {
          return;
        }
        if (!Bloodstorm.IsRaging(caster, Rage))
        {
          MissionFeats.Logger.Info("[floodgate] not raging - nothing happens.");
          return;
        }
        var victims = Game.Instance.State.LoadedAreaState.MainState.AllEntityData
          .OfType<UnitEntityData>()
          .Where(u => u.HPLeft > 0 && u.IsEnemy(caster)
            && Vector3.Distance(u.Position, caster.Position) <= 9.2f
            && u.Buffs.GetBuff(Bleed) != null)
          .ToList();
        int ranks = 0;
        foreach (var victim in victims)
        {
          var buff = victim.Buffs.GetBuff(Bleed);
          if (buff is null)
          {
            continue;
          }
          ranks += Math.Max(1, buff.Ranks);
          victim.Buffs.RemoveFact(Bleed);
        }
        if (ranks <= 0)
        {
          MissionFeats.Logger.Info("[floodgate] no bleeding enemies nearby.");
          return;
        }
        int healed = 0;
        for (int i = 0; i < ranks; i++)
        {
          healed += UnityEngine.Random.Range(1, 7);
        }
        caster.Descriptor.Damage = Math.Max(0, caster.Descriptor.Damage - healed);
        MissionFeats.Logger.Info(
          $"[floodgate] consumed {ranks} ranks from {victims.Count} enemies, healed {healed}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Bloodstorm: Floodgate failed.", e);
      }
    }
  }
}

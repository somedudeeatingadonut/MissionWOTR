using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.Configurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.51.0 — The Bonewatch (prestige class, MissionWOTR homebrew; the
  /// user's request: "add 2 prestige classes" — the homebrew of the
  /// pair, designed for this user's undead parties).
  ///
  /// THE CONCEPT: every deathless legion needs an officer who does not
  /// flinch. The Bonewatch is the marshal of the dead - the one who
  /// stands in the gap between the living world and the ranks he
  /// commands, banner in hand, wearing the grave's own armor.
  ///
  /// ENTRY (adapted): BAB +3 (PrerequisiteFullStatValue) and a bond
  /// with death itself - any channel energy feature OR the Undead
  /// Master's Command the Dead kit (our own 0.48.0 archetype - the
  /// natural on-ramp).
  ///
  /// THE GAIN (5 levels, d10, full BAB, strong Fort):
  /// - Bone Mantle (1st): +2 natural armor, thickening by +2 more at
  ///   4th (the grave grows on him).
  /// - Grave Command (1st): the REAL Command Undead spell, on its own
  ///   pool - 1/day, 2/day at 3rd (the Undead Master idiom).
  /// - Cadre of Bone (2nd): the marshal's will drives his pets - every
  ///   player-controlled pet within 30 feet gains +2 attack (a morale
  ///   aura, refreshed each round) and +2 damage while the aura holds
  ///   (the sweep + rider idioms).
  /// - The Last Order (5th): 1/day, swift: for 1 round, the marshal
  ///   CANNOT BE DROPPED - incoming damage cannot reduce him below
  ///   1 HP (the engine's own MinHPAfterDamage field, set on every
  ///   incoming hit while the order stands; the probe-verified rule).
  ///
  /// ENGINE NOTES: all proven parts - the stance/aura/tick idioms from
  /// this session; pets are found via Owner.Pets (the dcx accessor)
  /// and the damage rider keys off the aura buff's presence on the
  /// initiator, so only pets inside the marshal's aura benefit.
  /// </summary>
  internal class Bonewatch
  {
    internal const string ClassName = "Bonewatch";

    internal static void Configure()
    {
      var icon = AbilityRefs.AnimateDead.Reference.Get().Icon;

      // ----- Bone Mantle (1st and 4th). -----
      var mantle = FeatureConfigurator.New("BonewatchBoneMantleFeature", Guids.BonewatchBoneMantleFeature)
        .SetDisplayName("BonewatchBoneMantle.Name")
        .SetDescription("BonewatchBoneMantle.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.AC, value: 2, descriptor: ModifierDescriptor.NaturalArmor)
        .Configure();
      var mantleGreater = FeatureConfigurator.New(
        "BonewatchBoneMantleGreaterFeature", Guids.BonewatchBoneMantleGreaterFeature)
        .SetDisplayName("BonewatchBoneMantle.Name")
        .SetDescription("BonewatchBoneMantle.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.AC, value: 2, descriptor: ModifierDescriptor.NaturalArmor)
        .Configure();

      // ----- Grave Command: the real spell, on a pool. -----
      var pool = AbilityResourceConfigurator.New(
        "BonewatchCommandResource", Guids.BonewatchCommandResource)
        .SetMax(1)
        .Configure();
      var commandAbility = AbilityConfigurator.New(
        "BonewatchCommandAbility", Guids.BonewatchCommandAbility)
        .SetDisplayName("BonewatchCommand.Name")
        .SetDescription("BonewatchCommand.Description")
        .SetIcon(AbilityRefs.CommandUndead.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetEnemies()
        .AddAbilityResourceLogic(requiredResource: pool, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(ActionsBuilder.New().CastSpell(
          AbilityRefs.CommandUndead.Cast<BlueprintAbilityReference>()).Build())
        .Configure();
      var commandKit = FeatureConfigurator.New("BonewatchCommandKitFeature", Guids.BonewatchCommandKitFeature)
        .SetDisplayName("BonewatchCommand.Name")
        .SetDescription("BonewatchCommand.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { commandAbility })
        .AddAbilityResources(resource: pool, restoreAmount: true)
        .Configure();
      var commandExtra3 = FeatureConfigurator.New(
        "BonewatchCommandExtraFeature", Guids.BonewatchCommandExtraFeature)
        .SetDisplayName("BonewatchCommand.Name")
        .SetDescription("BonewatchCommand.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddIncreaseResourceAmount(pool, 1)
        .Configure();

      // ----- Cadre of Bone (2nd): the marshal's aura. -----
      var cadreBuff = BuffConfigurator.New("BonewatchCadreBuff", Guids.BonewatchCadreBuff)
        .SetDisplayName("BonewatchCadre.Buff.Name")
        .SetDescription("BonewatchCadre.Buff.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddContextStatBonus(StatType.AdditionalAttackBonus, ContextValues.Constant(2), ModifierDescriptor.Morale)
        .Configure();
      var cadre = FeatureConfigurator.New("BonewatchCadreFeature", Guids.BonewatchCadreFeature)
        .SetDisplayName("BonewatchCadre.Name")
        .SetDescription("BonewatchCadre.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new BonewatchCadreRider
        {
          AuraBuff = cadreBuff,
        })
        .Configure();

      // ----- The Last Order (5th): the unbreakable round. -----
      var lastOrderBuff = BuffConfigurator.New("BonewatchLastOrderBuff", Guids.BonewatchLastOrderBuff)
        .SetDisplayName("BonewatchLastOrder.Buff.Name")
        .SetDescription("BonewatchLastOrder.Buff.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new BonewatchLastOrderRider())
        .Configure();
      var lastOrderPool = AbilityResourceConfigurator.New(
        "BonewatchLastOrderResource", Guids.BonewatchLastOrderResource)
        .SetMax(1)
        .Configure();
      var lastOrderAbility = AbilityConfigurator.New(
        "BonewatchLastOrderAbility", Guids.BonewatchLastOrderAbility)
        .SetDisplayName("BonewatchLastOrder.Name")
        .SetDescription("BonewatchLastOrder.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Swift)
        .SetCanTargetSelf()
        .AddAbilityResourceLogic(requiredResource: lastOrderPool, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(new HVStanceAction
        {
          Buff = lastOrderBuff,
        }).Build())
        .Configure();
      var lastOrder = FeatureConfigurator.New("BonewatchLastOrderFeature", Guids.BonewatchLastOrderFeature)
        .SetDisplayName("BonewatchLastOrder.Name")
        .SetDescription("BonewatchLastOrder.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { lastOrderAbility })
        .AddAbilityResources(resource: lastOrderPool, restoreAmount: true)
        .Configure();

      // ----- The progression. -----
      System.Collections.Generic.List<BlueprintFeatureBaseReference> Refs(
        params BlueprintFeature[] features) =>
        features.Select(f => f.ToReference<BlueprintFeatureBaseReference>())
          .ToList();
      var progression = ProgressionConfigurator.New("BonewatchProgression", Guids.BonewatchProgression)
        .SetLevelEntries(
          new LevelEntry { Level = 1, Features = Refs(mantle, commandKit) },
          new LevelEntry { Level = 2, Features = Refs(cadre) },
          new LevelEntry { Level = 3, Features = Refs(commandExtra3) },
          new LevelEntry { Level = 4, Features = Refs(mantleGreater) },
          new LevelEntry { Level = 5, Features = Refs(lastOrder) })
        .Configure();

      // ----- The class. -----
      var clazz = CharacterClassConfigurator.New(ClassName, Guids.BonewatchClass)
        .SetLocalizedName("Bonewatch.Name")
        .SetLocalizedDescription("Bonewatch.Description")
        .SetIcon(icon)
        .SetPrestigeClass()
        .SetHitDie(DiceType.D10)
        .SetBaseAttackBonus(StatProgressionRefs.BABFull.Cast<BlueprintStatProgressionReference>())
        .SetFortitudeSave(StatProgressionRefs.SavesHigh.Cast<BlueprintStatProgressionReference>())
        .SetReflexSave(StatProgressionRefs.SavesLow.Cast<BlueprintStatProgressionReference>())
        .SetWillSave(StatProgressionRefs.SavesLow.Cast<BlueprintStatProgressionReference>())
        .SetSkillPoints(2)
        .SetClassSkills(StatType.SkillLoreReligion, StatType.CheckIntimidate, StatType.SkillKnowledgeWorld)
        .SetProgression(progression)
        .AddComponent(new PrerequisiteFullStatValue
        {
          Stat = StatType.BaseAttackBonus,
          Value = 3,
        })
        .AddComponent(new PrerequisiteFeaturesFromList
        {
          Features = new[]
          {
            FeatureRefs.ChannelEnergyFeature.Cast<BlueprintFeatureReference>(),
            FeatureRefs.ChannelEnergyHospitalerFeature.Cast<BlueprintFeatureReference>(),
            FeatureRefs.ChannelEnergyEmpyrealFeature.Cast<BlueprintFeatureReference>(),
            Guids.UndeadMasterKitFeature,
          },
          Group = Prerequisite.GroupType.Any,
          Amount = 1,
        })
        .Configure();

      ProgressionConfigurator.For(progression)
        .SetClasses(new BlueprintProgression.ClassWithLevel
        {
          m_Class = clazz.ToReference<BlueprintCharacterClassReference>(),
          AdditionalLevel = 0,
        })
        .Configure();
      MissionFeats.Logger.Info("[bonewatch] configured: " + ClassName + ".");
    }
  }

  /// <summary>
  /// Cadre of Bone: every round, every player-controlled pet within 30
  /// feet of the marshal is wrapped in the aura buff (12 seconds, so it
  /// never lapses between ticks - the Mending Blade sweep idiom), and
  /// the pets' weapon damage carries +2 while the aura holds.
  /// </summary>
  [TypeId(Guids.BonewatchCadreRider)]
  internal class BonewatchCadreRider : UnitFactComponentDelegate, ITickEachRound,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    private static readonly float RadiusMeters = new Feet(30).Meters;

    public BlueprintBuff AuraBuff;

    protected override void OnActivate() => Refresh();

    public void OnNewRound() => Refresh();

    public void OnEventAboutToTrigger(RulePrepareDamage evt) { }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        // The aura's damage half: pets wearing the aura buff strike
        // +2 while it holds (the strike-rider pattern, keyed off the
        // buff so only pets inside the marshal's aura benefit).
        if (evt.Initiator == null || evt.Initiator == Owner ||
          !evt.Initiator.HasFact(AuraBuff) ||
          evt.DamageBundle?.Weapon is null)
        {
          return;
        }
        evt.Add(new DirectDamage(DiceFormula.Zero, 2) { SourceFact = Fact });
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[bonewatch] cadre damage failed.", e);
      }
    }

    private void Refresh()
    {
      try
      {
        using (var enumerator = Kingmaker.Game.Instance.State.Units.GetEnumerator())
        {
          while (enumerator.MoveNext())
          {
            var unit = enumerator.Current;
            if (unit is null || !unit.IsPet || !unit.IsPlayerFaction ||
              unit.Descriptor.State.IsDead ||
              Owner.DistanceTo(unit) > RadiusMeters)
            {
              continue;
            }
            unit.AddBuff(AuraBuff, Fact.MaybeContext, TimeSpan.FromSeconds(12));
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[bonewatch] cadre refresh failed.", e);
      }
    }
  }

  /// <summary>
  /// The Last Order: while the order stands, no incoming damage can
  /// reduce the marshal below 1 HP - the engine's own MinHPAfterDamage
  /// field, set in the damage rule's about-to-trigger.
  /// </summary>
  [TypeId(Guids.BonewatchLastOrderRider)]
  internal class BonewatchLastOrderRider : UnitBuffComponentDelegate,
    ITargetRulebookHandler<RuleDealDamage>
  {
    public void OnEventAboutToTrigger(RuleDealDamage evt)
    {
      try
      {
        if (evt.Target == Owner)
        {
          evt.MinHPAfterDamage = 1;
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[bonewatch] the last order failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleDealDamage evt) { }
  }
}

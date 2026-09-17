using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.Configurators;
using BlueprintCore.Blueprints.Configurators.AI;
using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Construct Crafter abilities, AI brains, and role-variant units (brains v1).
  ///
  /// Brains v1: Wrath brains are ordered AiAction lists (utility AI). Two custom brains:
  ///   - CrafterCasterBrain: cast any granted construct ability (fire blast, ice ray,
  ///     mend, bolt spit), then fall back to weapon attacks
  ///   - CrafterRangedBrain: bolt spit first, then weapon attacks
  /// Construct abilities are granted as facts at deploy; the brain can only cast what the
  /// construct actually has. No custom considerations yet (defaults used) - behavior
  /// tuning is the next iteration, per playtest feedback.
  ///
  /// Role variants: the deploy action picks a variant unit by active core:
  ///   HumanoidArcher (Arbalest - bow in inventory, attacks with equipped weapon),
  ///   HumanoidCaster (Flaming/Soft), GolemCaster (Cold/Arbalest/Soft),
  ///   HoundRanged (Arbalest/Soft). Base identity is tracked with marker buffs so any
  ///   variant replaces any earlier construct of the same base.
  /// </summary>
  internal static class ConstructCrafterAbilities
  {
    internal static BlueprintAbility FireBlast;
    internal static BlueprintAbility IceRay;
    internal static BlueprintAbility Mend;
    internal static BlueprintAbility BoltSpit;

    internal static BlueprintBuff MendBuff;
    internal static BlueprintBuff HoundBaseMarker;
    internal static BlueprintBuff ManBaseMarker;
    internal static BlueprintBuff GolemBaseMarker;

    internal static BlueprintUnit HumanoidArcherUnit;
    internal static BlueprintUnit HumanoidCasterUnit;
    internal static BlueprintUnit GolemCasterUnit;
    internal static BlueprintUnit HoundRangedUnit;

    internal static void Configure()
    {
      ConfigureMarkers();
      ConfigureAbilities();
      ConfigureBrains();
      ConfigureVariantUnits();
    }

    private static void ConfigureMarkers()
    {
      var icon = FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon;
      HoundBaseMarker = BuffConfigurator.New("ConstructCrafterHoundBaseMarker", Guids.HoundBaseMarker)
        .SetDisplayName("HoundBaseMarker.Name")
        .SetDescription("HoundBaseMarker.Description")
        .SetIcon(icon)
        .Configure();
      ManBaseMarker = BuffConfigurator.New("ConstructCrafterManBaseMarker", Guids.ManBaseMarker)
        .SetDisplayName("ManBaseMarker.Name")
        .SetDescription("ManBaseMarker.Description")
        .SetIcon(icon)
        .Configure();
      GolemBaseMarker = BuffConfigurator.New("ConstructCrafterGolemBaseMarker", Guids.GolemBaseMarker)
        .SetDisplayName("GolemBaseMarker.Name")
        .SetDescription("GolemBaseMarker.Description")
        .SetIcon(icon)
        .Configure();
    }

    private static void ConfigureAbilities()
    {
      var icon = FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon;

      // Fire Blast (Flaming humanoid caster): 15-ft burst, 6d6 fire, Reflex halves.
      FireBlast = AbilityConfigurator.New("ConstructCrafterFireBlast", Guids.FireBlastAbility)
        .SetDisplayName("FireBlast.Name")
        .SetDescription("FireBlast.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetEnemies()
        .AddAbilityAoERadius(
          diameterInCells: 3,
          targetType: Kingmaker.UnitLogic.Abilities.Components.TargetType.Enemy)
        .AddAbilityEffectRunAction(ActionsBuilder.New().DealDamage(
          DamageTypes.Energy(DamageEnergyType.Fire),
          new ContextDiceValue
          {
            DiceType = DiceType.D6,
            DiceCountValue = ContextValues.Constant(6),
            BonusValue = ContextValues.Constant(0),
          },
          halfIfSaved: true))
        .Configure();

      // Ice Ray (Cold golem caster): single target, 4d6 cold, Reflex halves, slowed.
      IceRay = AbilityConfigurator.New("ConstructCrafterIceRay", Guids.IceRayAbility)
        .SetDisplayName("IceRay.Name")
        .SetDescription("IceRay.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetEnemies()
        .AddAbilityEffectRunAction(
          ActionsBuilder.New()
            .DealDamage(
              DamageTypes.Energy(DamageEnergyType.Cold),
              new ContextDiceValue
              {
                DiceType = DiceType.D6,
                DiceCountValue = ContextValues.Constant(4),
                BonusValue = ContextValues.Constant(0),
              },
              halfIfSaved: true)
            .ApplyBuff(BuffRefs.Slowed.Reference.Get(), ContextDuration.Fixed(3)))
        .Configure();

      // Mend (Soft caster): field patch-up granting temporary hit points (adaptation:
      // context-heal actions are evaluator-based; temp HP is the deterministic stand-in).
      MendBuff = BuffConfigurator.New("ConstructCrafterMendBuff", Guids.MendBuff)
        .SetDisplayName("MendBuff.Name")
        .SetDescription("MendBuff.Description")
        .SetIcon(icon)
        .AddComponent<Kingmaker.Designers.Mechanics.Buffs.TemporaryHitPointsFromAbilityValue>(c =>
        {
          c.Value = ContextValues.Rank();
          c.RemoveWhenHitPointsEnd = true;
        })
        .AddContextRankConfig(
          ContextRankConfigs.ClassLevel(new[] { CharacterClassRefs.AlchemistClass.ToString() }, min: 5))
        .Configure();

      Mend = AbilityConfigurator.New("ConstructCrafterMend", Guids.MendAbility)
        .SetDisplayName("Mend.Name")
        .SetDescription("Mend.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetFriends()
        .SetCanTargetSelf()
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().ApplyBuff(MendBuff, ContextDuration.Fixed(10)))
        .Configure();

      // Bolt Spit (Arbalest ranged): single target, 2d6 piercing, Reflex halves.
      BoltSpit = AbilityConfigurator.New("ConstructCrafterBoltSpit", Guids.BoltSpitAbility)
        .SetDisplayName("BoltSpit.Name")
        .SetDescription("BoltSpit.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetEnemies()
        .AddAbilityEffectRunAction(ActionsBuilder.New().DealDamage(
          DamageTypes.Physical(),
          new ContextDiceValue
          {
            DiceType = DiceType.D6,
            DiceCountValue = ContextValues.Constant(2),
            BonusValue = ContextValues.Constant(0),
          },
          halfIfSaved: true))
        .Configure();
    }

    private static void ConfigureBrains()
    {
      // Cast actions: one per construct ability; the AI can only use what the unit has.
      var castFireBlast = AiCastSpellConfigurator.New("ConstructCrafterAiFireBlast", Guids.AiCastFireBlast)
        .SetAbility(FireBlast)
        .Configure();
      var castIceRay = AiCastSpellConfigurator.New("ConstructCrafterAiIceRay", Guids.AiCastIceRay)
        .SetAbility(IceRay)
        .Configure();
      var castMend = AiCastSpellConfigurator.New("ConstructCrafterAiMend", Guids.AiCastMend)
        .SetAbility(Mend)
        .Configure();
      var castBoltSpit = AiCastSpellConfigurator.New("ConstructCrafterAiBoltSpit", Guids.AiCastBoltSpit)
        .SetAbility(BoltSpit)
        .Configure();
      var attack = AiAttackConfigurator.New("ConstructCrafterAiAttack", Guids.AiAttack)
        .Configure();

      BrainConfigurator.New("ConstructCrafterCasterBrain", Guids.CrafterCasterBrain)
        .SetActions(castFireBlast, castIceRay, castMend, castBoltSpit, attack)
        .Configure();

      BrainConfigurator.New("ConstructCrafterRangedBrain", Guids.CrafterRangedBrain)
        .SetActions(castBoltSpit, attack)
        .Configure();
    }

    private static void ConfigureVariantUnits()
    {
      var dogFaction = UnitRefs.AnimalCompanionUnitDog.Reference.Get().Faction;

      // Archer humanoid: bow in inventory; the stock brain attacks with the equipped weapon.
      HumanoidArcherUnit = UnitConfigurator.New(
          "ConstructCrafterHumanoidArcher", Guids.ConstructCrafterHumanoidArcherUnit)
        .CopyFrom(UnitRefs.CR0_5_Bandit_Human_FighterMelee_Male)
        .SetFaction(dogFaction)
        .SetStartingInventory(ItemWeaponRefs.CompositeLongbow)
        .Configure();

      // Caster humanoid / caster golem / ranged hound: custom brains.
      HumanoidCasterUnit = UnitConfigurator.New(
          "ConstructCrafterHumanoidCaster", Guids.ConstructCrafterHumanoidCasterUnit)
        .CopyFrom(UnitRefs.CR0_5_Bandit_Human_FighterMelee_Male)
        .SetFaction(dogFaction)
        .Configure();
      SetBrain(Guids.ConstructCrafterHumanoidCasterUnit, "ConstructCrafterCasterBrain");

      GolemCasterUnit = UnitConfigurator.New(
          "ConstructCrafterGolemCaster", Guids.ConstructCrafterGolemCasterUnit)
        .CopyFrom(
          UnitRefs.CR11_GolemStone,
          c => !c.name.Contains("Slow")
            && c is not Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical)
        .SetStrength(32 - 2)
        .SetMaxHP(107 - 20)
        .SetFaction(dogFaction)
        .Configure();
      SetBrain(Guids.ConstructCrafterGolemCasterUnit, "ConstructCrafterCasterBrain");

      HoundRangedUnit = UnitConfigurator.New(
          "ConstructCrafterHoundRanged", Guids.ConstructCrafterHoundRangedUnit)
        .CopyFrom(UnitRefs.AnimalCompanionUnitDog)
        .SetStrength(UnitRefs.AnimalCompanionUnitDog.Reference.Get().Strength - 2)
        .SetDexterity(UnitRefs.AnimalCompanionUnitDog.Reference.Get().Dexterity - 2)
        .SetMaxHP(Math.Max(4, UnitRefs.AnimalCompanionUnitDog.Reference.Get().MaxHP - 4))
        .SetFaction(dogFaction)
        .Configure();
      SetBrain(Guids.ConstructCrafterHoundRangedUnit, "ConstructCrafterRangedBrain");
    }

    private static void SetBrain(string unitGuid, string brainName)
    {
      var unit = BlueprintTool.Get<BlueprintUnit>(unitGuid);
      unit.m_Brain = BlueprintTool.GetRef<BlueprintBrainReference>(brainName);
    }
  }

  /// <summary>
  /// Booming core payload: every weapon hit detonates - enemies near the target take
  /// 2d6 sonic damage.
  /// </summary>
  [Kingmaker.Blueprints.JsonSystem.TypeId(Guids.ConstructSonicBoom)]
  internal class ConstructSonicBoom : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>
  {
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
        if (target is null)
        {
          return;
        }
        // Everyone hostile to the construct within ~10 feet of the struck target.
        var victims = Game.Instance.State.LoadedAreaState.MainState.AllEntityData
          .OfType<UnitEntityData>()
          .Where(u => u != target && u.HPLeft > 0 && u.IsEnemy(Owner)
            && Vector3.Distance(u.Position, target.Position) <= 3.5f)
          .ToList();
        foreach (var victim in victims)
        {
          var bundle = new DamageBundle();
          bundle.Add(new DirectDamage(new DiceFormula(2, DiceType.D6), 0));
          Rulebook.Trigger(new RuleDealDamage(Owner, victim, bundle) { Reason = Fact });
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ConstructCrafter: sonic boom failed.", e);
      }
    }
  }
}

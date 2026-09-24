using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.Pathfinding;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using Kingmaker.View;
using MissionWOTR.Feats;
using System;
using System.Collections;
using System.Collections.Generic;
using TurnBased.Controllers;
using UnityEngine;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// The Carousel (homebrew cavalier archetype, user-commissioned - the
  /// charging half of the cavalier pair, next to the Sister-in-Arms port).
  ///
  /// The fantasy: a rider who never stops moving. The charge is not an
  /// opener - it is the whole fighting style. Strike one enemy, wheel, strike
  /// the next; the battlefield is a ring and the Carousel never leaves it.
  ///
  /// - Carousel Training (1st): the Carousel Charge ability - a
  ///   standard-action charge (the pplus Stag Charge engine: real movement
  ///   along the navmesh at double speed, the vanilla charge buff, a true
  ///   charge attack). Wind-Rider: while charging she does not provoke
  ///   attacks of opportunity (a rider at full gallop is a hard target).
  /// - Wheel About (3rd, replaces cavalier's charge): after any charge this
  ///   round, she may immediately make a SECOND charge against a different
  ///   enemy as a free action (the priming marker rides the vanilla charge
  ///   buff - any charge source primes the wheel).
  /// - Surefooted Steed (4th): the mount ignores difficult terrain -
  ///   magical mud, grease and worse do not slow the gallop - and gains +2
  ///   on all saving throws. Her charges ignore difficult terrain too.
  /// - Slip the Line (5th, replaces banner): allies never block her charge
  ///   line; she threads the gaps in her own formation at full speed.
  /// - Grand Carousel (11th, replaces mighty charge): the wheel no longer
  ///   needs priming - once per round, any round, she may wheel; and her
  ///   charges gain +2 on attack rolls.
  /// - Eternal Carousel (20th, replaces supreme charge): the wheel knows no
  ///   limit - every round is one long charge.
  ///
  /// Implementation notes: the charge is an AbilityCustomLogic coroutine
  /// adapted from pplus's StagCharge (ForcedPath movement, double speed,
  /// charge state, a real UnitAttack with IsCharge, turn-based and real-time
  /// routines, mounted via GetSaddledUnit/GetRider command sync). AoO
  /// immunity and wheel priming ride the vanilla ChargeBuff via
  /// AddBuffExtraEffects (the pplus ShiningKnight pattern). The wheel is a
  /// free-action copy of the charge logic whose targeting gate reads the
  /// archetype features (priming/spent markers). Log prefix: [carousel].
  /// </summary>
  internal static class Carousel
  {
    internal const string ArchetypeName = "CarouselArchetype";
    internal const string DisplayName = "Carousel.Name";
    internal const string Description = "Carousel.Description";

    internal const string TrainingName = "CarouselTraining";
    internal const string ChargeAbilityName = "CarouselChargeAbility";
    internal const string WheelName = "CarouselWheelAbout";
    internal const string WheelAbilityName = "CarouselWheelAbility";
    internal const string SurefootName = "CarouselSurefootedSteed";
    internal const string SlipName = "CarouselSlipTheLine";
    internal const string GrandName = "CarouselGrand";
    internal const string EternalName = "CarouselEternal";

    public static void Configure()
    {
      var cavalier = CharacterClassRefs.CavalierClass.Reference.Get();
      var chargeIcon = FeatureRefs.CavalierCharge.Reference.Get().Icon;

      // ----- Wind-Rider: no attacks of opportunity while charging -----
      var windRiderBuff = BuffConfigurator.New("CarouselWindRiderBuff", Guids.CarouselWindRiderBuff)
        .SetDisplayName("CarouselWindRider.Name")
        .SetDescription("CarouselWindRider.Description")
        .SetIcon(chargeIcon)
        .AddCondition(UnitCondition.ImmuneToAttackOfOpportunity)
        .Configure();

      // ----- Wheel markers -----
      var wheelReadyBuff = BuffConfigurator.New("CarouselWheelReadyBuff", Guids.CarouselWheelReadyBuff)
        .SetDisplayName("CarouselWheelReady.Name")
        .SetDescription("CarouselWheelReady.Description")
        .SetIcon(chargeIcon)
        .Configure();

      var wheelSpentBuff = BuffConfigurator.New("CarouselWheelSpentBuff", Guids.CarouselWheelSpentBuff)
        .SetDisplayName("CarouselWheelSpent.Name")
        .SetDescription("CarouselWheelSpent.Description")
        .SetIcon(chargeIcon)
        .Configure();

      // ----- Grand Carousel's +2 attack while charging -----
      var grandBuff = BuffConfigurator.New("CarouselGrandBuff", Guids.CarouselGrandBuff)
        .SetDisplayName("CarouselGrandBuff.Name")
        .SetDescription("CarouselGrandBuff.Description")
        .SetIcon(chargeIcon)
        .AddContextStatBonus(
          StatType.AdditionalAttackBonus, ContextValues.Constant(2), ModifierDescriptor.Morale)
        .Configure();

      // ----- The charge abilities (pplus AerialAssault wiring recipe) -----
      var chargeLogic = new CarouselChargeLogic();
      var chargeAbility = AbilityConfigurator.New(ChargeAbilityName, Guids.CarouselChargeAbility)
        .CopyFrom(
          AbilityRefs.ChargeAbility,
          typeof(HideDCFromTooltip),
          typeof(AbilityCasterHasNoFacts),
          typeof(AbilityIsFullRoundInTurnBased),
          typeof(AbilityRequirementCanMove))
        .AddComponent(chargeLogic)
        .AddComponent(new CarouselChargeConditions())
        .SetDisplayName("CarouselCharge.Name")
        .SetDescription("CarouselCharge.Description")
        .SetIcon(chargeIcon)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetEnemies(true)
        .SetCanTargetSelf(false)
        .SetAnimation(
          Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell
            .CastAnimationStyle.Immediate)
        .SetRange(AbilityRange.DoubleMove)
        .SetType(AbilityType.Physical)
        .Configure();

      var wheelLogic = new CarouselChargeLogic
      {
        IsWheel = true,
        WheelReadyBuff = wheelReadyBuff,
        WheelSpentBuff = wheelSpentBuff,
      };
      var wheelAbility = AbilityConfigurator.New(WheelAbilityName, Guids.CarouselWheelAbility)
        .CopyFrom(
          AbilityRefs.ChargeAbility,
          typeof(HideDCFromTooltip),
          typeof(AbilityCasterHasNoFacts),
          typeof(AbilityIsFullRoundInTurnBased),
          typeof(AbilityRequirementCanMove))
        .AddComponent(wheelLogic)
        .AddComponent(new CarouselChargeConditions())
        .SetDisplayName("CarouselWheelAbility.Name")
        .SetDescription("CarouselWheelAbility.Description")
        .SetIcon(chargeIcon)
        .SetActionType(UnitCommand.CommandType.Free)
        .SetCanTargetEnemies(true)
        .SetCanTargetSelf(false)
        .SetAnimation(
          Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell
            .CastAnimationStyle.Immediate)
        .SetRange(AbilityRange.DoubleMove)
        .SetType(AbilityType.Physical)
        .Configure();

      // ----- Features -----
      var training = FeatureConfigurator.New(TrainingName, Guids.CarouselTraining)
        .SetDisplayName("CarouselTraining.Name")
        .SetDescription("CarouselTraining.Description")
        .SetIcon(chargeIcon)
        .SetIsClassFeature()
        .AddFacts(new() { chargeAbility })
        .AddBuffExtraEffects(
          checkedBuff: BuffRefs.ChargeBuff.Reference.Get(), extraEffectBuff: windRiderBuff)
        .Configure();

      var wheel = FeatureConfigurator.New(WheelName, Guids.CarouselWheel)
        .SetDisplayName("CarouselWheel.Name")
        .SetDescription("CarouselWheel.Description")
        .SetIcon(chargeIcon)
        .SetIsClassFeature()
        .AddFacts(new() { wheelAbility })
        .AddBuffExtraEffects(
          checkedBuff: BuffRefs.ChargeBuff.Reference.Get(), extraEffectBuff: wheelReadyBuff)
        .Configure();

      // Surefooted Steed: the mount feature itself (granted to the pet).
      var mountFeat = FeatureConfigurator.New("CarouselSurefootMount", Guids.CarouselSurefootMount)
        .SetDisplayName("CarouselSurefootMount.Name")
        .SetDescription("CarouselSurefootMount.Description")
        .SetIcon(FeatureSelectionRefs.CavalierMountSelection.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddConditionImmunity(UnitCondition.DifficultTerrain)
        .AddStatBonus(stat: StatType.SaveFortitude, value: 2, descriptor: ModifierDescriptor.Competence)
        .AddStatBonus(stat: StatType.SaveReflex, value: 2, descriptor: ModifierDescriptor.Competence)
        .AddStatBonus(stat: StatType.SaveWill, value: 2, descriptor: ModifierDescriptor.Competence)
        .Configure();

      var surefoot = FeatureConfigurator.New(SurefootName, Guids.CarouselSurefoot)
        .SetDisplayName("CarouselSurefoot.Name")
        .SetDescription("CarouselSurefoot.Description")
        .SetIcon(FeatureSelectionRefs.CavalierMountSelection.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFeatureToPet(mountFeat, PetType.AnimalCompanion)
        .Configure();

      var slip = FeatureConfigurator.New(SlipName, Guids.CarouselSlipLine)
        .SetDisplayName("CarouselSlipLine.Name")
        .SetDescription("CarouselSlipLine.Description")
        .SetIcon(chargeIcon)
        .SetIsClassFeature()
        .Configure();

      var grand = FeatureConfigurator.New(GrandName, Guids.CarouselGrand)
        .SetDisplayName("CarouselGrand.Name")
        .SetDescription("CarouselGrand.Description")
        .SetIcon(chargeIcon)
        .SetIsClassFeature()
        .AddBuffExtraEffects(
          checkedBuff: BuffRefs.ChargeBuff.Reference.Get(), extraEffectBuff: grandBuff)
        .Configure();

      var eternal = FeatureConfigurator.New(EternalName, Guids.CarouselEternal)
        .SetDisplayName("CarouselEternal.Name")
        .SetDescription("CarouselEternal.Description")
        .SetIcon(chargeIcon)
        .SetIsClassFeature()
        .Configure();

      // The logic components need the feature facts for their gates.
      CarouselChargeLogic.SlipFeature = slip;
      CarouselChargeLogic.SurefootFeature = surefoot;
      CarouselChargeLogic.GrandFeature = grand;
      CarouselChargeLogic.EternalFeature = eternal;

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.CarouselArchetype, CharacterClassRefs.CavalierClass)
          .SetLocalizedName(DisplayName)
          .SetLocalizedDescription(Description);

      // Trades: cavalier's charge (Wheel About - the charge line is upgraded,
      // not lost), banner (Slip the Line), mighty charge (Grand Carousel) and
      // supreme charge (Eternal Carousel). The mount, challenge, order and
      // tactician all stay - this is still a cavalier.
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, cavalier,
        FeatureRefs.CavalierCharge.ToString(),
        FeatureRefs.CavalierBanner.ToString(),
        FeatureRefs.CavalierMightyCharge.ToString(),
        FeatureRefs.CavalierSupremeCharge.ToString());

      archetype
        .AddToAddFeatures(LevelPlan.L(1), TrainingName)
        .AddToAddFeatures(LevelPlan.L(3), WheelName)
        .AddToAddFeatures(LevelPlan.L(4), SurefootName)
        .AddToAddFeatures(LevelPlan.L(5), SlipName)
        .AddToAddFeatures(LevelPlan.L(11), GrandName)
        .AddToAddFeatures(LevelPlan.L(20), EternalName)
        .Configure();

      MissionFeats.Logger.Info("Carousel: configured.");
    }
  }

  /// <summary>
  /// Charge gating (the pplus ChargeConditions pattern, adapted): fatigued
  /// and entangled riders cannot charge; difficult terrain stops the charge
  /// UNLESS the rider has Surefooted Steed (her mount eats the mud).
  /// </summary>
  internal class CarouselChargeConditions : BlueprintComponent, IAbilityRestriction
  {
    string IAbilityRestriction.GetAbilityRestrictionUIText()
    {
      return "Fatigued, Entangled or Difficult Terrain";
    }

    bool IAbilityRestriction.IsAbilityRestrictionPassed(AbilityData ability)
    {
      var caster = ability.Caster;
      if (caster is null)
      {
        return false;
      }
      if (caster.State.HasCondition(UnitCondition.Fatigued) ||
        caster.State.HasCondition(UnitCondition.Entangled))
      {
        return false;
      }
      var mount = caster.GetSaddledUnit();
      var terrain = caster.State.HasCondition(UnitCondition.DifficultTerrain) ||
        (mount != null && mount.State.HasCondition(UnitCondition.DifficultTerrain));
      if (terrain && CarouselChargeLogic.SurefootFeature is not null &&
        !caster.HasFact(CarouselChargeLogic.SurefootFeature))
      {
        return false;
      }
      return true;
    }
  }

  /// <summary>
  /// The Carousel charge: adapted from pplus's StagCharge (an
  /// AbilityCustomLogic coroutine). Real movement along the navmesh at
  /// double speed, the vanilla charge buff, a genuine charge attack
  /// (UnitAttack with IsCharge), turn-based and real-time routines, mounted
  /// or on foot. As the Wheel, it is a free action gated by the priming and
  /// spent markers.
  /// </summary>
  [TypeId(Guids.CarouselChargeLogic)]
  internal class CarouselChargeLogic : AbilityCustomLogic, IAbilityTargetRestriction,
    IAbilityMinRangeProvider
  {
    /// <summary>Feature facts shared by every instance (set at configure).</summary>
    internal static BlueprintFeature SlipFeature;
    internal static BlueprintFeature SurefootFeature;
    internal static BlueprintFeature GrandFeature;
    internal static BlueprintFeature EternalFeature;

    public bool IsWheel;
    public BlueprintBuff WheelReadyBuff;
    public BlueprintBuff WheelSpentBuff;

    public override bool IsEngageUnit => true;

    public override IEnumerator<AbilityDeliveryTarget> Deliver(
      AbilityExecutionContext context, TargetWrapper targetWrapper)
    {
      UnitEntityData target = targetWrapper.Unit;
      if (target == null)
      {
        MissionFeats.Logger.Warn("[carousel] charge target is missing.");
        yield break;
      }
      UnitEntityData caster = context.Caster;
      if (caster.GetThreatHandMelee(true) == null)
      {
        MissionFeats.Logger.Warn("[carousel] invalid caster weapon.");
        yield break;
      }
      if (IsWheel)
      {
        ConsumeWheelGates(caster, context);
      }
      Vector3 position = caster.Position;
      Vector3 endPoint = target.Position;
      caster.View.StopMoving();
      caster.View.AgentASP.IsCharging = true;
      caster.View.AgentASP.ForcePath(new ForcedPath(new List<Vector3>
      {
        position,
        endPoint,
      }), true);
      caster.Descriptor.AddBuff(
        BlueprintRoot.Instance.SystemMechanics.ChargeBuff, context, 1.Rounds().Seconds);
      caster.Descriptor.State.IsCharging = true;
      UnitAttack attack = new UnitAttack(target, null);
      attack.Init(caster);
      IEnumerator turnBasedRoutine = null;
      IEnumerator runtimeRoutine = null;
      for (; ; )
      {
        IEnumerator enumerator;
        if (CombatController.IsInTurnBasedCombat())
        {
          enumerator = turnBasedRoutine = turnBasedRoutine ?? TurnBasedRoutine(caster, target, attack);
        }
        else
        {
          enumerator = runtimeRoutine =
            runtimeRoutine ?? RuntimeRoutine(caster, target, attack, endPoint);
        }
        if (!enumerator.MoveNext())
        {
          break;
        }
        yield return null;
      }
    }

    private void ConsumeWheelGates(UnitEntityData caster, AbilityExecutionContext context)
    {
      try
      {
        var ready = WheelReadyBuff is null ? null : caster.Buffs.GetBuff(WheelReadyBuff);
        if (ready != null)
        {
          caster.RemoveFact(ready);
        }
        if (EternalFeature is null || !caster.HasFact(EternalFeature))
        {
          caster.Descriptor.AddBuff(
            WheelSpentBuff, context, 1.Rounds().Seconds);
        }
        MissionFeats.Logger.Info($"[carousel] wheel engaged against a new mark.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Carousel: wheel gating failed.", e);
      }
    }

    private static IEnumerator TurnBasedRoutine(
      UnitEntityData caster, UnitEntityData target, UnitAttack attack)
    {
      UnitEntityData mount = caster.GetSaddledUnit();
      if (mount == null)
      {
        UnitMovementAgent agentASP = caster.View.AgentASP;
        float timeSinceStart = 0f;
        while (attack.ShouldUnitApproach)
        {
          if (Game.Instance.TurnBasedCombatController.WaitingForUI)
          {
            yield return null;
          }
          else
          {
            timeSinceStart += Game.Instance.TimeController.GameDeltaTime;
            if (timeSinceStart > 6f)
            {
              break;
            }
            if (caster.GetThreatHand() == null || !caster.Descriptor.State.CanMove || !agentASP)
            {
              break;
            }
            if (!agentASP.IsReallyMoving)
            {
              agentASP.ForcePath(new ForcedPath(new List<Vector3>
              {
                caster.Position,
                target.Position,
              }), true);
              if (!agentASP.IsReallyMoving)
              {
                break;
              }
            }
            agentASP.MaxSpeedOverride = new float?(
              Math.Max(agentASP.MaxSpeedOverride.GetValueOrDefault(), caster.CombatSpeedMps * 2f));
            yield return null;
          }
        }
      }
      else
      {
        while (IsMountCharging(caster))
        {
          yield return null;
        }
      }
      caster.View.StopMoving();
      if (!attack.ShouldUnitApproach)
      {
        attack.IgnoreCooldown(null);
        attack.IsCharge = true;
        SyncMountCommands(caster, mount, attack);
        caster.Commands.AddToQueueFirst(attack);
      }
    }

    private static IEnumerator RuntimeRoutine(
      UnitEntityData caster, UnitEntityData target, UnitAttack attack, Vector3 endPoint)
    {
      float maxDistance = GetMaxRangeMeters(caster);
      UnitEntityData mount = caster.GetSaddledUnit();
      if (mount == null)
      {
        float passedDistance = 0f;
        while (caster.View.MovementAgent.IsReallyMoving)
        {
          float valueOrDefault = caster.View.MovementAgent.MaxSpeedOverride.GetValueOrDefault();
          caster.View.MovementAgent.MaxSpeedOverride = new float?(
            Math.Max(valueOrDefault, caster.CombatSpeedMps * 2f));
          passedDistance += (caster.Position - caster.PreviousPosition).magnitude;
          if (passedDistance > maxDistance || !attack.ShouldUnitApproach)
          {
            break;
          }
          if (caster.GetThreatHand() == null)
          {
            break;
          }
          Vector3 position = target.Position;
          if (ObstacleAnalyzer.TraceAlongNavmesh(caster.Position, position) != position)
          {
            break;
          }
          if (position != endPoint)
          {
            endPoint = position;
            caster.View.AgentASP.ForcePath(new ForcedPath(new List<Vector3>
            {
              caster.Position,
              endPoint,
            }), true);
          }
          yield return null;
        }
      }
      else
      {
        while (IsMountCharging(caster))
        {
          yield return null;
        }
      }
      if (!attack.ShouldUnitApproach)
      {
        attack.IgnoreCooldown(null);
        attack.IsCharge = true;
      }
      SyncMountCommands(caster, mount, attack);
      caster.Commands.AddToQueueFirst(attack);
    }

    private static void SyncMountCommands(
      UnitEntityData caster, UnitEntityData mount, UnitAttack attack)
    {
      UnitEntityData rider = caster.GetRider();
      if (rider != null)
      {
        if (rider.Commands.Attack != null)
        {
          attack.AddRiderCommand(rider.Commands.Attack);
          rider.Commands.Attack.AddMountCommand(attack);
        }
      }
      else if (mount != null && mount.Commands.Attack != null)
      {
        attack.AddMountCommand(mount.Commands.Attack);
        mount.Commands.Attack.AddRiderCommand(attack);
      }
    }

    private static bool IsMountCharging(UnitEntityData rider)
    {
      UnitEntityData saddledUnit = rider.GetSaddledUnit();
      if (saddledUnit == null)
      {
        return false;
      }
      UnitUseAbility unitUseAbility = saddledUnit.Commands.Standard as UnitUseAbility;
      return unitUseAbility != null && unitUseAbility.Ability.Blueprint.GetComponent<CarouselChargeLogic>() != null;
    }

    public override void Cleanup(AbilityExecutionContext context)
    {
      context.Caster.View.AgentASP.IsCharging = false;
      context.Caster.View.AgentASP.MaxSpeedOverride = null;
      context.Caster.Descriptor.State.IsCharging = false;
    }

    public static float GetMinRangeMeters(UnitEntityData caster, UnitEntityData target)
    {
      float num = target != null ? target.View.Corpulence : 0.5f;
      if (Game.Instance.Player.IsTurnBasedModeOn())
      {
        return TurnController.MetersOfFiveFootStep + GameConsts.MinWeaponRange.Meters +
          caster.View.Corpulence + num;
      }
      return 10.Feet().Meters + caster.View.Corpulence + num;
    }

    public float GetMinRangeMeters(UnitEntityData caster)
    {
      return GetMinRangeMeters(caster, null);
    }

    public static float GetMaxRangeMeters(UnitEntityData caster)
    {
      return caster.CombatSpeedMps * 6f;
    }

    public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper targetWrapper)
    {
      LocalizedString failReason;
      return CheckTargetRestriction(caster, targetWrapper, out failReason);
    }

    public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target)
    {
      LocalizedString failReason;
      CheckTargetRestriction(caster, target, out failReason);
      return failReason;
    }

    private bool CheckTargetRestriction(
      UnitEntityData caster, TargetWrapper targetWrapper, out LocalizedString failReason)
    {
      UnitEntityData targetUnit = targetWrapper != null ? targetWrapper.Unit : null;
      if (targetUnit == null)
      {
        failReason = BlueprintRoot.Instance.LocalizedTexts.Reasons.TargetIsInvalid;
        return false;
      }
      if (IsWheel)
      {
        bool grand = GrandFeature != null && caster.HasFact(GrandFeature);
        bool eternal = EternalFeature != null && caster.HasFact(EternalFeature);
        if (!grand && (WheelReadyBuff == null || caster.Buffs.GetBuff(WheelReadyBuff) is null))
        {
          failReason = BlueprintRoot.Instance.LocalizedTexts.Reasons.TargetIsInvalid;
          return false;
        }
        if (!eternal && (WheelSpentBuff != null && caster.Buffs.GetBuff(WheelSpentBuff) != null))
        {
          failReason = BlueprintRoot.Instance.LocalizedTexts.Reasons.TargetIsInvalid;
          return false;
        }
      }
      float magnitude = (targetUnit.Position - caster.Position).magnitude;
      if (magnitude > GetMaxRangeMeters(caster))
      {
        failReason = BlueprintRoot.Instance.LocalizedTexts.Reasons.TargetIsTooFar;
        return false;
      }
      if (magnitude < GetMinRangeMeters(caster, targetUnit))
      {
        failReason = BlueprintRoot.Instance.LocalizedTexts.Reasons.TargetIsTooClose;
        return false;
      }
      if (ObstacleAnalyzer.TraceAlongNavmesh(caster.Position, targetUnit.Position) != targetUnit.Position)
      {
        failReason = BlueprintRoot.Instance.LocalizedTexts.Reasons.ObstacleBetweenCasterAndTarget;
        return false;
      }
      UnitEntityData saddledUnit = caster.GetSaddledUnit();
      if (!(saddledUnit ?? caster).View.MovementAgent.AvoidanceDisabled)
      {
        float num = caster.View.Corpulence + targetUnit.View.Corpulence;
        ItemEntityWeapon firstWeapon = caster.GetFirstWeapon();
        float valueOrDefault =
          (num + (firstWeapon != null ? new float?(firstWeapon.AttackRange.Meters) : null))
          .GetValueOrDefault();
        Vector2 normalized = (targetUnit.Position - caster.Position).To2D().normalized;
        Vector2 a = targetUnit.Position.To2D() - normalized * valueOrDefault;
        foreach (UnitEntityData other in Game.Instance.State.AwakeUnits)
        {
          if (other == caster || other == targetUnit || !other.View ||
            other.View.MovementAgent.AvoidanceDisabled)
          {
            continue;
          }
          magnitude = (a - other.Position.To2D()).magnitude;
          // Slip the Line: allies never block the charge (the pplus stag
          // overflight bypass, on the ground).
          bool slip = SlipFeature != null && caster.HasFact(SlipFeature);
          if (magnitude < (caster.View.Corpulence + other.View.Corpulence) * 0.8f &&
            (!slip || !other.IsAlly(caster)))
          {
            failReason = BlueprintRoot.Instance.LocalizedTexts.Reasons.ObstacleBetweenCasterAndTarget;
            return false;
          }
        }
      }
      UnitEntityData mover = caster.GetSaddledUnit() ?? caster;
      bool charging = caster.State.IsCharging ||
        (saddledUnit != null && saddledUnit.State.IsCharging) ||
        (mover != null && mover.State.IsCharging);
      if (CombatController.IsInTurnBasedCombat() && caster.IsCurrentUnit() && !charging &&
        mover.CombatState.TBM.TimeMoved > 0f)
      {
        failReason = BlueprintRoot.Instance.LocalizedTexts.Reasons.AlreadyMovedThisTurn;
        return false;
      }
      failReason = null;
      return true;
    }
  }
}

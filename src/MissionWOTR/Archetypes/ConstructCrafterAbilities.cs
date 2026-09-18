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
using Kingmaker.AI.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Items;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Reflection;
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
  /// Construct abilities are granted as facts at deploy; the engine drops cast actions
  /// for abilities the unit does not own, so brains can list every cast safely.
  ///
  /// Brains v2 adds program behaviors: Passive (close escort of the crafter, pacifist),
  /// Guard (support casts, attacks, then escort), Distance (casts, ranged attacks, loose
  /// escort). Follow actions home in on the crafter via a FactConsideration on the
  /// active program's marker buff. The deploy action assigns the brain at runtime via
  /// UnitBrain.SetBrain.
  ///
  /// Role variants: the deploy action picks a variant unit by active core:
  ///   HumanoidArcher (Arbalest - bow in inventory, attacks with equipped weapon),
  ///   HumanoidCaster (Flaming/Soft), GolemCaster (Cold/Arbalest/Soft),
  ///   SentryRanged (Arbalest/Soft). Base identity is tracked with marker buffs so any
  ///   variant replaces any earlier construct of the same base.
  /// </summary>
  internal static class ConstructCrafterAbilities
  {
    internal static BlueprintAbility FireBlast;
    internal static BlueprintAbility IceRay;
    internal static BlueprintAbility Mend;
    internal static BlueprintAbility BoltSpit;
    internal static BlueprintAbility BlinkStrike;

    internal static BlueprintBuff MendBuff;
    internal static BlueprintBuff SentryBaseMarker;
    internal static BlueprintBuff ManBaseMarker;
    internal static BlueprintBuff GolemBaseMarker;

    internal static BlueprintUnit HumanoidArcherUnit;
    internal static BlueprintUnit HumanoidCasterUnit;
    internal static BlueprintUnit GolemCasterUnit;
    internal static BlueprintUnit SentryRangedUnit;

    // Brains: role (v1) and program-behavior (v2) handles for the deploy-time
    // runtime brain assignment (UnitBrain.SetBrain).
    internal static BlueprintBrain CasterBrain;
    internal static BlueprintBrain DefaultBrain;
    internal static BlueprintBrain PassiveBrain;
    internal static BlueprintBrain GuardBrain;
    internal static BlueprintBrain DistanceBrain;

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
      SentryBaseMarker = BuffConfigurator.New("ConstructCrafterSentryBaseMarker", Guids.SentryBaseMarker)
        .SetDisplayName("SentryBaseMarker.Name")
        .SetDescription("SentryBaseMarker.Description")
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

      // Blink Strike (Infernal, all bases): teleport to the target and strike it with
      // the construct's weapon (full attack pipeline - hit chance, crit, sneak dice,
      // on-hit riders). Long range: this is the Infernal's gap-closer.
      BlinkStrike = AbilityConfigurator.New("ConstructCrafterBlinkStrike", Guids.BlinkStrikeAbility)
        .SetDisplayName("BlinkStrike.Name")
        .SetDescription("BlinkStrike.Description")
        .SetIcon(AbilityRefs.BloodragerInfernalHellfireStrikeAbility.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetEnemies()
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().Add(ElementTool.Create<ContextActionBlinkStrike>()))
        .Configure();
    }

    private static void ConfigureBrains()
    {
      // Cast actions: one per construct ability. The engine keeps a cast action out of
      // a unit's available actions unless the unit actually owns the ability
      // (BlueprintAiCastSpell.ShouldBeInActionsList), so a brain can safely list every
      // cast. BaseScore 20 ranks casts above the stock attack action (whose default
      // score wins otherwise); once per round prevents chain-casting in real time.
      // Mend only targets wounded allies (full-health targets score 0).
      var wounded = HealthConsiderationConfigurator.New(
          "ConstructCrafterWoundedConsideration", Guids.CrafterWoundedConsideration)
        .SetFullBorder(95)
        .SetAboveFullScore(0f)
        .SetFullScore(1f)
        .SetDeadBorder(1)
        .SetDeadScore(0f)
        .SetBelowDeadScore(0f)
        .Configure();

      var castFireBlast = AiCastSpellConfigurator.New("ConstructCrafterAiFireBlast", Guids.AiCastFireBlast)
        .SetAbility(FireBlast)
        .SetBaseScore(20f)
        .SetOncePerRound()
        .Configure();
      var castIceRay = AiCastSpellConfigurator.New("ConstructCrafterAiIceRay", Guids.AiCastIceRay)
        .SetAbility(IceRay)
        .SetBaseScore(20f)
        .SetOncePerRound()
        .Configure();
      var castMend = AiCastSpellConfigurator.New("ConstructCrafterAiMend", Guids.AiCastMend)
        .SetAbility(Mend)
        .SetBaseScore(20f)
        .SetOncePerRound()
        .SetTargetConsiderations(wounded)
        .Configure();
      var castBoltSpit = AiCastSpellConfigurator.New("ConstructCrafterAiBoltSpit", Guids.AiCastBoltSpit)
        .SetAbility(BoltSpit)
        .SetBaseScore(20f)
        .SetOncePerRound()
        .Configure();
      var castBlinkStrike = AiCastSpellConfigurator.New("ConstructCrafterAiBlinkStrike", Guids.AiCastBlinkStrike)
        .SetAbility(BlinkStrike)
        .SetBaseScore(20f)
        .SetOncePerRound()
        .Configure();
      // The game's BlueprintAiAttack is compiled internal, so a custom attack action
      // cannot be created from mod code. The stock attack action is instead lifted out
      // of a base-game unit's brain and reused as the weapon-attack fallback.
      var attack = LiftStockAttackAction();
      if (attack is null)
      {
        Main.Logger.Warn("Construct Crafter: no stock attack action found; brains will only cast.");
      }

      // --- v1 role brains (baked defaults on the variant units) ---
      // The blink strike is only in the caster brain: Guard/Distance constructs hold
      // formation (their behavior programs replace the caster brain at deploy).
      var casterActions = new List<Blueprint<BlueprintAiActionReference>>
        { castFireBlast, castIceRay, castMend, castBoltSpit, castBlinkStrike };
      var rangedActions = new List<Blueprint<BlueprintAiActionReference>> { castBoltSpit };
      if (attack != null)
      {
        casterActions.Add(attack);
        rangedActions.Add(attack);
      }
      CasterBrain = BrainConfigurator.New("ConstructCrafterCasterBrain", Guids.CrafterCasterBrain)
        .SetActions(casterActions.ToArray())
        .Configure();
      BrainConfigurator.New("ConstructCrafterRangedBrain", Guids.CrafterRangedBrain)
        .SetActions(rangedActions.ToArray())
        .Configure();

      // --- v2 program-behavior brains ---
      // Follow-the-crafter: candidate targets are the friend group; only units
      // carrying an active program marker buff score (that is the crafter - marker
      // buffs sit on the crafter while the program toggle is on).
      var followMaster = FactConsiderationConfigurator.New(
          "ConstructCrafterFollowMasterConsideration", Guids.CrafterFollowMasterConsideration)
        .SetFact(ProgramMarkersForFollow())
        .SetHasFactScore(1f)
        .SetNoFactScore(0f)
        .Configure();

      Blueprint<BlueprintAiActionReference> FollowAction(
          string name, string guid, float approachFeet, float score)
      {
        return AiFollowConfigurator.New(name, guid)
          .SetTargetType(TargetType.Friend)
          .SetApproachRange(new Feet(approachFeet))
          .SetBaseScore(score)
          .SetTargetConsiderations(followMaster)
          .Configure();
      }

      // Passive: escort only - stay glued to the crafter, never charge or cast.
      // (The engine's run-away action flees toward the map exit and would desert,
      // so passive is expressed as close escort instead.)
      var followPassive = FollowAction(
        "ConstructCrafterAiFollowPassive", Guids.AiFollowPassive, approachFeet: 5f, score: 25f);
      // Guard: hold the crafter's company (10 ft) when nothing threatens.
      var followGuard = FollowAction(
        "ConstructCrafterAiFollowGuard", Guids.AiFollowGuard, approachFeet: 10f, score: 10f);
      // Distance: shadow the crafter loosely (30 ft), fight from range.
      var followDistance = FollowAction(
        "ConstructCrafterAiFollowDistance", Guids.AiFollowDistance, approachFeet: 30f, score: 8f);

      // Default: follow the crafter closely and fight. Assigned at deploy when no
      // program is active and the core grants no role abilities - without this the
      // unit keeps its (player-companion) blueprint brain and just stands around.
      var followDefault = FollowAction(
        "ConstructCrafterAiFollowDefault", Guids.AiFollowDefault, approachFeet: 10f, score: 15f);
      var defaultActions = new List<Blueprint<BlueprintAiActionReference>> { followDefault };
      if (attack != null)
      {
        defaultActions.Add(attack);
      }
      DefaultBrain = BrainConfigurator.New("ConstructCrafterDefaultBrain", Guids.CrafterDefaultBrain)
        .SetActions(defaultActions.ToArray())
        .Configure();

      // Caster and ranged variants also escort the crafter when idle (low priority).
      casterActions.Add(followGuard);
      rangedActions.Add(followGuard);
      BrainConfigurator.For("ConstructCrafterCasterBrain")
        .SetActions(casterActions.ToArray())
        .Configure();
      BrainConfigurator.For("ConstructCrafterRangedBrain")
        .SetActions(rangedActions.ToArray())
        .Configure();

      var passiveActions = new List<Blueprint<BlueprintAiActionReference>> { followPassive };
      if (attack != null)
      {
        passiveActions.Add(attack);
      }
      PassiveBrain = BrainConfigurator.New("ConstructCrafterPassiveBrain", Guids.CrafterPassiveBrain)
        .SetActions(passiveActions.ToArray())
        .Configure();

      // Guard: casts (support - e.g. Mend) > attacks > return to the crafter.
      var guardActions = new List<Blueprint<BlueprintAiActionReference>>
        { castFireBlast, castIceRay, castMend, castBoltSpit };
      if (attack != null)
      {
        guardActions.Add(attack);
      }
      guardActions.Add(followGuard);
      GuardBrain = BrainConfigurator.New("ConstructCrafterGuardBrain", Guids.CrafterGuardBrain)
        .SetActions(guardActions.ToArray())
        .Configure();

      // Distance: casts > ranged attacks > loose escort at the crafter's side.
      var distanceActions = new List<Blueprint<BlueprintAiActionReference>>
        { castFireBlast, castIceRay, castMend, castBoltSpit };
      if (attack != null)
      {
        distanceActions.Add(attack);
      }
      distanceActions.Add(followDistance);
      DistanceBrain = BrainConfigurator.New("ConstructCrafterDistanceBrain", Guids.CrafterDistanceBrain)
        .SetActions(distanceActions.ToArray())
        .Configure();
    }

    /// <summary>
    /// Marker buffs of the behavior programs (Passive, Guard, Distance) - the buffs that
    /// sit on the crafter while those programs are toggled on.
    /// </summary>
    private static Blueprint<BlueprintUnitFactReference>[] ProgramMarkersForFollow()
    {
      // The crafter marker is always included: it sits on the crafter whenever a
      // construct has been deployed, so follow actions find their target even when
      // no program toggle is active.
      var markers = ConstructCrafterPrograms.Programs
        .Where(p => p.IsPassive || p.IsGuard || p.IsDistance)
        .Select(p => (Blueprint<BlueprintUnitFactReference>)p.Marker)
        .ToList();
      if (ConstructCrafter.CrafterMarkerBuff != null)
      {
        markers.Add(ConstructCrafter.CrafterMarkerBuff);
      }
      return markers.ToArray();
    }

    private static void ConfigureVariantUnits()
    {
      var dogFaction = UnitRefs.AnimalCompanionUnitDog.Reference.Get().Faction;
      var bandit = UnitRefs.CR0_5_Bandit_Human_FighterMelee_Male.Reference.Get();
      var stoneGolem = UnitRefs.CR11_GolemStone.Reference.Get();
      var woodGolem = UnitRefs.CR6_GolemWood.Reference.Get();

      // Variant units are FULL clones of their stock units (see ConstructCrafter.
      // CloneUnit for why bare CopyFrom produced empty units), with role overrides
      // and custom brains applied on top.

      // Archer humanoid: bow in inventory; the stock brain attacks with the equipped weapon.
      HumanoidArcherUnit = ConstructCrafter.CloneUnit(
        "ConstructCrafterHumanoidArcher", Guids.ConstructCrafterHumanoidArcherUnit, bandit);
      UnitConfigurator.For("ConstructCrafterHumanoidArcher")
        .SetFaction(dogFaction)
        .SetStartingInventory(ItemWeaponRefs.CompositeLongbow.Cast<BlueprintItemReference>())
        .Configure();

      // Caster humanoid / caster golem / ranged sentry: custom brains.
      HumanoidCasterUnit = ConstructCrafter.CloneUnit(
        "ConstructCrafterHumanoidCaster", Guids.ConstructCrafterHumanoidCasterUnit, bandit);
      UnitConfigurator.For("ConstructCrafterHumanoidCaster")
        .SetFaction(dogFaction)
        .Configure();
      SetBrain(Guids.ConstructCrafterHumanoidCasterUnit, "ConstructCrafterCasterBrain");

      GolemCasterUnit = ConstructCrafter.CloneUnit(
        "ConstructCrafterGolemCaster", Guids.ConstructCrafterGolemCasterUnit, stoneGolem,
        c => !c.name.Contains("Slow")
          && c is not Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical);
      UnitConfigurator.For("ConstructCrafterGolemCaster")
        .SetStrength(32 - 2)
        .SetMaxHP(107 - 20)
        .SetFaction(dogFaction)
        .Configure();
      SetBrain(Guids.ConstructCrafterGolemCasterUnit, "ConstructCrafterCasterBrain");

      SentryRangedUnit = ConstructCrafter.CloneUnit(
        "ConstructCrafterSentryRanged", Guids.ConstructCrafterSentryRangedUnit, woodGolem);
      UnitConfigurator.For("ConstructCrafterSentryRanged")
        .SetMaxHP(Math.Max(8, woodGolem.MaxHP / 3))
        .SetStrength(woodGolem.Strength - 4)
        .SetDexterity(woodGolem.Dexterity - 2)
        .SetFaction(dogFaction)
        .Configure();
      SetBrain(Guids.ConstructCrafterSentryRangedUnit, "ConstructCrafterRangedBrain");
    }

    private static void SetBrain(string unitGuid, string brainName)
    {
      // BlueprintUnit.m_Brain is private in current game builds (only a public
      // DefaultBrain getter is exposed), so the reference is assigned via reflection.
      var unit = BlueprintTool.Get<BlueprintUnit>(unitGuid);
      var brainRef = BlueprintTool.GetRef<BlueprintBrainReference>(brainName);
      var field = typeof(BlueprintUnit).GetField(
        "m_Brain", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      if (field is null)
      {
        Main.Logger.Warn($"SetBrain: BlueprintUnit.m_Brain not found; {unitGuid} keeps its copied brain.");
        return;
      }
      field.SetValue(unit, brainRef);
    }

    /// <summary>
    /// Returns the game's stock weapon-attack AiAction (reference form), taken from a
    /// base-game unit's brain. BlueprintAiAttack itself is internal, so the match is by
    /// runtime type name and the stock reference is reused directly.
    /// </summary>
    private static Blueprint<BlueprintAiActionReference> LiftStockAttackAction()
    {
      var sources = new[]
      {
        UnitRefs.CR0_5_Bandit_Human_FighterMelee_Male.Reference.Get(),
        UnitRefs.AnimalCompanionUnitDog.Reference.Get()
      };
      var actionsField = typeof(BlueprintBrain).GetField(
        "m_Actions", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      foreach (var source in sources)
      {
        var brain = source?.DefaultBrain;
        if (brain is null || actionsField is null) { continue; }
        if (actionsField.GetValue(brain) is BlueprintAiActionReference[] actions)
        {
          foreach (var action in actions)
          {
            var actionBlueprint = action?.Get();
            if (actionBlueprint != null && actionBlueprint.GetType().Name == "BlueprintAiAttack")
            {
              return action;
            }
          }
        }
      }
      return null;
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
        // The blast starts small (1d6) and grows with the crafter's alchemist level:
        // one extra die per 5 levels, topping out at 5d6.
        var alchemist = CharacterClassRefs.AlchemistClass.Reference.Get();
        var alchemistLevel = 1;
        var crafter = Context?.MaybeCaster;
        if (crafter != null)
        {
          alchemistLevel = Math.Max(
            1, crafter.Descriptor.Progression.GetClassLevel(alchemist));
        }
        var dice = Math.Min(5, 1 + alchemistLevel / 5);
        foreach (var victim in victims)
        {
          var bundle = new DamageBundle();
          bundle.Add(new DirectDamage(new DiceFormula(dice, DiceType.D6), 0));
          Rulebook.Trigger(new RuleDealDamage(Owner, victim, bundle) { Reason = Fact });
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ConstructCrafter: sonic boom failed.", e);
      }
    }
  }

  /// <summary>
  /// Infernal blink strike: teleport the caster next to the target and strike with the
  /// construct's weapon via the full attack pipeline (attack roll, crits, sneak dice,
  /// on-hit riders all apply). Falls back to raw 2d6 damage if the unit has no weapon.
  /// </summary>
  [Kingmaker.Blueprints.JsonSystem.TypeId(Guids.BlinkStrikeAction)]
  internal class ContextActionBlinkStrike : ContextAction
  {
    public override string GetCaption() => "Blink strike";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        var target = Target?.Unit ?? Context.MainTarget?.Unit;
        if (caster is null || target is null || target.HPLeft <= 0)
        {
          return;
        }

        // Blink to the target's side, approaching from the construct's original side.
        var direction = caster.Position - target.Position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
          direction = Vector3.forward;
        }
        var spot = target.Position
          + direction.normalized * (target.Corpulence + caster.Corpulence + 0.75f);
        caster.Position = spot;

        // Strike with the construct's weapon.
        var weapon = caster.Body?.PrimaryHand?.MaybeItem as ItemEntityWeapon;
        if (weapon != null)
        {
          Rulebook.Trigger(new RuleAttackWithWeapon(caster, target, weapon, 0)
          {
            Reason = Context,
          });
        }
        else
        {
          var bundle = new DamageBundle();
          bundle.Add(new DirectDamage(new DiceFormula(2, DiceType.D6), 0));
          Rulebook.Trigger(new RuleDealDamage(caster, target, bundle)
          {
            Reason = Context,
          });
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ConstructCrafter: blink strike failed.", e);
      }
    }
  }

  /// <summary>
  /// Flank program rider: while the flank program runs, the construct's casting is
  /// sapped - its effective caster level for every ability it uses drops by the
  /// crafter's full alchemist level (weaker ability DCs and level-scaled effects).
  /// The construct is a melee killer, not a spellcaster.
  /// </summary>
  [Kingmaker.Blueprints.JsonSystem.TypeId(Guids.FlankCasterLevelPenalty)]
  internal class ConstructFlankCasterLevelPenalty : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAbilityParams>
  {
    public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
    {
      try
      {
        var crafter = Context?.MaybeCaster;
        if (crafter is null)
        {
          return;
        }
        var alchemist = CharacterClassRefs.AlchemistClass.Reference.Get();
        var alchemistLevel = crafter.Descriptor.Progression.GetClassLevel(alchemist);
        if (alchemistLevel > 0)
        {
          evt.AddBonusCasterLevel(-alchemistLevel, ModifierDescriptor.Penalty);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ConstructCrafter: flank caster-level penalty failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAbilityParams evt) { }
  }
}

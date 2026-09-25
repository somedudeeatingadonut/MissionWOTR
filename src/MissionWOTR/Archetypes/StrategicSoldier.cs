using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Strategic Soldier (faithful port of the Orphaned Bookworm Productions
  /// fighter archetype, "Breath of Life: The Marshal" 2025) - a master of
  /// positioning and teamwork in combat.
  ///
  /// Tabletop and its WOTR adaptation:
  /// - Flanker (1st, replaces the 1st-level bonus combat feat): designate an
  ///   adjacent square as the flanking origin. WOTR computes flanking from
  ///   position - adapted via the AllyFlankerBonus component (shared with
  ///   the Polearm Master's Flexible Flanker): +2 on attacks against any
  ///   target that at least one other ally threatens.
  /// - Sidestep (2nd, replaces all bravery): the Sidestep bonus feat. WOTR
  ///   has no Sidestep feat and no 5-foot-step reactions - adapted by
  ///   granting the vanilla Mobility feat (the game's
  ///   evade-through-threatened-squares feat).
  /// - Interpose (3rd, replaces armor training 1): take the damage of an
  ///   attack on an adjacent ally, X/day. WOTR has the perfect engine for
  ///   it - grants the vanilla Divine Guardian's Bodyguard + In Harm's Way
  ///   package (the Sister-in-Arms Devoted Defender recipe), which is
  ///   unlimited but occupies the bodyguard's reaction each round.
  /// - Defending Allies (7th, replaces armor training 2): while fighting
  ///   defensively, share the AC bonus with two adjacent allies. WOTR
  ///   exposes no fighting-defensively hook - adapted to a constant guard:
  ///   allies within 5 feet gain +2 dodge AC while he stands with them.
  /// - Reckless Strike (11th, replaces armor training 3): once per round, a
  ///   swift action granting +5 attack and damage for 1 round, while any
  ///   attack that lands on him counts as a critical threat - adapted to a
  ///   -5 AC penalty during the reckless round (the averaged cost of every
  ///   hit becoming a crit threat).
  /// - Knock Off-kilter (15th, replaces armor training 4): a full-round
  ///   single attack that leaves the target provoking attacks of opportunity
  ///   from threatening allies. Realized as a standard-action strike: the
  ///   soldier and every ally threatening the target immediately attack it
  ///   (the combat-engagement ForceAttackOfOpportunity API, the
  ///   Sister-in-Arms Act as One recipe). Not usable on creatures more than
  ///   one size larger. Vital Strike interplay is not implemented.
  /// - Punishing Strike (19th, replaces armor mastery): an attack of
  ///   opportunity against a foe that damages an ally, once per round, if he
  ///   threatens the attacker and has attacks of opportunity left (the pplus
  ///   GoldenLegionnaire global-reaction pattern + the ttt AoO-budget
  ///   decrement).
  /// Log prefix: [strategic].
  /// </summary>
  internal static class StrategicSoldier
  {
    internal const string ArchetypeName = "StrategicSoldierArchetype";
    internal const string FlankerName = "StrategicSoldierFlanker";
    internal const string SidestepName = "StrategicSoldierSidestep";
    internal const string InterposeName = "StrategicSoldierInterpose";
    internal const string DefendingAlliesName = "StrategicSoldierDefendingAllies";
    internal const string RecklessName = "StrategicSoldierRecklessStrike";
    internal const string KnockOffName = "StrategicSoldierKnockOffKilter";
    internal const string PunishingName = "StrategicSoldierPunishingStrike";

    public static void Configure()
    {
      var fighter = CharacterClassRefs.FighterClass.Reference.Get();
      var icon = FeatureRefs.ArmorTraining.Reference.Get().Icon;

      // ----- Flanker (1st) -----
      var flanker = FeatureConfigurator.New(FlankerName, Guids.StrategicFlanker)
        .SetDisplayName(FlankerName + ".Name")
        .SetDescription(FlankerName + ".Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new AllyFlankerBonus())
        .Configure();

      // ----- Sidestep (2nd) -----
      var sidestep = FeatureConfigurator.New(SidestepName, Guids.StrategicSidestep)
        .SetDisplayName(SidestepName + ".Name")
        .SetDescription(SidestepName + ".Description")
        .SetIcon(FeatureRefs.Mobility.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { FeatureRefs.Mobility.Reference.Get() })
        .Configure();

      // ----- Interpose (3rd) -----
      var interpose = FeatureConfigurator.New(InterposeName, Guids.StrategicInterpose)
        .SetDisplayName(InterposeName + ".Name")
        .SetDescription(InterposeName + ".Description")
        .SetIcon(FeatureRefs.DivineGuardianBodyguardFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { FeatureRefs.DivineGuardianBodyguardFeature.Reference.Get() })
        .Configure();

      // ----- Defending Allies (7th) -----
      var defending = FeatureConfigurator.New(DefendingAlliesName, Guids.StrategicDefendingAllies)
        .SetDisplayName(DefendingAlliesName + ".Name")
        .SetDescription(DefendingAlliesName + ".Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new StrategicGuardAura())
        .Configure();

      // ----- Reckless Strike (11th) -----
      var recklessBuff = BuffConfigurator.New(RecklessName + "Buff", Guids.StrategicRecklessBuff)
        .SetDisplayName(RecklessName + ".Name")
        .SetDescription(RecklessName + ".Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new StrategicRecklessBuffLogic())
        .Configure();
      var recklessAbility = AbilityConfigurator.New(RecklessName + "Ability", Guids.StrategicRecklessAbility)
        .SetDisplayName(RecklessName + ".Name")
        .SetDescription(RecklessName + ".Description")
        .SetIcon(icon)
        .SetRange(AbilityRange.Personal)
        .SetCanTargetSelf(true)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
        .AddAbilityEffectRunAction(BlueprintCore.Actions.Builder.ActionsBuilder.New()
          .Add(new StrategicRecklessAction { Buff = recklessBuff }))
        .Configure();
      var reckless = FeatureConfigurator.New(RecklessName, Guids.StrategicRecklessFeature)
        .SetDisplayName(RecklessName + ".Name")
        .SetDescription(RecklessName + ".Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { recklessAbility })
        .Configure();

      // ----- Knock Off-kilter (15th) -----
      var knockOff = AbilityConfigurator.New(KnockOffName + "Ability", Guids.StrategicKnockAbility)
        .SetDisplayName(KnockOffName + ".Name")
        .SetDescription(KnockOffName + ".Description")
        .SetIcon(icon)
        .SetRange(AbilityRange.Close)
        .SetCanTargetEnemies(true)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard)
        .AddComponent(new StrategicKnockSizeRestriction())
        .AddAbilityEffectRunAction(BlueprintCore.Actions.Builder.ActionsBuilder.New()
          .Add(new StrategicKnockAction()))
        .Configure();
      var knockFeature = FeatureConfigurator.New(KnockOffName, Guids.StrategicKnockFeature)
        .SetDisplayName(KnockOffName + ".Name")
        .SetDescription(KnockOffName + ".Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { knockOff })
        .Configure();

      // ----- Punishing Strike (19th) -----
      var punishing = FeatureConfigurator.New(PunishingName, Guids.StrategicPunishingFeature)
        .SetDisplayName(PunishingName + ".Name")
        .SetDescription(PunishingName + ".Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new StrategicPunishingStrike())
        .Configure();

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.StrategicSoldierArchetype, CharacterClassRefs.FighterClass)
          .SetLocalizedName("StrategicSoldier.Name")
          .SetLocalizedDescription("StrategicSoldier.Description")
          .AddToAddFeatures(LevelPlan.L(1), flanker)
          .AddToAddFeatures(LevelPlan.L(2), sidestep)
          .AddToAddFeatures(LevelPlan.L(3), interpose)
          .AddToAddFeatures(LevelPlan.L(7), defending)
          .AddToAddFeatures(LevelPlan.L(11), reckless)
          .AddToAddFeatures(LevelPlan.L(15), knockFeature)
          .AddToAddFeatures(LevelPlan.L(19), punishing);

      // Trades: ONLY the 1st-level bonus combat feat (the selection is
      // granted at many levels - RemoveAtLevel prunes just the 1st), all
      // bravery, armor training (all four), and armor mastery.
      archetype = ArchetypeRemovals.RemoveAtLevel(
        archetype, fighter.Progression, 1,
        "41c8486641f7d6d4283ca9dae4147a9f"); // FighterFeatSelection @1
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, fighter,
        "f6388946f9f472f4585591b80e9f2452", // Bravery (all instances)
        "3c380607706f209499d951b29d3c44f3", // ArmorTraining (all four)
        "ae177f17cfb45264291d4d7c2cb64671", // ArmorMastery
        "e52aa4151b214d00b720e682fbb4538b"); // FighterArmorMastery

      archetype.Configure();

      MissionFeats.Logger.Info("StrategicSoldier: configured.");
    }
  }

  /// <summary>
  /// Defending Allies, adapted: allies within 5 feet of the soldier gain
  /// +2 dodge AC (a global AC handler - he need not be the attacker).
  /// </summary>
  [TypeId(Guids.StrategicGuardComponent)]
  internal class StrategicGuardAura : UnitFactComponentDelegate,
    IGlobalRulebookHandler<RuleCalculateAC>, IRulebookHandler<RuleCalculateAC>,
    ISubscriber, IGlobalRulebookSubscriber
  {
    public void OnEventAboutToTrigger(RuleCalculateAC evt)
    {
      try
      {
        if (evt.Target != null && evt.Target != Owner && evt.Target.IsAlly(Owner) &&
          evt.Target.DistanceTo(Owner) <= 5.Feet().Meters)
        {
          evt.AddModifier(2, Fact, ModifierDescriptor.Dodge);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[strategic] guard aura failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt)
    {
    }
  }

  /// <summary>Applies the 1-round reckless buff to the caster.</summary>
  [TypeId(Guids.StrategicRecklessAction)]
  internal class StrategicRecklessAction : ContextAction
  {
    public BlueprintBuff Buff;

    public override string GetCaption() => "Reckless Strike";

    public override void RunAction()
    {
      try
      {
        var caster = Context?.MaybeCaster;
        if (caster is null || Buff is null)
        {
          return;
        }
        caster.Descriptor.AddBuff(
          Buff, Context, ContextDuration.Fixed(1).Calculate(Context).Seconds);
        MissionFeats.Logger.Info(
          $"[strategic] reckless strike: {caster.CharacterName} throws caution away.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[strategic] reckless strike failed.", e);
      }
    }
  }

  /// <summary>
  /// The reckless round: +5 attack, +5 damage, and -5 AC (the averaged cost
  /// of the tabletop's "every hit on him is a critical threat").
  /// </summary>
  [TypeId(Guids.StrategicRecklessComponent)]
  internal class StrategicRecklessBuffLogic : UnitBuffComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonus>,
    IRulebookHandler<RuleCalculateAttackBonus>,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    ITargetRulebookHandler<RuleCalculateAC>, IRulebookHandler<RuleCalculateAC>,
    IInitiatorRulebookSubscriber, ITargetRulebookSubscriber, ISubscriber
  {
    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
    {
      evt.AddModifier(5, Fact, ModifierDescriptor.UntypedStackable);
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt)
    {
    }

    public void OnEventAboutToTrigger(RulePrepareDamage evt)
    {
      if (evt.Initiator != Owner)
      {
        return;
      }
      foreach (var damage in evt.DamageBundle)
      {
        damage.AddModifier(new Modifier(5, Fact, ModifierDescriptor.UntypedStackable));
      }
    }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
    }

    public void OnEventAboutToTrigger(RuleCalculateAC evt)
    {
      evt.AddModifier(-5, Fact, ModifierDescriptor.Penalty);
    }

    public void OnEventDidTrigger(RuleCalculateAC evt)
    {
    }
  }

  /// <summary>
  /// Knock Off-kilter's size restriction: not usable on creatures more than
  /// one size category larger than the soldier.
  /// </summary>
  [TypeId(Guids.StrategicKnockRestriction)]
  internal class StrategicKnockSizeRestriction : BlueprintComponent, IAbilityTargetRestriction
  {
    public bool IsTargetRestrictionPassed(UnitEntityData caster, TargetWrapper target)
    {
      try
      {
        var unit = target?.Unit;
        if (unit is null)
        {
          return true;
        }
        return (int)unit.State.Size <= (int)caster.State.Size + 1;
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[strategic] size check failed.", e);
        return true;
      }
    }

    public string GetAbilityTargetRestrictionUIText(UnitEntityData caster, TargetWrapper target)
    {
      return "Target is more than one size larger than the strategic soldier";
    }
  }

  /// <summary>
  /// Knock Off-kilter: the soldier strikes the target, and every ally
  /// threatening it immediately attacks it as well (the Act as One recipe).
  /// </summary>
  [TypeId(Guids.StrategicKnockAction)]
  internal class StrategicKnockAction : ContextAction
  {
    public override string GetCaption() => "Knock Off-kilter";

    public override void RunAction()
    {
      try
      {
        var caster = Context?.MaybeCaster;
        var target = Target?.Unit;
        if (caster is null || target is null || target.HPLeft <= 0)
        {
          return;
        }
        int attacks = 0;
        if (caster.DistanceTo(target) <= 10.Feet().Meters)
        {
          Game.Instance.CombatEngagementController.ForceAttackOfOpportunity(caster, target, false);
          attacks++;
        }
        foreach (var ally in SanguineFont.AlliesWithin(caster, 30))
        {
          if (ally != caster && !ally.Descriptor.State.IsDead &&
            ally.CombatState.EngagedUnits.Contains(target))
          {
            Game.Instance.CombatEngagementController.ForceAttackOfOpportunity(ally, target, false);
            attacks++;
          }
        }
        MissionFeats.Logger.Info($"[strategic] knock off-kilter: {attacks} strikes on {target.CharacterName}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[strategic] knock off-kilter failed.", e);
      }
    }
  }

  /// <summary>
  /// Punishing Strike: when an enemy damages an ally, the soldier makes an
  /// attack of opportunity against the attacker - once per round, only if he
  /// threatens the attacker and has attacks of opportunity left (which the
  /// strike consumes).
  /// </summary>
  [TypeId(Guids.StrategicPunishingComponent)]
  internal class StrategicPunishingStrike : UnitFactComponentDelegate<StrategicPunishingStrike.Data>,
    IGlobalRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    ISubscriber, IGlobalRulebookSubscriber
  {
    public void OnEventAboutToTrigger(RulePrepareDamage evt)
    {
    }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        var attacker = evt.Initiator;
        var victim = evt.Target;
        if (attacker is null || victim is null || victim == Owner ||
          !attacker.IsEnemy(Owner) || !victim.IsAlly(Owner))
        {
          return;
        }
        if (Data.LastUse + 1.Rounds().Seconds > Game.Instance.TimeController.GameTime)
        {
          return; // once per round
        }
        if (!Owner.CombatState.EngagedUnits.Contains(attacker) ||
          Owner.CombatState.AttackOfOpportunityCount <= 0)
        {
          return;
        }
        Owner.CombatState.AttackOfOpportunityCount -= 1;
        Game.Instance.CombatEngagementController.ForceAttackOfOpportunity(Owner, attacker, false);
        Data.LastUse = Game.Instance.TimeController.GameTime;
        MissionFeats.Logger.Info(
          $"[strategic] punishing strike: {Owner.CharacterName} answers for {victim.CharacterName}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[strategic] punishing strike failed.", e);
      }
    }

    public class Data
    {
      public TimeSpan LastUse;
    }
  }
}

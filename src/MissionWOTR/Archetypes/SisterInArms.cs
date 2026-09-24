using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.Configurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Sister-in-Arms (faithful port of the Adventurer's Guide cavalier
  /// archetype - the Gray Maiden commander; the faithful-port half of the
  /// cavalier pair, next to the homebrew Carousel).
  ///
  /// Tabletop (Adventurer's Guide):
  /// - Halfhearted Challenge (Ex) 1st: adds only HALF her cavalier level
  ///   (minimum 1) to damage rolls against her challenge target. Alters
  ///   challenge.
  /// - Maiden's Order (Ex) 1st: no order choice; she belongs to BOTH the
  ///   order of the dragon and the order of the lion and gains all benefits
  ///   of both at the appropriate levels. Replaces mount; alters order.
  /// - Devoted Defender (Ex) 3rd: gains Bodyguard as a bonus feat (no
  ///   prerequisites). Replaces cavalier's charge.
  /// - Maiden's Loyalty (Ex) 4th: +2 on Will saves against effects that
  ///   would compel her to attack or betray her allies, +1 per 4 levels
  ///   beyond 4th. Replaces expert trainer.
  /// - Dedicated Commander (Ex) 11th: lion's call or strategy order as a
  ///   move action; at 20th, as a swift or move action (and act as one).
  ///   Replaces mighty charge and supreme charge.
  ///
  /// WOTR adaptations (engine gaps):
  /// - Order of the Lion exists in vanilla WOTR and is granted whole via its
  ///   progression. Order of the Dragon does NOT exist in WOTR - it is ported
  ///   here as a custom order progression (challenge aura, class skills, Aid
  ///   Allies, Strategy, Act as One) built with the pplus Inquisition
  ///   custom-order recipe.
  /// - Dragon challenge benefit ("allies gain a circumstance bonus on melee
  ///   attacks against her challenge target while she threatens it") is an
  ///   aura: while her challenge is active, allies within 30 feet carry a
  ///   buff with AddAttackBonusAgainstFactOwner against the vanilla
  ///   challenge-target buff (the Constable order-benefit pattern).
  /// - Aid Another does not exist in WOTR: Aid Allies is adapted to a
  ///   challenge-gated ally aura (+ circumstance AC and saves while her
  ///   challenge is active; +2, rising to +3 at 8th and +4 at 14th).
  /// - Strategy/Act as One are adapted into standard-action abilities with a
  ///   shared 1/rest pool (the tabletop's once-per-combat and per-ally
  ///   choices are simplified); Act as One's "allies move and attack" is
  ///   realized as an immediate attack for adjacent allies plus a dodge-AC
  ///   aura (no engine API to move other units on command).
  /// - The Bodyguard feat does not exist in WOTR: Devoted Defender grants
  ///   the vanilla Divine Guardian's Bodyguard feature instead.
  /// - Expert Trainer could not be located in the WOTR cavalier progression:
  ///   its name is passed speculatively (warn-and-skip if absent), and
  ///   Maiden's Loyalty is additive in that case.
  /// - "Strategy order" and "act as one" (the order abilities named by
  ///   Dedicated Commander) map to the Dragon-order abilities ported above;
  ///   lion's call is the vanilla ability, cloned at move/swift action cost.
  ///
  /// Implementation notes: the half-strength challenge is applied as a
  /// negative untyped damage modifier whenever she damages a unit carrying
  /// the vanilla challenge-target buff (the Spellfist cascade zeroing
  /// pattern); the auras refresh on round ticks with short natural durations
  /// (the Sanguine Font aura idiom). Log prefix: [sister].
  /// </summary>
  internal static class SisterInArms
  {
    internal const string ArchetypeName = "SisterInArmsArchetype";
    internal const string DisplayName = "SisterInArms.Name";
    internal const string Description = "SisterInArms.Description";

    internal const string HalfheartedName = "SisterHalfheartedChallenge";
    internal const string MaidensOrderName = "SisterMaidensOrder";
    internal const string DragonSkillsName = "SisterDragonSkills";
    internal const string DragonChallengeName = "SisterDragonChallenge";
    internal const string AidAlliesName = "SisterAidAllies";
    internal const string StrategyName = "SisterDragonStrategy";
    internal const string ActAsOneName = "SisterActAsOne";
    internal const string DevotedDefenderName = "SisterDevotedDefender";
    internal const string LoyaltyName = "SisterMaidensLoyalty";
    internal const string CommanderName = "SisterDedicatedCommander";
    internal const string CommanderSwiftName = "SisterDedicatedCommanderSwift";

    public static void Configure()
    {
      var cavalier = CharacterClassRefs.CavalierClass.Reference.Get();

      var challengeSelf = BuffRefs.CavalierChallengeBuffSelf.Reference.Get();
      var challengeTarget = BuffRefs.CavalierChallengeBuffTarget.Reference.Get();

      // ----- Halfhearted Challenge (alters challenge) -----
      var halfhearted = FeatureConfigurator.New(HalfheartedName, Guids.SisterHalfhearted)
        .SetDisplayName("SisterHalfhearted.Name")
        .SetDescription("SisterHalfhearted.Description")
        .SetIcon(FeatureRefs.CavalierChallengeFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new SisterHalfheartedChallenge
        {
          CharacterClass = cavalier,
          ChallengeSelf = challengeSelf,
          ChallengeTarget = challengeTarget,
        })
        .Configure();

      // ----- Maiden's Order: the Dragon half (custom order progression) -----
      var dragonSkills = FeatureConfigurator.New(DragonSkillsName, Guids.SisterDragonSkills)
        .SetDisplayName("SisterDragonSkills.Name")
        .SetDescription("SisterDragonSkills.Description")
        .SetIcon(FeatureRefs.CavalierOrder.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new AddClassSkill { Skill = StatType.SkillPerception })
        .AddComponent(new AddClassSkill { Skill = StatType.SkillLoreNature })
        .Configure();

      var dragonAllyBuff = BuffConfigurator.New("SisterDragonAllyBuff", Guids.SisterDragonAllyBuff)
        .SetDisplayName("SisterDragonChallengeBuff.Name")
        .SetDescription("SisterDragonChallengeBuff.Description")
        .SetIcon(FeatureRefs.CavalierChallengeFeature.Reference.Get().Icon)
        .AddAttackBonusAgainstFactOwner(
          bonus: ContextValues.Rank(), checkedFact: challengeTarget,
          descriptor: ModifierDescriptor.Circumstance)
        .AddContextRankConfig(
          ContextRankConfigs.ClassLevel(new[] { CharacterClassRefs.CavalierClass.ToString() })
            .WithCustomProgression(
              (1, 1), (4, 2), (8, 3), (12, 4), (16, 5), (20, 6)))
        .Configure();

      var dragonChallenge = FeatureConfigurator.New(DragonChallengeName, Guids.SisterDragonChallenge)
        .SetDisplayName("SisterDragonChallenge.Name")
        .SetDescription("SisterDragonChallenge.Description")
        .SetIcon(FeatureRefs.CavalierChallengeFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new SisterDragonAura
        {
          ChallengeSelf = challengeSelf,
          AllyBuff = dragonAllyBuff,
        })
        .Configure();

      var aidBuff = BuffConfigurator.New("SisterAidBuff", Guids.SisterAidBuff)
        .SetDisplayName("SisterAidAlliesBuff.Name")
        .SetDescription("SisterAidAlliesBuff.Description")
        .SetIcon(FeatureRefs.CavalierBanner.Reference.Get().Icon)
        .AddContextStatBonus(StatType.AC, ContextValues.Rank(), ModifierDescriptor.Circumstance)
        .AddContextStatBonus(StatType.SaveFortitude, ContextValues.Rank(), ModifierDescriptor.Circumstance)
        .AddContextStatBonus(StatType.SaveReflex, ContextValues.Rank(), ModifierDescriptor.Circumstance)
        .AddContextStatBonus(StatType.SaveWill, ContextValues.Rank(), ModifierDescriptor.Circumstance)
        .AddContextRankConfig(
          ContextRankConfigs.ClassLevel(new[] { CharacterClassRefs.CavalierClass.ToString() })
            .WithCustomProgression((2, 2), (8, 3), (14, 4)))
        .Configure();

      var aidAllies = FeatureConfigurator.New(AidAlliesName, Guids.SisterAidAllies)
        .SetDisplayName("SisterAidAllies.Name")
        .SetDescription("SisterAidAllies.Description")
        .SetIcon(FeatureRefs.CavalierBanner.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new SisterDragonAura
        {
          ChallengeSelf = challengeSelf,
          AllyBuff = aidBuff,
        })
        .Configure();

      var strategyBuff = BuffConfigurator.New("SisterStrategyBuff", Guids.SisterStrategyBuff)
        .SetDisplayName("SisterStrategyBuff.Name")
        .SetDescription("SisterStrategyBuff.Description")
        .SetIcon(FeatureRefs.CavalierLionsCall.Reference.Get().Icon)
        .AddContextStatBonus(StatType.AC, ContextValues.Constant(2), ModifierDescriptor.Dodge)
        .AddContextStatBonus(
          StatType.AdditionalAttackBonus, ContextValues.Constant(2), ModifierDescriptor.Morale)
        .Configure();

      var strategyAction = ElementTool.Create<SisterStrategyAction>();
      strategyAction.Buff = strategyBuff;
      var strategyAbility = AbilityConfigurator.New(StrategyName, Guids.SisterStrategyAbility)
        .SetDisplayName("SisterStrategy.Name")
        .SetDescription("SisterStrategy.Description")
        .SetIcon(FeatureRefs.CavalierLionsCall.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard)
        .AllowTargeting(self: true)
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(strategyAction).Build())
        .Configure();

      var actAsOneBuff = BuffConfigurator.New("SisterActAsOneBuff", Guids.SisterActAsOneBuff)
        .SetDisplayName("SisterActAsOneBuff.Name")
        .SetDescription("SisterActAsOneBuff.Description")
        .SetIcon(FeatureRefs.CavalierLionsCall.Reference.Get().Icon)
        .AddContextStatBonus(StatType.AC, ContextValues.Constant(2), ModifierDescriptor.Dodge)
        .Configure();

      var actAsOneAction = ElementTool.Create<SisterActAsOneAction>();
      actAsOneAction.Buff = actAsOneBuff;
      var actAsOneAbility = AbilityConfigurator.New(ActAsOneName, Guids.SisterActAsOneAbility)
        .SetDisplayName("SisterActAsOne.Name")
        .SetDescription("SisterActAsOne.Description")
        .SetIcon(FeatureRefs.CavalierLionsCall.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard)
        .AllowTargeting(enemies: true)
        .SetEffectOnEnemy(AbilityEffectOnUnit.Harmful)
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(actAsOneAction).Build())
        .Configure();

      // The Dragon order progression (pplus Inquisition custom-order recipe).
      var dragonOrder = ProgressionConfigurator.New("SisterDragonOrder", Guids.SisterDragonProgression)
        .SetDisplayName("SisterDragonOrder.Name")
        .SetDescription("SisterDragonOrder.Description")
        .SetIcon(FeatureRefs.CavalierOrder.Reference.Get().Icon)
        .SetIsClassFeature(true)
        .SetClasses(cavalier)
        .SetGiveFeaturesForPreviousLevels(true)
        .AddToLevelEntry(1, DragonSkillsName, DragonChallengeName)
        .AddToLevelEntry(2, AidAlliesName)
        .AddToLevelEntry(8, StrategyName)
        .AddToLevelEntry(15, ActAsOneName)
        .Configure();

      // ----- Maiden's Order (replaces mount, alters order): both orders -----
      var maidensOrder = FeatureConfigurator.New(MaidensOrderName, Guids.SisterMaidensOrder)
        .SetDisplayName("SisterMaidensOrder.Name")
        .SetDescription("SisterMaidensOrder.Description")
        .SetIcon(FeatureRefs.CavalierOrder.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new()
        {
          ProgressionRefs.CavalierOrderOfTheLionProgression.Reference.Get(),
          dragonOrder,
        })
        .Configure();

      // ----- Devoted Defender (3rd): the vanilla Bodyguard-style feature -----
      var devotedDefender = FeatureConfigurator.New(DevotedDefenderName, Guids.SisterDevotedDefender)
        .SetDisplayName("SisterDevotedDefender.Name")
        .SetDescription("SisterDevotedDefender.Description")
        .SetIcon(FeatureRefs.CavalierCharge.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { FeatureRefs.DivineGuardianBodyguardFeature.Reference.Get() })
        .Configure();

      // ----- Maiden's Loyalty (4th): scaling Will save vs compulsion -----
      var loyalty = FeatureConfigurator.New(LoyaltyName, Guids.SisterLoyalty)
        .SetDisplayName("SisterLoyalty.Name")
        .SetDescription("SisterLoyalty.Description")
        .SetIcon(FeatureRefs.IronWill.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new SisterLoyaltySaves { CharacterClass = cavalier })
        .Configure();

      // ----- Dedicated Commander (11th/20th): faster lion's call -----
      var lionsCallMove = AbilityConfigurator.New("SisterLionsCallMove", Guids.SisterLionsCallMove)
        .CopyFrom(AbilityRefs.CavalierLionsCallAbility)
        .SetDisplayName("SisterLionsCallMove.Name")
        .SetDescription("SisterLionsCallMove.Description")
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move)
        .Configure();

      var lionsCallSwift = AbilityConfigurator.New("SisterLionsCallSwift", Guids.SisterLionsCallSwift)
        .CopyFrom(AbilityRefs.CavalierLionsCallAbility)
        .SetDisplayName("SisterLionsCallSwift.Name")
        .SetDescription("SisterLionsCallSwift.Description")
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
        .Configure();

      var commander = FeatureConfigurator.New(CommanderName, Guids.SisterCommander)
        .SetDisplayName("SisterCommander.Name")
        .SetDescription("SisterCommander.Description")
        .SetIcon(FeatureRefs.CavalierLionsCall.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { lionsCallMove, strategyAbility, actAsOneAbility })
        .Configure();

      var commanderSwift = FeatureConfigurator.New(CommanderSwiftName, Guids.SisterCommanderSwift)
        .SetDisplayName("SisterCommanderSwift.Name")
        .SetDescription("SisterCommanderSwift.Description")
        .SetIcon(FeatureRefs.CavalierLionsCall.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddPrerequisiteFeature(commander)
        .AddFacts(new() { lionsCallSwift })
        .Configure();

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.SisterArchetype, CharacterClassRefs.CavalierClass)
          .SetLocalizedName(DisplayName)
          .SetLocalizedDescription(Description);

      // Trades: mount + order choice (Maiden's Order), cavalier's charge
      // (Devoted Defender), expert trainer (Maiden's Loyalty - speculative
      // name, warn-and-skip if the progression lacks it), mighty charge +
      // supreme charge (Dedicated Commander).
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, cavalier,
        FeatureSelectionRefs.CavalierMountSelection.ToString(),
        FeatureSelectionRefs.CavalierOrderSelection.ToString(),
        FeatureRefs.CavalierCharge.ToString(),
        "CavalierExpertTrainer",
        "ExpertTrainer",
        FeatureRefs.CavalierMightyCharge.ToString(),
        FeatureRefs.CavalierSupremeCharge.ToString());

      archetype
        .AddToAddFeatures(LevelPlan.L(1), HalfheartedName, MaidensOrderName)
        .AddToAddFeatures(LevelPlan.L(3), DevotedDefenderName)
        .AddToAddFeatures(LevelPlan.L(4), LoyaltyName)
        .AddToAddFeatures(LevelPlan.L(11), CommanderName)
        .AddToAddFeatures(LevelPlan.L(20), CommanderSwiftName)
        .Configure();

      MissionFeats.Logger.Info("SisterInArms: configured.");
    }
  }

  /// <summary>
  /// Halfhearted Challenge: whenever the sister damages a unit carrying the
  /// vanilla challenge-target buff while her own challenge is active, the
  /// level-based challenge bonus is cut in half - implemented as a negative
  /// untyped modifier of ceil(level / 2) on every damage instance (the
  /// Spellfist cascade zeroing pattern).
  /// </summary>
  [TypeId(Guids.SisterHalfheartedComponent)]
  internal class SisterHalfheartedChallenge : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass CharacterClass;
    public BlueprintBuff ChallengeSelf;
    public BlueprintBuff ChallengeTarget;

    public void OnEventAboutToTrigger(RulePrepareDamage evt) { }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.Target is null)
        {
          return;
        }
        if (!Owner.HasFact(ChallengeSelf))
        {
          return;
        }
        if (evt.Target.Buffs.GetBuff(ChallengeTarget) is null)
        {
          return;
        }
        int level = Owner.Descriptor.Progression.GetClassLevel(CharacterClass);
        int halved = Math.Max(1, (level + 1) / 2);
        foreach (BaseDamage damage in evt.DamageBundle)
        {
          damage.AddModifier(new Modifier(-halved, Fact, ModifierDescriptor.UntypedStackable));
        }
        MissionFeats.Logger.Info(
          $"[sister] halfhearted challenge: {halved} withheld against {evt.Target.CharacterName}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("SisterInArms: halfhearted challenge failed.", e);
      }
    }
  }

  /// <summary>
  /// Dragon-order aura: while the sister's challenge is active (she carries
  /// the vanilla challenge self-buff), every ally within 30 feet carries the
  /// configured buff. Buffs are applied with short natural durations and
  /// refreshed each round, so they lapse on their own when the challenge ends
  /// (the Sanguine Font aura idiom).
  /// </summary>
  [TypeId(Guids.SisterDragonAuraComponent)]
  internal class SisterDragonAura : UnitFactComponentDelegate, ITickEachRound
  {
    public BlueprintBuff ChallengeSelf;
    public BlueprintBuff AllyBuff;

    public void OnNewRound()
    {
      try
      {
        if (AllyBuff is null || !Owner.HasFact(ChallengeSelf))
        {
          return;
        }
        foreach (var ally in SanguineFont.AlliesWithin(Owner, 30))
        {
          if (ally.Buffs.GetBuff(AllyBuff) is null)
          {
            // Two rounds of natural duration, refreshed each round tick: the
            // buff outlives the challenge by at most a round after it ends.
            ally.Descriptor.AddBuff(
              AllyBuff, Context, ContextDuration.Fixed(2).Calculate(Context).Seconds);
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("SisterInArms: dragon aura failed.", e);
      }
    }
  }

  /// <summary>
  /// Maiden's Loyalty: +2 on Will saves against compulsion and mind-affecting
  /// effects that would turn her against her allies, +1 per 4 levels beyond
  /// 4th (the SkilledRiderSave temporary-modifier pattern).
  /// </summary>
  [TypeId(Guids.SisterLoyaltyComponent)]
  internal class SisterLoyaltySaves : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSavingThrow>, IRulebookHandler<RuleSavingThrow>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass CharacterClass;

    public void OnEventAboutToTrigger(RuleSavingThrow evt)
    {
      try
      {
        if (evt.Initiator != Owner)
        {
          return;
        }
        var descriptor =
          evt.Reason.Context?.SourceAbility?.SpellDescriptor ??
          evt.Reason.Ability?.Blueprint?.SpellDescriptor ?? SpellDescriptor.None;
        if ((descriptor & (SpellDescriptor.Compulsion | SpellDescriptor.MindAffecting)) == 0)
        {
          return;
        }
        int level = Owner.Descriptor.Progression.GetClassLevel(CharacterClass);
        int bonus = 2 + Math.Max(0, (level - 4) / 4);
        evt.AddTemporaryModifier(
          evt.Initiator.Stats.SaveWill.AddModifier(bonus, Runtime, ModifierDescriptor.Circumstance));
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("SisterInArms: loyalty saves failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }
  }

  /// <summary>
  /// Dragon's Strategy: every ally within 30 feet (herself included) gains
  /// +2 dodge AC and +2 morale on attack rolls for 1 round.
  /// </summary>
  [TypeId(Guids.SisterStrategyComponent)]
  internal class SisterStrategyAction : Kingmaker.UnitLogic.Mechanics.Actions.ContextAction
  {
    public BlueprintBuff Buff;

    public override string GetCaption() => "Dragon's Strategy";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        if (caster is null || Buff is null)
        {
          return;
        }
        int applied = 0;
        foreach (var ally in SanguineFont.AlliesWithin(caster, 30))
        {
          ally.Descriptor.AddBuff(
            Buff, Context, ContextDuration.Fixed(1).Calculate(Context).Seconds);
          applied++;
        }
        MissionFeats.Logger.Info($"[sister] strategy applied to {applied} allies.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("SisterInArms: strategy failed.", e);
      }
    }
  }

  /// <summary>
  /// Act as One: every ally within 30 feet gains +2 dodge AC for 1 round, and
  /// each ally adjacent to the chosen enemy immediately attacks it (the
  /// combat-engagement ForceAttackOfOpportunity API - WOTR has no way to move
  /// other units on command).
  /// </summary>
  [TypeId(Guids.SisterActAsOneComponent)]
  internal class SisterActAsOneAction : Kingmaker.UnitLogic.Mechanics.Actions.ContextAction
  {
    public BlueprintBuff Buff;

    public override string GetCaption() => "Act as One";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        var target = Target.Unit;
        if (caster is null || target is null || target.HPLeft <= 0)
        {
          return;
        }
        int attacks = 0;
        foreach (var ally in SanguineFont.AlliesWithin(caster, 30))
        {
          ally.Descriptor.AddBuff(
            Buff, Context, ContextDuration.Fixed(1).Calculate(Context).Seconds);
          if (ally != caster && !ally.Descriptor.State.IsDead &&
            ally.DistanceTo(target) <= 7.Feet().Meters)
          {
            Kingmaker.Game.Instance.CombatEngagementController
              .ForceAttackOfOpportunity(ally, target, false);
            attacks++;
          }
        }
        MissionFeats.Logger.Info($"[sister] act as one: {attacks} allies strike.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("SisterInArms: act as one failed.", e);
      }
    }
  }
}

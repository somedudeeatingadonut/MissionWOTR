using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators;
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
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// THE GUIDE (tabletop port - Advanced Player's Guide pg. 125).
  ///
  /// "Many rangers are loners, but some choose to use their
  /// familiarity with the land to guide others safely through the
  /// wilderness. The guide forgoes a favored enemy to focus on the
  /// task or foe at hand, and can pass his knowledge and luck on to
  /// his charges."
  ///
  /// Coverage (0.27.0): vanilla WOTR ships SEVEN ranger archetypes -
  /// Flamewarden, Freebooter, Stormwalker, Demonslayer, Espionage
  /// Expert, Nomad, and Sable Company Marine (the user corrected an
  /// earlier four-name count: the first grep matched only names
  /// containing "ranger"). TTT/COP/DarkCodex add none. The Guide is
  /// unclaimed, real tabletop, and the thematic opposite of a
  /// companion-focused homebrew: the ranger who walks alone and
  /// gives his luck away.
  ///
  /// The kit (tabletop -> WOTR):
  /// - Ranger's Focus (1st, replaces FAVORED ENEMY at every rank):
  ///   a swift-action mark on one enemy in line of sight. +2 on
  ///   attack and damage rolls against the focus, +2 more at 5th and
  ///   every five levels (+2/+4/+6/+8/+10). 1/day, +1 use per three
  ///   levels after 1st (7/day at 19th). A new focus clears the old
  ///   one; the mark ends with the target's death (the tabletop's
  ///   "or surrenders" clause has no engine state - documented cut;
  ///   death is the exit that matters in the Worldwound).
  /// - Terrain Bond (4th, replaces Hunter's Bond): allies within 30
  ///   feet (adapted from "line of sight and hearing" - the WOTR
  ///   shout radius) gain +2 on initiative, Perception, Stealth and
  ///   Survival (WOTR has no Survival; Lore (Nature) is the
  ///   wilderness skill - documented adaptation). Documented cuts:
  ///   the favored-terrain condition itself (the engine exposes no
  ///   inspectable current-terrain check - the bond is always on)
  ///   and the leave-no-trail clause (WOTR has no tracking).
  /// - Ranger's Luck (9th, replaces Evasion): a swift action wraps
  ///   his next attack of the round in luck - if it misses, the die
  ///   rolls again (RuleRollD20.Reroll, the TTT Azata FavorableMagic
  ///   idiom; we only ever reroll MISSES, so take-best and
  ///   take-second-result are indistinguishable - documented, not
  ///   faked). 1/day, +1 at 14th and 19th. Documented cut: the
  ///   defensive mode (forcing an enemy's attack to reroll) needs an
  ///   interrupt-reaction the engine does not generically expose.
  /// - Improved Ranger's Luck (16th, replaces Improved Evasion): the
  ///   reroll carries a +4 luck bonus on the attack (a temporary
  ///   AdditionalAttackBonus modifier, the SisterLoyaltySaves
  ///   pattern), spent the moment it fires.
  /// - Inspired Moment (11th, replaces Quarry and Improved Quarry):
  ///   a free action for one round of clarity: +10 feet speed, +4 AC
  ///   and +4 on attack rolls, and every critical threat he scores
  ///   is automatically confirmed (evt.AutoCriticalConfirmation -
  ///   the TTT CritAutoconfirmAgainstClass idiom, minus the flank
  ///   condition). 1/day, +1 at 19th. Documented cuts: the extra
  ///   move/swift action (no generic extra-action API in the engine)
  ///   and the +4 on skill and ability checks (WOTR has no
  ///   all-skills stat).
  ///
  /// Master Hunter (20th) is NOT replaced - the Guide keeps it.
  /// Log prefix: [guide].
  /// </summary>
  internal static class Guide
  {
    internal const string ArchetypeName = "GuideArchetype";

    public static void Configure()
    {
      var ranger = CharacterClassRefs.RangerClass.Reference.Get();
      var rangerClass = CharacterClassRefs.RangerClass.ToString();

      // ----- The mark -----
      var markBuff = BuffConfigurator.New(
        "GuideFocusMarkBuff", Guids.GuideFocusMarkBuff)
        .SetDisplayName("GuideFocusMarkBuff.Name")
        .SetDescription("GuideFocusMarkBuff.Description")
        .SetIcon(AbilityRefs.QuarryAbility.Reference.Get().Icon)
        .AddComponent(new GuideFocusMark())
        .Configure();
      markBuff.GetComponent<GuideFocusMark>().MarkBuff = markBuff;

      // ----- Ranger's Focus: uses = 1 + 1 per 3 levels from 4th -----
      var focusResource = AbilityResourceConfigurator.New(
        "GuideFocusResource", Guids.GuideFocusResource)
        .SetMaxAmount(
          ResourceAmountBuilder.New(1)
            .IncreaseByLevelStartPlusDivStep(
              classes: new[] { rangerClass },
              startingLevel: 4, startingBonus: 1,
              levelsPerStep: 3, bonusPerStep: 1))
        .Configure();

      var focusAbility = AbilityConfigurator.New(
        "GuideFocusAbility", Guids.GuideFocusAbility)
        .SetDisplayName("GuideFocus.Name")
        .SetDescription("GuideFocusAbility.Description")
        .SetIcon(AbilityRefs.QuarryAbility.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(UnitCommand.CommandType.Swift)
        .SetCanTargetEnemies()
        .AddAbilityResourceLogic(requiredResource: focusResource, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(
          // A 60-minute mark rather than a permanent one: our bpcore
          // build has no ApplyBuffPermanentFixed, and an hour covers
          // any fight - a fresh focus re-marks anyway (the old mark
          // is cleared by the swap logic on application).
          ActionsBuilder.New().ApplyBuff(
            markBuff, ContextDuration.Fixed(60, DurationRate.Minutes)))
        .Configure();

      var focusFeature = FeatureConfigurator.New(
        "GuideFocusFeature", Guids.GuideFocusFeature)
        .SetDisplayName("GuideFocus.Name")
        .SetDescription("GuideFocus.Description")
        .SetIcon(AbilityRefs.QuarryAbility.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { "GuideFocusAbility" })
        .AddAbilityResources(resource: focusResource, restoreAmount: true)
        .AddComponent(new GuideFocusBonus { MarkBuff = markBuff, Class = ranger })
        .Configure();
      focusFeature.GetComponent<GuideFocusBonus>().MarkBuff = markBuff;

      // ----- Terrain Bond: the standing camp aura -----
      var terrainBuff = BuffConfigurator.New(
        "GuideTerrainBondBuff", Guids.GuideTerrainBondBuff)
        .SetDisplayName("GuideTerrainBondBuff.Name")
        .SetDescription("GuideTerrainBondBuff.Description")
        .SetIcon(AbilityRefs.Longstrider.Reference.Get().Icon)
        .AddStatBonus(stat: StatType.Initiative, value: 2, descriptor: ModifierDescriptor.UntypedStackable)
        .AddStatBonus(stat: StatType.SkillPerception, value: 2, descriptor: ModifierDescriptor.UntypedStackable)
        .AddStatBonus(stat: StatType.SkillStealth, value: 2, descriptor: ModifierDescriptor.UntypedStackable)
        .AddStatBonus(stat: StatType.SkillLoreNature, value: 2, descriptor: ModifierDescriptor.UntypedStackable)
        .Configure();

      var terrainFeature = FeatureConfigurator.New(
        "GuideTerrainBondFeature", Guids.GuideTerrainBondFeature)
        .SetDisplayName("GuideTerrainBond.Name")
        .SetDescription("GuideTerrainBond.Description")
        .SetIcon(AbilityRefs.Longstrider.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new GuideTerrainAura { AuraBuff = terrainBuff, RadiusFeet = 30 })
        .Configure();

      // ----- Ranger's Luck: uses = 1 at 9th, +1 at 14th and 19th -----
      var luckResource = AbilityResourceConfigurator.New(
        "GuideLuckResource", Guids.GuideLuckResource)
        .SetMaxAmount(
          ResourceAmountBuilder.New(0)
            .IncreaseByLevelStartPlusDivStep(
              classes: new[] { rangerClass },
              startingLevel: 9, startingBonus: 1,
              levelsPerStep: 5, bonusPerStep: 1))
        .Configure();

      var luckBuff = BuffConfigurator.New(
        "GuideLuckBuff", Guids.GuideLuckBuff)
        .SetDisplayName("GuideLuckBuff.Name")
        .SetDescription("GuideLuckBuff.Description")
        .SetIcon(AbilityRefs.Foresight.Reference.Get().Icon)
        .AddComponent(new GuideLuckReroll())
        .Configure();

      var luckAbility = AbilityConfigurator.New(
        "GuideLuckAbility", Guids.GuideLuckAbility)
        .SetDisplayName("GuideLuck.Name")
        .SetDescription("GuideLuckAbility.Description")
        .SetIcon(AbilityRefs.Foresight.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Swift)
        .SetCanTargetSelf()
        .AddAbilityResourceLogic(requiredResource: luckResource, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().ApplyBuff(luckBuff, ContextDuration.Fixed(1), toCaster: true))
        .Configure();

      var luckFeature = FeatureConfigurator.New(
        "GuideLuckFeature", Guids.GuideLuckFeature)
        .SetDisplayName("GuideLuck.Name")
        .SetDescription("GuideLuck.Description")
        .SetIcon(AbilityRefs.Foresight.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { "GuideLuckAbility" })
        .AddAbilityResources(resource: luckResource, restoreAmount: true)
        .Configure();

      // ----- Improved Ranger's Luck (16th): the +4 upgrade -----
      var improvedLuck = FeatureConfigurator.New(
        "GuideImprovedLuckFeature", Guids.GuideImprovedLuckFeature)
        .SetDisplayName("GuideImprovedLuck.Name")
        .SetDescription("GuideImprovedLuck.Description")
        .SetIcon(AbilityRefs.Foresight.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();
      luckBuff.GetComponent<GuideLuckReroll>().ImprovedFeature = improvedLuck;
      luckBuff.GetComponent<GuideLuckReroll>().SelfBuff = luckBuff;

      // ----- Inspired Moment: uses = 1 at 11th, +1 at 19th -----
      var inspiredResource = AbilityResourceConfigurator.New(
        "GuideInspiredResource", Guids.GuideInspiredResource)
        .SetMaxAmount(
          ResourceAmountBuilder.New(0)
            .IncreaseByLevelStartPlusDivStep(
              classes: new[] { rangerClass },
              startingLevel: 11, startingBonus: 1,
              levelsPerStep: 8, bonusPerStep: 1))
        .Configure();

      var inspiredBuff = BuffConfigurator.New(
        "GuideInspiredBuff", Guids.GuideInspiredBuff)
        .SetDisplayName("GuideInspiredBuff.Name")
        .SetDescription("GuideInspiredBuff.Description")
        .SetIcon(AbilityRefs.Haste.Reference.Get().Icon)
        .AddStatBonus(stat: StatType.Speed, value: 10, descriptor: ModifierDescriptor.Enhancement)
        .AddStatBonus(stat: StatType.AC, value: 4, descriptor: ModifierDescriptor.UntypedStackable)
        .AddStatBonus(stat: StatType.AdditionalAttackBonus, value: 4, descriptor: ModifierDescriptor.UntypedStackable)
        .AddComponent(new GuideInspiredCrit())
        .Configure();

      var inspiredAbility = AbilityConfigurator.New(
        "GuideInspiredAbility", Guids.GuideInspiredAbility)
        .SetDisplayName("GuideInspired.Name")
        .SetDescription("GuideInspiredAbility.Description")
        .SetIcon(AbilityRefs.Haste.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Free)
        .SetCanTargetSelf()
        .AddAbilityResourceLogic(requiredResource: inspiredResource, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().ApplyBuff(inspiredBuff, ContextDuration.Fixed(1), toCaster: true))
        .Configure();

      var inspiredFeature = FeatureConfigurator.New(
        "GuideInspiredFeature", Guids.GuideInspiredFeature)
        .SetDisplayName("GuideInspired.Name")
        .SetDescription("GuideInspired.Description")
        .SetIcon(AbilityRefs.Haste.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { "GuideInspiredAbility" })
        .AddAbilityResources(resource: inspiredResource, restoreAmount: true)
        .Configure();

      // ----- The archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.GuideArchetype, CharacterClassRefs.RangerClass)
          .SetLocalizedName("Guide.Name")
          .SetLocalizedDescription("Guide.Description")
          .AddToAddFeatures(LevelPlan.L(1), focusFeature)
          .AddToAddFeatures(LevelPlan.L(4), terrainFeature)
          .AddToAddFeatures(LevelPlan.L(9), luckFeature)
          .AddToAddFeatures(LevelPlan.L(11), inspiredFeature)
          .AddToAddFeatures(LevelPlan.L(16), improvedLuck);

      // The trades. Favored enemy is granted at FIVE levels (1, 5,
      // 10, 15, 20) - removing only the first grant would leave the
      // later ranks alive, so this one goes through the every-level
      // helper. Hunter's Bond, Evasion, Quarry, Improved Evasion and
      // Improved Quarry each come once (both Improved Evasion ref
      // spellings are passed; whichever the progression carries is
      // removed and the other skips with a warning).
      archetype = ArchetypeRemovals.AddRemovalsAtAllLevels(
        archetype, ranger,
        FeatureSelectionRefs.FavoriteEnemySelection.ToString());
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, ranger,
        FeatureSelectionRefs.HuntersBondSelection.ToString(),
        FeatureRefs.Evasion.ToString(),
        FeatureRefs.Quarry.ToString(),
        FeatureRefs.ImprovedEvasion.ToString(),
        FeatureRefs.ImprovedEvasion_0.ToString(),
        FeatureRefs.ImprovedQuarry.ToString());

      archetype.Configure();
      MissionFeats.Logger.Info("[guide] configured: " + ArchetypeName + ".");
    }
  }

  /// <summary>
  /// The mark itself: when a new focus is declared, every older mark
  /// laid by the SAME ranger is cleared (the tabletop's "until the
  /// ranger designates a new focus, whichever occurs first").
  /// </summary>
  [TypeId(Guids.GuideFocusMarkComponent)]
  internal class GuideFocusMark : UnitFactComponentDelegate
  {
    public BlueprintBuff MarkBuff;

    protected override void OnActivate()
    {
      try
      {
        var caster = Fact.MaybeContext?.MaybeCaster;
        if (caster is null || MarkBuff is null)
        {
          return;
        }
        foreach (var unit in Game.Instance.State.Units)
        {
          if (unit == Owner)
          {
            continue;
          }
          var existing = unit.Buffs.GetBuff(MarkBuff);
          if (existing is not null && existing.MaybeContext?.MaybeCaster == caster)
          {
            unit.Buffs.RemoveFact(MarkBuff);
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[guide] focus mark swap failed.", e);
      }
    }
  }

  /// <summary>
  /// Ranger's Focus, the ranger side: +2 on attack rolls (a temporary
  /// AdditionalAttackBonus modifier - the FoolFearless pattern) and
  /// the same bonus as flat damage (DirectDamage with DiceFormula
  /// .Zero, the TTT DamageRetribution idiom) against the marked
  /// target - scaling +2 at 1st and every five levels (+2/+4/+6/
  /// +8/+10). Only the ranger's OWN mark counts.
  /// </summary>
  [TypeId(Guids.GuideFocusBonusComponent)]
  internal class GuideFocusBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintBuff MarkBuff;
    public BlueprintCharacterClass Class;

    private bool IsFocus(UnitEntityData target)
    {
      var mark = target.Buffs.GetBuff(MarkBuff);
      return mark is not null && mark.MaybeContext?.MaybeCaster == Owner;
    }

    private int Bonus =>
      2 + 2 * (Owner.Progression.GetClassLevel(Class) / 5);

    public void OnEventAboutToTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.IsFake)
        {
          return;
        }
        if (!IsFocus(evt.Target))
        {
          return;
        }
        evt.AddTemporaryModifier(evt.Initiator.Stats.AdditionalAttackBonus
          .AddModifier(Bonus, Runtime, ModifierDescriptor.Competence));
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[guide] focus attack bonus failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }

    public void OnEventAboutToTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner)
        {
          return;
        }
        var roll = evt.ParentRule?.AttackRoll;
        if (roll is null || !IsFocus(roll.Target))
        {
          return;
        }
        evt.Add(new DirectDamage(DiceFormula.Zero, Bonus) { SourceFact = Fact });
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[guide] focus damage bonus failed.", e);
      }
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
  }

  /// <summary>
  /// Terrain Bond: the standing camp aura - every round, the ranger
  /// and every ally within 30 feet carry the terrain bond buff (two
  /// rounds of natural duration, refreshed on the tick - the
  /// SisterDragonAura idiom, the FoolChaosAura shape with a radius).
  /// </summary>
  [TypeId(Guids.GuideTerrainAuraComponent)]
  internal class GuideTerrainAura : UnitFactComponentDelegate,
    Kingmaker.Controllers.Units.ITickEachRound
  {
    public BlueprintBuff AuraBuff;
    public int RadiusFeet = 30;

    public void OnNewRound()
    {
      try
      {
        Spread();
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[guide] terrain aura tick failed.", e);
      }
    }

    protected override void OnActivate()
    {
      Spread();
    }

    private void Spread()
    {
      if (AuraBuff is null)
      {
        return;
      }
      Apply(Owner);
      foreach (var ally in SanguineFont.AlliesWithin(Owner, RadiusFeet))
      {
        Apply(ally);
      }
    }

    private void Apply(UnitEntityData unit)
    {
      if (unit is null || unit.Descriptor.State.IsDead ||
        unit.Buffs.GetBuff(AuraBuff) is not null)
      {
        return;
      }
      unit.Descriptor.AddBuff(AuraBuff, Fact.MaybeContext, new Rounds(2).Seconds);
    }
  }

  /// <summary>
  /// Ranger's Luck, the rider: while the luck holds, his next missed
  /// attack rolls again (RuleRollD20.Reroll - the TTT Azata
  /// FavorableMagic idiom; only misses are rerolled, so take-best and
  /// take-second are indistinguishable). With Improved Ranger's Luck
  /// the reroll carries a +4 luck bonus. The buff spends itself the
  /// moment it fires - and is removed BEFORE the reroll, so a
  /// synchronous re-trigger cannot recurse.
  /// </summary>
  [TypeId(Guids.GuideLuckRerollComponent)]
  internal class GuideLuckReroll : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintFeature ImprovedFeature;
    public BlueprintBuff SelfBuff;

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    public void OnEventDidTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.IsFake || evt.IsHit)
        {
          return;
        }
        // Spend first: no recursion, no second reroll.
        if (SelfBuff is not null)
        {
          Owner.Buffs.RemoveFact(SelfBuff);
        }
        if (ImprovedFeature is not null && Owner.HasFact(ImprovedFeature))
        {
          evt.AddTemporaryModifier(evt.Initiator.Stats.AdditionalAttackBonus
            .AddModifier(4, Runtime, ModifierDescriptor.Luck));
        }
        evt.D20.Reroll(Fact, true);
        CombatLog.Write("The guide refuses the miss - the die rolls again.", Owner);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[guide] luck reroll failed.", e);
      }
    }
  }

  /// <summary>
  /// Inspired Moment's clarity: while it lasts, every critical threat
  /// he scores is automatically confirmed
  /// (evt.AutoCriticalConfirmation - the TTT CritAutoconfirmAgainst
  /// Class idiom, minus the flanking condition).
  /// </summary>
  [TypeId(Guids.GuideInspiredCritComponent)]
  internal class GuideInspiredCrit : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public void OnEventAboutToTrigger(RuleAttackRoll evt)
    {
      if (evt.Initiator == Owner)
      {
        evt.AutoCriticalConfirmation = true;
      }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
  }
}

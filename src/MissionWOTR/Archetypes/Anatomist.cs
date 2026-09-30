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
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// THE ANATOMIST (rogue homebrew - the user's design, 0.31.0: a
  /// third rogue archetype "focused on finding the weak point of an
  /// enemy").
  ///
  /// The user's three mechanics, verbatim-first (0.32.0 rebalance:
  /// threshold scaling, lower SR, Risky Maneuver, no Vital Reading):
  /// - WEAK POINT (1st, replaces trapfinding): "if an attack misses
  ///   while needing to get d16 or above to hit, the rogue gets 1+
  ///   to attack, stackable." Implementation: the needed natural
  ///   roll is read straight off the attack event
  ///   (TargetAC - AttackBonus, the dcx PanacheDodge formula
  ///   attack.Roll + attack.AttackBonus >= attack.TargetAC); a miss
  ///   that needed the threshold or higher finds a SEAM - a
  ///   stacking +1 attack bonus against that enemy (a ranked
  ///   per-enemy mark, the Wildbond mark idiom).
  ///   THE THRESHOLD SCALES (0.32.0, the user's fix): 16 at 1st
  ///   level, falling by 1 every three levels, to 10 from 18th
  ///   (Math.Max(10, 16 - level/3)). The user's insight: a fixed
  ///   16 makes raising her attack a CON - the better she hits, the
  ///   rarer 16+ misses become, and Perfect Strike's five seams go
  ///   unreachable against anything she can hit without a natural
  ///   20. The falling threshold keeps study in pace with her
  ///   attack bonus.
  /// - RISKY MANEUVER (4th, the user's design, 0.32.0): a swift
  ///   action - she gives herself -6 AC for a round to find an
  ///   opening: her next attack this round applies a seam to its
  ///   target REGARDLESS of the roll - hit or miss, no threshold.
  ///   The guaranteed answer to the attack-investment con: the
  ///   dice can no longer starve the study (and if she never
  ///   attacks, the -6 AC was simply the price of hesitation).
  /// - STUDENT OF DEFENSES (8th), both halves of the user's brief:
  ///   attacking a SPELLCASTER grants her SPELL RESISTANCE (4 +
  ///   rogue level, 0.32.0 - the user lowered it from 11 + level;
  ///   the vanilla AddSpellResistance component with a class-level
  ///   rank config - WithLinearProgression(1, 4)) for one round,
  ///   renewed by every attack she makes on a caster (Spellbooks
  ///   detect them - the TTT OppositionResearch enumeration); and
  ///   attacking an enemy with DAMAGE REDUCTION grants additional
  ///   damage, +1 per five rogue levels, NOT stackable (a flat
  ///   rider - the DR read via the vanilla UnitPartDamageReduction,
  ///   whose existence IS the has-DR check: created on the first
  ///   reduction, removed when the last goes; the chunk list is
  ///   not public - documented edge: DR overhauled by other mods
  ///   may go unread).
  ///
  /// My additions ("as well as some other effects you can think
  /// of"), the study deepening:
  /// - READ THE TELL (12th, 0.32.0 - replaces Learn the Seams,
  ///   removed per the user): against enemies carrying her seams
  ///   she reads the telegraphs - a +2 dodge bonus to AC against
  ///   every one of them. A defensive study rather than another
  ///   damage rider.
  /// - PERFECT STRIKE (16th): once per round, a hit against an
  ///   enemy carrying five or more seams is an automatic critical
  ///   (AutoCriticalThreat + AutoCriticalConfirmation, both set
  ///   pre-resolution and only when the roll already shows a hit -
  ///   the PanacheDodge roll-read idiom; a spent strike on a miss
  ///   is impossible). The five seams are REACHABLE now: the
  ///   falling threshold and Risky Maneuver both feed the stack.
  /// - 20th carries no anatomist feature (Vital Reading removed per
  ///   the user) - the rogue's own Master Strike remains the
  ///   capstone.
  ///
  /// The trades: trapfinding (1st) and danger sense (every rank,
  /// 3rd-18th) - the same skill-side price as the Steel Rain, so
  /// the two are alternatives, not companions (each still stacks
  /// with the Scout's uncanny-dodge trades). Sneak attack, evasion,
  /// uncanny dodge, talents, debilitating injuries and master
  /// strike are untouched: she studies, she does not need them
  /// changed.
  /// Log prefix: [anatomist].
  /// </summary>
  internal static class Anatomist
  {
    internal const string ArchetypeName = "AnatomistArchetype";

    public static void Configure()
    {
      var rogue = CharacterClassRefs.RogueClass.Reference.Get();

      // ----- The seam mark (a ranked per-enemy buff) -----
      var seams = BuffConfigurator.New("AnatomistSeamsMarkBuff", Guids.AnatomistSeamsMarkBuff)
        .SetDisplayName("AnatomistSeamsMarkBuff.Name")
        .SetDescription("AnatomistSeamsMarkBuff.Description")
        .SetIcon(AbilityRefs.SeeInvisibility.Reference.Get().Icon)
        .Configure();

      // ----- The spell-resistance ward (8th, on the caster-strike) -----
      var ward = BuffConfigurator.New("AnatomistWardBuff", Guids.AnatomistWardBuff)
        .SetDisplayName("AnatomistWardBuff.Name")
        .SetDescription("AnatomistWardBuff.Description")
        .SetIcon(AbilityRefs.MindBlank.Reference.Get().Icon)
        .AddSpellResistance(value: ContextValues.Rank())
        .AddContextRankConfig(
          ContextRankConfigs.ClassLevel(
            new[] { CharacterClassRefs.RogueClass.ToString() })
            .WithLinearProgression(1, 4))
        .Configure();

      // ----- Weak Point (1st, replaces trapfinding) -----
      var weakPoint = FeatureConfigurator.New(
        "AnatomistWeakPointFeature", Guids.AnatomistWeakPointFeature)
        .SetDisplayName("AnatomistWeakPoint.Name")
        .SetDescription("AnatomistWeakPoint.Description")
        .SetIcon(AbilityRefs.SeeInvisibility.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new AnatomistWeakPoint { Seams = seams, RogueClass = rogue })
        .Configure();

      // ----- Student of Defenses (8th) -----
      var defenses = FeatureConfigurator.New(
        "AnatomistDefensesFeature", Guids.AnatomistDefensesFeature)
        .SetDisplayName("AnatomistStudentOfDefenses.Name")
        .SetDescription("AnatomistStudentOfDefenses.Description")
        .SetIcon(AbilityRefs.ProtectionFromEnergy.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new AnatomistDefenses
        {
          RogueClass = rogue,
          Ward = ward,
        })
        .Configure();

      // ----- Risky Maneuver (4th, the user's design) -----
      var maneuverBuff = BuffConfigurator.New(
        "AnatomistRiskyManeuverBuff", Guids.AnatomistRiskyManeuverBuff)
        .SetDisplayName("AnatomistRiskyManeuverBuff.Name")
        .SetDescription("AnatomistRiskyManeuverBuff.Description")
        .SetIcon(AbilityRefs.LeadBlades.Reference.Get().Icon)
        .AddStatBonus(stat: StatType.AC, value: -6,
          descriptor: ModifierDescriptor.UntypedStackable)
        .AddComponent(new AnatomistRiskyManeuver())
        .Configure();
      maneuverBuff.GetComponent<AnatomistRiskyManeuver>().Seams = seams;
      maneuverBuff.GetComponent<AnatomistRiskyManeuver>().SelfBuff = maneuverBuff;

      var maneuver = AbilityConfigurator.New(
        "AnatomistRiskyManeuverAbility", Guids.AnatomistRiskyManeuverAbility)
        .SetDisplayName("AnatomistRiskyManeuver.Name")
        .SetDescription("AnatomistRiskyManeuver.Description")
        .SetIcon(AbilityRefs.LeadBlades.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Swift)
        .SetCanTargetSelf()
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().ApplyBuff(maneuverBuff, ContextDuration.Fixed(1), toCaster: true))
        .Configure();

      var riskyManeuver = FeatureConfigurator.New(
        "AnatomistRiskyManeuverFeature", Guids.AnatomistRiskyManeuverFeature)
        .SetDisplayName("AnatomistRiskyManeuver.Name")
        .SetDescription("AnatomistRiskyManeuver.Description")
        .SetIcon(AbilityRefs.LeadBlades.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { "AnatomistRiskyManeuverAbility" })
        .Configure();

      // ----- Read the Tell (12th, replaces Learn the Seams) -----
      var readTheTell = FeatureConfigurator.New(
        "AnatomistReadTheTellFeature", Guids.AnatomistReadTheTellFeature)
        .SetDisplayName("AnatomistReadTheTell.Name")
        .SetDescription("AnatomistReadTheTell.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new AnatomistReadTheTell { Seams = seams })
        .Configure();

      // ----- Perfect Strike (16th) -----
      var perfectStrike = FeatureConfigurator.New(
        "AnatomistPerfectStrikeFeature", Guids.AnatomistPerfectStrikeFeature)
        .SetDisplayName("AnatomistPerfectStrike.Name")
        .SetDescription("AnatomistPerfectStrike.Description")
        .SetIcon(AbilityRefs.QuarryAbility.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new AnatomistPerfectStrike { Seams = seams })
        .Configure();

      // ----- The archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.AnatomistArchetype, CharacterClassRefs.RogueClass)
          .SetLocalizedName("Anatomist.Name")
          .SetLocalizedDescription("Anatomist.Description")
          .AddToAddFeatures(LevelPlan.L(1), weakPoint)
          .AddToAddFeatures(LevelPlan.L(4), riskyManeuver)
          .AddToAddFeatures(LevelPlan.L(8), defenses)
          .AddToAddFeatures(LevelPlan.L(12), readTheTell)
          .AddToAddFeatures(LevelPlan.L(16), perfectStrike);

      archetype = ArchetypeRemovals.AddRemovals(
        archetype, rogue, FeatureRefs.Trapfinding.ToString());
      archetype = ArchetypeRemovals.AddRemovalsAtAllLevels(
        archetype, rogue, FeatureRefs.DangerSenseRogue.ToString());

      archetype.Configure();
      MissionFeats.Logger.Info("[anatomist] configured: " + ArchetypeName + ".");
    }
  }

  /// <summary>
  /// Weak Point: every attack against a seamed enemy carries the
  /// stacks (+1 attack each), and every MISS that needed a natural
  /// 16+ finds a new seam (the needed roll is read straight off the
  /// event: TargetAC - AttackBonus - the dcx PanacheDodge formula).
  /// Self-limiting by design: the stacks lower the needed roll
  /// until misses stop qualifying.
  /// </summary>
  [TypeId(Guids.AnatomistWeakPointComponent)]
  internal class AnatomistWeakPoint : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintBuff Seams;
    public BlueprintCharacterClass RogueClass;

    private int StacksAgainst(UnitEntityData target)
    {
      var mark = target?.Buffs.GetBuff(Seams);
      return mark is not null && mark.MaybeContext?.MaybeCaster == Owner
        ? mark.GetRank()
        : 0;
    }

    public void OnEventAboutToTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.IsFake)
        {
          return;
        }
        int stacks = StacksAgainst(evt.Target);
        if (stacks > 0)
        {
          evt.AddTemporaryModifier(evt.Initiator.Stats.AdditionalAttackBonus
            .AddModifier(stacks, Runtime, ModifierDescriptor.Insight));
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[anatomist] weak point bonus failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.IsFake || evt.IsHit)
        {
          return;
        }
        // What natural roll did she need? The threshold falls as
        // she levels (16 at 1st, -1 every three levels, 10 from
        // 18th - the user's 0.32.0 fix: a fixed 16 makes attack
        // investment a con, and five seams unreachable).
        int needed = evt.TargetAC - evt.AttackBonus;
        int level = Owner.Progression.GetClassLevel(RogueClass);
        int threshold = Math.Max(10, 16 - level / 3);
        if (needed < threshold)
        {
          return;
        }
        int current = StacksAgainst(evt.Target);
        Wildbond.ApplyMark(evt.Target, Seams, Fact.MaybeContext, 600, current + 1);
        if (current == 0)
        {
          CombatLog.Write("A seam in the armor - she will remember it.", Owner);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[anatomist] seam-finding failed.", e);
      }
    }
  }

  /// <summary>
  /// Student of Defenses (8th): attacking a spellcaster wreathes
  /// her in spell resistance (11 + rogue level, one round, renewed
  /// by every attack on a caster - Spellbooks detect them); and her
  /// damage against an enemy with damage reduction carries +1 per
  /// five rogue levels (not stackable - a flat rider). The DR read
  /// is the vanilla UnitPartDamageReduction's chunks (the dcx
  /// AddDamageResistancePhysicalImproved idiom).
  /// </summary>
  [TypeId(Guids.AnatomistDefensesComponent)]
  internal class AnatomistDefenses : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass RogueClass;
    public BlueprintBuff Ward;

    public void OnEventAboutToTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.IsFake)
        {
          return;
        }
        // Attacking a spellcaster: the ward rises for a round.
        if (evt.Target.Spellbooks.Any())
        {
          Owner.Descriptor.AddBuff(Ward, Fact.MaybeContext, new Rounds(1).Seconds);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[anatomist] ward failed.", e);
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
        if (roll is null)
        {
          return;
        }
        // Attacking an armored thing: read the reduction, press
        // past it. +1 per five levels, not stackable. The DR read:
        // the vanilla part is created on the first reduction and
        // removed when the last one goes - its EXISTENCE is the
        // has-DR check (the chunk list dcx reads is not public).
        // Documented edge: DR overhauled by other mods (TTT's
        // replacement part) may go unread - the bonus simply does
        // not fire.
        if (roll.Target.Get<UnitPartDamageReduction>() is null)
        {
          return;
        }
        int level = Owner.Progression.GetClassLevel(RogueClass);
        int bonus = Math.Max(1, level / 5);
        evt.Add(new DirectDamage(DiceFormula.Zero, bonus) { SourceFact = Fact });
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[anatomist] armor reading failed.", e);
      }
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
  }

  /// <summary>
  /// Perfect Strike (16th): once per round, a hit against an enemy
  /// carrying five or more seams is an automatic critical. The
  /// flags are set pre-resolution and ONLY when the roll already
  /// shows a hit (the PanacheDodge roll-read), so a spent strike on
  /// a miss is impossible.
  /// </summary>
  [TypeId(Guids.AnatomistPerfectStrikeComponent)]
  internal class AnatomistPerfectStrike : UnitFactComponentDelegate,
    Kingmaker.Controllers.Units.ITickEachRound,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintBuff Seams;

    private bool spent;

    public void OnNewRound()
    {
      spent = false;
    }

    protected override void OnActivate()
    {
      spent = false;
    }

    public void OnEventAboutToTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.IsFake || spent)
        {
          return;
        }
        var mark = evt.Target.Buffs.GetBuff(Seams);
        int stacks = mark is not null && mark.MaybeContext?.MaybeCaster == Owner
          ? mark.GetRank()
          : 0;
        if (stacks < 5)
        {
          return;
        }
        // Only when this roll is already a hit - the strike is
        // never wasted on a miss (the PanacheDodge formula).
        if (evt.Roll + evt.AttackBonus < evt.TargetAC)
        {
          return;
        }
        evt.AutoCriticalThreat = true;
        evt.AutoCriticalConfirmation = true;
        spent = true;
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[anatomist] perfect strike failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
  }

  /// <summary>
  /// Risky Maneuver's rider, on the -6 AC buff: her next attack
  /// this round applies a seam to its target REGARDLESS of the
  /// roll - hit or miss, no threshold (the user's 0.32.0 design:
  /// "Give yourself -6 ac to find an opening in your opponents
  /// defenses, giving them a seam"). The buff spends itself when
  /// the opening is found.
  /// </summary>
  [TypeId(Guids.AnatomistRiskyManeuverComponent)]
  internal class AnatomistRiskyManeuver : UnitBuffComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintBuff Seams;
    public BlueprintBuff SelfBuff;

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    public void OnEventDidTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.IsFake)
        {
          return;
        }
        var mark = evt.Target.Buffs.GetBuff(Seams);
        int current = mark is not null && mark.MaybeContext?.MaybeCaster == Owner
          ? mark.GetRank()
          : 0;
        Wildbond.ApplyMark(evt.Target, Seams, Fact.MaybeContext, 600, current + 1);
        if (SelfBuff is not null)
        {
          Owner.Buffs.RemoveFact(SelfBuff);
        }
        CombatLog.Write("She spends her guard - and finds the opening.", Owner);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[anatomist] risky maneuver failed.", e);
      }
    }
  }

  /// <summary>
  /// Read the Tell (12th, replaces Learn the Seams - removed per
  /// the user): against enemies carrying her seams she reads the
  /// telegraphs - a +2 dodge bonus to AC against every one of them.
  /// </summary>
  [TypeId(Guids.AnatomistReadTheTellComponent)]
  internal class AnatomistReadTheTell : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    ITargetRulebookSubscriber, ISubscriber
  {
    public BlueprintBuff Seams;

    public void OnEventAboutToTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Target != Owner)
        {
          return;
        }
        var mark = evt.Initiator?.Buffs.GetBuff(Seams);
        if (mark is null || mark.MaybeContext?.MaybeCaster != Owner)
        {
          return;
        }
        evt.AddTemporaryModifier(Owner.Stats.AC
          .AddModifier(2, Runtime, ModifierDescriptor.Dodge));
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[anatomist] read the tell failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
  }
}

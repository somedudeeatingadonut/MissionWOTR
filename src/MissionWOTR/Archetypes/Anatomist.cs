using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
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
  /// The user's three mechanics, verbatim-first:
  /// - WEAK POINT (1st, replaces trapfinding): "if an attack misses
  ///   while needing to get d16 or above to hit, the rogue gets 1+
  ///   to attack, stackable." Implementation: the needed natural
  ///   roll is read straight off the attack event
  ///   (TargetAC - AttackBonus, the dcx PanacheDodge formula
  ///   attack.Roll + attack.AttackBonus >= attack.TargetAC); a miss
  ///   that needed a 16+ finds a SEAM - a stacking +1 attack bonus
  ///   against that enemy (a ranked per-enemy mark, the Wildbond
  ///   mark idiom). The stacks are SELF-LIMITING, elegantly: each
  ///   seam lowers the roll she needs, and once the needed roll
  ///   drops below 16, misses stop qualifying. A hard target
  ///   teaches her until it is no longer hard.
  /// - STUDENT OF DEFENSES (8th), both halves of the user's brief:
  ///   attacking a SPELLCASTER grants her SPELL RESISTANCE (11 +
  ///   rogue level, the vanilla AddSpellResistance component with a
  ///   class-level rank config - WithLinearProgression(1, 11)) for
  ///   one round, renewed by every attack she makes on a caster
  ///   (Spellbooks detect them - the TTT OppositionResearch
  ///   enumeration); and attacking an enemy with DAMAGE REDUCTION
  ///   grants additional damage, +1 per five rogue levels, NOT
  ///   stackable (a flat rider - the DR read via the vanilla
  ///   UnitPartDamageReduction's chunks, the dcx
  ///   AddDamageResistancePhysicalImproved read).
  ///
  /// My additions ("as well as some other effects you can think
  /// of"), the study deepening:
  /// - LEARN THE SEAMS (12th): every Weak Point stack also adds +1
  ///   damage against that enemy - the seams show where to press.
  /// - PERFECT STRIKE (16th): once per round, a hit against an
  ///   enemy carrying five or more seams is an automatic critical
  ///   (AutoCriticalThreat + AutoCriticalConfirmation, both set
  ///   pre-resolution and only when the roll already shows a hit -
  ///   the PanacheDodge roll-read idiom; a spent strike on a miss
  ///   is impossible).
  /// - VITAL READING (20th): the seams are shared - ALLIES gain her
  ///   Weak Point attack bonus against marked enemies (a rider on
  ///   the mark itself, target-side, granting +rank to any attacker
  ///   on her side - the COP SignatureStealthSurprise buff-rider
  ///   shape).
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
        .AddComponent(new AnatomistSeamsShare())
        .Configure();
      seams.GetComponent<AnatomistSeamsShare>().ShareFeature =
        BlueprintTool.Get<BlueprintFeature>(Guids.AnatomistVitalReadingFeature);

      // ----- The spell-resistance ward (8th, on the caster-strike) -----
      var ward = BuffConfigurator.New("AnatomistWardBuff", Guids.AnatomistWardBuff)
        .SetDisplayName("AnatomistWardBuff.Name")
        .SetDescription("AnatomistWardBuff.Description")
        .SetIcon(AbilityRefs.MindBlank.Reference.Get().Icon)
        .AddSpellResistance(value: ContextValues.Rank())
        .AddContextRankConfig(
          ContextRankConfigs.ClassLevel(
            new[] { CharacterClassRefs.RogueClass.ToString() })
            .WithLinearProgression(1, 11))
        .Configure();

      // ----- Weak Point (1st, replaces trapfinding) -----
      var weakPoint = FeatureConfigurator.New(
        "AnatomistWeakPointFeature", Guids.AnatomistWeakPointFeature)
        .SetDisplayName("AnatomistWeakPoint.Name")
        .SetDescription("AnatomistWeakPoint.Description")
        .SetIcon(AbilityRefs.SeeInvisibility.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new AnatomistWeakPoint { Seams = seams })
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

      // ----- Learn the Seams (12th) -----
      var learnSeams = FeatureConfigurator.New(
        "AnatomistLearnSeamsFeature", Guids.AnatomistLearnSeamsFeature)
        .SetDisplayName("AnatomistLearnSeams.Name")
        .SetDescription("AnatomistLearnSeams.Description")
        .SetIcon(AbilityRefs.LeadBlades.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new AnatomistLearnSeams { Seams = seams })
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

      // ----- Vital Reading (20th) -----
      var vitalReading = FeatureConfigurator.New(
        "AnatomistVitalReadingFeature", Guids.AnatomistVitalReadingFeature)
        .SetDisplayName("AnatomistVitalReading.Name")
        .SetDescription("AnatomistVitalReading.Description")
        .SetIcon(AbilityRefs.Foresight.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      // ----- The archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.AnatomistArchetype, CharacterClassRefs.RogueClass)
          .SetLocalizedName("Anatomist.Name")
          .SetLocalizedDescription("Anatomist.Description")
          .AddToAddFeatures(LevelPlan.L(1), weakPoint)
          .AddToAddFeatures(LevelPlan.L(8), defenses)
          .AddToAddFeatures(LevelPlan.L(12), learnSeams)
          .AddToAddFeatures(LevelPlan.L(16), perfectStrike)
          .AddToAddFeatures(LevelPlan.L(20), vitalReading);

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
        // What natural roll did she need? 16 or above qualifies -
        // the hard target teaches.
        int needed = evt.TargetAC - evt.AttackBonus;
        if (needed < 16)
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
        // past it. +1 per five levels, not stackable.
        var part = roll.Target.Get<UnitPartDamageReduction>();
        if (part is null || part.m_Chunks is null || part.m_Chunks.Count == 0)
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
  /// Learn the Seams (12th): every seam also adds +1 damage against
  /// that enemy - the seams show where to press.
  /// </summary>
  [TypeId(Guids.AnatomistLearnSeamsComponent)]
  internal class AnatomistLearnSeams : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintBuff Seams;

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
        var mark = roll.Target.Buffs.GetBuff(Seams);
        int stacks = mark is not null && mark.MaybeContext?.MaybeCaster == Owner
          ? mark.GetRank()
          : 0;
        if (stacks > 0)
        {
          evt.Add(new DirectDamage(DiceFormula.Zero, stacks) { SourceFact = Fact });
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[anatomist] learn the seams failed.", e);
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
  /// Vital Reading (20th): the seams are shared. A rider on the
  /// mark itself - any attacker on the anatomist's side gains her
  /// stacks as an attack bonus against the marked enemy (the
  /// target-side buff-rider shape, the COP SignatureStealthSurprise
  /// precedent). Only while she carries Vital Reading.
  /// </summary>
  [TypeId(Guids.AnatomistSeamsShareComponent)]
  internal class AnatomistSeamsShare : UnitBuffComponentDelegate,
    ITargetRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    ITargetRulebookSubscriber, ISubscriber
  {
    public BlueprintFeature ShareFeature;

    public void OnEventAboutToTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Target != Owner)
        {
          return;
        }
        var caster = Context?.MaybeCaster;
        if (caster is null || ShareFeature is null || !caster.HasFact(ShareFeature))
        {
          return;
        }
        if (evt.Initiator is null || evt.Initiator == caster ||
          !evt.Initiator.IsAlly(caster))
        {
          return;
        }
        evt.AddTemporaryModifier(evt.Initiator.Stats.AdditionalAttackBonus
          .AddModifier(GetStacks(), Runtime, ModifierDescriptor.Insight));
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[anatomist] vital reading share failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }

    private int GetStacks()
    {
      return Fact.GetRank();
    }
  }
}

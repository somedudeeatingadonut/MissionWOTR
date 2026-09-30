using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Fortune's Fool (original homebrew paladin archetype - 0.25.0).
  /// The user's brief: "for the homebrew how about a paladin focused
  /// on chaos? (more chance effects, higher miss chance with higher
  /// damage, etc) must be of chaotic alignment instead of the usual
  /// paladin alignment requirements, and doesnt get lay on hands,
  /// different support auras."
  ///
  /// The fantasy: she swore her oath not to a god of order but to
  /// chance itself - Desna's road, the roll of the die, the freedom of
  /// the unrepeated moment. Fortune's fool, and fortune's blade.
  ///
  /// The alignment gate (the user's requirement): the archetype
  /// carries a PrerequisiteAlignment of the three chaotic alignments
  /// with ArchetypeAlignment = true - Owlcat's own rule, verbatim from
  /// the component's tracker note (PF-485644): "the alignment
  /// restriction for an archetype ALWAYS REPLACES the class's
  /// restriction." Only the chaotic may take her road, and the lawful
  /// gate of the base paladin does not apply to her.
  ///
  /// The kit:
  /// - Fate's Wager (1st): every weapon attack she makes rides the
  ///   die. On a NATURAL d20 of 1-4 the blow scatters - a forced miss
  ///   even if her modifiers would hit (the higher miss chance). On a
  ///   natural 17+ the dice surge - the hit deals bonus damage, 1d6
  ///   per two paladin levels (the higher damage). Pure real dice, no
  ///   hidden random numbers: the attack roll's own natural die
  ///   decides (the TTT TricksterParry idiom - evt.D20 read and
  ///   evt.AutoMiss set in DidTrigger; the surge adds a DirectDamage
  ///   entry via evt.Add, the TTT AddAdditionalWeaponDamageOnHit
  ///   idiom). The Wager also carries her answer to fear and the
  ///   chaos in her blade (0.26.0, per the user):
  ///   - THE FOOL LAUGHS: a luck bonus on saves against fear equal
  ///     to her Charisma modifier (minimum +2) - NOT the paladin's
  ///     immunity; terror can still find her, it just blinks first
  ///     (the SisterLoyaltySaves descriptor-gated modifier pattern).
  ///   - CHAOS IN THE BLADE: her physical damage counts as
  ///     chaotic-aligned - the vanilla AddOutgoingPhysicalDamage
  ///     Property component (the very mechanism the creature
  ///     subtypes use), configured with DamageAlignment.Chaotic.
  /// - The auras (replacing the entire vanilla suite - "different
  ///   support auras"): each is a 10-foot ally aura carried on the
  ///   round tick (the SisterDragonAura idiom, self included), and
  ///   every one of them is luck:
  ///   - 3rd, Aura of the Open Road (for Aura of Courage): allies
  ///     within 10 ft gain +1 luck on attack rolls.
  ///   - 8th, Aura of Whimsy (for Aura of Resolve): allies within
  ///     10 ft gain DR that grows with her - 2 at 8th, +1 every three
  ///     levels after (3 at 11th, 4 at 14th), stopping at 5 at 17th
  ///     (0.27.0, per the user: "5 dr is too much for level 8, have
  ///     it start at 2 and go up every 3 levels until it reaches 5
  ///     and stops"). Four tier buffs, one aura that applies the
  ///     right one for her level.
  ///   - 11th, Aura of the Wandering Star (for Aura of Justice):
  ///     allies' weapon hits on a natural 17+ deal an extra 2d6 -
  ///     her lesser blessing, ONCE PER ALLY until her next turn
  ///     (0.26.0, per the user: the surge spends that ally's star,
  ///     and her next round tick re-lights it).
  ///   - 14th, Aura of Fortune's Favor (for Aura of Faith): allies
  ///     within 10 ft gain +2 luck on all saving throws.
  ///   - 17th, Aura of the Laughing Fool (for Aura of
  ///     Righteousness): allies within 10 ft carry Fate's Wager
  ///     itself - her curse and her blessing, shared.
  ///
  /// The trades (hefty): Lay on Hands (the user's requirement - "doesnt
  /// get lay on hands") with the entire Mercy selection (mercies
  /// improve a pool she does not have - removed as the direct
  /// consequence), ALL FIVE vanilla auras, and - 0.26.0, per the user -
  /// SMITE EVIL and CHANNEL POSITIVE ENERGY: the price paid for the
  /// fear-save blessing and the chaos in her blade. Divine grace,
  /// spells and the bond remain.
  ///
  /// Honesty notes (documented, not faked):
  /// - The scatter band (natural 1-4) overlaps the natural-1 auto-miss
  ///   - the effective extra miss chance is the 2-4 band (15% of
  ///     attacks that would otherwise hit).
  /// - The surge stacks with critical hits (a natural 20 crit also
  ///   surges) - documented, not smoothed away; chaos.
  /// - The engine's fallen-paladin logic was written for lawful good;
  ///   what happens to a fool who abandons chaos is unverified -
  ///   documented edge.
  /// - The auras are tick-refreshed with two rounds of natural
  ///   duration (the SisterDragonAura idiom), so a mark outlives her
  ///   by at most a round.
  /// - The star's once-per-ally reset rides HER round tick: if she
  ///   falls, a spent star stays spent (her next round never comes).
  /// Log prefix: [fool].
  /// </summary>
  internal static class FortunesFool
  {
    internal const string ArchetypeName = "FortunesFoolArchetype";

    public static void Configure()
    {
      var paladin = CharacterClassRefs.PaladinClass.Reference.Get();

      // ----- The aura buffs -----
      var openRoadBuff = BuffConfigurator.New(
        "FortunesFoolOpenRoadBuff", Guids.FortunesFoolOpenRoadBuff)
        .SetDisplayName("FortunesFoolOpenRoadBuff.Name")
        .SetDescription("FortunesFoolOpenRoadBuff.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .AddComponent(new FoolLuckAttack())
        .Configure();
      // 0.27.0: DR starts at 2 and climbs +1 every three levels
      // (11th, 14th) to a maximum of 5 at 17th - four tier buffs,
      // the aura below applies the one matching her level.
      var whimsyBuff = BuffConfigurator.New(
        "FortunesFoolWhimsyBuff", Guids.FortunesFoolWhimsyBuff)
        .SetDisplayName("FortunesFoolWhimsyBuff.Name")
        .SetDescription("FortunesFoolWhimsyBuff.Description")
        .SetIcon(AbilityRefs.ShieldOfFaith.Reference.Get().Icon)
        .AddDamageResistancePhysical(value: ContextValues.Constant(2))
        .Configure();
      var whimsyBuff2 = BuffConfigurator.New(
        "FortunesFoolWhimsyBuff2", Guids.FortunesFoolWhimsyBuff2)
        .SetDisplayName("FortunesFoolWhimsyBuff.Name")
        .SetDescription("FortunesFoolWhimsyBuff.Description")
        .SetIcon(AbilityRefs.ShieldOfFaith.Reference.Get().Icon)
        .AddDamageResistancePhysical(value: ContextValues.Constant(3))
        .Configure();
      var whimsyBuff3 = BuffConfigurator.New(
        "FortunesFoolWhimsyBuff3", Guids.FortunesFoolWhimsyBuff3)
        .SetDisplayName("FortunesFoolWhimsyBuff.Name")
        .SetDescription("FortunesFoolWhimsyBuff.Description")
        .SetIcon(AbilityRefs.ShieldOfFaith.Reference.Get().Icon)
        .AddDamageResistancePhysical(value: ContextValues.Constant(4))
        .Configure();
      var whimsyBuff4 = BuffConfigurator.New(
        "FortunesFoolWhimsyBuff4", Guids.FortunesFoolWhimsyBuff4)
        .SetDisplayName("FortunesFoolWhimsyBuff.Name")
        .SetDescription("FortunesFoolWhimsyBuff.Description")
        .SetIcon(AbilityRefs.ShieldOfFaith.Reference.Get().Icon)
        .AddDamageResistancePhysical(value: ContextValues.Constant(5))
        .Configure();
      var starBuff = BuffConfigurator.New(
        "FortunesFoolWanderingStarBuff", Guids.FortunesFoolWanderingStarBuff)
        .SetDisplayName("FortunesFoolWanderingStarBuff.Name")
        .SetDescription("FortunesFoolWanderingStarBuff.Description")
        .SetIcon(AbilityRefs.SeeInvisibility.Reference.Get().Icon)
        .AddComponent(new FoolStarSurge())
        .Configure();
      // The star must know itself: the surge component removes the
      // star buff when it spends, so it needs the finished blueprint.
      starBuff.GetComponent<FoolStarSurge>().StarBuff = starBuff;
      var favorBuff = BuffConfigurator.New(
        "FortunesFoolFavorBuff", Guids.FortunesFoolFavorBuff)
        .SetDisplayName("FortunesFoolFavorBuff.Name")
        .SetDescription("FortunesFoolFavorBuff.Description")
        .SetIcon(AbilityRefs.ProtectionFromEnergy.Reference.Get().Icon)
        .AddStatBonus(stat: StatType.SaveFortitude, value: 2, descriptor: ModifierDescriptor.Luck)
        .AddStatBonus(stat: StatType.SaveReflex, value: 2, descriptor: ModifierDescriptor.Luck)
        .AddStatBonus(stat: StatType.SaveWill, value: 2, descriptor: ModifierDescriptor.Luck)
        .Configure();
      var laughingBuff = BuffConfigurator.New(
        "FortunesFoolLaughingBuff", Guids.FortunesFoolLaughingBuff)
        .SetDisplayName("FortunesFoolLaughingBuff.Name")
        .SetDescription("FortunesFoolLaughingBuff.Description")
        .SetIcon(AbilityRefs.MindBlank.Reference.Get().Icon)
        .AddComponent(new FoolFatesWager())
        .Configure();

      // ----- The aura features -----
      var openRoad = AuraFeature("OpenRoad", Guids.FortunesFoolOpenRoadFeature, openRoadBuff);
      // Whimsy is tiered (0.27.0): the aura picks the DR buff for her
      // level instead of spreading a single fixed one.
      var whimsy = FeatureConfigurator.New(
        "FortunesFoolWhimsyFeature", Guids.FortunesFoolWhimsyFeature)
        .SetDisplayName("FortunesFoolWhimsy.Name")
        .SetDescription("FortunesFoolWhimsy.Description")
        .SetIcon(whimsyBuff.Icon)
        .SetIsClassFeature()
        .AddComponent(new FoolWhimsyAura
        {
          Tiers = new[] { whimsyBuff, whimsyBuff2, whimsyBuff3, whimsyBuff4 },
          Class = paladin,
        })
        .Configure();
      var wanderingStar = AuraFeature("WanderingStar", Guids.FortunesFoolWanderingStarFeature, starBuff);
      var favor = AuraFeature("Favor", Guids.FortunesFoolFavorFeature, favorBuff);
      // She already carries the wager itself - the laughing aura
      // shares it with her companions only.
      var laughing = AuraFeature(
        "Laughing", Guids.FortunesFoolLaughingFeature, laughingBuff, includeSelf: false);

      // ----- Fate's Wager (1st) -----
      var wager = FeatureConfigurator.New(
        "FortunesFoolFatesWagerFeature", Guids.FortunesFoolFatesWagerFeature)
        .SetDisplayName("FortunesFoolFatesWager.Name")
        .SetDescription("FortunesFoolFatesWager.Description")
        .SetIcon(AbilityRefs.ChainLightning.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new FoolFatesWager())
        // 0.26.0, the user's additions: she laughs at fear (a luck
        // save bonus, NOT immunity), and her blade carries chaos.
        .AddComponent(new FoolFearless())
        .AddOutgoingPhysicalDamageProperty(
          addAlignment: true, alignment: DamageAlignment.Chaotic)
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.FortunesFoolArchetype, CharacterClassRefs.PaladinClass)
          .SetLocalizedName("FortunesFool.Name")
          .SetLocalizedDescription("FortunesFool.Description")
          // THE user requirement: chaotic only - the archetype's
          // alignment gate REPLACES the paladin's lawful-good gate
          // (Owlcat's own rule, PF-485644).
          .AddPrerequisiteAlignment(
            AlignmentMaskType.ChaoticGood | AlignmentMaskType.ChaoticNeutral |
            AlignmentMaskType.ChaoticEvil,
            archetypeAlignment: true)
          .AddToAddFeatures(LevelPlan.L(1), wager)
          .AddToAddFeatures(LevelPlan.L(3), openRoad)
          .AddToAddFeatures(LevelPlan.L(8), whimsy)
          .AddToAddFeatures(LevelPlan.L(11), wanderingStar)
          .AddToAddFeatures(LevelPlan.L(14), favor)
          .AddToAddFeatures(LevelPlan.L(17), laughing);

      // The trades: lay on hands, every mercy, the whole aura suite,
      // and - 0.26.0, per the user - smite evil and channel positive
      // energy (the price of the fear blessing and the chaos blade).
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, paladin,
        FeatureRefs.LayOnHandsFeature.ToString(),
        FeatureSelectionRefs.SelectionMercy.ToString(),
        FeatureRefs.AuraOfCourageFeature.ToString(),
        FeatureRefs.AuraOfResolveFeature.ToString(),
        FeatureRefs.AuraOfJusticeFeature.ToString(),
        FeatureRefs.AuraOfFaithFeature.ToString(),
        FeatureRefs.AuraOfRighteousnessFeature.ToString(),
        FeatureRefs.SmiteEvilFeature.ToString(),
        FeatureRefs.ChannelEnergyPaladinFeature.ToString());;

      archetype.Configure();

      MissionFeats.Logger.Info("FortunesFool: configured.");
    }

    private static BlueprintFeature AuraFeature(string suffix, string guid, BlueprintBuff buff,
      bool includeSelf = true)
    {
      return FeatureConfigurator.New("FortunesFool" + suffix + "Feature", guid)
        .SetDisplayName("FortunesFool" + suffix + ".Name")
        .SetDescription("FortunesFool" + suffix + ".Description")
        .SetIcon(buff.Icon)
        .SetIsClassFeature()
        .AddComponent(new FoolChaosAura { AuraBuff = buff, IncludeSelf = includeSelf })
        .Configure();
    }
  }

  /// <summary>
  /// The standing aura: every round, she and every ally within 10 feet
  /// carry the configured buff (two rounds of natural duration,
  /// refreshed on the tick - the SisterDragonAura idiom; self
  /// included, the paladin-aura convention).
  /// </summary>
  [TypeId(Guids.FortunesFoolChaosAuraComponent)]
  internal class FoolChaosAura : UnitFactComponentDelegate, Kingmaker.Controllers.Units.ITickEachRound
  {
    public BlueprintBuff AuraBuff;
    /// <summary>
    /// False only for the 17th aura: she already carries Fate's Wager
    /// itself, and the wager never stacks with itself.
    /// </summary>
    public bool IncludeSelf = true;

    public void OnNewRound()
    {
      try
      {
        Spread();
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[fool] aura tick failed.", e);
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
      if (IncludeSelf)
      {
        Apply(Owner);
      }
      foreach (var ally in SanguineFont.AlliesWithin(Owner, 10))
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
  /// Fate's Wager itself, shared by her 1st-level feature and the 17th
  /// aura's buff: her weapon attacks scatter on a natural 1-4 (forced
  /// miss - the TricksterParry idiom) and surge on a natural 17+ (a
  /// DirectDamage entry of 1d6 per two paladin levels, added via
  /// evt.Add - the AddAdditionalWeaponDamageOnHit idiom). Pure real
  /// dice: the attack roll's own natural die decides.
  /// </summary>
  [TypeId(Guids.FortunesFoolFatesWagerComponent)]
  internal class FoolFatesWager : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    public void OnEventDidTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.IsFake || evt.Weapon is null)
        {
          return; // only her real weapon attacks
        }
        if (evt.D20 >= 1 && evt.D20 <= 4)
        {
          // The die betrays her: the blow scatters even if it would hit.
          evt.AutoMiss = true;
          CombatLog.Write("The die turns - her blow scatters on the wind.", Owner);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[fool] wager scatter failed.", e);
      }
    }

    public void OnEventAboutToTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner)
        {
          return;
        }
        var roll = evt.ParentRule?.AttackRoll;
        if (roll is null || roll.Weapon is null || roll.D20.Result < 17)
        {
          return; // only surging weapon hits
        }
        int level = Owner.Descriptor.Progression.GetClassLevel(
          CharacterClassRefs.PaladinClass.Reference.Get());
        var surge = new DirectDamage(new DiceFormula(Math.Max(1, level / 2), DiceType.D6), 0)
        {
          SourceFact = Fact,
        };
        evt.Add(surge);
        CombatLog.Write("The dice surge - fortune pays her hand.", Owner);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[fool] wager surge failed.", e);
      }
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
  }

  /// <summary>
  /// Aura of the Open Road rider: +1 luck on the bearer's attack rolls
  /// (the Solipsist RuleCalculateAttackBonus idiom).
  /// </summary>
  [TypeId(Guids.FortunesFoolLuckAttackComponent)]
  internal class FoolLuckAttack : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonus>, IRulebookHandler<RuleCalculateAttackBonus>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
    {
      if (evt.Initiator == Owner)
      {
        evt.AddModifier(1, Fact, ModifierDescriptor.Luck);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
  }

  /// <summary>
  /// The fool laughs at terror, but it can still find her: a luck
  /// bonus on saves against fear equal to her Charisma modifier
  /// (minimum +2) - NOT the paladin's aura-of-courage immunity (the
  /// user's distinction, 0.26.0). The SisterLoyaltySaves pattern:
  /// descriptor-gated temporary modifiers, applied to all three save
  /// stats (only the rolled one is consumed by the event).
  /// </summary>
  [TypeId(Guids.FortunesFoolFearlessComponent)]
  internal class FoolFearless : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleSavingThrow>, IRulebookHandler<RuleSavingThrow>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public void OnEventAboutToTrigger(RuleSavingThrow evt)
    {
      try
      {
        if (evt.Initiator != Owner)
        {
          return;
        }
        var descriptor =
          evt.Reason?.Context?.SourceAbility?.SpellDescriptor ??
          evt.Reason?.Ability?.Blueprint?.SpellDescriptor ?? SpellDescriptor.None;
        if ((descriptor & SpellDescriptor.Fear) == 0)
        {
          return; // only fear
        }
        int bonus = Math.Max(2, (Owner.Stats.Charisma.ModifiedValue - 10) / 2);
        evt.AddTemporaryModifier(evt.Initiator.Stats.SaveFortitude
          .AddModifier(bonus, Runtime, ModifierDescriptor.Luck));
        evt.AddTemporaryModifier(evt.Initiator.Stats.SaveReflex
          .AddModifier(bonus, Runtime, ModifierDescriptor.Luck));
        evt.AddTemporaryModifier(evt.Initiator.Stats.SaveWill
          .AddModifier(bonus, Runtime, ModifierDescriptor.Luck));
        CombatLog.Write("The fool laughs at terror - and terror blinks first.", Owner);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[fool] fearless failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }
  }

  /// <summary>
  /// Aura of the Wandering Star rider: the bearer's weapon hits on a
  /// natural 17+ deal an extra 2d6 - her lesser blessing (the same
  /// add-damage idiom as the wager surge, fixed dice).
  /// </summary>
  [TypeId(Guids.FortunesFoolStarSurgeComponent)]
  internal class FoolStarSurge : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    /// <summary>The star buff itself - spent when the surge fires
    /// (once per ally until the Fool's next turn, 0.26.0).</summary>
    public BlueprintBuff StarBuff;

    public void OnEventAboutToTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner)
        {
          return;
        }
        var roll = evt.ParentRule?.AttackRoll;
        if (roll is null || roll.Weapon is null || roll.D20.Result < 17)
        {
          return;
        }
        var surge = new DirectDamage(new DiceFormula(2, DiceType.D6), 0)
        {
          SourceFact = Fact,
        };
        evt.Add(surge);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[fool] star surge failed.", e);
      }
    }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner)
        {
          return;
        }
        var roll = evt.ParentRule?.AttackRoll;
        if (roll is null || roll.Weapon is null || roll.D20.Result < 17)
        {
          return;
        }
        // The star spends itself: once per ally until her next turn
        // (her round tick re-lights it via the aura refresh).
        if (StarBuff is not null)
        {
          Owner.Buffs.RemoveFact(StarBuff);
          CombatLog.Write("The wandering star spends itself - it returns with her next turn.", Owner);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[fool] star spend failed.", e);
      }
    }
  }

  /// <summary>
  /// The tiered whimsy aura (0.27.0): allies within 10 feet (self
  /// included) carry the DR buff matching the paladin's level - DR 2
  /// at 8th, 3 at 11th, 4 at 14th, 5 at 17th and beyond (the user's
  /// schedule: "start at 2 and go up every 3 levels until it reaches
  /// 5 and stops"). Tick-refreshed like every Fool aura; when she
  /// levels past a tier boundary the old tier is swapped for the new
  /// one on the next tick.
  /// </summary>
  [TypeId(Guids.FortunesFoolWhimsyAuraComponent)]
  internal class FoolWhimsyAura : UnitFactComponentDelegate,
    Kingmaker.Controllers.Units.ITickEachRound
  {
    /// <summary>The four DR tiers, lowest first (DR 2, 3, 4, 5).</summary>
    public BlueprintBuff[] Tiers;

    /// <summary>The paladin class, for her level.</summary>
    public BlueprintCharacterClass Class;

    public void OnNewRound()
    {
      try
      {
        Spread();
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[fool] whimsy aura tick failed.", e);
      }
    }

    protected override void OnActivate()
    {
      Spread();
    }

    private void Spread()
    {
      if (Tiers is null || Tiers.Length == 0)
      {
        return;
      }
      int level = Owner.Progression.GetClassLevel(Class);
      int tier = level >= 17 ? 3 : level >= 14 ? 2 : level >= 11 ? 1 : 0;
      Apply(Owner, tier);
      foreach (var ally in SanguineFont.AlliesWithin(Owner, 10))
      {
        Apply(ally, tier);
      }
    }

    private void Apply(UnitEntityData unit, int tier)
    {
      if (unit is null || unit.Descriptor.State.IsDead)
      {
        return;
      }
      // Retire any other tier she has outgrown (or has not yet
      // reached), then carry the current one if not already held.
      for (int i = 0; i < Tiers.Length; i++)
      {
        if (i != tier && unit.Buffs.GetBuff(Tiers[i]) is not null)
        {
          unit.Buffs.RemoveFact(Tiers[i]);
        }
      }
      if (unit.Buffs.GetBuff(Tiers[tier]) is null)
      {
        unit.Descriptor.AddBuff(Tiers[tier], Fact.MaybeContext, new Rounds(2).Seconds);
      }
    }
  }
}

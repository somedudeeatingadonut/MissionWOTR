using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.BasicEx;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.Configurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Conditions.Builder;
using BlueprintCore.Conditions.Builder.ContextEx;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Doomsayer (original homebrew inquisitor archetype - 0.17.0; concept by
  /// the user: an inquisitor focused ENTIRELY on intimidation, flavored
  /// "mostly the doomsayer", combining three kit shapes across the levels:
  /// the Confessor's per-target pronouncement, the Dread Aspect's aura of
  /// pressure, and the Doomsayer's escalating sentence - with HEFTY
  /// drawbacks per the user: "just be sure the drawbacks for the archetype
  /// are hefty (definitely no teamwork feats at all, for one)").
  ///
  /// She does not argue. She reads the sentence, and the world agrees with
  /// her. Mechanically: blend lean per the user's pick - the Intimidate
  /// skill as the chassis, supernatural payoffs stacked on top at the
  /// levels where the trades bite.
  ///
  /// The trades (hefty, all of them):
  /// - 1st: judgments - the ENTIRE line (judgment, second judgment at 8th,
  ///   third judgment at 16th, and true judgment at 17th).
  /// - 3rd: solo tactics AND every bonus teamwork feat (the user's
  ///   explicit "definitely no teamwork feats at all").
  /// - 11th: stalwart (wrath over resilience).
  /// - 14th: exploit weakness.
  /// She keeps: domain, bane, slayer, monster lore, spells, the capstone.
  ///
  /// The kit:
  /// - Pronounce Doom (1st): swift; she adds her Wisdom modifier on
  ///   Intimidate checks; pronouncing reads the target its sentence - an
  ///   Intimidate check (10 + target's level + its Wisdom). Success: the
  ///   target is Shaken for a minute and marked as her CONDEMNED (hidden
  ///   marker; one condemned at a time - a new pronouncement clears the
  ///   old). She gains +1 on attack rolls against the condemned (+2 at
  ///   9th, +3 at 17th).
  ///   - 5th: pronouncing on an already-shaken target escalates to
  ///     Frightened for 3 rounds.
  ///   - 12th: pronouncing on a frightened target also seizes it with
  ///     terror - Staggered for 1 round.
  /// - Dreadful Certainty (3rd): every weapon hit carries the verdict - a
  ///   free Intimidate check against the victim; success shakes it for a
  ///   round (two from 10th).
  /// - Dread Mien (8th): a 15-ft pressure aura (the game's own area-effect
  ///   system): enemies inside bear -2 on attack rolls and saving throws
  ///   AGAINST HER ONLY (0.17.1 nerf - the user's call). No save, and NOT
  ///   fear - the weight of doom, which the fearless still feel.
  /// - Death's Echo (11th): when the condemned dies, every enemy within
  ///   30 ft hears the sentence close: Will save (10 + half level + Wis)
  ///   or Shaken for a minute.
  /// - Sentence of Ruin (14th): the condemned now wears the sentence where
  ///   all can see it: -2 on attack rolls, saving throws and AC against
  ///   EVERYONE (the marker upgrades from hidden to visible).
  /// - Greater Dread Mien (16th): the aura reaches 30 feet.
  /// - Final Verdict (17th): the pronouncement needs no check and no
  ///   luck - the sentence is simply read (the Intimidate check is
  ///   skipped), and Death's Echo reaches 50 feet.
  ///
  /// Documented edges: with two doomsayers in one party a pronouncement
  /// clears the other's condemned marker (same documented-edge policy as
  /// the other shared-blueprint designs). Fear-immune creatures: the aura
  /// and the Sentence of Ruin still apply (not fear effects); the shaken/
  /// frightened tiers respect the game's own immunity handling.
  /// Log prefix: [removals] carries trade diagnostics; [doomsayer] the rest.
  /// </summary>
  internal static class Doomsayer
  {
    internal const string ArchetypeName = "DoomsayerArchetype";

    // Wired during Configure; read by the components.
    internal static BlueprintBuff CondemnedMarker;
    internal static BlueprintBuff DreadMienBuff15;
    internal static BlueprintBuff DreadMienBuff30;
    internal static BlueprintBuff CondemnedDoomed;
    internal static BlueprintFeature SentenceOfRuinFeature;
    internal static BlueprintFeature FinalVerdictFeature;

    public static void Configure()
    {
      var inquisitor = CharacterClassRefs.InquisitorClass.Reference.Get();
      var icon = FeatureRefs.InquisitorJudgements.Reference.Get().Icon;

      // ----- The condemned markers (Sentence of Ruin upgrades the first) -----
      CondemnedMarker = BuffConfigurator.New("DoomsayerCondemnedMarker", Guids.DoomsayerCondemnedMarkerBuff)
        .SetDisplayName("DoomsayerCondemnedMarker.Name")
        .SetDescription("DoomsayerCondemnedMarker.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .Configure();

      CondemnedDoomed = BuffConfigurator.New("DoomsayerCondemnedDoomed", Guids.DoomsayerCondemnedDoomedBuff)
        .SetDisplayName("DoomsayerCondemnedDoomed.Name")
        .SetDescription("DoomsayerCondemnedDoomed.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.AdditionalAttackBonus, value: -2,
          descriptor: ModifierDescriptor.UntypedStackable)
        .AddStatBonus(stat: StatType.SaveWill, value: -2,
          descriptor: ModifierDescriptor.UntypedStackable)
        .AddStatBonus(stat: StatType.SaveReflex, value: -2,
          descriptor: ModifierDescriptor.UntypedStackable)
        .AddStatBonus(stat: StatType.SaveFortitude, value: -2,
          descriptor: ModifierDescriptor.UntypedStackable)
        .AddStatBonus(stat: StatType.AC, value: -2,
          descriptor: ModifierDescriptor.UntypedStackable)
        .Configure();

      // ----- Dread Mien's payload: the weight of doom (not fear) -----
      var dreadDebuff = BuffConfigurator.New("DoomsayerDreadDebuff", Guids.DoomsayerDreadDebuffBuff)
        .SetDisplayName("DoomsayerDreadDebuff.Name")
        .SetDescription("DoomsayerDreadDebuff.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        // 0.17.1 nerf (user ask): the weight of doom applies only AGAINST
        // the doomsayer - the enemy strikes and saves at full strength
        // against everyone else while inside her mien.
        .AddComponent(new DoomsayerDreadWeight())
        .Configure();

      // ----- The auras: the game's own area-effect system (the same
      // mechanism the PackRager-family features use). -----
      // Engine note: bpcore's SetTargetType takes an enum this game build
      // does not expose (CI CS0122) - the enemy filter rides the buff
      // condition instead (ContextConditionIsEnemy).
      var dreadArea15 = AbilityAreaEffectConfigurator.New("DoomsayerDreadMienArea15", Guids.DoomsayerDreadMienArea15)
        .AddAbilityAreaEffectBuff(buff: dreadDebuff,
          condition: ConditionsBuilder.New().IsEnemy())
        .SetSize(new(15))
        .SetShape(AreaEffectShape.Cylinder)
        .Configure();

      var dreadArea30 = AbilityAreaEffectConfigurator.New("DoomsayerDreadMienArea30", Guids.DoomsayerDreadMienArea30)
        .AddAbilityAreaEffectBuff(buff: dreadDebuff,
          condition: ConditionsBuilder.New().IsEnemy())
        .SetSize(new(30))
        .SetShape(AreaEffectShape.Cylinder)
        .Configure();

      // The areas ride BUFFS (features cannot carry areas in this bpcore
      // build - CI CS1061); the features apply/remove their aura buff on
      // gain/loss via DoomsayerAuraBearer below.
      DreadMienBuff15 = BuffConfigurator.New("DoomsayerDreadMienBuff15", Guids.DoomsayerDreadMienBuff15)
        .SetDisplayName("DoomsayerDreadMien.Name")
        .SetDescription("DoomsayerDreadMien.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddAreaEffect(areaEffect: dreadArea15)
        .Configure();

      DreadMienBuff30 = BuffConfigurator.New("DoomsayerDreadMienBuff30", Guids.DoomsayerDreadMienBuff30)
        .SetDisplayName("DoomsayerDreadMienGreater.Name")
        .SetDescription("DoomsayerDreadMienGreater.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddAreaEffect(areaEffect: dreadArea30)
        .Configure();

      // ----- Pronounce Doom (1st): the ability -----
      var pronounce = ElementTool.Create<DoomsayerPronounce>();
      pronounce.Class = inquisitor;
      var pronounceAbility = AbilityConfigurator.New("DoomsayerPronounceDoomAbility", Guids.DoomsayerPronounceDoomAbility)
        .SetDisplayName("DoomsayerPronounceDoom.Name")
        .SetDescription("DoomsayerPronounceDoom.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Close)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
        .AllowTargeting(enemies: true)
        .SetEffectOnEnemy(AbilityEffectOnUnit.Harmful)
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(pronounce).Build())
        .Configure();

      // ----- Pronounce Doom (1st): the feature -----
      var pronouncer = FeatureConfigurator.New("DoomsayerPronounceDoomFeature", Guids.DoomsayerPronounceDoomFeature)
        .SetDisplayName("DoomsayerPronounceDoom.Name")
        .SetDescription("DoomsayerPronounceDoom.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { pronounceAbility })
        // She adds her Wisdom modifier on Intimidate checks.
        .AddContextStatBonus(StatType.SkillPersuasion, ContextValues.Rank(),
          ModifierDescriptor.UntypedStackable)
        .AddContextRankConfig(ContextRankConfigs.StatBonus(StatType.Wisdom))
        // +1/+2/+3 on attack rolls against the condemned (9th/17th).
        .AddComponent(new DoomsayerCondemnedBonus())
        .Configure();

      // ----- Dreadful Certainty (3rd) -----
      var certainty = FeatureConfigurator.New("DoomsayerDreadfulCertaintyFeature", Guids.DoomsayerDreadfulCertaintyFeature)
        .SetDisplayName("DoomsayerDreadfulCertainty.Name")
        .SetDescription("DoomsayerDreadfulCertainty.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new DoomsayerDreadfulCertainty { Class = inquisitor })
        .Configure();

      // ----- Dread Mien (8th) -----
      var dreadMien = FeatureConfigurator.New("DoomsayerDreadMienFeature", Guids.DoomsayerDreadMienFeature)
        .SetDisplayName("DoomsayerDreadMien.Name")
        .SetDescription("DoomsayerDreadMien.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new DoomsayerAuraBearer { AuraBuff = DreadMienBuff15 })
        .Configure();

      // ----- Death's Echo (11th) -----
      var deathsEcho = FeatureConfigurator.New("DoomsayerDeathsEchoFeature", Guids.DoomsayerDeathsEchoFeature)
        .SetDisplayName("DoomsayerDeathsEcho.Name")
        .SetDescription("DoomsayerDeathsEcho.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new DoomsayerDeathsEcho { Class = inquisitor })
        .Configure();

      // ----- Sentence of Ruin (14th) -----
      SentenceOfRuinFeature = FeatureConfigurator.New("DoomsayerSentenceOfRuinFeature", Guids.DoomsayerSentenceOfRuinFeature)
        .SetDisplayName("DoomsayerSentenceOfRuin.Name")
        .SetDescription("DoomsayerSentenceOfRuin.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .Configure();

      // ----- Greater Dread Mien (16th) -----
      var dreadMienGreater = FeatureConfigurator.New("DoomsayerDreadMienGreaterFeature", Guids.DoomsayerDreadMienGreaterFeature)
        .SetDisplayName("DoomsayerDreadMienGreater.Name")
        .SetDescription("DoomsayerDreadMienGreater.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new DoomsayerAuraBearer { AuraBuff = DreadMienBuff30 })
        .Configure();

      // ----- Final Verdict (17th) -----
      FinalVerdictFeature = FeatureConfigurator.New("DoomsayerFinalVerdictFeature", Guids.DoomsayerFinalVerdictFeature)
        .SetDisplayName("DoomsayerFinalVerdict.Name")
        .SetDescription("DoomsayerFinalVerdict.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.DoomsayerArchetype, CharacterClassRefs.InquisitorClass)
          .SetLocalizedName("Doomsayer.Name")
          .SetLocalizedDescription("Doomsayer.Description")
          .AddToAddFeatures(LevelPlan.L(1), pronouncer)
          .AddToAddFeatures(LevelPlan.L(3), certainty)
          .AddToAddFeatures(LevelPlan.L(8), dreadMien)
          .AddToAddFeatures(LevelPlan.L(11), deathsEcho)
          .AddToAddFeatures(LevelPlan.L(14), SentenceOfRuinFeature)
          .AddToAddFeatures(LevelPlan.L(16), dreadMienGreater)
          .AddToAddFeatures(LevelPlan.L(17), FinalVerdictFeature);

      // The hefty trades: the entire judgment line (1/8/16/17), solo
      // tactics + EVERY bonus teamwork feat, stalwart, exploit weakness.
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, inquisitor,
        FeatureRefs.InquisitorJudgements.ToString(),
        FeatureRefs.SecondJudgment.ToString(),
        FeatureRefs.ThirdJudgment.ToString(),
        FeatureRefs.TrueJudgmentFeature.ToString(),
        FeatureRefs.JudgmentAdditionalUse.ToString(),
        FeatureRefs.InquisitorSoloTactician.ToString(),
        FeatureSelectionRefs.TeamworkFeat.ToString(),
        FeatureRefs.Stalwart.ToString(),
        FeatureRefs.ExploitWeakness.ToString());

      archetype.Configure();

      MissionFeats.Logger.Info("Doomsayer: configured.");
    }
  }

  /// <summary>
  /// The weight of doom, 0.17.1 form (user nerf ask): -2 on attack rolls
  /// and saving throws AGAINST THE DOOMSAYER only. Sits on the debuff the
  /// mien applies; the penalty fires when the holder attacks a doomsayer
  /// (attack-bonus modifier) or saves against her effects (the save is
  /// rolled against her DC, so the penalty rides the DC side - the
  /// engine's own pattern, TitanStrike-style AddBonusDC).
  /// </summary>
  [TypeId(Guids.DoomsayerDreadWeightComponent)]
  internal class DoomsayerDreadWeight : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonus>, IRulebookHandler<RuleCalculateAttackBonus>,
    IInitiatorRulebookHandler<RuleSavingThrow>, IRulebookHandler<RuleSavingThrow>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    private static bool AgainstDoomsayer(UnitEntityData unit)
    {
      return unit is not null &&
        ((Doomsayer.DreadMienBuff15 is not null &&
          unit.Buffs.GetBuff(Doomsayer.DreadMienBuff15) != null) ||
         (Doomsayer.DreadMienBuff30 is not null &&
          unit.Buffs.GetBuff(Doomsayer.DreadMienBuff30) != null));
    }

    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
    {
      try
      {
        if (evt.Initiator != Owner || !AgainstDoomsayer(evt.Target))
        {
          return;
        }
        evt.AddModifier(-2, Fact, ModifierDescriptor.UntypedStackable);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[doomsayer] dread weight (attack) failed.", e);
      }
    }

    public void OnEventAboutToTrigger(RuleSavingThrow evt)
    {
      try
      {
        if (evt.Initiator != Owner || !AgainstDoomsayer(evt.Reason?.Caster))
        {
          return;
        }
        // The roller saves against her: the penalty rides her DC.
        evt.AddBonusDC(2);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[doomsayer] dread weight (saves) failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }

    public void OnEventDidTrigger(RuleSavingThrow evt) { }
  }

  /// <summary>
  /// Carries a permanent aura buff for as long as the feature is held:
  /// features cannot carry area effects directly in this bpcore build,
  /// but buffs can - so the feature applies its aura buff on gain and
  /// removes it on loss. (Context from a passive feature's fact - the
  /// same pattern the Riftstalker mark delivery uses.)
  /// </summary>
  [TypeId(Guids.DoomsayerAuraBearerComponent)]
  internal class DoomsayerAuraBearer : UnitFactComponentDelegate
  {
    public BlueprintBuff AuraBuff;

    protected override void OnActivate()
    {
      try
      {
        if (AuraBuff is not null)
        {
          Owner.AddBuff(AuraBuff, Context);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[doomsayer] aura bearer failed.", e);
      }
    }

    protected override void OnDeactivate()
    {
      try
      {
        if (AuraBuff is not null)
        {
          Owner.Buffs.RemoveFact(AuraBuff);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[doomsayer] aura removal failed.", e);
      }
    }
  }

  /// <summary>
  /// Pronounce Doom: reads the target its sentence. Intimidate check
  /// (10 + target's level + its Wisdom modifier) - skipped entirely once
  /// the Final Verdict is hers (17th): the sentence is simply read.
  /// Success: Shaken for a minute, escalating if the target already fears
  /// her (Frightened at 5th+, seized Staggered at 12th+), and the target
  /// becomes her condemned (one at a time; a new pronouncement clears the
  /// old marker).
  /// </summary>
  [TypeId(Guids.DoomsayerPronounceComponent)]
  internal class DoomsayerPronounce : Kingmaker.UnitLogic.Mechanics.Actions.ContextAction
  {
    public BlueprintCharacterClass Class;

    public override string GetCaption() => "Pronounce Doom";

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
        var level = caster.Progression.GetClassLevel(Class);

        // One condemned at a time: clear any standing sentence first.
        // (Documented edge: this clears another doomsayer's marker too.)
        using (var enumerator = Kingmaker.Game.Instance.State.Units.GetEnumerator())
        {
          while (enumerator.MoveNext())
          {
            var unit = enumerator.Current;
            if (unit is null)
            {
              continue;
            }
            if (Doomsayer.CondemnedMarker is not null)
            {
              unit.Buffs.RemoveFact(Doomsayer.CondemnedMarker);
            }
            if (Doomsayer.CondemnedDoomed is not null)
            {
              unit.Buffs.RemoveFact(Doomsayer.CondemnedDoomed);
            }
          }
        }

        // The check - unless the Final Verdict (17th) is hers.
        var succeeded =
          (Doomsayer.FinalVerdictFeature is not null &&
            caster.HasFact(Doomsayer.FinalVerdictFeature));
        if (!succeeded)
        {
          var dc = 10 + target.Descriptor.Progression.CharacterLevel +
            target.Stats.Wisdom.Bonus;
          var check = new RuleSkillCheck(caster, StatType.SkillPersuasion, dc);
          Rulebook.Trigger<RuleSkillCheck>(check);
          succeeded = check.Success;
        }
        if (!succeeded)
        {
          MissionFeats.Logger.Info(
            $"[doomsayer] pronouncement failed against {target.CharacterName}.");
          CombatLog.Write("The sentence fails to take.", caster);
          return;
        }
        CombatLog.Write($"Doom is pronounced upon {target.CharacterName}.", caster);

        var shaken = BuffRefs.Shaken.Reference.Get();
        var frightened = BuffRefs.Frightened.Reference.Get();
        var staggered = BuffRefs.Staggered.Reference.Get();
        var minute = ContextDuration.Fixed(10).Calculate(Context).Seconds;
        var threeRounds = ContextDuration.Fixed(3).Calculate(Context).Seconds;
        var oneRound = ContextDuration.Fixed(1).Calculate(Context).Seconds;

        var wasFrightened = target.Buffs.GetBuff(frightened) != null;
        var wasShaken = target.Buffs.GetBuff(shaken) != null;
        target.AddBuff(shaken, Context, duration: minute);
        if (wasFrightened && level >= 12)
        {
          // Terror seizes it mid-sentence.
          target.AddBuff(frightened, Context, duration: threeRounds);
          target.AddBuff(staggered, Context, duration: oneRound);
        }
        else if (wasShaken && level >= 5)
        {
          target.AddBuff(frightened, Context, duration: threeRounds);
        }

        // The mark: hidden until the Sentence of Ruin (14th) makes it
        // something everyone can see.
        var marker = Doomsayer.SentenceOfRuinFeature is not null &&
          caster.HasFact(Doomsayer.SentenceOfRuinFeature)
            ? Doomsayer.CondemnedDoomed
            : Doomsayer.CondemnedMarker;
        if (marker is not null)
        {
          var tenMinutes = ContextDuration.Fixed(100).Calculate(Context).Seconds;
          target.AddBuff(marker, Context, duration: tenMinutes);
        }

        MissionFeats.Logger.Info(
          $"[doomsayer] {target.CharacterName} stands condemned.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[doomsayer] pronouncement failed.", e);
      }
    }
  }

  /// <summary>
  /// +1 on attack rolls against the condemned (+2 at 9th, +3 at 17th).
  /// </summary>
  [TypeId(Guids.DoomsayerCondemnedBonusComponent)]
  internal class DoomsayerCondemnedBonus : UnitFactComponentDelegate,
    IGlobalRulebookHandler<RuleCalculateAttackBonus>, IRulebookHandler<RuleCalculateAttackBonus>,
    ISubscriber, IGlobalRulebookSubscriber
  {
    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.Target is null)
        {
          return;
        }
        if (Doomsayer.CondemnedMarker is null ||
          (!evt.Target.HasFact(Doomsayer.CondemnedMarker) &&
            (Doomsayer.CondemnedDoomed is null ||
              !evt.Target.HasFact(Doomsayer.CondemnedDoomed))))
        {
          return;
        }
        var level = Owner.Progression.GetClassLevel(
          CharacterClassRefs.InquisitorClass.Reference.Get());
        var bonus = level >= 17 ? 3 : level >= 9 ? 2 : 1;
        evt.AddModifier(bonus, Fact, ModifierDescriptor.UntypedStackable);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[doomsayer] condemned bonus failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
  }

  /// <summary>
  /// Dreadful Certainty (3rd): every weapon hit carries the verdict - a
  /// free Intimidate check against the victim; success shakes it for a
  /// round (two rounds from 10th).
  /// </summary>
  [TypeId(Guids.DoomsayerCertaintyComponent)]
  internal class DoomsayerDreadfulCertainty : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>, IRulebookHandler<RuleAttackWithWeapon>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass Class;

    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt)
    {
      try
      {
        if (evt.AttackRoll is null || !evt.AttackRoll.IsHit ||
          evt.Initiator != Owner || evt.Target is null || evt.Target.HPLeft <= 0)
        {
          return;
        }
        var victim = evt.Target;
        var dc = 10 + victim.Descriptor.Progression.CharacterLevel +
          victim.Stats.Wisdom.Bonus;
        var check = new RuleSkillCheck(Owner, StatType.SkillPersuasion, dc);
        Rulebook.Trigger<RuleSkillCheck>(check);
        if (!check.Success)
        {
          return;
        }
        var level = Owner.Progression.GetClassLevel(Class);
        var rounds = ContextDuration.Fixed(level >= 10 ? 2 : 1)
          .Calculate(Context).Seconds;
        victim.AddBuff(BuffRefs.Shaken.Reference.Get(), Context, duration: rounds);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[doomsayer] dreadful certainty failed.", e);
      }
    }
  }

  /// <summary>
  /// Death's Echo (11th): when the condemned dies, every enemy that can
  /// hear the sentence close must save (Will, 10 + half level + Wis) or be
  /// shaken for a minute. The Final Verdict (17th) carries it further:
  /// 50 feet instead of 30.
  /// </summary>
  [TypeId(Guids.DoomsayerEchoComponent)]
  internal class DoomsayerDeathsEcho : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>, IRulebookHandler<RuleAttackWithWeapon>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass Class;

    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt)
    {
      try
      {
        if (evt.AttackRoll is null || !evt.AttackRoll.IsHit ||
          evt.Initiator != Owner || evt.Target is null || evt.Target.HPLeft > 0)
        {
          return;
        }
        var fallen = evt.Target;
        if (Doomsayer.CondemnedMarker is null ||
          (!fallen.HasFact(Doomsayer.CondemnedMarker) &&
            (Doomsayer.CondemnedDoomed is null ||
              !fallen.HasFact(Doomsayer.CondemnedDoomed))))
        {
          return;
        }

        var level = Owner.Progression.GetClassLevel(Class);
        var dc = 10 + level / 2 + Owner.Stats.Wisdom.Bonus;
        var feet = Doomsayer.FinalVerdictFeature is not null &&
          Owner.HasFact(Doomsayer.FinalVerdictFeature) ? 50 : 30;
        var minute = ContextDuration.Fixed(10).Calculate(Context).Seconds;

        using (var enumerator = Kingmaker.Game.Instance.State.Units.GetEnumerator())
        {
          while (enumerator.MoveNext())
          {
            var unit = enumerator.Current;
            if (unit is null || unit.Descriptor.State.IsDead || unit == fallen ||
              !unit.IsEnemy(Owner))
            {
              continue;
            }
            if (unit.DistanceTo(fallen) <= feet.Feet().Meters)
            {
              var save = new RuleSavingThrow(unit, SavingThrowType.Will, dc);
              Rulebook.Trigger(save);
              if (!save.IsPassed)
              {
                unit.AddBuff(BuffRefs.Shaken.Reference.Get(), Context,
                  duration: minute);
              }
            }
          }
        }
        MissionFeats.Logger.Info("[doomsayer] the sentence closes - echo heard.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[doomsayer] death's echo failed.", e);
      }
    }
  }
}

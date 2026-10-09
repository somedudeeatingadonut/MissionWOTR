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
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.40.0 — the Dreadnaught (slayer homebrew, the user's design).
  ///
  /// "For the homebrew, how about a slayer that focuses on intimidation,
  /// and reducing saves using studied target instead of attack and damage
  /// rolls, and is able to use heavy armor, has 8 hp per level, and
  /// medium will. At the cost of all sneak attack, and 1 slayer talent
  /// in the middle."
  ///
  /// THE TRADES (found in the live progression by scan):
  /// - Studied target: EVERY grant goes (the sweep matches anything
  ///   "StudyTarget" at any level). Her study still exists — but it
  ///   breaks resolve instead of sharpening blades (below).
  /// - Sneak attack: EVERY grant goes (the "SneakAttack" sweep).
  /// - One slayer talent, the middle one — the level-10 grant.
  ///
  /// THE GAIN:
  /// - Dread Study (1st): a swift-action study that MARKS a foe: a
  ///   penalty on all three of its saving throws, −1 at 1st and one more
  ///   at 5th/10th/15th/20th (matching the studied-target tier schedule),
  ///   for ten minutes. No attack or damage bonus — the trade the user
  ///   designed. A FRESH mark (a foe not already marked) also attempts
  ///   to demoralize: a Persuasion check against 10 + the foe's level +
  ///   its Wisdom; success shakes it for a minute (the Doomsayer
  ///   pronouncement machinery, verbatim). Re-studying a marked foe
  ///   refreshes the dread but does not re-terrorize.
  /// - Heavy armor proficiency (1st).
  /// - Menace (1st): +half her level (minimum 1) on Persuasion — the
  ///   intimidation focus (Persuasion covers Intimidate in this game).
  /// - Unbreakable (1st): "medium Will" — the slayer's poor Will raised
  ///   halfway to good: +1 at 1st, +2 at 8th, +3 at 15th (poor 6, medium
  ///   9, good 12 at 20th).
  ///
  /// NOTE ON 8 HP: the slayer is already a d8 class — that line of the
  /// brief is satisfied by the class itself, unchanged.
  ///
  /// DOCUMENTED SIMPLIFICATIONS:
  /// - The mark lasts ten minutes and multiple foes can be marked at
  ///   once (the tabletop's one-study-at-a-time is bookkeeping, not
  ///   balance; re-study refreshes the tier).
  /// - The demoralize is a flat 1 minute on success (no margin-scaling).
  /// </summary>
  internal class Dreadnaught
  {
    internal const string ArchetypeName = "Dreadnaught";

    internal static void Configure()
    {
      var slayer = CharacterClassRefs.SlayerClass.Reference.Get();
      var icon = AbilityRefs.CauseFear.Reference.Get().Icon;
      var shaken = BuffRefs.Shaken.Reference.Get();

      // ----- The five marks: −1..−5 on all three saves (one per tier). -----
      var markGuids = new[]
      {
        Guids.DreadnaughtMarkBuff1, Guids.DreadnaughtMarkBuff2,
        Guids.DreadnaughtMarkBuff3, Guids.DreadnaughtMarkBuff4,
        Guids.DreadnaughtMarkBuff5,
      };
      var marks = new BlueprintBuff[5];
      for (var i = 0; i < 5; i++)
      {
        marks[i] = BuffConfigurator.New("DreadnaughtMarkBuff" + (i + 1), markGuids[i])
          .SetDisplayName("DreadnaughtMark.Name")
          .SetDescription("DreadnaughtMark.Description")
          .SetIcon(icon)
          .SetIsClassFeature()
          .AddStatBonus(stat: StatType.SaveFortitude, value: -(i + 1),
            descriptor: ModifierDescriptor.Penalty)
          .AddStatBonus(stat: StatType.SaveReflex, value: -(i + 1),
            descriptor: ModifierDescriptor.Penalty)
          .AddStatBonus(stat: StatType.SaveWill, value: -(i + 1),
            descriptor: ModifierDescriptor.Penalty)
          .Configure();
      }

      // ----- Dread Study: the ability. -----
      var study = new DreadnaughtStudyAction
      {
        Marks = marks,
        ShakenBuff = shaken,
        Class = slayer,
      };
      var ability = AbilityConfigurator.New("DreadnaughtStudyAbility", Guids.DreadnaughtStudyAbility)
        .SetDisplayName("DreadnaughtStudy.Name")
        .SetDescription("DreadnaughtStudy.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
        .AllowTargeting(enemies: true)
        .SetEffectOnEnemy(AbilityEffectOnUnit.Harmful)
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(study).Build())
        .Configure();

      // ----- The kit (1st): study, heavy armor, the menace. -----
      var kit = FeatureConfigurator.New("DreadnaughtKitFeature", Guids.DreadnaughtKitFeature)
        .SetDisplayName("DreadnaughtStudy.Name")
        .SetDescription("DreadnaughtStudy.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { ability,
          FeatureRefs.HeavyArmorProficiency.Cast<BlueprintUnitFactReference>() })
        .AddContextStatBonus(StatType.SkillPersuasion, ContextValues.Rank(),
          ModifierDescriptor.UntypedStackable)
        .AddContextRankConfig(ContextRankConfigs.ClassLevel(
            new[] { CharacterClassRefs.SlayerClass.ToString() })
          .WithCustomProgression(
            (1, 1), (4, 2), (6, 3), (8, 4), (10, 5), (12, 6), (14, 7), (16, 8), (18, 9), (20, 10)))
        .Configure();

      // ----- Unbreakable (1st): medium Will. -----
      var resolve = FeatureConfigurator.New("DreadnaughtResolveFeature", Guids.DreadnaughtResolveFeature)
        .SetDisplayName("DreadnaughtResolve.Name")
        .SetDescription("DreadnaughtResolve.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddContextStatBonus(StatType.SaveWill, ContextValues.Rank(),
          ModifierDescriptor.UntypedStackable)
        .AddContextRankConfig(ContextRankConfigs.ClassLevel(
            new[] { CharacterClassRefs.SlayerClass.ToString() })
          .WithCustomProgression((1, 1), (8, 2), (15, 3)))
        .Configure();

      // ----- The archetype. -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.DreadnaughtArchetype, CharacterClassRefs.SlayerClass)
          .SetLocalizedName("Dreadnaught.Name")
          .SetLocalizedDescription("Dreadnaught.Description")
          .AddToAddFeatures(LevelPlan.L(1), kit)
          .AddToAddFeatures(LevelPlan.L(1), resolve);
      // The trades: every studied-target grant, every sneak attack
      // grant, and the middle talent (the level-10 one).
      archetype = ArchetypeRemovals.RemoveEveryGrant(archetype, slayer, "StudyTarget");
      archetype = ArchetypeRemovals.RemoveEveryGrant(archetype, slayer, "SneakAttack");
      archetype = ArchetypeRemovals.RemoveAtLevel(
        archetype, slayer.Progression, 10, FeatureRefs.SlayerTalents.ToString());
      archetype.Configure();
      MissionFeats.Logger.Info("[dreadnaught] configured: " + ArchetypeName + ".");
    }
  }

  /// <summary>
  /// Dread Study: marks the target (−tier on all saves, ten minutes) and,
  /// on a fresh mark, attempts to demoralize it (Persuasion vs 10 + its
  /// level + its Wisdom; success shakes it for a minute) — the Doomsayer
  /// pronouncement machinery.
  /// </summary>
  [TypeId(Guids.DreadnaughtStudyAction)]
  internal class DreadnaughtStudyAction : ContextAction
  {
    public BlueprintBuff[] Marks;
    public BlueprintBuff ShakenBuff;
    public BlueprintCharacterClass Class;

    public override void RunAction()
    {
      try
      {
        // 0.62.0: Context and Target are ambient reads of ContextData<...>.Current, so both
        // are null when no data scope was pushed - and dereferencing them threw before the
        // guard below could say which case it was.
        var ambient = Context;
        var caster = ambient?.MaybeCaster;
        var target = Target?.Unit;
        if (caster is null || target is null || target.HPLeft <= 0)
        {
          MissionFeats.Logger.Warn(
            ambient is null
              ? "[dreadnaught] study aborted: no mechanics data scope."
              : "[dreadnaught] study aborted: no caster, no target, or target is down.");
          return;
        }
        int level = caster.Progression.GetClassLevel(Class);
        var tier = level >= 20 ? 4 : level >= 15 ? 3 : level >= 10 ? 2 : level >= 5 ? 1 : 0;

        var tenMinutes = ContextDuration.Fixed(100).Calculate(Context).Seconds;
        var minute = ContextDuration.Fixed(10).Calculate(Context).Seconds;

        // Clear any standing mark of ours; remember whether one was there
        // (a re-study refreshes dread but does not re-terrorize).
        var hadMark = false;
        foreach (var mark in Marks)
        {
          if (mark is null)
          {
            continue;
          }
          if (target.Buffs.GetBuff(mark) != null)
          {
            hadMark = true;
          }
          target.Buffs.RemoveFact(mark);
        }
        target.AddBuff(Marks[tier], Context, duration: tenMinutes);
        MissionFeats.Logger.Info(
          $"[dreadnaught] {target.CharacterName} is marked: -{tier + 1} on saves.");

        if (hadMark)
        {
          return;
        }

        // The demoralize: a fresh mark tries to break the foe outright.
        var dc = 10 + target.Descriptor.Progression.CharacterLevel +
          target.Stats.Wisdom.Bonus;
        var check = new RuleSkillCheck(caster, StatType.SkillPersuasion, dc);
        Rulebook.Trigger<RuleSkillCheck>(check);
        if (check.Success)
        {
          target.AddBuff(ShakenBuff, Context, duration: minute);
          CombatLog.Write($"{target.CharacterName} quails before the mark.", caster);
          MissionFeats.Logger.Info(
            $"[dreadnaught] {target.CharacterName} is shaken by the study.");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[dreadnaught] study failed.", e);
      }
    }

    public override string GetCaption() => "Dread Study";
  }
}

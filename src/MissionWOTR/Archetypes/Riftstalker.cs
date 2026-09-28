using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
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
  /// Riftstalker Hunter (HOMEBREW - 0.12.0, reworked 0.13.0 after user
  /// feedback: "I like the idea of the riftstalker, however not really the
  /// execution, it just ends up being a lot of extra damage with a
  /// slayer-like ability. Instead of the mark increasing the hunter's
  /// damage, how about her animal companion attacks through the rift (only
  /// applicable to one enemy), functioning like a status effect but having
  /// the save be AC" + "she doesn't keep the beast, the beast is still in
  /// the rift, but it can help invisibly with a specific guided command.")
  ///
  /// The concept, v2: a hunter of the Worldwound whose beast never came
  /// back through the rift with her - but it never left either. It hangs
  /// in the tear between worlds, unseen, and when she marks a prey and
  /// calls, it strikes through. She trades the ENTIRE companion side of
  /// the class (companion, raise companion, one with the wild) plus the
  /// teamwork-tactics side, and keeps the aspects, the spells, the stride.
  ///
  /// Kit (no self damage bonuses anywhere - the damage is the BEAST's):
  /// - Rift Mark (1st): once per round, the first hit that connects brands
  ///   the target for one minute. ONE creature can bear the mark at a time
  ///   (a new mark strips the old). The mark grants the stalker nothing by
  ///   itself - it is the tether through which the beast can reach.
  /// - Guided Command (1st): a swift-action ability, usable only against
  ///   the marked creature. The unseen beast lunges through the rift: an
  ///   attack roll (d20 + hunter level + Wisdom) against the target's REAL
  ///   AC (a live RuleCalculateAC query, so every AC modifier applies -
  ///   the "save" is AC, as designed). On a hit: 1d6 + half hunter level
  ///   raw damage (the mauling is not of this world - no DR applies) and
  ///   the target is shaken for one round by the sight of nothing
  ///   tearing into it.
  /// - Rift Stride (2nd): +10 feet of movement speed.
  /// - Unseen Guardian (5th): the beast circles her through the rift -
  ///   +2 dodge AC against attacks from the marked creature.
  /// - Blood in the Rift (12th): when a marked creature dies by her hand,
  ///   the beast feeds. For one minute afterward, its guided strikes need
  ///   no roll - it already knows the taste of the blood.
  /// Log prefix: [removals] carries the trade diagnostics; [riftstalker] the rest.
  /// </summary>
  internal static class Riftstalker
  {
    internal const string ArchetypeName = "RiftstalkerArchetype";

    // Wired during Configure; read by the components.
    internal static BlueprintBuff MarkBuff;
    internal static BlueprintBuff BloodfedBuff;

    public static void Configure()
    {
      var hunter = CharacterClassRefs.HunterClass.Reference.Get();
      var markIcon = AbilityRefs.HuntersSurpriseAbility.Reference.Get().Icon;

      // ----- The mark itself: the tether through which the beast can reach -----
      MarkBuff = BuffConfigurator.New("RiftstalkerMarkBuff", Guids.RiftstalkerMarkBuff)
        .SetDisplayName("RiftstalkerMark.Name")
        .SetDescription("RiftstalkerMark.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .Configure();

      // ----- Blood in the Rift: the fed beast needs no roll (pure flag buff) -----
      BloodfedBuff = BuffConfigurator.New("RiftstalkerBloodfedBuff", Guids.RiftstalkerBloodfedBuff)
        .SetDisplayName("RiftstalkerBloodfed.Name")
        .SetDescription("RiftstalkerBloodfed.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .Configure();

      // ----- Guided Command (1st): direct the unseen beast -----
      var strikeAction = ElementTool.Create<RiftstalkerGuidedStrike>();
      strikeAction.Class = hunter;
      var guidedCommand = AbilityConfigurator.New("RiftstalkerGuidedCommand", Guids.RiftstalkerGuidedCommand)
        .SetDisplayName("RiftstalkerGuidedCommand.Name")
        .SetDescription("RiftstalkerGuidedCommand.Description")
        .SetIcon(markIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
        .AllowTargeting(enemies: true)
        .SetEffectOnEnemy(AbilityEffectOnUnit.Harmful)
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(strikeAction).Build())
        .Configure();

      // ----- Rift Mark (1st): brand the prey (the tether, nothing more) -----
      var riftMark = FeatureConfigurator.New("RiftstalkerRiftMark", Guids.RiftstalkerRiftMark)
        .SetDisplayName("RiftstalkerRiftMark.Name")
        .SetDescription("RiftstalkerRiftMark.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .AddComponent(new RiftstalkerMarkDelivery { Class = hunter })
        .AddFacts(new() { guidedCommand })
        .Configure();

      // ----- Rift Stride (2nd): +10 feet -----
      var riftStride = FeatureConfigurator.New("RiftstalkerRiftStride", Guids.RiftstalkerRiftStride)
        .SetDisplayName("RiftstalkerRiftStride.Name")
        .SetDescription("RiftstalkerRiftStride.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .AddBuffMovementSpeed(value: 10, descriptor: ModifierDescriptor.Enhancement)
        .Configure();

      // ----- Unseen Guardian (5th): the beast guards her flank -----
      var unseenGuardian = FeatureConfigurator.New("RiftstalkerUnseenGuardian", Guids.RiftstalkerUnseenGuardian)
        .SetDisplayName("RiftstalkerUnseenGuardian.Name")
        .SetDescription("RiftstalkerUnseenGuardian.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .AddComponent(new RiftstalkerUnseenGuard())
        .Configure();

      // ----- Blood in the Rift (12th): marked kills feed the beast -----
      var bloodInTheRift = FeatureConfigurator.New("RiftstalkerBloodInTheRift", Guids.RiftstalkerBloodInTheRift)
        .SetDisplayName("RiftstalkerBloodInTheRift.Name")
        .SetDescription("RiftstalkerBloodInTheRift.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .AddComponent(new RiftstalkerMarkDelivery { Class = hunter, Feast = true })
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.RiftstalkerArchetype, CharacterClassRefs.HunterClass)
          .SetLocalizedName("Riftstalker.Name")
          .SetLocalizedDescription("Riftstalker.Description")
          .AddToAddFeatures(LevelPlan.L(1), riftMark)
          .AddToAddFeatures(LevelPlan.L(2), riftStride)
          .AddToAddFeatures(LevelPlan.L(5), unseenGuardian)
          .AddToAddFeatures(LevelPlan.L(12), bloodInTheRift);

      archetype = ArchetypeRemovals.AddRemovals(
        archetype, hunter,
        "715ac15eb8bd5e342bc8a0a3c9e3e38f", // Animal Companion (hunter's selection)
        "27ad1316abbbbb34b8ffb9a87f38c10e", // Raise Companion
        "1b9916f7675d6ef4fb427081250d49de", // Hunter Tactics (teamwork sharing)
        "14b66a1e2a6a415182a651db8c0f1143", // Hunter teamwork-feat progression
        "c1e0f4ada7c673e4f8e5c57d1eea13d0", // One with the Wild
        "f34a34c8f8a8410ca5e0e21800fa4961"); // One with the Wild (pet half)

      archetype.Configure();

      MissionFeats.Logger.Info("Riftstalker: configured (0.13.0 guided-beast rework).");
    }
  }

  /// <summary>
  /// Applies the rift mark (first hit each round) and, when the Feast
  /// upgrade is owned, feeds the beast when marked prey dies by her hand.
  /// The mark is unique: applying a new one strips it from every other
  /// unit (the rift holds one tether at a time).
  /// </summary>
  [TypeId(Guids.RiftstalkerMarkDeliveryComponent)]
  internal class RiftstalkerMarkDelivery :
    UnitFactComponentDelegate<RiftstalkerMarkDelivery.ComponentData>,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>, IRulebookHandler<RuleAttackWithWeapon>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass Class;
    public bool Feast;

    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt)
    {
      try
      {
        if (evt.AttackRoll is null || !evt.AttackRoll.IsHit || evt.Target is null)
        {
          return;
        }
        var target = evt.Target;
        var wasMarked = Riftstalker.MarkBuff is not null &&
          target.Buffs.GetBuff(Riftstalker.MarkBuff) is not null;

        // Once per round, the first connecting strike brands the target -
        // and only one creature may bear the mark at a time.
        var now = Kingmaker.Game.Instance.TimeController.GameTime;
        var markApplied = false;
        if (Data.LastUse + 1.Rounds().Seconds <= now && target.HPLeft > 0)
        {
          Data.LastUse = now;
          using var enumerator = Kingmaker.Game.Instance.State.Units.GetEnumerator();
          while (enumerator.MoveNext())
          {
            var unit = enumerator.Current;
            if (unit is null || unit == target)
            {
              continue;
            }
            var oldMark = unit.Buffs.GetBuff(Riftstalker.MarkBuff);
            if (oldMark is not null)
            {
              unit.RemoveFact(oldMark);
            }
          }
          target.AddBuff(
            Riftstalker.MarkBuff, Context,
            duration: ContextDuration.Fixed(10).Calculate(Context).Seconds);
          markApplied = true;
        }

        // Blood in the Rift: the kill of a marked creature feeds the beast.
        if (Feast && Riftstalker.BloodfedBuff is not null && target.HPLeft <= 0 &&
          (wasMarked || markApplied))
        {
          Owner.AddBuff(
            Riftstalker.BloodfedBuff, Context,
            duration: ContextDuration.Fixed(10).Calculate(Context).Seconds);
          MissionFeats.Logger.Info("[riftstalker] blood in the rift: the beast feeds.");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[riftstalker] mark delivery failed.", e);
      }
    }

    public class ComponentData
    {
      public TimeSpan LastUse;
    }
  }

  /// <summary>
  /// Guided Command: the unseen beast strikes through the rift at the
  /// marked target. The "save" is AC - a live RuleCalculateAC query, so
  /// every AC modifier (armor, dodge, flank, the works) applies. The
  /// beast's accuracy is the bond itself: d20 + hunter level + Wisdom.
  /// On a hit: 1d6 + half level raw damage (no DR - the mauling is not of
  /// this world) and shaken for one round. While the beast is bloodfed,
  /// no roll is made - it always hits.
  /// </summary>
  [TypeId(Guids.RiftstalkerGuidedStrikeComponent)]
  internal class RiftstalkerGuidedStrike : Kingmaker.UnitLogic.Mechanics.Actions.ContextAction
  {
    public BlueprintCharacterClass Class;

    private static readonly System.Random Dice = new();

    public override string GetCaption() => "Guided Command";

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

        // The beast only reaches through the mark.
        if (Riftstalker.MarkBuff is null ||
          target.Buffs.GetBuff(Riftstalker.MarkBuff) is null)
        {
          MissionFeats.Logger.Info("[riftstalker] guided command: target is not marked.");
          return;
        }

        var level = caster.Progression.GetClassLevel(Class);
        if (level <= 0)
        {
          return;
        }

        // Bloodfed: it already knows the taste - no roll needed.
        var bloodfed = Riftstalker.BloodfedBuff is not null &&
          caster.HasFact(Riftstalker.BloodfedBuff);

        // The "save" is AC: a real AC query, all modifiers included.
        var ac = Rulebook.Trigger(new RuleCalculateAC(caster, target, AttackType.Melee)).Result;
        var bonus = level + caster.Stats.Wisdom.Bonus;
        var roll = Dice.Next(1, 21);
        var hits = bloodfed || roll == 20 || (roll != 1 && roll + bonus >= ac);

        if (!hits)
        {
          MissionFeats.Logger.Info(
            $"[riftstalker] guided strike misses (d20 {roll} + {bonus} vs AC {ac}).");
          return;
        }

        // Mauling: raw rift damage - damage reduction does not apply.
        var damage = Dice.Next(1, 7) + level / 2;
        target.Descriptor.Damage += damage;

        // Unseen jaws: the sight of nothing tearing in is deeply unsettling.
        var seconds = ContextDuration.Fixed(1).Calculate(Context).Seconds;
        target.AddBuff(BuffRefs.Shaken.Reference.Get(), Context, duration: seconds);
        MissionFeats.Logger.Info(
          $"[riftstalker] guided strike hits (d20 {roll} + {bonus} vs AC {ac}): {damage} damage.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[riftstalker] guided strike failed.", e);
      }
    }
  }

  /// <summary>
  /// Unseen Guardian: the beast circles her through the rift. +2 dodge AC
  /// against attacks made by the creature bearing the mark - the exact
  /// AC-when-attacked handler shape Step Aside uses.
  /// </summary>
  [TypeId(Guids.RiftstalkerUnseenGuardComponent)]
  internal class RiftstalkerUnseenGuard : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateAC>, IRulebookHandler<RuleCalculateAC>,
    ISubscriber, ITargetRulebookSubscriber
  {
    public void OnEventAboutToTrigger(RuleCalculateAC evt)
    {
      try
      {
        if (evt.Initiator is null || Riftstalker.MarkBuff is null ||
          evt.Initiator.Buffs.GetBuff(Riftstalker.MarkBuff) is null)
        {
          return;
        }
        evt.AddModifier(2, Fact, ModifierDescriptor.Dodge);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[riftstalker] unseen guard failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }
  }
}

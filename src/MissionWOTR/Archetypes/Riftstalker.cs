using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
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
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Riftstalker Hunter (HOMEBREW - 0.12.0; reworked 0.13.0 after user
  /// feedback - the mark no longer boosts her damage, the unseen beast
  /// strikes by guided command with AC as the save; reworked 0.14.0 -
  /// command toolbox; tuned 0.15.0 - base damage now scales 1d4/1d5/1d6
  /// by level 3/4, Mauling locked to 12th, Rending and Cataclysm replaced
  /// by Paralyzing and Scattering, and the commands now live in a VARIANT
  /// MENU: clicking Guided Command opens the game's own click-to-choose
  /// submenu (the AbilityVariants component - the mechanism vanilla's
  /// MasterHunterAbility uses). Engine honesty: the variant list is
  /// blueprint-static in this build of the game (the member that mutates
  /// it at runtime is not exposed - probed in CI), so the menu lists ALL
  /// TEN directives and the gating lives in each command's action: a
  /// directive only fires if she owns its learned feature; unlearned
  /// entries are empty whispers.)
  ///
  /// The concept, v3: a hunter of the Worldwound whose beast never came
  /// back through the rift with her - but it never left either. It hangs
  /// in the tear between worlds, unseen, and it LEARNS. As she levels she
  /// teaches it new directives - guided commands - and each round she may
  /// give it one. The mark is the tether; the command is the leash.
  ///
  /// Kit:
  /// - Rift Mark (1st): once per round, the first hit that connects brands
  ///   the target for one minute. ONE creature bears the mark at a time.
  ///   Grants nothing by itself - it is the tether.
  /// - Guided Command (1st): the base directive - a swift action against
  ///   the marked: d20 + hunter level + Wisdom vs the target's REAL AC
  ///   (live RuleCalculateAC - the "save" is AC). Hit: 1d4 + half level
  ///   raw damage (no DR; the die grows to 1d5 at 3rd and 1d6 at 4th) and
  ///   shaken 1 round. Clicking it opens the command menu.
  /// - Rift Stride (2nd): +10 feet of movement speed.
  /// - Unseen Guardian (5th): +2 dodge AC against the marked's attacks.
  /// - RIFT COMMANDS (4/8/12/16/20): a selection of ten directives, one
  ///   new command learned at each of those levels (user spec). Every
  ///   command is a swift action against the marked, resolved with the
  ///   same attack-roll-vs-AC check unless noted; the beast can be given
  ///   only ONE directive per round (shared budget with Guided Command,
  ///   tracked by a one-round hidden buff).
  ///     Pinning (4): entangled 1 round.
  ///     Terrifying (4): Will save or frightened 1 round.
  ///     Guarding (4): no roll - +4 dodge AC vs the marked for 1 round.
  ///     Blinding (8): Fort save or blind 1 round.
  ///     Fatiguing (8): Fort save or fatigued 1 minute.
  ///     Mauling (12): 2d6 + half level raw damage (locked to 12th per
  ///       user tuning - the big hit comes late now).
  ///     Staggering (12): Fort save or staggered 1 round.
  ///     Crippling (12): -2 attack rolls for 1 minute (custom debuff).
  ///     Paralyzing (16): Will save or paralyzed 1 round (the beast
  ///       seizes the target bodily through the rift).
  ///     Scattering (16): the beast erupts through at the marked - the
  ///       marked is frightened 1 round on a failed Will save, and every
  ///       other enemy within 10 feet is shaken 1 round on a failed save.
  /// Save DCs where a save applies: 10 + half hunter level + Wisdom.
  /// Log prefix: [removals] carries the trade diagnostics; [riftstalker] the rest.
  /// </summary>
  internal static class Riftstalker
  {
    internal const string ArchetypeName = "RiftstalkerArchetype";

    // Wired during Configure; read by the components.
    internal static BlueprintBuff MarkBuff;
    internal static BlueprintBuff ActedBuff;
    internal static BlueprintBuff GuardBuff;
    internal static BlueprintBuff CrippledBuff;

    /// <summary>The ten directives the beast can learn.</summary>
    internal enum RiftCommand
    {
      Mauling,
      Pinning,
      Terrifying,
      Guarding,
      Blinding,
      Fatiguing,
      Staggering,
      Crippling,
      Paralyzing,
      Scattering,
    }

    private static readonly (RiftCommand Command, string Name, string FeatureGuid,
      string AbilityGuid, int Gate)[] Commands =
    {
      (RiftCommand.Mauling, "Mauling", "2D7043BC-76CB-4DF3-A0FA-113E7C2A3110",
        "F8322D3E-656B-4042-9B5B-C174D55DAC34", 12),
      (RiftCommand.Pinning, "Pinning", "2261463B-252E-46EB-BA7B-903E255A6590",
        "BDF4D01C-F780-4D51-95CA-4D17848AA0E3", 4),
      (RiftCommand.Terrifying, "Terrifying", "323288E5-1B07-4517-94E6-1E2513D88D00",
        "08D0419D-4993-4780-A282-664681BF9CC6", 4),
      (RiftCommand.Guarding, "Guarding", "BF0BC23F-68EF-4609-BAE9-304A56B736D2",
        "B07EB50B-11A5-4424-A43C-90F0BCD7B1DC", 4),
      (RiftCommand.Blinding, "Blinding", "25A81052-20FC-4F5C-A930-4AE5DD0EE7D6",
        "AEEA8014-0E8F-4F23-8B1F-865684B9A9A7", 8),
      (RiftCommand.Fatiguing, "Fatiguing", "48C33912-3494-4171-9538-7DCF62DB5174",
        "DB084F8A-6ABF-4309-A1C0-FC8674CDCEE3", 8),
      (RiftCommand.Staggering, "Staggering", "0B7878E4-E175-40D1-9E16-12A03A1998A4",
        "899A75ED-223B-4D19-8604-7E14D5072162", 12),
      (RiftCommand.Crippling, "Crippling", "206AE47E-6E9C-44FC-ADE9-7DB3CC80DE1C",
        "C87364FB-D02C-4BB1-9A69-B1E7AAB8B5B0", 12),
      (RiftCommand.Paralyzing, "Paralyzing", "98A5BE43-E722-4E63-A69B-583628A9CACF",
        "683B55CA-1B4B-4D01-998C-DED8A7EF1247", 16),
      (RiftCommand.Scattering, "Scattering", "C99343A8-F066-4F71-A6CA-AAB126D53A02",
        "F46EB338-068D-4D82-B0B3-1CC557E26784", 16),
    };

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

      // ----- The one-directive-per-round marker (hidden bookkeeping buff) -----
      ActedBuff = BuffConfigurator.New("RiftstalkerActedBuff", Guids.RiftstalkerActedBuff)
        .SetDisplayName("RiftstalkerActed.Name")
        .SetDescription("RiftstalkerActed.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .Configure();

      // ----- Guarding Command's payload: +4 dodge vs the marked -----
      GuardBuff = BuffConfigurator.New("RiftstalkerGuardBuff", Guids.RiftstalkerGuardBuff)
        .SetDisplayName("RiftstalkerGuard.Name")
        .SetDescription("RiftstalkerGuard.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .AddComponent(new RiftstalkerUnseenGuard { Bonus = 4 })
        .Configure();

      // ----- Crippling Command's payload: -2 attacks for a minute -----
      CrippledBuff = BuffConfigurator.New("RiftstalkerCrippledBuff", Guids.RiftstalkerCrippledBuff)
        .SetDisplayName("RiftstalkerCrippled.Name")
        .SetDescription("RiftstalkerCrippled.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .AddComponent(new RiftstalkerCripple())
        .Configure();

      // ----- Guided Command (1st): the base directive -----
      var strikeAction = ElementTool.Create<RiftstalkerGuidedStrike>();
      strikeAction.Class = hunter;

      // ----- Rift Stride (2nd): +10 feet -----
      var riftStride = FeatureConfigurator.New("RiftstalkerRiftStride", Guids.RiftstalkerRiftStride)
        .SetDisplayName("RiftstalkerRiftStride.Name")
        .SetDescription("RiftstalkerRiftStride.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .AddBuffMovementSpeed(value: 10, descriptor: ModifierDescriptor.Enhancement)
        .Configure();

      // ----- Unseen Guardian (5th): the beast circles her flank -----
      var unseenGuardian = FeatureConfigurator.New("RiftstalkerUnseenGuardian", Guids.RiftstalkerUnseenGuardian)
        .SetDisplayName("RiftstalkerUnseenGuardian.Name")
        .SetDescription("RiftstalkerUnseenGuardian.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .AddComponent(new RiftstalkerUnseenGuard { Bonus = 2 })
        .Configure();

      // ----- Rift Commands (4/8/12/16/20): the directive toolbox -----
      var commandFeatures =
        new System.Collections.Generic.List<Blueprint<BlueprintFeatureReference>>();
      var commandAbilities =
        new System.Collections.Generic.List<Blueprint<BlueprintAbilityReference>>();
      foreach (var entry in Commands)
      {
        var action = ElementTool.Create<RiftstalkerCommandAction>();
        action.Class = hunter;
        action.Command = entry.Command;
        action.FeatureGuid = entry.FeatureGuid;
        var ability = AbilityConfigurator.New("Riftstalker" + entry.Name + "Command", entry.AbilityGuid)
          .SetDisplayName("Riftstalker" + entry.Name + "Command.Name")
          .SetDescription("Riftstalker" + entry.Name + "Command.Description")
          .SetIcon(markIcon)
          .SetType(AbilityType.Special)
          .SetRange(AbilityRange.Long)
          .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
          .AllowTargeting(enemies: true)
          .SetEffectOnEnemy(AbilityEffectOnUnit.Harmful)
          .AddAbilityEffectRunAction(ActionsBuilder.New().Add(action).Build())
          .Configure();
        var feature = FeatureConfigurator.New("Riftstalker" + entry.Name + "CommandFeature", entry.FeatureGuid)
          .SetDisplayName("Riftstalker" + entry.Name + "Command.Name")
          .SetDescription("Riftstalker" + entry.Name + "Command.Description")
          .SetIcon(markIcon)
          .SetIsClassFeature()
          // The learned flag the command's action checks; the command
          // itself is cast through the Guided Command variant menu, so it
          // is NOT granted as a separate action-bar ability.
          // 0.53.0 - LevelPlan.Gate: in test mode all five command picks land
          // at level 1, and every command is gated at hunter 4+, so none of
          // them could be taken.
          .AddPrerequisiteClassLevel(
            CharacterClassRefs.HunterClass.Reference.Get(), LevelPlan.Gate(entry.Gate))
          .Configure();
        commandAbilities.Add(ability);
        commandFeatures.Add(feature);
        MissionFeats.Logger.Info(
          $"[riftstalker] rift command: {entry.Name} (gate: hunter {entry.Gate}+).");
      }

      var commandSelection =
        FeatureSelectionConfigurator.New("RiftstalkerCommandSelection", Guids.RiftstalkerCommandSelection)
          .SetDisplayName("RiftstalkerCommands.Name")
          .SetDescription("RiftstalkerCommands.Description")
          .SetIcon(markIcon)
          .SetAllFeatures(commandFeatures.ToArray())
          .Configure();

      // ----- Guided Command (1st): the base directive AND the command menu -----
      // Clicking it opens the game's variant submenu (AbilityVariants - the
      // MasterHunterAbility mechanism). The menu lists all ten directives;
      // each one's action checks whether she owns its learned feature, so
      // unlearned entries are empty whispers (the variant list itself is
      // blueprint-static in this build of the game - the runtime-mutation
      // member is not exposed; probed in CI and documented).
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
        .AddAbilityVariants(commandAbilities)
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

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.RiftstalkerArchetype, CharacterClassRefs.HunterClass)
          .SetLocalizedName("Riftstalker.Name")
          .SetLocalizedDescription("Riftstalker.Description")
          .AddToAddFeatures(LevelPlan.L(1), riftMark)
          .AddToAddFeatures(LevelPlan.L(2), riftStride)
          .AddToAddFeatures(LevelPlan.L(4), commandSelection)
          .AddToAddFeatures(LevelPlan.L(8), commandSelection)
          .AddToAddFeatures(LevelPlan.L(12), commandSelection)
          .AddToAddFeatures(LevelPlan.L(16), commandSelection)
          .AddToAddFeatures(LevelPlan.L(20), commandSelection)
          .AddToAddFeatures(LevelPlan.L(5), unseenGuardian);

      archetype = ArchetypeRemovals.AddRemovals(
        archetype, hunter,
        "715ac15eb8bd5e342bc8a0a3c9e3e38f", // Animal Companion (hunter's selection)
        "27ad1316abbbbb34b8ffb9a87f38c10e", // Raise Companion
        "1b9916f7675d6ef4fb427081250d49de", // Hunter Tactics (teamwork sharing)
        "c1e0f4ada7c673e4f8e5c57d1eea13d0"); // One with the Wild

      // 0.53.0 FIX — same bug as the venomblood's: the teamwork-feat
      // trade was a hard-coded GUID the 0.52.1 log proves is not in the
      // hunter progression, so the trade silently never happened. Now
      // removed by name at every grant level.
      archetype = ArchetypeRemovals.RemoveEveryGrant(archetype, hunter, "Teamwork");

      archetype.Configure();

      MissionFeats.Logger.Info("Riftstalker: configured (0.14.0 command toolbox).");
    }
  }

  /// <summary>
  /// Shared machinery for every directive: the one-per-round budget (the
  /// beast acts once, however she words it), the mark gate, the
  /// attack-roll-vs-AC check (a live RuleCalculateAC query, so every AC
  /// modifier applies - the "save" is AC), dice, and save DCs.
  /// </summary>
  internal static class RiftCommands
  {
    private static readonly System.Random Dice = new();

    /// <summary>The beast can be directed once per round. True if the
    /// directive may proceed (and the budget is spent).</summary>
    internal static bool Begin(MechanicsContext context)
    {
      var caster = context.MaybeCaster;
      if (caster is null || Riftstalker.ActedBuff is null)
      {
        return false;
      }
      if (caster.HasFact(Riftstalker.ActedBuff))
      {
        MissionFeats.Logger.Info("[riftstalker] the beast has already acted this round.");
        return false;
      }
      var seconds = ContextDuration.Fixed(1).Calculate(context).Seconds;
      caster.AddBuff(Riftstalker.ActedBuff, context, duration: seconds);
      return true;
    }

    /// <summary>True if the target bears the rift mark (the tether).</summary>
    internal static bool IsMarked(UnitEntityData target) =>
      target is not null && Riftstalker.MarkBuff is not null &&
      target.Buffs.GetBuff(Riftstalker.MarkBuff) is not null;

    /// <summary>The beast's lunge: d20 + hunter level + Wisdom vs real AC.</summary>
    internal static bool BeastHits(UnitEntityData caster, UnitEntityData target, int level)
    {
      var ac = Rulebook.Trigger(new RuleCalculateAC(caster, target, AttackType.Melee)).Result;
      var bonus = level + caster.Stats.Wisdom.Bonus;
      var roll = Dice.Next(1, 21);
      var hits = roll == 20 || (roll != 1 && roll + bonus >= ac);
      MissionFeats.Logger.Info(
        $"[riftstalker] beast strike: d20 {roll} + {bonus} vs AC {ac} -> {(hits ? "hit" : "miss")}.");
      return hits;
    }

    internal static int Roll(int dice, int sides) =>
      dice <= 0 ? 0 : Dice.Next(dice, dice * sides + 1);

    /// <summary>The base strike's die, per the user's tuning: 1d4 at the
    /// start, 1d5 at 3rd, 1d6 from 4th on.</summary>
    internal static int DieSides(int level) => level < 3 ? 4 : level < 4 ? 5 : 6;

    internal static int SaveDC(UnitEntityData caster, int level) =>
      10 + level / 2 + caster.Stats.Wisdom.Bonus;

    internal static bool FailsSave(UnitEntityData target, SavingThrowType type, int dc)
    {
      var save = new RuleSavingThrow(target, type, dc);
      return !Rulebook.Trigger(save).IsPassed;
    }
  }

  /// <summary>
  /// Applies the rift mark (first hit each round) and keeps it unique - a
  /// new mark strips the old from every other unit (the rift holds one
  /// tether at a time).
  /// </summary>
  [TypeId(Guids.RiftstalkerMarkDeliveryComponent)]
  internal class RiftstalkerMarkDelivery :
    UnitFactComponentDelegate<RiftstalkerMarkDelivery.ComponentData>,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>, IRulebookHandler<RuleAttackWithWeapon>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass Class;

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

        // Once per round, the first connecting strike brands the target -
        // and only one creature may bear the mark at a time.
        var now = CombatTime.Now();
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
  /// Guided Command, the base directive: the unseen beast strikes through
  /// the rift at the marked target. Hit: 1d6 + half level raw damage (no
  /// DR - the mauling is not of this world) and shaken 1 round.
  /// </summary>
  [TypeId(Guids.RiftstalkerGuidedStrikeComponent)]
  internal class RiftstalkerGuidedStrike : NamedContextAction
  {
    public BlueprintCharacterClass Class;

    public override string GetCaption() => "Guided Command";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        var target = Target.Unit;
        if (caster is null || target is null || target.HPLeft <= 0 ||
          !RiftCommands.IsMarked(target) || !RiftCommands.Begin(Context))
        {
          return;
        }

        var level = caster.Progression.GetClassLevel(Class);
        if (level <= 0)
        {
          return;
        }

        if (!RiftCommands.BeastHits(caster, target, level))
        {
          return;
        }

        var damage = RiftCommands.Roll(1, RiftCommands.DieSides(level)) + level / 2;
        target.Descriptor.Damage += damage;

        var seconds = ContextDuration.Fixed(1).Calculate(Context).Seconds;
        target.AddBuff(BuffRefs.Shaken.Reference.Get(), Context, duration: seconds);
        MissionFeats.Logger.Info($"[riftstalker] guided strike: {damage} damage.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[riftstalker] guided strike failed.", e);
      }
    }
  }

  /// <summary>
  /// The ten learned directives. All are swift actions against the marked
  /// target; all resolve through the shared attack-roll-vs-AC check unless
  /// noted; all share the one-directive-per-round budget.
  /// </summary>
  [TypeId(Guids.RiftstalkerCommandActionComponent)]
  internal class RiftstalkerCommandAction : NamedContextAction
  {
    public BlueprintCharacterClass Class;
    public Riftstalker.RiftCommand Command;
    public string FeatureGuid;

    public override string GetCaption() => Command + " Command";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        var target = Target.Unit;
        if (caster is null || target is null || target.HPLeft <= 0 ||
          !RiftCommands.IsMarked(target))
        {
          return;
        }

        // The menu lists every directive; only learned ones can be given.
        // Checked BEFORE the once-per-round budget so an empty whisper
        // costs nothing.
        if (FeatureGuid is not null)
        {
          var learned = BlueprintTool.Get<BlueprintFeature>(FeatureGuid);
          if (learned is null || !caster.HasFact(learned))
          {
            MissionFeats.Logger.Info(
              $"[riftstalker] {Command} command: the beast has not learned this.");
            return;
          }
        }

        if (!RiftCommands.Begin(Context))
        {
          return;
        }

        var level = caster.Progression.GetClassLevel(Class);
        if (level <= 0)
        {
          return;
        }

        var round = ContextDuration.Fixed(1).Calculate(Context).Seconds;
        var minute = ContextDuration.Fixed(10).Calculate(Context).Seconds;
        var dc = RiftCommands.SaveDC(caster, level);

        switch (Command)
        {
          case Riftstalker.RiftCommand.Mauling:
            if (RiftCommands.BeastHits(caster, target, level))
            {
              var damage = RiftCommands.Roll(2, 6) + level / 2;
              target.Descriptor.Damage += damage;
              MissionFeats.Logger.Info($"[riftstalker] mauling: {damage} damage.");
            }
            break;

          case Riftstalker.RiftCommand.Pinning:
            if (RiftCommands.BeastHits(caster, target, level))
            {
              target.AddBuff(BuffRefs.EntangledBuff.Reference.Get(), Context, duration: round);
            }
            break;

          case Riftstalker.RiftCommand.Terrifying:
            if (RiftCommands.BeastHits(caster, target, level) &&
              RiftCommands.FailsSave(target, SavingThrowType.Will, dc))
            {
              target.AddBuff(BuffRefs.Frightened.Reference.Get(), Context, duration: round);
            }
            break;

          case Riftstalker.RiftCommand.Guarding:
            // No strike - the beast interposes itself between her and the
            // marked until her next turn.
            caster.AddBuff(Riftstalker.GuardBuff, Context, duration: round);
            break;

          case Riftstalker.RiftCommand.Blinding:
            if (RiftCommands.BeastHits(caster, target, level) &&
              RiftCommands.FailsSave(target, SavingThrowType.Fortitude, dc))
            {
              target.AddBuff(BuffRefs.Blind.Reference.Get(), Context, duration: round);
            }
            break;

          case Riftstalker.RiftCommand.Fatiguing:
            if (RiftCommands.BeastHits(caster, target, level) &&
              RiftCommands.FailsSave(target, SavingThrowType.Fortitude, dc))
            {
              target.AddBuff(BuffRefs.Fatigued.Reference.Get(), Context, duration: minute);
            }
            break;

          case Riftstalker.RiftCommand.Staggering:
            if (RiftCommands.BeastHits(caster, target, level) &&
              RiftCommands.FailsSave(target, SavingThrowType.Fortitude, dc))
            {
              target.AddBuff(BuffRefs.Staggered.Reference.Get(), Context, duration: round);
            }
            break;

          case Riftstalker.RiftCommand.Crippling:
            if (RiftCommands.BeastHits(caster, target, level))
            {
              target.AddBuff(Riftstalker.CrippledBuff, Context, duration: minute);
            }
            break;

          case Riftstalker.RiftCommand.Paralyzing:
            // The beast seizes the target bodily through the rift.
            if (RiftCommands.BeastHits(caster, target, level) &&
              RiftCommands.FailsSave(target, SavingThrowType.Will, dc))
            {
              target.AddBuff(BuffRefs.Paralyzed.Reference.Get(), Context, duration: round);
            }
            break;

          case Riftstalker.RiftCommand.Scattering:
            // The beast erupts through at the marked: the marked flees and
            // everything cowering near it wavers.
            if (RiftCommands.BeastHits(caster, target, level))
            {
              if (RiftCommands.FailsSave(target, SavingThrowType.Will, dc))
              {
                target.AddBuff(BuffRefs.Frightened.Reference.Get(), Context, duration: round);
              }
              using (var enumerator = Kingmaker.Game.Instance.State.Units.GetEnumerator())
              {
                while (enumerator.MoveNext())
                {
                  var unit = enumerator.Current;
                  if (unit is null || unit.Descriptor.State.IsDead || unit == target ||
                    !unit.IsEnemy(caster))
                  {
                    continue;
                  }
                  if (unit.DistanceTo(target) <= 10.Feet().Meters &&
                    RiftCommands.FailsSave(unit, SavingThrowType.Will, dc))
                  {
                    unit.AddBuff(BuffRefs.Shaken.Reference.Get(), Context, duration: round);
                  }
                }
              }
            }
            break;
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error($"[riftstalker] {Command} command failed.", e);
      }
    }
  }

  /// <summary>
  /// Unseen Guardian: the beast circles her through the rift. +Bonus dodge
  /// AC against attacks made by the creature bearing the mark - the exact
  /// AC-when-attacked handler shape Step Aside uses. Used passively by the
  /// 5th-level feature (+2) and by the Guarding Command's buff (+4).
  /// </summary>
  [TypeId(Guids.RiftstalkerUnseenGuardComponent)]
  internal class RiftstalkerUnseenGuard : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateAC>, IRulebookHandler<RuleCalculateAC>,
    ISubscriber, ITargetRulebookSubscriber
  {
    public int Bonus = 2;

    public void OnEventAboutToTrigger(RuleCalculateAC evt)
    {
      try
      {
        if (evt.Initiator is null || Riftstalker.MarkBuff is null ||
          evt.Initiator.Buffs.GetBuff(Riftstalker.MarkBuff) is null)
        {
          return;
        }
        evt.AddModifier(Bonus, Fact, ModifierDescriptor.Dodge);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[riftstalker] unseen guard failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }
  }

  /// <summary>
  /// Crippling Command's payload: the beast's claws leave the target's
  /// movements ragged - -2 on its attack rolls for a minute.
  /// </summary>
  [TypeId(Guids.RiftstalkerCrippleComponent)]
  internal class RiftstalkerCripple : UnitFactComponentDelegate,
    IGlobalRulebookHandler<RuleCalculateAttackBonus>, IRulebookHandler<RuleCalculateAttackBonus>,
    ISubscriber, IGlobalRulebookSubscriber
  {
    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
    {
      try
      {
        if (evt.Initiator == Owner)
        {
          evt.AddModifier(-2, Fact, ModifierDescriptor.UntypedStackable);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[riftstalker] cripple failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
  }
}

using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Verminous Hunter (faithful port of the ACG archetype - 0.15.0, the
  /// hunter's first tabletop archetype in the mod, per the user: "seems
  /// perfect for a swarm that walks playthrough").
  ///
  /// A verminous hunter calls on the ceaseless, single-minded dedication
  /// of vermin to hunt and overwhelm her prey. Where other hunters invoke
  /// the cunning of alpha predators, she reaches out to the spider instead
  /// of the monkey, the mantis instead of the snake.
  ///
  /// Tabletop and its WOTR adaptation:
  /// - Vermin Companion (1st, ALTERS animal companion): she must choose a
  ///   vermin companion. WOTR ships exactly one creepy-crawly companion -
  ///   the centipede - so the archetype swaps the hunter's companion
  ///   selection for a vermen-only one (the centipede list; the game has
  ///   no spider/scorpion/wasp companions to offer).
  /// - Vermin Empathy (ALTERS wild empathy): vermin only. WOTR's hunter
  ///   has no wild empathy feature at all, so there is nothing to alter -
  ///   documented, skipped.
  /// - Vermin Focus (1st, REPLACES animal focus): the aspect system, vermin
  ///   flavors. WOTR's animal focus is a toggleable engine of its own; our
  ///   adaptation follows the game's own Forester precedent (which made
  ///   animal focus a permanent self-aspect): a PERMANENT aspect chosen at
  ///   1st, with a second pick at 8th and a third at 15th (mirroring the
  ///   second/third focus cadence), each scaling its bonus at 8th and 15th.
  ///   Aspect list adapted to what WOTR can express (five of the
  ///   tabletop's fifteen; the cut ones ride on Climb/Swim/jump/web checks
  ///   or skill stats the engine barely exercises - documented):
  ///     Ant: +1/+2/+3 on attack and damage rolls (the hive's strength).
  ///     Beetle: +2/+4/+6 natural armor.
  ///     Mantis: +2/+4/+6 on attacks of opportunity.
  ///     Scorpion: +2/+4/+6 on combat maneuver rolls.
  ///     Worm: 50% fortification (fixed - the engine's component takes a
  ///       flat value; the tabletop's 25->50->75 would need runtime
  ///       recalculation it does not offer).
  /// - Swarm Stride (5th, REPLACES woodland stride): she moves through
  ///   vermin swarms without danger. WOTR exposes no swarm-detection hook
  ///   (no IsSwarm flag on units in any reference source), so the
  ///   damage-immunity half cannot be gated honestly; adapted as immunity
  ///   to the swarm's actual weapons - poison and disease - with the
  ///   impossibility documented.
  /// Log prefix: [removals] carries the trade diagnostics; [verminous] the rest.
  /// </summary>
  internal static class Verminous
  {
    internal const string ArchetypeName = "VerminousArchetype";

    /// <summary>Aspect bonus tier: 0 at 1st, 1 at 8th, 2 at 15th.</summary>
    internal static int Tier(int hunterLevel) => hunterLevel < 8 ? 0 : hunterLevel < 15 ? 1 : 2;

    public static void Configure()
    {
      var hunter = CharacterClassRefs.HunterClass.Reference.Get();
      var verminIcon = AbilityRefs.Poison.Reference.Get().Icon;

      // ----- Vermin Companion (1st): the centipede, and only the centipede -----
      var centipede = FeatureRefs.AnimalCompanionFeatureCentipede.Reference.Get();
      var verminCompanion = FeatureSelectionConfigurator.New(
        "VerminousCompanionSelection", Guids.VerminousCompanionSelection)
        .SetDisplayName("VerminousCompanion.Name")
        .SetDescription("VerminousCompanion.Description")
        .SetIcon(verminIcon)
        .SetAllFeatures(centipede)
        .Configure();

      // ----- Vermin Focus (1st/8th/15th): the permanent aspects -----
      var ant = FeatureConfigurator.New("VerminousAspectAnt", Guids.VerminousAspectAnt)
        .SetDisplayName("VerminousAspectAnt.Name")
        .SetDescription("VerminousAspectAnt.Description")
        .SetIcon(verminIcon)
        .SetIsClassFeature()
        .AddComponent(new VerminAspectAttackDamage { Class = hunter, Step = 1 })
        .Configure();

      var beetle = FeatureConfigurator.New("VerminousAspectBeetle", Guids.VerminousAspectBeetle)
        .SetDisplayName("VerminousAspectBeetle.Name")
        .SetDescription("VerminousAspectBeetle.Description")
        .SetIcon(verminIcon)
        .SetIsClassFeature()
        .AddComponent(new VerminAspectNaturalArmor { Class = hunter, Step = 2 })
        .Configure();

      var mantis = FeatureConfigurator.New("VerminousAspectMantis", Guids.VerminousAspectMantis)
        .SetDisplayName("VerminousAspectMantis.Name")
        .SetDescription("VerminousAspectMantis.Description")
        .SetIcon(verminIcon)
        .SetIsClassFeature()
        .AddComponent(new VerminAspectOpportunity { Class = hunter, Step = 2 })
        .Configure();

      var scorpion = FeatureConfigurator.New("VerminousAspectScorpion", Guids.VerminousAspectScorpion)
        .SetDisplayName("VerminousAspectScorpion.Name")
        .SetDescription("VerminousAspectScorpion.Description")
        .SetIcon(verminIcon)
        .SetIsClassFeature()
        .AddComponent(new VerminAspectManeuver { Class = hunter, Step = 2 })
        .Configure();

      var worm = FeatureConfigurator.New("VerminousAspectWorm", Guids.VerminousAspectWorm)
        .SetDisplayName("VerminousAspectWorm.Name")
        .SetDescription("VerminousAspectWorm.Description")
        .SetIcon(verminIcon)
        .SetIsClassFeature()
        .AddFortification(bonus: 50)
        .Configure();

      var verminFocus = FeatureSelectionConfigurator.New(
        "VerminousFocusSelection", Guids.VerminousFocusSelection)
        .SetDisplayName("VerminousFocus.Name")
        .SetDescription("VerminousFocus.Description")
        .SetIcon(verminIcon)
        .SetAllFeatures(ant, beetle, mantis, scorpion, worm)
        .Configure();

      // ----- Swarm Stride (5th): the swarm's venom and filth no longer touch her -----
      var swarmStride = FeatureConfigurator.New("VerminousSwarmStride", Guids.VerminousSwarmStride)
        .SetDisplayName("VerminousSwarmStride.Name")
        .SetDescription("VerminousSwarmStride.Description")
        .SetIcon(verminIcon)
        .SetIsClassFeature()
        .AddFacts(new() {
          FeatureRefs.ImmunityToPoison.Reference.Get(),
          FeatureRefs.ImmunityToDisease.Reference.Get(),
        })
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.VerminousArchetype, CharacterClassRefs.HunterClass)
          .SetLocalizedName("Verminous.Name")
          .SetLocalizedDescription("Verminous.Description")
          .AddToAddFeatures(LevelPlan.L(1), verminCompanion)
          .AddToAddFeatures(LevelPlan.L(1), verminFocus)
          .AddToAddFeatures(LevelPlan.L(5), swarmStride)
          .AddToAddFeatures(LevelPlan.L(8), verminFocus)
          .AddToAddFeatures(LevelPlan.L(15), verminFocus);

      archetype = ArchetypeRemovals.AddRemovals(
        archetype, hunter,
        "715ac15eb8bd5e342bc8a0a3c9e3e38f", // Animal Companion (hunter's selection)
        "8f96e5a671bf4dbd84ebe6f38a86dc94", // Animal Focus
        "443365823b7d6d14b8d12f4e7bce1077", // Animal Focus (feature)
        "2efe5983c9064cc6b55f16bc68f0fc33"); // Woodland Stride

      archetype.Configure();

      MissionFeats.Logger.Info("Verminous: configured.");
    }
  }

  /// <summary>
  /// Ant aspect: the worker's ceaseless strength - +Step x tier on her
  /// attack and damage rolls. (Tabletop grants a Strength enhancement
  /// bonus; the attack/damage translation carries its combat effect
  /// without the stat-attach lifecycle.)
  /// </summary>
  [TypeId(Guids.VerminAspectAttackDamageComponent)]
  internal class VerminAspectAttackDamage : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonus>, IRulebookHandler<RuleCalculateAttackBonus>,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass Class;
    public int Step;

    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
    {
      try
      {
        if (evt.Initiator != Owner)
        {
          return;
        }
        var bonus = Step * (1 + Verminous.Tier(Owner.Progression.GetClassLevel(Class)));
        if (bonus > 0)
        {
          evt.AddModifier(bonus, Fact, ModifierDescriptor.UntypedStackable);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[verminous] ant attack bonus failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }

    public void OnEventAboutToTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner)
        {
          return;
        }
        var bonus = Step * (1 + Verminous.Tier(Owner.Progression.GetClassLevel(Class)));
        if (bonus <= 0)
        {
          return;
        }
        foreach (var damage in evt.DamageBundle)
        {
          damage.AddModifier(new Modifier(bonus, Fact, ModifierDescriptor.UntypedStackable));
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[verminous] ant damage bonus failed.", e);
      }
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
  }

  /// <summary>
  /// Beetle aspect: +Step x tier natural armor - the carapace.
  /// </summary>
  [TypeId(Guids.VerminAspectNaturalArmorComponent)]
  internal class VerminAspectNaturalArmor : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateAC>, IRulebookHandler<RuleCalculateAC>,
    ISubscriber, ITargetRulebookSubscriber
  {
    public BlueprintCharacterClass Class;
    public int Step;

    public void OnEventAboutToTrigger(RuleCalculateAC evt)
    {
      try
      {
        if (evt.Target != Owner)
        {
          return;
        }
        var bonus = Step * (1 + Verminous.Tier(Owner.Progression.GetClassLevel(Class)));
        if (bonus > 0)
        {
          evt.AddModifier(bonus, Fact, ModifierDescriptor.NaturalArmor);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[verminous] beetle armor failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt) { }
  }

  /// <summary>
  /// Mantis aspect: +Step x tier on attacks of opportunity - the striking
  /// forelimbs. AoO detection via the proven Reason.Rule idiom.
  /// </summary>
  [TypeId(Guids.VerminAspectOpportunityComponent)]
  internal class VerminAspectOpportunity : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonus>, IRulebookHandler<RuleCalculateAttackBonus>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass Class;
    public int Step;

    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
    {
      try
      {
        if (evt.Initiator != Owner ||
          evt.Reason.Rule is not RuleAttackWithWeapon attack ||
          !attack.IsAttackOfOpportunity)
        {
          return;
        }
        var bonus = Step * (1 + Verminous.Tier(Owner.Progression.GetClassLevel(Class)));
        if (bonus > 0)
        {
          evt.AddModifier(bonus, Fact, ModifierDescriptor.UntypedStackable);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[verminous] mantis opportunity failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
  }

  /// <summary>
  /// Scorpion aspect: +Step x tier on combat maneuver rolls - the seizing
  /// claws. The Sweep Fend wiring.
  /// </summary>
  [TypeId(Guids.VerminAspectManeuverComponent)]
  internal class VerminAspectManeuver : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCombatManeuver>, IRulebookHandler<RuleCombatManeuver>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass Class;
    public int Step;

    public void OnEventAboutToTrigger(RuleCombatManeuver evt)
    {
      try
      {
        if (evt.Initiator != Owner)
        {
          return;
        }
        var bonus = Step * (1 + Verminous.Tier(Owner.Progression.GetClassLevel(Class)));
        if (bonus > 0)
        {
          evt.AddModifier(bonus, Fact, ModifierDescriptor.UntypedStackable);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[verminous] scorpion maneuver failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCombatManeuver evt) { }
  }
}

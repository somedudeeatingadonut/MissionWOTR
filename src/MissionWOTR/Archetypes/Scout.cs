using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using UnityEngine;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// THE SCOUT (tabletop port - Advanced Player's Guide pg. 134).
  ///
  /// "Not all rogues live in the city. Scouts frequently roam the
  /// wilderness, often banding together as bandits, but sometimes
  /// serving as guides, as trailblazers, or as companions to a
  /// ranger or barbarian warrior."
  ///
  /// Coverage (0.29.0): vanilla WOTR ships EIGHT rogue archetypes -
  /// Eldritch Scoundrel, Knife Master, Thug, Master of All, Rowdy,
  /// Sylvan Trickster, Underground Chemist, and Dark Lurker (the
  /// Through the Ashes/Lord of Nothing DLC's). TTT, COP and
  /// DarkCodex add none. The Scout (APG) was unclaimed - the rogue
  /// of the open road, the perfect companion piece to the Guide and
  /// the Wildbond.
  ///
  /// The kit (tabletop -> WOTR, both abilities verbatim-faithful):
  /// - Scout's Charge (4th, replaces uncanny dodge): whenever the
  ///   scout makes a CHARGE, the attack deals sneak attack damage
  ///   as if the target were flat-footed. Implementation: an
  ///   initiator-side RuleAttackRoll rider sets evt.IsSneakAttack
  ///   (the engine's own flag - TTT's RuleAttackWithWeaponPrecision
  ///   constructs its rolls with IsSneakAttack for exactly this
  ///   purpose) when the parent weapon-attack rule IsCharge (the
  ///   ShiningKnight/Wildbond charge idiom). SNEAK ONLY - not full
  ///   flat-footed - per the APG text ("deals sneak attack damage
  ///   as if the target were flat-footed").
  /// - Skirmisher (8th, replaces improved uncanny dodge): whenever
  ///   the scout moves more than 10 feet in a round and makes an
  ///   attack, the attack deals sneak attack damage as if the
  ///   target were flat-footed - only the FIRST attack of the
  ///   round. Implementation: the round tick captures her position
  ///   (the Wildbond mastodon-momentum idiom); the rider checks
  ///   displacement > 10 feet.
  ///
  /// Both abilities respect the tabletop's immunity clause: FOES
  /// WITH UNCANNY DODGE ARE IMMUNE (checked against the vanilla
  /// UncannyDodge feature).
  ///
  /// Documented adaptations (honest, not faked):
  /// - "moves more than 10 feet" is measured as NET DISPLACEMENT
  ///   from her position at the round tick (a scout who runs 30
  ///   feet and returns has net zero); WOTR exposes no
  ///   distance-traveled accumulator.
  /// - "first attack of the turn" resets on the ROUND TICK (the
  ///   engine's per-unit round boundary), not the attack counter.
  /// - Everything else in the rogue's kit - sneak attack, trap
  ///   finding, danger sense, debilitating injuries, talents,
  ///   master strike - is untouched.
  /// Log prefix: [scout].
  /// </summary>
  internal static class Scout
  {
    internal const string ArchetypeName = "ScoutArchetype";

    public static void Configure()
    {
      var rogue = CharacterClassRefs.RogueClass.Reference.Get();
      var uncannyDodge = FeatureRefs.UncannyDodge.Reference.Get();

      // ----- Scout's Charge (4th) -----
      var charge = FeatureConfigurator.New(
        "ScoutsChargeFeature", Guids.ScoutsChargeFeature)
        .SetDisplayName("ScoutsCharge.Name")
        .SetDescription("ScoutsCharge.Description")
        .SetIcon(AbilityRefs.LeadBlades.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new ScoutChargeComponent { UncannyDodge = uncannyDodge })
        .Configure();

      // ----- Skirmisher (8th) -----
      var skirmisher = FeatureConfigurator.New(
        "ScoutSkirmisherFeature", Guids.ScoutSkirmisherFeature)
        .SetDisplayName("ScoutSkirmisher.Name")
        .SetDescription("ScoutSkirmisher.Description")
        .SetIcon(AbilityRefs.Longstrider.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new ScoutSkirmisherComponent { UncannyDodge = uncannyDodge })
        .Configure();

      // ----- The archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.ScoutArchetype, CharacterClassRefs.RogueClass)
          .SetLocalizedName("Scout.Name")
          .SetLocalizedDescription("Scout.Description")
          .AddToAddFeatures(LevelPlan.L(4), charge)
          .AddToAddFeatures(LevelPlan.L(8), skirmisher);

      archetype = ArchetypeRemovals.AddRemovals(
        archetype, rogue,
        FeatureRefs.UncannyDodge.ToString(),
        FeatureRefs.ImprovedUncannyDodge.ToString());

      archetype.Configure();
      MissionFeats.Logger.Info("[scout] configured: " + ArchetypeName + ".");
    }
  }

  /// <summary>
  /// Scout's Charge: a charge attack counts as a sneak attack - the
  /// engine's IsSneakAttack flag on the attack roll (the TTT
  /// RuleAttackWithWeaponPrecision idiom), sneak-only per the APG
  /// text. Foes with uncanny dodge are immune (the tabletop's
  /// clause).
  /// </summary>
  [TypeId(Guids.ScoutChargeComponent)]
  internal class ScoutChargeComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintFeature UncannyDodge;

    public void OnEventAboutToTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.IsFake)
        {
          return;
        }
        if (evt.RuleAttackWithWeapon?.IsCharge != true)
        {
          return; // charges only
        }
        if (UncannyDodge is not null && evt.Target.HasFact(UncannyDodge))
        {
          return; // the tabletop's immunity clause
        }
        evt.IsSneakAttack = true;
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[scout] charge rider failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
  }

  /// <summary>
  /// Skirmisher: after moving more than 10 feet this round, her
  /// FIRST attack counts as a sneak attack. Position captured on
  /// the round tick (net displacement - documented adaptation);
  /// the first-attack gate resets with it. Foes with uncanny dodge
  /// are immune.
  /// </summary>
  [TypeId(Guids.ScoutSkirmisherComponent)]
  internal class ScoutSkirmisherComponent : UnitFactComponentDelegate,
    Kingmaker.Controllers.Units.ITickEachRound,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintFeature UncannyDodge;

    private Vector3 roundStart;
    private bool spent;

    public void OnNewRound()
    {
      roundStart = Owner.Position;
      spent = false;
    }

    protected override void OnActivate()
    {
      roundStart = Owner.Position;
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
        if (Vector3.Distance(roundStart, Owner.Position) <= 10.Feet().Meters)
        {
          return; // she has not moved far enough
        }
        if (UncannyDodge is not null && evt.Target.HasFact(UncannyDodge))
        {
          return; // the tabletop's immunity clause
        }
        evt.IsSneakAttack = true;
        spent = true; // the first attack only
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[scout] skirmisher rider failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
  }
}

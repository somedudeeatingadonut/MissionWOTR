using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.UnitLogic;
using Kingmaker.Enums;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.51.0 — the Beastbound (shifter homebrew, this mod's own design).
  /// The user's concept, verbatim: "one where the shifter themselves
  /// arent the ones shifting, but they have an animal companion that
  /// does."
  ///
  /// THE CONCEPT: the wild never lived in her. She is the still point
  /// at the center of the storm - the watcher, the keeper, the one who
  /// remembers the shapes. The shifting lives in her BEAST: a real
  /// animal companion from the game's own menagerie, who carries every
  /// aspect she would have taken and becomes the storm in her stead.
  ///
  /// THE GAIN - all of it real engine content:
  /// - Bond of the Wild (1st): a real animal companion - the game's own
  ///   companion selection, the whole vanilla menagerie, with the
  ///   engine's own AddPet internals handling spawn, control, and
  ///   leveling (the rank feature re-granted every shifter level, the
  ///   druid idiom). Player-controlled, per the session's standing
  ///   rule - and the vanilla companion system is player control.
  /// - Aspect Link (1st, 5th, 9th, 13th, 17th): a selection of twelve
  ///   link features, one per real animal aspect (Bear, Boar, Dinosaur,
  ///   Elephant, Griffon, Horse, Lizard, Manticore, Spider, Tiger,
  ///   Wolf, Wolverine). Each link uses the engine's own AddFactsToPet
  ///   to grant the REAL shifter aspect feature to her companion: the
  ///   minor-form passive always on, the major form on the pet's own
  ///   ability bar - the pet shifts, not her.
  ///
  /// THE TRADE (live-progression scans): her own aspect selection
  /// (every grant - the animal aspects and their major forms leave HER
  /// and go to the beast), the entire shifter claws line (the beast has
  /// the claws), and chimeric aspect + greater chimeric aspect (the
  /// beast's growing collection of linked aspects stands in for them -
  /// documented). Defensive instinct, wild empathy, track, and woodland
  /// stride stay: she is still a creature of the wild - just the fixed
  /// point of it.
  ///
  /// ENGINE NOTES: AddFactsToPet is the engine's own component for
  /// pushing facts onto a unit's pets (filtered by pet type); the real
  /// aspect features carry their own minor-form bonuses and major-form
  /// abilities, and player-controlled companions have their own ability
  /// bars to use them from. The companion rank re-grant loop is the
  /// druid/Corpse-Bond idiom.
  /// </summary>
  internal class Beastbound
  {
    internal const string ArchetypeName = "Beastbound";

    internal static void Configure()
    {
      var shifter = CharacterClassRefs.ShifterClass.Reference.Get();

      // ----- The twelve real aspects, linked. -----
      var links = new[]
      {
        new { Key = "Bear", Ref = FeatureRefs.ShifterAspectBear, Shape = FeatureRefs.ShifterWildShapeBearFeature },
        new { Key = "Boar", Ref = FeatureRefs.ShifterAspectBoar, Shape = FeatureRefs.ShifterWildShapeBoarFeature },
        new { Key = "Dinosaur", Ref = FeatureRefs.ShifterAspectDinosaur, Shape = FeatureRefs.ShifterWildShapeDinosaurFeature },
        new { Key = "Elephant", Ref = FeatureRefs.ShifterAspectElephant, Shape = FeatureRefs.ShifterWildShapeElephantFeature },
        new { Key = "Griffon", Ref = FeatureRefs.ShifterAspectGriffon, Shape = FeatureRefs.ShifterWildShapeGriffonFeature },
        new { Key = "Horse", Ref = FeatureRefs.ShifterAspectHorse, Shape = FeatureRefs.ShifterWildShapeHorseFeature },
        new { Key = "Lizard", Ref = FeatureRefs.ShifterAspectLizard, Shape = FeatureRefs.ShifterWildShapeLizardFeature },
        new { Key = "Manticore", Ref = FeatureRefs.ShifterAspectManticore, Shape = FeatureRefs.ShifterWildShapeManticoreFeature },
        new { Key = "Spider", Ref = FeatureRefs.ShifterAspectSpider, Shape = FeatureRefs.ShifterWildShapeSpiderFeature },
        new { Key = "Tiger", Ref = FeatureRefs.ShifterAspectTiger, Shape = FeatureRefs.ShifterWildShapeTigerFeature },
        new { Key = "Wolf", Ref = FeatureRefs.ShifterAspectWolf, Shape = FeatureRefs.ShifterWildShapeWolfFeature },
        new { Key = "Wolverine", Ref = FeatureRefs.ShifterAspectWolverine, Shape = FeatureRefs.ShifterWildShapeWolverineFeature },
      };
      var linkGuids = new[]
      {
        Guids.BeastboundLinkBear, Guids.BeastboundLinkBoar, Guids.BeastboundLinkDinosaur,
        Guids.BeastboundLinkElephant, Guids.BeastboundLinkGriffon, Guids.BeastboundLinkHorse,
        Guids.BeastboundLinkLizard, Guids.BeastboundLinkManticore, Guids.BeastboundLinkSpider,
        Guids.BeastboundLinkTiger, Guids.BeastboundLinkWolf, Guids.BeastboundLinkWolverine,
      };
      for (var i = 0; i < links.Length; i++)
      {
        var link = links[i];
        // The link itself: the engine's own AddFactsToPet pushes the
        // REAL aspect feature onto her companion. The aspect carries
        // its own minor form (passive) and major form (the pet's
        // ability to shift) - all vanilla content, doing the shifting.
        FeatureConfigurator.New("BeastboundLink" + link.Key + "Feature", linkGuids[i])
          .SetDisplayName("BeastboundLink.Name")
          .SetDescription("BeastboundLink.Description")
          .SetIcon(link.Ref.Reference.Get().Icon)
          .SetIsClassFeature()
          .AddFactsToPet(
            petType: PetType.AnimalCompanion,
            facts: new List<Blueprint<BlueprintUnitFactReference>>
            {
              // The real aspect (minor form) AND the real major form -
              // 0.52.0: the shape feature too, so the beast can actually
              // shift (the base class's wild-shape progression never
              // reaches a pet on its own).
              link.Ref.Cast<BlueprintUnitFactReference>(),
              link.Shape.Cast<BlueprintUnitFactReference>(),
            })
          // 0.52.0: the linked bonus - the beast grows into the shape,
          // scaling with the MASTER's shifter level (the pet has none).
          .AddComponent(new BeastboundLinkRider
          {
            ShifterClass = shifter,
          })
          .Configure();
      }

      // ----- Aspect Link: the selection, five picks. -----
      var aspectLink = FeatureSelectionConfigurator.New(
        "BeastboundAspectLinkSelection", Guids.BeastboundAspectLinkSelection)
        .SetDisplayName("BeastboundAspectLink.Name")
        .SetDescription("BeastboundAspectLink.Description")
        .SetIcon(FeatureRefs.ShifterAspectBear.Reference.Get().Icon)
        .SetIsClassFeature()
        .SetMode(SelectionMode.Default)
        .Configure();
      FeatureSelectionConfigurator.For(aspectLink)
        .AddToAllFeatures(linkGuids[0], linkGuids[1], linkGuids[2], linkGuids[3],
          linkGuids[4], linkGuids[5], linkGuids[6], linkGuids[7],
          linkGuids[8], linkGuids[9], linkGuids[10], linkGuids[11])
        .Configure();

      // ----- The archetype. -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.BeastboundArchetype, CharacterClassRefs.ShifterClass)
          .SetLocalizedName("Beastbound.Name")
          .SetLocalizedDescription("Beastbound.Description")
          // Bond of the Wild (1st): the real companion menagerie.
          .AddToAddFeatures(LevelPlan.L(1),
            FeatureSelectionRefs.AnimalCompanionSelectionBase.Cast<BlueprintFeatureBaseReference>())
          // Aspect Link: five picks on the shifter's aspect cadence.
          .AddToAddFeatures(LevelPlan.L(1), aspectLink)
          .AddToAddFeatures(LevelPlan.L(5), aspectLink)
          .AddToAddFeatures(LevelPlan.L(9), aspectLink)
          .AddToAddFeatures(LevelPlan.L(13), aspectLink)
          .AddToAddFeatures(LevelPlan.L(17), aspectLink);
      // The companion's rank: re-granted at every shifter level (the
      // druid idiom) so the beast scales with her.
      var rankRef = FeatureRefs.AnimalCompanionRank.Cast<BlueprintFeatureBaseReference>();
      for (var level = 1; level <= 20; level++)
      {
        archetype = archetype.AddToAddFeatures(LevelPlan.L(level), rankRef);
      }
      // The trades: HER aspects (every grant - they go to the beast),
      // the claws line (the beast has the claws), and the chimeric
      // line (the beast's many linked aspects stand in - documented).
      archetype = ArchetypeRemovals.RemoveEveryGrant(
        archetype, shifter, FeatureSelectionRefs.ShifterAspectSelectionFeature.ToString());
      var clawsNames = new[]
      {
        "ShifterClawsFeatureLevel1", "ShifterClawsFeatureLevel11", "ShifterClawsFeatureLevel13",
        "ShifterClawsFeatureLevel17", "ShifterClawsFeatureLevel19", "ShifterClawsFeatureAddLevel",
        "ShifterClawsFeatureAddLevel1", "ShifterClawsFeatureAddLevel2", "ShifterClawsFeatureAddLevel3",
        "ShifterClawsFeatureAddLevel4", "ShifterClawsFeatureAddLevel5", "ShifterClawsFeatureAddLevel6",
      };
      foreach (var claws in clawsNames)
      {
        archetype = ArchetypeRemovals.RemoveEveryGrant(archetype, shifter, claws);
      }
      archetype = ArchetypeRemovals.RemoveEveryGrant(
        archetype, shifter, FeatureRefs.ChimericAspectFeature.ToString());
      archetype = ArchetypeRemovals.RemoveEveryGrant(
        archetype, shifter, FeatureRefs.GreaterChimericAspectFeature.ToString());
      archetype.Configure();
      MissionFeats.Logger.Info("[beastbound] configured: " + ArchetypeName + ".");
    }
  }

  /// <summary>
  /// The linked bonus (0.52.0 bug-hunt fix): the vanilla aspect's
  /// minor form ranks off SHIFTER levels the pet does not have, and
  /// its major form lives in class-progression features that never
  /// reach a pet. The link therefore also grants the real wild-shape
  /// feature (done in the feature above), and this rider scales a
  /// morale bonus on the beast with the MASTER's shifter level: +2
  /// attack and damage, +3 at 8th, +4 at 15th. The modifiers are
  /// removed-then-reapplied each refresh so they never stack.
  /// </summary>
  [TypeId(Guids.BeastboundLinkRider)]
  internal class BeastboundLinkRider : UnitFactComponentDelegate, ITickEachRound
  {
    public BlueprintCharacterClass ShifterClass;

    protected override void OnActivate() => Refresh();

    public void OnNewRound() => Refresh();

    protected override void OnDeactivate()
    {
      try
      {
        foreach (var pet in Owner.Pets)
        {
          var stats = pet.Entity?.Descriptor.Stats;
          if (stats is null)
          {
            continue;
          }
          stats.AdditionalAttackBonus.RemoveModifiersFrom(Fact);
          stats.AdditionalDamage.RemoveModifiersFrom(Fact);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[beastbound] link release failed.", e);
      }
    }

    private void Refresh()
    {
      try
      {
        var level = ShifterClass is null ? 1 :
          Owner.Descriptor.Progression.GetClassLevel(ShifterClass);
        var tier = level >= 15 ? 4 : level >= 8 ? 3 : 2;
        foreach (var pet in Owner.Pets)
        {
          var stats = pet.Entity?.Descriptor.Stats;
          if (stats is null)
          {
            continue;
          }
          stats.AdditionalAttackBonus.RemoveModifiersFrom(Fact);
          stats.AdditionalDamage.RemoveModifiersFrom(Fact);
          stats.AdditionalAttackBonus.AddModifierUnique(tier, Fact, ModifierDescriptor.Morale);
          stats.AdditionalDamage.AddModifierUnique(tier, Fact, ModifierDescriptor.Morale);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[beastbound] link refresh failed.", e);
      }
    }
  }
}

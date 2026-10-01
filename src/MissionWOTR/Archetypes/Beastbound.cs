using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Enums;
using MissionWOTR.Feats;
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
        new { Key = "Bear", Ref = FeatureRefs.ShifterAspectBear },
        new { Key = "Boar", Ref = FeatureRefs.ShifterAspectBoar },
        new { Key = "Dinosaur", Ref = FeatureRefs.ShifterAspectDinosaur },
        new { Key = "Elephant", Ref = FeatureRefs.ShifterAspectElephant },
        new { Key = "Griffon", Ref = FeatureRefs.ShifterAspectGriffon },
        new { Key = "Horse", Ref = FeatureRefs.ShifterAspectHorse },
        new { Key = "Lizard", Ref = FeatureRefs.ShifterAspectLizard },
        new { Key = "Manticore", Ref = FeatureRefs.ShifterAspectManticore },
        new { Key = "Spider", Ref = FeatureRefs.ShifterAspectSpider },
        new { Key = "Tiger", Ref = FeatureRefs.ShifterAspectTiger },
        new { Key = "Wolf", Ref = FeatureRefs.ShifterAspectWolf },
        new { Key = "Wolverine", Ref = FeatureRefs.ShifterAspectWolverine },
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
              link.Ref.Cast<BlueprintUnitFactReference>(),
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
}

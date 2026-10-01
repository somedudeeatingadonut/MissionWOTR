using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using MissionWOTR.Feats;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.44.0 — the Sacred Fist (warpriest archetype, Advanced Class Guide
  /// pg. 130). The replacement warpriest port after the user's catch:
  /// the 0.43.0 Champion of the Faith duplicated a VANILLA archetype —
  /// Owlcat names archetype blueprints after the archetype, not the
  /// class ("ChampionOfTheFaithArchetype" contains no "Warpriest"), so
  /// the trap-check grep for the class name was structurally blind.
  /// Lesson recorded; that archetype is withdrawn. The vanilla warpriest
  /// ships SEVEN archetypes (Champion of the Faith, Cult Leader,
  /// Disenchanter, Feral Champion, Mantis Zealot, Proclaimer,
  /// Shieldbearer); the Sacred Fist is genuinely absent.
  ///
  /// "Unlike many warpriests, sacred fists leave behind armor and shield
  /// and instead rely on their fists and whatever protection their deity
  /// bestows on them."
  ///
  /// THE TRADES (live-progression scans):
  /// - weapon and armor proficiencies (the monk's weapon package comes
  ///   instead: no armor, no shields);
  /// - focus weapon (the 1st-level weapon focus selection);
  /// - sacred weapon — EVERY grant (flurry of blows replaces it whole);
  /// - the bonus feats at 3rd (Blessed Fortitude) and 6th/12th/18th
  ///   (the style-feat line).
  ///
  /// THE GAIN — all of it REAL MONK CONTENT, granted as-is (the lowest
  /// cut this mod has shipped):
  /// - AC Bonus (1st): the monk's own MonkACBonus — Wisdom to AC and
  ///   CMD plus the scaling dodge bonus.
  /// - Flurry of Blows (1st): FlurryOfBlows + the game's own
  ///   non-monk flurry scaling unlock (ImitationMonkFlurryUnlock), and
  ///   the level-11 extra attack.
  /// - Unarmed Strike (1st): ImprovedUnarmedStrike + the monk's
  ///   MonkUnarmedStrike damage scaling.
  /// - Blessed Fortitude (3rd): documented substitute — the tabletop's
  ///   "avoid the effect entirely on a successful save" is not exposed
  ///   to mods; +2 sacred bonus on Fortitude saves instead.
  /// - Bonus style feats (6th/12th/18th): FighterFeatSelection (style
  ///   feats are combat feats; no separate style selection exists) —
  ///   documented substitute.
  /// </summary>
  internal class SacredFist
  {
    internal const string ArchetypeName = "SacredFist";

    internal static void Configure()
    {
      var warpriest = CharacterClassRefs.WarpriestClass.Reference.Get();

      // ----- Blessed Fortitude (3rd): the documented substitute. -----
      var blessedFortitude = FeatureConfigurator.New(
        "SacredFistBlessedFortitudeFeature", Guids.SacredFistBlessedFortitudeFeature)
        .SetDisplayName("SacredFistBlessedFortitude.Name")
        .SetDescription("SacredFistBlessedFortitude.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.SaveFortitude, value: 2,
          descriptor: ModifierDescriptor.Sacred)
        .Configure();

      // ----- The archetype: the monk package, granted as-is. -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.SacredFistArchetype, CharacterClassRefs.WarpriestClass)
          .SetLocalizedName("SacredFist.Name")
          .SetLocalizedDescription("SacredFist.Description")
          .AddToAddFeatures(LevelPlan.L(1),
            FeatureRefs.MonkWeaponProficiency.Cast<BlueprintFeatureBaseReference>())
          .AddToAddFeatures(LevelPlan.L(1),
            FeatureRefs.MonkACBonus.Cast<BlueprintFeatureBaseReference>())
          .AddToAddFeatures(LevelPlan.L(1),
            FeatureRefs.FlurryOfBlows.Cast<BlueprintFeatureBaseReference>())
          .AddToAddFeatures(LevelPlan.L(1),
            FeatureRefs.ImitationMonkFlurryUnlock.Cast<BlueprintFeatureBaseReference>())
          .AddToAddFeatures(LevelPlan.L(1),
            FeatureRefs.ImprovedUnarmedStrike.Cast<BlueprintFeatureBaseReference>())
          .AddToAddFeatures(LevelPlan.L(1),
            FeatureRefs.MonkUnarmedStrike.Cast<BlueprintFeatureBaseReference>())
          .AddToAddFeatures(LevelPlan.L(1),
            FeatureRefs.MonkUnarmedStrikeLevel1.Cast<BlueprintFeatureBaseReference>())
          .AddToAddFeatures(LevelPlan.L(3), blessedFortitude)
          .AddToAddFeatures(LevelPlan.L(6),
            FeatureSelectionRefs.FighterFeatSelection.Cast<BlueprintFeatureBaseReference>())
          .AddToAddFeatures(LevelPlan.L(11),
            FeatureRefs.FlurryOfBlowsLevel11.Cast<BlueprintFeatureBaseReference>())
          .AddToAddFeatures(LevelPlan.L(12),
            FeatureSelectionRefs.FighterFeatSelection.Cast<BlueprintFeatureBaseReference>())
          .AddToAddFeatures(LevelPlan.L(18),
            FeatureSelectionRefs.FighterFeatSelection.Cast<BlueprintFeatureBaseReference>());
      // The trades.
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, warpriest,
        "WarpriestProficiencies",
        FeatureSelectionRefs.WarpriestWeaponFocusSelection.ToString());
      archetype = ArchetypeRemovals.RemoveEveryGrant(archetype, warpriest, "SacredWeapon");
      archetype = ArchetypeRemovals.RemoveAtLevel(
        archetype, warpriest.Progression, 3,
        FeatureSelectionRefs.WarpriestFeatSelection.ToString());
      archetype = ArchetypeRemovals.RemoveAtLevel(
        archetype, warpriest.Progression, 6,
        FeatureSelectionRefs.WarpriestFeatSelection.ToString());
      archetype = ArchetypeRemovals.RemoveAtLevel(
        archetype, warpriest.Progression, 12,
        FeatureSelectionRefs.WarpriestFeatSelection.ToString());
      archetype = ArchetypeRemovals.RemoveAtLevel(
        archetype, warpriest.Progression, 18,
        FeatureSelectionRefs.WarpriestFeatSelection.ToString());
      archetype.Configure();
      MissionFeats.Logger.Info("[sacredfist] configured: " + ArchetypeName + ".");
    }
  }
}

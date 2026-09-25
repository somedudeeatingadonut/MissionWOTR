using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Enums;
using MissionWOTR.Feats;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Exoticist (homebrew fighter archetype, user design: "a fighter that
  /// specializes in exotic weapons specifically" - deliberately simple).
  ///
  /// - Exotic Arsenal (1st): proficiency with every exotic weapon in the
  ///   game (the vanilla AddProficiencies component with the full exotic
  ///   category list).
  /// - Exotic Training (5th, replaces weapon training 1-4): +1 on attack and
  ///   damage rolls with exotic weapons while one is wielded, +1 per four
  ///   levels beyond 5th (+4 at 17th) - the shared WeaponSpecialistBonus
  ///   component, the same engine as the Polearm Master's training.
  ///
  /// Everything else about the fighter is untouched - the whole archetype is
  /// one broad door (all exotic weapons open) and one narrow specialization
  /// (only exotic weapons get the training).
  /// Log prefix: [removals] carries the trade diagnostics.
  /// </summary>
  internal static class Exoticist
  {
    internal const string ArchetypeName = "ExoticistArchetype";
    internal const string ArsenalName = "ExoticistExoticArsenal";
    internal const string TrainingName = "ExoticistExoticTraining";

    /// <summary>The game's exotic weapon family.</summary>
    internal static readonly WeaponCategory[] Exotics =
    {
      WeaponCategory.BastardSword,
      WeaponCategory.DwarvenWaraxe,
      WeaponCategory.DuelingSword,
      WeaponCategory.SawtoothSabre,
      WeaponCategory.Estoc,
      WeaponCategory.Falcata,
      WeaponCategory.Fauchard,
      WeaponCategory.HeavyRepeatingCrossbow,
      WeaponCategory.LightRepeatingCrossbow,
      WeaponCategory.Shuriken,
      WeaponCategory.SlingStaff,
      WeaponCategory.Sai,
      WeaponCategory.Kama,
      WeaponCategory.Nunchaku,
      WeaponCategory.Siangham,
    };

    public static void Configure()
    {
      var fighter = CharacterClassRefs.FighterClass.Reference.Get();
      var icon = FeatureRefs.WeaponTrainingAxes.Reference.Get().Icon;

      // ----- Exotic Arsenal (1st) -----
      var arsenal = FeatureConfigurator.New(ArsenalName, Guids.ExoticArsenal)
        .SetDisplayName(ArsenalName + ".Name")
        .SetDescription(ArsenalName + ".Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddProficiencies(weaponProficiencies: Exotics)
        .Configure();

      // ----- Exotic Training (5th) -----
      var training = FeatureConfigurator.New(TrainingName, Guids.ExoticTraining)
        .SetDisplayName(TrainingName + ".Name")
        .SetDescription(TrainingName + ".Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new WeaponSpecialistBonus
        {
          Class = fighter,
          Categories = Exotics,
        })
        .Configure();

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.ExoticistArchetype, CharacterClassRefs.FighterClass)
          .SetLocalizedName("Exoticist.Name")
          .SetLocalizedDescription("Exoticist.Description")
          .AddToAddFeatures(LevelPlan.L(1), arsenal)
          .AddToAddFeatures(LevelPlan.L(5), training);

      // Trades: weapon training, all four instances (the selection and its
      // rank-ups). Bravery and armor training stay - the exoticist is a
      // simple archetype by design.
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, fighter,
        "b8cecf4e5e464ad41b79d5b42b76b399", // WeaponTrainingSelection
        "5f3cc7b9a46b880448275763fe70c0b0"); // WeaponTrainingRankUpSelection

      archetype.Configure();

      MissionFeats.Logger.Info("Exoticist: configured.");
    }
  }
}

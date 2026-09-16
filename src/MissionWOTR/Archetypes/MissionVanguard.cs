using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using MissionWOTR.Feats;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Mission Vanguard
  /// A Magus archetype that currently serves as the mod's test harness: it grants every
  /// Mission WOTR feat - along with the vanilla feats they are normally gated behind - at
  /// level 1, so all of the mod's mechanics can be exercised from the very first fight
  /// without meeting the normal prerequisites. Features granted through an archetype
  /// bypass prerequisite checks.
  ///
  /// It intentionally removes nothing from the base Magus. The plan is to expand this into
  /// a full class later, so its GUID is permanent (see Guids.cs).
  /// </summary>
  public class MissionVanguard
  {
    internal const string ArchetypeName = "MissionVanguard";
    internal const string DisplayName = "MissionVanguard.Name";
    internal const string Description = "MissionVanguard.Description";

    internal static void Configure()
    {
      ArchetypeConfigurator.New(ArchetypeName, Guids.MissionVanguardArchetype, CharacterClassRefs.MagusClass)
        .SetLocalizedName(DisplayName)
        .SetLocalizedDescription(Description)
        .AddToAddFeatures(1,
          // All Mission WOTR feats...
          VengefulCounterstrike.FeatName,
          ArcaneMomentum.FeatName,
          BattlefieldScavenger.FeatName,
          SecondWind.FeatName,
          TauntingBlows.FeatName,
          ResonantStrikes.FeatName,
          WardedSoul.FeatName,
          // ...plus the vanilla feats they normally require, so every one of them is
          // immediately usable (e.g. Taunting Blows needs Power Attack's stance active).
          FeatureRefs.PowerAttackFeature.ToString(),
          FeatureRefs.PointBlankShot.ToString(),
          FeatureRefs.ImprovedUnarmedStrike.ToString(),
          FeatureRefs.IronWill.ToString(),
          FeatureRefs.Endurance.ToString(),
          FeatureRefs.CombatReflexes.ToString())
        .Configure(delayed: true);
    }
  }
}

using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;

namespace MissionWOTR.Mythics
{
  /// <summary>
  /// Aegis of Legend (original mythic ability)
  /// Mythic power hardens around you: +1 dodge bonus to AC for every 2 mythic ranks
  /// (minimum +1).
  /// </summary>
  internal static class AegisOfLegend
  {
    internal const string FeatName = "AegisOfLegend";
    internal const string DisplayName = "AegisOfLegend.Name";
    internal const string Description = "AegisOfLegend.Description";

    internal static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.AegisOfLegendAbility)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.Dodge.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddContextStatBonus(StatType.AC, ContextValues.Rank(), ModifierDescriptor.Dodge)
        .AddContextRankConfig(ContextRankConfigs.MythicLevel(min: 1).WithDiv2Progression())
        .AddToFeatureSelection(
          FeatureSelectionRefs.MythicAbilitySelection.Cast<BlueprintFeatureSelectionReference>(),
          FeatureSelectionRefs.ExtraMythicAbilityMythicFeat.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }
  }
}

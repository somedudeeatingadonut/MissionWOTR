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
  /// Untouchable (original mythic ability; formerly "Aegis of Legend")
  /// Attacks simply fail to find you: +1 dodge bonus to AC for every 2 mythic ranks
  /// (minimum +1).
  /// </summary>
  internal static class Untouchable
  {
    internal const string FeatName = "Untouchable";
    internal const string DisplayName = "Untouchable.Name";
    internal const string Description = "Untouchable.Description";

    internal static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.UntouchableAbility)
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

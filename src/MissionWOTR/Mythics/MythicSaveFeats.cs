using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;

namespace MissionWOTR.Mythics
{
  /// <summary>
  /// Iron Will (Mythic) - tabletop name, Wrath-style effect (the base game only has a mythic
  /// version of Great Fortitude; this one is absent).
  /// Benefit: +1 insight bonus on Will saves per 2 mythic ranks (minimum +2).
  /// Requires Iron Will.
  /// </summary>
  internal static class IronWillMythic
  {
    internal const string FeatName = "IronWillMythic";
    internal const string DisplayName = "IronWillMythic.Name";
    internal const string Description = "IronWillMythic.Description";

    internal static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.IronWillMythicFeat, FeatureGroup.MythicFeat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.IronWill.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddPrerequisiteFeature(FeatureRefs.IronWill.ToString())
        .AddContextStatBonus(StatType.SaveWill, ContextValues.Rank(), ModifierDescriptor.Insight)
        .AddContextRankConfig(ContextRankConfigs.MythicLevel(min: 2).WithDiv2Progression())
        .AddToFeatureSelection(FeatureSelectionRefs.MythicFeatSelection.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }
  }

  /// <summary>
  /// Lightning Reflexes (Mythic) - tabletop name, absent from the base game.
  /// Benefit: +1 insight bonus on Reflex saves per 2 mythic ranks (minimum +2).
  /// Requires Lightning Reflexes.
  /// </summary>
  internal static class LightningReflexesMythic
  {
    internal const string FeatName = "LightningReflexesMythic";
    internal const string DisplayName = "LightningReflexesMythic.Name";
    internal const string Description = "LightningReflexesMythic.Description";

    internal static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.LightningReflexesMythicFeat, FeatureGroup.MythicFeat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.LightningReflexes.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddPrerequisiteFeature(FeatureRefs.LightningReflexes.ToString())
        .AddContextStatBonus(StatType.SaveReflex, ContextValues.Rank(), ModifierDescriptor.Insight)
        .AddContextRankConfig(ContextRankConfigs.MythicLevel(min: 2).WithDiv2Progression())
        .AddToFeatureSelection(FeatureSelectionRefs.MythicFeatSelection.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }
  }

  /// <summary>
  /// Endurance (Mythic) - tabletop name, absent from the base game.
  /// Benefit: +1 insight bonus on Fortitude saves per 2 mythic ranks (minimum +2).
  /// Requires Endurance.
  /// </summary>
  internal static class EnduranceMythic
  {
    internal const string FeatName = "EnduranceMythic";
    internal const string DisplayName = "EnduranceMythic.Name";
    internal const string Description = "EnduranceMythic.Description";

    internal static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.EnduranceMythicFeat, FeatureGroup.MythicFeat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.Endurance.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddPrerequisiteFeature(FeatureRefs.Endurance.ToString())
        .AddContextStatBonus(StatType.SaveFortitude, ContextValues.Rank(), ModifierDescriptor.Insight)
        .AddContextRankConfig(ContextRankConfigs.MythicLevel(min: 2).WithDiv2Progression())
        .AddToFeatureSelection(FeatureSelectionRefs.MythicFeatSelection.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }
  }
}

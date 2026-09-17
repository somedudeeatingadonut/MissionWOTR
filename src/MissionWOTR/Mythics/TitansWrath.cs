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
  /// Titan's Wrath (original mythic ability)
  /// The strength of legend flows through every blow: you gain a +1 mythic bonus on
  /// weapon damage rolls per mythic rank.
  /// </summary>
  internal static class TitansWrath
  {
    internal const string FeatName = "TitansWrath";
    internal const string DisplayName = "TitansWrath.Name";
    internal const string Description = "TitansWrath.Description";

    internal static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.TitansWrathAbility)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.PowerAttackFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddContextStatBonus(StatType.AdditionalDamage, ContextValues.Rank(), ModifierDescriptor.Mythic)
        .AddContextRankConfig(ContextRankConfigs.MythicLevel(min: 1))
        .AddToFeatureSelection(
          FeatureSelectionRefs.MythicAbilitySelection.Cast<BlueprintFeatureSelectionReference>(),
          FeatureSelectionRefs.ExtraMythicAbilityMythicFeat.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }
  }

  /// <summary>
  /// Titan Hide (original mythic ability)
  /// Your flesh hardens like living stone: you gain a +1 natural armor bonus to AC for
  /// every 2 mythic ranks (minimum +1).
  /// </summary>
  internal static class TitanHide
  {
    internal const string FeatName = "TitanHide";
    internal const string DisplayName = "TitanHide.Name";
    internal const string Description = "TitanHide.Description";

    internal static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.TitanHideAbility)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(AbilityRefs.Barkskin.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddContextStatBonus(StatType.AC, ContextValues.Rank(), ModifierDescriptor.NaturalArmor)
        .AddContextRankConfig(ContextRankConfigs.MythicLevel(min: 1).WithDiv2Progression())
        .AddToFeatureSelection(
          FeatureSelectionRefs.MythicAbilitySelection.Cast<BlueprintFeatureSelectionReference>(),
          FeatureSelectionRefs.ExtraMythicAbilityMythicFeat.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }
  }
}

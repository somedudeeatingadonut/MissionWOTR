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
  /// Acrobatic (Mythic) - tabletop port, adapted for Wrath's merged skill list.
  /// Benefit: additional +2 on Mobility and Athletics checks.
  /// The base Acrobatic feat does not exist in Wrath, so there is no prerequisite; the
  /// tabletop's Acrobatics/Fly pair maps to Mobility/Athletics here.
  /// </summary>
  internal static class AcrobaticMythic
  {
    internal const string FeatName = "AcrobaticMythic";
    internal const string DisplayName = "AcrobaticMythic.Name";
    internal const string Description = "AcrobaticMythic.Description";

    internal static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.AcrobaticMythicFeat, FeatureGroup.MythicFeat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.Dodge.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddContextStatBonus(StatType.SkillMobility, ContextValues.Constant(2), ModifierDescriptor.UntypedStackable)
        .AddContextStatBonus(StatType.SkillAthletics, ContextValues.Constant(2), ModifierDescriptor.UntypedStackable)
        .AddToFeatureSelection(FeatureSelectionRefs.MythicFeatSelection.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }
  }

  /// <summary>
  /// Persuasive (Mythic) - tabletop port.
  /// Benefit: additional +2 on Persuasion checks. Requires Persuasive.
  /// </summary>
  internal static class PersuasiveMythic
  {
    internal const string FeatName = "PersuasiveMythic";
    internal const string DisplayName = "PersuasiveMythic.Name";
    internal const string Description = "PersuasiveMythic.Description";

    internal static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.PersuasiveMythicFeat, FeatureGroup.MythicFeat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.Persuasive.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddPrerequisiteFeature(FeatureRefs.Persuasive.ToString())
        .AddContextStatBonus(StatType.SkillPersuasion, ContextValues.Constant(2), ModifierDescriptor.UntypedStackable)
        .AddToFeatureSelection(FeatureSelectionRefs.MythicFeatSelection.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }
  }

  /// <summary>
  /// Magical Aptitude (Mythic) - tabletop port, adapted for Wrath's skill list.
  /// Benefit: additional +2 on Knowledge (Arcana) and Use Magic Device checks.
  /// The base Magical Aptitude feat does not exist in Wrath, so there is no prerequisite.
  /// </summary>
  internal static class MagicalAptitudeMythic
  {
    internal const string FeatName = "MagicalAptitudeMythic";
    internal const string DisplayName = "MagicalAptitudeMythic.Name";
    internal const string Description = "MagicalAptitudeMythic.Description";

    internal static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.MagicalAptitudeMythicFeat, FeatureGroup.MythicFeat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.SkillFocusKnowledgeArcana.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddContextStatBonus(StatType.SkillKnowledgeArcana, ContextValues.Constant(2), ModifierDescriptor.UntypedStackable)
        .AddContextStatBonus(StatType.SkillUseMagicDevice, ContextValues.Constant(2), ModifierDescriptor.UntypedStackable)
        .AddToFeatureSelection(FeatureSelectionRefs.MythicFeatSelection.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }
  }
}

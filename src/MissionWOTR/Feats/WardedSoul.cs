using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Blueprints.Classes.Selection;

namespace MissionWOTR.Feats
{
  /// <summary>
  /// Warded Soul
  /// Prerequisites: Iron Will.
  /// You were born under the wardstones of Kenabres, and their echo lingers in your blood.
  /// You gain a +1 luck bonus on Fortitude, Reflex, and Will saving throws.
  /// </summary>
  public class WardedSoul
  {
    internal const string FeatName = "WardedSoul";
    internal const string DisplayName = "WardedSoul.Name";
    internal const string Description = "WardedSoul.Description";

    internal static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.WardedSoulFeat, FeatureGroup.Feat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.IronWill.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFeatureTagsComponent(FeatureTag.Defense)
        .AddPrerequisiteFeature(FeatureRefs.IronWill.ToString())
        .AddContextStatBonus(StatType.SaveFortitude, ContextValues.Constant(1), ModifierDescriptor.Luck)
        .AddContextStatBonus(StatType.SaveReflex, ContextValues.Constant(1), ModifierDescriptor.Luck)
        .AddContextStatBonus(StatType.SaveWill, ContextValues.Constant(1), ModifierDescriptor.Luck)
        .Configure(delayed: true);
    }
  }
}

using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;
using System;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Buffs;

namespace MissionWOTR.Feats
{
  /// <summary>
  /// Second Wind
  /// Prerequisites: Endurance.
  /// Once per day, when a hit brings you to half your maximum hit points or below, you catch a
  /// second wind: you gain temporary hit points equal to your character level (minimum 2) and a
  /// +2 morale bonus on Fortitude saves for 1 minute. The daily use is restored on rest.
  /// </summary>
  public class SecondWind
  {
    internal const string FeatName = "SecondWind";
    internal const string DisplayName = "SecondWind.Name";
    internal const string Description = "SecondWind.Description";

    internal const string BuffName = "SecondWindBuff";
    internal const string BuffDisplayName = "SecondWind.Buff.Name";
    internal const string BuffDescription = "SecondWind.Buff.Description";

    internal const string ResourceName = "SecondWindResource";

    internal static void Configure()
    {
      var icon = FeatureRefs.Endurance.Reference.Get().Icon;

      // One use per day; refilled when the character rests.
      var resource =
        AbilityResourceConfigurator.New(ResourceName, Guids.SecondWindResource)
          .SetMin(0)
          .SetMax(1)
          .Configure();

      var buff = BuffConfigurator.New(BuffName, Guids.SecondWindBuff)
        .SetDisplayName(BuffDisplayName)
        .SetDescription(BuffDescription)
        .SetIcon(icon)
        // Temporary hit points equal to character level (minimum 2, see rank config below).
        .AddComponent<TemporaryHitPointsFromAbilityValue>(c =>
        {
          c.Value = ContextValues.Rank();
          c.RemoveWhenHitPointsEnd = true;
        })
        .AddContextRankConfig(ContextRankConfigs.CharacterLevel(min: 2))
        .AddContextStatBonus(StatType.SaveFortitude, ContextValues.Constant(2), ModifierDescriptor.Morale)
        .Configure();

      FeatureConfigurator.New(FeatName, Guids.SecondWindFeat, FeatureGroup.Feat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFeatureTagsComponent(FeatureTag.Defense)
        .AddPrerequisiteFeature(FeatureRefs.Endurance.ToString())
        .AddAbilityResources(resource: resource, restoreAmount: true)
        .AddComponent(new SecondWindTrigger(buff, resource))
        .Configure(delayed: true);
    }

    [TypeId(Guids.SecondWindTrigger)]
    private class SecondWindTrigger :
      UnitFactComponentDelegate, ITargetRulebookHandler<RuleDealDamage>
    {
      private readonly BlueprintBuff Buff;
      private readonly BlueprintAbilityResource Resource;

      public SecondWindTrigger(BlueprintBuff buff, BlueprintAbilityResource resource)
      {
        Buff = buff;
        Resource = resource;
      }

      public void OnEventAboutToTrigger(RuleDealDamage evt) { }

      public void OnEventDidTrigger(RuleDealDamage evt)
      {
        try
        {
          if (evt.Target != Owner)
          {
            return;
          }

          // Once per day: only trigger while we still have the resource.
          if (!Owner.Resources.HasEnoughResource(Resource, 1))
          {
            return;
          }

          // Only trigger when we are at or below half of our maximum hit points.
          if (Owner.HPLeft * 2 > Owner.Stats.HitPoints.ModifiedValue)
          {
            return;
          }

          Owner.Resources.Spend(Resource, 1);
          Owner.AddBuff(Buff, Context, duration: ContextDuration.Fixed(10).Calculate(Context).Seconds);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("SecondWind: failed to handle damage event.", e);
        }
      }
    }
  }
}

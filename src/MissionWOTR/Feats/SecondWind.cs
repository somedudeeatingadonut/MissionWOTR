using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
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

namespace MissionWOTR.Feats
{
  /// <summary>
  /// Second Wind
  /// Prerequisites: Endurance.
  /// Once per minute, when a hit brings you to half your maximum hit points or below, you
  /// catch a second wind: you gain temporary hit points equal to half your character level
  /// and a +2 morale bonus on Fortitude saves for 1 minute.
  /// </summary>
  public class SecondWind
  {
    internal const string FeatName = "SecondWind";
    internal const string DisplayName = "SecondWind.Name";
    internal const string Description = "SecondWind.Description";

    internal const string BuffName = "SecondWindBuff";
    internal const string BuffDisplayName = "SecondWind.Buff.Name";
    internal const string BuffDescription = "SecondWind.Buff.Description";

    internal static void Configure()
    {
      var icon = FeatureRefs.Endurance.Reference.Get().Icon;

      var buff = BuffConfigurator.New(BuffName, Guids.SecondWindBuff)
        .SetDisplayName(BuffDisplayName)
        .SetDescription(BuffDescription)
        .SetIcon(icon)
        // Temporary hit points equal to half character level (rank config below).
        // NOTE: verify exact component name against the game assembly (AddTemporaryHP vs
        // BuffAddTemporaryHP) on the first CI build.
        .AddTemporaryHP(ContextValues.Rank())
        .AddContextRankConfig(ContextRankConfigs.CharacterLevel(div: 2, minimum: 2))
        .AddContextStatBonus(StatType.SaveFortitude, ContextValues.Constant(2), ModifierDescriptor.Morale)
        .Configure();

      FeatureConfigurator.New(FeatName, Guids.SecondWindFeat, FeatureGroup.Feat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFeatureTagsComponent(FeatureTag.Defense)
        .AddPrerequisiteFeature(FeatureRefs.Endurance.ToString())
        .AddComponent(new SecondWindTrigger(buff))
        .Configure(delayed: true);
    }

    [TypeId(Guids.SecondWindTrigger)]
    private class SecondWindTrigger :
      UnitFactComponentDelegate, ITargetRulebookHandler<RuleDealDamage>
    {
      private readonly BlueprintBuff Buff;

      public SecondWindTrigger(BlueprintBuff buff)
      {
        Buff = buff;
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

          // At most once per minute: while the buff is running it cannot re-trigger.
          if (Owner.Buffs.GetBuff(Buff) != null)
          {
            return;
          }

          // Only trigger when we are at or below half of our maximum hit points.
          if (Owner.HPLeft * 2 > Owner.Stats.HitPoints.ModifiedValue)
          {
            return;
          }

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

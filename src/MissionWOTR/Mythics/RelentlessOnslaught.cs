using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Mythics
{
  /// <summary>
  /// Relentless Onslaught (original mythic ability)
  /// A kill today is momentum for the next: whenever you reduce an enemy to 0 hit points
  /// with a weapon attack, you gain a +1 insight bonus on attack rolls for 1 round per 2
  /// mythic ranks (minimum +2). Cannot be gained again while active.
  /// </summary>
  internal static class RelentlessOnslaught
  {
    internal const string FeatName = "RelentlessOnslaught";
    internal const string DisplayName = "RelentlessOnslaught.Name";
    internal const string Description = "RelentlessOnslaught.Description";
    internal const string BuffName = "RelentlessOnslaughtBuff";
    internal const string BuffDisplayName = "RelentlessOnslaught.Buff.Name";
    internal const string BuffDescription = "RelentlessOnslaught.Buff.Description";

    internal static void Configure()
    {
      var buff = BuffConfigurator.New(BuffName, Guids.RelentlessOnslaughtBuff)
        .SetDisplayName(BuffDisplayName)
        .SetDescription(BuffDescription)
        .SetIcon(FeatureRefs.PointBlankShot.Reference.Get().Icon)
        .AddContextStatBonus(
          StatType.AdditionalAttackBonus, ContextValues.Rank(), ModifierDescriptor.Insight)
        .AddContextRankConfig(ContextRankConfigs.MythicLevel(min: 2).WithDiv2Progression())
        .Configure();

      FeatureConfigurator.New(FeatName, Guids.RelentlessOnslaughtAbility)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.PointBlankShot.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new RelentlessOnslaughtTrigger(buff))
        .AddToFeatureSelection(
          FeatureSelectionRefs.MythicAbilitySelection.Cast<BlueprintFeatureSelectionReference>(),
          FeatureSelectionRefs.ExtraMythicAbilityMythicFeat.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }

    /// <summary>
    /// Watches for weapon damage the owner deals and awards the onslaught buff on a kill.
    /// </summary>
    [TypeId(Guids.RelentlessOnslaughtTrigger)]
    private class RelentlessOnslaughtTrigger :
      UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleDealDamage>
    {
      private readonly BlueprintBuff Buff;

      public RelentlessOnslaughtTrigger(BlueprintBuff buff)
      {
        Buff = buff;
      }

      public void OnEventAboutToTrigger(RuleDealDamage evt) { }

      public void OnEventDidTrigger(RuleDealDamage evt)
      {
        try
        {
          // Only weapon attacks...
          if (evt.Reason.Rule is not RuleAttackWithWeapon)
          {
            return;
          }
          // ...that finished the target.
          if (evt.Target is null || evt.Target.HPLeft > 0)
          {
            return;
          }
          // No refresh while already running.
          if (Owner.Buffs.GetBuff(Buff) != null)
          {
            return;
          }

          Owner.AddBuff(Buff, Context, duration: ContextDuration.Fixed(1).Calculate(Context).Seconds);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("RelentlessOnslaught: failed to handle damage event.", e);
        }
      }
    }
  }
}

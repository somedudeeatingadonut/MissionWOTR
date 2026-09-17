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
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Mythics
{
  /// <summary>
  /// Defiant Soul (original mythic ability)
  /// Every assault you shrug off stokes the legend: whenever you succeed at a saving
  /// throw against an effect created by someone else, you gain a +1 morale bonus on
  /// attack rolls for 1 round per 2 mythic ranks (minimum +2). Cannot be gained again
  /// while active.
  /// </summary>
  internal static class DefiantSoul
  {
    internal const string FeatName = "DefiantSoul";
    internal const string DisplayName = "DefiantSoul.Name";
    internal const string Description = "DefiantSoul.Description";
    internal const string BuffName = "DefiantSoulBuff";
    internal const string BuffDisplayName = "DefiantSoul.Buff.Name";
    internal const string BuffDescription = "DefiantSoul.Buff.Description";

    internal static void Configure()
    {
      var buff = BuffConfigurator.New(BuffName, Guids.DefiantSoulBuff)
        .SetDisplayName(BuffDisplayName)
        .SetDescription(BuffDescription)
        .SetIcon(FeatureRefs.IronWill.Reference.Get().Icon)
        .AddContextStatBonus(
          StatType.AdditionalAttackBonus, ContextValues.Rank(), ModifierDescriptor.Morale)
        .AddContextRankConfig(ContextRankConfigs.MythicLevel(min: 2).WithDiv2Progression())
        .Configure();

      FeatureConfigurator.New(FeatName, Guids.DefiantSoulAbility)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.IronWill.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new DefiantSoulTrigger(buff))
        .AddToFeatureSelection(
          FeatureSelectionRefs.MythicAbilitySelection.Cast<BlueprintFeatureSelectionReference>(),
          FeatureSelectionRefs.ExtraMythicAbilityMythicFeat.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }

    /// <summary>
    /// Watches for successful saves against other creatures' effects and awards the
    /// defiance buff.
    /// </summary>
    [TypeId(Guids.DefiantSoulTrigger)]
    private class DefiantSoulTrigger :
      UnitFactComponentDelegate, ITargetRulebookHandler<RuleSavingThrow>
    {
      private readonly BlueprintBuff Buff;

      public DefiantSoulTrigger(BlueprintBuff buff)
      {
        Buff = buff;
      }

      public void OnEventAboutToTrigger(RuleSavingThrow evt) { }

      public void OnEventDidTrigger(RuleSavingThrow evt)
      {
        try
        {
          // Only successful saves count.
          if (!evt.IsPassed)
          {
            return;
          }
          // Ignore saves against your own effects.
          if (evt.Initiator == Owner)
          {
            return;
          }
          // No stacking while already active.
          if (Owner.Buffs.GetBuff(Buff) != null)
          {
            return;
          }

          Owner.AddBuff(Buff, Context, duration: ContextDuration.Fixed(1).Calculate(Context).Seconds);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("DefiantSoul: failed to handle save event.", e);
        }
      }
    }
  }
}

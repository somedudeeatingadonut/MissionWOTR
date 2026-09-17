using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Mythics
{
  /// <summary>
  /// Last Stand (original mythic ability)
  /// Wounds focus the legend within: while at or below half your hit points, you gain a
  /// +1 dodge bonus to AC per 2 mythic ranks (minimum +1).
  /// </summary>
  internal static class LastStand
  {
    internal const string FeatName = "LastStand";
    internal const string DisplayName = "LastStand.Name";
    internal const string Description = "LastStand.Description";

    internal static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.LastStandAbility)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(AbilityRefs.MageShield.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent<LastStandAcBonus>()
        .AddToFeatureSelection(
          FeatureSelectionRefs.MythicAbilitySelection.Cast<BlueprintFeatureSelectionReference>(),
          FeatureSelectionRefs.ExtraMythicAbilityMythicFeat.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }

    /// <summary>
    /// Applies the dodge bonus while the owner is at or below half hit points.
    /// </summary>
    [TypeId(Guids.LastStandAcBonus)]
    private class LastStandAcBonus :
      UnitFactComponentDelegate, ITargetRulebookHandler<RuleCalculateAC>
    {
      public void OnEventAboutToTrigger(RuleCalculateAC evt)
      {
        try
        {
          // Only while at or below half hit points.
          if (Owner.HPLeft * 2 > Owner.Stats.HitPoints.ModifiedValue)
          {
            return;
          }
          var rank = Owner.Descriptor.Progression.MythicLevel;
          evt.AddModifier(Math.Max(1, rank / 2), Fact, ModifierDescriptor.Dodge);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("LastStand: failed to apply AC bonus.", e);
        }
      }

      public void OnEventDidTrigger(RuleCalculateAC evt) { }
    }
  }

  /// <summary>
  /// Desperate Fury (original mythic ability)
  /// Nothing sharpens the mind like mortality: while at or below half your hit points, you
  /// gain a +1 insight bonus on attack rolls per 2 mythic ranks (minimum +1).
  /// </summary>
  internal static class DesperateFury
  {
    internal const string FeatName = "DesperateFury";
    internal const string DisplayName = "DesperateFury.Name";
    internal const string Description = "DesperateFury.Description";

    internal static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.DesperateFuryAbility)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.PowerAttackFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent<DesperateFuryAttackBonus>()
        .AddToFeatureSelection(
          FeatureSelectionRefs.MythicAbilitySelection.Cast<BlueprintFeatureSelectionReference>(),
          FeatureSelectionRefs.ExtraMythicAbilityMythicFeat.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }

    /// <summary>
    /// Applies the insight attack bonus while the owner is at or below half hit points.
    /// </summary>
    [TypeId(Guids.DesperateFuryAttackBonus)]
    private class DesperateFuryAttackBonus :
      UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>
    {
      public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
      {
        try
        {
          // Only while at or below half hit points.
          if (Owner.HPLeft * 2 > Owner.Stats.HitPoints.ModifiedValue)
          {
            return;
          }
          var rank = Owner.Descriptor.Progression.MythicLevel;
          evt.AddModifier(Math.Max(1, rank / 2), Fact, ModifierDescriptor.Insight);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("DesperateFury: failed to apply attack bonus.", e);
        }
      }

      public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }
    }
  }
}

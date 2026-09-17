using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using MissionWOTR.Feats;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using System;

namespace MissionWOTR.Mythics
{
  /// <summary>
  /// Slayer's Vigor (original mythic ability)
  /// Death feeds your legend: reducing an enemy to 0 HP with a weapon attack grants
  /// temporary hit points equal to your mythic rank (minimum 2) for 1 minute. Cannot be
  /// gained again while active.
  /// </summary>
  internal static class SlayersVigor
  {
    internal const string FeatName = "SlayersVigor";
    internal const string DisplayName = "SlayersVigor.Name";
    internal const string Description = "SlayersVigor.Description";
    internal const string BuffName = "SlayersVigorBuff";
    internal const string BuffDisplayName = "SlayersVigor.Buff.Name";
    internal const string BuffDescription = "SlayersVigor.Buff.Description";

    internal static void Configure()
    {
      var buff = BuffConfigurator.New(BuffName, Guids.SlayersVigorBuff)
        .SetDisplayName(BuffDisplayName)
        .SetDescription(BuffDescription)
        .SetIcon(FeatureRefs.Endurance.Reference.Get().Icon)
        // Temporary hit points equal to mythic rank (minimum 2, see rank config below).
        .AddComponent<TemporaryHitPointsFromAbilityValue>(c =>
        {
          c.Value = ContextValues.Rank();
          c.RemoveWhenHitPointsEnd = true;
        })
        .AddContextRankConfig(ContextRankConfigs.MythicLevel(min: 2))
        .Configure();

      FeatureConfigurator.New(FeatName, Guids.SlayersVigorAbility)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.Endurance.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new SlayersVigorTrigger(buff))
        .AddToFeatureSelection(
          FeatureSelectionRefs.MythicAbilitySelection.Cast<BlueprintFeatureSelectionReference>(),
          FeatureSelectionRefs.ExtraMythicAbilityMythicFeat.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }

    /// <summary>
    /// Watches for weapon damage the owner deals and awards the vigor buff when the
    /// damage drops the target to 0 HP.
    /// </summary>
    [TypeId(Guids.SlayersVigorTrigger)]
    private class SlayersVigorTrigger :
      UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleDealDamage>
    {
      private readonly BlueprintBuff Buff;

      public SlayersVigorTrigger(BlueprintBuff buff)
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
          // No refresh while the vigor is already running.
          if (Owner.Buffs.GetBuff(Buff) != null)
          {
            return;
          }

          // 1 minute (10 rounds).
          Owner.AddBuff(Buff, Context, duration: ContextDuration.Fixed(10).Calculate(Context).Seconds);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("SlayersVigor: failed to handle damage event.", e);
        }
      }
    }
  }
}

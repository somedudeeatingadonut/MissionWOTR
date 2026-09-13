using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using System;

namespace MissionWOTR.Feats
{
  /// <summary>
  /// Resonant Strikes
  /// Prerequisites: Wis 13, Improved Unarmed Strike.
  /// Your unarmed strikes carry a harmonic shock. When you confirm a critical hit with an
  /// unarmed strike, vibrations rattle the target's guard: it takes a -2 penalty to AC for
  /// 1 round. Repeated critical hits refresh the duration.
  /// </summary>
  public class ResonantStrikes
  {
    internal const string FeatName = "ResonantStrikes";
    internal const string DisplayName = "ResonantStrikes.Name";
    internal const string Description = "ResonantStrikes.Description";

    internal const string DebuffName = "ResonantStrikesDebuff";
    internal const string DebuffDisplayName = "ResonantStrikes.Debuff.Name";
    internal const string DebuffDescription = "ResonantStrikes.Debuff.Description";

    internal static void Configure()
    {
      var icon = FeatureRefs.ImprovedUnarmedStrike.Reference.Get().Icon;

      var debuff = BuffConfigurator.New(DebuffName, Guids.ResonantStrikesDebuff)
        .SetDisplayName(DebuffDisplayName)
        .SetDescription(DebuffDescription)
        .SetIcon(icon)
        .AddComponent<ResonantStrikesAcDebuff>()
        .Configure();

      FeatureConfigurator.New(FeatName, Guids.ResonantStrikesFeat, FeatureGroup.Feat, FeatureGroup.CombatFeat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFeatureTagsComponent(FeatureTag.Melee | FeatureTag.Attack)
        .AddPrerequisiteStatValue(StatType.Wisdom, 13)
        .AddPrerequisiteFeature(FeatureRefs.ImprovedUnarmedStrike.ToString())
        .AddComponent(new ResonantStrikesTrigger(debuff))
        .Configure(delayed: true);
    }

    [TypeId(Guids.ResonantStrikesTrigger)]
    private class ResonantStrikesTrigger :
      UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleAttackWithWeapon>
    {
      private readonly BlueprintBuff Debuff;

      public ResonantStrikesTrigger(BlueprintBuff debuff)
      {
        Debuff = debuff;
      }

      public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

      public void OnEventDidTrigger(RuleAttackWithWeapon evt)
      {
        try
        {
          if (evt.AttackRoll is null || !evt.AttackRoll.IsCriticalConfirmed)
          {
            return;
          }
          if (evt.Weapon is null || !evt.Weapon.Blueprint.IsUnarmed)
          {
            return;
          }
          if (evt.Target is null)
          {
            return;
          }

          evt.Target.AddBuff(Debuff, Context, duration: ContextDuration.Fixed(1).Calculate(Context).Seconds);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("ResonantStrikes: failed to handle attack event.", e);
        }
      }
    }

    [TypeId(Guids.ResonantStrikesAcDebuff)]
    private class ResonantStrikesAcDebuff :
      UnitFactComponentDelegate, ITargetRulebookHandler<RuleCalculateAC>
    {
      public void OnEventAboutToTrigger(RuleCalculateAC evt)
      {
        try
        {
          evt.AddModifier(-2, Fact);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("ResonantStrikes: failed to apply AC penalty.", e);
        }
      }

      public void OnEventDidTrigger(RuleCalculateAC evt) { }
    }
  }
}

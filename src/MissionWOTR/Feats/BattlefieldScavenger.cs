using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using System;

namespace MissionWOTR.Feats
{
  /// <summary>
  /// Battlefield Scavenger
  /// Prerequisites: Dex 13, Point-Blank Shot.
  /// Precision comes cheap when the target stops moving. When you reduce an enemy to 0 hit
  /// points with a ranged weapon attack, your next ranged attack within 1 round gains a +2
  /// bonus on the attack roll.
  /// </summary>
  public class BattlefieldScavenger
  {
    internal const string FeatName = "BattlefieldScavenger";
    internal const string DisplayName = "BattlefieldScavenger.Name";
    internal const string Description = "BattlefieldScavenger.Description";

    internal const string BuffName = "BattlefieldScavengerBuff";
    internal const string BuffDisplayName = "BattlefieldScavenger.Buff.Name";
    internal const string BuffDescription = "BattlefieldScavenger.Buff.Description";

    internal static void Configure()
    {
      var icon = FeatureRefs.PointBlankShot.Reference.Get().Icon;

      var buff = BuffConfigurator.New(BuffName, Guids.BattlefieldScavengerBuff)
        .SetDisplayName(BuffDisplayName)
        .SetDescription(BuffDescription)
        .SetIcon(icon)
        .AddComponent<BattlefieldScavengerAttackBonus>()
        .Configure();

      FeatureConfigurator.New(FeatName, Guids.BattlefieldScavengerFeat, FeatureGroup.Feat, FeatureGroup.CombatFeat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Ranged)
        .AddPrerequisiteStatValue(StatType.Dexterity, 13)
        .AddPrerequisiteFeature(FeatureRefs.PointBlankShot.ToString())
        .AddComponent(new BattlefieldScavengerTrigger(buff))
        .Configure(delayed: true);
    }

    [TypeId(Guids.BattlefieldScavengerTrigger)]
    private class BattlefieldScavengerTrigger :
      UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleDealDamage>
    {
      private readonly BlueprintBuff Buff;

      public BattlefieldScavengerTrigger(BlueprintBuff buff)
      {
        Buff = buff;
      }

      public void OnEventAboutToTrigger(RuleDealDamage evt) { }

      public void OnEventDidTrigger(RuleDealDamage evt)
      {
        try
        {
          // Only weapon attacks...
          if (evt.Reason.Rule is not RuleAttackWithWeapon attack)
          {
            return;
          }
          // ...with a ranged weapon...
          if (attack.Weapon is null || !attack.Weapon.Blueprint.IsRanged)
          {
            return;
          }
          // ...that finished the target.
          if (evt.Target is null || evt.Target.HPLeft > 0)
          {
            return;
          }

          Owner.AddBuff(Buff, Context, duration: ContextDuration.Fixed(1).Calculate(Context).Seconds);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("BattlefieldScavenger: failed to handle damage event.", e);
        }
      }
    }

    [TypeId(Guids.BattlefieldScavengerAttackBonus)]
    private class BattlefieldScavengerAttackBonus :
      UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>
    {
      public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt) { }

      public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt)
      {
        try
        {
          if (evt.Weapon is null || !evt.Weapon.Blueprint.IsRanged)
          {
            return;
          }
          evt.AddModifier(2, Fact);
          evt.Result += 2;
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("BattlefieldScavenger: failed to apply attack bonus.", e);
        }
      }
    }
  }
}

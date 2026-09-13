using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using System;

namespace MissionWOTR.Feats
{
  /// <summary>
  /// Taunting Blows
  /// Prerequisites: Str 13, Power Attack.
  /// Your brutal swings do more than wound. When you hit with a melee attack while Power Attack
  /// is active, the target must succeed at a Will save (DC 10 + half your character level +
  /// your Strength modifier) or suffer a -2 penalty on attack rolls for 1 round as pain and
  /// fury cloud its focus. The penalty cannot be applied again while it is already active.
  /// </summary>
  public class TauntingBlows
  {
    internal const string FeatName = "TauntingBlows";
    internal const string DisplayName = "TauntingBlows.Name";
    internal const string Description = "TauntingBlows.Description";

    internal const string DebuffName = "TauntingBlowsDebuff";
    internal const string DebuffDisplayName = "TauntingBlows.Debuff.Name";
    internal const string DebuffDescription = "TauntingBlows.Debuff.Description";

    internal static void Configure()
    {
      var icon = FeatureRefs.PowerAttackFeature.Reference.Get().Icon;

      var debuff = BuffConfigurator.New(DebuffName, Guids.TauntingBlowsDebuff)
        .SetDisplayName(DebuffDisplayName)
        .SetDescription(DebuffDescription)
        .SetIcon(icon)
        .AddContextStatBonus(StatType.AdditionalAttackBonus, ContextValues.Constant(-2), ModifierDescriptor.Penalty)
        .Configure();

      FeatureConfigurator.New(FeatName, Guids.TauntingBlowsFeat, FeatureGroup.Feat, FeatureGroup.CombatFeat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFeatureTagsComponent(FeatureTag.Melee | FeatureTag.Attack)
        .AddPrerequisiteStatValue(StatType.Strength, 13)
        .AddPrerequisiteFeature(FeatureRefs.PowerAttackFeature.ToString())
        .AddComponent(new TauntingBlowsTrigger(debuff))
        .Configure(delayed: true);
    }

    [TypeId(Guids.TauntingBlowsTrigger)]
    private class TauntingBlowsTrigger :
      UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleAttackWithWeapon>
    {
      private readonly BlueprintBuff Debuff;
      private readonly BlueprintBuff PowerAttackBuff = BuffRefs.PowerAttackBuff.Reference.Get();

      public TauntingBlowsTrigger(BlueprintBuff debuff)
      {
        Debuff = debuff;
      }

      public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

      public void OnEventDidTrigger(RuleAttackWithWeapon evt)
      {
        try
        {
          if (evt.AttackRoll is null || !evt.AttackRoll.IsHit)
          {
            return;
          }
          if (evt.Weapon is null || !evt.Weapon.Blueprint.IsMelee)
          {
            return;
          }
          if (evt.Target is null || evt.Target == Owner)
          {
            return;
          }

          // Only while Power Attack is active.
          if (Owner.Buffs.GetBuff(PowerAttackBuff) is null)
          {
            return;
          }

          // No re-application while the taunt is already active: wait for it to wear off.
          if (evt.Target.Buffs.GetBuff(Debuff) != null)
          {
            return;
          }

          // Will save negates (mind-affecting effect of pain and fury).
          var dc =
            10 + Owner.Descriptor.Progression.CharacterLevel / 2 + Owner.Stats.Strength.Bonus;
          var save = new RuleSavingThrow(evt.Target, SavingThrowType.Will, dc) { Reason = Fact };
          if (Rulebook.Trigger(save).IsPassed)
          {
            return;
          }

          evt.Target.AddBuff(Debuff, Context, duration: ContextDuration.Fixed(1).Calculate(Context).Seconds);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("TauntingBlows: failed to handle attack event.", e);
        }
      }
    }
  }
}

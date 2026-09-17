using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Enums;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Feats
{
  /// <summary>
  /// Guarded Momentum
  /// Prerequisites: Combat Expertise.
  /// Each melee attack that misses you hardens your guard: you gain a +1 dodge bonus to AC
  /// until the start of your next turn each time a melee attack misses you, stacking up to
  /// +3. (Adapted from the roadmap design: Wrath exposes no "fighting defensively" state
  /// to code, so the feat keys off Combat Expertise and any melee miss instead.)
  /// </summary>
  public class GuardedMomentum
  {
    internal const string FeatName = "GuardedMomentum";
    internal const string DisplayName = "GuardedMomentum.Name";
    internal const string Description = "GuardedMomentum.Description";
    internal const string BuffName = "GuardedMomentumBuff";
    internal const string BuffDisplayName = "GuardedMomentum.Buff.Name";
    internal const string BuffDescription = "GuardedMomentum.Buff.Description";

    public static void Configure()
    {
      var buff = BuffConfigurator.New(BuffName, Guids.GuardedMomentumBuff)
        .SetDisplayName(BuffDisplayName)
        .SetDescription(BuffDescription)
        .SetIcon(FeatureRefs.CombatExpertiseFeature.Reference.Get().Icon)
        // Each application adds a rank (up to 3); each rank is +1 dodge AC.
        .SetStacking(StackingType.Rank)
        .SetRanks(3)
        .AddContextStatBonus(StatType.AC, ContextValues.Rank(), ModifierDescriptor.Dodge)
        .AddContextRankConfig(ContextRankConfigs.BuffRank(Guids.GuardedMomentumBuff, max: 3))
        .Configure();

      FeatureConfigurator.New(FeatName, Guids.GuardedMomentumFeat, FeatureGroup.Feat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.CombatExpertiseFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddPrerequisiteFeature(FeatureRefs.CombatExpertiseFeature.ToString())
        .AddComponent(new GuardedMomentumTrigger(buff))
        .Configure(delayed: true);
    }

    /// <summary>
    /// Watches for melee attacks that miss the owner and applies/stacks the momentum buff.
    /// </summary>
    [TypeId(Guids.GuardedMomentumTrigger)]
    private class GuardedMomentumTrigger :
      UnitFactComponentDelegate, ITargetRulebookHandler<RuleAttackWithWeapon>
    {
      private readonly BlueprintBuff Buff;

      public GuardedMomentumTrigger(BlueprintBuff buff)
      {
        Buff = buff;
      }

      public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

      public void OnEventDidTrigger(RuleAttackWithWeapon evt)
      {
        try
        {
          // Only melee attacks that missed...
          if (evt.AttackRoll is null || evt.AttackRoll.IsHit)
          {
            return;
          }
          if (evt.Weapon is null || !evt.Weapon.Blueprint.IsMelee)
          {
            return;
          }

          // One rank per miss; the buff expires (with all ranks) at the start of the
          // owner's next turn.
          Owner.AddBuff(Buff, Context, duration: ContextDuration.Fixed(1).Calculate(Context).Seconds);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("GuardedMomentum: failed to handle attack event.", e);
        }
      }
    }
  }
}

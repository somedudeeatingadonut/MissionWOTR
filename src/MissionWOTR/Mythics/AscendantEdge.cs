using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using System;

namespace MissionWOTR.Mythics
{
  /// <summary>
  /// Ascendant Edge (original mythic ability)
  /// Your spells leave a wake of power that sharpens your strikes: casting a spell of 1st
  /// level or higher from one of your spellbooks grants a +1 insight bonus on attack
  /// rolls for 1 round, increasing by +1 for every 2 mythic ranks (minimum +1). Cannot be
  /// gained again while active.
  /// </summary>
  internal static class AscendantEdge
  {
    internal const string FeatName = "AscendantEdge";
    internal const string DisplayName = "AscendantEdge.Name";
    internal const string Description = "AscendantEdge.Description";
    internal const string BuffName = "AscendantEdgeBuff";
    internal const string BuffDisplayName = "AscendantEdge.Buff.Name";
    internal const string BuffDescription = "AscendantEdge.Buff.Description";

    internal static void Configure()
    {
      var buff = BuffConfigurator.New(BuffName, Guids.AscendantEdgeBuff)
        .SetDisplayName(BuffDisplayName)
        .SetDescription(BuffDescription)
        .SetIcon(FeatureRefs.PowerAttackFeature.Reference.Get().Icon)
        // Insight bonus on attack rolls, half mythic rank (minimum +1).
        .AddContextStatBonus(
          StatType.AdditionalAttackBonus, ContextValues.Rank(), ModifierDescriptor.Insight)
        .AddContextRankConfig(ContextRankConfigs.MythicLevel(min: 1).WithDiv2Progression())
        .Configure();

      FeatureConfigurator.New(FeatName, Guids.AscendantEdgeAbility)
        .SetLocalizedName(DisplayName)
        .SetLocalizedDescription(Description)
        .SetIcon(FeatureRefs.PowerAttackFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new AscendantEdgeTrigger(buff))
        .AddToFeatureSelection(
          FeatureSelectionRefs.MythicAbilitySelection.Cast<BlueprintFeatureSelectionReference>(),
          FeatureSelectionRefs.ExtraMythicAbilityMythicFeat.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }

    /// <summary>
    /// Watches for non-cantrip spells cast from one of the owner's spellbooks and applies
    /// the edge buff.
    /// </summary>
    [TypeId(Guids.AscendantEdgeTrigger)]
    private class AscendantEdgeTrigger :
      UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCastSpell>
    {
      private readonly BlueprintBuff Buff;

      public AscendantEdgeTrigger(BlueprintBuff buff)
      {
        Buff = buff;
      }

      public void OnEventAboutToTrigger(RuleCastSpell evt) { }

      public void OnEventDidTrigger(RuleCastSpell evt)
      {
        try
        {
          // Only spells cast from an actual spellbook count (not scrolls, items, or SLAs).
          if (evt.Spell?.Spellbook is null)
          {
            return;
          }
          // Only real spells count (not cantrips).
          if (evt.Spell.SpellLevel < 1)
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
          MissionFeats.Logger.Error("AscendantEdge: failed to handle cast event.", e);
        }
      }
    }
  }
}

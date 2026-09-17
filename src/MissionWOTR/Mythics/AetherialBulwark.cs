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
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Mythics
{
  /// <summary>
  /// Aetherial Bulwark (original mythic ability)
  /// Magic leaves a shield in its wake: whenever you cast a spell of 1st level or higher
  /// from one of your spellbooks, you gain a +1 deflection bonus to AC for 1 round per 2
  /// mythic ranks (minimum +1). Cannot be gained again while active.
  /// </summary>
  internal static class AetherialBulwark
  {
    internal const string FeatName = "AetherialBulwark";
    internal const string DisplayName = "AetherialBulwark.Name";
    internal const string Description = "AetherialBulwark.Description";
    internal const string BuffName = "AetherialBulwarkBuff";
    internal const string BuffDisplayName = "AetherialBulwark.Buff.Name";
    internal const string BuffDescription = "AetherialBulwark.Buff.Description";

    internal static void Configure()
    {
      var buff = BuffConfigurator.New(BuffName, Guids.AetherialBulwarkBuff)
        .SetDisplayName(BuffDisplayName)
        .SetDescription(BuffDescription)
        .SetIcon(AbilityRefs.MageShield.Reference.Get().Icon)
        .AddContextStatBonus(StatType.AC, ContextValues.Rank(), ModifierDescriptor.Deflection)
        .AddContextRankConfig(ContextRankConfigs.MythicLevel(min: 1).WithDiv2Progression())
        .Configure();

      FeatureConfigurator.New(FeatName, Guids.AetherialBulwarkAbility)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(AbilityRefs.MageShield.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new AetherialBulwarkTrigger(buff))
        .AddToFeatureSelection(
          FeatureSelectionRefs.MythicAbilitySelection.Cast<BlueprintFeatureSelectionReference>(),
          FeatureSelectionRefs.ExtraMythicAbilityMythicFeat.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);
    }

    /// <summary>
    /// Watches for non-cantrip spells cast from one of the owner's spellbooks and applies
    /// the bulwark buff.
    /// </summary>
    [TypeId(Guids.AetherialBulwarkTrigger)]
    private class AetherialBulwarkTrigger :
      UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCastSpell>
    {
      private readonly BlueprintBuff Buff;

      public AetherialBulwarkTrigger(BlueprintBuff buff)
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
          MissionFeats.Logger.Error("AetherialBulwark: failed to handle cast event.", e);
        }
      }
    }
  }
}

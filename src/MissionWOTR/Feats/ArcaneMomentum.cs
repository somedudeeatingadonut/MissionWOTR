using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using System;

namespace MissionWOTR.Feats
{
  /// <summary>
  /// Arcane Momentum
  /// Prerequisites: caster level 3rd.
  /// Whenever you cast a spell using one of your highest-level spell slots, the surge of power
  /// sharpens your defenses: you gain a +1 dodge bonus to AC until the start of your next turn.
  /// </summary>
  public class ArcaneMomentum
  {
    internal const string FeatName = "ArcaneMomentum";
    internal const string DisplayName = "ArcaneMomentum.Name";
    internal const string Description = "ArcaneMomentum.Description";

    internal const string BuffName = "ArcaneMomentumBuff";
    internal const string BuffDisplayName = "ArcaneMomentum.Buff.Name";
    internal const string BuffDescription = "ArcaneMomentum.Buff.Description";

    internal static void Configure()
    {
      var icon = AbilityRefs.MageArmor.Reference.Get().Icon;

      var buff = BuffConfigurator.New(BuffName, Guids.ArcaneMomentumBuff)
        .SetDisplayName(BuffDisplayName)
        .SetDescription(BuffDescription)
        .SetIcon(icon)
        .AddContextStatBonus(StatType.AC, ContextValues.Constant(1), ModifierDescriptor.Dodge)
        .Configure();

      FeatureConfigurator.New(FeatName, Guids.ArcaneMomentumFeat, FeatureGroup.Feat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFeatureTagsComponent(FeatureTag.Magic | FeatureTag.Defense)
        .AddRecommendationRequiresSpellbook()
        .AddComponent<PrerequisiteCasterLevel>(c => c.RequiredCasterLevel = 3)
        .AddComponent(new ArcaneMomentumTrigger(buff))
        .Configure(delayed: true);
    }

    [TypeId(Guids.ArcaneMomentumTrigger)]
    private class ArcaneMomentumTrigger :
      UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCastSpell>
    {
      private readonly BlueprintBuff Buff;

      public ArcaneMomentumTrigger(BlueprintBuff buff)
      {
        Buff = buff;
      }

      public void OnEventAboutToTrigger(RuleCastSpell evt) { }

      public void OnEventDidTrigger(RuleCastSpell evt)
      {
        try
        {
          // Only spells cast from an actual spellbook count (not scrolls, items, or SLAs).
          var spellbook = evt.Spell?.Spellbook;
          if (spellbook is null)
          {
            return;
          }

          // Only your highest-level casts trigger the momentum.
          if (evt.Spell.SpellLevel < spellbook.GetMaxSpellLevel())
          {
            return;
          }

          // One surge per round.
          if (Owner.Buffs.GetBuff(Buff) != null)
          {
            return;
          }

          Owner.AddBuff(Buff, Context, duration: ContextDuration.Fixed(1).Calculate(Context).Seconds);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("ArcaneMomentum: failed to handle cast event.", e);
        }
      }
    }
  }
}

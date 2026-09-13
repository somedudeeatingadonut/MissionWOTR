using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using System;
using System.Linq;

namespace MissionWOTR.Feats
{
  /// <summary>
  /// Arcane Momentum
  /// Prerequisites: caster level 3rd.
  /// Whenever you cast a spell from one of your two highest spell levels, the surge of power
  /// sharpens your defenses: until the start of your next turn you gain a dodge bonus to AC.
  /// The bonus grows with the mightiest spell level you can cast: +1 (levels 1-2), +2
  /// (levels 3-4), or +3 (level 5 or higher).
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
        .AddComponent<ArcaneMomentumAcBonus>()
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

    /// <summary>
    /// Applies the scaling dodge bonus while the momentum buff is active. The size of the
    /// ward matches the highest spell level the owner can currently cast.
    /// </summary>
    [TypeId(Guids.ArcaneMomentumAcBonus)]
    private class ArcaneMomentumAcBonus :
      UnitFactComponentDelegate, ITargetRulebookHandler<RuleCalculateAC>
    {
      public void OnEventAboutToTrigger(RuleCalculateAC evt)
      {
        try
        {
          evt.AddModifier(GetBonus(), Fact);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("ArcaneMomentum: failed to apply AC bonus.", e);
        }
      }

      public void OnEventDidTrigger(RuleCalculateAC evt) { }

      private int GetBonus()
      {
        var bestSpellLevel =
          Owner.Spellbooks
            .Select(spellbook => spellbook.GetMaxSpellLevel())
            .DefaultIfEmpty(1)
            .Max();
        return bestSpellLevel switch
        {
          >= 5 => 3,
          >= 3 => 2,
          _ => 1
        };
      }
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

          // Only real spells count (not cantrips).
          if (evt.Spell.SpellLevel < 1)
          {
            return;
          }

          // Only your two highest spell levels trigger the momentum.
          if (evt.Spell.SpellLevel < spellbook.GetMaxSpellLevel() - 1)
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

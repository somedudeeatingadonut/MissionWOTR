using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using System;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;

namespace MissionWOTR.Feats
{
  /// <summary>
  /// Vengeful Counterstrike
  /// Prerequisites: Dex 13, Combat Reflexes.
  /// When a melee attack misses you and the attacker is within your reach, you may make an
  /// attack of opportunity against them. This uses (and is limited by) your normal attacks of
  /// opportunity per round, exactly like Combat Reflexes.
  /// </summary>
  public class VengefulCounterstrike
  {
    internal const string FeatName = "VengefulCounterstrike";
    internal const string DisplayName = "VengefulCounterstrike.Name";
    internal const string Description = "VengefulCounterstrike.Description";

    internal static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.VengefulCounterstrikeFeat, FeatureGroup.Feat, FeatureGroup.CombatFeat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.CombatReflexes.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFeatureTagsComponent(FeatureTag.Attack | FeatureTag.Melee)
        .AddPrerequisiteStatValue(StatType.Dexterity, 13)
        .AddPrerequisiteFeature(FeatureRefs.CombatReflexes.ToString())
        .AddComponent<VengefulCounterstrikeTrigger>()
        .Configure(delayed: true);
    }

    [TypeId(Guids.VengefulCounterstrikeTrigger)]
    private class VengefulCounterstrikeTrigger :
      UnitFactComponentDelegate, ITargetRulebookHandler<RuleAttackWithWeapon>
    {
      public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

      public void OnEventDidTrigger(RuleAttackWithWeapon evt)
      {
        try
        {
          // Only react to melee attacks that missed us.
          if (evt.Initiator is null || evt.Initiator == Owner)
          {
            return;
          }
          if (evt.AttackRoll is null || evt.AttackRoll.IsHit)
          {
            return;
          }
          if (evt.Weapon is null || !evt.Weapon.Blueprint.IsMelee)
          {
            return;
          }

          // We need a melee weapon in hand and the attacker within our reach.
          var weapon = Owner.Body.PrimaryHand.MaybeWeapon;
          if (weapon is null || !weapon.Blueprint.IsMelee)
          {
            return;
          }
          if (!Owner.CombatState.IsEngage(evt.Initiator))
          {
            return;
          }

          // Spend one of our attacks of opportunity for the round.
          Game.Instance.CombatEngagementController.ForceAttackOfOpportunity(Owner, evt.Initiator);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("VengefulCounterstrike: failed to handle attack event.", e);
        }
      }
    }
  }
}

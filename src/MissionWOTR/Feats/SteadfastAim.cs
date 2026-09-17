using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Enums;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using MissionWOTR.Feats;
using System;
using System.Linq;

namespace MissionWOTR.Feats
{
  /// <summary>
  /// Steadfast Aim
  /// Prerequisites: Dex 13, Point-Blank Shot.
  /// Pick your moment when steel is already crossed: you gain a +1 bonus on ranged attack
  /// rolls against enemies engaged in melee with one of your allies.
  /// </summary>
  public class SteadfastAim
  {
    internal const string FeatName = "SteadfastAim";
    internal const string DisplayName = "SteadfastAim.Name";
    internal const string Description = "SteadfastAim.Description";

    public static void Configure()
    {
      FeatureConfigurator.New(FeatName, Guids.SteadfastAimFeat, FeatureGroup.Feat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.PointBlankShot.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddPrerequisiteStatValue(StatType.Dexterity, 13)
        .AddPrerequisiteFeature(FeatureRefs.PointBlankShot.ToString())
        .AddComponent<SteadfastAimAttackBonus>()
        .Configure(delayed: true);
    }

    /// <summary>
    /// Adds the attack bonus when the target is tied up in melee with an ally.
    /// </summary>
    [TypeId(Guids.SteadfastAimAttackBonus)]
    private class SteadfastAimAttackBonus :
      UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCalculateAttackBonus>
    {
      public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
      {
        try
        {
          // Only ranged attacks...
          if (evt.Weapon is null || !evt.Weapon.Blueprint.IsRanged)
          {
            return;
          }
          // ...against a target engaged in melee with one of your allies (not you).
          if (evt.Target is null)
          {
            return;
          }
          var engagedByAlly = evt.Target.CombatState.EngagedBy
            .Any(unit => unit != Owner && unit.IsAlly(Owner));
          if (!engagedByAlly)
          {
            return;
          }

          evt.AddModifier(1, Fact, ModifierDescriptor.UntypedStackable);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("SteadfastAim: failed to apply attack bonus.", e);
        }
      }

      public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }
    }
  }
}

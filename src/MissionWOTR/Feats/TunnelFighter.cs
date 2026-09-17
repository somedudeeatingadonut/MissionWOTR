using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;

namespace MissionWOTR.Feats
{
  /// <summary>
  /// Tunnel Fighter
  /// Prerequisites: Combat Reflexes.
  /// A toggleable stance: while active, you may make any number of attacks of opportunity
  /// each round, but you take a -2 penalty on all attack rolls.
  /// </summary>
  public class TunnelFighter
  {
    internal const string FeatName = "TunnelFighter";
    internal const string DisplayName = "TunnelFighter.Name";
    internal const string Description = "TunnelFighter.Description";
    internal const string AbilityName = "TunnelFighterAbility";
    internal const string BuffName = "TunnelFighterBuff";
    internal const string BuffDisplayName = "TunnelFighter.Buff.Name";
    internal const string BuffDescription = "TunnelFighter.Buff.Description";

    public static void Configure()
    {
      var buff = BuffConfigurator.New(BuffName, Guids.TunnelFighterBuff)
        .SetDisplayName(BuffDisplayName)
        .SetDescription(BuffDescription)
        .SetIcon(FeatureRefs.CombatReflexes.Reference.Get().Icon)
        // Effectively unlimited attacks of opportunity...
        .AddContextStatBonus(
          StatType.AttackOfOpportunityCount, ContextValues.Constant(50), ModifierDescriptor.UntypedStackable)
        // ...at the cost of accuracy.
        .AddContextStatBonus(
          StatType.AdditionalAttackBonus, ContextValues.Constant(-2), ModifierDescriptor.Penalty)
        .Configure();

      var ability = ActivatableAbilityConfigurator.New(AbilityName, Guids.TunnelFighterAbility)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.CombatReflexes.Reference.Get().Icon)
        .SetBuff(buff)
        .Configure();

      FeatureConfigurator.New(FeatName, Guids.TunnelFighterFeat, FeatureGroup.Feat)
        .SetDisplayName(DisplayName)
        .SetDescription(Description)
        .SetIcon(FeatureRefs.CombatReflexes.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddPrerequisiteFeature(FeatureRefs.CombatReflexes.ToString())
        .AddFacts(new() { ability })
        .Configure(delayed: true);
    }
  }
}

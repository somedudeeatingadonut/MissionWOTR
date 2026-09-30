using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using MissionWOTR.Feats;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// THE HEDGE WITCH (witch tabletop port - Ultimate Magic pg. 84,
  /// the first faithful witch port; the game's own witch archetypes
  /// - Stigmatized, Hagbound, Hex Channeler, Ley Line Guardian,
  /// Elemental Witch, Witch of the Veil - left it free to take).
  ///
  /// The tabletop brief: "Among witches, there are those who devote
  /// themselves to the care of others and restrict their practices
  /// to the healing arts. They often take the place of clerics in
  /// rural communities."
  ///
  /// The trades are tiny and precise (the UM text trades only two
  /// hex levels):
  /// - the hex gained at 4th level -> Spontaneous Healing: "The
  ///   witch can 'lose' any prepared spell that is not an orison in
  ///   order to cast any cure spell of the same spell level or
  ///   lower, even if she doesn't know that cure spell." Ported
  ///   with the engine's own SpontaneousSpellConversion component
  ///   (the cleric's mechanism) retargeted to the witch class.
  ///   Documented substitutions: regenerate is not in the game, so
  ///   7th-level slots convert to heal (the rule allows same level
  ///   OR LOWER); breath of life uses the castable variant.
  /// - the hex gained at 8th level -> Empathic Healing: "minister
  ///   to a diseased or poisoned target, redirecting the affliction
  ///   into herself." Port adaptation (documented): a standard-action
  ///   touch that removes all poisons and diseases from the target
  ///   by way of the vanilla cure spells, while the witch takes the
  ///   remembered pain - a 1-round visible pain buff dealing 2d6
  ///   direct damage. The tabletop's exact "suffer the failed save
  ///   instead" redirect is not exposed to Wrath's data layer.
  ///
  /// The tabletop's patron note ("normally one with a healing
  /// theme") is a recommendation, not a rule - the patron selection
  /// is untouched.
  /// Log prefix: [hedgewitch].
  /// </summary>
  internal static class HedgeWitch
  {
    internal const string ArchetypeName = "HedgeWitchArchetype";

    public static void Configure()
    {
      // ----- Spontaneous Healing (the 4th-level trade) -----
      // The engine's own conversion component (the cleric's), pointed
      // at the witch class: lose any prepared witch spell, cast the
      // cure spell of that level or lower instead.
      var spontaneous = FeatureConfigurator.New(
        "HedgeWitchSpontaneousHealing", Guids.HedgeWitchSpontaneousHealing)
        .SetDisplayName("HedgeWitchSpontaneousHealing.Name")
        .SetDescription("HedgeWitchSpontaneousHealing.Description")
        .SetIcon(AbilityRefs.CureLightWounds.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddSpontaneousSpellConversion(
          characterClass: CharacterClassRefs.WitchClass.Cast<BlueprintCharacterClassReference>(),
          spellsByLevel: new System.Collections.Generic.List<Blueprint<BlueprintAbilityReference>>
          {
            AbilityRefs.CureLightWounds.Cast<BlueprintAbilityReference>(),        // 1st
            AbilityRefs.CureModerateWounds.Cast<BlueprintAbilityReference>(),     // 2nd
            AbilityRefs.CureSeriousWounds.Cast<BlueprintAbilityReference>(),      // 3rd
            AbilityRefs.CureCriticalWounds.Cast<BlueprintAbilityReference>(),     // 4th
            AbilityRefs.BreathOfLifeCast.Cast<BlueprintAbilityReference>(),       // 5th
            AbilityRefs.Heal.Cast<BlueprintAbilityReference>(),                   // 6th
            AbilityRefs.Heal.Cast<BlueprintAbilityReference>(),                   // 7th (regenerate is not in the game; same-or-lower makes heal legal)
            AbilityRefs.CureCriticalWoundsMass.Cast<BlueprintAbilityReference>(), // 8th
            AbilityRefs.HealMass.Cast<BlueprintAbilityReference>(),               // 9th
          })
        .Configure();

      // ----- Empathic Healing (the 8th-level trade) -----
      // The pain the witch takes: a 1-round visible buff whose
      // on-apply action deals 2d6 direct damage to its owner (the
      // buff context runs on the caster).
      var pain = BuffConfigurator.New(
        "HedgeWitchEmpathicPainBuff", Guids.HedgeWitchEmpathicPainBuff)
        .SetDisplayName("HedgeWitchEmpathicPain.Name")
        .SetDescription("HedgeWitchEmpathicPain.Description")
        .SetIcon(AbilityRefs.NeutralizePoison.Reference.Get().Icon)
        .AddFactContextActions(activated: ActionsBuilder.New().DealDamage(
          DamageTypes.Direct(),
          new ContextDiceValue
          {
            DiceType = DiceType.D6,
            DiceCountValue = ContextValues.Constant(2),
            BonusValue = ContextValues.Constant(0),
          }))
        .Configure();

      // The touch: draw the afflictions out through the vanilla cure
      // spells, then feel them pass through the caster.
      var empathic = AbilityConfigurator.New(
        "HedgeWitchEmpathicHealingAbility", Guids.HedgeWitchEmpathicHealingAbility)
        .SetDisplayName("HedgeWitchEmpathicHealing.Name")
        .SetDescription("HedgeWitchEmpathicHealing.Description")
        .SetIcon(AbilityRefs.NeutralizePoison.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Touch)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetSelf()
        .SetCanTargetAllies()
        .SetCanTargetEnemies(false)
        .AddAbilityEffectRunAction(ActionsBuilder.New()
          .CastSpell(AbilityRefs.NeutralizePoison.Cast<BlueprintAbilityReference>(), castByTarget: true)
          .CastSpell(AbilityRefs.RemoveDisease.Cast<BlueprintAbilityReference>(), castByTarget: true)
          .ApplyBuff(pain, ContextDuration.Fixed(1), toCaster: true))
        .Configure();

      var empathicFeature = FeatureConfigurator.New(
        "HedgeWitchEmpathicHealing", Guids.HedgeWitchEmpathicHealing)
        .SetDisplayName("HedgeWitchEmpathicHealing.Name")
        .SetDescription("HedgeWitchEmpathicHealing.Description")
        .SetIcon(AbilityRefs.NeutralizePoison.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { empathic })
        .Configure();

      // ----- The archetype -----
      ArchetypeConfigurator.New(ArchetypeName, Guids.HedgeWitchArchetype, CharacterClassRefs.WitchClass)
        .SetLocalizedName("HedgeWitch.Name")
        .SetLocalizedDescription("HedgeWitch.Description")
        .AddToAddFeatures(LevelPlan.L(4), spontaneous)
        .AddToAddFeatures(LevelPlan.L(8), empathicFeature)
        .AddToRemoveFeatures(4, FeatureSelectionRefs.WitchHexSelection.ToString())
        .AddToRemoveFeatures(8, FeatureSelectionRefs.WitchHexSelection.ToString())
        .Configure();
      MissionFeats.Logger.Info("[hedgewitch] configured: " + ArchetypeName + ".");
    }
  }
}

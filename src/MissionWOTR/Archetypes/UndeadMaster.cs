using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using MissionWOTR.Feats;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.48.0 — the Undead Master (wizard archetype, Horror Adventures
  /// pg. 75). The user handed me the wheel for the wizard round; this is
  /// the port, chosen because the user literally builds for undead
  /// parties (0.46.0's Grave Tithe feat) - and because nearly all of it
  /// is REAL engine content.
  ///
  /// "Undead masters have great power over undeath."
  ///
  /// THE GAIN:
  /// - Command the Dead (1st): the tabletop grants the Command Undead
  ///   feat, powered by channel energy or school-power uses. WOTR has no
  ///   Command Undead FEAT - but it has the real Command Undead SPELL.
  ///   Documented substitute: a special ability that casts the genuine
  ///   spell, on its own pool (3/day, +1 at 6th/12th/18th - a nod to
  ///   the school powers' 3 + Int idiom). Replaces Scribe Scroll - which
  ///   WOTR wizards never had - so the cost folds into the feat trade
  ///   below.
  /// - Reanimator (3rd/5th/11th): the tabletop adds nine spells to the
  ///   book. In the game's spell list: lesser animate dead (3rd circle,
  ///   granted at 3rd), animate dead (5th, at 5th), undeath to death
  ///   (6th, at 11th) - each the REAL spell, written into the real
  ///   spellbook via the engine's own AddKnownSpell component. The rest
  ///   of the tabletop list (repair undead, undead anatomy, create
  ///   undead, create greater undead, cursed earth) is not on the
  ///   game's spell list and does not exist to grant - a documented
  ///   cut. The tabletop's spontaneous-cast-any-of-them clause has no
  ///   clean engine hook (SpontaneousSpellConversion's data shape is
  ///   not modder-exposed) - also a documented cut.
  /// - Lich-Loved (20th): the undead sorcerer bloodline's One Of Us -
  ///   which EXISTS in the game as a grantable feature - verbatim.
  ///
  /// THE TRADE (live-progression scans): the wizard bonus feats at 5th,
  /// 10th, 15th, and 20th. (Tabletop: scribe scroll + those feats; the
  /// game has no scribe scroll, so the feats carry the whole trade.)
  ///
  /// Necromantic Focus (must be evil, can't oppose necromancy) is a
  /// roleplay guideline, not enforced - documented cut, same as the
  /// tabletop's atonement clauses. Necropolitan's conditional
  /// skill-check modifiers (vs undead vs living) are not expressible -
  /// documented cut. Corpse Bond's corpse companion has no pet type in
  /// this engine - documented cut; the bond is left as-is.
  /// </summary>
  internal class UndeadMaster
  {
    internal const string ArchetypeName = "UndeadMaster";

    internal static void Configure()
    {
      var wizard = CharacterClassRefs.WizardClass.Reference.Get();
      var icon = AbilityRefs.AnimateDead.Reference.Get().Icon;

      // ----- The command pool: 3/day, +1 at 6th, 12th, 18th. -----
      var pool = AbilityResourceConfigurator.New(
        "UndeadMasterCommandResource", Guids.UndeadMasterCommandResource)
        .SetMax(3)
        .Configure();

      // ----- Command the Dead: the real Command Undead spell, on the pool. -----
      var ability = AbilityConfigurator.New(
        "UndeadMasterCommandAbility", Guids.UndeadMasterCommandAbility)
        .SetDisplayName("UndeadMasterCommand.Name")
        .SetDescription("UndeadMasterCommand.Description")
        .SetIcon(AbilityRefs.CommandUndead.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetEnemies()
        .AddAbilityResourceLogic(requiredResource: pool, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().CastSpell(AbilityRefs.CommandUndead).Build())
        .Configure();

      // ----- The kit (1st): the ability + the pool. -----
      var kit = FeatureConfigurator.New("UndeadMasterKitFeature", Guids.UndeadMasterKitFeature)
        .SetDisplayName("UndeadMasterCommand.Name")
        .SetDescription("UndeadMasterCommand.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { ability })
        .AddAbilityResources(resource: pool, restoreAmount: true)
        .Configure();

      // ----- Pool bumps at 6th/12th/18th (the smite-extras idiom). -----
      var extraGuids = new[]
      {
        Guids.UndeadMasterCommandExtra6, Guids.UndeadMasterCommandExtra12,
        Guids.UndeadMasterCommandExtra18,
      };
      var extras = new BlueprintFeature[extraGuids.Length];
      for (var i = 0; i < extraGuids.Length; i++)
      {
        extras[i] = FeatureConfigurator.New("UndeadMasterCommandExtra" + (6 * (i + 1)), extraGuids[i])
          .SetDisplayName("UndeadMasterCommand.Name")
          .SetDescription("UndeadMasterCommand.Description")
          .SetIcon(icon)
          .SetIsClassFeature()
          .AddIncreaseResourceAmount(pool, 1)
          .Configure();
      }

      // ----- Reanimator: the real spells, into the real spellbook. -----
      var reanimator3 = FeatureConfigurator.New(
        "UndeadMasterReanimator3", Guids.UndeadMasterReanimator3)
        .SetDisplayName("UndeadMasterReanimator.Name")
        .SetDescription("UndeadMasterReanimator.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddKnownSpell(
          characterClass: CharacterClassRefs.WizardClass,
          spell: AbilityRefs.AnimateDeadLesser,
          spellLevel: 3)
        .Configure();
      var reanimator5 = FeatureConfigurator.New(
        "UndeadMasterReanimator5", Guids.UndeadMasterReanimator5)
        .SetDisplayName("UndeadMasterReanimator.Name")
        .SetDescription("UndeadMasterReanimator.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddKnownSpell(
          characterClass: CharacterClassRefs.WizardClass,
          spell: AbilityRefs.AnimateDead,
          spellLevel: 5)
        .Configure();
      var reanimator11 = FeatureConfigurator.New(
        "UndeadMasterReanimator11", Guids.UndeadMasterReanimator11)
        .SetDisplayName("UndeadMasterReanimator.Name")
        .SetDescription("UndeadMasterReanimator.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddKnownSpell(
          characterClass: CharacterClassRefs.WizardClass,
          spell: AbilityRefs.UndeathToDeath,
          spellLevel: 6)
        .Configure();

      // ----- The archetype. -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.UndeadMasterArchetype, CharacterClassRefs.WizardClass)
          .SetLocalizedName("UndeadMaster.Name")
          .SetLocalizedDescription("UndeadMaster.Description")
          .AddToAddFeatures(LevelPlan.L(1), kit)
          .AddToAddFeatures(LevelPlan.L(3), reanimator3)
          .AddToAddFeatures(LevelPlan.L(5), reanimator5)
          .AddToAddFeatures(LevelPlan.L(6), extras[0])
          .AddToAddFeatures(LevelPlan.L(11), reanimator11)
          .AddToAddFeatures(LevelPlan.L(12), extras[1])
          .AddToAddFeatures(LevelPlan.L(18), extras[2])
          .AddToAddFeatures(LevelPlan.L(20),
            FeatureRefs.BloodlineUndeadOneOfUs.Cast<BlueprintFeatureBaseReference>());
      // The trade: the wizard bonus feats at 5/10/15/20 (scribe scroll
      // does not exist in this game, so the feats carry the whole trade).
      foreach (var level in new[] { 5, 10, 15, 20 })
      {
        archetype = ArchetypeRemovals.RemoveAtLevel(
          archetype, wizard.Progression, level,
          FeatureSelectionRefs.WizardFeatSelection.ToString());
      }
      archetype.Configure();
      MissionFeats.Logger.Info("[undeadmaster] configured: " + ArchetypeName + ".");
    }
  }
}

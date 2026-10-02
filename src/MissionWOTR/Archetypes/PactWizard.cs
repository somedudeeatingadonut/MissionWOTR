using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints.Classes;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Pact Wizard — Horror Adventures pg. 79. The wizard who signed something.
  ///
  /// 0.54.0 replacement for the Undead Master, withdrawn in 0.53.0 as a
  /// duplicate. Trap-checked against the loaded-mod list in the 0.52.1 log:
  /// vanilla ships Arcane Bomber, Scroll Savant, Thassilonian Specialist,
  /// Exploiter Wizard, Elemental Specialist, Spell Master, Cruoromancer and
  /// Shadowcaster; Expanded Content ships Draconic Scholar; HomebrewArchetypes
  /// ships no wizard archetypes at all. Pact Wizard is unclaimed.
  ///
  /// THE TRADE: the arcane bond, removed by the selection guid — the same
  /// removal Inkbound uses, so it is a verified lookup rather than a name
  /// guess.
  ///
  /// WHAT SHE GETS: a patron's spells, written into the real spellbook
  /// through the engine's own AddKnownSpell — the Apocryphal's theft idiom,
  /// minus the price. The patron here is a patron of dread, and the five
  /// spells are chosen to line up exactly with the levels a wizard would
  /// reach that spell level anyway (1st at level 1, 2nd at 3, 3rd at 5, 4th at
  /// 7, 5th at 9), so the pact never hands her a spell early.
  /// </summary>
  internal static class PactWizard
  {
    internal const string ArchetypeName = "PactWizardArchetype";

    public static void Configure()
    {
      var wizard = CharacterClassRefs.WizardClass.Reference.Get();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.PactWizardArchetype,
            CharacterClassRefs.WizardClass)
          .SetLocalizedName("PactWizard.Name")
          .SetLocalizedDescription("PactWizard.Description");

      archetype = PatronSpell(archetype, wizard, Guids.PactWizardPatron1,
        1, 1, AbilityRefs.CauseFear.Reference.Get(), "Cause Fear");
      archetype = PatronSpell(archetype, wizard, Guids.PactWizardPatron3,
        3, 2, AbilityRefs.ScorchingRay.Reference.Get(), "Scorching Ray");
      archetype = PatronSpell(archetype, wizard, Guids.PactWizardPatron5,
        5, 3, AbilityRefs.Fireball.Reference.Get(), "Fireball");
      archetype = PatronSpell(archetype, wizard, Guids.PactWizardPatron7,
        7, 4, AbilityRefs.Fear.Reference.Get(), "Fear");
      archetype = PatronSpell(archetype, wizard, Guids.PactWizardPatron9,
        9, 5, AbilityRefs.TrueSeeing.Reference.Get(), "True Seeing");

      archetype = ArchetypeRemovals.AddRemovals(
        archetype, wizard, FeatureSelectionRefs.ArcaneBondSelection.ToString());

      archetype.Configure();
      MissionFeats.Logger.Info("[pactwizard] configured: " + ArchetypeName + ".");
    }

    /// <summary>
    /// One patron spell: a class feature that writes a real spell into the
    /// real wizard book at the given spell level, granted at the class level
    /// where that spell level opens.
    /// </summary>
    private static ArchetypeConfigurator PatronSpell(
      ArchetypeConfigurator archetype,
      BlueprintCharacterClass wizard,
      string guid,
      int classLevel,
      int spellLevel,
      Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility spell,
      string spellName)
    {
      var key = "PactWizardPatron" + classLevel;
      // The icon is resolved inside the helper rather than passed in, so the
      // UnityEngine sprite type never has to be named in a signature.
      var feature = FeatureConfigurator.New("PactWizardPatron" + classLevel + "Feature", guid)
        .SetDisplayName(key + ".Name")
        .SetDescription("PactWizardPatronSpell.Description")
        .SetIcon(AbilityRefs.FalseLife.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddKnownSpell(characterClass: wizard, spell: spell, spellLevel: spellLevel)
        .Configure();
      MissionFeats.Logger.Info(
        $"[pactwizard] patron spell: {spellName} ({spellLevel}nd level) at class level {classLevel}.");
      return archetype.AddToAddFeatures(LevelPlan.L(classLevel), feature);
    }
  }
}

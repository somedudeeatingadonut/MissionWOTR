using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using MissionWOTR.Feats;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.42.0 — the Eldritch Scrapper (sorcerer archetype, Advanced Class
  /// Guide pg. 122). The user's requested tabletop port for the sorcerer.
  ///
  /// "An eldritch scrapper is usually spoiling for a fight, looking to
  /// prove that she's just as tough as a martial character... a fighting
  /// style that blends weapons with spells."
  ///
  /// THE TRADE (adapted, documented): the tabletop trades the 1st/9th/
  /// 15th-level bloodline powers for martial flexibility. In this engine
  /// bloodline powers live inside the bloodline blueprints' own
  /// progressions - not the class progression - so an archetype cannot
  /// remove them. Owlcat's OWN in-game Crossblooded archetype ships the
  /// same adaptation: it grants the second bloodline's powers in full and
  /// pays with SPELLS KNOWN instead (its custom spellbook, one fewer
  /// known per level). This port reuses that exact vanilla asset - the
  /// game's own balanced price - rather than inventing a new tax.
  ///
  /// THE GAIN: martial weapon proficiency (1st, faithful - the tabletop's
  /// martial-flexibility entry point) plus a bonus combat feat at 1st,
  /// 9th, and 15th - the three tiers the tabletop's flexibility reached
  /// (one feat, then two, then three at a time). The game has no brawler
  /// class and no mid-combat feat-picker, so flexibility's "borrow any
  /// combat feat for a minute" is substituted by real feat picks at the
  /// same tiers (the user's standing suitable-alternative rule). The
  /// Arcane Strike / Combat Casting rider and the Bloodline Weapons
  /// footnote are moot: she keeps her bloodline powers, natural attacks
  /// included.
  /// </summary>
  internal class EldritchScrapper
  {
    internal const string ArchetypeName = "EldritchScrapper";

    internal static void Configure()
    {
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.EldritchScrapperArchetype, CharacterClassRefs.SorcererClass)
          .SetLocalizedName("EldritchScrapper.Name")
          .SetLocalizedDescription("EldritchScrapper.Description")
          // The adapted trade: the vanilla crossblooded spellbook - one
          // fewer spell known per level, the game's own price for
          // bloodline-strength trades.
          .SetReplaceSpellbook(
            SpellbookRefs.CrossbloodedSpellbook.Cast<BlueprintSpellbookReference>())
          .AddToAddFeatures(LevelPlan.L(1),
            FeatureRefs.MartialWeaponProficiency.Cast<BlueprintFeatureBaseReference>())
          .AddToAddFeatures(LevelPlan.L(1),
            FeatureSelectionRefs.FighterFeatSelection.Cast<BlueprintFeatureBaseReference>())
          .AddToAddFeatures(LevelPlan.L(9),
            FeatureSelectionRefs.FighterFeatSelection.Cast<BlueprintFeatureBaseReference>())
          .AddToAddFeatures(LevelPlan.L(15),
            FeatureSelectionRefs.FighterFeatSelection.Cast<BlueprintFeatureBaseReference>());
      archetype.Configure();
      MissionFeats.Logger.Info("[scrapper] configured: " + ArchetypeName + ".");
    }
  }
}

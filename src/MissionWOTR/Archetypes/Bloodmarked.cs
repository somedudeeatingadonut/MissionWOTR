using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using MissionWOTR.Feats;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Bloodmarked — Ultimate Magic pg. 77. The weretouched sorcerer: the bite
  /// is in the bloodline whether she wants it or not.
  ///
  /// 0.54.0 replacement for the Eldritch Scrapper, withdrawn in 0.53.0 as a
  /// duplicate of Ebon's Content Mod. Trap-checked against the loaded-mod
  /// list in the 0.52.1 log: vanilla ships Crossblooded, Seeker, Sage,
  /// Empyreal, Sylvan and the Nine-Tailed Heir; Ebon's ships exactly three
  /// archetypes (Collegiate Initiate, Eldritch Scrapper, Hungry Ghost Monk);
  /// HomebrewArchetypes ships no sorcerer archetypes at all. Bloodmarked is
  /// unclaimed.
  ///
  /// THE TRADE: one fewer spell known per level, taken by replacing her book
  /// with the game's own Crossblooded book — the same lever the withdrawn
  /// Eldritch Scrapper used and the one this repo documented as the working
  /// route, since the crossblooded price is baked into that book's
  /// spells-known table.
  ///
  /// DOCUMENTED CUT: the tabletop's Skinchange (9th) is an animal-form
  /// transformation, and the standing rule for this mod is no
  /// self-transformation on a full caster. What ships in its place is the
  /// hide — the weretouched toughness the change of shape was always about.
  /// </summary>
  internal static class Bloodmarked
  {
    internal const string ArchetypeName = "BloodmarkedArchetype";

    public static void Configure()
    {
      var icon = FeatureRefs.AnimalFuryFeature.Reference.Get().Icon;

      // Bloodmark (1st): the bite, and the weretouched resilience that goes
      // with carrying it.
      var bloodmark = FeatureConfigurator.New("BloodmarkedMark", Guids.Bloodmark)
        .SetDisplayName("Bloodmark.Name")
        .SetDescription("Bloodmark.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { FeatureRefs.AnimalFuryFeature.Reference.Get() })
        .AddStatBonus(stat: StatType.SaveWill, value: 2,
          descriptor: ModifierDescriptor.UntypedStackable)
        .Configure();

      // Skinchange's stand-in (9th): the hide underneath.
      var hide = FeatureConfigurator.New("BloodmarkedHide", Guids.BloodmarkedHide)
        .SetDisplayName("BloodmarkedHide.Name")
        .SetDescription("BloodmarkedHide.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.AC, value: 1, descriptor: ModifierDescriptor.NaturalArmor)
        .Configure();

      ArchetypeConfigurator.New(ArchetypeName, Guids.BloodmarkedArchetype,
          CharacterClassRefs.SorcererClass)
        .SetLocalizedName("Bloodmarked.Name")
        .SetLocalizedDescription("Bloodmarked.Description")
        .SetReplaceSpellbook(SpellbookRefs.CrossbloodedSpellbook.Reference.Get())
        .AddToAddFeatures(LevelPlan.L(1), bloodmark)
        .AddToAddFeatures(LevelPlan.L(9), hide)
        .Configure();

      MissionFeats.Logger.Info("[bloodmarked] configured: " + ArchetypeName + ".");
    }
  }
}

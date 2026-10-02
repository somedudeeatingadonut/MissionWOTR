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
  /// SKINCHANGE (9th): the real thing. The no-self-transformation rule the
  /// user set was scoped to one archetype, not to the mod, so this is a
  /// genuine change of shape - the shifter's own wolf form, granted as a fact
  /// exactly the way Beastbound references it. She keeps a +1 natural armor
  /// bonus underneath, because the hide does not go away when she does.
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

      // Skinchange (9th): the wolf, and the hide that goes with it.
      var skinchange = FeatureConfigurator.New("BloodmarkedSkinchange", Guids.BloodmarkedSkinchange)
        .SetDisplayName("BloodmarkedSkinchange.Name")
        .SetDescription("BloodmarkedSkinchange.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { FeatureRefs.ShifterWildShapeWolfFeature.Reference.Get() })
        .AddStatBonus(stat: StatType.AC, value: 1, descriptor: ModifierDescriptor.NaturalArmor)
        .Configure();

      ArchetypeConfigurator.New(ArchetypeName, Guids.BloodmarkedArchetype,
          CharacterClassRefs.SorcererClass)
        .SetLocalizedName("Bloodmarked.Name")
        .SetLocalizedDescription("Bloodmarked.Description")
        .SetReplaceSpellbook(SpellbookRefs.CrossbloodedSpellbook.Reference.Get())
        .AddToAddFeatures(LevelPlan.L(1), bloodmark)
        .AddToAddFeatures(LevelPlan.L(9), skinchange)
        .Configure();

      MissionFeats.Logger.Info("[bloodmarked] configured: " + ArchetypeName + ".");
    }
  }
}

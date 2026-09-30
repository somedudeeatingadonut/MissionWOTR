using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MissionWOTR.Feats;
using System.Collections.Generic;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// SPEAKER FOR THE PAST (shaman tabletop port - Advanced Class
  /// Guide pg. 111, the first shaman archetype in this mod; the
  /// game's own shaman archetypes left it free to take).
  ///
  /// The tabletop brief: "A speaker for the past is a shaman who
  /// specifically serves as the voice for spirits from her people's
  /// history... the voice of experience, and a powerful resource
  /// that allows the past to aid the present."
  ///
  /// The trades (ACG): "Mysteries of the Past... replaces the
  /// shaman's familiar"; "Revelations of the Past... replaces
  /// wandering spirit and wandering hex."
  /// - The spirit familiar (level 1) goes, found in the live
  ///   progression by name - as do the wandering spirit and
  ///   wandering hex grants (the whole chain: the candidate names
  ///   are scanned one by one; any that are not progression grants
  ///   are skipped with a warning, which is expected noise for a
  ///   couple of them). Her MAIN spirit and her ordinary hexes are
  ///   untouched.
  /// - Mysteries of the Past: class skills (Knowledge [history and
  ///   local] -> Knowledge World, Perception, Use Magic Device;
  ///   Linguistics has no Wrath skill and is a documented cut) and
  ///   the ancestors spells on her list. Of the nine ancestor
  ///   mystery bonus spells only heroism and greater heroism exist
  ///   in the game - they are granted as bonus known spells (the
  ///   witch patron's mechanism, the prepared-caster equivalent of
  ///   a list addition); the other seven are a documented cut.
  /// - Revelations of the Past at 4th, 6th, 12th, 14th and 20th:
  ///   a selection of the ancestors mystery's revelations as
  ///   CLONES (prerequisites stripped at copy time; the two
  ///   11th-level ones re-gated on SHAMAN level 11). The time
  ///   mystery half of the archetype is a documented cut - Wrath
  ///   has no time mystery. Scaling note: the clones reference the
  ///   vanilla oracle abilities, whose level hooks stay as Owlcat
  ///   wrote them; revelations that scale strictly off oracle
  ///   levels may under-scale for a shaman (the ancestor set is
  ///   mostly flat and self-buff effects). The probe script now
  ///   dumps the rank-config and ability-params field maps so a
  ///   playtest round can tune this precisely.
  /// Log prefix: [speaker].
  /// </summary>
  internal static class SpeakerForThePast
  {
    internal const string ArchetypeName = "SpeakerArchetype";

    public static void Configure()
    {
      var shaman = CharacterClassRefs.ShamanClass.Reference.Get();
      var icon = FeatureRefs.OracleAncestorsMysteryFeature.Reference.Get().Icon;

      // ----- Mysteries of the Past (the level-1 trade) -----
      var past = FeatureConfigurator.New("SpeakerPast", Guids.SpeakerPast)
        .SetDisplayName("SpeakerPast.Name")
        .SetDescription("SpeakerPast.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddClassSkill(StatType.KnowledgeWorld)
        .AddClassSkill(StatType.Perception)
        .AddClassSkill(StatType.UseMagicDevice)
        // The ancestors' spells, as bonus known (the witch patron's
        // mechanism). Only heroism and greater heroism exist in the
        // game; the other seven ancestor spells are a documented cut.
        .AddKnownSpell(
          characterClass: CharacterClassRefs.ShamanClass.Cast<BlueprintCharacterClassReference>(),
          spell: AbilityRefs.Heroism.Cast<BlueprintAbilityReference>(),
          spellLevel: 3)
        .AddKnownSpell(
          characterClass: CharacterClassRefs.ShamanClass.Cast<BlueprintCharacterClassReference>(),
          spell: AbilityRefs.HeroismGreater.Cast<BlueprintAbilityReference>(),
          spellLevel: 6)
        .Configure();

      // ----- Revelations of the Past (4th/6th/12th/14th/20th) -----
      var revelations = new List<BlueprintFeature>
      {
        Revelation("SpeakerBloodOfHeroesRevelation", Guids.SpeakerBloodOfHeroesRevelation,
          FeatureRefs.OracleRevelationBloodOfHeroes, minLevel: 0),
        Revelation("SpeakerPhantomTouchRevelation", Guids.SpeakerPhantomTouchRevelation,
          FeatureRefs.OracleRevelationPhantomTouch, minLevel: 0),
        Revelation("SpeakerSacredCouncilRevelation", Guids.SpeakerSacredCouncilRevelation,
          FeatureRefs.OracleRevelationSacredCouncil, minLevel: 0),
        Revelation("SpeakerSpiritShieldRevelation", Guids.SpeakerSpiritShieldRevelation,
          FeatureRefs.OracleRevelationSpiritShield, minLevel: 0),
        Revelation("SpeakerStormOfSoulsRevelation", Guids.SpeakerStormOfSoulsRevelation,
          FeatureRefs.OracleRevelationStormOfSouls, minLevel: 0),
        // The two 11th-level revelations, re-gated on shaman level.
        Revelation("SpeakerSpiritOfTheWarriorRevelation", Guids.SpeakerSpiritOfTheWarriorRevelation,
          FeatureRefs.OracleRevelationSpiritOfTheWarrior, minLevel: 11),
        Revelation("SpeakerSpiritWalkRevelation", Guids.SpeakerSpiritWalkRevelation,
          FeatureRefs.OracleRevelationSpiritWalk, minLevel: 11),
      };

      var selection = FeatureSelectionConfigurator.New(
        "SpeakerRevelationSelection", Guids.SpeakerRevelationSelection)
        .SetDisplayName("SpeakerRevelationSelection.Name")
        .SetDescription("SpeakerRevelationSelection.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .SetMode(SelectionMode.OnlyNew)
        .Configure();
      FeatureSelectionConfigurator.For(selection)
        .AddToAllFeatures(
          Guids.SpeakerBloodOfHeroesRevelation, Guids.SpeakerPhantomTouchRevelation,
          Guids.SpeakerSacredCouncilRevelation, Guids.SpeakerSpiritShieldRevelation,
          Guids.SpeakerStormOfSoulsRevelation, Guids.SpeakerSpiritOfTheWarriorRevelation,
          Guids.SpeakerSpiritWalkRevelation)
        .Configure();

      // ----- The archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.SpeakerArchetype, CharacterClassRefs.ShamanClass)
          .SetLocalizedName("Speaker.Name")
          .SetLocalizedDescription("Speaker.Description")
          .AddToAddFeatures(LevelPlan.L(1), past)
          .AddToAddFeatures(LevelPlan.L(4), selection)
          .AddToAddFeatures(LevelPlan.L(6), selection)
          .AddToAddFeatures(LevelPlan.L(12), selection)
          .AddToAddFeatures(LevelPlan.L(14), selection)
          .AddToAddFeatures(LevelPlan.L(20), selection);

      // The trades: the spirit familiar, and the whole wandering
      // chain. The names are scanned in the LIVE progression; the
      // handful that are not direct progression grants warn and
      // skip (expected for a couple of candidates).
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, shaman, "ShamanSpiritAnimalSelection");
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, shaman, "ShamanWanderingSpirit");
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, shaman, "ShamanWanderingSpiritFeature");
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, shaman, "ShamanWanderingHex");
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, shaman, "ShamanWanderingHexFeature");
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, shaman, "ShamanWanderingHexFeature2");

      archetype.Configure();
      MissionFeats.Logger.Info("[speaker] configured: " + ArchetypeName + ".");
    }

    /// <summary>
    /// One revelation of the ancestors, as a self-contained clone:
    /// everything copied EXCEPT prerequisites (the vanilla ones
    /// point at oracle levels a shaman never reaches), the
    /// 11th-level ones re-gated on shaman level. The clone keeps
    /// the vanilla components - including the oracle abilities they
    /// reference - read-only, so nothing of the vanilla mystery is
    /// touched (bpcore's copies share component instances; the
    /// 0.33.0 chimera taught us what editing shared components
    /// does).
    /// </summary>
    private static BlueprintFeature Revelation(
      string name,
      string guid,
      Blueprint<BlueprintReference<BlueprintFeature>> source,
      int minLevel)
    {
      var builder = FeatureConfigurator.New(name, guid)
        .CopyFrom(source, c => !(c is Prerequisite))
        .SetIsClassFeature();
      if (minLevel > 0)
      {
        builder = builder.AddPrerequisiteClassLevel(
          CharacterClassRefs.ShamanClass.Cast<BlueprintCharacterClassReference>(), minLevel);
      }
      return builder.Configure();
    }
  }
}

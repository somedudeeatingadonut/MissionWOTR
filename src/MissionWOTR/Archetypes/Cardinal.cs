using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker.EntitySystem.Stats;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Cardinal (faithful port of the Ultimate Intrigue cleric archetype).
  ///
  /// Tabletop (Ultimate Intrigue):
  /// - Armor Proficiency: a cardinal is proficient with light armor, but not
  ///   with medium armor or shields. This replaces the cleric's armor
  ///   proficiencies.
  /// - Political Skill: a cardinal adds Bluff, Intimidate, Knowledge
  ///   (geography) and Knowledge (local) to her class skills, and gains 6 +
  ///   Int skill ranks per level instead of 2 + Int. However, she gains only
  ///   ONE domain, and her base attack bonus from cleric levels is equal to
  ///   half her class level (as a sorcerer or wizard). This replaces the
  ///   cleric's spontaneous casting ability and alters her domains, class
  ///   skills, skill ranks and base attack bonus.
  ///
  /// Coverage: HomebrewArchetypes (the user's other mod) already ships
  /// Crusader, Divine Agent, Elder Mythos Cultist, Evangelist and Undead Lord;
  /// the base game ships Crusader, Divine Commander, Ecclesitheurge, Herald
  /// Caller, Angelfire Apostle and Priest of Balance. Cardinal is free.
  ///
  /// Adaptation notes:
  /// - WOTR folds Bluff/Intimidate/Diplomacy into Persuasion and the
  ///   Knowledges into Lore/Knowledge skills; clerics ALREADY count Knowledge
  ///   (Arcana), Knowledge (World), Lore (Religion) and Persuasion as class
  ///   skills, so the tabletop's four extra skills collapse into existing
  ///   ones. Trickery (the game's deception skill) is granted as the
  ///   intrigue-flavored stand-in so Political Skill keeps some skill teeth.
  /// - 6 + Int ranks: BlueprintArchetype.AddSkillPoints = 4 on top of the
  ///   cleric's 2 + Int (the DarkCodex archetype recipe).
  /// - Half BAB: the archetype carries the slow BAB progression table
  ///   (StatProgressionRefs.BABLow, the wizard/sorcerer table).
  /// - Removing the vanilla ClericProficiencies package drops medium armor,
  ///   shields and the weapon package at once; simple weapons and light armor
  ///   are re-added as separate vanilla features. If the deity's favored
  ///   weapon proficiency rides the removed package, it is lost - documented
  ///   as the honest cost of the armor trade (retrain Weapon Proficiency if
  ///   the deity's weapon matters).
  /// - One domain: the second domain selection is removed at the level the
  ///   live progression actually grants it (ArchetypeRemovals.AddRemovals).
  /// - Spontaneous casting: removed via the ClericSpontaneousCast feature; if
  ///   the live progression does not carry that feature by name, the removal
  ///   is skipped with a warning (the trade then under-delivers - visible in
  ///   the log).
  /// Log prefix: [removals] carries the removal diagnostics.
  /// </summary>
  internal static class Cardinal
  {
    internal const string ArchetypeName = "CardinalArchetype";
    internal const string PoliticalSkillName = "CardinalPoliticalSkill";

    public static void Configure()
    {
      var cleric = CharacterClassRefs.ClericClass.Reference.Get();

      // ----- Political Skill (1st level feature) -----
      var politicalSkill = FeatureConfigurator.New(PoliticalSkillName, Guids.CardinalPoliticalSkill)
        .SetDisplayName("CardinalPoliticalSkill.Name")
        .SetDescription("CardinalPoliticalSkill.Description")
        .SetIcon(FeatureRefs.SkillFocusDiplomacy.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddClassSkill(StatType.SkillPersuasion)
        .AddClassSkill(StatType.SkillLoreWorld)
        .AddClassSkill(StatType.SkillThievery)
        .Configure();

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.CardinalArchetype, CharacterClassRefs.ClericClass)
          .SetLocalizedName("Cardinal.Name")
          .SetLocalizedDescription("Cardinal.Description")
          // Political Skill: 6 + Int skill ranks per level (cleric: 2 + Int).
          .SetAddSkillPoints(4)
          // Political Skill: half base attack bonus (the wizard table).
          .SetBaseAttackBonus(StatProgressionRefs.BABLow)
          .AddToAddFeatures(LevelPlan.L(1), politicalSkill)
          // Armor Proficiency: light armor only, no shields; simple weapons stay.
          .AddToAddFeatures(LevelPlan.L(1), FeatureRefs.LightArmorProficiency)
          .AddToAddFeatures(LevelPlan.L(1), FeatureRefs.SimpleWeaponProficiency);

      // Armor package (medium + shields + weapons), the second domain, and
      // spontaneous casting - all removed at the levels the live progression
      // actually grants them.
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, cleric,
        "ClericProficiencies",
        "SecondDomainsSelection",
        "ClericSpontaneousCast");

      archetype.Configure();

      MissionFeats.Logger.Info("Cardinal: configured.");
    }
  }
}

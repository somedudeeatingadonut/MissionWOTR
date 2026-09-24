using BlueprintCore.Blueprints.Configurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Mummer Mage (faithful port of the Legendary Games bard archetype; WOTR
  /// adaptations noted in docs/ARCHETYPES.md).
  ///
  /// Tabletop:
  /// - Shtick of the Magi (1st): ornate masterwork quarterstaff prop; +2
  ///   circumstance to Perform (act/oratory) and Bluff to sell the arcane persona.
  ///   From 5th, it functions as a wizard's bonded item. Replaces bardic knowledge.
  /// - Imperious Gestures (2nd): +2 Concentration on bard spells with somatic
  ///   components. Replaces well-versed.
  /// - Arcane Imitation (2nd): add a wizard-list spell to bard spells known (or
  ///   two lower-level ones); more picks at 6th and every 4 levels after.
  ///   Replaces versatile performance.
  /// - Method Actor (5th): bard level counts as wizard level for feat
  ///   qualification. Replaces lore master.
  /// - Eucatastrophe (10th): 1/day cast any bard- or wizard-list spell of a
  ///   castable level, even unlearned (2/day at 16th, 3/day at 19th).
  ///   Replaces jack-of-all-trades.
  ///
  /// WOTR adaptations (engine gaps):
  /// - No Perform or Bluff skills: the Shtick's bonus applies to Persuasion (the
  ///   stand-in for both). The physical staff is flavor - any quarterstaff serves,
  ///   and the prop is modeled as its own feature, the same way the Magic Deceiver
  ///   archetype carries its mask as a class-granted signature.
  /// - The 5th-level bonded-item benefit grants the vanilla BondedItem feature
  ///   (2fb5e65bd57caa943b45ee32d825e9b9): once per day, cast any spell from her
  ///   spellbook through the prop, as a full-round action.
  /// - Versatile performance does not exist in WOTR (no Perform skill), so Arcane
  ///   Imitation's trade is void - the feature is additive, like Breaker's Battle
  ///   Scavenger. Each pick learns one wizard spell (of a level she can cast); the
  ///   tabletop's "or two lower-level spells" clause is simplified away.
  /// - Method Actor: WOTR has no wizard-level feat prerequisites to redirect, so
  ///   the persona mastery becomes: all Knowledge (Lore) skills use Charisma
  ///   instead of Intelligence - she remembers playing a scholar convincingly.
  /// - Eucatastrophe: the engine has no "cast any unlearned spell" UI outside the
  ///   bonded item, so it is adapted into extra Arcane Imitation picks at 10th,
  ///   16th and 19th (the role becomes real at the climax), on top of the bonded
  ///   prop's once-a-day any-known-spell cast granted at 5th.
  /// </summary>
  internal static class MummerMage
  {
    internal const string ArchetypeName = "MummerMageArchetype";
    internal const string DisplayName = "MummerMage.Name";
    internal const string Description = "MummerMage.Description";

    internal const string ShtickName = "MummerMageShtick";
    internal const string ImperiousName = "MummerMageImperiousGestures";
    internal const string ImitationName = "MummerMageArcaneImitation";
    internal const string AttunementName = "MummerMageShtickAttunement";
    internal const string MethodActorName = "MummerMageMethodActor";
    internal const string EucatastropheName = "MummerMageEucatastrophe";

    // Vanilla: the wizard's bonded item feature (TheLostGrimoire-verified GUID).
    private const string BondedItemGuid = "2fb5e65bd57caa943b45ee32d825e9b9";

    public static void Configure()
    {
      var bard = CharacterClassRefs.BardClass.Reference.Get();
      var wizardList = SpellListRefs.WizardSpellList.Reference.Get();

      // ----- Shtick of the Magi (1st): the prop -----
      var shtick = FeatureConfigurator.New(ShtickName, Guids.MummerShtick)
        .SetDisplayName("MummerShtick.Name")
        .SetDescription("MummerShtick.Description")
        .SetIcon(FeatureRefs.ImprovedSunder.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.SkillPersuasion, value: 2,
          descriptor: ModifierDescriptor.Circumstance)
        .Configure();

      // ----- Imperious Gestures (2nd): flourishes that ward the spell -----
      var imperious = FeatureConfigurator.New(ImperiousName, Guids.MummerImperious)
        .SetDisplayName("MummerImperious.Name")
        .SetDescription("MummerImperious.Description")
        .SetIcon(FeatureRefs.Toughness.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new MummerImperiousConcentration { Bonus = 2 })
        .Configure();

      // ----- Arcane Imitation (2nd): steal from the wizard's list -----
      var imitation = ParametrizedFeatureConfigurator.New(ImitationName, Guids.MummerImitation)
        .SetDisplayName("MummerImitation.Name")
        .SetDescription("MummerImitation.Description")
        .SetIcon(AbilityRefs.BombStandart.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(BuildImitation(bard, wizardList))
        .Configure();

      // ----- Shtick Attunement (5th): the prop becomes a bonded item -----
      var bondedItem = BlueprintTool.Get<BlueprintFeature>(BondedItemGuid);
      var attunement = FeatureConfigurator.New(AttunementName, Guids.MummerAttunement)
        .SetDisplayName("MummerAttunement.Name")
        .SetDescription("MummerAttunement.Description")
        .SetIcon(bondedItem.Icon)
        .SetIsClassFeature()
        .AddFacts(new() { bondedItem })
        .Configure();

      // ----- Method Actor (5th): the persona is convincing -----
      var methodActor = FeatureConfigurator.New(MethodActorName, Guids.MummerMethodActor)
        .SetDisplayName("MummerMethodActor.Name")
        .SetDescription("MummerMethodActor.Description")
        .SetIcon(FeatureRefs.Toughness.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new ReplaceStatBaseAttribute
        {
          TargetStat = StatType.SkillKnowledgeArcana,
          BaseAttributeReplacement = StatType.Charisma,
        })
        .AddComponent(new ReplaceStatBaseAttribute
        {
          TargetStat = StatType.SkillLoreReligion,
          BaseAttributeReplacement = StatType.Charisma,
        })
        .AddComponent(new ReplaceStatBaseAttribute
        {
          TargetStat = StatType.SkillLoreNature,
          BaseAttributeReplacement = StatType.Charisma,
        })
        .AddComponent(new ReplaceStatBaseAttribute
        {
          TargetStat = StatType.SkillKnowledgeWorld,
          BaseAttributeReplacement = StatType.Charisma,
        })
        .Configure();

      // ----- Eucatastrophe (10th): the climax (marker; the picks are extra
      // Arcane Imitation grants, the bond cast covers "any spell she knows") -----
      var eucatastrophe = FeatureConfigurator.New(EucatastropheName, Guids.MummerEucatastrophe)
        .SetDisplayName("MummerEucatastrophe.Name")
        .SetDescription("MummerEucatastrophe.Description")
        .SetIcon(AbilityRefs.BloodragerInfernalHellfireStrikeAbility.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.MummerArchetype, CharacterClassRefs.BardClass)
          .SetLocalizedName(DisplayName)
          .SetLocalizedDescription(Description);

      // Trades: bardic knowledge (Shtick), well-versed (Imperious Gestures),
      // lore master (Method Actor), jack-of-all-trades (Eucatastrophe).
      // Versatile performance does not exist in WOTR - the name is passed
      // speculatively and safely skipped if unfound.
      archetype = ArchetypeRemovals.AddRemovals(
        archetype,
        bard,
        FeatureRefs.BardicKnowledge.ToString(),
        FeatureRefs.BardWellVersed.ToString(),
        FeatureRefs.BardLoreMaster.ToString(),
        FeatureRefs.BardJackOfAllTrades.ToString(),
        "VersatilePerformance",
        "VersatilePerformanceSelection");

      archetype = archetype
        .AddToAddFeatures(LevelPlan.L(1), ShtickName)
        .AddToAddFeatures(LevelPlan.L(2), ImperiousName, ImitationName)
        .AddToAddFeatures(LevelPlan.L(5), AttunementName, MethodActorName)
        .AddToAddFeatures(LevelPlan.L(6), ImitationName)
        .AddToAddFeatures(LevelPlan.L(10), EucatastropheName, ImitationName)
        .AddToAddFeatures(LevelPlan.L(14), ImitationName)
        .AddToAddFeatures(LevelPlan.L(16), ImitationName)
        .AddToAddFeatures(LevelPlan.L(18), ImitationName)
        .AddToAddFeatures(LevelPlan.L(19), ImitationName);

      archetype.Configure();

      MissionFeats.Logger.Info("MummerMage: configured.");
    }

    /// <summary>
    /// LearnSpellParametrized with its class and spell list set by reflection -
    /// the properties are read-only in this build (the same treatment as the
    /// construct fake-class levels).
    /// </summary>
    private static Kingmaker.Designers.Mechanics.Facts.LearnSpellParametrized BuildImitation(
      BlueprintCharacterClass bard, Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList wizardList)
    {
      var component = new Kingmaker.Designers.Mechanics.Facts.LearnSpellParametrized();
      const System.Reflection.BindingFlags flags =
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.NonPublic;
      var classField = typeof(Kingmaker.Designers.Mechanics.Facts.LearnSpellParametrized)
        .GetField("m_SpellcasterClass", flags);
      var listField = typeof(Kingmaker.Designers.Mechanics.Facts.LearnSpellParametrized)
        .GetField("m_SpellList", flags);
      if (classField is null || listField is null)
      {
        MissionFeats.Logger.Warn(
          "[mummer] LearnSpellParametrized field names not found - Arcane Imitation picks will be broken.");
        return component;
      }
      classField.SetValue(component, bard.ToReference<BlueprintCharacterClassReference>());
      listField.SetValue(component, wizardList.ToReference<BlueprintSpellListReference>());
      return component;
    }
  }

  /// <summary>
  /// Imperious Gestures: +2 on concentration checks while casting her spells.
  /// WOTR exposes almost no concentration surface (there is no concentration
  /// stat), so the bonus is applied where the build allows it: the DC is lowered
  /// when it is visible (CustomDC), or a bonus-shaped int member is adjusted by
  /// reflection. If neither path exists, a warning is logged once.
  /// </summary>
  [TypeId(Guids.MummerImperiousComponent)]
  internal class MummerImperiousConcentration : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCheckConcentration>, IRulebookHandler<RuleCheckConcentration>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public int Bonus;

    private static bool m_Warned;

    public void OnEventAboutToTrigger(RuleCheckConcentration evt)
    {
      try
      {
        if (evt.CustomDC.HasValue)
        {
          evt.CustomDC = evt.CustomDC.Value - Bonus;
          return;
        }
        const System.Reflection.BindingFlags flags =
          System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
          System.Reflection.BindingFlags.NonPublic;
        foreach (var name in new[] { "Bonus", "m_Bonus", "ConcentrationBonus" })
        {
          var field = typeof(RuleCheckConcentration).GetField(name, flags);
          if (field != null && field.FieldType == typeof(int))
          {
            field.SetValue(evt, (int)field.GetValue(evt) + Bonus);
            return;
          }
          var prop = typeof(RuleCheckConcentration).GetProperty(name, flags);
          if (prop != null && prop.CanWrite && prop.PropertyType == typeof(int))
          {
            prop.SetValue(evt, (int)prop.GetValue(evt, null) + Bonus, null);
            return;
          }
        }
        if (!m_Warned)
        {
          m_Warned = true;
          MissionFeats.Logger.Warn(
            "[mummer] Imperious Gestures: no concentration adjustment path found on this build.");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("MummerMage: Imperious Gestures failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCheckConcentration evt) { }
  }
}

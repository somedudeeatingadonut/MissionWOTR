using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// True Shape (homebrew druid archetype, user design: "a more permanent
  /// wild shape, with more wild shape forms to choose from for various
  /// roles, that also gains some stats from the equipment you are wearing,
  /// in exchange for significantly less spellcasting, and losing detect
  /// magic").
  ///
  /// Design:
  /// - True Shape: while the Beast Soul toggle is on, whatever form the
  ///   druid wears does not lapse - its duration is replenished every round
  ///   shortly before it would expire. Shifting back is always possible (the
  ///   refresh stops the moment the form is gone).
  /// - The Menagerie: the druid's wild shape is REPLACED by a broad stable
  ///   of forms drawn from the shifter's arsenal, each a complete package
  ///   (model, attacks, special abilities) - tank (bear, elephant,
  ///   dinosaur), predator (tiger/smilodon, wolf, wolverine, boar),
  ///   utility/control (spider, fey, manticore, griffon) - with the stronger
  ///   tier of each family unlocking at 8th and the final tier at 15th.
  /// - Wild Attunement: while in any beast form, the druid's worn armor and
  ///   shield keep protecting him (their full AC, read live from the
  ///   equipment) - beast shape without abandoning the armory.
  /// - The cost: spellcasting is cut significantly - the spellbook stops at
  ///   4th-level spells - and detect magic is lost outright (any spell whose
  ///   name matches is filtered from the cloned list).
  ///
  /// Engine notes:
  /// - The reduced spellbook is a clone of the druid book (the Sanguine Font
  ///   reflection-copy pattern) carrying a cloned spell list (levels 0-4 of
  ///   the live druid list, minus detect-magic-named spells), attached via
  ///   the archetype's ReplaceSpellbook - the engine-native archetype
  ///   spellbook swap. Higher-level spell slots atrophy with the list.
  /// - Elemental wild shape is a separate feature family and is KEPT - only
  ///   the animal-form package (DruidWildShape) is removed.
  /// - Form detection uses Body.IsPolymorphed (the pplus ArmorUnlockPP
  ///   idiom); the refresh re-applies the exact active polymorph buff
  ///   (matched by a Polymorph-named component) with a long duration when
  ///   its remaining time runs low (Buff.TimeLeft, the pplus
  ///   TransformOthersDuration idiom), remove-then-add so no double buff can
  ///   accrue (the Untouchable Rager maintenance idiom).
  /// - Attunement reads the worn armor/shield AC by reflection (member "AC",
  ///   falling back to the blueprint's base value; the equipped item's exact
  ///   members vary across game builds) and adds it as an Armor-descriptor
  ///   modifier on RuleCalculateAC while polymorphed (the pplus TitanMauler
  ///   handler pattern) - recomputed on every attack, so gear swaps apply
  ///   immediately.
  /// Log prefix: [trueshape].
  /// </summary>
  internal static class TrueShape
  {
    internal const string ArchetypeName = "TrueShapeArchetype";
    internal const string FeatureName = "TrueShapeFeature";
    internal const string SoulName = "TrueShapeBeastSoul";
    internal const string MenagerieName = "TrueShapeMenagerie";

    public static void Configure()
    {
      var druid = CharacterClassRefs.DruidClass.Reference.Get();
      var druidBook = SpellbookRefs.DruidSpellbook.Reference.Get();
      var tigerIcon = AbilityRefs.ShifterWildShapeTigerAbillity.Reference.Get().Icon;

      // ----- The reduced spell list: levels 0-4, no detect magic -----
      var trimmedList = BlueprintTool.Create<BlueprintSpellList>(
        "TrueShapeSpellList", Guids.TrueShapeSpellList);
      var trimmed = new SpellLevelList[5];
      for (int i = 0; i < 5; i++)
      {
        trimmed[i] = new SpellLevelList(i) { SpellLevel = i };
        var source = druidBook.SpellList?.SpellsByLevel?
          .FirstOrDefault(e => e != null && e.SpellLevel == i);
        if (source == null)
        {
          continue;
        }
        foreach (var spell in source.Spells)
        {
          if (spell == null)
          {
            continue;
          }
          // Losing detect magic is part of the trade - filtered by name so
          // mod-added versions (e.g. Call of the Wild) are caught too.
          if (spell.name != null &&
            spell.name.IndexOf("DetectMagic", StringComparison.OrdinalIgnoreCase) >= 0)
          {
            MissionFeats.Logger.Info($"[trueshape] detect magic filtered: {spell.name}.");
            continue;
          }
          ElementalObsessor.AddSpellToEntry(trimmed[i], spell);
        }
      }
      trimmedList.SpellsByLevel = trimmed;
      var book = CloneSpellbook(druidBook, trimmedList, "TrueShapeSpellbook", Guids.TrueShapeSpellbook);

      // ----- Beast Soul (the permanence toggle) -----
      var soulBuff = BuffConfigurator.New(SoulName + "Buff", Guids.TrueShapeBeastSoulBuff)
        .SetDisplayName(SoulName + ".Name")
        .SetDescription(SoulName + ".Description")
        .SetIcon(AbilityRefs.ShifterWildShapeWolfAbillity.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();
      var soulToggle = ActivatableAbilityConfigurator.New(
          SoulName + "Activatable", Guids.TrueShapeBeastSoulActivatable)
        .SetDisplayName(SoulName + ".Name")
        .SetDescription(SoulName + ".Description")
        .SetIcon(AbilityRefs.ShifterWildShapeWolfAbillity.Reference.Get().Icon)
        .SetBuff(soulBuff)
        .SetIsOnByDefault(true)
        .Configure();

      // ----- The Menagerie -----
      var menagerie = FeatureConfigurator.New(MenagerieName, Guids.TrueShapeMenagerie)
        .SetDisplayName(MenagerieName + ".Name")
        .SetDescription(MenagerieName + ".Description")
        .SetIcon(AbilityRefs.ShifterWildShapeBearAbillity.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new()
        {
          AbilityRefs.ShifterWildShapeBearAbillity.Reference.Get(),
          AbilityRefs.ShifterWildShapeBoarAbillity.Reference.Get(),
          AbilityRefs.ShifterWildShapeDinosaurAbillity.Reference.Get(),
          AbilityRefs.ShifterWildShapeElephantAbillity.Reference.Get(),
          AbilityRefs.ShifterWildShapeFeyAbillity.Reference.Get(),
          AbilityRefs.ShifterWildShapeGriffonAbillity.Reference.Get(),
          AbilityRefs.ShifterWildShapeManticoreAbillity.Reference.Get(),
          AbilityRefs.ShifterWildShapeSpiderAbillity.Reference.Get(),
          AbilityRefs.ShifterWildShapeTigerAbillity.Reference.Get(),
          AbilityRefs.ShifterWildShapeWolfAbillity.Reference.Get(),
          AbilityRefs.ShifterWildShapeWolverineAbillity.Reference.Get(),
        })
        .Configure();
      var menagerie8 = FeatureConfigurator.New(MenagerieName + "8", Guids.TrueShapeMenagerie8)
        .SetDisplayName(MenagerieName + "8.Name")
        .SetDescription(MenagerieName + "8.Description")
        .SetIcon(AbilityRefs.ShifterWildShapeElephantAbillity.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new()
        {
          AbilityRefs.ShifterWildShapeBearAbillity8.Reference.Get(),
          AbilityRefs.ShifterWildShapeBoarAbillity8.Reference.Get(),
          AbilityRefs.ShifterWildShapeDinosaurAbillity8.Reference.Get(),
          AbilityRefs.ShifterWildShapeElephantAbillity8.Reference.Get(),
          AbilityRefs.ShifterWildShapeFeyAbillity8.Reference.Get(),
          AbilityRefs.ShifterWildShapeGriffonAbillity9.Reference.Get(),
          AbilityRefs.ShifterWildShapeManticoreAbillity8.Reference.Get(),
          AbilityRefs.ShifterWildShapeSpiderAbillity8.Reference.Get(),
          AbilityRefs.ShifterWildShapeTigerAbillity8.Reference.Get(),
          AbilityRefs.ShifterWildShapeWolfAbillity8.Reference.Get(),
          AbilityRefs.ShifterWildShapeWolverineAbillity8.Reference.Get(),
        })
        .Configure();
      var menagerie15 = FeatureConfigurator.New(MenagerieName + "15", Guids.TrueShapeMenagerie15)
        .SetDisplayName(MenagerieName + "15.Name")
        .SetDescription(MenagerieName + "15.Description")
        .SetIcon(AbilityRefs.ShifterWildShapeDinosaurAbillity.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new()
        {
          AbilityRefs.ShifterWildShapeBearAbillity15.Reference.Get(),
          AbilityRefs.ShifterWildShapeBoarAbillity15.Reference.Get(),
          AbilityRefs.ShifterWildShapeDinosaurAbillity15.Reference.Get(),
          AbilityRefs.ShifterWildShapeElephantAbillity15.Reference.Get(),
          AbilityRefs.ShifterWildShapeFeyAbillity15.Reference.Get(),
          AbilityRefs.ShifterWildShapeGriffonAbillity14.Reference.Get(),
          AbilityRefs.ShifterWildShapeManticoreAbillity15.Reference.Get(),
          AbilityRefs.ShifterWildShapeSpiderAbillity15.Reference.Get(),
          AbilityRefs.ShifterWildShapeTigerAbillity15.Reference.Get(),
          AbilityRefs.ShifterWildShapeWolfAbillity15.Reference.Get(),
          AbilityRefs.ShifterWildShapeWolverineAbillity15.Reference.Get(),
        })
        .Configure();

      // ----- True Shape (permanence + attunement) -----
      var trueShape = FeatureConfigurator.New(FeatureName, Guids.TrueShapeFeature)
        .SetDisplayName(FeatureName + ".Name")
        .SetDescription(FeatureName + ".Description")
        .SetIcon(tigerIcon)
        .SetIsClassFeature()
        .AddFacts(new() { soulToggle })
        .AddComponent(new TrueShapeMaintenance { SoulBuff = soulBuff })
        .AddComponent(new TrueShapeAttunement())
        .Configure();

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.TrueShapeArchetype, CharacterClassRefs.DruidClass)
          .SetLocalizedName("TrueShape.Name")
          .SetLocalizedDescription("TrueShape.Description")
          // Significantly less spellcasting: the book stops at 4th-level
          // spells (and detect magic is gone with the list).
          .SetReplaceSpellbook(book)
          .AddToAddFeatures(LevelPlan.L(1), trueShape, menagerie)
          .AddToAddFeatures(LevelPlan.L(8), menagerie8)
          .AddToAddFeatures(LevelPlan.L(15), menagerie15);

      // The vanilla animal wild shape is replaced by the menagerie
      // (elemental wild shape is a separate family and is kept).
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, druid, "e9d4d569f5354fac88e26b058c6a1de8"); // DruidWildShape

      archetype.Configure();

      MissionFeats.Logger.Info("TrueShape: configured.");
    }

    /// <summary>Clone of a spellbook with a swapped-in spell list (the
    /// Sanguine Font reflection-copy pattern).</summary>
    private static BlueprintSpellbook CloneSpellbook(
      BlueprintSpellbook source, BlueprintSpellList list, string name, string guid)
    {
      var book = BlueprintCore.Blueprints.Configurators.Classes.Spells.SpellbookConfigurator
        .New(name, guid)
        .Configure();
      const BindingFlags flags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
        BindingFlags.DeclaredOnly;
      foreach (var field in typeof(BlueprintSpellbook).GetFields(flags))
      {
        try
        {
          field.SetValue(book, field.GetValue(source));
        }
        catch
        {
          // Init-only or compiler-generated members are skipped.
        }
      }
      foreach (var field in typeof(BlueprintScriptableObject).GetFields(flags))
      {
        if (field.Name == "m_AssetGuid")
        {
          continue;
        }
        try
        {
          field.SetValue(book, field.GetValue(source));
        }
        catch
        {
          // Skipped.
        }
      }
      var listField = typeof(BlueprintSpellbook).GetFields(
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .FirstOrDefault(f => f.FieldType == typeof(BlueprintSpellListReference));
      if (listField != null)
      {
        listField.SetValue(book, list.ToReference<BlueprintSpellListReference>());
      }
      else
      {
        MissionFeats.Logger.Warn("[trueshape] spellbook list field not found - book uses source list!");
      }
      return book;
    }
  }

  /// <summary>
  /// The permanence: while the Beast Soul toggle is on and the owner wears a
  /// form, every polymorph buff whose remaining time runs low is re-applied
  /// with a long duration - remove first, then add, so exactly one copy ever
  /// exists. The refresh stops the moment the form is dropped (or the toggle
  /// turned off), letting it lapse naturally.
  /// </summary>
  [TypeId(Guids.TrueShapeMaintenance)]
  internal class TrueShapeMaintenance : UnitFactComponentDelegate, ITickEachRound
  {
    public BlueprintBuff SoulBuff;

    public void OnNewRound()
    {
      try
      {
        if (SoulBuff != null && !Owner.HasFact(SoulBuff))
        {
          return; // permanence switched off
        }
        if (!Owner.Body.IsPolymorphed)
        {
          return; // no form, nothing to hold
        }
        foreach (var buff in Owner.Buffs.ToArray())
        {
          if (buff?.Blueprint == null || !IsPolymorphBuff(buff))
          {
            continue;
          }
          if (buff.TimeLeft > TimeSpan.FromSeconds(12))
          {
            continue; // plenty of time left this round
          }
          var existing = Owner.Buffs.GetBuff(buff.Blueprint);
          if (existing != null)
          {
            Owner.RemoveFact(existing);
          }
          Owner.Descriptor.AddBuff(buff.Blueprint, Context, TimeSpan.FromMinutes(10));
          MissionFeats.Logger.Info(
            $"[trueshape] form held: {buff.Blueprint.name} on {Owner.CharacterName}.");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[trueshape] maintenance failed.", e);
      }
    }

    private static bool IsPolymorphBuff(Buff buff)
    {
      foreach (var component in buff.Blueprint.ComponentsArray ?? Array.Empty<BlueprintComponent>())
      {
        if (component != null && component.GetType().Name.Contains("Polymorph"))
        {
          return true;
        }
      }
      return false;
    }
  }

  /// <summary>
  /// Wild Attunement: while polymorphed, the worn armor and shield keep
  /// protecting the druid - their full AC, read live from the equipment by
  /// reflection (member "AC" on the item, falling back to the blueprint's
  /// base value; exact members vary across game builds). Recomputed on every
  /// AC calculation, so gear swaps apply immediately. Applies as an
  /// Armor-descriptor modifier, which stacks cleanly because a polymorphed
  /// body has no armor bonus of its own.
  /// </summary>
  [TypeId(Guids.TrueShapeAttunement)]
  internal class TrueShapeAttunement : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateAC>, IRulebookHandler<RuleCalculateAC>,
    ISubscriber, ITargetRulebookSubscriber
  {
    public void OnEventAboutToTrigger(RuleCalculateAC evt)
    {
      try
      {
        if (!Owner.Body.IsPolymorphed)
        {
          return;
        }
        int bonus = EquipmentAc();
        if (bonus > 0)
        {
          evt.AddModifier(bonus, Fact, ModifierDescriptor.Armor);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[trueshape] attunement failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt)
    {
    }

    private int EquipmentAc()
    {
      int total = ReadAc(Owner.Body.Armor?.MaybeArmor, depth: 0);
      var hand = Owner.Body.SecondaryHand;
      if (hand != null && hand.HasShield)
      {
        total += ReadAc(hand.MaybeShield, depth: 0);
      }
      return total;
    }

    /// <summary>
    /// Reflection read of an item's AC: any int member named AC (or ArmorAC)
    /// on the item, then on its blueprint, then one level deep into a wrapped
    /// "Armor" item (shields wrap an armor). Returns 0 when nothing matches.
    /// </summary>
    private static int ReadAc(object item, int depth)
    {
      if (item is null || depth > 2)
      {
        return 0;
      }
      foreach (var name in new[] { "AC", "ArmorAC", "m_AC" })
      {
        var value = ReadInt(item, name);
        if (value > 0)
        {
          return value;
        }
      }
      var blueprint = Read(item, "Blueprint") ?? Read(item, "m_Blueprint");
      foreach (var name in new[] { "AC", "ArmorAC" })
      {
        var value = ReadInt(blueprint, name);
        if (value > 0)
        {
          return value;
        }
      }
      // Shields wrap an inner armor item - follow it one level down.
      var inner = Read(item, "Armor") ?? Read(item, "MaybeArmor");
      if (inner != null && !ReferenceEquals(inner, item))
      {
        return ReadAc(inner, depth + 1);
      }
      return 0;
    }

    private static int ReadInt(object target, string name)
    {
      if (target is null)
      {
        return 0;
      }
      var type = target.GetType();
      var property = type.GetProperty(
        name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      if (property != null && property.PropertyType == typeof(int))
      {
        return (int)property.GetValue(target, null);
      }
      var field = type.GetField(
        name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      if (field != null && field.FieldType == typeof(int))
      {
        return (int)field.GetValue(target);
      }
      return 0;
    }

    private static object Read(object target, string name)
    {
      if (target is null)
      {
        return null;
      }
      var type = target.GetType();
      var property = type.GetProperty(
        name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      if (property != null)
      {
        return property.GetValue(target, null);
      }
      var field = type.GetField(
        name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      return field?.GetValue(target);
    }
  }
}

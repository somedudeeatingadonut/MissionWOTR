using BlueprintCore.Blueprints.Configurators.Classes;
using BlueprintCore.Blueprints.Configurators.Classes.Spells;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Elemental Obsessor (homebrew, user-commissioned: a DPS-focused arcanist to
  /// contrast the class's usual support/debuff builds).
  ///
  /// The fantasy: total, single-minded devotion to one element. The obsessor can
  /// ONLY cast spells of her chosen element - but her fixation reaches across every
  /// non-mythic spellbook in the game, so her element's whole arsenal is hers.
  ///
  /// - Elemental Fixation (1st, free spellbook swap): choose Fire, Cold, Acid,
  ///   Electricity or Sonic. The arcanist spellbook is replaced by a spellbook
  ///   containing every spell of that element's descriptor from every non-mythic
  ///   spellbook in the game (each spell at its lowest level anywhere).
  /// - Obsessive Focus (1st, trades the 1st-level exploit): one hit per cast of a
  ///   damaging spell - the first ray, or the first tick of a persistent effect -
  ///   deals bonus damage equal to half arcanist level (min 1; cantrips at half
  ///   that bonus, min 1).
  /// - Unstoppable Obsession (3rd, trades the 3rd-level exploit): the element
  ///   cannot be denied - when her chosen element's damage is reduced by immunity
  ///   or resistance, the target still takes at least 20% of the raw damage; the
  ///   minimum rises 5% every two levels after 3rd (60% at 19th).
  /// - Cathartic Release (7th, trades the 7th-level exploit): when a SINGLE-TARGET
  ///   spell of hers kills an enemy, the element erupts - other enemies within
  ///   10 feet take 1d4 per two levels beyond 7th (1d4 at 7th, 7d4 at 19th).
  ///
  /// Implementation notes: riders are separate DirectDamage instances triggered
  /// from outgoing spell damage (Elemental Barrage detection pattern); per-cast
  /// dedupe keys on Reason.Ability (one AbilityData per cast, shared by rays and
  /// persistent ticks); the immunity bypass pays the shortfall between the floor
  /// and the post-resistance result as untyped direct damage. Spell lists are
  /// built at configure time from ResourcesLibrary (all non-mythic spellbooks).
  /// </summary>
  internal static class ElementalObsessor
  {
    internal const string ArchetypeName = "ElementalObsessorArchetype";
    internal const string DisplayName = "ElementalObsessor.Name";
    internal const string Description = "ElementalObsessor.Description";

    internal const string FixationSelectionName = "ElementObsessorFixation";
    internal const string FocusName = "ElementObsessorFocus";
    internal const string PermeationName = "ElementObsessorPermeation";
    internal const string CatharticName = "ElementObsessorCathartic";

    // Vanilla: the exploit selection granted at every odd arcanist level.
    private const string ArcanistExploitSelectionGuid = "b8bf3d5023f2d8c428fdf6438cecaea7";

    internal static BlueprintFeature[] ElementFeatures;
    internal static DamageEnergyType[] ElementEnergies;

    public static void Configure()
    {
      var arcanist = CharacterClassRefs.ArcanistClass.Reference.Get();
      var arcanistBook = SpellbookRefs.ArcanistSpellbook.Reference.Get();

      var elements = new (string Key, SpellDescriptor Descriptor, DamageEnergyType Energy)[]
      {
        ("Fire", SpellDescriptor.Fire, DamageEnergyType.Fire),
        ("Cold", SpellDescriptor.Cold, DamageEnergyType.Cold),
        ("Acid", SpellDescriptor.Acid, DamageEnergyType.Acid),
        ("Electricity", SpellDescriptor.Electricity, DamageEnergyType.Electricity),
        ("Sonic", SpellDescriptor.Sonic, DamageEnergyType.Sonic),
      };

      var icon = FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon;

      // ----- Per element: spell list, spellbook, fixation feature -----
      var features = new List<BlueprintFeature>();
      var energies = new List<DamageEnergyType>();
      foreach (var e in elements)
      {
        var list = BuildElementList(
          $"ElementObsessorSpellList{e.Key}", GetListGuid(e.Key), e.Descriptor);
        var book = BuildSpellbook(
          $"ElementObsessorSpellbook{e.Key}", GetBookGuid(e.Key), arcanistBook, list);
        var feature = FeatureReplaceSpellbookConfigurator.New(
            $"ElementObsessorFixation{e.Key}", GetFeatureGuid(e.Key))
          .SetDisplayName($"ElementFixation{e.Key}.Name")
          .SetDescription($"ElementFixation{e.Key}.Description")
          .SetIcon(icon)
          .SetSpellbook(GetBookGuid(e.Key))
          .SetHideNotAvailibleInUI(true)
          .Configure();
        features.Add(feature);
        energies.Add(e.Energy);
      }
      ElementFeatures = features.ToArray();
      ElementEnergies = energies.ToArray();

      // ----- Elemental Fixation: the choice -----
      var selection = FeatureSelectionConfigurator.New(FixationSelectionName, Guids.ElementObsessorFixationSelection)
        .SetDisplayName("ElementFixation.Name")
        .SetDescription("ElementFixation.Description")
        .SetIcon(icon)
        .SetObligatory(true)
        .SetAllFeatures(features.Cast<Blueprint<BlueprintFeatureReference>>().ToArray())
        .Configure();

      // ----- Obsessive Focus (1st): bonus damage, once per cast, halved on cantrips -----
      var focus = FeatureConfigurator.New(FocusName, Guids.ElementObsessorFocusFeature)
        .SetDisplayName("ObsessiveFocus.Name")
        .SetDescription("ObsessiveFocus.Description")
        .SetIcon(AbilityRefs.BombStandart.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new ObsessorFocusRider { CharacterClass = arcanist })
        .Configure();

      // ----- Unstoppable Obsession (3rd): immunity/resistance floor -----
      var permeation = FeatureConfigurator.New(PermeationName, Guids.ElementObsessorPermeationFeature)
        .SetDisplayName("UnstoppableObsession.Name")
        .SetDescription("UnstoppableObsession.Description")
        .SetIcon(FeatureRefs.RagingBrutality.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new ObsessorPermeation
        {
          CharacterClass = arcanist,
          Elements = ElementFeatures,
          Energies = ElementEnergies,
        })
        .Configure();

      // ----- Cathartic Release (7th): single-target kills erupt -----
      var cathartic = FeatureConfigurator.New(CatharticName, Guids.ElementObsessorCatharticFeature)
        .SetDisplayName("CatharticRelease.Name")
        .SetDescription("CatharticRelease.Description")
        .SetIcon(AbilityRefs.BloodragerInfernalHellfireStrikeAbility.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new ObsessorCathartic { CharacterClass = arcanist })
        .Configure();

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.ElementObsessorArchetype, CharacterClassRefs.ArcanistClass)
          .SetLocalizedName(DisplayName)
          .SetLocalizedDescription(Description);

      // Trades the exploits gained at 1st, 3rd and 7th level (level-specific: the
      // selection appears at every odd level).
      archetype = archetype
        .AddToRemoveFeatures(1, ArcanistExploitSelectionGuid)
        .AddToRemoveFeatures(3, ArcanistExploitSelectionGuid)
        .AddToRemoveFeatures(7, ArcanistExploitSelectionGuid);

      archetype = archetype
        .AddToAddFeatures(LevelPlan.L(1), FixationSelectionName, FocusName)
        .AddToAddFeatures(LevelPlan.L(3), PermeationName)
        .AddToAddFeatures(LevelPlan.L(7), CatharticName);

      archetype.Configure();

      MissionFeats.Logger.Info("ElementalObsessor: configured.");
    }

    private static string GetFeatureGuid(string key)
    {
      return key switch
      {
        "Fire" => Guids.ElementObsessorFixationFire,
        "Cold" => Guids.ElementObsessorFixationCold,
        "Acid" => Guids.ElementObsessorFixationAcid,
        "Electricity" => Guids.ElementObsessorFixationElectricity,
        "Sonic" => Guids.ElementObsessorFixationSonic,
        _ => throw new ArgumentOutOfRangeException(nameof(key)),
      };
    }

    private static string GetBookGuid(string key)
    {
      return key switch
      {
        "Fire" => Guids.ElementObsessorSpellbookFire,
        "Cold" => Guids.ElementObsessorSpellbookCold,
        "Acid" => Guids.ElementObsessorSpellbookAcid,
        "Electricity" => Guids.ElementObsessorSpellbookElectricity,
        "Sonic" => Guids.ElementObsessorSpellbookSonic,
        _ => throw new ArgumentOutOfRangeException(nameof(key)),
      };
    }

    private static string GetListGuid(string key)
    {
      return key switch
      {
        "Fire" => Guids.ElementObsessorSpellListFire,
        "Cold" => Guids.ElementObsessorSpellListCold,
        "Acid" => Guids.ElementObsessorSpellListAcid,
        "Electricity" => Guids.ElementObsessorSpellListElectricity,
        "Sonic" => Guids.ElementObsessorSpellListSonic,
        _ => throw new ArgumentOutOfRangeException(nameof(key)),
      };
    }

    /// <summary>
    /// Enumerates every loaded blueprint of a type, across cache field naming
    /// differences between game builds (the MakeDragonGreatAgain probe pattern:
    /// static fields on BlueprintsCache plus instance fields of
    /// ResourcesLibrary.BlueprintsCache, dictionary or enumerable shapes).
    /// </summary>
    private static IEnumerable<T> AllBlueprints<T>() where T : BlueprintScriptableObject
    {
      var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
        | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
      var seen = new HashSet<BlueprintScriptableObject>();
      IEnumerable<T> Probe(object container)
      {
        if (container is System.Collections.IDictionary dict)
        {
          foreach (System.Collections.DictionaryEntry kv in dict)
          {
            if (kv.Value is T t && seen.Add(t))
            {
              yield return t;
            }
          }
        }
        else if (container is System.Collections.IEnumerable en)
        {
          foreach (var v in en)
          {
            if (v is T t && seen.Add(t))
            {
              yield return t;
            }
            else if (v is KeyValuePair<BlueprintGuid, SimpleBlueprint> kvp
              && kvp.Value is T t2 && seen.Add(t2))
            {
              yield return t2;
            }
          }
        }
      }
      foreach (var name in new[]
      {
        "m_LoadedBlueprints", "s_LoadedBlueprints", "m_Blueprints",
        "m_Cache", "m_LoadedBlueprintsByAssetId",
      })
      {
        var field = typeof(BlueprintsCache).GetField(name, flags);
        if (field is null || !field.IsStatic)
        {
          continue;
        }
        foreach (var bp in Probe(field.GetValue(null)))
        {
          yield return bp;
        }
      }
      var instanceResults = new List<T>();
      try
      {
        var cache = ResourcesLibrary.BlueprintsCache;
        if (cache != null)
        {
          foreach (var field in typeof(BlueprintsCache).GetFields(flags))
          {
            if (field.IsStatic)
            {
              continue;
            }
            instanceResults.AddRange(Probe(field.GetValue(cache)));
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ElementalObsessor: cache enumeration failed.", e);
      }
      foreach (var bp in instanceResults)
      {
        yield return bp;
      }
    }

    /// <summary>
    /// Every spell of the element's descriptor from every non-mythic spellbook in
    /// the game, each at its lowest level anywhere (cantrips stay cantrips).
    /// </summary>
    private static BlueprintSpellList BuildElementList(
      string name, string guid, SpellDescriptor element)
    {
      var minLevel = new Dictionary<BlueprintAbility, int>();
      foreach (var book in AllBlueprints<BlueprintSpellbook>())
      {
        if (book is null || book.IsMythic)
        {
          continue;
        }
        var list = book.SpellList;
        if (list?.SpellsByLevel is null)
        {
          continue;
        }
        foreach (var levelEntry in list.SpellsByLevel)
        {
          if (levelEntry is null)
          {
            continue;
          }
          int level = Math.Max(0, Math.Min(9, levelEntry.SpellLevel));
          // The Spells member converts implicitly to BlueprintAbility (TTT idiom).
          foreach (BlueprintAbility spell in levelEntry.Spells)
          {
            if (spell != null && spell.SpellDescriptor.HasFlag(element)
              && (!minLevel.TryGetValue(spell, out var current) || level < current))
            {
              minLevel[spell] = level;
            }
          }
        }
      }
      var byLevel = new SpellLevelList[10];
      for (int i = 0; i < 10; i++)
      {
        byLevel[i] = new SpellLevelList(i) { SpellLevel = i };
      }
      foreach (var pair in minLevel)
      {
        AddSpellToEntry(byLevel[Math.Max(0, Math.Min(9, pair.Value))], pair.Key);
      }
      BlueprintTool.Create<BlueprintSpellList>(name, guid);
      var result = BlueprintTool.Get<BlueprintSpellList>(guid);
      result.SpellsByLevel = byLevel;
      MissionFeats.Logger.Info(
        $"[obsessor] {element} list built: {minLevel.Count} spells.");
      return result;
    }

    /// <summary>
    /// Adds a spell to a SpellLevelList without binding to this build's member
    /// naming (Spells may be List&lt;BlueprintAbilityReference&gt; or
    /// List&lt;BlueprintAbility&gt;, field or property, possibly null-initialized).
    /// </summary>
    private static void AddSpellToEntry(SpellLevelList entry, BlueprintAbility spell)
    {
      const System.Reflection.BindingFlags flags =
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.NonPublic;
      try
      {
        var field = typeof(SpellLevelList).GetField("Spells", flags);
        object listObj = null;
        if (field != null)
        {
          listObj = field.GetValue(entry);
          if (listObj is null)
          {
            listObj = Activator.CreateInstance(field.FieldType);
            field.SetValue(entry, listObj);
          }
        }
        else
        {
          var prop = typeof(SpellLevelList).GetProperty("Spells", flags);
          if (prop is null)
          {
            MissionFeats.Logger.Warn("[obsessor] SpellLevelList.Spells not found - list incomplete.");
            return;
          }
          listObj = prop.GetValue(entry, null);
          if (listObj is null)
          {
            listObj = Activator.CreateInstance(prop.PropertyType);
            prop.SetValue(entry, listObj, null);
          }
        }
        if (listObj is List<BlueprintAbilityReference> refs)
        {
          refs.Add(spell.ToReference<BlueprintAbilityReference>());
        }
        else if (listObj is List<BlueprintAbility> blueprints)
        {
          blueprints.Add(spell);
        }
        else
        {
          MissionFeats.Logger.Warn(
            $"[obsessor] unexpected SpellLevelList.Spells type: {listObj.GetType()}");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ElementalObsessor: spell list entry failed.", e);
      }
    }

    /// <summary>
    /// Clones the arcanist spellbook (components, tables, casting stat, class
    /// binding) and swaps in the element's spell list - the CloneUnit pattern. The
    /// list field is located by type, not name (naming varies between builds).
    /// </summary>
    private static BlueprintSpellbook BuildSpellbook(
      string name, string guid, BlueprintSpellbook source, BlueprintSpellList list)
    {
      var book = SpellbookConfigurator.New(name, guid).Configure();

      const System.Reflection.BindingFlags flags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly;
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
          continue; // blueprint identity must stay its own
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
          System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
          System.Reflection.BindingFlags.NonPublic)
        .FirstOrDefault(f => f.FieldType == typeof(BlueprintSpellListReference));
      if (listField != null)
      {
        listField.SetValue(book, list.ToReference<BlueprintSpellListReference>());
      }
      else
      {
        MissionFeats.Logger.Warn("[obsessor] spellbook list field not found - book uses source list!");
      }
      return book;
    }

    /// <summary>
    /// True when the damage event is spell damage dealt by this unit - not this
    /// fact's own rider (Elemental Barrage pattern). Persistent-area ticks are
    /// allowed through: they are paid once per cast by the riders' dedupe.
    /// </summary>
    internal static bool IsOwnSpellDamage(RuleDealDamage evt, UnitEntityData owner, EntityFact selfFact)
    {
      if (evt.Initiator != owner || evt.Target is null || !evt.Target.IsEnemy(owner))
      {
        return false;
      }
      if (evt.Reason.Fact == selfFact)
      {
        return false;
      }
      var ability = evt.Reason.Ability?.Blueprint ?? evt.Reason.Context?.SourceAbility;
      return ability?.Type == AbilityType.Spell;
    }

    /// <summary>Deals direct bonus damage to the target as a separate instance.</summary>
    internal static void DealRider(UnitEntityData caster, UnitEntityData target, EntityFact reason,
      DiceFormula dice, int flat)
    {
      var bundle = new DamageBundle();
      bundle.Add(new DirectDamage(dice, flat));
      Rulebook.Trigger(new RuleDealDamage(caster, target, bundle) { Reason = reason });
    }

    /// <summary>The unit's chosen element, or a negative index if none.</summary>
    internal static int ChosenElementIndex(UnitEntityData unit)
    {
      if (ElementFeatures is null || unit is null)
      {
        return -1;
      }
      for (int i = 0; i < ElementFeatures.Length; i++)
      {
        if (ElementFeatures[i] != null && unit.HasFact(ElementFeatures[i]))
        {
          return i;
        }
      }
      return -1;
    }
  }

  /// <summary>
  /// Obsessive Focus: one hit per cast of a damaging spell (the first ray or the
  /// first tick of a persistent effect) deals bonus damage equal to half arcanist
  /// level; cantrips receive half that bonus. Both minimum 1.
  /// </summary>
  [TypeId(Guids.ElementObsessorFocusRider)]
  internal class ObsessorFocusRider : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass CharacterClass;

    /// <summary>Casts already paid, keyed by the cast's AbilityData (shared by rays
    /// and persistent ticks of the same cast).</summary>
    private static readonly HashSet<object> Paid = new();

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        if (!ElementalObsessor.IsOwnSpellDamage(evt, Owner, Fact))
        {
          return;
        }
        var key = (object)evt.Reason.Ability ?? evt.Reason.Context;
        if (key is null || !Paid.Add(key))
        {
          return; // this cast already got its focus
        }
        if (Paid.Count > 32)
        {
          Paid.Clear();
          Paid.Add(key);
        }
        int level = Owner.Descriptor.Progression.GetClassLevel(CharacterClass);
        var ability = evt.Reason.Ability;
        bool cantrip = ability != null && ability.Spellbook != null
          && ability.Spellbook.GetSpellLevel(ability) == 0;
        int bonus = Math.Max(1, level / 2);
        if (cantrip)
        {
          bonus = Math.Max(1, bonus / 2);
        }
        ElementalObsessor.DealRider(Owner, evt.Target, Fact, DiceFormula.Zero, bonus);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ElementalObsessor: Obsessive Focus failed.", e);
      }
    }
  }

  /// <summary>
  /// Unstoppable Obsession: the chosen element cannot be denied. When its damage
  /// is reduced by immunity or resistance, the target still takes at least 20% of
  /// the raw damage (rising 5% every two arcanist levels after 3rd - 60% at 19th).
  /// The shortfall is paid as untyped direct damage.
  /// </summary>
  [TypeId(Guids.ElementObsessorPermeation)]
  internal class ObsessorPermeation : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass CharacterClass;
    public BlueprintFeature[] Elements;
    public DamageEnergyType[] Energies;

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        if (!ElementalObsessor.IsOwnSpellDamage(evt, Owner, Fact))
        {
          return;
        }
        int chosen = -1;
        for (int i = 0; i < Elements.Length; i++)
        {
          if (Elements[i] != null && Owner.HasFact(Elements[i]))
          {
            chosen = i;
            break;
          }
        }
        if (chosen < 0 || chosen >= Energies.Length)
        {
          return;
        }
        var energy = Energies[chosen];
        int raw = 0;
        int applied = 0;
        foreach (var value in evt.ResultList)
        {
          if ((value.Source as EnergyDamage)?.EnergyType == energy)
          {
            raw += value.ValueWithoutReduction;
            applied += value.FinalValue;
          }
        }
        if (raw <= 0)
        {
          return;
        }
        int level = Owner.Descriptor.Progression.GetClassLevel(CharacterClass);
        int floorPct = 20 + 5 * Math.Max(0, (level - 3) / 2);
        int minimum = raw * floorPct / 100;
        if (applied >= minimum)
        {
          return;
        }
        int shortfall = minimum - applied;
        ElementalObsessor.DealRider(Owner, evt.Target, Fact, DiceFormula.Zero, shortfall);
        MissionFeats.Logger.Info(
          $"[obsessor] Unstoppable Obsession: {energy} damage floored " +
          $"{applied}->{minimum} ({floorPct}%) on {evt.Target.CharacterName}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ElementalObsessor: Unstoppable Obsession failed.", e);
      }
    }
  }

  /// <summary>
  /// Cathartic Release: when a SINGLE-TARGET spell kills an enemy, the pent-up
  /// element erupts - other enemies within 10 feet take 1d4 damage per two levels
  /// beyond 7th (1d4 at 7th, up to 7d4 at 19th). Each dying enemy erupts once.
  /// </summary>
  [TypeId(Guids.ElementObsessorCathartic)]
  internal class ObsessorCathartic : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass CharacterClass;

    private static readonly HashSet<string> Erupted = new();

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        if (!ElementalObsessor.IsOwnSpellDamage(evt, Owner, Fact))
        {
          return;
        }
        // Single-target spells only - AoE spells and persistent areas are skipped.
        if (evt.SourceArea)
        {
          return;
        }
        var ability = evt.Reason.Ability;
        if (ability != null && ability.IsAOE)
        {
          return;
        }
        var victim = evt.Target;
        if (victim.HPLeft > 0 || Erupted.Contains(victim.UniqueId))
        {
          return;
        }
        Erupted.Add(victim.UniqueId);
        if (Erupted.Count > 64)
        {
          Erupted.Clear();
        }
        int level = Owner.Descriptor.Progression.GetClassLevel(CharacterClass);
        var dice = new DiceFormula(1 + Math.Max(0, (level - 7) / 2), DiceType.D4);
        int hits = 0;
        foreach (var u in Game.Instance.State.LoadedAreaState.MainState.AllEntityData
          .OfType<UnitEntityData>())
        {
          if (u != victim && u.HPLeft > 0 && u.IsEnemy(Owner)
            && Vector3.Distance(u.Position, victim.Position) <= 3.5f)
          {
            ElementalObsessor.DealRider(Owner, u, Fact, dice, 0);
            hits++;
          }
        }
        if (hits > 0)
        {
          MissionFeats.Logger.Info(
            $"[obsessor] Cathartic Release: {victim.CharacterName}'s death hit {hits} enemies.");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ElementalObsessor: Cathartic Release failed.", e);
      }
    }
  }
}

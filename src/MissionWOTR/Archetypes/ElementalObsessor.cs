using BlueprintCore.Blueprints.Configurators.Classes;
using BlueprintCore.Blueprints.Configurators.Classes.Spells;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
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
  /// - Obsessive Wellspring (5th, additive - the arcanist identity hook): a kill
  ///   with her element's magic restores 1 arcane reservoir point, feeding the
  ///   exploits she still gains from 9th level on (and Consume Spells).
  /// - Cathartic Release (7th, trades the 7th-level exploit): when a SINGLE-TARGET
  ///   spell of hers kills an enemy, the element erupts - other enemies within
  ///   10 feet take 1d4 per two levels beyond 7th (1d4 at 7th, 7d4 at 19th; fire
  ///   and cold erupt one extra die - their element perk).
  /// - Shattering Pitch (7th, additive - sonic perk): her single-target sonic
  ///   spells splash 25% of the damage dealt to enemies within 10 ft of the target.
  /// - Corrosive Adaptation (acid perk, picks at 12/16/20): adopt any spell from
  ///   another element's list - it joins the acid list and its damage becomes acid
  ///   (ReplaceEnergy on RulePrepareDamage, the TTT Elemental Spell mechanism).
  ///
  /// Element perks (deliberate equalizers): fire/cold +1d4 on Cathartic Release;
  /// acid Corrosive Adaptation (late-game spell access); electricity's Unstoppable
  /// Obsession floor begins at 40% instead of 20%; sonic Shattering Pitch splash.
  /// Unstoppable Obsession: floor 20% rising to 105% at 20th (40% base for
  /// electricity) - at the cap, immunity deals back extra damage, like weakness.
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
    internal const string WellspringName = "ElementObsessorWellspring";
    internal const string ShatteringName = "ElementObsessorShatteringPitch";
    internal const string AdaptationName = "ElementObsessorCorrosiveAdaptation";

    // Vanilla: the exploit selection granted at every odd arcanist level.
    private const string ArcanistExploitSelectionGuid = "b8bf3d5023f2d8c428fdf6438cecaea7";
    // Vanilla: the arcane reservoir.
    private const string ArcaneReservoirResourceGuid = "cac948cbbe79b55459459dd6a8fe44ce";

    internal static BlueprintFeature[] ElementFeatures;
    internal static DamageEnergyType[] ElementEnergies;
    internal static SpellDescriptor[] ElementDescriptors;

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
      var descriptors = new List<SpellDescriptor>();
      var levelsByElement = new Dictionary<SpellDescriptor, Dictionary<BlueprintAbility, int>>();
      foreach (var e in elements)
      {
        var (list, spellLevels) = BuildElementList(
          $"ElementObsessorSpellList{e.Key}", GetListGuid(e.Key), e.Descriptor);
        levelsByElement[e.Descriptor] = spellLevels;
        var book = BuildSpellbook(
          $"ElementObsessorSpellbook{e.Key}", GetBookGuid(e.Key), arcanistBook, list);
        var feature = FeatureReplaceSpellbookConfigurator.New(
            $"ElementObsessorFixation{e.Key}", GetFeatureGuid(e.Key))
          .SetDisplayName($"ElementFixation{e.Key}.Name")
          .SetDescription($"ElementFixation{e.Key}.Description")
          .SetIcon(icon)
          .SetSpellbook(GetBookGuid(e.Key))
          .SetHideNotAvailibleInUI(true)
          // 0.60.0: the engine only honours BlueprintFeatureReplaceSpellbook inside the level-up
          // SelectFeature action (Kingmaker.UnitLogic.Class.LevelUp.Actions.SelectFeature), and
          // even then it merely repoints ClassData.Spellbook — an Arcanist spellbook already
          // created at character creation is left in place. Apply the swap ourselves so the
          // fixation works however the feature arrives.
          .AddComponent(new ObsessorFixationApplier
          {
            CharacterClass = arcanist,
            NewSpellbook = book,
          })
          .Configure();
        features.Add(feature);
        energies.Add(e.Energy);
        descriptors.Add(e.Descriptor);
      }
      ElementFeatures = features.ToArray();
      ElementEnergies = energies.ToArray();
      ElementDescriptors = descriptors.ToArray();

      // ----- Elemental Fixation: the choice -----
      var selection = FeatureSelectionConfigurator.New(FixationSelectionName, Guids.ElementObsessorFixationSelection)
        .SetDisplayName("ElementFixation.Name")
        .SetDescription("ElementFixation.Description")
        .SetIcon(icon)
        .SetObligatory(true)
        .SetAllFeatures(ToFeatureRefs(features))
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

      // ----- Obsessive Wellspring (5th): arcanist identity - the DPS loop feeds
      // the arcane reservoir that powers the class's exploits and Consume Spells. -----
      var reservoir = BlueprintTool.Get<BlueprintAbilityResource>(ArcaneReservoirResourceGuid);
      var wellspring = FeatureConfigurator.New(WellspringName, Guids.ElementObsessorWellspringFeature)
        .SetDisplayName("ObsessiveWellspring.Name")
        .SetDescription("ObsessiveWellspring.Description")
        .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new ObsessorWellspring
        {
          CharacterClass = arcanist,
          Reservoir = reservoir,
        })
        .Configure();

      // ----- Shattering Pitch (7th, sonic perk): single-target sonic spells splash -----
      var shattering = FeatureConfigurator.New(ShatteringName, Guids.ElementObsessorShatteringFeature)
        .SetDisplayName("ShatteringPitch.Name")
        .SetDescription("ShatteringPitch.Description")
        .SetIcon(AbilityRefs.BloodragerInfernalHellfireStrikeAbility.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new ObsessorSonicSplash())
        .Configure();

      // ----- Corrosive Adaptation (acid perk, picks at 12/16/20): adopt any spell
      // from another element's list - it joins the acid list and its damage
      // becomes acid (ReplaceEnergy on RulePrepareDamage, the Elemental Spell
      // metamagic mechanism). -----
      var acidGuid = GetListGuid("Acid");
      var acidSpells = new HashSet<BlueprintAbility>(levelsByElement[SpellDescriptor.Acid].Keys);
      var candidates = new List<BlueprintFeature>();
      var seenSpells = new HashSet<BlueprintAbility>();
      foreach (var e in elements)
      {
        if (e.Descriptor == SpellDescriptor.Acid)
        {
          continue;
        }
        foreach (var pair in levelsByElement[e.Descriptor])
        {
          if (pair.Value < 1 || !seenSpells.Add(pair.Key) || acidSpells.Contains(pair.Key))
          {
            continue;
          }
          candidates.Add(BuildAdaptationCandidate(pair.Key, pair.Value, acidGuid));
        }
      }
      var acidFixation = features[2]; // elements array order: Fire, Cold, Acid, ...
      var adaptation = FeatureSelectionConfigurator.New(AdaptationName, Guids.ElementObsessorAdaptationSelection)
        .SetDisplayName("CorrosiveAdaptation.Name")
        .SetDescription("CorrosiveAdaptation.Description")
        .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
        .SetHideNotAvailibleInUI(true)
        .AddPrerequisiteFeature(acidFixation)
        .SetAllFeatures(ToFeatureRefs(candidates))
        .Configure();
      MissionFeats.Logger.Info(
        $"[obsessor] Corrosive Adaptation: {candidates.Count} candidate spells.");

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
        .AddToAddFeatures(LevelPlan.L(5), WellspringName)
        .AddToAddFeatures(LevelPlan.L(7), CatharticName, ShatteringName)
        .AddToAddFeatures(LevelPlan.L(12), AdaptationName)
        .AddToAddFeatures(LevelPlan.L(16), AdaptationName)
        .AddToAddFeatures(LevelPlan.L(20), AdaptationName);

      archetype.Configure();

      MissionFeats.Logger.Info("ElementalObsessor: configured.");
    }

    /// <summary>
    /// Builds a BPCore feature-reference list from feature blueprints. A plain
    /// .Cast&lt;Blueprint&lt;TRef&gt;&gt;() throws at runtime: implicit
    /// conversions are not casts, they must be applied per element.
    /// </summary>
    private static Blueprint<BlueprintFeatureReference>[] ToFeatureRefs(
      IEnumerable<BlueprintFeature> features)
    {
      var refs = new List<Blueprint<BlueprintFeatureReference>>();
      foreach (var feature in features)
      {
        refs.Add(feature);
      }
      return refs.ToArray();
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
    /// Enumerates every loaded blueprint of a type. The cache dictionary
    /// (BlueprintsCache.m_LoadedBlueprints) maps BlueprintGuid to cache-entry
    /// wrapper objects, each exposing ".Blueprint" (null until materialized)
    /// and ".Offset"; the DarkCodex blueprint-loader pattern, via reflection
    /// because this build compiles against non-publicized game DLLs. Shared
    /// with the other spell-list-building archetypes (Spellfist).
    /// </summary>
    internal static List<T> AllBlueprints<T>() where T : BlueprintScriptableObject
    {
      var results = new List<T>();
      var seen = new HashSet<BlueprintScriptableObject>();
      try
      {
        var cache = ResourcesLibrary.BlueprintsCache;
        if (cache is null)
        {
          MissionFeats.Logger.Warn("[obsessor] ResourcesLibrary.BlueprintsCache is null.");
          return results;
        }
        const System.Reflection.BindingFlags flags =
          System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
          System.Reflection.BindingFlags.Instance;
        var dict = typeof(BlueprintsCache).GetField("m_LoadedBlueprints", flags)?.GetValue(cache)
          as System.Collections.IDictionary;
        if (dict is null)
        {
          MissionFeats.Logger.Warn("[obsessor] m_LoadedBlueprints not found on the cache.");
          return results;
        }
        // BlueprintCacheEntry: ".Blueprint" (property/field, may be null until
        // loaded) and ".Offset"; BlueprintsCache.Load(guid) materializes on demand.
        System.Reflection.MethodInfo loadMethod = null;
        foreach (var m in typeof(BlueprintsCache).GetMethods(flags))
        {
          var pars = m.GetParameters();
          if (m.Name == "Load" && pars.Length == 1 &&
            pars[0].ParameterType.Name == "BlueprintGuid")
          {
            loadMethod = m;
            break;
          }
        }
        foreach (System.Collections.DictionaryEntry kv in dict)
        {
          object blueprint = null;
          try
          {
            if (kv.Value is not null)
            {
              var entryType = kv.Value.GetType();
              blueprint =
                entryType.GetProperty("Blueprint", flags)?.GetValue(kv.Value) ??
                entryType.GetField("Blueprint", flags)?.GetValue(kv.Value);
              if (blueprint is null && loadMethod is not null)
              {
                try
                {
                  blueprint = loadMethod.Invoke(cache, new[] { kv.Key });
                }
                catch
                {
                  // Not loadable (yet) - skip this entry.
                }
              }
            }
          }
          catch
          {
            // Unreadable entry - skip.
          }
          if (blueprint is T typed && seen.Add(typed))
          {
            results.Add(typed);
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ElementalObsessor: cache enumeration failed.", e);
      }
      MissionFeats.Logger.Info(
        $"[obsessor] enumerated {results.Count} {typeof(T).Name} blueprints.");
      return results;
    }

    /// <summary>
    /// Every spell of the element's descriptor from every non-mythic spellbook in
    /// the game, each at its lowest level anywhere (cantrips stay cantrips).
    /// </summary>
    private static (BlueprintSpellList list, Dictionary<BlueprintAbility, int> levels)
      BuildElementList(string name, string guid, SpellDescriptor element)
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
      return (result, minLevel);
    }

    /// <summary>Stable GUID for an adaptation candidate (never persisted as a const:
    /// derived from the source spell's asset id at configure time).</summary>
    private static string DeterministicGuid(string seed)
    {
      using (var md5 = System.Security.Cryptography.MD5.Create())
      {
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(seed));
        return new Guid(hash).ToString("D").ToUpperInvariant();
      }
    }

    /// <summary>
    /// One Corrosive Adaptation option: the adopted spell, shown under its own
    /// name, icon and description. On attach it joins the acid spell list; while
    /// owned its damage is converted to acid at the RulePrepareDamage stage.
    /// </summary>
    private static BlueprintFeature BuildAdaptationCandidate(
      BlueprintAbility spell, int level, string acidListGuid)
    {
      var name = "ElementObsessorAdaptation" + spell.name;
      var guid = DeterministicGuid("MissionWOTR.ObsessorAdaptation." + spell.AssetGuid);
      var feature = FeatureConfigurator.New(name, guid)
        .SetIsClassFeature()
        .SetIcon(spell.Icon)
        .AddComponent(new ObsessorAcidAdaptation
        {
          Spell = spell,
          Level = level,
          AcidListGuid = acidListGuid,
        })
        .Configure();
      CopyStringFields(feature, spell);
      return feature;
    }

    /// <summary>
    /// Copies the spell's localized display name and description onto the
    /// adaptation feature. The string fields are not public in this build, so
    /// they are located by type on the shared base chain and set by reflection.
    /// </summary>
    private static void CopyStringFields(BlueprintFeature feature, BlueprintAbility spell)
    {
      const System.Reflection.BindingFlags flags =
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly;
      var targetFields = new Dictionary<string, System.Reflection.FieldInfo>();
      for (var t = (System.Type)typeof(BlueprintFeature); t != null && t != typeof(object); t = t.BaseType)
      {
        foreach (var f in t.GetFields(flags))
        {
          if (!targetFields.ContainsKey(f.Name))
          {
            targetFields[f.Name] = f;
          }
        }
      }
      for (var t = (System.Type)spell.GetType(); t != null && t != typeof(object); t = t.BaseType)
      {
        foreach (var f in t.GetFields(flags))
        {
          if (targetFields.TryGetValue(f.Name, out var target) && target.FieldType == f.FieldType
            && f.FieldType.Name.Contains("String"))
          {
            try
            {
              target.SetValue(feature, f.GetValue(spell));
            }
            catch
            {
              // Read-only or init-only members are skipped.
            }
          }
        }
      }
    }

    /// <summary>
    /// Adds a spell to a SpellLevelList without binding to this build's member
    /// naming (Spells may be List&lt;BlueprintAbilityReference&gt; or
    /// List&lt;BlueprintAbility&gt;, field or property, possibly null-initialized).
    /// </summary>
    internal static void AddSpellToEntry(SpellLevelList entry, BlueprintAbility spell)
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
      // All spells in this book - whatever their origin spellbook - cast off the
      // arcanist's Intelligence: the spellbook, not the spell, owns the casting
      // attribute (same behavior as the game's own merged spellbooks).
      try
      {
        book.CastingAttribute = source.CastingAttribute;
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ElementalObsessor: casting attribute pin failed.", e);
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
  /// Elemental Fixation: puts the chosen element's spellbook in place of the arcanist's.
  /// <para>
  /// The engine has two spellbook hooks and neither is enough on its own.
  /// <c>BlueprintArchetype.ReplaceSpellbook</c> is applied by <c>ClassData.AddArchetype</c>, but it
  /// is one fixed book per archetype and cannot express a choice made at runtime.
  /// <c>BlueprintFeatureReplaceSpellbook</c> is read only by the level-up
  /// <c>SelectFeature</c> action, and even there it merely repoints <c>ClassData.Spellbook</c> — a
  /// spellbook that already exists is left behind, which is why the obsessor came out with the
  /// ordinary arcanist book. This component performs the swap itself, so it works whichever path
  /// delivered the fixation.
  /// </para>
  /// </summary>
  [TypeId(Guids.ObsessorFixationApplier)]
  internal class ObsessorFixationApplier : UnitFactComponentDelegate
  {
    public BlueprintCharacterClass CharacterClass;
    public BlueprintSpellbook NewSpellbook;

    protected override void OnTurnOn()
    {
      try
      {
        if (Owner?.Progression is null || CharacterClass is null || NewSpellbook is null)
        {
          return;
        }

        var classData = Owner.Progression.GetClassData(CharacterClass);
        if (classData is null)
        {
          return;
        }

        var oldSpellbook = classData.Spellbook;
        if (oldSpellbook == NewSpellbook)
        {
          return;
        }

        classData.Spellbook = NewSpellbook;

        // Bring the new book up to the class's caster level, the same way the engine's own
        // spellbook catch-up does.
        var book = Owner.DemandSpellbook(NewSpellbook);
        var classLevel = Owner.Progression.GetClassLevel(CharacterClass);
        while (book.RawBaseLevel < classLevel)
        {
          var before = book.RawBaseLevel;
          book.AddBaseLevel();
          if (book.RawBaseLevel == before)
          {
            break;
          }
        }

        // Drop the superseded book only if no other class on this character still uses it.
        if (oldSpellbook is not null &&
            Owner.Progression.Classes.All(c => c.Spellbook != oldSpellbook))
        {
          Owner.DeleteSpellbook(oldSpellbook);
        }

        MissionFeats.Logger.Info(
          $"ElementalObsessor: fixation applied for {Owner.CharacterName} — " +
          $"{oldSpellbook?.Name ?? "none"} -> {NewSpellbook.Name}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ElementalObsessor: failed to apply elemental fixation.", e);
      }
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
        // The floor climbs +5% per level after 3rd and ends at 105% at 20th -
        // full damage through immunity plus 5%, as if the enemy were WEAK to the
        // element. Electricity starts at 40% (the same milestones arrive early);
        // every element shares the 105% cap.
        int baseFloor = Energies[chosen] == DamageEnergyType.Electricity ? 40 : 20;
        int grown = 20 + 5 * Math.Max(0, level - 3);
        int floorPct = Math.Min(105, Math.Max(baseFloor, grown));
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
        // Fire and cold erupt harder (+1 die - their perk).
        int chosen = ElementalObsessor.ChosenElementIndex(Owner);
        int extraDice = chosen >= 0
          && (ElementalObsessor.ElementEnergies[chosen] == DamageEnergyType.Fire
            || ElementalObsessor.ElementEnergies[chosen] == DamageEnergyType.Cold)
          ? 1 : 0;
        var dice = new DiceFormula(1 + Math.Max(0, (level - 7) / 2) + extraDice, DiceType.D4);
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

  /// <summary>
  /// Obsessive Wellspring: when a spell of her chosen element kills an enemy, the
  /// arcanist regains 1 arcane reservoir point (never exceeding her maximum) - the
  /// DPS loop feeds the same reservoir that powers her exploits and Consume Spells.
  /// </summary>
  [TypeId(Guids.ElementObsessorWellspring)]
  internal class ObsessorWellspring : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass CharacterClass;
    public BlueprintAbilityResource Reservoir;

    private static readonly HashSet<string> Rewarded = new();

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        if (!ElementalObsessor.IsOwnSpellDamage(evt, Owner, Fact))
        {
          return;
        }
        var victim = evt.Target;
        if (victim.HPLeft > 0 || Rewarded.Contains(victim.UniqueId))
        {
          return;
        }
        int chosen = ElementalObsessor.ChosenElementIndex(Owner);
        if (chosen < 0 || Reservoir is null)
        {
          return;
        }
        var energy = ElementalObsessor.ElementEnergies[chosen];
        bool has = false;
        foreach (var value in evt.ResultList)
        {
          if ((value.Source as EnergyDamage)?.EnergyType == energy)
          {
            has = true;
            break;
          }
        }
        if (!has)
        {
          return;
        }
        Rewarded.Add(victim.UniqueId);
        if (Rewarded.Count > 64)
        {
          Rewarded.Clear();
        }
        Owner.Descriptor.Resources.Restore(Reservoir, 1);
        MissionFeats.Logger.Info(
          $"[obsessor] Obsessive Wellspring: {victim.CharacterName}'s death restores 1 arcane reservoir point.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ElementalObsessor: Obsessive Wellspring failed.", e);
      }
    }
  }

  /// <summary>
  /// Shattering Pitch (sonic perk): single-target sonic spells splash - every
  /// other enemy within 10 feet of the target takes 25% of the damage dealt.
  /// </summary>
  [TypeId(Guids.ElementObsessorShattering)]
  internal class ObsessorSonicSplash : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        if (!ElementalObsessor.IsOwnSpellDamage(evt, Owner, Fact) || evt.SourceArea)
        {
          return;
        }
        var ability = evt.Reason.Ability;
        if (ability is null || ability.IsAOE)
        {
          return;
        }
        int chosen = ElementalObsessor.ChosenElementIndex(Owner);
        if (chosen < 0
          || ElementalObsessor.ElementEnergies[chosen] != DamageEnergyType.Sonic
          || !(ability.Blueprint?.SpellDescriptor.HasFlag(
                 ElementalObsessor.ElementDescriptors[chosen]) == true))
        {
          return;
        }
        int dealt = 0;
        foreach (var value in evt.ResultList)
        {
          dealt += value.FinalValue;
        }
        if (dealt <= 0)
        {
          return;
        }
        int splash = Math.Max(1, dealt / 4);
        int hits = 0;
        foreach (var u in Game.Instance.State.LoadedAreaState.MainState.AllEntityData
          .OfType<UnitEntityData>())
        {
          if (u != evt.Target && u.HPLeft > 0 && u.IsEnemy(Owner)
            && Vector3.Distance(u.Position, evt.Target.Position) <= 3.5f)
          {
            ElementalObsessor.DealRider(Owner, u, Fact, DiceFormula.Zero, splash);
            hits++;
          }
        }
        if (hits > 0)
        {
          MissionFeats.Logger.Info(
            $"[obsessor] Shattering Pitch: {splash} splash damage hit {hits} enemies near {evt.Target.CharacterName}.");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ElementalObsessor: Shattering Pitch failed.", e);
      }
    }
  }

  /// <summary>
  /// Corrosive Adaptation (acid perk option): the adopted spell joins the acid
  /// spell list when picked, and while owned its energy damage is converted to
  /// acid at the RulePrepareDamage stage (ReplaceEnergy - the TTT Elemental Spell
  /// metamagic mechanism).
  /// </summary>
  [TypeId(Guids.ElementObsessorAcidAdaptation)]
  internal class ObsessorAcidAdaptation : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintAbility Spell;
    public int Level;
    public string AcidListGuid;

    private static readonly HashSet<string> AddedToList = new();

    protected override void OnTurnOn()
    {
      try
      {
        if (Spell is null || AcidListGuid is null)
        {
          return;
        }
        string key = Spell.AssetGuid.ToString();
        if (AddedToList.Contains(key))
        {
          return;
        }
        var list = BlueprintTool.Get<BlueprintSpellList>(AcidListGuid);
        if (list?.SpellsByLevel is null)
        {
          return;
        }
        int level = Math.Max(1, Math.Min(9, Level));
        foreach (var entry in list.SpellsByLevel)
        {
          if (entry != null && entry.SpellLevel == level)
          {
            ElementalObsessor.AddSpellToEntry(entry, Spell);
            AddedToList.Add(key);
            MissionFeats.Logger.Info(
              $"[obsessor] Corrosive Adaptation: {Spell.name} joins the acid list at level {level}.");
            return;
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ElementalObsessor: adaptation list add failed.", e);
      }
    }

    public void OnEventAboutToTrigger(RulePrepareDamage evt) { }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.Reason.Ability?.Blueprint != Spell)
        {
          return;
        }
        foreach (BaseDamage damage in evt.DamageBundle)
        {
          if (damage is EnergyDamage energy && energy.EnergyType != DamageEnergyType.Acid)
          {
            energy.ReplaceEnergy(DamageEnergyType.Acid);
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ElementalObsessor: damage conversion failed.", e);
      }
    }
  }
}

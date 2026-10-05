using BlueprintCore.Blueprints.Configurators.Root;
using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.DialogSystem.Blueprints;
using MissionWOTR.Archetypes;
using MissionWOTR.Feats;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityModManagerNet;

namespace MissionWOTR
{
  public static class Main
  {
    public static bool Enabled;
    internal static string ModPath;
    internal static readonly LogWrapper Logger = LogWrapper.Get("MissionWOTR");

    public static bool Load(UnityModManager.ModEntry modEntry)
    {
      try
      {
        modEntry.OnToggle = OnToggle;
        ModPath = modEntry.Path;
        // First line in the game log: identifies the RUNNING build, so a stale or
        // missing install is obvious from the log alone (cost us a debugging round).
        Logger.Info($"MissionWOTR v{modEntry.Info.Version} loading.");
        var harmony = new Harmony(modEntry.Info.Id);
        PatchAllSafely(harmony);
        Kingmaker.PubSubSystem.EventBus.Subscribe(new ConstructAreaProbe());
        Logger.Info("MissionWOTR loaded; patches applied.");
      }
      catch (Exception e)
      {
        Logger.Error("Failed to patch", e);
      }
      return true;
    }

    /// <summary>
    /// Applies every [HarmonyPatch] class in its own try/catch: one broken patch is
    /// logged and skipped instead of aborting the mod's load (same doctrine as the
    /// per-feat configure try/catch).
    /// </summary>
    private static void PatchAllSafely(Harmony harmony)
    {
      var patchTypes = typeof(Main).Assembly.GetTypes()
        .Where(t => t.IsClass && t.IsDefined(typeof(HarmonyPatch), false))
        .ToList();
      var applied = 0;
      foreach (var t in patchTypes)
      {
        try
        {
          harmony.CreateClassProcessor(t).Patch();
          applied++;
        }
        catch (Exception e)
        {
          Logger.Error($"[patch] {t.Name} failed to apply - continuing without it.", e);
        }
      }
      Logger.Info($"[patch] {applied}/{patchTypes.Count} patch classes applied.");
    }

    public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
    {
      Enabled = value;
      return true;
    }

    // Blueprint creation/modification runs as soon as the game has loaded its blueprint cache.
    [HarmonyPatch(typeof(BlueprintsCache))]
    static class BlueprintsCaches_Patch
    {
      private static bool Initialized = false;

      [HarmonyPriority(Priority.First)]
      [HarmonyPatch(nameof(BlueprintsCache.Init)), HarmonyPostfix]
      static void Init()
      {
        try
        {
          if (Initialized)
          {
            Logger.Info("Already configured blueprints.");
            return;
          }
          Initialized = true;

          Logger.Info("Configuring blueprints.");
          LoadLocalization();
          MissionFeats.ConfigureAll();
          DumpModStamp();
          DumpDialogInventory();
          DumpCompanionInventory();
          DumpLocalizationCoverage();
          DumpGuidCensus();
        }
        catch (Exception e)
        {
          Logger.Error("Failed to configure blueprints.", e);
        }
      }
    }

    /// <summary>
    /// Registers our LocalizedStrings.json with the game's localization pack.
    /// BlueprintCore loads *Strings.json lazily (only when a string is created through its
    /// API), but our configurators reference keys directly - so without this call the game
    /// has no text for them and UIs show raw keys like "TauntingBlows.Name".
    /// </summary>
    private static void LoadLocalization()
    {
      try
      {
        var stringsFile = Path.Combine(ModPath, "LocalizedStrings.json");
        if (!File.Exists(stringsFile))
        {
          Logger.Error($"Localization file not found: {stringsFile}");
          return;
        }
        LocalizationTool.LoadLocalizationPack(stringsFile);
        Logger.Info($"Localization loaded from {stringsFile}.");
        DumpLocalizationPackShape();
      }
      catch (Exception e)
      {
        Logger.Error("Failed to load localization.", e);
      }
    }

    /// <summary>
    /// 0.56.0 diagnostic - the dialog inventory, needed before any companion
    /// conversation can be written.
    ///
    /// Why this has to be a runtime dump rather than a lookup: the CI probe reads
    /// Assembly-CSharp.dll, which is TYPE metadata. Blueprint GUIDs are game
    /// CONTENT and are not in that DLL, and BlueprintCore ships reference lists
    /// for units, abilities, features and buffs but NOT for dialogs, cues,
    /// answers or etudes - the only dialog-adjacent refs file it has is
    /// DialogExperienceModifierTableRefs. So there is no way to name a vanilla
    /// companion conversation from anything available offline.
    ///
    /// Enumerating the loaded blueprint cache gets them, via the AllBlueprints
    /// idiom ElementalObsessor already uses. This is the same loop that settled
    /// the HomebrewArchetypes roster from the playtest log.
    ///
    /// Companion UNITS are already known offline (BlueprintCore's UnitRefs names
    /// Seelah, Arueshalae, Camellia, Daeran, Ember, Nenio, Sosiel and the rest),
    /// so the missing half is purely the dialog side.
    /// </summary>
    private static void DumpDialogInventory()
    {
      try
      {
        var dialogs = ElementalObsessor.AllBlueprints<BlueprintDialog>();
        Logger.Info($"[diag] dialog inventory: {dialogs.Count} BlueprintDialog blueprints");
        foreach (var d in dialogs.OrderBy(x => x.name, StringComparer.OrdinalIgnoreCase))
        {
          Logger.Info($"[diag] DIALOG {d.name} {d.AssetGuid}");
        }
        var cues = ElementalObsessor.AllBlueprints<BlueprintCue>();
        Logger.Info($"[diag] cue inventory: {cues.Count} BlueprintCue blueprints (names omitted - too many)");
      }
      catch (Exception e)
      {
        Logger.Error("Dialog inventory dump failed.", e);
      }
    }

    /// <summary>
    /// 0.59.0 diagnostic - what build is this, and is test mode on?
    ///
    /// Test mode collapses every LevelPlan.L(n) grant to level 1 so the whole kit
    /// can be playtested from the first fight. That has caused real bugs before
    /// (options inside a multi-grant selection carrying AddPrerequisiteClassLevel
    /// become untakeable when everything lands at level 1), so the state is worth
    /// one explicit line rather than something to infer from behaviour.
    /// </summary>
    private static void DumpModStamp()
    {
      Logger.Info($"[diag] test mode LevelPlan.AllAtLevelOne = {LevelPlan.AllAtLevelOne}"
        + (LevelPlan.AllAtLevelOne
          ? " - every L(n) grant collapses to level 1, and Gate(n) options open at 1 too"
          : " - features arrive at their intended levels"));
    }

    /// <summary>
    /// 0.59.0 diagnostic - the companion side of the dialog work.
    ///
    /// DumpDialogInventory covers conversations; this covers who is in them. Story
    /// companions are identified by the game's own UnitIsStoryCompanion component
    /// rather than by a hardcoded name list, matched on type NAME so no game type
    /// has to be named here. Etudes come along for the ride: BlueprintEtude is a
    /// BlueprintFact, so it is the other way a conversation can be triggered, and
    /// knowing which ones exist matters if a mod ever collides with ours.
    /// </summary>
    private static void DumpCompanionInventory()
    {
      try
      {
        var units = ElementalObsessor.AllBlueprints<BlueprintUnit>();
        var companions = units
          .Where(u => u.ComponentsArray != null
            && u.ComponentsArray.Any(c => c != null && c.GetType().Name == "UnitIsStoryCompanion"))
          .OrderBy(u => u.name, StringComparer.OrdinalIgnoreCase)
          .ToList();
        Logger.Info($"[diag] companion inventory: {companions.Count} of {units.Count} units are story companions");
        foreach (var u in companions)
        {
          Logger.Info($"[diag] COMPANION {u.name} {u.AssetGuid}");
        }

        var etudes = ElementalObsessor.AllBlueprints<BlueprintEtude>();
        Logger.Info($"[diag] etude inventory: {etudes.Count} BlueprintEtude blueprints");
        foreach (var e in etudes.OrderBy(x => x.name, StringComparer.OrdinalIgnoreCase))
        {
          Logger.Info($"[diag] ETUDE {e.name} {e.AssetGuid}");
        }
      }
      catch (Exception e)
      {
        Logger.Error("Companion inventory dump failed.", e);
      }
    }

    /// <summary>
    /// 0.59.0 diagnostic - do our strings actually resolve?
    ///
    /// BlueprintCore only loads *Strings.json lazily, and our configurators
    /// reference keys directly, so a key that fails to register shows up in game
    /// as raw text like "TauntingBlows.Name". That is easy to miss on a tooltip
    /// you are not looking at. This compares every key in our own file against
    /// what the loaded pack actually holds and names the ones that are missing.
    ///
    /// Reflection is used deliberately: the pack's internals are exactly what
    /// DumpLocalizationPackShape is fingerprinting, and neither the field names
    /// nor the StringEntry shape are known for certain yet. Every step degrades
    /// to a warning instead of throwing.
    /// </summary>
    private static void DumpLocalizationCoverage()
    {
      try
      {
        var manager = Type.GetType("Kingmaker.Localization.LocalizationManager, Assembly-CSharp");
        var current = manager?.GetProperty(
          "CurrentPack", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (current is null)
        {
          Logger.Warn("[diag] coverage: LocalizationManager.CurrentPack unreachable.");
          return;
        }

        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        if (current.GetType().GetField("m_Strings", flags)?.GetValue(current)
            is not System.Collections.IEnumerable entries)
        {
          Logger.Warn("[diag] coverage: m_Strings missing or not enumerable.");
          return;
        }

        var present = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
          if (entry is null) { continue; }
          if (entry.GetType().GetField("Key", flags)?.GetValue(entry) is string key)
          {
            present.Add(key);
          }
        }

        var ours = LoadOurStringKeys();
        var missing = ours.Where(k => !present.Contains(k))
          .OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        Logger.Info($"[diag] coverage: the pack holds {present.Count} strings; "
          + $"{ours.Count - missing.Count}/{ours.Count} of ours resolve");
        foreach (var k in missing.Take(40))
        {
          Logger.Info($"[diag] LOC-MISSING {k}");
        }
        if (missing.Count > 40)
        {
          Logger.Info($"[diag] LOC-MISSING ... and {missing.Count - 40} more");
        }
      }
      catch (Exception e)
      {
        Logger.Warn($"[diag] localization coverage failed: {e}");
      }
    }

    /// <summary>
    /// Reads the keys out of our own LocalizedStrings.json with a regex rather than
    /// a JSON dependency, because the only thing needed is the key names.
    /// </summary>
    private static HashSet<string> LoadOurStringKeys()
    {
      var keys = new HashSet<string>(StringComparer.Ordinal);
      try
      {
        var path = Path.Combine(ModPath, "LocalizedStrings.json");
        if (!File.Exists(path)) { return keys; }
        foreach (Match m in Regex.Matches(File.ReadAllText(path), "\"Key\"\\s*:\\s*\"([^\"]+)\""))
        {
          keys.Add(m.Groups[1].Value);
        }
      }
      catch (Exception e)
      {
        Logger.Warn($"[diag] could not read our own string keys: {e}");
      }
      return keys;
    }

    /// <summary>
    /// 0.59.0 diagnostic - which declared GUIDs never became a blueprint?
    ///
    /// BlueprintCore keys blueprints by name, so a duplicate name aborts
    /// Configure(), and MissionFeats.Configure wraps each call in a try/catch -
    /// the failure is logged but the game carries on with the feature simply
    /// absent. That class of bug is invisible in play: nothing errors, the
    /// archetype is just missing a piece. Comparing every const in Guids against
    /// what actually materialized surfaces it.
    ///
    /// Expect a small non-zero count: the constants left behind by withdrawn
    /// archetypes are deliberately never created.
    /// </summary>
    private static void DumpGuidCensus()
    {
      try
      {
        var fields = typeof(Guids)
          .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
          .Where(f => f.IsLiteral && f.FieldType == typeof(string))
          .ToArray();
        var missing = new List<string>();
        foreach (var f in fields)
        {
          if (f.GetValue(null) is not string guid || string.IsNullOrEmpty(guid)) { continue; }
          if (!BlueprintTool.TryGet<SimpleBlueprint>(guid, out var bp) || bp is null)
          {
            missing.Add(f.Name);
          }
        }
        Logger.Info($"[diag] guid census: {fields.Length} declared, {missing.Count} did not materialize "
          + "(withdrawn archetypes legitimately account for some)");
        foreach (var name in missing.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Take(40))
        {
          Logger.Info($"[diag] GUID-MISSING {name}");
        }
        if (missing.Count > 40)
        {
          Logger.Info($"[diag] GUID-MISSING ... and {missing.Count - 40} more");
        }
      }
      catch (Exception e)
      {
        Logger.Warn($"[diag] guid census failed: {e}");
      }
    }

    /// <summary>
    /// 0.53.0 diagnostic - the "unknown mod" report.
    ///
    /// The game prints a mod's name in small type above a tooltip's
    /// description, and ours prints "unknown mod" where other mods print
    /// their own. The likely cause is that BlueprintCore's
    /// MultiLocalizationPack.GetCurrentPack() builds a LocalizationPack with
    /// only Locale and m_Strings populated, so whatever field carries the
    /// attribution is never set. That is a hypothesis, not a finding: the
    /// game assembly is not available in this environment and the probe
    /// never fingerprinted Kingmaker.Localization, so the field's name - and
    /// whether it lives on the pack or on each StringEntry - is unknown.
    ///
    /// Guessing is worse than useless here. If the field is on the pack and
    /// LocalizationManager.CurrentPack.AddStrings only copies m_Strings,
    /// setting it on our own pack would do nothing, and setting it on the
    /// shared pack would mislabel every string in the game as ours. So this
    /// dumps the real shape into the log instead; the next playtest log
    /// answers the question with evidence and the fix can then be exact.
    ///
    /// Read-only: it reads no game state it does not log and changes nothing.
    /// </summary>
    private static void DumpLocalizationPackShape()
    {
      try
      {
        var manager = Type.GetType(
          "Kingmaker.Localization.LocalizationManager, Assembly-CSharp");
        var current = manager?.GetProperty(
          "CurrentPack", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (current is null)
        {
          Logger.Warn("[diag] LocalizationManager.CurrentPack unreachable - cannot inspect.");
          return;
        }

        var type = current.GetType();
        Logger.Info($"[diag] localization pack type: {type.FullName}");
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        foreach (var field in type.GetFields(flags))
        {
          // Only string fields are worth reading: a collection field would
          // dump every localized string in the game into the log.
          var value = field.FieldType == typeof(string)
            ? $" = {field.GetValue(current) ?? "(null)"}"
            : "";
          Logger.Info($"[diag] pack field: {field.FieldType.Name} {field.Name}{value}");
        }
        foreach (var property in type.GetProperties(flags))
        {
          Logger.Info($"[diag] pack property: {property.PropertyType.Name} {property.Name}");
        }

        var entry = type.GetNestedType("StringEntry");
        if (entry is null)
        {
          Logger.Info("[diag] LocalizationPack has no nested StringEntry type.");
          return;
        }
        foreach (var field in entry.GetFields(flags))
        {
          Logger.Info($"[diag] StringEntry field: {field.FieldType.Name} {field.Name}");
        }
      }
      catch (Exception e)
      {
        Logger.Warn($"[diag] localization pack dump failed: {e}");
      }
    }

    // BlueprintCore commits blueprints configured with delayed: true here.
    [HarmonyPatch(typeof(StartGameLoader))]
    static class StartGameLoader_Patch
    {
      private static bool Initialized = false;

      [HarmonyPatch(nameof(StartGameLoader.LoadPackTOC)), HarmonyPostfix]
      static void LoadPackTOC()
      {
        try
        {
          if (Initialized)
          {
            Logger.Info("Already configured delayed blueprints.");
            return;
          }
          Initialized = true;

          Logger.Info("Committing delayed blueprints...");
          var stopwatch = System.Diagnostics.Stopwatch.StartNew();
          RootConfigurator.ConfigureDelayedBlueprints();
          stopwatch.Stop();
          Logger.Info($"Delayed blueprints committed in {stopwatch.ElapsedMilliseconds} ms.");

          // Final archetype state (after delayed commits) - self-diagnosing playtest logs.
          MissionFeats.LogArchetypeDiagnostics();
        }
        catch (Exception e)
        {
          Logger.Error("Failed to configure delayed blueprints.", e);
        }
      }
    }
  }
}

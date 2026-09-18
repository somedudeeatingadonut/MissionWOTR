using BlueprintCore.Blueprints.Configurators.Root;
using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using MissionWOTR.Feats;
using System;
using System.IO;
using System.Linq;
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
      }
      catch (Exception e)
      {
        Logger.Error("Failed to load localization.", e);
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

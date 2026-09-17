using BlueprintCore.Blueprints.Configurators.Root;
using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using MissionWOTR.Feats;
using System;
using System.IO;
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
        var harmony = new Harmony(modEntry.Info.Id);
        harmony.PatchAll();
        Logger.Info("MissionWOTR loaded; patches applied.");
      }
      catch (Exception e)
      {
        Logger.Error("Failed to patch", e);
      }
      return true;
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

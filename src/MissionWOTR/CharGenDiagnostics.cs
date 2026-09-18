using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Class;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MissionWOTR
{
  // ---------------------------------------------------------------------------
  // Char-gen runtime diagnostics for the greyed-archetype investigation.
  //
  // Proven so far (0.4.6 logs): our archetypes are fully registered
  // (onClass/inAvailableList/minLevel all good), the item VMs are built with
  // available=True, prerequisitesDone=True - identical to base archetypes - and
  // the PrestigePlus DisposeImplementation crash fires on EVERY class phase
  // (magus too) yet never blocked MissionVanguard. The grey-out therefore lives
  // BELOW the availability VM state: in the view/selection layer, silently.
  //
  // This probe diffs the full runtime state of OUR items against BASE items:
  // exact item type (a mod subclassing/replacing item VMs shows instantly),
  // every bool/enum/string property, and a forced tooltip build - an exception
  // there names the exact broken piece of archetype data.
  // ---------------------------------------------------------------------------

  /// <summary>
  /// Logs the availability computation for our archetypes while the class phase builds.
  /// </summary>
  [HarmonyPatch(typeof(CharGenClassSelectorItemVM), "IsArchetypeAvailable")]
  internal static class ArchetypeAvailabilityLogger
  {
    private static readonly LogWrapper Logger = LogWrapper.Get("MissionWOTR.CharGen");

    [HarmonyPostfix]
    internal static void Postfix(bool __result, BlueprintArchetype __1)
    {
      try
      {
        var n = __1?.name;
        if (n is null || !(n.Contains("Eldritch") || n.Contains("Construct") || n.Contains("Vanguard")))
        {
          return;
        }
        Logger.Info($"[diag] IsArchetypeAvailable({n}) = {__result}.");
      }
      catch (Exception e)
      {
        Logger.Info($"[diag] availability log failed: {e.Message}");
      }
    }
  }

  /// <summary>
  /// Full state probe: for each rendered archetype item (ours plus base controls),
  /// dumps item type, all simple properties, and forces the tooltip build so any
  /// data-level failure surfaces with its exception. Throttled.
  /// </summary>
  [HarmonyPatch(typeof(CharGenClassSelectorItemVM), "GetArchetypesList")]
  internal static class ArchetypeListLogger
  {
    private static readonly LogWrapper Logger = LogWrapper.Get("MissionWOTR.CharGen");
    private static int loggedCalls;

    [HarmonyPostfix]
    internal static void Postfix(object __result)
    {
      try
      {
        if (++loggedCalls > 5 || __result is not IEnumerable items)
        {
          return;
        }
        var probed = 0;
        foreach (var item in items)
        {
          if (item is null)
          {
            continue;
          }
          var t = Traverse.Create(item);
          var archetype = t.Field("Archetype").GetValue<BlueprintArchetype>();
          var name = archetype?.name;
          var ours = name is not null &&
            (name.Contains("Eldritch") || name.Contains("Construct") || name.Contains("Vanguard"));
          // Probe our items plus the first base item as a control sample.
          if (!ours && probed > 0)
          {
            continue;
          }
          if (probed >= 5)
          {
            break;
          }
          probed++;
          Logger.Info($"[probe] {Describe(item, archetype)}");
        }
      }
      catch (Exception e)
      {
        Logger.Info($"[diag] archetype list log failed: {e.Message}");
      }
    }

    private static string Describe(object item, BlueprintArchetype archetype)
    {
      var sb = new StringBuilder();
      sb.Append(item.GetType().Name);
      sb.Append($" [{archetype?.name ?? "<class base>"}]");
      sb.Append($" icon={(archetype?.Icon != null ? "set" : "null")}");

      // Every simple readable property: IsAvailable/IsAvailible/IsSelected/nesting...
      foreach (var p in item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead &&
          (p.PropertyType == typeof(bool) || p.PropertyType == typeof(string) ||
           p.PropertyType.IsEnum || p.PropertyType == typeof(int)))
        .OrderBy(p => p.Name))
      {
        string value;
        try
        {
          value = p.GetValue(item)?.ToString() ?? "null";
        }
        catch (Exception e)
        {
          value = $"THREW: {e.InnerException?.Message ?? e.Message}";
        }
        sb.Append($" | {p.Name}={value}");
      }

      // Force the tooltip template build: if our archetype's data breaks it, the
      // exception names the culprit. (The empty tooltip is a live symptom.)
      try
      {
        var tip = item.GetType().GetProperty("TooltipTemplate",
          BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(item);
        sb.Append($" | tooltip={(tip is null ? "NULL" : tip.GetType().Name)}");
      }
      catch (Exception e)
      {
        var root = e;
        while (root.InnerException is not null)
        {
          root = root.InnerException;
        }
        sb.Append($" | tooltip THREW: {root.GetType().Name}: {root.Message}");
      }
      return sb.ToString();
    }
  }
}

  /// <summary>
  /// Logs when one of OUR archetypes enters the level-up plan (char-gen selection),
  /// then dumps everything ConstructCrafter-flavored the unit actually received once
  /// the progression grant runs - features AND abilities - so 'the gimmicks are
  /// missing' becomes a precise list in the log.
  /// </summary>
  [HarmonyPatch(typeof(LevelUpController), "AddArchetype")]
  internal static class ArchetypeAddedLogger
  {
    private static readonly LogWrapper Logger = LogWrapper.Get("MissionWOTR.CharGen");

    [HarmonyPostfix]
    internal static void Postfix(BlueprintArchetype __0)
    {
      try
      {
        var n = __0?.name;
        if (n is not null &&
          (n.Contains("Eldritch") || n.Contains("Construct") || n.Contains("Vanguard")))
        {
          Logger.Info($"[levelup] archetype {n} added to the level-up plan.");
        }
      }
      catch (Exception e)
      {
        Logger.Info($"[levelup] archetype-added log failed: {e.Message}");
      }
    }
  }

  [HarmonyPatch(typeof(LevelUpHelper), "UpdateProgression")]
  internal static class ProgressionGrantLogger
  {
    private static readonly LogWrapper Logger = LogWrapper.Get("MissionWOTR.CharGen");
    private static int logged;

    [HarmonyPostfix]
    internal static void Postfix(UnitDescriptor __1)
    {
      try
      {
        if (__1 is null || logged > 20)
        {
          return;
        }
        var classes = string.Join(", ", __1.Progression.Classes.Select(c =>
          $"{c.CharacterClass.name}({c.Level})" +
          (c.Archetypes.Count == 0 ? "" :
            "/" + string.Join("+", c.Archetypes.Select(a => a.name)))));
        var ours = __1.Progression.Features
          .Select(f => f.Blueprint?.name)
          .Where(n => n is not null && n.StartsWith("ConstructCrafter"))
          .OrderBy(n => n)
          .ToList();
        if (ours.Count == 0 && !classes.Contains("Construct"))
        {
          return;
        }
        logged++;
        Logger.Info($"[levelup] unit={__1.Unit?.Blueprint?.name ?? "?"} classes={classes}");
        Logger.Info(
          $"[levelup] ConstructCrafter features on unit ({ours.Count}): " +
          string.Join(", ", ours));
        // Abilities are facts too - dump any ConstructCrafter-named ones.
        var abilityNames = __1.Abilities?
          .Where(a => a.Blueprint?.name?.StartsWith("ConstructCrafter") == true)
          .Select(a => a.Blueprint.name).ToList();
        if (abilityNames is not null)
        {
          Logger.Info($"[levelup] ConstructCrafter abilities on unit ({abilityNames.Count}): " +
            string.Join(", ", abilityNames));
        }
      }
      catch (Exception e)
      {
        Logger.Info($"[levelup] progression log failed: {e.Message}");
      }
    }
  }

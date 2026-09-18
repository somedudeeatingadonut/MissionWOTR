using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Class;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.Progression.Main;
using System;
using System.Collections;

namespace MissionWOTR
{
  // ---------------------------------------------------------------------------
  // Char-gen runtime diagnostics + a defensive patch for a PrestigePlus bug that
  // breaks the class phase exactly where our archetypes live.
  //
  // PrestigePlus's FixNoToybox2 is a prefix on ClassProgressionVM.DisposeImplementation:
  //     if (__instance.ProgressionVms.First() == null) __instance.ProgressionVms = [];
  // .First() (not FirstOrDefault) throws InvalidOperationException("Sequence contains
  // no elements") whenever the list is EMPTY. The throw propagates up through
  // UnitProgressionVM.RefreshData into CharGenVM.UpdateAllPhases and aborts the phase
  // update, leaving the class/archetype list half-refreshed: our archetype renders
  // greyed out with an empty tooltip even though it is fully registered and selectable
  // (confirmed by [diag] lines: onClass/inAvailableList/minLevel). The finalizer below
  // swallows exactly that crash so the update completes.
  //
  // NOTE: IsArchetypeAvailable is STATIC, so its postfix must not declare __instance.
  // Patch classes are applied one-by-one by Main.PatchAllSafely - a bad patch can
  // never take the whole mod down.
  // ---------------------------------------------------------------------------

  [HarmonyPatch(typeof(ClassProgressionVM), nameof(ClassProgressionVM.DisposeImplementation))]
  internal static class ClassProgressionDisposeSuppressor
  {
    private static readonly LogWrapper Logger = LogWrapper.Get("MissionWOTR.CharGen");

    [HarmonyFinalizer]
    internal static bool Finalizer(Exception __exception)
    {
      if (__exception is InvalidOperationException)
      {
        Logger.Info(
          "[diag] Suppressed crash in ClassProgressionVM.DisposeImplementation " +
          $"({__exception.Message}); PrestigePlus's FixNoToybox2 calls ProgressionVms.First() " +
          "on an empty list, which aborts char-gen phase updates.");
        return false; // swallow so CharGenVM.UpdateAllPhases finishes
      }
      return true;
    }
  }

  /// <summary>
  /// Logs the availability computation for our archetypes while the class phase builds.
  /// IsArchetypeAvailable(controller, archetype) is static; the result is what feeds the
  /// item's canSelect at level-up (at fresh char-gen the item is forced available, so a
  //  greyed item here means the phase build itself was aborted - see the suppressor).
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
  /// Dumps the final state of every archetype item the class phase actually rendered,
  /// so a playtest log answers "was the item built available?" without guessing.
  /// Throttled: only the first few invocations are logged.
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
        foreach (var item in items)
        {
          if (item is null)
          {
            continue;
          }
          var t = Traverse.Create(item);
          var archetype = t.Field("Archetype").GetValue<BlueprintArchetype>();
          var prerequisitesDone = t.Field("PrerequisitesDone").GetValue<bool>();
          string available;
          try
          {
            available = Convert.ToString(t.Property("IsAvailible").GetValue());
          }
          catch
          {
            available = "?";
          }
          Logger.Info(
            $"[diag] archetype item {archetype?.name ?? "<class base>"}: " +
            $"available={available}, prerequisitesDone={prerequisitesDone}.");
        }
      }
      catch (Exception e)
      {
        Logger.Info($"[diag] archetype list log failed: {e.Message}");
      }
    }
  }
}

using BlueprintCore.Blueprints.Configurators.DialogSystem;
using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.DialogSystem.Blueprints;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.61.0 - the companion conversation test line.
  ///
  /// The goal is to prove the Gracious Friendships mechanism in game: GF patches a
  /// companion's vanilla BlueprintDialog and merges cues into its opening CueSelection,
  /// whose Strategy is "First" - the first cue whose Conditions pass wins. This does the
  /// same thing to Seelah's main dialog with one unmistakable all-caps line, so that if
  /// the line shows up, injected companion dialogue works and the rest can be built on it.
  ///
  /// The one thing this build does not have is the GUID of Seelah's dialog. Two routes to
  /// it both came up empty: the startup dialog dump enumerates BlueprintsCache while
  /// vanilla dialogs are still unmaterialized (it found one BlueprintDialog against 573
  /// cues), and the GF probe records the vanilla references each patch touches but not the
  /// patch's own AssetId, which is the dialog's GUID. So the dialog is matched by NAME at
  /// the moment it materializes, through a postfix on BlueprintsCache.Load.
  ///
  /// That is deliberately self-reporting: every BlueprintDialog that passes through is
  /// logged with its name and GUID. So this either works, or the next playtest log names
  /// the exact dialog to target by GUID and the name matching can be retired.
  /// </summary>
  internal static class CompanionTestLines
  {
    internal const string TestCueName = "MissionWOTRSeelahTestCue";

    // UnitRefs: Seelah.
    private const string SeelahUnitGuid = "8608eed026b849f4a8690f846bb8ec62";

    private const string TestText =
      "!!! MISSION WOTR TEST LINE !!! IF YOU CAN READ THIS, INJECTED COMPANION " +
      "DIALOGUE WORKS. THIS IS NOT REAL SEELAH DIALOGUE AND WILL BE REPLACED. " +
      "PLEASE TELL ME YOU SAW THIS. !!!";

    private const BindingFlags F =
      BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private static BlueprintCue s_cue;
    private static readonly HashSet<string> s_injected = new();
    private static bool s_installed;

    /// <summary>Builds the test cue. Called once at startup, before any dialog loads.</summary>
    internal static void Configure()
    {
      try
      {
        s_cue = CueConfigurator.New(TestCueName, Guids.SeelahTestCue).Configure();
        typeof(BlueprintCue).GetField("Text", F)?.SetValue(
          s_cue, (Kingmaker.Localization.LocalizedString)TestText);

        // Give the line a speaker so it is visibly hers rather than narrator text.
        try
        {
          var speaker = BlueprintTool.GetRef<Kingmaker.Blueprints.UnitReference>(SeelahUnitGuid);
          typeof(BlueprintCue).GetField("Speaker", F)?.SetValue(s_cue, speaker);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Warn(
            $"[testline] could not set the cue's speaker ({e.GetType().Name}); the line will " +
            "still display.");
        }

        MissionFeats.Logger.Info(
          $"[testline] test cue built: {TestCueName} {Guids.SeelahTestCue.Replace("-", "").ToLowerInvariant()}");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[testline] failed to build the test cue.", e);
      }
    }

    /// <summary>
    /// Hooks BlueprintsCache.Load by reflection rather than by attribute: the method's
    /// exact signature and visibility are not part of the reference assemblies this build
    /// compiles against, and a wrong [HarmonyPatch] would abort the patch pass. A missing
    /// method is logged and skipped instead.
    /// </summary>
    internal static void Install(Harmony harmony)
    {
      try
      {
        if (s_installed) { return; }
        var load = typeof(BlueprintsCache).GetMethod(
          "Load", F, null, new[] { typeof(BlueprintGuid) }, null);
        if (load is null)
        {
          MissionFeats.Logger.Warn(
            "[testline] BlueprintsCache.Load(BlueprintGuid) not found - the Seelah test line " +
            "will not be injected. Dialogs that do load will not be reported either.");
          return;
        }
        var postfix = typeof(CompanionTestLines).GetMethod(
          nameof(AfterLoad), BindingFlags.Static | BindingFlags.NonPublic);
        harmony.Patch(load, postfix: new HarmonyMethod(postfix));
        s_installed = true;
        MissionFeats.Logger.Info(
          $"[testline] hooked {load.DeclaringType?.Name}.{load.Name} - watching for Seelah's dialog.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[testline] failed to hook BlueprintsCache.Load.", e);
      }
    }

    private static void AfterLoad(object __result)
    {
      try
      {
        if (__result is not BlueprintDialog dialog) { return; }
        var guid = dialog.AssetGuid.ToString().Replace("-", "").ToLowerInvariant();
        MissionFeats.Logger.Info($"[testline] DIALOG {dialog.name} {guid}");
        if (s_cue is null || dialog.name is null) { return; }
        if (dialog.name.IndexOf("Seelah", StringComparison.OrdinalIgnoreCase) < 0) { return; }
        if (!s_injected.Add(guid)) { return; }
        Inject(dialog);
      }
      catch (Exception e)
      {
        // Never let a diagnostic hook break dialog loading.
        MissionFeats.Logger.Error("[testline] injection failed.", e);
      }
    }

    private static void Inject(BlueprintDialog dialog)
    {
      var firstCueField = typeof(BlueprintDialog).GetField("FirstCue", F);
      var firstCue = firstCueField?.GetValue(dialog);
      if (firstCue is null)
      {
        MissionFeats.Logger.Warn($"[testline] {dialog.name} has no FirstCue - not injected.");
        return;
      }

      var cuesField = firstCue.GetType().GetField("Cues", F);
      var cues = cuesField?.GetValue(firstCue) as List<BlueprintCueReference>;
      if (cues is null)
      {
        MissionFeats.Logger.Warn($"[testline] {dialog.name} FirstCue.Cues is not a cue list - not injected.");
        return;
      }

      // Copy the opening cues BEFORE inserting, and chain the test cue into that copy.
      // Sharing the live list would put our own cue into its own Continue and loop.
      var original = new List<BlueprintCueReference>(cues);
      var strategyField = firstCue.GetType().GetField("Strategy", F);
      var strategyValue = strategyField?.GetValue(firstCue);

      var continuation = Activator.CreateInstance(firstCue.GetType());
      firstCue.GetType().GetField("Cues", F)?.SetValue(continuation, original);
      if (strategyField is not null) { strategyField.SetValue(continuation, strategyValue); }
      typeof(BlueprintCue).GetField("Continue", F)?.SetValue(s_cue, continuation);

      cues.Insert(0, BlueprintTool.GetRef<BlueprintCueReference>(TestCueName));

      MissionFeats.Logger.Info(
        $"[testline] INJECTED into {dialog.name} - the test line now leads " +
        $"{cues.Count} opening cue(s), chaining into {original.Count} original(s).");
    }
  }
}

using BlueprintCore.Blueprints.Configurators.DialogSystem;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
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
  /// 0.62.0 - injected companion dialogue, proven with test lines.
  ///
  /// The mechanism is the Gracious Friendships one: put a cue at the head of an existing
  /// conversation's opening CueSelection, whose Strategy is First, so the first cue whose
  /// Conditions pass wins. Setting the cue's Speaker lets a companion react inside someone
  /// else's conversation - which is what "Seelah has something to say about the mongrels"
  /// needs, rather than a line in her own dialog.
  ///
  /// Why this attaches by NAME rather than by GUID: the engine has no name-to-GUID lookup.
  /// BlueprintsCache.Init reads blueprints-pack.bbp as 16-byte GUID + 4-byte offset per
  /// entry with no names at all, and both BlueprintsCache and ResourcesLibrary are keyed
  /// purely by GUID - a vanilla dialog's name only exists once it is materialized. GF is in
  /// the same boat: its .patch files carry no AssetId, and the target is the filename
  /// gfr__<VanillaAssetName>.patch, resolved by name at runtime.
  ///
  /// So dialogs are matched by name as they materialize, through a postfix on
  /// BlueprintsCache.Load. That needs nothing from the player first - it does not matter
  /// whether the conversation has been seen before.
  ///
  /// The target names come from GF's own patch list, which names the assets INSIDE a
  /// conversation with the conversation's name as a prefix: ch0_choice_wenduag_lann_Answer_0013,
  /// ch0_choice_wenduag_lann_AnswersList_0106, and so on. That makes the dialog itself
  /// ch0_choice_wenduag_lann - the first meeting with Lann and Wenduag. The hook is
  /// self-reporting: every dialog that materializes is logged with its name and GUID, so if
  /// a guess is wrong the next log names the real one.
  /// </summary>
  internal static class CompanionTestLines
  {
    // UnitRefs: Seelah.
    private const string SeelahUnitGuid = "8608eed026b849f4a8690f846bb8ec62";

    private const BindingFlags F =
      BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private sealed class Rule
    {
      // Preferred: an exact AssetGuid, which needs no guessing at all.
      public string MatchGuid;
      // Fallback: a name substring, for the dialogs whose GUID is still unknown.
      public string Match;
      public string CueName;
      public string CueGuid;
      public string Text;
      // Null = the dialog's own owner speaks it.
      public string SpeakerGuid;
      public BlueprintCue Cue;
    }

    private static readonly Rule[] s_rules =
    {
      new Rule
      {
        // "MeetLann" - the first meeting with Lann and Wenduag. From probe-dialogs.txt,
        // built by CI out of blueprints.zip on the game-libs release. The name this was
        // guessed at before (ch0_choice_wenduag_lann) is not a dialog at all: it is the
        // prefix Owlcat gives the cues and answers INSIDE the conversation, which is what
        // GF's patch files name. Guessing from those was wrong; the catalog is not.
        MatchGuid = "d39643a2584efac449060b733c98b0c0",
        CueName = "MissionWOTRSeelahMongrelTestCue",
        CueGuid = Guids.SeelahMongrelTestCue,
        SpeakerGuid = SeelahUnitGuid,
        Text =
          "!!! MISSION WOTR TEST LINE !!! SEELAH REACTING TO MEETING LANN AND WENDUAG. " +
          "IF YOU CAN READ THIS, INJECTED COMPANION DIALOGUE WORKS. THIS IS NOT REAL " +
          "SEELAH DIALOGUE AND WILL BE REPLACED WITH PROPER WRITING. !!!",
      },
      new Rule
      {
        // "Seelah_Main_dialog" - ed7b39d0716d25c439f2b93c409da883. This was name-matched
        // until probe v3: the catalog had it all along, but 943 of the 1711 dialogs had been
        // given a component's name instead of their own, so it did not turn up in a search.
        MatchGuid = "ed7b39d0716d25c439f2b93c409da883",
        CueName = "MissionWOTRSeelahMainTestCue",
        CueGuid = Guids.SeelahTestCue,
        SpeakerGuid = null,
        Text =
          "!!! MISSION WOTR TEST LINE !!! THIS IS THE HEAD OF SEELAH'S OWN CONVERSATION. " +
          "IF YOU CAN READ THIS, INJECTED COMPANION DIALOGUE WORKS. THIS IS NOT REAL " +
          "SEELAH DIALOGUE AND WILL BE REPLACED WITH PROPER WRITING. !!!",
      },
    };

    private static readonly HashSet<string> s_injected = new();
    private static bool s_installed;

    /// <summary>Builds one cue per rule. Called once at startup, before any dialog loads.</summary>
    internal static void Configure()
    {
      foreach (var rule in s_rules)
      {
        try
        {
          var configurator = CueConfigurator.New(rule.CueName, rule.CueGuid).SetText(rule.Text);
          if (rule.SpeakerGuid is not null)
          {
            // DialogSpeakers.New takes Blueprint<BlueprintUnitReference>, which an implicit
            // cast builds from the GUID string.
            configurator = configurator.SetSpeaker(DialogSpeakers.New(rule.SpeakerGuid));
          }
          rule.Cue = configurator.Configure();
          MissionFeats.Logger.Info(
            $"[testline] cue built: {rule.CueName} {Flat(rule.CueGuid)} " +
            $"speaker={(rule.SpeakerGuid is null ? "dialog owner" : Flat(rule.SpeakerGuid))} " +
            $"match=\"{rule.Match}\"");
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error($"[testline] failed to build cue {rule.CueName}.", e);
        }
      }
    }

    /// <summary>
    /// Hooks BlueprintsCache.Load by reflection rather than by attribute: the method's exact
    /// signature and visibility are not part of the reference assemblies this build compiles
    /// against, and a wrong [HarmonyPatch] would abort the patch pass. A missing method is
    /// logged and skipped instead.
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
            "[testline] BlueprintsCache.Load(BlueprintGuid) not found - no dialogue will be " +
            "injected, and dialogs will not be reported either.");
          return;
        }
        var postfix = typeof(CompanionTestLines).GetMethod(
          nameof(AfterLoad), BindingFlags.Static | BindingFlags.NonPublic);
        harmony.Patch(load, postfix: new HarmonyMethod(postfix));
        s_installed = true;
        MissionFeats.Logger.Info(
          $"[testline] hooked {load.DeclaringType?.Name}.{load.Name} - " +
          $"{s_rules.Length} injection rule(s) armed.");
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
        if (__result is not BlueprintDialog dialog || dialog.name is null) { return; }
        var guid = Flat(dialog.AssetGuid.ToString());
        MissionFeats.Logger.Info($"[testline] DIALOG {dialog.name} {guid}");
        foreach (var rule in s_rules)
        {
          if (rule.Cue is null) { continue; }
          var hit = rule.MatchGuid is not null
            ? string.Equals(guid, rule.MatchGuid, StringComparison.OrdinalIgnoreCase)
            : rule.Match is not null &&
              dialog.name.IndexOf(rule.Match, StringComparison.OrdinalIgnoreCase) >= 0;
          if (!hit) { continue; }
          if (!s_injected.Add($"{guid}:{rule.CueName}")) { continue; }
          MissionFeats.Logger.Info($"[testline] matched {dialog.name} by " +
            (rule.MatchGuid is not null ? "GUID" : $"name \"{rule.Match}\""));
          Inject(dialog, rule);
        }
      }
      catch (Exception e)
      {
        // Never let a diagnostic hook break dialog loading.
        MissionFeats.Logger.Error("[testline] injection failed.", e);
      }
    }

    private static void Inject(BlueprintDialog dialog, Rule rule)
    {
      var firstCue = typeof(BlueprintDialog).GetField("FirstCue", F)?.GetValue(dialog);
      if (firstCue is null)
      {
        MissionFeats.Logger.Warn($"[testline] {dialog.name} has no FirstCue - not injected.");
        return;
      }

      var cues = firstCue.GetType().GetField("Cues", F)?.GetValue(firstCue)
        as List<BlueprintCueBaseReference>;
      if (cues is null)
      {
        MissionFeats.Logger.Warn(
          $"[testline] {dialog.name} FirstCue.Cues is not a cue list - not injected.");
        return;
      }

      // Copy the opening cues BEFORE inserting, and chain the test cue into that copy.
      // Sharing the live list would put our own cue into its own Continue and loop.
      var original = new List<BlueprintCueBaseReference>(cues);
      var strategyField = firstCue.GetType().GetField("Strategy", F);
      var continuation = Activator.CreateInstance(firstCue.GetType());
      firstCue.GetType().GetField("Cues", F)?.SetValue(continuation, original);
      if (strategyField is not null)
      {
        strategyField.SetValue(continuation, strategyField.GetValue(firstCue));
      }
      typeof(BlueprintCue).GetField("Continue", F)?.SetValue(rule.Cue, continuation);

      cues.Insert(0, BlueprintTool.GetRef<BlueprintCueBaseReference>(rule.CueName));

      MissionFeats.Logger.Info(
        $"[testline] INJECTED {rule.CueName} into {dialog.name} - the test line now leads " +
        $"{cues.Count} opening cue(s), chaining into {original.Count} original(s).");
    }

    private static string Flat(string guid) =>
      (guid ?? "").Replace("-", "").ToLowerInvariant();
  }
}

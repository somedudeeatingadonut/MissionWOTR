using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using Kingmaker.Blueprints.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Archetype feature removals, derived from the class's ACTUAL progression.
  ///
  /// The level-up gate UnitProgressionData.CanAddArchetype (called inside
  /// LevelUpController.AddArchetype, whose bool result the vanilla char-gen UI
  /// silently ignores) requires that every feature an archetype removes is
  /// granted by the class progression AT THAT EXACT LEVEL - removing a feature
  /// the progression does not list there makes the whole archetype un-addable:
  /// the item stays visually selected but the character gets a plain class with
  /// no archetype features. (MissionVanguard has no removals, which is why it
  /// always worked.)
  ///
  /// Instead of hard-coding level guesses, AddRemovals finds the level entry in
  /// the live class progression that grants each feature and registers the
  /// removal there - adapting automatically to other mods that reshape the
  /// class. Features not present in the progression are skipped and logged.
  ///
  /// Matching notes (learned the hard way in playtest logs):
  /// - Callers pass either an asset name ("MagusSpellRecall") or a guid string,
  ///   dashed or undashed (BPCore's FeatureRefs.ToString() yields the DASHED
  ///   form). Both sides are normalized before comparison.
  /// - SimpleBlueprint.AssetGuid is a BlueprintGuid STRUCT, not a string; it
  ///   must be stringified before comparing.
  /// - The resolved feature BLUEPRINT is passed to AddToRemoveFeatures (not the
  ///   name string): BPCore's implicit string conversion runs Guid.Parse, which
  ///   throws on names.
  /// </summary>
  internal static class ArchetypeRemovals
  {
    /// <summary>
    /// For each feature name or guid, finds its level in the class's progression
    /// and registers the removal at that level. Returns the configurator for chaining.
    /// </summary>
    internal static ArchetypeConfigurator AddRemovals(
      ArchetypeConfigurator archetype,
      BlueprintCharacterClass clazz,
      params string[] featureNames)
    {
      var progression = clazz.Progression;
      foreach (var featureName in featureNames)
      {
        var match = FindFeature(progression, featureName);
        if (match is null)
        {
          MissionWOTR.Main.Logger.Warn(
            $"[removals] {featureName} not found in {clazz.name} progression (checked feature " +
            "names and guids, dashed and undashed) - removal skipped. If this feature should " +
            "be removed, its blueprint id may not match the progression entry.");
          continue;
        }
        archetype = archetype.AddToRemoveFeatures(match.Value.Level, match.Value.Feature);
      }
      return archetype;
    }

    /// <summary>
    /// Delays features: removes each at the level the live progression
    /// actually grants it and re-adds the same feature the given number of
    /// levels later (the "wild shape functions at druid level - 2" style of
    /// trade). Features not present in the progression are skipped and
    /// logged.
    /// </summary>
    internal static ArchetypeConfigurator DelayFeatures(
      ArchetypeConfigurator archetype,
      BlueprintCharacterClass clazz,
      int byLevels,
      params string[] featureNames)
    {
      var progression = clazz.Progression;
      foreach (var featureName in featureNames)
      {
        var match = FindFeature(progression, featureName);
        if (match is null)
        {
          MissionWOTR.Main.Logger.Warn(
            $"[removals] {featureName} not found in {clazz.name} progression - delay skipped.");
          continue;
        }
        archetype = archetype
          .AddToRemoveFeatures(match.Value.Level, match.Value.Feature)
          .AddToAddFeatures(match.Value.Level + byLevels, match.Value.Feature);
        MissionWOTR.Main.Logger.Info(
          $"[removals] {clazz.name}: {featureName} delayed from level " +
          $"{match.Value.Level} to {match.Value.Level + byLevels}.");
      }
      return archetype;
    }

    /// <summary>
    /// Removes a feature at ONE specific level only - for selections the
    /// progression grants at many levels (e.g. the fighter's bonus feat at
    /// 1st and every even level), where AddRemovals would remove them all.
    /// The feature must actually appear at that level in the live
    /// progression, otherwise the removal is skipped with a warning.
    /// </summary>
    internal static ArchetypeConfigurator RemoveAtLevel(
      ArchetypeConfigurator archetype,
      BlueprintProgression progression,
      int level,
      params string[] featureNames)
    {
      foreach (var featureName in featureNames)
      {
        var match = FindFeature(progression, featureName);
        if (match is null || match.Value.Level != level)
        {
          MissionWOTR.Main.Logger.Warn(
            $"[removals] {featureName} not found at level {level} - removal skipped.");
          continue;
        }
        archetype = archetype.AddToRemoveFeatures(level, match.Value.Feature);
        MissionWOTR.Main.Logger.Info(
          $"[removals] {featureName} removed at level {level} only.");
      }
      return archetype;
    }

    /// <summary>
    /// Removes EVERY grant of each named feature across the whole
    /// progression - the sneak-attack style of trade, where the class
    /// re-grants the feature at many levels and every grant must go.
    /// Matching is by asset name or guid (dashed or undashed), EXACT OR
    /// SUBSTRING, so "SneakAttack" catches however the class names its
    /// sneak-attack grants. Pass onlyAtLevel to restrict to one level
    /// (the "one talent in the middle" trade). Returns the configurator
    /// for chaining; a name matching nothing is logged, not fatal.
    /// </summary>
    internal static ArchetypeConfigurator RemoveEveryGrant(
      ArchetypeConfigurator archetype,
      BlueprintCharacterClass clazz,
      string featureName,
      int? onlyAtLevel = null)
    {
      var progression = clazz.Progression;
      if (progression?.LevelEntries is null)
      {
        MissionWOTR.Main.Logger.Warn(
          $"[removals] {clazz.name} has no progression - '{featureName}' NOT removed.");
        return archetype;
      }
      string wanted = NormalizeGuid(featureName);
      int removed = 0;
      foreach (var entry in progression.LevelEntries)
      {
        if (entry is null || (onlyAtLevel.HasValue && entry.Level != onlyAtLevel.Value))
        {
          continue;
        }
        foreach (var feature in EntryFeatures(entry))
        {
          if (feature is null)
          {
            continue;
          }
          var name = (Read(feature, "name") as string) ?? "";
          var guid = NormalizeGuid(Read(feature, "AssetGuid")?.ToString());
          var matches =
            name.IndexOf(featureName, StringComparison.OrdinalIgnoreCase) >= 0 ||
            (wanted.Length > 0 && guid == wanted);
          if (!matches)
          {
            continue;
          }
          archetype = archetype.AddToRemoveFeatures(entry.Level, feature);
          removed++;
        }
      }
      if (removed == 0)
      {
        MissionWOTR.Main.Logger.Warn(
          $"[removals] RemoveEveryGrant: '{featureName}' matched no progression grant - nothing removed.");
      }
      else
      {
        MissionWOTR.Main.Logger.Info(
          $"[removals] RemoveEveryGrant: '{featureName}' removed at {removed} level(s).");
      }
      return archetype;
    }

    /// <summary>
    /// 0.53.0 — logs every progression grant whose asset name contains
    /// `contains` (level, name, guid). Added because the 0.52.1 in-game
    /// log showed removals silently skipped across 16 classes: the
    /// hard-coded GUIDs in the archetype files do not all match the live
    /// progression, and nothing in the log said what the real entries
    /// ARE. One playtest with this in place turns that log into the
    /// ground truth needed to fix the rest.
    /// </summary>
    internal static void DumpProgression(BlueprintCharacterClass clazz, string contains)
    {
      try
      {
        var progression = clazz?.Progression;
        if (progression?.LevelEntries is null)
        {
          MissionWOTR.Main.Logger.Warn(
            $"[removals] dump: {clazz?.name} has no progression.");
          return;
        }
        var hits = new List<string>();
        foreach (var entry in progression.LevelEntries)
        {
          if (entry is null)
          {
            continue;
          }
          foreach (var feature in EntryFeatures(entry))
          {
            if (feature is null)
            {
              continue;
            }
            var name = (Read(feature, "name") as string) ?? "";
            if (name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) < 0)
            {
              continue;
            }
            hits.Add($"L{entry.Level} {name} ({Read(feature, "AssetGuid")})");
          }
        }
        MissionWOTR.Main.Logger.Info(
          $"[removals] dump {clazz.name} matching '{contains}': " +
          (hits.Count == 0 ? "NO MATCHES" : string.Join(" | ", hits)));
      }
      catch (Exception e)
      {
        MissionWOTR.Main.Logger.Error($"[removals] dump failed for {clazz?.name}.", e);
      }
    }

    /// <summary>
    /// First level at which any of the named features appears in the
    /// progression (asset name or guid, dashed or not); 1 if none match -
    /// callers use it to place replacement features at the traded level.
    /// </summary>
    internal static int FindLevel(BlueprintProgression progression, params string[] featureNames)
    {
      foreach (var featureName in featureNames)
      {
        var match = FindFeature(progression, featureName);
        if (match is not null)
        {
          return match.Value.Level;
        }
      }
      return 1;
    }

    /// <summary>
    /// Removes a class's spellcasting: finds every progression feature whose
    /// AddSpellbook component grants the given spellbook and registers removals
    /// at the levels the progression actually grants them. Component-based on
    /// purpose - the feature's asset name is not needed (and names proved
    /// unreliable across builds).
    /// </summary>
    internal static ArchetypeConfigurator RemoveSpellcasting(
      ArchetypeConfigurator archetype,
      BlueprintCharacterClass clazz,
      Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook spellbook)
    {
      int removed = 0;
      var progression = clazz.Progression;
      if (progression?.LevelEntries is null)
      {
        MissionWOTR.Main.Logger.Warn(
          $"[removals] {clazz.name} has no progression - spellcasting NOT removed!");
        return archetype;
      }
      foreach (var entry in progression.LevelEntries)
      {
        if (entry is null)
        {
          continue;
        }
        foreach (var feature in EntryFeatures(entry))
        {
          if (feature?.ComponentsArray is null)
          {
            continue;
          }
          foreach (var component in feature.ComponentsArray)
          {
            // The AddSpellbook component's namespace moved between game
            // builds - matched by type NAME and read by reflection so this
            // compiles against any of them.
            if (component is null || component.GetType().Name != "AddSpellbook")
            {
              continue;
            }
            var book = Call(Read(component, "m_Spellbook"), "Get");
            if (book != spellbook)
            {
              continue;
            }
            archetype = archetype.AddToRemoveFeatures(entry.Level, feature);
            removed++;
            MissionWOTR.Main.Logger.Info(
              $"[removals] {clazz.name}: spellcasting feature {feature.name} " +
              $"({spellbook.name}) removed at level {entry.Level}.");
            break;
          }
        }
      }
      if (removed == 0)
      {
        MissionWOTR.Main.Logger.Warn(
          $"[removals] no feature granting {spellbook.name} found in {clazz.name} " +
          "progression - spellcasting NOT removed!");
      }
      return archetype;
    }

    /// <summary>
    /// The significant NERF, not the Skirmisher's removal: the class's
    /// spellcasting feature is removed at its native level and re-granted,
    /// whole, at reAddLevel (the Wildbond's price - no ranger spells until
    /// 12th level, then the vanilla spellbook). Same AddSpellbook scan as
    /// RemoveSpellcasting (type-name matched: the namespace moved between
    /// game builds).
    /// </summary>
    internal static ArchetypeConfigurator RemoveSpellcastingDelayed(
      ArchetypeConfigurator archetype,
      BlueprintCharacterClass clazz,
      Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook spellbook,
      int reAddLevel)
    {
      int handled = 0;
      var progression = clazz.Progression;
      if (progression?.LevelEntries is null)
      {
        MissionWOTR.Main.Logger.Warn(
          $"[removals] {clazz.name} has no progression - spellcasting NOT delayed!");
        return archetype;
      }
      foreach (var entry in progression.LevelEntries)
      {
        if (entry is null)
        {
          continue;
        }
        foreach (var feature in EntryFeatures(entry))
        {
          if (feature?.ComponentsArray is null)
          {
            continue;
          }
          foreach (var component in feature.ComponentsArray)
          {
            if (component is null || component.GetType().Name != "AddSpellbook")
            {
              continue;
            }
            var book = Call(Read(component, "m_Spellbook"), "Get");
            if (book != spellbook)
            {
              continue;
            }
            archetype = archetype.AddToRemoveFeatures(entry.Level, feature);
            archetype = archetype.AddToAddFeatures(reAddLevel, feature);
            handled++;
            MissionWOTR.Main.Logger.Info(
              $"[removals] {clazz.name}: spellcasting feature {feature.name} " +
              $"({spellbook.name}) delayed - removed at {entry.Level}, restored at {reAddLevel}.");
            break;
          }
        }
      }
      if (handled == 0)
      {
        MissionWOTR.Main.Logger.Warn(
          $"[removals] no feature granting {spellbook.name} found in {clazz.name} " +
          "progression - spellcasting NOT delayed!");
      }
      return archetype;
    }

    /// <summary>
    /// Removes a repeatedly-granted feature at every level EXCEPT its
    /// first N grants (the Chimera's price: eight of the witch's ten
    /// hex levels, keeping the first two). Features never granted are
    /// skipped and logged - same contract as AddRemovals.
    /// </summary>
    internal static ArchetypeConfigurator AddRemovalsExceptFirstN(
      ArchetypeConfigurator archetype,
      BlueprintCharacterClass clazz,
      string featureName,
      int keepFirst)
    {
      var progression = clazz.Progression;
      var matches = FindAllFeatures(progression, featureName);
      if (matches.Count == 0)
      {
        MissionWOTR.Main.Logger.Warn(
          $"[removals] {featureName} not found at any level of {clazz.name} progression - removal skipped.");
        return archetype;
      }
      int removed = 0;
      for (int i = Math.Max(0, keepFirst); i < matches.Count; i++)
      {
        archetype = archetype.AddToRemoveFeatures(matches[i].Level, matches[i].Feature);
        removed++;
      }
      MissionWOTR.Main.Logger.Info(
        $"[removals] {featureName}: {matches.Count} grants found, first {Math.Max(0, keepFirst)} kept, {removed} removed.");
      return archetype;
    }

    private static (int Level, BlueprintFeatureBase Feature)? FindFeature(
      BlueprintProgression progression,
      string featureName)
    {
      if (progression?.LevelEntries is null)
      {
        return null;
      }
      string wantedGuid = NormalizeGuid(featureName);
      foreach (var entry in progression.LevelEntries)
      {
        if (entry is null)
        {
          continue;
        }
        foreach (var feature in EntryFeatures(entry))
        {
          if (feature is null)
          {
            continue;
          }
          var name = Read(feature, "name") as string;
          if (name is not null &&
            string.Equals(name, featureName, StringComparison.OrdinalIgnoreCase))
          {
            return (entry.Level, feature);
          }
          var guid = NormalizeGuid(Read(feature, "AssetGuid")?.ToString());
          if (guid.Length > 0 && guid == wantedGuid)
          {
            return (entry.Level, feature);
          }
        }
      }
      return null;
    }

    /// <summary>
    /// Like FindFeature, but returns EVERY level that grants the feature -
    /// for selections the base class grants repeatedly (the ranger's
    /// favored enemy at 1/5/10/15/20, the Guide's reason to exist:
    /// removing only the first grant would leave the later ranks alive).
    /// </summary>
    private static List<(int Level, BlueprintFeatureBase Feature)> FindAllFeatures(
      BlueprintProgression progression,
      string featureName)
    {
      var matches = new List<(int Level, BlueprintFeatureBase Feature)>();
      if (progression?.LevelEntries is null)
      {
        return matches;
      }
      string wantedGuid = NormalizeGuid(featureName);
      foreach (var entry in progression.LevelEntries)
      {
        if (entry is null)
        {
          continue;
        }
        foreach (var feature in EntryFeatures(entry))
        {
          if (feature is null)
          {
            continue;
          }
          var name = Read(feature, "name") as string;
          if (name is not null &&
            string.Equals(name, featureName, StringComparison.OrdinalIgnoreCase))
          {
            matches.Add((entry.Level, feature));
            continue;
          }
          var guid = NormalizeGuid(Read(feature, "AssetGuid")?.ToString());
          if (guid.Length > 0 && guid == wantedGuid)
          {
            matches.Add((entry.Level, feature));
          }
        }
      }
      return matches;
    }

    /// <summary>
    /// Removes a feature at EVERY level the progression grants it (see
    /// FindAllFeatures). Features never granted are skipped and logged -
    /// same contract as AddRemovals.
    /// </summary>
    internal static ArchetypeConfigurator AddRemovalsAtAllLevels(
      ArchetypeConfigurator archetype,
      BlueprintCharacterClass clazz,
      params string[] featureNames)
    {
      var progression = clazz.Progression;
      foreach (var featureName in featureNames)
      {
        var matches = FindAllFeatures(progression, featureName);
        if (matches.Count == 0)
        {
          MissionWOTR.Main.Logger.Warn(
            $"[removals] {featureName} not found at any level of {clazz.name} progression - removal skipped.");
          continue;
        }
        foreach (var match in matches)
        {
          archetype = archetype.AddToRemoveFeatures(match.Level, match.Feature);
        }
        MissionWOTR.Main.Logger.Info(
          $"[removals] {featureName} removed at {matches.Count} level(s) of {clazz.name}.");
      }
      return archetype;
    }

    /// <summary>Strips dash/brace formatting and lowercases: guids compare equal
    /// in dashed, undashed, and braced spellings.</summary>
    private static string NormalizeGuid(string s)
    {
      return (s ?? string.Empty).Replace("-", "").Replace("{", "").Replace("}", "")
        .Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Dereferenced features of a LevelEntry. Handles both member spellings:
    /// the dereferenced "Features" list (blueprint objects) and the serialized
    /// "m_Features" list (references with a Get() method).
    /// </summary>
    private static IEnumerable<BlueprintFeatureBase> EntryFeatures(LevelEntry entry)
    {
      var list = Read(entry, "Features") ?? Read(entry, "m_Features");
      if (list is not IEnumerable items)
      {
        yield break;
      }
      foreach (var item in items)
      {
        if (item is null)
        {
          continue;
        }
        if (item is BlueprintFeatureBase direct)
        {
          yield return direct;
          continue;
        }
        if (Call(item, "Get") is BlueprintFeatureBase dereferenced)
        {
          yield return dereferenced;
        }
      }
    }

    private static object Read(object obj, string name)
    {
      if (obj is null)
      {
        return null;
      }
      const BindingFlags flags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
      var type = obj as Type ?? obj.GetType();
      try
      {
        return type.GetProperty(name, flags)?.GetValue(obj) ??
          type.GetField(name, flags)?.GetValue(obj);
      }
      catch
      {
        // Members that throw on read (e.g. lazy properties mid-load) are treated
        // as absent.
        return null;
      }
    }

    private static object Call(object obj, string methodName)
    {
      if (obj is null)
      {
        return null;
      }
      const BindingFlags flags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
      try
      {
        var method = (obj as Type ?? obj.GetType()).GetMethod(
          methodName, flags, null, Type.EmptyTypes, null);
        return method is null ? null : method.Invoke(obj, null);
      }
      catch
      {
        return null;
      }
    }
  }
}

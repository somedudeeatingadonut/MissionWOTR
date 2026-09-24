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

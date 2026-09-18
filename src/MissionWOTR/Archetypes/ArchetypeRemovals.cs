using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using System;
using System.Collections;
using System.Collections.Generic;

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
  /// </summary>
  internal static class ArchetypeRemovals
  {
    /// <summary>
    /// For each feature name, finds its level in the class's progression and
    /// registers the removal at that level. Returns the configurator for chaining.
    /// </summary>
    internal static ArchetypeConfigurator AddRemovals(
      ArchetypeConfigurator archetype,
      BlueprintCharacterClass clazz,
      params string[] featureNames)
    {
      var progression = clazz.Progression;
      foreach (var featureName in featureNames)
      {
        var level = FindFeatureLevel(progression, featureName);
        if (level is null)
        {
          MissionWOTR.Main.Logger.Warn(
            $"[removals] {featureName} not found in {clazz.name} progression - removal skipped " +
            "(the feature is not granted by the class; removing it would make the archetype " +
            "un-addable at char-gen).");
          continue;
        }
        archetype = archetype.AddToRemoveFeatures(level.Value, featureName);
      }
      return archetype;
    }

    private static int? FindFeatureLevel(
      Kingmaker.Blueprints.Classes.BlueprintProgression progression,
      string featureName)
    {
      if (progression?.LevelEntries is null)
      {
        return null;
      }
      foreach (var entry in progression.LevelEntries)
      {
        foreach (var feature in EntryFeatures(entry))
        {
          if (string.Equals(feature, featureName, StringComparison.Ordinal))
          {
            return entry.Level;
          }
        }
      }
      return null;
    }

    /// <summary>
    /// Dereferenced feature names of a LevelEntry. Reflection with both member
    /// spellings, because the game's LevelEntry layout varies by version.
    /// </summary>
    private static IEnumerable<string> EntryFeatures(LevelEntry entry)
    {
      var list = Get(entry, "Features") ?? Get(entry, "m_Features");
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
        var blueprint = Invoke(item, "Get") ?? item;
        var name = Get(blueprint, "name") as string;
        if (name is not null)
        {
          yield return name;
        }
      }
    }

    private static object Get(object obj, string name)
    {
      if (obj is null)
      {
        return null;
      }
      const BindingFlags flags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
      var type = obj as Type ?? obj.GetType();
      return type.GetProperty(name, flags)?.GetValue(obj) ??
        type.GetField(name, flags)?.GetValue(obj);
    }

    private static object Invoke(object obj, string methodName)
    {
      if (obj is null)
      {
        return null;
      }
      const BindingFlags flags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
      var method = (obj as Type ?? obj.GetType()).GetMethod(methodName, flags, null, Type.EmptyTypes, null);
      return method is null ? null : method.Invoke(obj, null);
    }
  }
}

using BlueprintCore.Blueprints.Configurators.Root;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using MissionWOTR.Feats;
using System;
using System.Linq;
using System.Reflection;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Puts a mod-created character class on the level-up menu.
  ///
  /// 0.53.0 FIX — the two prestige classes (the Holy Vindicator and the
  /// Bonewatch) were built correctly and logged "[holyvindicator]
  /// configured" / "[bonewatch] configured", yet neither appeared in
  /// game. Creating a `BlueprintCharacterClass` is not enough on its own:
  /// the level-up UI enumerates the classes listed on
  /// `BlueprintRoot.Progression`'s character-class array, and nothing ever
  /// put ours there. Both reference mods do exactly this registration —
  /// YLMstring/Prestige-Plus (`Blueprint/PrestigeClass/FakeAlignedClass.cs`,
  /// `AddtoMenu`) and Balkoth-dev/WOTR_MAKING_FRIENDS
  /// (`CharacterClass/SummonerClass.cs`) — and
  /// Vek17/TabletopTweaks-Core (`Utilities/ClassTools.cs`) confirms the
  /// array's shape: `Progression.m_CharacterClasses` is a
  /// `BlueprintCharacterClassReference[]`.
  ///
  /// The field is reached by reflection rather than by name because this
  /// project compiles against the stock game assembly (the csproj is
  /// explicit: "the mod's code only touches public game APIs"), while
  /// TabletopTweaks and friends build against a publicized one. Finding
  /// the field by its TYPE keeps this working either way and keeps it
  /// working if the name ever changes.
  ///
  /// Configured with `delayed: true`: BlueprintRoot is committed by
  /// `Main.StartGameLoader_Patch.LoadPackTOC` calling
  /// `RootConfigurator.ConfigureDelayedBlueprints()`, which runs after
  /// every class in ConfigureAll() has been built.
  ///
  /// Failure here is caught and logged rather than thrown — a class that
  /// cannot be registered must not take the rest of the mod's blueprints
  /// down with it.
  /// </summary>
  internal static class ClassRegistration
  {
    private const BindingFlags Flags =
      BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    internal static void AddToLevelUpList(BlueprintCharacterClass clazz)
    {
      if (clazz is null)
      {
        return;
      }
      try
      {
        RootConfigurator.For(RootRefs.BlueprintRoot)
          .ModifyProgression(progression => Append(progression, clazz))
          .Configure(delayed: true);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error(
          $"[classes] {clazz.name}: could not be registered for the level-up list.", e);
      }
    }

    private static void Append(object progression, BlueprintCharacterClass clazz)
    {
      try
      {
        var field = progression?.GetType().GetFields(Flags)
          .FirstOrDefault(f => f.FieldType == typeof(BlueprintCharacterClassReference[]));
        if (field is null)
        {
          MissionFeats.Logger.Warn(
            "[classes] no BlueprintCharacterClassReference[] field on the progression root - " +
            $"{clazz.name} will not appear in the level-up list. Fields present: " +
            string.Join(", ", (progression?.GetType().GetFields(Flags) ?? new FieldInfo[0])
              .Select(f => f.Name)));
          return;
        }
        var existing = field.GetValue(progression) as BlueprintCharacterClassReference[]
          ?? new BlueprintCharacterClassReference[0];
        var grown = new BlueprintCharacterClassReference[existing.Length + 1];
        Array.Copy(existing, grown, existing.Length);
        grown[existing.Length] = clazz.ToReference<BlueprintCharacterClassReference>();
        field.SetValue(progression, grown);
        MissionFeats.Logger.Info(
          $"[classes] {clazz.name}: registered for the level-up list " +
          $"({existing.Length} -> {grown.Length} classes).");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error(
          $"[classes] {clazz.name}: registration failed.", e);
      }
    }
  }
}

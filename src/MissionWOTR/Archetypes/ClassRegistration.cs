using BlueprintCore.Blueprints.Configurators.Root;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints.Classes;
using MissionWOTR.Feats;
using System;

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
  /// `BlueprintRoot.Progression.m_CharacterClasses`, and nothing ever put
  /// ours there. This is the same registration both reference mods use —
  /// YLMstring/Prestige-Plus
  /// (`Blueprint/PrestigeClass/FakeAlignedClass.cs`, `AddtoMenu`) and
  /// Balkoth-dev/WOTR_MAKING_FRIENDS (`CharacterClass/SummonerClass.cs`,
  /// `root.Progression.m_CharacterClasses = CommonTool.Append(...)`).
  ///
  /// Configured with `delayed: true`: BlueprintRoot is committed by
  /// `Main.StartGameLoader_Patch.LoadPackTOC` calling
  /// `RootConfigurator.ConfigureDelayedBlueprints()`, which runs after
  /// every class in ConfigureAll() has been built.
  ///
  /// Failure here is caught and logged rather than thrown — a class that
  /// cannot be registered should not take the rest of the mod's
  /// blueprints down with it.
  /// </summary>
  internal static class ClassRegistration
  {
    internal static void AddToLevelUpList(BlueprintCharacterClass clazz)
    {
      if (clazz is null)
      {
        return;
      }
      try
      {
        RootConfigurator.For(RootRefs.BlueprintRoot)
          .ModifyProgression(progression =>
          {
            var existing = progression.m_CharacterClasses ??
              new BlueprintCharacterClassReference[0];
            var grown = new BlueprintCharacterClassReference[existing.Length + 1];
            Array.Copy(existing, grown, existing.Length);
            grown[existing.Length] = clazz.ToReference<BlueprintCharacterClassReference>();
            progression.m_CharacterClasses = grown;
          })
          .Configure(delayed: true);
        MissionFeats.Logger.Info(
          $"[classes] {clazz.name}: registered for the level-up class list.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error(
          $"[classes] {clazz.name}: could not be registered for the level-up list.", e);
      }
    }
  }
}

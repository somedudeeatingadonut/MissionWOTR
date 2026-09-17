using BlueprintCore.Utils;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints.Classes;
using System.Linq;
using MissionWOTR.Archetypes;
using MissionWOTR.Mythics;
using System;

namespace MissionWOTR.Feats
{
  /// <summary>
  /// Entry point for all feat configuration. Each feat is configured in its own try/catch so a
  /// single broken feat can never hang or crash the game during blueprint loading.
  /// </summary>
  internal static class MissionFeats
  {
    internal static readonly LogWrapper Logger = LogWrapper.Get("MissionWOTR.Feats");

    internal static void ConfigureAll()
    {
      Logger.Info("Configuring MissionWOTR feats.");

      Configure(nameof(VengefulCounterstrike), VengefulCounterstrike.Configure);
      Configure(nameof(ArcaneMomentum), ArcaneMomentum.Configure);
      Configure(nameof(BattlefieldScavenger), BattlefieldScavenger.Configure);
      Configure(nameof(SecondWind), SecondWind.Configure);
      Configure(nameof(TauntingBlows), TauntingBlows.Configure);
      Configure(nameof(ResonantStrikes), ResonantStrikes.Configure);
      Configure(nameof(WardedSoul), WardedSoul.Configure);
      // Batch 2.
      Configure(nameof(SteadfastAim), SteadfastAim.Configure);
      Configure(nameof(GuardedMomentum), GuardedMomentum.Configure);
      Configure(nameof(TunnelFighter), TunnelFighter.Configure);

      // Mythic feats (tabletop ports) and mythic abilities (originals).
      Configure(nameof(AcrobaticMythic), AcrobaticMythic.Configure);
      Configure(nameof(PersuasiveMythic), PersuasiveMythic.Configure);
      Configure(nameof(MagicalAptitudeMythic), MagicalAptitudeMythic.Configure);
      Configure(nameof(IronWillMythic), IronWillMythic.Configure);
      Configure(nameof(LightningReflexesMythic), LightningReflexesMythic.Configure);
      Configure(nameof(EnduranceMythic), EnduranceMythic.Configure);
      Configure(nameof(Untouchable), Untouchable.Configure);
      Configure(nameof(SlayersVigor), SlayersVigor.Configure);
      Configure(nameof(AscendantEdge), AscendantEdge.Configure);
      Configure(nameof(LastStand), LastStand.Configure);
      Configure(nameof(DesperateFury), DesperateFury.Configure);
      Configure(nameof(DefiantSoul), DefiantSoul.Configure);
      Configure(nameof(RelentlessOnslaught), RelentlessOnslaught.Configure);
      Configure(nameof(AetherialBulwark), AetherialBulwark.Configure);
      Configure(nameof(TitansWrath), TitansWrath.Configure);
      Configure(nameof(TitanHide), TitanHide.Configure);

      // Class archetypes (level 1 test mode; see LevelPlan + docs/ARCHETYPES.md).
      Configure(nameof(EldritchPoisoner), EldritchPoisoner.Configure);
      Configure(nameof(ConstructCrafter), ConstructCrafter.Configure);

      // The archetype references the feats above, so it is configured last.
      Configure(nameof(MissionVanguard), MissionVanguard.Configure);

      LogArchetypeDiagnostics();
      Logger.Info("MissionWOTR feat configuration complete.");
    }

    /// <summary>
    /// Post-configure state dump for every class archetype: proves whether each one was
    /// created, landed on its class, and passes the availability filter the char-gen UI
    /// uses. Written to the game log so playtest reports are self-diagnosing.
    /// </summary>
    private static void LogArchetypeDiagnostics()
    {
      try
      {
        var alchemist = CharacterClassRefs.AlchemistClass.Reference.Get();
        var magus = CharacterClassRefs.MagusClass.Reference.Get();
        var entries = new (string Name, string Guid, BlueprintCharacterClass Class)[]
        {
          ("EldritchPoisoner", Guids.EldritchPoisonerArchetype, alchemist),
          ("ConstructCrafter", Guids.ConstructCrafterArchetype, alchemist),
          ("MissionVanguard", Guids.MissionVanguardArchetype, magus),
        };
        foreach (var entry in entries)
        {
          var archetype = BlueprintTool.Get<BlueprintArchetype>(entry.Guid);
          if (archetype is null)
          {
            Logger.Warn($"[diag] {entry.Name}: blueprint NOT created.");
            continue;
          }
          var inClass = entry.Class.Archetypes.Contains(archetype);
          var inAvailable = entry.Class.AvailableArchetypes.Contains(archetype);
          Logger.Info(
            $"[diag] {entry.Name}: created={true}, components={archetype.ComponentsArray.Length}, " +
            $"addLevels={(archetype.AddFeatures?.Length ?? 0)}, removeLevels={(archetype.RemoveFeatures?.Length ?? 0)}, " +
            $"minLevel={archetype.MinFeatureLevel}, onClass={inClass}, inAvailableList={inAvailable}.");
        }
        Logger.Info(
          $"[diag] alchemist archetypes={alchemist.Archetypes.Length} " +
          $"(available={alchemist.AvailableArchetypes.Length}), magus archetypes={magus.Archetypes.Length}.");
      }
      catch (Exception e)
      {
        Logger.Error("[diag] archetype diagnostics failed.", e);
      }
    }

    private static void Configure(string name, Action configure)
    {
      try
      {
        configure();
      }
      catch (Exception e)
      {
        Logger.Error($"Failed to configure feat: {name}", e);
      }
    }
  }
}

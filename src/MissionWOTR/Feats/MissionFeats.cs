using BlueprintCore.Utils;
using MissionWOTR.Archetypes;
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
      // The archetype references the feats above, so it is configured last.
      Configure(nameof(MissionVanguard), MissionVanguard.Configure);

      Logger.Info("MissionWOTR feat configuration complete.");
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

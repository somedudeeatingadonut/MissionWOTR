using BlueprintCore.Utils;
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

using System;
using TurnBased.Controllers;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.52.1 — one clock for every "once per round" gate in the mod.
  ///
  /// The alchemist-to-present sweep that produced this class found six
  /// features still reading `Game.Instance.TimeController.GameTime`
  /// directly: Hammerfist's Crushing Fist, Polearm Master's Step Aside,
  /// Riftstalker's mark delivery, Stormcaller's swift-action budget,
  /// Strategic Soldier's Punishing Strike and Venomblood's supernatural
  /// venom. That is the real-time clock. In turn-based combat the game
  /// does not advance it the way it advances rounds — it holds it while
  /// the turn-based controller waits on the player's commands — so a
  /// gate written as `LastUse + 1 round > now` never expires and the
  /// feature quietly degrades from "once per round" to "once per
  /// combat". MendingBlade had the same bug until 0.47.0; these six were
  /// simply never swept.
  ///
  /// The engine agrees that this clock is the wrong one in turn-based
  /// mode: its own `BuffCollection.AddBuffInternal` starts from
  /// `Game.Instance.TimeController.GameTime` and then replaces it with
  /// `Game.Instance.TurnBasedCombatController.TurnStartTime` whenever
  /// `CombatController.IsInTurnBasedCombat()` is true (reproduced in
  /// edoipi/TweakOrTreat, `TweakOrTreat/BuffTickFix.cs`, decompiled from
  /// the shipped game). `TurnStartTime` is the same clock MendingBlade's
  /// surge window has used since 0.47.0, so this class is that
  /// implementation lifted into one place rather than a new convention.
  ///
  /// Considered and rejected: `RoundStartTime`, the more literal round
  /// boundary. Its type is confirmed — `TimeSpan`, since the engine
  /// assigns it to the same variable it assigns `TurnStartTime` to — but
  /// nothing in the shipped game, and nothing in any reference mod
  /// (edoipi/TweakOrTreat, NosVladimir/KineticArchetypes,
  /// fl01/pathfinder-wotr-multiplayer, hsinyuhcan/KingmakerTurnBasedMod),
  /// demonstrates that it is stamped on every `StartRound`. If it is only
  /// stamped at combat start, every gate routed through it becomes
  /// once-per-combat — exactly the bug this class exists to remove.
  /// `TurnStartTime` is compile-proven in this repository (MendingBlade
  /// line 295 has built green since 0.47.0) and is the clock the engine
  /// itself reaches for.
  /// </summary>
  internal static class CombatTime
  {
    /// <summary>
    /// The combat clock the caller's round arithmetic should run on: the
    /// turn-based controller's stamp in turn-based combat, the game clock
    /// in real time. Both are `System.TimeSpan` values on the game's own
    /// timeline (the engine feeds either one to a buff's end time), so
    /// the existing `LastUse + 1.Rounds().Seconds` arithmetic at every
    /// call site stays valid in both modes.
    /// </summary>
    internal static TimeSpan Now()
    {
      return CombatController.IsInTurnBasedCombat()
        ? Kingmaker.Game.Instance.TurnBasedCombatController.TurnStartTime
        : Kingmaker.Game.Instance.TimeController.GameTime;
    }
  }
}

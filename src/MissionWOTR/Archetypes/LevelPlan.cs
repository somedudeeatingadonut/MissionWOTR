namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Master switch for archetype test mode.
  ///
  /// While TEST MODE is on, every class-archetype feature is granted at level 1 so the
  /// whole kit can be playtested from the first fight. Flip AllAtLevelOne to false and
  /// every LevelPlan.L(...) call below returns the real (tabletop) level instead - the
  /// intended levels are documented in each archetype file and in docs/ARCHETYPES.md.
  /// </summary>
  internal static class LevelPlan
  {
    internal const bool AllAtLevelOne = true;

    /// <summary>Maps an intended (tabletop) level to the level actually used.</summary>
    internal static int L(int intendedLevel)
    {
      return AllAtLevelOne ? 1 : intendedLevel;
    }

    /// <summary>
    /// The class-level gate for a level-gated OPTION inside a selection -
    /// the counterpart to L() for the other side of the same problem.
    ///
    /// 0.53.0 bug. In test mode every grant of a selection collapses to
    /// level 1, so an option carrying a real prerequisite is excluded from
    /// EVERY pick and can never be taken at all. The 0.52.1 in-game log
    /// proves it for Spirit-Ridden: "level-1 grants (4):
    /// SpiritRiddenSpiritSelection x4" - all four picks at level 1, and the
    /// four caster spirits (gated at shaman 6/12/18) absent from all four.
    /// Qinggong was worse: every one of its seven ki powers is gated at monk
    /// 8+, so all three ki picks were empty. Riftstalker likewise gated every
    /// rift command at hunter 4+.
    ///
    /// Test mode exists so a whole kit can be tried from the first fight, so
    /// the gate comes off with it and returns automatically when the mod is
    /// switched to normal leveling. Use this for prerequisites on options
    /// inside a multi-grant selection; keep using the real level for anything
    /// granted by a vanilla progression (Eldritch Poisoner's discoveries and
    /// Kineticist Explosion's infusion sit in vanilla selections whose picks
    /// still happen at real levels, so their gates must NOT be softened).
    /// </summary>
    internal static int Gate(int intendedLevel)
    {
      return AllAtLevelOne ? 1 : intendedLevel;
    }
  }
}

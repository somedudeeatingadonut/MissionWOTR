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
  }
}

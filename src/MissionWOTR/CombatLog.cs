using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Models.Log;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR
{
  /// <summary>
  /// Player-visible combat-log feedback for custom mechanics - the
  /// DarkCodex recipe (see docs/NOTES.md, "Techniques from other mods"):
  /// a CombatLogMessage pushed through LogThreadService's public
  /// HitDiceRestrictionLogThread (the m_Logs route NineSwords/ToyBox use
  /// does not exist in this game build - member drift; DarkCodex left
  /// that route commented out for the same reason). Best-effort by
  /// design: a failure is logged to the mod log and NEVER breaks the
  /// mechanic that called it.
  /// </summary>
  internal static class CombatLog
  {
    internal static void Write(string message, UnitEntityData source = null)
    {
      try
      {
        using (GameLogContext.Scope)
        {
          if (source is not null)
          {
            GameLogContext.SourceUnit = source;
          }
          LogThreadService.Instance.HitDiceRestrictionLogThread.AddMessage(
            new CombatLogMessage(message, GameLogStrings.Instance.DefaultColor,
              PrefixIcon.None, null, true));
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[combatlog] write failed.", e);
      }
    }
  }
}

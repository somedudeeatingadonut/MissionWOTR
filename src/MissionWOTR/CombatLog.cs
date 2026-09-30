using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Models.Log;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.Common;
using MissionWOTR.Feats;
using System;
using System.Linq;

namespace MissionWOTR
{
  /// <summary>
  /// Player-visible combat-log feedback for custom mechanics - the
  /// NineSwords recipe (see docs/NOTES.md, "Techniques from other mods"):
  /// open a GameLogContext scope, tag the source unit, and push a
  /// CombatLogMessage into the common MessageLogThread. Best-effort by
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
          var log = LogThreadService.Instance.m_Logs[LogChannelType.Common]
            .Last(x => x is MessageLogThread);
          log.AddMessage(new CombatLogMessage(message,
            GameLogStrings.Instance.DefaultColor, GameLogContext.GetIcon(),
            null, true));
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[combatlog] write failed.", e);
      }
    }
  }
}

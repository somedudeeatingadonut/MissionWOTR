using Kingmaker.UnitLogic.Mechanics.Actions;

namespace MissionWOTR
{
  /// <summary>
  /// 0.63.0 - base class for every hand-written ContextAction in the mod.
  ///
  /// BPCore's ActionsBuilder validates each action it is handed, and for Element types its
  /// ElementValidator checks that Element.name is set. ElementTool.Create&lt;T&gt;() and direct
  /// construction both leave it null, so every one of the mod's custom actions logged
  /// "Actions (ActionsBuilder) failed validation: * &lt;TypeName&gt;." at load - sixteen distinct
  /// names in the playtest log.
  ///
  /// The warnings were cosmetic: ActionsBuilder.Add() runs Validate() and then adds the action
  /// regardless, so nothing was actually dropped. But they were 16 lines of self-inflicted
  /// noise in a log that also carries real validation failures from other mods, which is the
  /// worst place to have noise.
  ///
  /// Setting the name here covers both creation patterns in one place - ElementTool.Create&lt;T&gt;()
  /// runs the constructor, and so does `new X { ... }`. The value follows the convention Owlcat
  /// itself uses when cloning an Element.
  /// </summary>
  // Public, not internal: four of the mod's action classes (SanguinePulse, SanguineBlastAction,
  // SpellbladeCreateAthame, SpellbladeThrowAthameAction) are declared public, and C# will not
  // let a public class derive from an internal one.
  public abstract class NamedContextAction : ContextAction
  {
    protected NamedContextAction()
    {
      name = GetType().Name;
    }
  }
}

using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;

namespace MissionWOTR.Mythics
{
  /// <summary>
  /// Sacred Vow (mythic ability - 0.24.0). The surviving shard of the
  /// Intercessor (0.23.0, withdrawn per the user's correction: a
  /// tabletop archetype was meant, not a homebrew one): "maybe keep the
  /// vow as a mythic ability if you can make it so that only oracles
  /// can take it."
  ///
  /// The gate is real: the ability carries a prerequisite of one oracle
  /// level (the in-repo AddPrerequisiteClassLevel idiom; prerequisites
  /// are respected by the mythic ability selection - TTT's Abundant*
  /// mythic abilities are prerequisite-gated the same way), so only
  /// oracles can take it.
  ///
  /// The vow itself, unchanged from its first life: Intercession (a
  /// swift action, close range, one ally, never herself) speaks the vow
  /// over a companion; while marked, HALF of the damage the companion
  /// takes is transferred to her - the ally healed the share, she
  /// taking it as DirectDamage (raw harm that no resistance or
  /// immunity can touch; the TTT DamageRetribution rule pair with a
  /// Reason-fact loop guard). A fallen vow-carrier carries nothing.
  /// At mythic rank 4 the vow widens: she may carry it for two
  /// companions at once (the in-repo Progression.MythicLevel read).
  ///
  /// Honesty notes (documented, not faked): the transfer resolves
  /// AFTER the marked ally's damage lands (RuleDealDamage DidTrigger);
  /// the mark registry is session-static (the Stormcaller budget
  /// precedent - after a save/reload the vow's memory is fresh, marks
  /// in excess of capacity persist until re-spoken or the ability is
  /// lost); a dying or dead carrier carries nothing.
  /// Log prefix: [sacredvow].
  /// </summary>
  internal static class SacredVow
  {
    // Wired during Configure; read by the components.
    internal static BlueprintBuff MarkBuff;

    public static void Configure()
    {
      var oracle = CharacterClassRefs.OracleClass.Reference.Get();

      MarkBuff = BuffConfigurator.New("SacredVowMarkBuff", Guids.SacredVowMarkBuff)
        .SetDisplayName("SacredVowMark.Name")
        .SetDescription("SacredVowMark.Description")
        .SetIcon(AbilityRefs.ShieldOfFaith.Reference.Get().Icon)
        .AddComponent(new SacredVowMark())
        .Configure();

      var intercede = ElementTool.Create<ContextActionSacredVowIntercede>();
      var intercession = AbilityConfigurator.New(
        "SacredVowIntercedeAbility", Guids.SacredVowIntercedeAbility)
        .SetDisplayName("SacredVowIntercession.Name")
        .SetDescription("SacredVowIntercession.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Close)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
        .SetCanTargetEnemies(false)
        .SetCanTargetFriends(true)
        .AddAbilityEffectRunAction(
          BlueprintCore.Actions.Builder.ActionsBuilder.New().Add(intercede).Build())
        .Configure();

      FeatureConfigurator.New("SacredVowFeature", Guids.SacredVowFeature)
        .SetDisplayName("SacredVow.Name")
        .SetDescription("SacredVow.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .SetIsClassFeature()
        // The user's gate: only oracles may take it.
        .AddPrerequisiteClassLevel(oracle, 1)
        .AddFacts(new() { intercession })
        .AddComponent(new SacredVowCleanup())
        // The SlayersVigor mythic-registration idiom: the mythic ability
        // pool plus Extra Mythic Ability.
        .AddToFeatureSelection(
          FeatureSelectionRefs.MythicAbilitySelection.Cast<BlueprintFeatureSelectionReference>(),
          FeatureSelectionRefs.ExtraMythicAbilityMythicFeat.Cast<BlueprintFeatureSelectionReference>())
        .Configure(delayed: true);

      MissionFeats.Logger.Info("SacredVow: configured.");
    }
  }

  /// <summary>
  /// Which companions carry her vow: keyed by her unit id, in speaking
  /// order. Session-static (documented in the header).
  /// </summary>
  internal static class SacredVowRegistry
  {
    internal static readonly Dictionary<string, List<UnitEntityData>> Marks = new();

    internal static List<UnitEntityData> List(string casterId)
    {
      if (!Marks.TryGetValue(casterId, out var list))
      {
        list = new List<UnitEntityData>();
        Marks[casterId] = list;
      }
      list.RemoveAll(u => u is null || u.Descriptor.State.IsDead ||
        u.Buffs.GetBuff(SacredVow.MarkBuff) is null);
      return list;
    }

    internal static void Add(string casterId, UnitEntityData ally)
    {
      var list = List(casterId);
      if (!list.Contains(ally))
      {
        list.Add(ally);
      }
    }

    internal static void Remove(string casterId, UnitEntityData ally)
    {
      if (Marks.TryGetValue(casterId, out var list))
      {
        list.RemoveAll(u => u == ally);
      }
    }

    internal static void Clear(string casterId)
    {
      if (Marks.TryGetValue(casterId, out var list))
      {
        foreach (var ally in list)
        {
          if (ally is not null && !ally.Descriptor.State.IsDead)
          {
            ally.Buffs.RemoveFact(SacredVow.MarkBuff);
          }
        }
      }
      Marks.Remove(casterId);
    }
  }

  /// <summary>
  /// When the ability is lost (mythic respec and the like), every mark
  /// she spoke falls with it.
  /// </summary>
  [TypeId(Guids.SacredVowCleanupComponent)]
  internal class SacredVowCleanup : UnitFactComponentDelegate
  {
    protected override void OnDeactivate()
    {
      try
      {
        SacredVowRegistry.Clear(Owner.UniqueId);
        MissionFeats.Logger.Info("[sacredvow] ability lost - marks cleared.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[sacredvow] cleanup failed.", e);
      }
    }
  }

  /// <summary>
  /// Intercession: speak the vow over one ally (a swift action). At
  /// capacity, the oldest mark passes on when a new one is spoken.
  /// </summary>
  [TypeId(Guids.SacredVowIntercedeAction)]
  internal class ContextActionSacredVowIntercede : ContextAction
  {
    public override string GetCaption() => "Intercession";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        var target = Target?.Unit ?? Context.MainTarget?.Unit;
        if (caster is null || target is null || target == caster || target.HPLeft <= 0)
        {
          return;
        }
        // Mythic rank 4 widens the vow to two companions (the
        // DesperateMeasures Progression.MythicLevel read).
        int capacity = caster.Descriptor.Progression.MythicLevel >= 4 ? 2 : 1;
        var marks = SacredVowRegistry.List(caster.UniqueId);
        while (marks.Count >= capacity && marks.Count > 0)
        {
          var oldest = marks[0];
          marks.RemoveAt(0);
          oldest.Buffs.RemoveFact(SacredVow.MarkBuff);
        }
        if (target.Buffs.GetBuff(SacredVow.MarkBuff) is null)
        {
          target.Descriptor.AddBuff(SacredVow.MarkBuff, Context, new TimeSpan?());
        }
        CombatLog.Write("She speaks the vow: her body answers for another.", caster);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[sacredvow] intercession failed.", e);
      }
    }
  }

  /// <summary>
  /// The Mark of the Vow, carried by the ally: half of the damage the
  /// ally takes crosses the bond - the ally healed the share, the
  /// carrier taking it as raw, unmitigable harm (the TTT
  /// DamageRetribution rule pair).
  /// </summary>
  [TypeId(Guids.SacredVowMarkComponent)]
  internal class SacredVowMark : UnitBuffComponentDelegate,
    ITargetRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    ITargetRulebookSubscriber, ISubscriber
  {
    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        if (evt.Target != Owner)
        {
          return; // only wounds that find the marked ally
        }
        if (evt.Reason?.Fact == Fact)
        {
          return; // our own transferred wound - the vow does not loop
        }
        var caster = Context.MaybeCaster;
        if (caster is null || caster.HPLeft <= 0 || caster.Descriptor.State.IsDead)
        {
          return; // a fallen vow-carrier carries nothing
        }
        int share = (int)(evt.Result * 0.5f);
        if (share <= 0)
        {
          return;
        }
        Game.Instance.Rulebook.TriggerEvent(
          new RuleHealDamage(caster, Owner, DiceFormula.Zero, bonus: share)
          {
            SourceFact = Fact,
          });
        Game.Instance.Rulebook.TriggerEvent(
          new RuleDealDamage(caster, caster,
            new DirectDamage(DiceFormula.Zero, share) { SourceFact = Fact })
          {
            Reason = new RuleReason(Fact),
          });
        CombatLog.Write("The vow answers: half the wound crosses the bond to her.", caster);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[sacredvow] mark transfer failed.", e);
      }
    }

    protected override void OnActivate()
    {
      try
      {
        var caster = Context.MaybeCaster;
        if (caster is not null)
        {
          SacredVowRegistry.Add(caster.UniqueId, Owner);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[sacredvow] mark registration failed.", e);
      }
    }

    protected override void OnDeactivate()
    {
      try
      {
        var caster = Context.MaybeCaster;
        if (caster is not null)
        {
          SacredVowRegistry.Remove(caster.UniqueId, Owner);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[sacredvow] mark unregistration failed.", e);
      }
    }
  }
}

using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.Configurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// The Intercessor (original homebrew oracle archetype - 0.23.0). The
  /// user's brief: "pick an oracle archetype and make it."
  ///
  /// Coverage check (run before designing, docs/COVERAGE.md): vanilla
  /// oracle archetypes are Seeker, Dual-Cursed, Enlightened Philosopher,
  /// Possessed and Divine Herbalist; the content mods add none (TTT-Base:
  /// channel fix + alternate capstone only; CharacterOptions+: none;
  /// DarkCodex: channel-ability patches; Kinetic Archetypes: kineticist
  /// only). The vanilla paladin Martyr shares the SACRIFICE fantasy but
  /// is stigmata + bardic performances - no wound-taking mechanics; the
  /// live wound-transfer (a Shield-Other that runs both ways) is
  /// unoccupied everywhere. The name collides with nothing (the vanilla
  /// "Wound-Bearer" is an item; "Martyr" a paladin archetype).
  ///
  /// The fantasy: some oracles speak for the gods. The intercessor
  /// speaks for the wounded. Her power is the oldest bargain in
  /// scripture - let the blow fall on me - and the more broken her
  /// body, the stronger her mercy.
  ///
  /// The kit:
  /// - The Vow (1st, free - the curse is already her price): the
  ///   Intercession ability (swift, close range, one ally, not herself)
  ///   applies the Mark of the Vow. While marked, 25% of the damage the
  ///   ally takes is transferred to her. The transfer is exact
  ///   conservation: the ally is healed the share and she takes it as
  ///   DirectDamage - raw harm that no resistance or immunity can touch,
  ///   because it is not damage crossing the bond, it is duty (the TTT
  ///   DamageRetribution rule pair: RuleHealDamage + RuleDealDamage with
  ///   a Reason-fact guard against loops).
  /// - Well of Wounds (3rd, trades the 3rd revelation): her healing on
  ///   OTHERS is empowered by her own broken body - +25% below 75% HP,
  ///   +50% below half, +75% below a quarter (the TTT
  ///   OutcomingAdditionalDamageAndHealingModifier idiom:
  ///   IInitiatorRulebookHandler&lt;RuleHealDamage&gt; + AddModifierBonus).
  /// - Death Refused (7th, trades the 7th revelation): once per rest
  ///   (a 1-charge rest-restoring resource), a blow that would take her
  ///   below 1 HP leaves her AT 1 HP instead - the exact heal-to-one of
  ///   COP's NineLives death save - plus a 3-round fast healing 5 surge.
  /// - The Redress (11th, trades the 11th revelation): the transfer
  ///   deepens to half, and she may carry the vow for two companions.
  /// - Saint of the Broken Body (15th, trades the 15th revelation): the
  ///   refusal charge is shared - when a marked companion would be
  ///   slain, the companion stands at 1 HP and the REST OF THE BLOW, all
  ///   of it, is hers. Because the charge is already spent, the wound
  ///   she chose cannot afterward be refused - not by special case but
  ///   by construction.
  /// - The Open Embrace (19th, trades the 19th revelation): the vow is
  ///   no longer spoken, it stands: every ally within 30 feet carries
  ///   the mark, refreshed each round (the SisterDragonAura tick idiom).
  ///
  /// The trades (hefty): ALL FIVE revelations (3/7/11/15/19). Her
  /// mystery still answers her prayers - bonus spells, curse, and the
  /// final revelation remain - but its wonders are no longer hers to
  /// invoke.
  ///
  /// Honesty notes (documented, not faked):
  /// - The transfer resolves AFTER the marked ally's damage lands
  ///   (RuleDealDamage DidTrigger): heal the share, deal the share -
  ///   exact conservation, but the ally's on-damage triggers see the
  ///   full wound first.
  /// - The transferred share is DirectDamage: her armor, resistances
  ///   and immunities do not apply. This is the vow's absoluteness, and
  ///   it is also the engine's honest limit (a mitigable transfer would
  ///   need a damage-reduction pass the rule pipe does not offer).
  /// - A dying or dead intercessor carries nothing: the mark goes quiet
  ///   until she stands again.
  /// - The refusal covers hit-point death only; a death that arrives by
  ///   other roads is not refused.
  /// - The mark-capacity registry is session-static (the Stormcaller
  ///   budget precedent): after a save/reload the vow's memory is
  ///   fresh - marks in excess of capacity persist until they are
  ///   re-spoken or the vow feature is lost.
  /// Log prefix: [intercessor].
  /// </summary>
  internal static class Intercessor
  {
    internal const string ArchetypeName = "IntercessorArchetype";

    // Wired during Configure; read by the components.
    internal static BlueprintBuff MarkBuff;
    internal static BlueprintBuff SurgeBuff;
    internal static BlueprintAbilityResource Charge;
    internal static BlueprintFeature VowFeature;
    internal static BlueprintFeature RedressFeature;
    internal static BlueprintFeature BrokenBodyFeature;

    public static void Configure()
    {
      var oracle = CharacterClassRefs.OracleClass.Reference.Get();

      // ----- The charge of Death Refused: one per rest -----
      Charge = AbilityResourceConfigurator.New(
        "IntercessorChargeResource", Guids.IntercessorChargeResource)
        .SetMaxAmount(ResourceAmountBuilder.New(1))
        .Configure();

      // ----- The Mark of the Vow: lives on the ally, carries the bond -----
      MarkBuff = BuffConfigurator.New("IntercessorMarkBuff", Guids.IntercessorMarkBuff)
        .SetDisplayName("IntercessorMark.Name")
        .SetDescription("IntercessorMark.Description")
        .SetIcon(AbilityRefs.ShieldOfFaith.Reference.Get().Icon)
        .AddComponent(new IntercessorMark())
        .Configure();

      // ----- The surge of a refused death -----
      SurgeBuff = BuffConfigurator.New("IntercessorSurgeBuff", Guids.IntercessorSurgeBuff)
        .SetDisplayName("IntercessorSurge.Name")
        .SetDescription("IntercessorSurge.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .AddEffectFastHealing(heal: 5)
        .Configure();

      // ----- Intercession: speak the vow over one ally -----
      var intercede = ElementTool.Create<ContextActionIntercede>();
      var intercession = AbilityConfigurator.New(
        "IntercessorIntercedeAbility", Guids.IntercessorIntercedeAbility)
        .SetDisplayName("Intercession.Name")
        .SetDescription("Intercession.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Close)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
        .SetCanTargetEnemies(false)
        .SetCanTargetFriends(true)
        .AddAbilityEffectRunAction(
          BlueprintCore.Actions.Builder.ActionsBuilder.New().Add(intercede).Build())
        .Configure();

      // ----- The Vow (1st) -----
      VowFeature = FeatureConfigurator.New("IntercessorVowFeature", Guids.IntercessorVowFeature)
        .SetDisplayName("IntercessorVow.Name")
        .SetDescription("IntercessorVow.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { intercession })
        .AddComponent(new IntercessorVowGrant())
        .Configure();

      // ----- Well of Wounds (3rd) -----
      var well = FeatureConfigurator.New(
        "IntercessorWellOfWoundsFeature", Guids.IntercessorWellOfWoundsFeature)
        .SetDisplayName("IntercessorWellOfWounds.Name")
        .SetDescription("IntercessorWellOfWounds.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new IntercessorWellOfWounds())
        .Configure();

      // ----- Death Refused (7th) -----
      var refused = FeatureConfigurator.New(
        "IntercessorDeathRefusedFeature", Guids.IntercessorDeathRefusedFeature)
        .SetDisplayName("IntercessorDeathRefused.Name")
        .SetDescription("IntercessorDeathRefused.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new IntercessorDeathRefusal())
        .AddAbilityResources(resource: Charge, restoreAmount: true)
        .Configure();

      // ----- The Redress (11th) -----
      RedressFeature = FeatureConfigurator.New(
        "IntercessorRedressFeature", Guids.IntercessorRedressFeature)
        .SetDisplayName("IntercessorRedress.Name")
        .SetDescription("IntercessorRedress.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      // ----- Saint of the Broken Body (15th) -----
      BrokenBodyFeature = FeatureConfigurator.New(
        "IntercessorBrokenBodyFeature", Guids.IntercessorBrokenBodyFeature)
        .SetDisplayName("IntercessorBrokenBody.Name")
        .SetDescription("IntercessorBrokenBody.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      // ----- The Open Embrace (19th) -----
      var embrace = FeatureConfigurator.New(
        "IntercessorOpenEmbraceFeature", Guids.IntercessorOpenEmbraceFeature)
        .SetDisplayName("IntercessorOpenEmbrace.Name")
        .SetDescription("IntercessorOpenEmbrace.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new IntercessorOpenEmbrace())
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.IntercessorArchetype, CharacterClassRefs.OracleClass)
          .SetLocalizedName("Intercessor.Name")
          .SetLocalizedDescription("Intercessor.Description")
          .AddToAddFeatures(LevelPlan.L(1), VowFeature)
          .AddToAddFeatures(LevelPlan.L(3), well)
          .AddToAddFeatures(LevelPlan.L(7), refused)
          .AddToAddFeatures(LevelPlan.L(11), RedressFeature)
          .AddToAddFeatures(LevelPlan.L(15), BrokenBodyFeature)
          .AddToAddFeatures(LevelPlan.L(19), embrace);

      // The hefty trade: every revelation of her mystery.
      foreach (var level in new[] { 3, 7, 11, 15, 19 })
      {
        archetype = ArchetypeRemovals.RemoveAtLevel(
          archetype, oracle.Progression, level, "OracleRevelationSelection");
      }

      archetype.Configure();

      MissionFeats.Logger.Info("Intercessor: configured.");
    }
  }

  /// <summary>
  /// Which companions carry her vow: keyed by her unit id, in speaking
  /// order (the front of the list is the oldest mark). Session-static -
  /// after a save/reload the registry is fresh, documented in the header.
  /// </summary>
  internal static class IntercessorRegistry
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
        u.Buffs.GetBuff(Intercessor.MarkBuff) is null);
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
            ally.Buffs.RemoveFact(Intercessor.MarkBuff);
          }
        }
      }
      Marks.Remove(casterId);
    }
  }

  /// <summary>
  /// The vow's grant-side bookkeeping: when the Vow feature is lost, every
  /// mark she spoke falls with it (the StormcallerSwiftGrant removal
  /// pattern).
  /// </summary>
  [TypeId(Guids.IntercessorVowGrantComponent)]
  internal class IntercessorVowGrant : UnitFactComponentDelegate
  {
    protected override void OnDeactivate()
    {
      try
      {
        IntercessorRegistry.Clear(Owner.UniqueId);
        MissionFeats.Logger.Info("[intercessor] vow lost - marks cleared.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[intercessor] vow cleanup failed.", e);
      }
    }
  }

  /// <summary>
  /// Intercession: speak the vow over one ally (a swift action). At
  /// capacity, the oldest mark passes on when a new one is spoken; with
  /// The Open Embrace the vow has no limit worth counting.
  /// </summary>
  [TypeId(Guids.IntercessorIntercedeAction)]
  internal class ContextActionIntercede : ContextAction
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
        int capacity =
          Intercessor.RedressFeature is not null && caster.HasFact(Intercessor.RedressFeature) ? 2 : 1;
        var marks = IntercessorRegistry.List(caster.UniqueId);
        while (marks.Count >= capacity && marks.Count > 0)
        {
          var oldest = marks[0];
          marks.RemoveAt(0);
          oldest.Buffs.RemoveFact(Intercessor.MarkBuff);
        }
        if (target.Buffs.GetBuff(Intercessor.MarkBuff) is null)
        {
          target.Descriptor.AddBuff(Intercessor.MarkBuff, Context, new TimeSpan?());
        }
        CombatLog.Write("She speaks the vow: her body answers for another.", caster);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[intercessor] intercession failed.", e);
      }
    }
  }

  /// <summary>
  /// The Mark of the Vow, carried by the ally. When the ally takes
  /// damage, her share of it crosses the bond: the ally is healed the
  /// share and she takes it as raw, unmitigable harm (the TTT
  /// DamageRetribution rule pair). From Saint of the Broken Body, the
  /// shared refusal charge also answers for the ally's death: the ally
  /// stands at 1 HP and the rest of the blow is hers.
  /// </summary>
  [TypeId(Guids.IntercessorMarkComponent)]
  internal class IntercessorMark : UnitBuffComponentDelegate,
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
          return; // a fallen intercessor carries nothing
        }

        // Saint of the Broken Body: the shared refusal answers for the
        // ally's death - the friend stands at the edge, the rest is hers.
        var brokenBody = Intercessor.BrokenBodyFeature is not null &&
          caster.HasFact(Intercessor.BrokenBodyFeature);
        if (Owner.HPLeft <= 0)
        {
          if (!brokenBody ||
            caster.Descriptor.Resources.GetResourceAmount(Intercessor.Charge) <= 0)
          {
            return; // the dead are beyond the vow; or the charge is spent
          }
          caster.Descriptor.Resources.Spend(Intercessor.Charge, 1);
          int remainder = 1 - Owner.HPLeft;
          Game.Instance.Rulebook.TriggerEvent(
            new RuleHealDamage(caster, Owner, DiceFormula.Zero, bonus: remainder)
            {
              SourceFact = Fact,
            });
          Game.Instance.Rulebook.TriggerEvent(
            new RuleDealDamage(caster, caster,
              new DirectDamage(DiceFormula.Zero, remainder) { SourceFact = Fact })
            {
              Reason = new RuleReason(Fact),
            });
          CombatLog.Write(
            "The vow refuses: the friend stands at the edge of death, and the rest of the blow is hers.",
            caster);
          return;
        }

        // The standing transfer: her share of every wound the ally takes.
        int percent =
          Intercessor.RedressFeature is not null && caster.HasFact(Intercessor.RedressFeature) ? 50 : 25;
        int share = (int)(evt.Result * (percent / 100f));
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
        CombatLog.Write(percent >= 50
          ? "The vow answers: half the wound crosses the bond to her."
          : "The vow answers: a quarter of the wound crosses the bond to her.", caster);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[intercessor] mark transfer failed.", e);
      }
    }

    protected override void OnActivate()
    {
      try
      {
        var caster = Context.MaybeCaster;
        if (caster is not null)
        {
          IntercessorRegistry.Add(caster.UniqueId, Owner);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[intercessor] mark registration failed.", e);
      }
    }

    protected override void OnDeactivate()
    {
      try
      {
        var caster = Context.MaybeCaster;
        if (caster is not null)
        {
          IntercessorRegistry.Remove(caster.UniqueId, Owner);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[intercessor] mark unregistration failed.", e);
      }
    }
  }

  /// <summary>
  /// Well of Wounds: her mercy deepens as her body fails. Her healing on
  /// OTHERS is empowered by her own broken body (+25% below 75% HP, +50%
  /// below half, +75% below a quarter) - the TTT
  /// OutcomingAdditionalDamageAndHealingModifier idiom.
  /// </summary>
  [TypeId(Guids.IntercessorWellOfWoundsComponent)]
  internal class IntercessorWellOfWounds : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleHealDamage>, IRulebookHandler<RuleHealDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public void OnEventAboutToTrigger(RuleHealDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.Target == Owner)
        {
          return; // her hands are for others, as the vow is
        }
        int max = Math.Max(1, Owner.Descriptor.MaxHP);
        int hp = Owner.HPLeft;
        float bonus;
        if (hp * 4 <= max)
        {
          bonus = 0.75f; // below a quarter
        }
        else if (hp * 2 <= max)
        {
          bonus = 0.5f; // below half
        }
        else if (hp * 4 <= max * 3)
        {
          bonus = 0.25f; // below three quarters
        }
        else
        {
          return; // her body is whole; her mercy is ordinary
        }
        evt.AddModifierBonus(bonus, Fact);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[intercessor] well of wounds failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleHealDamage evt) { }
  }

  /// <summary>
  /// Death Refused: once per rest, a blow that would take her below 1 HP
  /// leaves her AT 1 HP instead - the exact heal-to-one of COP's
  /// NineLives death save - with a 3-round fast healing 5 surge. The
  /// refusal covers hit-point death only.
  /// </summary>
  [TypeId(Guids.IntercessorDeathRefusalComponent)]
  internal class IntercessorDeathRefusal : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    ITargetRulebookSubscriber, ISubscriber
  {
    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        if (evt.Target != Owner || Owner.HPLeft > 0)
        {
          return; // not her, or not a killing blow
        }
        if (Owner.Resources.GetResourceAmount(Intercessor.Charge) <= 0)
        {
          return; // the charge is spent - the next death is real
        }
        Owner.Resources.Spend(Intercessor.Charge, 1);
        int toOne = 1 - Owner.HPLeft;
        Game.Instance.Rulebook.TriggerEvent(
          new RuleHealDamage(Owner, Owner, DiceFormula.Zero, bonus: toOne)
          {
            SourceFact = Fact,
          });
        Owner.AddBuff(Intercessor.SurgeBuff, Fact.MaybeContext, new Rounds(3).Seconds);
        CombatLog.Write("She refuses. The vow is spent, and she stands at the edge, unbowed.", Owner);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[intercessor] death refusal failed.", e);
      }
    }
  }

  /// <summary>
  /// The Open Embrace: the vow is no longer spoken, it stands. Every
  /// ally within 30 feet carries the Mark of the Vow, refreshed each
  /// round while she lives (the SisterDragonAura tick idiom - two
  /// rounds of natural duration, refreshed on the round tick, so a mark
  /// outlives her by at most a round).
  /// </summary>
  [TypeId(Guids.IntercessorOpenEmbraceComponent)]
  internal class IntercessorOpenEmbrace : UnitFactComponentDelegate, ITickEachRound
  {
    public void OnNewRound()
    {
      try
      {
        if (Intercessor.MarkBuff is null || Intercessor.VowFeature is null ||
          !Owner.HasFact(Intercessor.VowFeature))
        {
          return;
        }
        foreach (var ally in SanguineFont.AlliesWithin(Owner, 30))
        {
          if (ally.Buffs.GetBuff(Intercessor.MarkBuff) is null)
          {
            ally.Descriptor.AddBuff(
              Intercessor.MarkBuff, Fact.MaybeContext, new Rounds(2).Seconds);
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[intercessor] open embrace failed.", e);
      }
    }
  }
}

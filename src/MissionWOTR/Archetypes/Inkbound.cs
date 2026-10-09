using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.48.0 — the Inkbound (wizard homebrew, this mod's own design; the
  /// user handed me the wheel and asked for something unique). The
  /// design question I set myself: every caster resource in this game -
  /// slots, reservoirs, grit, burn - starts full or refills at rest.
  /// What if one started EMPTY and was earned by the one thing wizards
  /// already do every round: casting?
  ///
  /// THE CONCEPT: he does not memorize spells; the book memorizes him.
  /// An Inkbound is a wizard who made a covenant with a living
  /// grimoire - it lends him its pages, and every spell he casts leaves
  /// ink behind. He begins every fight with a blank book and writes his
  /// way to power: the longer the battle, the deeper the well.
  ///
  /// THE GAIN - Living Ink (1st): each wizard spell he casts in combat
  /// leaves one measure of ink in the book (cap 10; the meter is a
  /// fight-to-fight state - it does not persist through save/load: the
  /// book starts each session blank, which is flavor, not a bug).
  /// Quillwork - the ink is spent, never wasted:
  /// - Blot (1st, 1 ink, swift): a stain across the target's destiny -
  ///   -2 attacks, saves, and AC for 1 round. At 8th the ink darkens
  ///   (Iron-Gall): -3.
  /// - Wordwall (1st, 2 ink, swift): a wall of script before an ally -
  ///   +4 deflection AC for 1 round (+6 with Iron-Gall).
  /// - Recitation (5th, 3 ink, standard): the book screams a stolen
  ///   syllable - sonic damage to one enemy (5 + wizard level).
  /// - Vellum Skin (16th): the book writes ON him - +2 natural armor,
  ///   always.
  /// - The Last Chapter (20th, 1/day, swift): the book finishes
  ///   itself - the ink floods back to full.
  ///
  /// THE TRADE (live-progression scans): the arcane bond (every grant -
  /// he can't take a familiar; the book would be jealous) and the
  /// wizard bonus feats at 10th, 15th, and 20th (1st and 5th remain -
  /// the quillwork line is actives, not passives, so the tax is
  /// lighter than the Undead Master's).
  ///
  /// ENGINE NOTES: the cast-watcher is the Overchanneler's RuleCastSpell
  /// rider verbatim; the ink meter is a static per-wizard well (the
  /// engine's resource API has no verified gain call, so the meter is
  /// plain C# keyed by unit - fight-scoped by design and documented as
  /// such); quillwork costs are paid inside the actions (the
  /// CrescendoSkald manual-spend idiom); Recitation is the Mending
  /// Blade's RuleDealDamage + EnergyDamage pattern (Sonic is a probe-
  /// verified DamageEnergyType); the buff bonuses are TunnelFighter's
  /// AddContextStatBonus pattern; Blot/Wordwall upgrade switching by
  /// HasFact is the withdrawn Champion's smite idiom.
  /// </summary>
  internal class Inkbound
  {
    internal const string ArchetypeName = "Inkbound";

    internal static void Configure()
    {
      var wizard = CharacterClassRefs.WizardClass.Reference.Get();
      var kitIcon = AbilityRefs.FalseLife.Reference.Get().Icon;

      // ----- Iron-Gall (8th): the flag the actions read. -----
      var ironGall = FeatureConfigurator.New("InkboundIronGallFeature", Guids.InkboundIronGallFeature)
        .SetDisplayName("InkboundIronGall.Name")
        .SetDescription("InkboundIronGall.Description")
        .SetIcon(kitIcon)
        .SetIsClassFeature()
        .Configure();

      // ----- The quillwork buffs. -----
      var blot = BuffConfigurator.New("InkboundBlotBuff", Guids.InkboundBlotBuff)
        .SetDisplayName("InkboundBlot.Buff.Name")
        .SetDescription("InkboundBlot.Buff.Description")
        .SetIcon(AbilityRefs.Blindness.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddContextStatBonus(StatType.AdditionalAttackBonus, ContextValues.Constant(-2), ModifierDescriptor.Penalty)
        .AddContextStatBonus(StatType.SaveFortitude, ContextValues.Constant(-2), ModifierDescriptor.Penalty)
        .AddContextStatBonus(StatType.SaveWill, ContextValues.Constant(-2), ModifierDescriptor.Penalty)
        .AddContextStatBonus(StatType.SaveReflex, ContextValues.Constant(-2), ModifierDescriptor.Penalty)
        .AddContextStatBonus(StatType.AC, ContextValues.Constant(-2), ModifierDescriptor.Penalty)
        .Configure();
      var blotIron = BuffConfigurator.New("InkboundBlotIronBuff", Guids.InkboundBlotIronBuff)
        .SetDisplayName("InkboundBlot.Buff.Name")
        .SetDescription("InkboundBlot.Buff.Description")
        .SetIcon(AbilityRefs.Blindness.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddContextStatBonus(StatType.AdditionalAttackBonus, ContextValues.Constant(-3), ModifierDescriptor.Penalty)
        .AddContextStatBonus(StatType.SaveFortitude, ContextValues.Constant(-3), ModifierDescriptor.Penalty)
        .AddContextStatBonus(StatType.SaveWill, ContextValues.Constant(-3), ModifierDescriptor.Penalty)
        .AddContextStatBonus(StatType.SaveReflex, ContextValues.Constant(-3), ModifierDescriptor.Penalty)
        .AddContextStatBonus(StatType.AC, ContextValues.Constant(-3), ModifierDescriptor.Penalty)
        .Configure();
      var wordwall = BuffConfigurator.New("InkboundWordwallBuff", Guids.InkboundWordwallBuff)
        .SetDisplayName("InkboundWordwall.Buff.Name")
        .SetDescription("InkboundWordwall.Buff.Description")
        .SetIcon(AbilityRefs.MageArmor.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddContextStatBonus(StatType.AC, ContextValues.Constant(4), ModifierDescriptor.Deflection)
        .Configure();
      var wordwallIron = BuffConfigurator.New("InkboundWordwallIronBuff", Guids.InkboundWordwallIronBuff)
        .SetDisplayName("InkboundWordwall.Buff.Name")
        .SetDescription("InkboundWordwall.Buff.Description")
        .SetIcon(AbilityRefs.MageArmor.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddContextStatBonus(StatType.AC, ContextValues.Constant(6), ModifierDescriptor.Deflection)
        .Configure();

      // ----- The quillwork abilities. -----
      var blotAbility = AbilityConfigurator.New("InkboundBlotAbility", Guids.InkboundBlotAbility)
        .SetDisplayName("InkboundBlot.Name")
        .SetDescription("InkboundBlot.Description")
        .SetIcon(AbilityRefs.Blindness.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(UnitCommand.CommandType.Swift)
        .SetCanTargetEnemies()
        .AddComponent(new InkboundInkRestriction { Cost = 1 })
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(new InkboundQuillAction
        {
          QuillMode = InkboundQuillAction.Mode.Blot,
          Cost = 1,
          BlotBuff = blot,
          BlotBuffIron = blotIron,
          WordwallBuff = wordwall,
          WordwallBuffIron = wordwallIron,
          IronGallFeature = ironGall,
        }).Build())
        .Configure();
      var wordwallAbility = AbilityConfigurator.New("InkboundWordwallAbility", Guids.InkboundWordwallAbility)
        .SetDisplayName("InkboundWordwall.Name")
        .SetDescription("InkboundWordwall.Description")
        .SetIcon(AbilityRefs.MageArmor.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(UnitCommand.CommandType.Swift)
        .SetCanTargetFriends(true)
        .SetCanTargetSelf(true)
        .AddComponent(new InkboundInkRestriction { Cost = 2 })
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(new InkboundQuillAction
        {
          QuillMode = InkboundQuillAction.Mode.Wordwall,
          Cost = 2,
          BlotBuff = blot,
          BlotBuffIron = blotIron,
          WordwallBuff = wordwall,
          WordwallBuffIron = wordwallIron,
          IronGallFeature = ironGall,
        }).Build())
        .Configure();
      var recitationAbility = AbilityConfigurator.New("InkboundRecitationAbility", Guids.InkboundRecitationAbility)
        .SetDisplayName("InkboundRecitation.Name")
        .SetDescription("InkboundRecitation.Description")
        .SetIcon(AbilityRefs.Shout.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetEnemies()
        .AddComponent(new InkboundInkRestriction { Cost = 3 })
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(new InkboundQuillAction
        {
          QuillMode = InkboundQuillAction.Mode.Recitation,
          Cost = 3,
          WizardClass = wizard,
        }).Build())
        .Configure();

      // ----- The kit (1st): the well + the first two quills. -----
      var kit = FeatureConfigurator.New("InkboundKitFeature", Guids.InkboundKitFeature)
        .SetDisplayName("InkboundInk.Name")
        .SetDescription("InkboundInk.Description")
        .SetIcon(kitIcon)
        .SetIsClassFeature()
        .AddComponent(new InkboundInkRider { WizardClass = wizard })
        .AddFacts(new() { blotAbility, wordwallAbility })
        .Configure();

      // ----- Recitation (5th). -----
      var recitation = FeatureConfigurator.New("InkboundRecitationFeature", Guids.InkboundRecitationFeature)
        .SetDisplayName("InkboundRecitation.Name")
        .SetDescription("InkboundRecitation.Description")
        .SetIcon(AbilityRefs.Shout.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { recitationAbility })
        .Configure();

      // ----- Vellum Skin (16th). -----
      var vellum = FeatureConfigurator.New("InkboundVellumFeature", Guids.InkboundVellumFeature)
        .SetDisplayName("InkboundVellum.Name")
        .SetDescription("InkboundVellum.Description")
        .SetIcon(kitIcon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.AC, value: 2, descriptor: ModifierDescriptor.NaturalArmor)
        .Configure();

      // ----- The Last Chapter (20th): 1/day, the ink floods back. -----
      var lastChapterPool = AbilityResourceConfigurator.New(
        "InkboundLastChapterResource", Guids.InkboundLastChapterResource)
        .SetMax(1)
        .Configure();
      var lastChapterAbility = AbilityConfigurator.New(
        "InkboundLastChapterAbility", Guids.InkboundLastChapterAbility)
        .SetDisplayName("InkboundLastChapter.Name")
        .SetDescription("InkboundLastChapter.Description")
        .SetIcon(kitIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Swift)
        .SetCanTargetSelf()
        .AddAbilityResourceLogic(
          requiredResource: lastChapterPool, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(new InkboundLastChapterAction { WizardClass = wizard }).Build())
        .Configure();
      var lastChapter = FeatureConfigurator.New(
        "InkboundLastChapterFeature", Guids.InkboundLastChapterFeature)
        .SetDisplayName("InkboundLastChapter.Name")
        .SetDescription("InkboundLastChapter.Description")
        .SetIcon(kitIcon)
        .SetIsClassFeature()
        .AddFacts(new() { lastChapterAbility })
        .AddAbilityResources(resource: lastChapterPool, restoreAmount: true)
        .Configure();

      // ----- The archetype. -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.InkboundArchetype, CharacterClassRefs.WizardClass)
          .SetLocalizedName("Inkbound.Name")
          .SetLocalizedDescription("Inkbound.Description")
          .AddToAddFeatures(LevelPlan.L(1), kit)
          .AddToAddFeatures(LevelPlan.L(5), recitation)
          .AddToAddFeatures(LevelPlan.L(8), ironGall)
          .AddToAddFeatures(LevelPlan.L(16), vellum)
          .AddToAddFeatures(LevelPlan.L(20), lastChapter);
      // The trades: the bond (the book would be jealous) and the bonus
      // feats at 10th/15th/20th (1st and 5th remain).
      archetype = ArchetypeRemovals.RemoveEveryGrant(
        archetype, wizard, FeatureSelectionRefs.ArcaneBondSelection.ToString());
      foreach (var level in new[] { 10, 15, 20 })
      {
        archetype = ArchetypeRemovals.RemoveAtLevel(
          archetype, wizard.Progression, level,
          FeatureSelectionRefs.WizardFeatSelection.ToString());
      }
      archetype.Configure();
      MissionFeats.Logger.Info("[inkbound] configured: " + ArchetypeName + ".");
    }
  }

  /// <summary>
  /// The ink well: a static, per-wizard, fight-scoped meter. The
  /// engine's resource API has no verified gain call (mods only ever
  /// Spend), so the meter is plain C# keyed by unit. It does not
  /// persist through save/load - the book starts each session blank,
  /// which is documented flavor, not an accident.
  /// </summary>
  internal static class InkboundInk
  {
    /// <summary>
    /// 0.53.0 - the book holds more ink the longer the wizard has been
    /// writing in it: 10 measures at 1st level, +2 every five wizard levels
    /// (12 at 5th, 14 at 10th, 16 at 15th, 18 at 20th).
    /// </summary>
    internal static int CapFor(int level)
    {
      return 10 + 2 * (level / 5);
    }

    private static readonly Dictionary<UnitEntityData, int> Well = new();

    internal static int Of(UnitEntityData unit)
    {
      return Well.TryGetValue(unit, out var value) ? value : 0;
    }

    internal static void Gain(UnitEntityData unit, int cap)
    {
      var value = Of(unit);
      if (value < cap)
      {
        Well[unit] = value + 1;
      }
    }

    internal static bool TrySpend(UnitEntityData unit, int cost)
    {
      var value = Of(unit);
      if (value < cost)
      {
        return false;
      }
      Well[unit] = value - cost;
      return true;
    }

    internal static void Fill(UnitEntityData unit, int cap)
    {
      Well[unit] = cap;
    }

    internal static void Drain(UnitEntityData unit)
    {
      Well.Remove(unit);
    }
  }

  /// <summary>
  /// Living Ink: each wizard spell cast in combat leaves one measure of
  /// ink in the book (the Overchanneler's RuleCastSpell watcher,
  /// watching for a different price).
  /// </summary>
  [TypeId(Guids.InkboundInkRider)]
  internal class InkboundInkRider : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCastSpell>, IRulebookHandler<RuleCastSpell>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass WizardClass;

    public void OnEventAboutToTrigger(RuleCastSpell evt) { }

    public void OnEventDidTrigger(RuleCastSpell evt)
    {
      try
      {
        if (evt.Initiator != Owner ||
          evt.Spell?.Blueprint?.Type != AbilityType.Spell ||
          !Owner.IsInCombat)
        {
          return;
        }
        var level = WizardClass is null
          ? 1
          : Owner.Progression.GetClassLevel(WizardClass);
        InkboundInk.Gain(Owner, InkboundInk.CapFor(level));
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[inkbound] ink gain failed.", e);
      }
    }

    protected override void OnDeactivate()
    {
      // The well drains when the covenant leaves the unit.
      InkboundInk.Drain(Owner);
    }
  }

  /// <summary>
  /// One action, three quills. The ink is paid up front (the
  /// CrescendoSkald manual-spend idiom); the mode picks the effect.
  /// Iron-Gall (8th) darkens Blot and thickens Wordwall - the switch
  /// is the withdrawn Champion's HasFact idiom.
  /// </summary>
  [TypeId(Guids.InkboundQuillAction)]
  internal class InkboundQuillAction : NamedContextAction
  {
    internal enum Mode
    {
      Blot,
      Wordwall,
      Recitation,
    }

    public Mode QuillMode;
    public int Cost;
    public BlueprintBuff BlotBuff;
    public BlueprintBuff BlotBuffIron;
    public BlueprintBuff WordwallBuff;
    public BlueprintBuff WordwallBuffIron;
    public BlueprintFeature IronGallFeature;
    public BlueprintCharacterClass WizardClass;

    public override void RunAction()
    {
      try
      {
        var caster = Context?.MaybeCaster;
        var target = Target?.Unit;
        if (caster is null || target is null)
        {
          return;
        }
        if (!InkboundInk.TrySpend(caster, Cost))
        {
          MissionFeats.Logger.Warn(
            $"[inkbound] not enough ink for {QuillMode} ({InkboundInk.Of(caster)}/{Cost}).");
          return;
        }
        var iron = IronGallFeature is not null && caster.HasFact(IronGallFeature);
        switch (QuillMode)
        {
          case Mode.Blot:
            target.AddBuff(iron ? BlotBuffIron : BlotBuff, Context, TimeSpan.FromSeconds(6));
            break;
          case Mode.Wordwall:
            target.AddBuff(iron ? WordwallBuffIron : WordwallBuff, Context, TimeSpan.FromSeconds(6));
            break;
          case Mode.Recitation:
            var level = caster.Descriptor.Progression.GetClassLevel(WizardClass);
            // 0.53.0 - it was 5 + wizard level, which at the level it
            // arrives (5th) is less than a 1st-level spell, for a standard
            // action and three measures of ink. It now scales at two per
            // level, and from 11th the scream carries a stain: the Blot the
            // target would otherwise cost another measure to lay.
            var bundle = new DamageBundle();
            bundle.Add(new EnergyDamage(
              DiceFormula.Zero, 5 + 2 * level, DamageEnergyType.Sonic));
            Rulebook.Trigger(new RuleDealDamage(caster, target, bundle));
            if (level >= 11)
            {
              var stain = iron ? BlotBuffIron : BlotBuff;
              if (stain is not null)
              {
                target.AddBuff(stain, Context, TimeSpan.FromSeconds(6));
              }
            }
            break;
        }
        MissionFeats.Logger.Info($"[inkbound] {QuillMode}: {Cost} ink spent.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[inkbound] quillwork failed.", e);
      }
    }

    public override string GetCaption() => QuillMode.ToString();
  }

  /// <summary>
  /// The Last Chapter: the book finishes itself - the ink floods back
  /// to full.
  /// </summary>
  [TypeId(Guids.InkboundLastChapterAction)]
  internal class InkboundLastChapterAction : NamedContextAction
  {
    public BlueprintCharacterClass WizardClass;

    public override void RunAction()
    {
      try
      {
        var caster = Context?.MaybeCaster;
        if (caster is null)
        {
          return;
        }
        var level = WizardClass is null
          ? 1
          : caster.Descriptor.Progression.GetClassLevel(WizardClass);
        InkboundInk.Fill(caster, InkboundInk.CapFor(level));
        MissionFeats.Logger.Info("[inkbound] the last chapter: the ink floods back.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[inkbound] the last chapter failed.", e);
      }
    }

    public override string GetCaption() => "The Last Chapter";
  }

  /// <summary>
  /// The ink gate (0.52.0 bug-hunt fix): quillwork abilities are
  /// UNUSABLE without enough ink in the well. Previously the action
  /// was consumed and only a log line explained why - now the button
  /// refuses.
  /// </summary>
  [TypeId(Guids.InkboundInkRestriction)]
  internal class InkboundInkRestriction : BlueprintComponent, IAbilityCasterRestriction
  {
    public int Cost;

    public bool IsCasterRestrictionPassed(UnitEntityData caster)
    {
      return caster is not null && InkboundInk.Of(caster) >= Cost;
    }

    public string GetAbilityCasterRestrictionUIText()
    {
      return "Not enough ink in the book";
    }
  }
}

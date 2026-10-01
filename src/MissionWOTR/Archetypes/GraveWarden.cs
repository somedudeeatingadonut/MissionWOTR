using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.41.0 — the Grave Warden (slayer archetype, Advanced Class Guide
  /// pg. 120). The user's requested tabletop port (the Vanguard was the
  /// trap question — already in the game; this is the real one).
  ///
  /// "While paladins and inquisitors use their connection with the divine
  /// to fight undead hordes and other horrors of the night, a grave
  /// warden relies on knowledge, skill with weapons, and tenacity to put
  /// an end to these night-born terrors." A natural fit for the
  /// Worldwound's ghouls, wights, and walking dead.
  ///
  /// THE TRADES (found in the live progression by scan):
  /// - the 2nd-level slayer talent (Holy Water Sprinkler);
  /// - stalker (the Death Ward ritual);
  /// - the 10th-level talent (Dustbringer).
  ///
  /// THE GAIN:
  /// - Blessed Edge (2nd): a swift action anoints the warden's weapon
  ///   for one round; weapon damage against UNDEAD carries +2d6 holy
  ///   damage — a direct hit of holy water, minus the flask bookkeeping.
  /// - Death Ward Ritual (7th): a standard action grants himself the
  ///   vanilla death ward buff for 10 rounds per slayer level (the
  ///   tabletop's 1 minute/level at CL = level; the flask cost and the
  ///   1-minute ritual are the documented cut).
  /// - Dustbringer (10th): a swift action marks an undead foe for one
  ///   round. If the warden's next attack hits, the mark resolves: Will
  ///   save (DC 10 + half slayer level + Intelligence) or the undead is
  ///   DESTROYED (delivered as overwhelming damage through the engine's
  ///   own RuleDealDamage — the Breaker rider idiom). A target that
  ///   saves is immune to this warden's Dustbringer for 24 hours.
  ///
  /// DOCUMENTED CUTS (the low-cut port rule): holy-water flask economy
  /// and the 1-minute ritual time (both out-of-combat bookkeeping); the
  /// "assassinate" lineage is implemented directly rather than calling
  /// an advanced talent (the WOTR advanced-talent list does not expose
  /// it as a reusable piece).
  /// </summary>
  internal class GraveWarden
  {
    internal const string ArchetypeName = "GraveWarden";

    internal static void Configure()
    {
      var slayer = CharacterClassRefs.SlayerClass.Reference.Get();
      var blessedIcon = AbilityRefs.Bless.Reference.Get().Icon;
      var wardIcon = AbilityRefs.DeathWard.Reference.Get().Icon;
      var dustIcon = AbilityRefs.UndeathToDeath.Reference.Get().Icon;

      // ----- Blessed Edge (2nd): the rider buff. -----
      var blessedEdge = BuffConfigurator.New(
        "GraveWardenBlessedEdgeBuff", Guids.GraveWardenBlessedEdgeBuff)
        .SetDisplayName("GraveWardenBlessedEdge.Name")
        .SetDescription("GraveWardenBlessedEdge.Description")
        .SetIcon(blessedIcon)
        .SetIsClassFeature()
        .AddComponent(new GraveWardenBlessedEdgeRider())
        .Configure();
      var blessedAbility = AbilityConfigurator.New(
        "GraveWardenBlessedEdgeAbility", Guids.GraveWardenBlessedEdgeAbility)
        .SetDisplayName("GraveWardenBlessedEdge.Name")
        .SetDescription("GraveWardenBlessedEdge.Description")
        .SetIcon(blessedIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Swift)
        .SetCanTargetSelf()
        .AddAbilityEffectRunAction(ActionsBuilder.New()
          .ApplyBuff(blessedEdge, ContextDuration.Fixed(1))
          .Build())
        .Configure();
      var blessedFeature = FeatureConfigurator.New(
        "GraveWardenBlessedEdgeFeature", Guids.GraveWardenBlessedEdgeFeature)
        .SetDisplayName("GraveWardenBlessedEdge.Name")
        .SetDescription("GraveWardenBlessedEdge.Description")
        .SetIcon(blessedIcon)
        .SetIsClassFeature()
        .AddFacts(new() { blessedAbility })
        .Configure();

      // ----- Death Ward Ritual (7th). -----
      var wardAction = new GraveWardenDeathWardAction
      {
        WardBuff = BuffRefs.DeathWardBuff.Reference.Get(),
        SlayerClass = slayer,
      };
      var wardAbility = AbilityConfigurator.New(
        "GraveWardenDeathWardAbility", Guids.GraveWardenDeathWardAbility)
        .SetDisplayName("GraveWardenDeathWard.Name")
        .SetDescription("GraveWardenDeathWard.Description")
        .SetIcon(wardIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetSelf()
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(wardAction).Build())
        .Configure();
      var wardFeature = FeatureConfigurator.New(
        "GraveWardenDeathWardFeature", Guids.GraveWardenDeathWardFeature)
        .SetDisplayName("GraveWardenDeathWard.Name")
        .SetDescription("GraveWardenDeathWard.Description")
        .SetIcon(wardIcon)
        .SetIsClassFeature()
        .AddFacts(new() { wardAbility })
        .Configure();

      // ----- Dustbringer (10th): the mark, its rider, and the ability. -----
      var immunity = BuffConfigurator.New(
        "GraveWardenDustbringerImmunityBuff", Guids.GraveWardenDustbringerImmunityBuff)
        .SetDisplayName("GraveWardenDustbringerImmunity.Name")
        .SetDescription("GraveWardenDustbringerImmunity.Description")
        .SetIcon(dustIcon)
        .SetIsClassFeature()
        .Configure();
      var mark = BuffConfigurator.New(
        "GraveWardenDustbringerMarkBuff", Guids.GraveWardenDustbringerMarkBuff)
        .SetDisplayName("GraveWardenDustbringerMark.Name")
        .SetDescription("GraveWardenDustbringerMark.Description")
        .SetIcon(dustIcon)
        .SetIsClassFeature()
        .AddComponent(new GraveWardenDustbringerMark
        {
          SlayerClass = slayer,
          ImmunityBuff = immunity,
        })
        .Configure();
      var dustAbility = AbilityConfigurator.New(
        "GraveWardenDustbringerAbility", Guids.GraveWardenDustbringerAbility)
        .SetDisplayName("GraveWardenDustbringer.Name")
        .SetDescription("GraveWardenDustbringer.Description")
        .SetIcon(dustIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Close)
        .SetActionType(UnitCommand.CommandType.Swift)
        .AllowTargeting(enemies: true)
        .SetEffectOnEnemy(AbilityEffectOnUnit.Harmful)
        .AddAbilityEffectRunAction(ActionsBuilder.New()
          .Add(new GraveWardenDustbringerStrikeAction
          {
            MarkBuff = mark,
            ImmunityBuff = immunity,
          })
          .Build())
        .Configure();
      var dustFeature = FeatureConfigurator.New(
        "GraveWardenDustbringerFeature", Guids.GraveWardenDustbringerFeature)
        .SetDisplayName("GraveWardenDustbringer.Name")
        .SetDescription("GraveWardenDustbringer.Description")
        .SetIcon(dustIcon)
        .SetIsClassFeature()
        .AddFacts(new() { dustAbility })
        .Configure();

      // ----- The archetype. -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.GraveWardenArchetype, CharacterClassRefs.SlayerClass)
          .SetLocalizedName("GraveWarden.Name")
          .SetLocalizedDescription("GraveWarden.Description")
          .AddToAddFeatures(LevelPlan.L(2), blessedFeature)
          .AddToAddFeatures(LevelPlan.L(7), wardFeature)
          .AddToAddFeatures(LevelPlan.L(10), dustFeature);
      archetype = ArchetypeRemovals.RemoveAtLevel(
        archetype, slayer.Progression, 2, FeatureRefs.SlayerTalents.ToString());
      archetype = ArchetypeRemovals.RemoveAtLevel(
        archetype, slayer.Progression, 10, FeatureRefs.SlayerTalents.ToString());
      archetype = ArchetypeRemovals.AddRemovals(archetype, slayer, "Stalker");
      archetype.Configure();
      MissionFeats.Logger.Info("[gravewarden] configured: " + ArchetypeName + ".");
    }
  }

  /// <summary>
  /// The Death Ward ritual's payoff: the vanilla death ward buff, self
  /// only, for one minute per slayer level (10 rounds per level).
  /// </summary>
  [TypeId(Guids.GraveWardenDeathWardAction)]
  internal class GraveWardenDeathWardAction : ContextAction
  {
    public BlueprintBuff WardBuff;
    public BlueprintCharacterClass SlayerClass;

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        if (caster is null)
        {
          return;
        }
        int level = caster.Descriptor.Progression.GetClassLevel(SlayerClass);
        caster.AddBuff(WardBuff, Context,
          duration: TimeSpan.FromSeconds(6 * 10 * Math.Max(1, level)));
        MissionFeats.Logger.Info(
          $"[gravewarden] death ward ritual: {level} minute(s) of protection.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[gravewarden] death ward ritual failed.", e);
      }
    }

    public override string GetCaption() => "Death Ward Ritual";
  }

  /// <summary>
  /// Blessed Edge: while the 1-round anointment holds, weapon damage
  /// against undead carries +2d6 holy damage (the holy-water direct
  /// hit). The SanguineFont kinetic-blade rider idiom with the target
  /// side checked for undeath.
  /// </summary>
  [TypeId(Guids.GraveWardenBlessedEdgeRiderComponent)]
  internal class GraveWardenBlessedEdgeRider : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public void OnEventAboutToTrigger(RulePrepareDamage evt) { }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.DamageBundle?.Weapon is null)
        {
          return;
        }
        var target = evt.Target;
        if (target is null || !target.Descriptor.IsUndead)
        {
          return;
        }
        evt.Add(new EnergyDamage(new DiceFormula(2, DiceType.D6), 0, DamageEnergyType.Holy)
        {
          SourceFact = Fact,
        });
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[gravewarden] blessed edge rider failed.", e);
      }
    }
  }

  /// <summary>
  /// The Dustbringer strike: marks an undead foe (non-undead targets are
  /// refused); a foe that has saved within the last 24 hours is immune.
  /// </summary>
  [TypeId(Guids.GraveWardenDustbringerStrikeAction)]
  internal class GraveWardenDustbringerStrikeAction : ContextAction
  {
    public BlueprintBuff MarkBuff;
    public BlueprintBuff ImmunityBuff;

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        var target = Target.Unit;
        if (caster is null || target is null || target.HPLeft <= 0)
        {
          return;
        }
        if (!target.Descriptor.IsUndead)
        {
          MissionFeats.Logger.Info(
            "[gravewarden] dustbringer refused: the target is not undead.");
          return;
        }
        if (target.Buffs.GetBuff(ImmunityBuff) != null)
        {
          MissionFeats.Logger.Info(
            "[gravewarden] dustbringer refused: the target saved within the last day.");
          return;
        }
        target.AddBuff(MarkBuff, Context, duration: ContextDuration.Fixed(1).Calculate(Context).Seconds);
        MissionFeats.Logger.Info(
          $"[gravewarden] dustbringer mark laid upon {target.CharacterName}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[gravewarden] dustbringer strike failed.", e);
      }
    }

    public override string GetCaption() => "Dustbringer";
  }

  /// <summary>
  /// The mark resolves when the warden's attack lands (the Spirit-Ridden
  /// attack-rider idiom): Will save DC 10 + half slayer level +
  /// Intelligence, or the undead is destroyed — delivered as overwhelming
  /// damage through the engine's own RuleDealDamage (the Breaker
  /// direct-damage idiom). Success grants 24 hours of immunity.
  /// </summary>
  [TypeId(Guids.GraveWardenDustbringerMarkComponent)]
  internal class GraveWardenDustbringerMark : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    ITargetRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass SlayerClass;
    public BlueprintBuff ImmunityBuff;

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    public void OnEventDidTrigger(RuleAttackRoll evt)
    {
      try
      {
        var warden = Context?.MaybeCaster;
        if (warden is null || evt.Initiator != warden || !evt.IsHit)
        {
          return;
        }
        // The mark is spent the moment the blow lands.
        Owner.Buffs.RemoveFact(Fact);
        int level = warden.Descriptor.Progression.GetClassLevel(SlayerClass);
        var dc = 10 + level / 2 + warden.Stats.Intelligence.Bonus;
        var save = new RuleSavingThrow(Owner, SavingThrowType.Will, dc);
        Rulebook.Trigger<RuleSavingThrow>(save);
        if (save.Success)
        {
          Owner.AddBuff(ImmunityBuff, Context, duration: TimeSpan.FromHours(24));
          MissionFeats.Logger.Info(
            $"[gravewarden] {Owner.CharacterName} withstands the dustbringer (DC {dc}).");
          return;
        }
        var bundle = new DamageBundle();
        bundle.Add(new DirectDamage(DiceFormula.Zero, 2000));
        Rulebook.Trigger(new RuleDealDamage(warden, Owner, bundle) { Reason = Fact });
        MissionFeats.Logger.Info(
          $"[gravewarden] {Owner.CharacterName} is destroyed by the dustbringer (DC {dc}).");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[gravewarden] dustbringer mark failed.", e);
      }
    }
  }
}

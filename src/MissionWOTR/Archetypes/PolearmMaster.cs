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
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using Kingmaker.UnitLogic.Mechanics;
using MissionWOTR.Feats;
using System;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Polearm Master (faithful port of the APG fighter archetype - the user's
  /// first TTRPG archetype). The polearm master is schooled in the ancient
  /// wisdom that enemies are best faced at the end of a long striking pole.
  ///
  /// Tabletop (APG) and its WOTR adaptation:
  /// - Pole Fighting (2nd, replaces bravery): shorten the grip to strike
  ///   adjacent targets at a -4 penalty, improving by 1 per four levels.
  ///   WOTR has no grip-shortening for reach weapons - adapted into a
  ///   close-quarters bonus: +1 on polearm attacks against ADJACENT enemies,
  ///   growing to +5 at 18th (the inverse of the shrinking penalty).
  /// - Steadfast Pike (3rd, replaces armor training 1-4): +1 on attack rolls
  ///   with readied attacks and attacks of opportunity, +1 per four levels
  ///   beyond 3rd. WOTR has no readied attacks - the vanilla
  ///   AttackOfOpportunityAttackBonus component carries the AoO half of it
  ///   (scaling +1 at 3rd to +5 at 19th; it applies to all attacks of
  ///   opportunity, not only polearm ones - a documented broadening).
  /// - Polearm Training (5th, replaces weapon training 1-4): +1 on attack
  ///   and damage rolls with spears and polearms, +1 per four levels beyond
  ///   5th (the WeaponSpecialistBonus component: +1 at 5th, +2 at 9th,
  ///   +3 at 13th, +4 at 17th).
  /// - Flexible Flanker (9th): choose any adjacent square as the flanking
  ///   origin. WOTR computes flanking from position - adapted via the
  ///   AllyFlankerBonus component: +2 on attacks against any target that at
  ///   least one other ally threatens (flanking, wherever he stands).
  /// - Sweeping Fend (13th): polearm bull rushes (-4) and polearm trips (as
  ///   if the weapon had the trip feature). WOTR maneuvers do not require
  ///   weapon features - adapted by granting the vanilla Improved Trip and
  ///   Improved Bull Rush feats (real +2 maneuver bonuses, no penalties).
  /// - Step Aside (17th): a reactive 5-foot step when a threatened creature
  ///   steps adjacent. WOTR has no 5-foot-step reactions (established in the
  ///   Carousel and True Shape work) - adapted to +2 dodge AC while wielding
  ///   a polearm (the constant footwork reading).
  /// - Polearm Parry (19th, replaces armor mastery): an immediate action
  ///   granting an attacked ally +2 shield AC and DR 5/- against that attack.
  ///   Adapted into an always-on guard (no immediate-action economy in the
  ///   engine): allies within 10 feet of the polearm master gain +2 shield
  ///   AC against attackers he threatens, and damage dealt to them by such
  ///   attackers is reduced by 5 (global rulebook handlers, the pplus
  ///   GoldenLegionnaire global-reaction pattern).
  /// - Weapon Mastery (20th): must choose a spear or polearm - the vanilla
  ///   selection already allows that choice; the restriction is not
  ///   enforced (documented).
  ///
  /// Polearm weapon categories (the game's polearm family - WOTR has no
  /// guisarme, halberd or ranseur weapons): shortspear, spear, longspear,
  /// trident, glaive, bardiche, fauchard, scythe.
  /// Log prefix: [polearm].
  /// </summary>
  internal static class PolearmMaster
  {
    internal const string ArchetypeName = "PolearmMasterArchetype";
    internal const string PoleFightingName = "PolearmMasterPoleFighting";
    internal const string SteadfastPikeName = "PolearmMasterSteadfastPike";
    internal const string PolearmTrainingName = "PolearmMasterPolearmTraining";
    internal const string FlexibleFlankerName = "PolearmMasterFlexibleFlanker";
    internal const string SweepingFendName = "PolearmMasterSweepingFend";
    internal const string StepAsideName = "PolearmMasterStepAside";
    internal const string PolearmParryName = "PolearmMasterPolearmParry";

    /// <summary>The game's polearm family.</summary>
    internal static readonly WeaponCategory[] Polearms =
    {
      WeaponCategory.Shortspear,
      WeaponCategory.Spear,
      WeaponCategory.Longspear,
      WeaponCategory.Trident,
      WeaponCategory.Glaive,
      WeaponCategory.Bardiche,
      WeaponCategory.Fauchard,
      WeaponCategory.Scythe,
    };

    public static void Configure()
    {
      var fighter = CharacterClassRefs.FighterClass.Reference.Get();
      var polearmIcon = FeatureRefs.WeaponTrainingAxes.Reference.Get().Icon;

      // ----- Pole Fighting (2nd) -----
      var poleFighting = FeatureConfigurator.New(PoleFightingName, Guids.PoleFighting)
        .SetDisplayName(PoleFightingName + ".Name")
        .SetDescription(PoleFightingName + ".Description")
        .SetIcon(polearmIcon)
        .SetIsClassFeature()
        .AddComponent(new PolearmCloseQuarters { Class = fighter })
        .Configure();

      // ----- Steadfast Pike (3rd): AoOs with polearms only -----
      var steadfastPike = FeatureConfigurator.New(SteadfastPikeName, Guids.SteadfastPike)
        .SetDisplayName(SteadfastPikeName + ".Name")
        .SetDescription(SteadfastPikeName + ".Description")
        .SetIcon(polearmIcon)
        .SetIsClassFeature()
        .AddComponent(new PolearmSteadfastPike { Class = fighter })
        .Configure();

      // ----- Polearm Training (5th) -----
      var polearmTraining = FeatureConfigurator.New(PolearmTrainingName, Guids.PolearmTraining)
        .SetDisplayName(PolearmTrainingName + ".Name")
        .SetDescription(PolearmTrainingName + ".Description")
        .SetIcon(polearmIcon)
        .SetIsClassFeature()
        .AddComponent(new WeaponSpecialistBonus
        {
          Class = fighter,
          Categories = Polearms,
        })
        .Configure();

      // ----- Flexible Flanker (9th) -----
      var flexibleFlanker = FeatureConfigurator.New(FlexibleFlankerName, Guids.FlexibleFlanker)
        .SetDisplayName(FlexibleFlankerName + ".Name")
        .SetDescription(FlexibleFlankerName + ".Description")
        .SetIcon(polearmIcon)
        .SetIsClassFeature()
        .AddComponent(new AllyFlankerBonus())
        .Configure();

      // ----- Sweeping Fend (13th): maneuvers WITH the polearm -----
      var sweepingFend = FeatureConfigurator.New(SweepingFendName, Guids.SweepingFend)
        .SetDisplayName(SweepingFendName + ".Name")
        .SetDescription(SweepingFendName + ".Description")
        .SetIcon(polearmIcon)
        .SetIsClassFeature()
        .AddComponent(new PolearmSweepFend { Categories = Polearms })
        .Configure();

      // ----- Step Aside (17th) -----
      var stepAside = FeatureConfigurator.New(StepAsideName, Guids.StepAside)
        .SetDisplayName(StepAsideName + ".Name")
        .SetDescription(StepAsideName + ".Description")
        .SetIcon(polearmIcon)
        .SetIsClassFeature()
        .AddComponent(new PolearmFootwork { Categories = Polearms })
        .Configure();

      // ----- Polearm Parry (19th): a swift action, like the tabletop's
      // immediate action - the guard lasts one round. -----
      var parryBuff = BuffConfigurator.New(PolearmParryName + "Buff", Guids.PoleParryBuff)
        .SetDisplayName(PolearmParryName + ".Name")
        .SetDescription(PolearmParryName + ".Description")
        .SetIcon(polearmIcon)
        .SetIsClassFeature()
        .AddComponent(new PolearmParryGuard { Categories = Polearms })
        .Configure();
      var parryAbility = AbilityConfigurator.New(PolearmParryName + "Ability", Guids.PoleParryAbility)
        .SetDisplayName(PolearmParryName + ".Name")
        .SetDescription(PolearmParryName + ".Description")
        .SetIcon(polearmIcon)
        .SetRange(AbilityRange.Personal)
        .SetCanTargetSelf(true)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
        .AddAbilityEffectRunAction(ActionsBuilder.New()
          .ApplyBuff(parryBuff, ContextDuration.Fixed(1), toCaster: true))
        .Configure();
      var polearmParry = FeatureConfigurator.New(PolearmParryName, Guids.PolearmParry)
        .SetDisplayName(PolearmParryName + ".Name")
        .SetDescription(PolearmParryName + ".Description")
        .SetIcon(polearmIcon)
        .SetIsClassFeature()
        .AddFacts(new() { parryAbility })
        .Configure();

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.PolearmMasterArchetype, CharacterClassRefs.FighterClass)
          .SetLocalizedName("PolearmMaster.Name")
          .SetLocalizedDescription("PolearmMaster.Description")
          .AddToAddFeatures(LevelPlan.L(2), poleFighting)
          .AddToAddFeatures(LevelPlan.L(3), steadfastPike)
          .AddToAddFeatures(LevelPlan.L(5), polearmTraining)
          .AddToAddFeatures(LevelPlan.L(9), flexibleFlanker)
          .AddToAddFeatures(LevelPlan.L(13), sweepingFend)
          .AddToAddFeatures(LevelPlan.L(17), stepAside)
          .AddToAddFeatures(LevelPlan.L(19), polearmParry);

      // Trades: bravery (all instances), armor training (all four, via the
      // generic feature guid), weapon training (the selection and its
      // rank-ups), and armor mastery.
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, fighter,
        "f6388946f9f472f4585591b80e9f2452", // Bravery
        "3c380607706f209499d951b29d3c44f3", // ArmorTraining
        "b8cecf4e5e464ad41b79d5b42b76b399", // WeaponTrainingSelection
        "5f3cc7b9a46b880448275763fe70c0b0", // WeaponTrainingRankUpSelection
        "ae177f17cfb45264291d4d7c2cb64671", // ArmorMastery
        "e52aa4151b214d00b720e682fbb4538b"); // FighterArmorMastery

      archetype.Configure();

      MissionFeats.Logger.Info("PolearmMaster: configured.");
    }
  }

  /// <summary>
  /// Steadfast Pike: +1 on ATTACKS OF OPPORTUNITY made with a spear or
  /// polearm, +1 per four levels beyond 3rd (+5 at 19th) - exactly the
  /// tabletop feature, minus its readied-attack half (WOTR has no readied
  /// attacks). AoO detection via the rule's Reason chain (the COP
  /// PairedOpportunists idiom).
  /// </summary>
  [TypeId(Guids.PoleSteadfastPikeComponent)]
  internal class PolearmSteadfastPike : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonus>,
    IRulebookHandler<RuleCalculateAttackBonus>, ISubscriber, IInitiatorRulebookSubscriber
  {
    public BlueprintCharacterClass Class;

    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
    {
      try
      {
        // The Reason chain carries the root attack rule; only
        // RuleAttackWithWeapon exposes AoO-ness (the COP idiom).
        if (evt.Reason.Rule is not RuleAttackWithWeapon withWeapon ||
          !withWeapon.IsAttackOfOpportunity ||
          !PolearmCloseQuarters.WieldsCategory(Owner, PolearmMaster.Polearms))
        {
          return;
        }
        int level = Owner.Progression.GetClassLevel(Class);
        int bonus = Math.Min(5, 1 + Math.Max(0, (level - 3) / 4));
        if (bonus > 0)
        {
          evt.AddModifier(bonus, Fact, ModifierDescriptor.UntypedStackable);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[polearm] steadfast pike failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt)
    {
    }
  }

  /// <summary>
  /// Sweeping Fend: +2 on trip and bull rush maneuvers made while wielding
  /// a spear or polearm (the tabletop's trip-feature benefit, without the
  /// free Improved feats).
  /// </summary>
  [TypeId(Guids.PoleSweepFendComponent)]
  internal class PolearmSweepFend : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCombatManeuver>, IRulebookHandler<RuleCombatManeuver>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public WeaponCategory[] Categories;

    public void OnEventAboutToTrigger(RuleCombatManeuver evt)
    {
      try
      {
        if (evt.Initiator != Owner ||
          (evt.Type != CombatManeuver.Trip && evt.Type != CombatManeuver.BullRush) ||
          !PolearmCloseQuarters.WieldsCategory(Owner, Categories))
        {
          return;
        }
        evt.AddModifier(2, Fact, ModifierDescriptor.UntypedStackable);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[polearm] sweeping fend failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCombatManeuver evt)
    {
    }
  }

  /// <summary>
  /// Pole Fighting, adapted: +1 on polearm attacks against adjacent enemies,
  /// growing with level (1 at 2nd, +1 per four levels beyond, cap +5 at
  /// 18th) - the inverse of the tabletop's shrinking grip penalty.
  /// </summary>
  [TypeId(Guids.PoleCloseQuartersComponent)]
  internal class PolearmCloseQuarters : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonus>,
    IRulebookHandler<RuleCalculateAttackBonus>, ISubscriber, IInitiatorRulebookSubscriber
  {
    public BlueprintCharacterClass Class;

    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
    {
      try
      {
        if (evt.Target is null || !WieldsCategory(Owner, PolearmMaster.Polearms))
        {
          return;
        }
        if (evt.Target.DistanceTo(Owner) > 7.Feet().Meters)
        {
          return; // only close-quarters strikes
        }
        int level = Owner.Progression.GetClassLevel(Class);
        int bonus = Math.Min(5, 1 + Math.Max(0, (level - 2) / 4));
        if (bonus > 0)
        {
          evt.AddModifier(bonus, Fact, ModifierDescriptor.UntypedStackable);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[polearm] close quarters failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt)
    {
    }

    internal static bool WieldsCategory(UnitEntityData owner, WeaponCategory[] categories)
    {
      var weapon = owner.GetFirstWeapon();
      return weapon?.Blueprint?.Category != null &&
        categories.Contains(weapon.Blueprint.Category);
    }
  }

  /// <summary>
  /// Polearm Training / Exotic Training (shared with the Exoticist): +1 on
  /// attack and damage rolls with weapons of the configured categories while
  /// one is wielded, +1 per four levels beyond 5th (+4 at 17th).
  /// </summary>
  [TypeId(Guids.WeaponSpecialistComponent)]
  internal class WeaponSpecialistBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonus>,
    IRulebookHandler<RuleCalculateAttackBonus>,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass Class;
    public WeaponCategory[] Categories;

    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
    {
      try
      {
        if (!PolearmCloseQuarters.WieldsCategory(Owner, Categories))
        {
          return;
        }
        int bonus = Rank();
        if (bonus > 0)
        {
          evt.AddModifier(bonus, Fact, ModifierDescriptor.UntypedStackable);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[polearm] training attack failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt)
    {
    }

    public void OnEventAboutToTrigger(RulePrepareDamage evt)
    {
    }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.Target is null ||
          !PolearmCloseQuarters.WieldsCategory(Owner, Categories))
        {
          return;
        }
        int bonus = Rank();
        if (bonus <= 0)
        {
          return;
        }
        foreach (var damage in evt.DamageBundle)
        {
          damage.AddModifier(new Modifier(bonus, Fact, ModifierDescriptor.UntypedStackable));
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[polearm] training damage failed.", e);
      }
    }

    private int Rank()
    {
      int level = Owner.Progression.GetClassLevel(Class);
      return Math.Max(0, 1 + (level - 5) / 4);
    }
  }

  /// <summary>
  /// Flexible Flanker / Flanker (shared with the Strategic Soldier): +2 on
  /// attacks against any target that at least one OTHER ally threatens -
  /// flanking, wherever he stands.
  /// </summary>
  [TypeId(Guids.AllyFlankerComponent)]
  internal class AllyFlankerBonus : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCalculateAttackBonus>,
    IRulebookHandler<RuleCalculateAttackBonus>, ISubscriber, IInitiatorRulebookSubscriber
  {
    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
    {
      try
      {
        var target = evt.Target;
        if (target is null)
        {
          return;
        }
        foreach (var unit in target.CombatState.EngagedUnits)
        {
          if (unit != null && unit != Owner && unit.IsAlly(Owner))
          {
            evt.AddModifier(2, Fact, ModifierDescriptor.UntypedStackable);
            return;
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[flanker] bonus failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt)
    {
    }
  }

  /// <summary>
  /// Step Aside, adapted: +2 dodge AC while wielding a weapon of the
  /// configured categories (the constant-footwork reading of the tabletop's
  /// reactive 5-foot step).
  /// </summary>
  [TypeId(Guids.PoleFootworkComponent)]
  internal class PolearmFootwork : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateAC>, IRulebookHandler<RuleCalculateAC>,
    ISubscriber, ITargetRulebookSubscriber
  {
    public WeaponCategory[] Categories;

    public void OnEventAboutToTrigger(RuleCalculateAC evt)
    {
      try
      {
        if (PolearmCloseQuarters.WieldsCategory(Owner, Categories))
        {
          evt.AddModifier(2, Fact, ModifierDescriptor.Dodge);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[polearm] footwork failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt)
    {
    }
  }

  /// <summary>
  /// Polearm Parry, adapted: while he wields a polearm, allies within 10
  /// feet gain +2 shield AC against attackers within his reach, and damage
  /// dealt to them by such attackers is reduced by 5 (global rulebook
  /// handlers - the pplus GoldenLegionnaire global-reaction pattern).
  /// </summary>
  [TypeId(Guids.PoleParryComponent)]
  internal class PolearmParryGuard : UnitFactComponentDelegate,
    IGlobalRulebookHandler<RuleCalculateAC>, IRulebookHandler<RuleCalculateAC>,
    IGlobalRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    ISubscriber, IGlobalRulebookSubscriber
  {
    public WeaponCategory[] Categories;

    public void OnEventAboutToTrigger(RuleCalculateAC evt)
    {
      try
      {
        if (evt.Target == null || evt.Target == Owner ||
          !PolearmCloseQuarters.WieldsCategory(Owner, Categories))
        {
          return;
        }
        if (Guards(evt.Initiator, evt.Target))
        {
          evt.AddModifier(2, Fact, ModifierDescriptor.Shield);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[polearm] parry AC failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAC evt)
    {
    }

    public void OnEventAboutToTrigger(RulePrepareDamage evt)
    {
    }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Target == null || evt.Target == Owner ||
          !PolearmCloseQuarters.WieldsCategory(Owner, Categories))
        {
          return;
        }
        if (Guards(evt.Initiator, evt.Target))
        {
          foreach (var damage in evt.DamageBundle)
          {
            damage.AddModifier(new Modifier(-5, Fact, ModifierDescriptor.UntypedStackable));
          }
          MissionFeats.Logger.Info(
            $"[polearm] parry: 5 damage turned from {evt.Target.CharacterName}.");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[polearm] parry damage failed.", e);
      }
    }

    private bool Guards(UnitEntityData attacker, UnitEntityData ally)
    {
      return attacker != null && ally != null && ally.IsAlly(Owner) &&
        ally.DistanceTo(Owner) <= 10.Feet().Meters &&
        attacker.IsEnemy(Owner) && attacker.DistanceTo(Owner) <= 10.Feet().Meters;
    }
  }
}

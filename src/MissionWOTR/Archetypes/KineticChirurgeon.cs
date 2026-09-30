using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Kinetic Chirurgeon (faithful port of the Occult Adventures kineticist
  /// archetype - 0.20.0; the user's pick of the two remaining unused
  /// tabletop kineticist archetypes, per the coverage check: vanilla ships
  /// Blood Kineticist, Dark Elementalist, Elemental Engine, Kinetic Knight,
  /// Kinetic Sharpshooter, Overwhelming Soul and Psychokineticist - the
  /// chirurgeon collides with nothing. Elemental Annihilator remains
  /// unimplemented everywhere).
  ///
  /// While any hydrokineticist can learn the rudiments of healing, some
  /// kineticists are virtuosos of the curative arts. The kinetic
  /// chirurgeon trades her entire offensive shaping discipline - infusions,
  /// metakinesis, infusion specialization - for the healer's craft.
  ///
  /// The trades (all vanilla features, verified by guid):
  /// - Infusions (the InfusionSelection at every level it is granted) and
  ///   Infusion Specialization: she can never use infusions.
  /// - Metakinesis (Empower, Maximize, Quicken - all three features).
  ///
  /// The kit:
  /// - Kinetic Healer (1st): touch, standard action - restores 1d6 + her
  ///   Constitution modifier per 2 kineticist levels to a willing target
  ///   (or herself). Raw-damage healing (the honest route - see NOTES.md).
  /// - Mercies (3rd onward): each use of kinetic healer also cures one
  ///   condition, auto-triaged from the worst tier her level has unlocked:
  ///   3rd - fatigued, shaken; 5th - staggered, entangled; 7th - blinded,
  ///   frightened; 9th - stunned. (CRPG adaptation of "one paladin mercy
  ///   per use, of her choice" - the triage picks for her, Owlcat-style.)
  /// - Metahealer (5th): +1 extra healing die at 5th, +2 at 11th, +3 at
  ///   17th (auto-applied - she would always choose it).
  /// - Swift Mending (13th): a second, swift-action version of kinetic
  ///   healer that targets herself only.
  /// - Shared Mending (17th): her kinetic healer now heals both the target
  ///   and herself with the same use.
  ///
  /// Adaptations (documented, not faked):
  /// - The tabletop's primary-element restriction (aether/water/wood) is
  ///   skipped: WOTR has no aether or wood, and the healer here draws on
  ///   the kineticist herself, not her element.
  /// - The 9th-level breath-of-life revive and the 6th-level doubled
  ///   internal buffer are skipped: mid-combat revival and the burn
  ///   buffer have no verifiable API surfaces. She keeps internal buffer.
  /// - The burn cost is the tabletop's (0.21.0): 1 point per use,
  ///   accepted by the chirurgeon through the real burn API
  ///   (UnitPartKineticist.AcceptBurn, verified via the KineticArchetypes
  ///   mod - see docs/NOTES.md); gather power and other burn reducers
  ///   apply engine-side. If she cannot accept the burn, nothing happens.
  ///   The tabletop's "the target may accept the burn instead" clause is
  ///   simplified: only she pays. The mercy list
  ///   is adapted to the engine's condition buffs - poisons, diseases and
  ///   curses are not buffs and are not curable by it.
  /// Log prefix: [removals] carries trade diagnostics; [chirurgeon] the rest.
  /// </summary>
  internal static class KineticChirurgeon
  {
    internal const string ArchetypeName = "KineticChirurgeonArchetype";

    public static void Configure()
    {
      var kineticist = CharacterClassRefs.KineticistClass.Reference.Get();
      var icon = AbilityRefs.HealersWayOthers.Reference.Get().Icon;

      var heal = ElementTool.Create<ContextActionKineticHeal>();
      heal.Class = kineticist;
      heal.SelfOnly = false;
      var healerAbility = AbilityConfigurator.New("ChirurgeonKineticHealerAbility", Guids.ChirurgeonHealerAbility)
        .SetDisplayName("ChirurgeonKineticHealer.Name")
        .SetDescription("ChirurgeonKineticHealer.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Touch)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard)
        .SetCanTargetFriends()
        .SetCanTargetSelf()
        .AddAbilityEffectRunAction(
          BlueprintCore.Actions.Builder.ActionsBuilder.New().Add(heal).Build())
        .Configure();

      var swiftHeal = ElementTool.Create<ContextActionKineticHeal>();
      swiftHeal.Class = kineticist;
      swiftHeal.SelfOnly = true;
      var swiftAbility = AbilityConfigurator.New("ChirurgeonSwiftMendingAbility", Guids.ChirurgeonSwiftMendingAbility)
        .SetDisplayName("ChirurgeonSwiftMending.Name")
        .SetDescription("ChirurgeonSwiftMending.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
        .SetCanTargetSelf()
        .AddAbilityEffectRunAction(
          BlueprintCore.Actions.Builder.ActionsBuilder.New().Add(swiftHeal).Build())
        .Configure();

      var healer = FeatureConfigurator.New("ChirurgeonKineticHealerFeature", Guids.ChirurgeonHealerFeature)
        .SetDisplayName("ChirurgeonKineticHealer.Name")
        .SetDescription("ChirurgeonKineticHealer.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { healerAbility })
        .Configure();

      var metahealer = FeatureConfigurator.New("ChirurgeonMetahealerFeature", Guids.ChirurgeonMetahealerFeature)
        .SetDisplayName("ChirurgeonMetahealer.Name")
        .SetDescription("ChirurgeonMetahealer.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .Configure();

      var swiftMending = FeatureConfigurator.New("ChirurgeonSwiftMendingFeature", Guids.ChirurgeonSwiftMendingFeature)
        .SetDisplayName("ChirurgeonSwiftMending.Name")
        .SetDescription("ChirurgeonSwiftMending.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { swiftAbility })
        .Configure();

      var sharedMending = FeatureConfigurator.New("ChirurgeonSharedMendingFeature", Guids.ChirurgeonSharedMendingFeature)
        .SetDisplayName("ChirurgeonSharedMending.Name")
        .SetDescription("ChirurgeonSharedMending.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.KineticChirurgeonArchetype, CharacterClassRefs.KineticistClass)
          .SetLocalizedName("KineticChirurgeon.Name")
          .SetLocalizedDescription("KineticChirurgeon.Description")
          .AddToAddFeatures(LevelPlan.L(1), healer)
          .AddToAddFeatures(LevelPlan.L(5), metahealer)
          .AddToAddFeatures(LevelPlan.L(13), swiftMending)
          .AddToAddFeatures(LevelPlan.L(17), sharedMending);

      // The trades: infusions (the selection at every level it appears),
      // infusion specialization, and all three metakinesis features.
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, kineticist,
        FeatureSelectionRefs.InfusionSelection.ToString(),
        FeatureRefs.InfusionSpecialization.ToString(),
        FeatureRefs.MetakinesisEmpowerFeature.ToString(),
        FeatureRefs.MetakinesisMaximizedFeature.ToString(),
        FeatureRefs.MetakinesisQuickenFeature.ToString());

      archetype.Configure();

      MissionFeats.Logger.Info("KineticChirurgeon: configured.");
    }
  }

  /// <summary>
  /// Kinetic Healer's engine: restores 1d6 + Con per 2 kineticist levels
  /// (plus the Metahealer's extra dice from 5th), cures one condition from
  /// the mercy ladder, and (17th, Shared Mending) heals the chirurgeon too.
  /// Raw-damage healing per NOTES.md - the honest, verifiable route.
  /// </summary>
  [TypeId(Guids.ChirurgeonHealComponent)]
  internal class ContextActionKineticHeal : Kingmaker.UnitLogic.Mechanics.Actions.ContextAction
  {
    public BlueprintCharacterClass Class;
    public bool SelfOnly;

    public override string GetCaption() => "Kinetic Healer";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        var target = SelfOnly ? caster : Target.Unit;
        if (caster is null || target is null || target.HPLeft <= 0)
        {
          return;
        }
        var level = caster.Progression.GetClassLevel(Class);

        // The burn cost (0.21.0): 1 point per use, the real burn API -
        // no burn left, no healing (the tabletop's cost, finally payable
        // now that the API is verified).
        var part = caster.Parts
          .Get<Kingmaker.UnitLogic.Class.Kineticist.UnitPartKineticist>();
        if (part is null || part.LeftBurn < 1 || part.LeftBurnThisRound < 1)
        {
          MissionFeats.Logger.Info("[chirurgeon] no burn left to power the healer.");
          CombatLog.Write("Her gate is spent - the healer falters.", caster);
          return;
        }
        part.AcceptBurn(1, Context.AssociatedAbility);

        // 1d6 + Con per 2 levels (min 1 die), plus Metahealer's dice.
        var rolls = Math.Max(1, level / 2);
        rolls += level >= 17 ? 3 : level >= 11 ? 2 : level >= 5 ? 1 : 0;
        var con = caster.Stats.Constitution.Bonus;
        var random = new System.Random();
        var heal = 0;
        for (int i = 0; i < rolls; i++)
        {
          heal += random.Next(1, 7) + con;
        }

        Heal(target, heal);
        MissionFeats.Logger.Info(
          $"[chirurgeon] healed {target.CharacterName} for {heal}.");
        CombatLog.Write(
          $"The kinetic healer mends {target.CharacterName} for {heal}.", caster);

        // Shared Mending (17th): the same use mends the chirurgeon.
        if (!SelfOnly && level >= 17 && caster != target)
        {
          Heal(caster, heal);
        }

        CureOneCondition(target, level);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[chirurgeon] heal failed.", e);
      }
    }

    private static void Heal(UnitEntityData unit, int amount)
    {
      var damage = unit.Descriptor.Damage;
      if (damage > 0)
      {
        unit.Descriptor.Damage = Math.Max(0, damage - amount);
      }
    }

    /// <summary>
    /// The mercy ladder: one condition per use, auto-triaged from the
    /// worst tier her level has unlocked. (All buffs verified in refs.)
    /// </summary>
    private static void CureOneCondition(UnitEntityData target, int level)
    {
      (int Gate, BlueprintBuff Buff)[] ladder =
      {
        (9, BuffRefs.Stunned.Reference.Get()),
        (7, BuffRefs.Blind.Reference.Get()),
        (7, BuffRefs.Frightened.Reference.Get()),
        (5, BuffRefs.Staggered.Reference.Get()),
        (5, BuffRefs.EntangledBuff.Reference.Get()),
        (3, BuffRefs.Fatigued.Reference.Get()),
        (3, BuffRefs.Shaken.Reference.Get()),
      };
      foreach (var (gate, buff) in ladder)
      {
        if (level < gate)
        {
          continue;
        }
        var fact = target.Buffs.GetBuff(buff);
        if (fact is not null)
        {
          target.Buffs.RemoveFact(fact);
          MissionFeats.Logger.Info(
            $"[chirurgeon] mercy: cured {buff.name} on {target.CharacterName}.");
          CombatLog.Write(
            $"The mending washes {buff.name} away.", target);
          return;
        }
      }
    }
  }
}

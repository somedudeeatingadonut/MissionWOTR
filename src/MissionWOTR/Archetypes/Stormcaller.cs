using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Stormcaller (original homebrew kineticist archetype - 0.22.0; concept
  /// by the user: an air-damage specialist - "there is already a fire and
  /// water specialist" (vanilla water/blood, modded fire/Cinder Adept) -
  /// whose electric damage OVERRIDES IMMUNITIES "as per other electric
  /// damage sources", i.e. the game's own answer to demon electricity
  /// immunity: the Ascendant Element mechanic. And something to do with
  /// SPEED: "more casts in exchange for less power in them? higher
  /// movement speed, whatever else you can think of").
  ///
  /// The storm never stops moving, and neither does she. She is the wind
  /// given a rider: faster than any kineticist, throwing more bolts than
  /// any kineticist, each one carrying less of the storm than a full
  /// blast - until the storm fully accepts her.
  ///
  /// Coverage check (docs/COVERAGE.md, run before designing): vanilla
  /// kineticist archetypes are Blood Kineticist, Dark Elementalist,
  /// Elemental Engine, Kinetic Knight, Kinetic Sharpshooter, Overwhelming
  /// Soul, Psychokineticist; modded: Kinetic Archetypes (Cinder Adept =
  /// fire, Duelist/Lancer/Onslaught), DarkCodex (Elemental Ascetic, the
  /// generic any-element Elemental Scion). No air/electric specialist
  /// exists. The name Stormcaller collides with nothing.
  ///
  /// The kit:
  /// - Storm's Swiftness (1st): +10 ft movement speed (enhancement), and
  ///   the SWIFT BLASTS: a free-action version of every air-family blast
  ///   she owns (Air, Electric, Blizzard, Charged Water, Plasma,
  ///   Sandstorm, Thunderstorm - one clone each, full vanilla damage
  ///   wiring), usable once per round, with every damage entry's DICE
  ///   HALVED (the "more casts, less power" trade). Also carries
  ///   Ascendant Element (Electricity) - the vanilla mythic feature's own
  ///   component (the same one ThunderingRageBuff uses): her electricity
  ///   ignores electricity immunity AND resistance, exactly as electric
  ///   builds achieve through Ascendant Element.
  /// - Tailwind (5th): +10 ft more (total +20).
  /// - Lightning Step (8th): swift action, blink to any unit's side
  ///   within close range (no attack riders - pure mobility; the engine's
  ///   one-swift-per-round economy is the limit).
  /// - Riding the Current (12th): two swift blasts per round.
  /// - Eye of the Storm (16th): the swift blasts no longer halve their
  ///   dice, and she gains the vanilla electricity immunity feature.
  ///
  /// The trades (hefty): Enveloping Winds (the air defense talent - the
  /// storm does not shield her), and the Expanded Element line (Secondary
  /// AND Greater Elemental Focus selections - air is all she is, all the
  /// way down).
  ///
    /// Honesty notes (documented, not faked):
    /// - The swift-blast budget is spent when the bolt is LOOSED: on a
    ///   hit via RulePrepareDamage, and - 0.22.1, per the user - on a
    ///   miss via RuleAttackRoll (the same Reason chain). A missed free
    ///   bolt consumes the round's charge; the storm does not refund.
  /// - The budget is a static per-unit registry read by the ability
  ///   restriction and written by the damage handler (the CovertMage
  ///   static-dictionary precedent; entries clear on feature loss).
  /// - Infusions do not apply to the swift clones (infusion wiring lives
  ///   in the vanilla blast's variant system; the clones are separate
  ///   abilities cloned from the base blasts). Her normal blasts keep
  ///   infusions.
  /// Log prefix: [removals] carries trade diagnostics; [stormcaller] the rest.
  /// </summary>
  internal static class Stormcaller
  {
    internal const string ArchetypeName = "StormcallerArchetype";

    /// <summary>
    /// The air-family blasts: token, the vanilla blast ability (from the
    /// Explosion work's tuple - also the clone source), and the new swift
    /// clone's guid.
    /// </summary>
    internal static readonly (string Blast, string CloneGuid)[] AirFamily =
    {
      ("Air", Guids.StormcallerSwiftAirBlastAbility),
      ("Electric", Guids.StormcallerSwiftElectricBlastAbility),
      ("Blizzard", Guids.StormcallerSwiftBlizzardBlastAbility),
      ("ChargedWater", Guids.StormcallerSwiftChargedWaterBlastAbility),
      ("Plasma", Guids.StormcallerSwiftPlasmaBlastAbility),
      ("Sandstorm", Guids.StormcallerSwiftSandstormBlastAbility),
      ("Thunderstorm", Guids.StormcallerSwiftThunderstormBlastAbility),
    };

    internal static string BlastGuid(string token)
    {
      return KineticistExplosion.Blasts
        .First(b => b.Blast == token).BlastGuid;
    }

    // Wired during Configure; read by the components.
    internal static BlueprintFeature RidingTheCurrentFeature;
    internal static BlueprintFeature EyeOfTheStormFeature;

    public static void Configure()
    {
      var kineticist = CharacterClassRefs.KineticistClass.Reference.Get();

      // ----- The swift blasts: one free-action clone per air-family blast -----
      foreach (var entry in AirFamily)
      {
        var source = BlueprintTool.Get<BlueprintAbility>(BlastGuid(entry.Blast));
        if (source is null)
        {
          MissionFeats.Logger.Warn(
            $"[stormcaller] {entry.Blast}: base blast blueprint missing - skipped.");
          continue;
        }
        var clone = CloneAbility(
          "StormcallerSwift" + entry.Blast + "BlastAbility", entry.CloneGuid, source);
        AbilityConfigurator.For(clone.name)
          .SetDisplayName("StormcallerSwift" + entry.Blast + ".Name")
          .SetDescription("StormcallerSwiftBlast.Description")
          .SetIcon(source.Icon)
          .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free)
          .AddComponent(new StormcallerSwiftRestriction())
          .Configure();
      }

      // ----- Storm's Swiftness (1st) -----
      var swiftness = FeatureConfigurator.New("StormcallerSwiftnessFeature", Guids.StormcallerSwiftnessFeature)
        .SetDisplayName("StormcallerSwiftness.Name")
        .SetDescription("StormcallerSwiftness.Description")
        .SetIcon(AbilityRefs.AirBlastAbility.Reference.Get().Icon)
        .SetIsClassFeature()
        // The wind at her back: +10 ft, enhancement.
        .AddBuffMovementSpeed(value: 10, descriptor: ModifierDescriptor.Enhancement)
        // THE user requirement: her electricity overrides immunity and
        // resistance - the Ascendant Element component itself (the same
        // one the vanilla mythic feature and ThunderingRageBuff carry).
        .AddAscendantElement(element: DamageEnergyType.Electricity)
        // Grants the swift clones for owned blasts; halves their dice.
        .AddComponent(new StormcallerSwiftGrant())
        .AddComponent(new StormcallerSwiftTracker())
        .Configure();

      // ----- Tailwind (5th) -----
      var tailwind = FeatureConfigurator.New("StormcallerTailwindFeature", Guids.StormcallerTailwindFeature)
        .SetDisplayName("StormcallerTailwind.Name")
        .SetDescription("StormcallerTailwind.Description")
        .SetIcon(AbilityRefs.AirBlastAbility.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddBuffMovementSpeed(value: 10, descriptor: ModifierDescriptor.Enhancement)
        .Configure();

      // ----- Lightning Step (8th) -----
      var step = ElementTool.Create<ContextActionStormStep>();
      var lightningStep = AbilityConfigurator.New("StormcallerLightningStepAbility", Guids.StormcallerLightningStepAbility)
        .SetDisplayName("StormcallerLightningStep.Name")
        .SetDescription("StormcallerLightningStep.Description")
        .SetIcon(AbilityRefs.AirBlastAbility.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Close)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
        .SetCanTargetEnemies()
        .SetCanTargetFriends()
        .AddAbilityEffectRunAction(
          BlueprintCore.Actions.Builder.ActionsBuilder.New().Add(step).Build())
        .Configure();
      var stepFeature = FeatureConfigurator.New("StormcallerLightningStepFeature", Guids.StormcallerLightningStepFeature)
        .SetDisplayName("StormcallerLightningStep.Name")
        .SetDescription("StormcallerLightningStep.Description")
        .SetIcon(AbilityRefs.AirBlastAbility.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { lightningStep })
        .Configure();

      // ----- Riding the Current (12th) -----
      RidingTheCurrentFeature = FeatureConfigurator.New(
        "StormcallerRidingTheCurrentFeature", Guids.StormcallerRidingTheCurrentFeature)
        .SetDisplayName("StormcallerRidingTheCurrent.Name")
        .SetDescription("StormcallerRidingTheCurrent.Description")
        .SetIcon(AbilityRefs.AirBlastAbility.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      // ----- Eye of the Storm (16th) -----
      EyeOfTheStormFeature = FeatureConfigurator.New(
        "StormcallerEyeOfTheStormFeature", Guids.StormcallerEyeOfTheStormFeature)
        .SetDisplayName("StormcallerEyeOfTheStorm.Name")
        .SetDescription("StormcallerEyeOfTheStorm.Description")
        .SetIcon(AbilityRefs.AirBlastAbility.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.StormcallerArchetype, CharacterClassRefs.KineticistClass)
          .SetLocalizedName("Stormcaller.Name")
          .SetLocalizedDescription("Stormcaller.Description")
          .AddToAddFeatures(LevelPlan.L(1), swiftness)
          .AddToAddFeatures(LevelPlan.L(5), tailwind)
          .AddToAddFeatures(LevelPlan.L(8), stepFeature)
          .AddToAddFeatures(LevelPlan.L(12), RidingTheCurrentFeature)
          .AddToAddFeatures(LevelPlan.L(16), EyeOfTheStormFeature)
          // The storm's final gift: the vanilla electricity immunity.
          .AddToAddFeatures(LevelPlan.L(16), FeatureRefs.ElectricityImmunity.ToString());

      // The hefty trades: the air defense, and the entire element-expansion
      // line (Owlcat's names verbatim, typo included: "Secondaty").
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, kineticist,
        FeatureRefs.EnvelopingWinds.ToString(),
        FeatureSelectionRefs.SecondatyElementalFocusSelection.ToString(),
        FeatureSelectionRefs.GreaterElementalFocusSelection.ToString());

      archetype.Configure();

      MissionFeats.Logger.Info("Stormcaller: configured.");
    }

    /// <summary>
    /// Clone recipe (proven on Construct Crafter units and the Explosion
    /// infusion's blast clones): CopyFrom with an all-matcher plus the
    /// reflection field copy.
    /// </summary>
    private static BlueprintAbility CloneAbility(
      string name, string guid, BlueprintAbility source)
    {
      var configurator = AbilityConfigurator.New(name, guid).CopyFrom(source, _ => true);
      var ability = configurator.Configure();

      const System.Reflection.BindingFlags flags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly;
      int copied = 0;
      foreach (var field in typeof(BlueprintAbility).GetFields(flags))
      {
        try
        {
          field.SetValue(ability, field.GetValue(source));
          copied++;
        }
        catch
        {
          // Init-only or compiler-generated members are skipped.
        }
      }
      MissionFeats.Logger.Info(
        $"[stormcaller] {name} cloned from {source.name}: {copied} fields.");
      return ability;
    }
  }

  /// <summary>
  /// The swift-blast budget registry, shared by the restriction (reads)
  /// and the tracker (writes). Keyed by unit id; resets one round after
  /// the last reset. The CovertMage static-dictionary precedent.
  /// </summary>
  internal static class StormcallerSwiftBudget
  {
    internal struct Entry
    {
      public TimeSpan ResetAt;
      public int Used;
    }

    internal static readonly Dictionary<string, Entry> Registry = new();

    internal static int UsedThisRound(string unitId)
    {
      if (!Registry.TryGetValue(unitId, out var entry))
      {
        return 0;
      }
      var now = CombatTime.Now();
      if (entry.ResetAt <= now)
      {
        return 0; // stale - the round rolled over
      }
      return entry.Used;
    }

    internal static void Spend(string unitId)
    {
      var now = CombatTime.Now();
      var entry = Registry.TryGetValue(unitId, out var existing) && existing.ResetAt > now
        ? existing
        : new Entry { ResetAt = now + 1.Rounds().Seconds };
      entry.Used++;
      Registry[unitId] = entry;
    }

    internal static void Clear(string unitId)
    {
      Registry.Remove(unitId);
    }
  }

  /// <summary>
  /// The ability-side gate: a swift clone may be cast while the round's
  /// budget allows it (1 per round; 2 from 12th with Riding the Current).
  /// The kinarch MustHaveEquippedKineticBlade pattern
  /// (BlueprintComponent + IAbilityCasterRestriction).
  /// </summary>
  [TypeId(Guids.StormcallerSwiftRestrictionComponent)]
  internal class StormcallerSwiftRestriction : BlueprintComponent, IAbilityCasterRestriction
  {
    private string failReason;

    public bool IsCasterRestrictionPassed(UnitEntityData caster)
    {
      if (caster is null)
      {
        return false;
      }
      var allowance = Stormcaller.RidingTheCurrentFeature is not null &&
        caster.HasFact(Stormcaller.RidingTheCurrentFeature) ? 2 : 1;
      var used = StormcallerSwiftBudget.UsedThisRound(caster.UniqueId);
      if (used >= allowance)
      {
        failReason = "The storm's swift charge is spent this round.";
        return false;
      }
      failReason = null;
      return true;
    }

    public string GetAbilityCasterRestrictionUIText()
    {
      return failReason;
    }
  }

  /// <summary>
  /// Grants the swift clones for every air-family blast the kineticist
  /// owns (the Explosion grant pattern; re-syncs on fact re-activation,
  /// so an Expanded-Element blast arrives after the next area load - the
  /// same documented edge).
  /// </summary>
  [TypeId(Guids.StormcallerSwiftGrantComponent)]
  internal class StormcallerSwiftGrant : UnitFactComponentDelegate
  {
    protected override void OnActivate()
    {
      try
      {
        foreach (var entry in Stormcaller.AirFamily)
        {
          var blast = BlueprintTool.Get<BlueprintAbility>(
            Stormcaller.BlastGuid(entry.Blast));
          var clone = BlueprintTool.Get<BlueprintAbility>(entry.CloneGuid);
          if (blast is null || clone is null)
          {
            continue;
          }
          if (Owner.HasFact(blast) && Owner.GetFact(clone) is null)
          {
            Owner.AddFact(clone);
            MissionFeats.Logger.Info(
              $"[stormcaller] granted {clone.name} (blast owned).");
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[stormcaller] swift grant failed.", e);
      }
    }

    protected override void OnDeactivate()
    {
      try
      {
        foreach (var entry in Stormcaller.AirFamily)
        {
          var clone = BlueprintTool.Get<BlueprintAbility>(entry.CloneGuid);
          if (clone is null)
          {
            continue;
          }
          var fact = Owner.GetFact(clone);
          if (fact is not null)
          {
            Owner.RemoveFact(fact);
          }
        }
        StormcallerSwiftBudget.Clear(Owner.UniqueId);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[stormcaller] swift removal failed.", e);
      }
    }
  }

  /// <summary>
  /// The storm's price and its lifting: halves every damage entry's DICE
  /// when the damage comes from a swift clone (the TTT MythicSneakAttack
  /// dice-modify pattern) until the Eye of the Storm is hers; spends the
  /// round's swift budget when a bolt LANDS (RulePrepareDamage fires on
  /// the hit), and - 0.22.1, per the user - ALSO when a bolt MISSES: the
  /// attack roll (RuleAttackRoll, one event per attack - crit
  /// confirmation is a flag inside it, not a second event, so no double
  /// spend) carries the same Reason chain as the damage rule (TTT's
  /// decompiled ContextActionDealDamage builds RuleDealDamage.Reason FROM
  /// attackRoll.Reason), and Reason.Ability is set for ability attacks
  /// (the InitiatorSpellCritAutoconfirm proof). A loosed bolt spends the
  /// charge whether it lands or breaks on the wind.
  /// </summary>
  [TypeId(Guids.StormcallerSwiftTrackerComponent)]
  internal class StormcallerSwiftTracker : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    private static readonly HashSet<BlueprintGuid> SwiftGuids =
      new HashSet<BlueprintGuid>(
        Stormcaller.AirFamily.Select(a => BlueprintGuid.Parse(a.CloneGuid)));

    public void OnEventAboutToTrigger(RulePrepareDamage evt) { }

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    /// <summary>
    /// The miss price: a swift bolt that breaks on the wind still spends
    /// the round's charge - the bolt was loosed either way. (The hit path
    /// spends in RulePrepareDamage below; IsHit rolls never reach the
    /// spend here.)
    /// </summary>
    public void OnEventDidTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.IsHit || evt.Initiator != Owner)
        {
          return; // lands are paid for below; only HER rolls count
        }
        var source = evt.Reason?.Ability?.Blueprint ?? evt.Reason?.Context?.SourceAbility;
        if (source is null || !SwiftGuids.Contains(source.AssetGuid))
        {
          return; // not one of her swift bolts
        }
        StormcallerSwiftBudget.Spend(Owner.UniqueId);
        CombatLog.Write("The swift bolt breaks on the wind - the charge is spent anyway.", Owner);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[stormcaller] swift miss tracker failed.", e);
      }
    }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner)
        {
          return;
        }
        var source = evt.Reason?.Ability?.Blueprint ?? evt.Reason?.Context?.SourceAbility;
        if (source is null || !SwiftGuids.Contains(source.AssetGuid))
        {
          return; // not one of her swift bolts
        }

        // The charge is spent when the bolt lands.
        StormcallerSwiftBudget.Spend(Owner.UniqueId);

        // The price: halved dice - until the Eye of the Storm (16th).
        var full = Stormcaller.EyeOfTheStormFeature is not null &&
          Owner.HasFact(Stormcaller.EyeOfTheStormFeature);
        if (full)
        {
          return;
        }
        foreach (BaseDamage damage in evt.DamageBundle)
        {
          var rolls = damage.Dice.ModifiedValue.Rolls;
          var die = damage.Dice.ModifiedValue.Dice;
          if (rolls > 1)
          {
            damage.Dice.Modify(
              new DiceFormula(Math.Max(1, rolls / 2), die), Fact);
          }
        }
        CombatLog.Write("The swift bolt lands at half the storm's force.", Owner);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[stormcaller] swift tracker failed.", e);
      }
    }
  }

  /// <summary>
  /// Lightning Step: blink to the target's side - pure mobility, no
  /// riders (the Construct Crafter's BlinkStrike position math, minus
  /// the strike).
  /// </summary>
  [TypeId(Guids.StormcallerStormStepComponent)]
  internal class ContextActionStormStep : NamedContextAction
  {
    public override string GetCaption() => "Lightning Step";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        var target = Target?.Unit ?? Context.MainTarget?.Unit;
        if (caster is null || target is null || target.HPLeft <= 0)
        {
          return;
        }
        var direction = caster.Position - target.Position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
          direction = UnityEngine.Vector3.forward;
        }
        var spot = target.Position
          + direction.normalized * (target.Corpulence + caster.Corpulence + 0.75f);
        caster.Position = spot;
        CombatLog.Write("She rides the current.", caster);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[stormcaller] lightning step failed.", e);
      }
    }
  }
}

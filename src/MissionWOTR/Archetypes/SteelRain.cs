using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// THE STEEL RAIN (rogue homebrew - the user's design, 0.30.0:
  /// "a rogue that focuses on throwing weapons. Keep it fairly
  /// simple.").
  ///
  /// In WOTR, thrown weapons are RANGED weapon types - javelins,
  /// throwing axes, darts fly as dedicated ranged items ("30 ft
  /// Ranged" on the wiki), not as melee blades that happen to leave
  /// the hand. Detection is therefore a whitelist of the thrown
  /// candidates from the vanilla weapon-type list, FILTERED AT
  /// CONFIGURE TIME to those whose AttackType is actually Ranged in
  /// this game (a dagger blueprint is melee here - dagger STABS
  /// must never count; the filter's surviving set is logged).
  /// Documented edge: modded thrown weapon types are not recognized
  /// (the whitelist is vanilla-only) - a simple design, simply
  /// documented.
  ///
  /// The kit (0.31.0 rebalance, per the user: Ricochet REMOVED,
  /// Flick of the Wrist moved to 6th, and a new downside added):
  /// - Quick Hands (1st, replaces trapfinding): +1 on attack rolls
  ///   with thrown weapons, +2 at 8th, +3 at 16th (a temporary
  ///   AdditionalAttackBonus modifier on the attack roll - the
  ///   GuideFocusBonus pattern).
  /// - Flick of the Wrist (6th): she adds her Dexterity modifier to
  ///   thrown-weapon damage, on top of the Strength the throw
  ///   already carries (a design choice, documented: throwing
  ///   builds need the help, and her arm is the point).
  /// - Catch! (12th): once per round, when she KILLS with a thrown
  ///   weapon, the blade comes back clean and goes out again - an
  ///   immediate free throw at the nearest living enemy within 30
  ///   feet: a phantom RuleAttackRoll with the same weapon, and on
  ///   a hit 1d6 + her Dexterity modifier (the weapon's own dice
  ///   are not re-read - simple by design, documented).
  /// - ONE ART (1st, the added downside, per the user): her hands
  ///   only know the throw - she takes a -2 penalty on attack
  ///   rolls with any weapon that is NOT a thrown weapon (melee
  ///   blades and bows alike, up close and out of practice). A
  ///   visible flaw feature, so the price is read at level-up.
  ///
  /// The trades: trapfinding (1st), danger sense (EVERY rank, 3rd
  /// through 18th - the AddRemovalsAtAllLevels helper), and One
  /// Art's standing penalty. Sneak attack, evasion, uncanny dodge,
  /// improved uncanny dodge, rogue talents, debilitating injuries
  /// and master strike are untouched - the knife was always the
  /// trade; this one simply flies.
  /// Log prefix: [steelrain].
  /// </summary>
  internal static class SteelRain
  {
    internal const string ArchetypeName = "SteelRainArchetype";

    /// <summary>The thrown weapon types of this game, resolved at
    /// configure time (candidates filtered by AttackType ==
    /// Ranged).</summary>
    internal static readonly List<BlueprintWeaponType> ThrownTypes = new();

    internal static bool IsThrown(ItemEntityWeapon weapon)
    {
      return weapon is not null && weapon.Type is not null &&
        ThrownTypes.Contains(weapon.Type);
    }

    public static void Configure()
    {
      var rogue = CharacterClassRefs.RogueClass.Reference.Get();

      // ----- The thrown whitelist -----
      string[] candidates =
      {
        WeaponTypeRefs.Dart.ToString(),
        WeaponTypeRefs.Javelin.ToString(),
        WeaponTypeRefs.ThrowingAxe.ToString(),
        WeaponTypeRefs.Starknife.ToString(),
        WeaponTypeRefs.Trident.ToString(),
        WeaponTypeRefs.Spear.ToString(),
        WeaponTypeRefs.LightHammer.ToString(),
        WeaponTypeRefs.Handaxe.ToString(),
        WeaponTypeRefs.Dagger.ToString(),
      };
      foreach (var candidate in candidates)
      {
        var type = BlueprintTool.Get<BlueprintWeaponType>(candidate);
        if (type is null)
        {
          continue;
        }
        if (type.AttackType == AttackType.Ranged)
        {
          ThrownTypes.Add(type);
        }
      }
      MissionFeats.Logger.Info("[steelrain] thrown weapon types: " +
        string.Join(", ", ThrownTypes.ConvertAll(t => t.name)) + ".");

      // ----- Quick Hands (1st) -----
      var quickHands = FeatureConfigurator.New(
        "SteelRainQuickHandsFeature", Guids.SteelRainQuickHandsFeature)
        .SetDisplayName("SteelRainQuickHands.Name")
        .SetDescription("SteelRainQuickHands.Description")
        .SetIcon(AbilityRefs.LeadBlades.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new SteelRainQuickHands { RogueClass = rogue })
        .Configure();

      // ----- One Art (1st, the added downside - the user's ask) -----
      var oneArt = FeatureConfigurator.New(
        "SteelRainOneArtFeature", Guids.SteelRainOneArtFeature)
        .SetDisplayName("SteelRainOneArt.Name")
        .SetDescription("SteelRainOneArt.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new SteelRainOneArt())
        .Configure();

      // ----- Flick of the Wrist (6th) -----
      var flick = FeatureConfigurator.New(
        "SteelRainFlickFeature", Guids.SteelRainFlickFeature)
        .SetDisplayName("SteelRainFlick.Name")
        .SetDescription("SteelRainFlick.Description")
        .SetIcon(AbilityRefs.Haste.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new SteelRainFlick())
        .Configure();

      // ----- Catch! (12th) -----
      var catchFeature = FeatureConfigurator.New(
        "SteelRainCatchFeature", Guids.SteelRainCatchFeature)
        .SetDisplayName("SteelRainCatch.Name")
        .SetDescription("SteelRainCatch.Description")
        .SetIcon(AbilityRefs.QuarryAbility.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new SteelRainCatch())
        .Configure();

      // ----- The archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.SteelRainArchetype, CharacterClassRefs.RogueClass)
          .SetLocalizedName("SteelRain.Name")
          .SetLocalizedDescription("SteelRain.Description")
          .AddToAddFeatures(LevelPlan.L(1), quickHands)
          .AddToAddFeatures(LevelPlan.L(1), oneArt)
          .AddToAddFeatures(LevelPlan.L(6), flick)
          .AddToAddFeatures(LevelPlan.L(12), catchFeature);

      archetype = ArchetypeRemovals.AddRemovals(
        archetype, rogue, FeatureRefs.Trapfinding.ToString());
      archetype = ArchetypeRemovals.AddRemovalsAtAllLevels(
        archetype, rogue, FeatureRefs.DangerSenseRogue.ToString());

      archetype.Configure();
      MissionFeats.Logger.Info("[steelrain] configured: " + ArchetypeName + ".");
    }
  }

  /// <summary>
  /// Quick Hands: +1 on attack rolls with thrown weapons, +2 at
  /// 8th, +3 at 16th - a temporary AdditionalAttackBonus modifier on
  /// the attack roll (the GuideFocusBonus pattern).
  /// </summary>
  [TypeId(Guids.SteelRainQuickHandsComponent)]
  internal class SteelRainQuickHands : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass RogueClass;

    public void OnEventAboutToTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.IsFake || !SteelRain.IsThrown(evt.Weapon))
        {
          return;
        }
        int level = Owner.Progression.GetClassLevel(RogueClass);
        int bonus = level >= 16 ? 3 : level >= 8 ? 2 : 1;
        evt.AddTemporaryModifier(evt.Initiator.Stats.AdditionalAttackBonus
          .AddModifier(bonus, Runtime, ModifierDescriptor.Competence));
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[steelrain] quick hands failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
  }

  /// <summary>
  /// Flick of the Wrist: her Dexterity modifier joins the throw -
  /// added to thrown-weapon damage on top of the Strength it
  /// already carries (a design choice, documented).
  /// </summary>
  [TypeId(Guids.SteelRainFlickComponent)]
  internal class SteelRainFlick : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public void OnEventAboutToTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner)
        {
          return;
        }
        var roll = evt.ParentRule?.AttackRoll;
        if (roll is null || !SteelRain.IsThrown(roll.Weapon))
        {
          return;
        }
        int dex = (Owner.Stats.Dexterity.ModifiedValue - 10) / 2;
        if (dex != 0)
        {
          evt.Add(new DirectDamage(DiceFormula.Zero, dex) { SourceFact = Fact });
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[steelrain] flick of the wrist failed.", e);
      }
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
  }

  /// <summary>
  /// Catch!: once per round, a thrown KILL earns an immediate free
  /// throw at the nearest living enemy within 30 feet - a phantom
  /// RuleAttackRoll with the same weapon (the elk idiom), and on a
  /// hit 1d6 + her Dexterity modifier. The once-per-round gate is
  /// set BEFORE the free throw, so a kill made by the free throw
  /// cannot chain (simple by design).
  /// </summary>
  [TypeId(Guids.SteelRainCatchComponent)]
  internal class SteelRainCatch : UnitFactComponentDelegate,
    Kingmaker.Controllers.Units.ITickEachRound,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    private bool spent;

    public void OnNewRound()
    {
      spent = false;
    }

    protected override void OnActivate()
    {
      spent = false;
    }

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    public void OnEventDidTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.IsFake || !evt.IsHit || spent ||
          !SteelRain.IsThrown(evt.Weapon))
        {
          return;
        }
        if (!evt.Target.Descriptor.State.IsDead && evt.Target.HPLeft > 0)
        {
          return; // not a kill
        }
        spent = true;
        // The nearest living enemy within 30 feet of HER.
        UnitEntityData victim = null;
        foreach (var enemy in Wildbond.EnemiesWithin(Owner, 30))
        {
          if (victim is null || enemy.DistanceTo(Owner) < victim.DistanceTo(Owner))
          {
            victim = enemy;
          }
        }
        if (victim is null)
        {
          return;
        }
        var freeThrow = new RuleAttackRoll(Owner, victim, evt.Weapon, 0);
        Game.Instance.Rulebook.TriggerEvent(freeThrow);
        if (!freeThrow.IsHit)
        {
          return;
        }
        int dex = (Owner.Stats.Dexterity.ModifiedValue - 10) / 2;
        var damage = new DirectDamage(new DiceFormula(1, DiceType.D6), dex)
        {
          SourceFact = Fact,
        };
        Game.Instance.Rulebook.TriggerEvent(new RuleDealDamage(Owner, victim, damage));
        CombatLog.Write("The blade comes back clean - and goes out again.", Owner);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[steelrain] catch failed.", e);
      }
    }
  }


  /// <summary>
  /// One Art (the added downside, 0.31.0): her hands only know the
  /// throw - a -2 penalty on attack rolls with any weapon that is
  /// NOT a thrown weapon (melee blades and bows alike).
  /// </summary>
  [TypeId(Guids.SteelRainOneArtComponent)]
  internal class SteelRainOneArt : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public void OnEventAboutToTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.IsFake)
        {
          return;
        }
        // Everything that is not a thrown weapon - including fists
        // and bows. Her art has exactly one shape.
        if (evt.Weapon is null || SteelRain.IsThrown(evt.Weapon))
        {
          return;
        }
        evt.AddTemporaryModifier(evt.Initiator.Stats.AdditionalAttackBonus
          .AddModifier(-2, Runtime, ModifierDescriptor.UntypedStackable));
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[steelrain] one art failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleAttackRoll evt) { }
  }
}

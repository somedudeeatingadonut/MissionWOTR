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
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Hammerfist Monk (HOMEBREW - 0.11.0). The user's brief: "a monk that
  /// focuses on one big damage hit with their fists instead of a lot of hits.
  /// Slightly lower overall since it's easier to hit, and removing quite a
  /// bit of the other gimmicks monk has, with a little something extra to
  /// differentiate it that you come up with."
  ///
  /// The answer:
  /// - Crushing Fist (1st, replaces the Flurry of Blows line and the Stunning
  ///   Fist line): once per round, the first unarmed strike that CONNECTS
  ///   deals extra damage - 2 plus 1.5 x monk level (+32 at 20th). One
  ///   move-and-punch round now carries most of a flurry's weight, while a
  ///   full-attack round still falls slightly short of the flurry monk's
  ///   output (only the first connecting blow is boosted) - the "slightly
  ///   lower overall" the brief asked for. The payoff is consistency: no
  ///   full-attack dependency, and the boost lands on the first HIT of the
  ///   round, not the first swing, so it is never wasted on a miss.
  /// - Rolling Thunder (9th, the differentiator): the first unarmed strike
  ///   of each attack sequence to hit also slams the target with a free trip
  ///   combat maneuver (CMB vs CMD, no action, no attack of opportunity).
  ///   Built from the vanilla AddInitiatorAttackWithWeaponTrigger - the same
  ///   wiring the game's own Two-Handed Fighter Piledriver uses - with the
  ///   knockdown action chained on maneuver success. Scales with the
  ///   maneuver training the monk keeps.
  /// - Removed outright (the "quite a bit of gimmicks"): Flurry of Blows and
  ///   both of its unlock tiers, the Stunning Fist line (base + fatigue +
  ///   sickened upgrades), Evasion and Improved Evasion. Kept: AC bonus,
  ///   maneuver training, fast movement, still mind, purity of body, the ki
  ///   pool and every ki power (including our 0.10.x additions). Deliberately
  ///   stackable with the Qinggong monk - the two remove disjoint features.
  /// Log prefix: [removals] carries the trade diagnostics; [hammerfist] the rest.
  /// </summary>
  internal static class Hammerfist
  {
    internal const string ArchetypeName = "HammerfistArchetype";

    public static void Configure()
    {
      var monk = CharacterClassRefs.MonkClass.Reference.Get();
      var fistIcon = AbilityRefs.CrushingBlowAbility.Reference.Get().Icon;
      var thunderIcon = AbilityRefs.KiShout.Reference.Get().Icon;

      // ----- Crushing Fist (1st): the one big hit -----
      var crushingFist = FeatureConfigurator.New("HammerfistCrushingFist", Guids.HammerfistCrushingFist)
        .SetDisplayName("HammerfistCrushingFist.Name")
        .SetDescription("HammerfistCrushingFist.Description")
        .SetIcon(fistIcon)
        .SetIsClassFeature()
        .AddComponent(new HammerfistCrushingFistComponent { Class = monk })
        .Configure();

      // ----- Rolling Thunder (9th): the differentiator -----
      // Vanilla on-hit trigger (the Piledriver wiring), fists-only via the
      // weapon-category check. The trip result is applied both by the
      // maneuver rule itself and, belt-and-braces, by the knockdown action
      // chained on success.
      var rollingThunder = FeatureConfigurator.New("HammerfistRollingThunder", Guids.HammerfistRollingThunder)
        .SetDisplayName("HammerfistRollingThunder.Name")
        .SetDescription("HammerfistRollingThunder.Description")
        .SetIcon(thunderIcon)
        .SetIsClassFeature()
        .AddComponent<AddInitiatorAttackWithWeaponTrigger>(trigger =>
        {
          trigger.Action = ActionsBuilder.New()
            .CombatManeuver(
              ActionsBuilder.New().KnockdownTarget(),
              CombatManeuver.Trip)
            .Build();
          trigger.OnlyHit = true;
          trigger.OnlyOnFirstHit = true;
          trigger.CheckWeaponCategory = true;
          trigger.Category = WeaponCategory.UnarmedStrike;
        })
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.HammerfistArchetype, CharacterClassRefs.MonkClass)
          .SetLocalizedName("Hammerfist.Name")
          .SetLocalizedDescription("Hammerfist.Description")
          .AddToAddFeatures(LevelPlan.L(1), crushingFist)
          .AddToAddFeatures(LevelPlan.L(9), rollingThunder);

      archetype = ArchetypeRemovals.AddRemovals(
        archetype, monk,
        "332362f3bd39ebe46a740a36960fdcb4", // Flurry of Blows
        "fd99770e6bd240a4aab70f7af103e56a", // Flurry of Blows unlock tier
        "a34b8a9fcc9024b42bacfd5e6b614bfa", // Flurry of Blows 11th-level unlock tier
        "de25523acc24b1448aa90f74d6512a08", // Flurry of Blows Level 11 (variant; no-op if unused)
        "a29a582c3daa4c24bb0e991c596ccb28", // Stunning Fist
        "819645da2e446f84d9b168ed1676ec29", // Stunning Fist: Fatigue
        "d256ab3837538cc489d4b571e3a813eb", // Stunning Fist: Sickened
        "576933720c440aa4d8d42b0c54b77e80", // Evasion
        "ce96af454a6137d47b9c6a1e02e66803", // Improved Evasion
        "0d35d6c4d5eef8d4790d09bd9a874e57"); // Improved Evasion (variant; no-op if unused)

      archetype.Configure();

      MissionFeats.Logger.Info("Hammerfist: configured.");
    }
  }

  /// <summary>
  /// Crushing Fist: once per round, the monk's first unarmed strike that hits
  /// deals extra damage (2 + 1.5 x monk level). Implemented on
  /// RulePrepareDamage - which fires only for attacks that connect - with a
  /// round-long cooldown in component data, the same bookkeeping Step Aside
  /// uses. Works identically in real-time and turn-based modes - true
  /// only as of 0.52.1, when the round clock moved to CombatTime.Now().
  /// Before that it read the real-time game clock, which turn-based
  /// combat holds still, so the bonus landed once per FIGHT rather than
  /// once per round.
  /// </summary>
  [TypeId(Guids.HammerfistCrushingFistComponent)]
  internal class HammerfistCrushingFistComponent :
    UnitFactComponentDelegate<HammerfistCrushingFistComponent.ComponentData>,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass Class;

    public void OnEventAboutToTrigger(RulePrepareDamage evt) { }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.Target is null ||
          evt.Reason.Rule is not RuleAttackWithWeapon attack ||
          attack.Weapon?.Blueprint?.Category != WeaponCategory.UnarmedStrike)
        {
          return;
        }

        // Once per round - only the first connecting unarmed strike.
        var now = CombatTime.Now();
        if (Data.LastUse + 1.Rounds().Seconds > now)
        {
          return;
        }

        int level = Owner.Progression.GetClassLevel(Class);
        int bonus = 2 + level * 3 / 2;
        if (bonus <= 0)
        {
          return;
        }

        foreach (var damage in evt.DamageBundle)
        {
          damage.AddModifier(new Modifier(bonus, Fact, ModifierDescriptor.UntypedStackable));
        }
        Data.LastUse = now;
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[hammerfist] crushing fist damage failed.", e);
      }
    }

    public class ComponentData
    {
      public TimeSpan LastUse;
    }
  }
}

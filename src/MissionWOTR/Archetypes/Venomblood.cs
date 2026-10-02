using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.BasicEx;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Venomblood Hunter (HOMEBREW - 0.12.0, wholly of our own devising - the
  /// user asked for two hunter archetypes "completely of your own volition").
  ///
  /// The concept: a hunter raised on serpent venom rather than mother's milk.
  /// Where the classic hunter wins through coordination - teamwork feats
  /// shared with her beast, tactics, one-ness with the wild - the venomblood
  /// wins through attrition: every bite of her blade leaves poison behind.
  /// She KEEPS the animal companion and the animal-focus aspects (the hound
  /// harries while the venom works), and trades away the entire tactical
  /// half of the class.
  ///
  /// Trades (real removals against the live progression): hunter tactics
  /// (teamwork sharing), the teamwork-feat progression, woodland stride and
  /// one with the wild (both the feature and its pet half).
  ///
  /// Kit:
  /// - Serpent's Gift (1st): once per round, the first weapon hit that
  ///   connects injects venom - Fortitude save (DC 10 + 1/2 hunter level +
  ///   Wis) or the target takes 1d2 Constitution damage as the toxin courses
  ///   (the strain burns out in a round; a fresh save comes with every
  ///   round's first hit). The venom is supernatural and works even on
  ///   poison-immune demons, though they resist it (+4 on the save) - the
  ///   same lever the Eldritch Poisoner documented for the Worldwound.
  /// - Serpent's Skin (6th): the hunter is immune to poison.
  /// - Neurotoxin (9th): a failed save also sickens the target for 1 round.
  /// Log prefix: [removals] carries the trade diagnostics; [venomblood] the rest.
  /// </summary>
  internal static class Venomblood
  {
    internal const string ArchetypeName = "VenombloodArchetype";

    // Wired during Configure; read by the delivery component.
    internal static BlueprintBuff ToxinStrain;
    internal static BlueprintBuff ToxinStrain2;
    internal static BlueprintBuff ToxinStrain3;
    internal static BlueprintBuff DebilitatingStrain;
    internal static BlueprintFeature NeurotoxinFeature;
    internal static BlueprintFeature PotentVenomFeature;
    internal static BlueprintFeature LethalVenomFeature;

    /// <summary>
    /// 0.53.0 — the dose deepens with the hunter's years: 1d2 Constitution at
    /// 1st, 1d4 at 12th, 1d6 at 16th.
    /// </summary>
    internal static BlueprintBuff StrainFor(int level)
    {
      if (level >= 16 && ToxinStrain3 is not null)
      {
        return ToxinStrain3;
      }
      if (level >= 12 && ToxinStrain2 is not null)
      {
        return ToxinStrain2;
      }
      return ToxinStrain;
    }

    /// <summary>
    /// One tier of the toxin: 1dN Constitution damage as it enters the blood.
    /// The three tiers share a display name and description; the delivery
    /// component picks one by level.
    /// </summary>
    private static BlueprintBuff ConStrain(string name, string guid, DiceType dice)
    {
      return BuffConfigurator.New(name, guid)
        .SetDisplayName("VenombloodToxinStrain.Name")
        .SetDescription("VenombloodToxinStrain.Description")
        .SetIcon(AbilityRefs.Poison.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFactContextActions(activated: ActionsBuilder.New().DealStatDamage(
          new DiceFormula(1, dice), StatType.Constitution,
          ElementTool.Create<ContextTargetUnit>(), damageBonus: 0))
        .Configure();
    }

    public static void Configure()
    {
      var hunter = CharacterClassRefs.HunterClass.Reference.Get();
      var venomIcon = AbilityRefs.Poison.Reference.Get().Icon;

      // ----- The toxin strain: Con damage the moment it enters the blood ----
      // Built like the Eldritch Poisoner's strains (stat damage on the buff's
      // activated action) - the same Owlcat-adjacent wiring, one round of burn.
      // 0.53.0: three tiers, picked by level in StrainFor.
      ToxinStrain = ConStrain(
        "VenombloodToxinStrain", Guids.VenombloodToxinStrain, DiceType.D2);
      ToxinStrain2 = ConStrain(
        "VenombloodToxinStrain2", Guids.VenombloodToxinStrain2, DiceType.D4);
      ToxinStrain3 = ConStrain(
        "VenombloodToxinStrain3", Guids.VenombloodToxinStrain3, DiceType.D6);

      // ----- Potent Venom's rider (12th): the venom also saps Dexterity ----
      DebilitatingStrain = BuffConfigurator.New(
          "VenombloodDebilitatingStrain", Guids.VenombloodDebilitatingStrain)
        .SetDisplayName("VenombloodDebilitatingStrain.Name")
        .SetDescription("VenombloodDebilitatingStrain.Description")
        .SetIcon(venomIcon)
        .SetIsClassFeature()
        .AddFactContextActions(activated: ActionsBuilder.New().DealStatDamage(
          new DiceFormula(1, DiceType.D2), StatType.Dexterity,
          ElementTool.Create<ContextTargetUnit>(), damageBonus: 0))
        .Configure();

      // ----- Serpent's Gift (1st): venom on the first hit of each round -----
      var serpentsGift = FeatureConfigurator.New("VenombloodSerpentsGift", Guids.VenombloodSerpentsGift)
        .SetDisplayName("VenombloodSerpentsGift.Name")
        .SetDescription("VenombloodSerpentsGift.Description")
        .SetIcon(venomIcon)
        .SetIsClassFeature()
        .AddComponent(new VenombloodDelivery { Class = hunter })
        .Configure();

      // ----- Serpent's Skin (6th): immunity to poison -----
      var serpentsSkin = FeatureConfigurator.New("VenombloodSerpentsSkin", Guids.VenombloodSerpentsSkin)
        .SetDisplayName("VenombloodSerpentsSkin.Name")
        .SetDescription("VenombloodSerpentsSkin.Description")
        .SetIcon(venomIcon)
        .SetIsClassFeature()
        .AddFacts(new() { FeatureRefs.ImmunityToPoison.Reference.Get() })
        .Configure();

      // ----- Neurotoxin (9th): failed saves also sicken -----
      NeurotoxinFeature = FeatureConfigurator.New("VenombloodNeurotoxin", Guids.VenombloodNeurotoxin)
        .SetDisplayName("VenombloodNeurotoxin.Name")
        .SetDescription("VenombloodNeurotoxin.Description")
        .SetIcon(venomIcon)
        .SetIsClassFeature()
        .Configure();

      // ----- Potent Venom (12th) / Lethal Venom (16th) ----
      // 0.53.0 - the user's note that the archetype needed more over the
      // levels was fair: it granted at 1st, 6th and 9th and then flat-lined
      // for eleven levels. These two are the back half, and they carry the
      // strain upgrades with them so the climb is visible in the tooltip.
      PotentVenomFeature = FeatureConfigurator.New("VenombloodPotentVenom", Guids.VenombloodPotentVenom)
        .SetDisplayName("VenombloodPotentVenom.Name")
        .SetDescription("VenombloodPotentVenom.Description")
        .SetIcon(venomIcon)
        .SetIsClassFeature()
        .Configure();

      LethalVenomFeature = FeatureConfigurator.New("VenombloodLethalVenom", Guids.VenombloodLethalVenom)
        .SetDisplayName("VenombloodLethalVenom.Name")
        .SetDescription("VenombloodLethalVenom.Description")
        .SetIcon(venomIcon)
        .SetIsClassFeature()
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.VenombloodArchetype, CharacterClassRefs.HunterClass)
          .SetLocalizedName("Venomblood.Name")
          .SetLocalizedDescription("Venomblood.Description")
          .AddToAddFeatures(LevelPlan.L(1), serpentsGift)
          .AddToAddFeatures(LevelPlan.L(6), serpentsSkin)
          .AddToAddFeatures(LevelPlan.L(9), NeurotoxinFeature)
          .AddToAddFeatures(LevelPlan.L(12), PotentVenomFeature)
          .AddToAddFeatures(LevelPlan.L(16), LethalVenomFeature);

      archetype = ArchetypeRemovals.AddRemovals(
        archetype, hunter,
        "1b9916f7675d6ef4fb427081250d49de", // Hunter Tactics (teamwork sharing)
        "2efe5983c9064cc6b55f16bc68f0fc33", // Woodland Stride
        "c1e0f4ada7c673e4f8e5c57d1eea13d0"); // One with the Wild

      // 0.53.0 FIX — the teamwork-feat trade was a hard-coded GUID
      // (14b66a1e2a6a415182a651db8c0f1143) that the 0.52.1 in-game log
      // proves is NOT in the hunter progression: "[removals]
      // 14b66a1e... not found in HunterClass progression - removal
      // skipped." So she kept the entire teamwork-feat progression she
      // was documented as trading away. It is now removed BY NAME at
      // every level the live progression grants one, which survives guid
      // churn. The dead "One with the Wild (pet half)" guid
      // (f34a34c8..., also logged as not found) is dropped with it.
      archetype = ArchetypeRemovals.RemoveEveryGrant(archetype, hunter, "Teamwork");
      ArchetypeRemovals.DumpProgression(hunter, "Teamwork");

      archetype.Configure();

      MissionFeats.Logger.Info("Venomblood: configured.");
    }
  }

  /// <summary>
  /// Delivers the venomblood toxin: once per round, on the first weapon hit
  /// that connects, the target must save or take the strain. Shaped after
  /// the Eldritch Poisoner's proven delivery component, with the once-per-
  /// round cooldown bookkeeping Step Aside and Crushing Fist use.
  /// </summary>
  [TypeId(Guids.VenombloodDeliveryComponent)]
  internal class VenombloodDelivery :
    UnitFactComponentDelegate<VenombloodDelivery.ComponentData>,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>, IRulebookHandler<RuleAttackWithWeapon>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass Class;

    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt)
    {
      try
      {
        if (evt.AttackRoll is null || !evt.AttackRoll.IsHit ||
          evt.Target is null || evt.Target.HPLeft <= 0)
        {
          return;
        }

        // Once per round - only the first connecting strike carries venom.
        var now = CombatTime.Now();
        if (Data.LastUse + 1.Rounds().Seconds > now)
        {
          return;
        }

        var target = evt.Target;
        var level = Owner.Progression.GetClassLevel(Class);
        var strain = Venomblood.StrainFor(level);

        // No reapplication while this tier of the strain is already burning.
        if (strain is not null && target.Buffs.GetBuff(strain) is not null)
        {
          return;
        }

        var dc = 10 + level / 2 + Owner.Stats.Wisdom.Bonus;

        // Supernatural venom: bites even poison-immune demons, though they
        // resist it - the same Worldwound lever the Eldritch Poisoner uses.
        if (target.HasFact(FeatureRefs.ImmunityToPoison.Reference.Get()) ||
          target.HasFact(FeatureRefs.PoisonImmunity.Reference.Get()))
        {
          dc -= 4;
        }

        Data.LastUse = now;

        var save = new RuleSavingThrow(target, SavingThrowType.Fortitude, dc) { Reason = Fact };
        if (Rulebook.Trigger(save).IsPassed)
        {
          return;
        }

        var seconds = ContextDuration.Fixed(1).Calculate(Context).Seconds;
        target.AddBuff(strain, Context, duration: seconds);

        // Potent Venom (12th): the venom also saps Dexterity.
        if (Venomblood.DebilitatingStrain is not null &&
          Venomblood.PotentVenomFeature is not null &&
          Owner.HasFact(Venomblood.PotentVenomFeature))
        {
          target.AddBuff(Venomblood.DebilitatingStrain, Context, duration: seconds);
        }

        // Neurotoxin (9th): the venom reaches the nerves. Lethal Venom (16th)
        // upgrades the sickening to nausea - the Eldritch Poisoner's 12th-level
        // escalation, at the same shape.
        if (Venomblood.NeurotoxinFeature is not null && Owner.HasFact(Venomblood.NeurotoxinFeature))
        {
          var lethal = Venomblood.LethalVenomFeature is not null &&
            Owner.HasFact(Venomblood.LethalVenomFeature);
          target.AddBuff(
            (lethal ? BuffRefs.Nauseated : BuffRefs.Sickened).Reference.Get(),
            Context, duration: seconds);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[venomblood] venom delivery failed.", e);
      }
    }

    public class ComponentData
    {
      public TimeSpan LastUse;
    }
  }
}

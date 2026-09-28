using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Riftstalker Hunter (HOMEBREW - 0.12.0, wholly of our own devising - the
  /// user asked for two hunter archetypes "completely of your own volition").
  ///
  /// The concept: a hunter of the Worldwound who walks alone. The rift took
  /// her beast - or she gave it up so it would not have to see what she
  /// became - and left something else in its place: a hunter's focus so
  /// absolute it marks reality itself. She trades the ENTIRE companion side
  /// of the class (companion, raise companion, one with the wild) plus the
  /// teamwork-tactics side, and keeps the aspects, the spells, the stride
  /// through the corrupted wilds.
  ///
  /// Kit:
  /// - Rift Mark (1st): once per round, the first hit that connects brands
  ///   the target for one minute. Against her mark, the stalker strikes
  ///   with terrible certainty: +2 attack and damage (see Riftstudy).
  /// - Rift Stride (2nd): nothing to wait for, no one to slow her down -
  ///   +10 feet of movement speed.
  /// - Riftstudy (5th): the mark deepens - a further +2 attack and damage
  ///   against marked prey (total +4).
  /// - Hunter's Feast (12th): when a marked creature dies by her hand, the
  ///   stalker feasts on the kill - one minute of +2 attack and damage
  ///   against everything.
  /// Log prefix: [removals] carries the trade diagnostics; [riftstalker] the rest.
  /// </summary>
  internal static class Riftstalker
  {
    internal const string ArchetypeName = "RiftstalkerArchetype";

    // Wired during Configure; read by the components.
    internal static BlueprintBuff MarkBuff;
    internal static BlueprintBuff FeastBuff;

    public static void Configure()
    {
      var hunter = CharacterClassRefs.HunterClass.Reference.Get();
      var markIcon = AbilityRefs.HuntersSurpriseAbility.Reference.Get().Icon;

      // ----- The mark itself: a plain brand, carried by the prey -----
      MarkBuff = BuffConfigurator.New("RiftstalkerMarkBuff", Guids.RiftstalkerMarkBuff)
        .SetDisplayName("RiftstalkerMark.Name")
        .SetDescription("RiftstalkerMark.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .Configure();

      // ----- Hunter's Feast: the reward for a successful hunt -----
      FeastBuff = BuffConfigurator.New("RiftstalkerFeastBuff", Guids.RiftstalkerFeastBuff)
        .SetDisplayName("RiftstalkerFeast.Name")
        .SetDescription("RiftstalkerFeast.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .AddComponent(new RiftstalkerFeastBonus())
        .Configure();

      // ----- Rift Mark (1st): brand the prey, strike it true -----
      var riftMark = FeatureConfigurator.New("RiftstalkerRiftMark", Guids.RiftstalkerRiftMark)
        .SetDisplayName("RiftstalkerRiftMark.Name")
        .SetDescription("RiftstalkerRiftMark.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .AddComponent(new RiftstalkerMarkDelivery { Class = hunter })
        .AddComponent(new RiftstalkerMarkBonus { Bonus = 2 })
        .Configure();

      // ----- Rift Stride (2nd): +10 feet -----
      var riftStride = FeatureConfigurator.New("RiftstalkerRiftStride", Guids.RiftstalkerRiftStride)
        .SetDisplayName("RiftstalkerRiftStride.Name")
        .SetDescription("RiftstalkerRiftStride.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .AddBuffMovementSpeed(value: 10, descriptor: ModifierDescriptor.Enhancement)
        .Configure();

      // ----- Riftstudy (5th): the mark deepens -----
      var riftstudy = FeatureConfigurator.New("RiftstalkerRiftstudy", Guids.RiftstalkerRiftstudy)
        .SetDisplayName("RiftstalkerRiftstudy.Name")
        .SetDescription("RiftstalkerRiftstudy.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .AddComponent(new RiftstalkerMarkBonus { Bonus = 2 })
        .Configure();

      // ----- Hunter's Feast (12th) -----
      var huntersFeast = FeatureConfigurator.New("RiftstalkerHuntersFeast", Guids.RiftstalkerHuntersFeast)
        .SetDisplayName("RiftstalkerHuntersFeast.Name")
        .SetDescription("RiftstalkerHuntersFeast.Description")
        .SetIcon(markIcon)
        .SetIsClassFeature()
        .AddComponent(new RiftstalkerMarkDelivery { Class = hunter, Feast = true })
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.RiftstalkerArchetype, CharacterClassRefs.HunterClass)
          .SetLocalizedName("Riftstalker.Name")
          .SetLocalizedDescription("Riftstalker.Description")
          .AddToAddFeatures(LevelPlan.L(1), riftMark)
          .AddToAddFeatures(LevelPlan.L(2), riftStride)
          .AddToAddFeatures(LevelPlan.L(5), riftstudy)
          .AddToAddFeatures(LevelPlan.L(12), huntersFeast);

      archetype = ArchetypeRemovals.AddRemovals(
        archetype, hunter,
        "715ac15eb8bd5e342bc8a0a3c9e3e38f", // Animal Companion (hunter's selection)
        "27ad1316abbbbb34b8ffb9a87f38c10e", // Raise Companion
        "1b9916f7675d6ef4fb427081250d49de", // Hunter Tactics (teamwork sharing)
        "14b66a1e2a6a415182a651db8c0f1143", // Hunter teamwork-feat progression
        "c1e0f4ada7c673e4f8e5c57d1eea13d0", // One with the Wild
        "f34a34c8f8a8410ca5e0e21800fa4961"); // One with the Wild (pet half)

      archetype.Configure();

      MissionFeats.Logger.Info("Riftstalker: configured.");
    }
  }

  /// <summary>
  /// Applies the rift mark (first hit each round) and, when the Feast
  /// upgrade is owned, rewards the stalker when marked prey dies by her
  /// hand. Same delivery shape as the venomblood: RuleAttackWithWeapon
  /// trigger with a once-per-round component-data cooldown.
  /// </summary>
  [TypeId(Guids.RiftstalkerMarkDeliveryComponent)]
  internal class RiftstalkerMarkDelivery :
    UnitFactComponentDelegate<RiftstalkerMarkDelivery.ComponentData>,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>, IRulebookHandler<RuleAttackWithWeapon>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass Class;
    public bool Feast;

    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt)
    {
      try
      {
        if (evt.AttackRoll is null || !evt.AttackRoll.IsHit || evt.Target is null)
        {
          return;
        }
        var target = evt.Target;
        var wasMarked = Riftstalker.MarkBuff is not null &&
          target.Buffs.GetBuff(Riftstalker.MarkBuff) is not null;

        // Once per round, the first connecting strike brands the target.
        var now = Kingmaker.Game.Instance.TimeController.GameTime;
        var markApplied = false;
        if (Data.LastUse + 1.Rounds().Seconds <= now && target.HPLeft > 0)
        {
          Data.LastUse = now;
          target.AddBuff(
            Riftstalker.MarkBuff, Context,
            duration: ContextDuration.Fixed(10).Calculate(Context).Seconds);
          markApplied = true;
        }

        // Hunter's Feast: the kill of a marked creature feeds the stalker.
        if (Feast && Riftstalker.FeastBuff is not null && target.HPLeft <= 0 &&
          (wasMarked || markApplied))
        {
          Owner.AddBuff(
            Riftstalker.FeastBuff, Context,
            duration: ContextDuration.Fixed(10).Calculate(Context).Seconds);
          MissionFeats.Logger.Info("[riftstalker] hunter's feast triggered.");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[riftstalker] mark delivery failed.", e);
      }
    }

    public class ComponentData
    {
      public TimeSpan LastUse;
    }
  }

  /// <summary>
  /// The stalker's certainty against her mark: +Bonus attack and damage
  /// against any target carrying the rift mark. Global rulebook handlers,
  /// the same wiring the Polearm Parry guard uses.
  /// </summary>
  [TypeId(Guids.RiftstalkerMarkBonusComponent)]
  internal class RiftstalkerMarkBonus : UnitFactComponentDelegate,
    IGlobalRulebookHandler<RuleCalculateAttackBonus>, IRulebookHandler<RuleCalculateAttackBonus>,
    IGlobalRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    ISubscriber, IGlobalRulebookSubscriber
  {
    public int Bonus;

    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.Target is null || Bonus <= 0 ||
          Riftstalker.MarkBuff is null ||
          evt.Target.Buffs.GetBuff(Riftstalker.MarkBuff) is null)
        {
          return;
        }
        evt.AddModifier(Bonus, Fact, ModifierDescriptor.UntypedStackable);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[riftstalker] mark attack bonus failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }

    public void OnEventAboutToTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.Target is null || Bonus <= 0 ||
          Riftstalker.MarkBuff is null ||
          evt.Target.Buffs.GetBuff(Riftstalker.MarkBuff) is null)
        {
          return;
        }
        foreach (var damage in evt.DamageBundle)
        {
          damage.AddModifier(new Modifier(Bonus, Fact, ModifierDescriptor.UntypedStackable));
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[riftstalker] mark damage bonus failed.", e);
      }
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
  }

  /// <summary>
  /// Hunter's Feast reward buff: +2 attack and damage against everything
  /// while it lasts. Carried by the feast buff; same handler wiring as the
  /// mark bonus, without the mark gate.
  /// </summary>
  [TypeId(Guids.RiftstalkerFeastBonusComponent)]
  internal class RiftstalkerFeastBonus : UnitFactComponentDelegate,
    IGlobalRulebookHandler<RuleCalculateAttackBonus>, IRulebookHandler<RuleCalculateAttackBonus>,
    IGlobalRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    ISubscriber, IGlobalRulebookSubscriber
  {
    public void OnEventAboutToTrigger(RuleCalculateAttackBonus evt)
    {
      try
      {
        if (evt.Initiator != Owner)
        {
          return;
        }
        evt.AddModifier(2, Fact, ModifierDescriptor.UntypedStackable);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[riftstalker] feast attack bonus failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCalculateAttackBonus evt) { }

    public void OnEventAboutToTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner)
        {
          return;
        }
        foreach (var damage in evt.DamageBundle)
        {
          damage.AddModifier(new Modifier(2, Fact, ModifierDescriptor.UntypedStackable));
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[riftstalker] feast damage bonus failed.", e);
      }
    }

    public void OnEventDidTrigger(RulePrepareDamage evt) { }
  }
}

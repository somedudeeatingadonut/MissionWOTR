using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Shining Knight (tabletop port - Advanced Player's Guide pg. 117;
  /// 0.25.0, the user's tabletop pick for the paladin). "While paladins
  /// often are seen mounted atop a loyal steed, the shining knight is
  /// the true symbol of mounted bravery. They are never far from their
  /// steeds and are always clad in brightly polished armor."
  ///
  /// Coverage check: vanilla WOTR paladin archetypes are Divine
  /// Guardian, Divine Hunter, Divine Scion, Hospitaler, Martyr,
  /// Stonelord, Tortured Crusader and Warrior of the Holy Light; the
  /// content mods add none. The tabletop Shining Knight is unclaimed
  /// (and deliberately contrasts this version's other paladin, the
  /// chaos-homebrew Fortune's Fool: the knight of chivalric order
  /// against the knight of entropy).
  ///
  /// The tabletop kit, and its Wrath adaptations (every cut
  /// documented, per house rules):
  /// - Skilled Rider (3rd, replaces Divine Health): "a shining knight
  ///   does not take any penalty to her Ride skill due to her armor
  ///   check penalty. In addition, any mount she is riding gains the
  ///   benefit of her divine grace class feature, adding her Charisma
  ///   bonus (if any) to its saving throws." WOTR has NO Ride skill -
  ///   the armor clause is void and documented-cut. The divine-grace
  ///   clause is implemented whole: a tick-managed grant of the
  ///   VANILLA DivineGrace feature to every pet she owns (the
  ///   SisterDragonAura tick idiom over Owner.Pets, the
  ///   SetPetMinimumStat pet-enumeration precedent) - granted while
  ///   the feature is hers, removed with it.
  /// - Divine Bond (5th): "must form a bond with a mount." The vanilla
  ///   PaladinDivineBondSelection (weapon OR mount) is removed and the
  ///   vanilla PaladinDivineMountSelection is granted as a fixed pick
  ///   - the bond is the horse, no choice.
  /// - Knight's Charge (11th, replaces Aura of Justice): "whenever a
  ///   mounted shining knight charges a foe, her movement does not
  ///   provoke attacks of opportunity, for either her or her mount. In
  ///   addition, if her target is also the target of her smite evil
  ///   ability and the charge attack hits, the target must make a Will
  ///   save or be panicked for a number of rounds equal to 1/2 the
  ///   shining knight's level. The DC of this save is equal to 10 +
  ///   1/2 the shining knight's level + the shining knight's Charisma
  ///   modifier." The panic rider is implemented whole (charge
  ///   detection via evt.IsCharge - the TTT OnCharge pattern; the save
  ///   via a triggered RuleSavingThrow - the TTT DisjointEnchantments
  ///   pattern; panicked = the vanilla Eyebite panic buff). The
  ///   no-provoke clause is a documented engine cut: WOTR exposes no
  ///   "this movement does not provoke" fact for charges.
  /// - The smite-target check reads the vanilla SmiteEvilBuff's
  ///   context target - best-effort and documented: if the buff is
  ///   absent or carries no target, the rider simply does not fire.
  /// Log prefix: [shining].
  /// </summary>
  internal static class ShiningKnight
  {
    internal const string ArchetypeName = "ShiningKnightArchetype";

    public static void Configure()
    {
      var paladin = CharacterClassRefs.PaladinClass.Reference.Get();

      // ----- Skilled Rider (3rd): her mount rides under her grace -----
      var skilledRider = FeatureConfigurator.New(
        "ShiningSkilledRiderFeature", Guids.ShiningSkilledRiderFeature)
        .SetDisplayName("ShiningSkilledRider.Name")
        .SetDescription("ShiningSkilledRider.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new ShiningSkilledRider())
        .Configure();

      // ----- Knight's Charge (11th): the charge that breaks morale -----
      var knightsCharge = FeatureConfigurator.New(
        "ShiningKnightsChargeFeature", Guids.ShiningKnightsChargeFeature)
        .SetDisplayName("ShiningKnightsCharge.Name")
        .SetDescription("ShiningKnightsCharge.Description")
        .SetIcon(AbilityRefs.HorridWilting.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new ShiningKnightsCharge())
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.ShiningKnightArchetype, CharacterClassRefs.PaladinClass)
          .SetLocalizedName("ShiningKnight.Name")
          .SetLocalizedDescription("ShiningKnight.Description")
          .AddToAddFeatures(LevelPlan.L(3), skilledRider)
          .AddToAddFeatures(LevelPlan.L(11), knightsCharge);

      // The 5th-level bond: the weapon option is traded away, the
      // mount option becomes the fixed pick (found at whatever level
      // the live progression grants the selection - FindLevel).
      var bondLevel = ArchetypeRemovals.FindLevel(
        paladin.Progression, FeatureSelectionRefs.PaladinDivineBondSelection.ToString());
      archetype = archetype
        .AddToRemoveFeatures(bondLevel, FeatureSelectionRefs.PaladinDivineBondSelection.ToString())
        .AddToAddFeatures(bondLevel, FeatureSelectionRefs.PaladinDivineMountSelection.ToString());

      // The trades: Divine Health (3rd) and Aura of Justice (11th).
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, paladin,
        FeatureRefs.DivineHealth.ToString(),
        FeatureRefs.AuraOfJusticeFeature.ToString());

      archetype.Configure();

      MissionFeats.Logger.Info("ShiningKnight: configured.");
    }
  }

  /// <summary>
  /// Skilled Rider's grace: every pet she owns carries her Divine Grace
  /// (the vanilla feature, granted and removed with this one; refreshed
  /// on the round tick so a later-acquired mount is not missed).
  /// </summary>
  [TypeId(Guids.ShiningSkilledRiderComponent)]
  internal class ShiningSkilledRider : UnitFactComponentDelegate, Kingmaker.Controllers.Units.ITickEachRound
  {
    private static BlueprintFeature _grace;

    public void OnNewRound()
    {
      try
      {
        Grant();
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[shining] skilled rider tick failed.", e);
      }
    }

    protected override void OnActivate()
    {
      Grant();
    }

    protected override void OnDeactivate()
    {
      try
      {
        if (_grace is null)
        {
          return;
        }
        foreach (var petRef in Owner.Pets)
        {
          var pet = petRef.Entity;
          var fact = pet?.GetFact(_grace);
          if (fact is not null)
          {
            pet.RemoveFact(fact);
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[shining] grace removal failed.", e);
      }
    }

    private void Grant()
    {
      try
      {
        _grace ??= FeatureRefs.DivineGrace.Reference.Get();
        if (_grace is null)
        {
          return;
        }
        foreach (var petRef in Owner.Pets)
        {
          var pet = petRef.Entity;
          if (pet is not null && pet.GetFact(_grace) is null)
          {
            pet.AddFact(_grace);
            MissionFeats.Logger.Info($"[shining] divine grace granted to {pet.CharacterName}.");
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[shining] grace grant failed.", e);
      }
    }
  }

  /// <summary>
  /// Knight's Charge: a hit landed as a CHARGE attack while mounted,
  /// against the target of her smite evil, forces a Will save (DC 10 +
  /// half level + Cha) or panics for half-level rounds (the vanilla
  /// Eyebite panic buff). Mounted is checked via her UnitPartRider's
  /// saddled mount (the TTT MountedCombatFixes API); the smite target
  /// is read from the vanilla SmiteEvilBuff's context, best-effort.
  /// </summary>
  [TypeId(Guids.ShiningKnightsChargeComponent)]
  internal class ShiningKnightsCharge : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    private static BlueprintBuff _smiteBuff;
    private static BlueprintBuff _panicBuff;

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner)
        {
          return; // only her hits
        }
        var roll = evt.AttackRoll;
        if (roll is null || !roll.IsHit || !roll.IsCharge || roll.Weapon is null)
        {
          return; // only weapon-charge hits
        }
        if (Owner.Get<UnitPartRider>()?.SaddledUnit is null)
        {
          return; // only while mounted (the TTT rider-part check)
        }
        _smiteBuff ??= BlueprintTool.Get<BlueprintBuff>("b6570b8cbb32eaf4ca8255d0ec3310b0"); // SmiteEvilBuff
        var smite = Owner.Buffs.GetBuff(_smiteBuff);
        if (smite?.Context?.MainTarget?.Unit != evt.Target)
        {
          return; // not the target of her smite - best-effort, documented
        }

        int level = Owner.Descriptor.Progression.GetClassLevel(
          CharacterClassRefs.PaladinClass.Reference.Get());
        int cha = (Owner.Stats.Charisma.ModifiedValue - 10) / 2;
        int dc = 10 + level / 2 + cha;

        var save = new RuleSavingThrow(evt.Target, SavingThrowType.Will, dc);
        Game.Instance.Rulebook.TriggerEvent(save);
        if (save.IsPassed)
        {
          CombatLog.Write("The charge breaks against her target's will.", Owner);
          return;
        }

        _panicBuff ??= BlueprintTool.Get<BlueprintBuff>("cf0e277e6b785f449bbaf4e993b556e0"); // EyebitePanickedBuff
        evt.Target.Descriptor.AddBuff(
          _panicBuff, Fact.MaybeContext, new Rounds(Math.Max(1, level / 2)).Seconds);
        CombatLog.Write("The shining charge shatters morale - her quarry panics.", Owner);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[shining] knight's charge failed.", e);
      }
    }
  }
}

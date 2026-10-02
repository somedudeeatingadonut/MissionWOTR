using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Sacred Shield — Advanced Player's Guide pg. 116. The paladin who stops
  /// smiting and starts standing in the way.
  ///
  /// 0.54.0 replacement for the Shining Knight, withdrawn in 0.53.0 as a
  /// duplicate of PrestigePlus. Trap-checked against the loaded-mod list in
  /// the 0.52.1 log: vanilla ships Divine Guardian, Divine Hunter, Divine
  /// Scion, Hospitaler, Martyr, Stonelord, Tortured Crusader, Warrior of the
  /// Holy Light and Crusader; HomebrewArchetypes ships FaithfulWanderer,
  /// HolyGuide, Oath of the People's Council, Oath of Vengeance and
  /// Wilderness Warden; PrestigePlus ships Shining Knight and Divine
  /// Champion; Expanded Content ships Divine Scourge, Silver Champion,
  /// Temple Champion, Conqueror and Faithful Paragon. Sacred Shield is in
  /// none of them.
  ///
  /// THE TRADE: smite evil, every grant of it. Removed by NAME at every level
  /// the live progression grants one rather than by guid, which is what the
  /// 0.53.0 teamwork-feat fix established — the hardcoded guid route silently
  /// skips when the id churns, and smite is re-granted several times as its
  /// daily uses climb.
  ///
  /// 0.55.0 — IN HARM'S WAY IS REAL. It shipped in 0.54.0 as a documented cut
  /// on the assumption that intercepting damage needed a hand-rolled transfer.
  /// It does not: the probe of the game DLL shows RuleDealDamage carries two
  /// public fields, RedirectedPercent and RedirectionTarget, plus a
  /// RedirectedDamage property. The engine already knows how to move damage
  /// from one unit to another, so this sets those two fields in the damage
  /// rule's about-to-trigger and lets the engine do the arithmetic. That is
  /// both less code and more correct than the manual conservation the
  /// withdrawn Intercessor did by hand.
  ///
  /// 0.56.0 — BASTION OF FAITH'S DAMAGE REDUCTION IS REAL. The 0.54.0/0.55.0
  /// notes called DR 5/evil a documented cut on the grounds that the
  /// configurator surface did not expose an alignment-qualified physical
  /// resistance. It does. The probe of the game DLL shows
  /// AddDamageResistancePhysical carrying public BypassedByAlignment and
  /// Alignment fields alongside the material ones, plus a
  /// CheckBypassedByAlignment(damage) method that the engine calls for us.
  /// Nothing left is cut from this archetype.
  /// </summary>
  internal static class SacredShield
  {
    internal const string ArchetypeName = "SacredShieldArchetype";

    public static void Configure()
    {
      var paladin = CharacterClassRefs.PaladinClass.Reference.Get();
      var icon = FeatureRefs.SmiteEvilFeature.Reference.Get().Icon;

      // ----- Guarded: the mark she lays on the ally she is standing for ----
      // The rider rides the BUFF, not the paladin — a component on her would
      // only ever see damage dealt to her, and the whole point is the damage
      // going to somebody else. This is the shape the withdrawn Intercessor
      // used for the same problem.
      var guarded = BuffConfigurator.New("SacredShieldGuarded", Guids.SacredShieldGuardedBuff)
        .SetDisplayName("SacredShieldGuarded.Name")
        .SetDescription("SacredShieldGuarded.Description")
        .SetIcon(icon)
        .AddComponent(new SacredShieldGuardedRider())
        .Configure();

      // ----- In Harm's Way (3rd): the action that lays the mark ----
      var inHarmsWay = AbilityConfigurator.New(
          "SacredShieldInHarmsWayAbility", Guids.SacredShieldInHarmsWayAbility)
        .SetDisplayName("InHarmsWay.Name")
        .SetDescription("InHarmsWay.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        // Touch stands in for the tabletop's "adjacent" - the engine has no
        // adjacency range, and Touch is the closest thing to it.
        .SetRange(AbilityRange.Touch)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetFriends()
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().ApplyBuff(guarded, ContextDuration.Fixed(1)))
        .Configure();

      var inHarmsWayFeature = FeatureConfigurator.New(
          "SacredShieldInHarmsWayFeature", Guids.SacredShieldInHarmsWayFeature)
        .SetDisplayName("InHarmsWay.Name")
        .SetDescription("InHarmsWay.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { inHarmsWay })
        .Configure();

      // Sacred Shield (3rd): the shield becomes the point.
      var guard = FeatureConfigurator.New("SacredShieldGuard", Guids.SacredShieldGuard)
        .SetDisplayName("SacredShieldGuard.Name")
        .SetDescription("SacredShieldGuard.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.AC, value: 2, descriptor: ModifierDescriptor.Shield)
        .Configure();

      // Bastion of Faith (11th): sacred, so it stacks with the shield bonus
      // rather than replacing it.
      //
      // DR 5/evil. Verified against the game DLL rather than assumed: the
      // component's alignment fields are public, and the engine's own
      // CheckBypassedByAlignment(damage) decides whether a given hit gets
      // through, so the evil-alignment carve-out is Owlcat's arithmetic and
      // not ours. Strong, as the user said it would be - but it is the
      // tabletop's actual Bastion of Faith, and a paladin archetype is
      // allowed to be strong.
      var bastion = FeatureConfigurator.New("SacredShieldBastion", Guids.BastionOfFaith)
        .SetDisplayName("BastionOfFaith.Name")
        .SetDescription("BastionOfFaith.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.SaveFortitude, value: 2, descriptor: ModifierDescriptor.Sacred)
        .AddStatBonus(stat: StatType.SaveReflex, value: 2, descriptor: ModifierDescriptor.Sacred)
        .AddStatBonus(stat: StatType.SaveWill, value: 2, descriptor: ModifierDescriptor.Sacred)
        .AddDamageResistancePhysical(
          value: 5, bypassedByAlignment: true, alignment: DamageAlignment.Evil)
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.SacredShieldArchetype,
            CharacterClassRefs.PaladinClass)
          .SetLocalizedName("SacredShield.Name")
          .SetLocalizedDescription("SacredShield.Description")
          .AddToAddFeatures(LevelPlan.L(3), inHarmsWayFeature)
          .AddToAddFeatures(LevelPlan.L(3), guard)
          .AddToAddFeatures(LevelPlan.L(11), bastion);

      archetype = ArchetypeRemovals.RemoveEveryGrant(archetype, paladin, "Smite");
      ArchetypeRemovals.DumpProgression(paladin, "Smite");

      archetype.Configure();
      MissionFeats.Logger.Info("[sacredshield] configured: " + ArchetypeName + ".");
    }
  }

  /// <summary>
  /// In Harm's Way, the engine's way. While the guarded ally wears this buff,
  /// damage that would land on her is redirected to the paladin who laid it.
  ///
  /// RuleDealDamage exposes RedirectedPercent and RedirectionTarget as public
  /// fields (probe-verified against the game DLL), so the transfer is the
  /// engine's own rather than a hand-rolled conservation calculation. Setting
  /// them in the about-to-trigger is the same hook Bonewatch's Last Order uses
  /// for MinHPAfterDamage.
  /// </summary>
  [TypeId(Guids.SacredShieldGuardedRider)]
  internal class SacredShieldGuardedRider : UnitBuffComponentDelegate,
    ITargetRulebookHandler<RuleDealDamage>
  {
    public void OnEventAboutToTrigger(RuleDealDamage evt)
    {
      try
      {
        if (evt is null || evt.Target != Owner)
        {
          return;
        }
        // The paladin who laid the mark. A buff's component reaches its
        // caster through the fact's context - the Anatomist's idiom for
        // "is this my mark".
        var guardian = Fact?.MaybeContext?.MaybeCaster;
        if (guardian is null || guardian == Owner || guardian.HPLeft <= 0)
        {
          return; // nobody to take it, or she is already down
        }
        evt.RedirectionTarget = guardian;
        evt.RedirectedPercent = 100;
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[sacredshield] in harm's way failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleDealDamage evt) { }
  }
}

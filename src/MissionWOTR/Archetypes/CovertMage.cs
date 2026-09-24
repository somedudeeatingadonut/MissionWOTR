using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Covert Mage (faithful port of the Flaming Crab Games arcanist archetype;
  /// WOTR adaptations noted in docs/ARCHETYPES.md).
  ///
  /// Tabletop:
  /// - Class skills: Bluff, Disguise, Perception, Perform, Sleight of Hand, Stealth.
  /// - Light armor proficiency; casts arcanist spells in light armor without arcane
  ///   spell failure.
  /// - Mesmerizing Touch (3rd): spend 1 reservoir point, melee touch, target takes a
  ///   Will-save penalty equal to arcanist level for Cha-mod rounds (min 1).
  ///   Replaces the exploit gained at 3rd level.
  /// - Spell Trick (7th): casting a 1-standard-action spell at an adjacent target
  ///   allows a feint (Bluff); on success the spell does not provoke an AoO from the
  ///   target and the target saves twice, taking the lesser result.
  ///   Replaces the exploit gained at 7th level.
  /// - Illusion Spotter (11th): near-illusion free disbelieve saves.
  ///   Replaces the exploit gained at 11th level.
  ///
  /// WOTR adaptations (engine gaps):
  /// - WOTR has no Bluff/Disguise/Perform/Sleight of Hand skills: class skills become
  ///   Persuasion, Perception, Stealth and Thievery.
  /// - Spell-failure immunity uses the engine's own ArcaneSpellFailureIncrease
  ///   component at -20 (the maximum ASF of any light armor), so heavier armor
  ///   (which arcanists are not proficient in anyway) still fails.
  /// - Feint check: Persuasion (Bluff stand-in) vs 15 + target Perception
  ///   (Sense Motive stand-in; WOTR exposes neither Bluff nor BAB).
  /// - No-provoke is delivered as a 1-round "Feinted" debuff that zeroes the
  ///   target's attacks of opportunity (the ConstructCrafter NoAoO pattern).
  /// - Save-twice-take-lesser uses the engine's d20 reroll hook (the same mechanism
  ///   as the vanilla Azata Favorable Magic ability).
  /// - Illusion Spotter: WOTR has no generic illusion-disbelief system, so "free
  ///   disbelieve save" becomes: her saves against illusion-school effects are
  ///   rolled twice and take the better result.
  /// - The tabletop spellbook clause (two free spells/level) is a no-op in WOTR,
  ///   where arcanists prepare from the full list.
  /// </summary>
  internal static class CovertMage
  {
    internal const string ArchetypeName = "CovertMageArchetype";
    internal const string DisplayName = "CovertMage.Name";
    internal const string Description = "CovertMage.Description";

    internal const string TrainingName = "CovertMageTraining";
    internal const string MesmerizingTouchName = "CovertMageMesmerizingTouch";
    internal const string MesmerizingDeliveryName = "CovertMageMesmerizingTouchDelivery";
    internal const string MesmerizingDebuffName = "CovertMageMesmerizingTouchDebuff";
    internal const string SpellTrickName = "CovertMageSpellTrick";
    internal const string IllusionSpotterName = "CovertMageIllusionSpotter";
    internal const string FeintedBuffName = "CovertMageFeintedBuff";

    // Vanilla blueprints (TabletopTweaks-verified GUIDs).
    private const string ArcanistExploitSelectionGuid = "b8bf3d5023f2d8c428fdf6438cecaea7";
    private const string ArcaneReservoirResourceGuid = "cac948cbbe79b55459459dd6a8fe44ce";

    internal static BlueprintBuff FeintedBuff;
    internal static BlueprintBuff MesmerizingDebuff;

    public static void Configure()
    {
      var arcanist = CharacterClassRefs.ArcanistClass.Reference.Get();

      // ----- Feinted: 1-round debuff, zeroes attacks of opportunity -----
      FeintedBuff = BuffConfigurator.New(FeintedBuffName, Guids.CovertMageFeintedBuff)
        .SetDisplayName("CovertMageFeinted.Name")
        .SetDescription("CovertMageFeinted.Description")
        .SetIcon(FeatureRefs.CriticalFocus.Reference.Get().Icon)
        .AddContextStatBonus(
          StatType.AttackOfOpportunityCount,
          ContextValues.Constant(-50), ModifierDescriptor.Penalty)
        .Configure();

      // ----- Mesmerizing Touch debuff: Will penalty equal to caster's arcanist level
      MesmerizingDebuff = BuffConfigurator.New(MesmerizingDebuffName, Guids.CovertMageMesmerizingDebuff)
        .SetDisplayName("CovertMageMesmerizingDebuff.Name")
        .SetDescription("CovertMageMesmerizingDebuff.Description")
        .SetIcon(BuffRefs.Confusion.Reference.Get().Icon)
        .AddComponent(new CovertMageMesmerizingPenalty { CharacterClass = arcanist })
        .Configure();

      // ----- Mesmerizing Touch payload (fires on a successful touch attack) -----
      var delivery = AbilityConfigurator.New(MesmerizingDeliveryName, Guids.CovertMageMesmerizingDelivery)
        .SetDisplayName("CovertMageMesmerizingTouch.Name")
        .SetDescription("CovertMageMesmerizingTouch.Description")
        .SetIcon(BuffRefs.Confusion.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Touch)
        .SetCanTargetEnemies()
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().Add(new CovertMageMesmerizingAction { Debuff = MesmerizingDebuff }))
        .Configure();

      // ----- Mesmerizing Touch (the cast): 1 arcane reservoir point, touch attack -----
      var reservoir = BlueprintTool.Get<BlueprintAbilityResource>(ArcaneReservoirResourceGuid);
      var mesmerizingTouch = AbilityConfigurator.New(MesmerizingTouchName, Guids.CovertMageMesmerizingAbility)
        .SetDisplayName("CovertMageMesmerizingTouch.Name")
        .SetDescription("CovertMageMesmerizingTouch.Description")
        .SetIcon(BuffRefs.Confusion.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Touch)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetEnemies()
        .AddAbilityResourceLogic(requiredResource: reservoir, amount: 1, isSpendResource: true)
        .AddAbilityDeliverTouch(touchWeapon: ItemWeaponRefs.TouchItem.ToString())
        .AddAbilityEffectStickyTouch(touchDeliveryAbility: delivery)
        .Configure();

      // ----- Covert Training (1st): class skills + light armor + no light-armor ASF -----
      var training = FeatureConfigurator.New(TrainingName, Guids.CovertMageTraining)
        .SetDisplayName("CovertMageTraining.Name")
        .SetDescription("CovertMageTraining.Description")
        .SetIcon(FeatureRefs.LightArmorProficiency.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new AddClassSkill { Skill = StatType.SkillPersuasion })
        .AddComponent(new AddClassSkill { Skill = StatType.SkillPerception })
        .AddComponent(new AddClassSkill { Skill = StatType.SkillStealth })
        .AddComponent(new AddClassSkill { Skill = StatType.SkillThievery })
        .AddFacts(new() { FeatureRefs.LightArmorProficiency.Reference.Get() })
        .AddComponent(new ArcaneSpellFailureIncrease { Bonus = -20, ToShield = false })
        .Configure();

      // ----- Mesmerizing Touch feature (3rd; replaces the 3rd-level exploit) -----
      var mesmerizingFeature = FeatureConfigurator.New(MesmerizingTouchName, Guids.CovertMageMesmerizingFeature)
        .SetDisplayName("CovertMageMesmerizingTouch.Name")
        .SetDescription("CovertMageMesmerizingTouch.Description")
        .SetIcon(BuffRefs.Confusion.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { mesmerizingTouch })
        .Configure();

      // ----- Spell Trick (7th; replaces the 7th-level exploit) -----
      var spellTrick = FeatureConfigurator.New(SpellTrickName, Guids.CovertMageSpellTrickFeature)
        .SetDisplayName("CovertMageSpellTrick.Name")
        .SetDescription("CovertMageSpellTrick.Description")
        .SetIcon(FeatureRefs.CriticalFocus.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new CovertMageSpellTrick { NoAoO = FeintedBuff })
        .Configure();

      // ----- Illusion Spotter (11th; replaces the 11th-level exploit) -----
      var illusionSpotter = FeatureConfigurator.New(IllusionSpotterName, Guids.CovertMageIllusionSpotterFeature)
        .SetDisplayName("CovertMageIllusionSpotter.Name")
        .SetDescription("CovertMageIllusionSpotter.Description")
        .SetIcon(FeatureRefs.CombatReflexes.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new CovertMageIllusionSpotter())
        .Configure();

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.CovertMageArchetype, CharacterClassRefs.ArcanistClass)
          .SetLocalizedName(DisplayName)
          .SetLocalizedDescription(Description);

      // Trades the exploits gained at 3rd, 7th and 11th level (the selection appears
      // at every odd level, so these removals are level-specific on purpose - the
      // generic ArchetypeRemovals helper would match its first occurrence).
      archetype = archetype
        .AddToRemoveFeatures(3, ArcanistExploitSelectionGuid)
        .AddToRemoveFeatures(7, ArcanistExploitSelectionGuid)
        .AddToRemoveFeatures(11, ArcanistExploitSelectionGuid);

      archetype = archetype
        .AddToAddFeatures(LevelPlan.L(1), TrainingName)
        .AddToAddFeatures(LevelPlan.L(3), MesmerizingTouchName)
        .AddToAddFeatures(LevelPlan.L(7), SpellTrickName)
        .AddToAddFeatures(LevelPlan.L(11), IllusionSpotterName);

      archetype.Configure();

      MissionFeats.Logger.Info("CovertMage: configured.");
    }
  }

  /// <summary>
  /// Applies the Mesmerizing Touch debuff for max(1, caster Cha mod) rounds.
  /// </summary>
  [TypeId(Guids.CovertMageMesmerizingAction)]
  internal class CovertMageMesmerizingAction : ContextAction
  {
    public BlueprintBuff Debuff;

    public override string GetCaption() => "Mesmerizing Touch";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        var target = Target.Unit;
        if (caster is null || target is null || target.HPLeft <= 0 || Debuff is null)
        {
          return;
        }
        int rounds = Math.Max(1, (caster.Stats.Charisma.Value - 10) / 2);
        target.AddBuff(
          Debuff, Context, duration: ContextDuration.Fixed(rounds).Calculate(Context).Seconds);
        MissionFeats.Logger.Info(
          $"[covert-mage] Mesmerizing Touch: {target.CharacterName} takes a Will penalty for {rounds} round(s).");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("CovertMage: Mesmerizing Touch apply failed.", e);
      }
    }
  }

  /// <summary>
  /// Mesmerizing Touch debuff: -arcanist level on Will saves while it lasts. The
  /// level is read from the caster that applied the buff (the buff's context).
  /// </summary>
  [TypeId(Guids.CovertMageMesmerizingPenalty)]
  internal class CovertMageMesmerizingPenalty : UnitFactComponentDelegate
  {
    public BlueprintCharacterClass CharacterClass;

    private ModifiableValue.Modifier m_Modifier;

    protected override void OnTurnOn()
    {
      try
      {
        var caster = Context?.MaybeCaster;
        int level = caster?.Descriptor.Progression.GetClassLevel(CharacterClass) ?? 1;
        m_Modifier = Owner.Stats.SaveWill.AddModifier(-level, Runtime, ModifierDescriptor.Penalty);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("CovertMage: Mesmerizing penalty failed.", e);
      }
    }

    protected override void OnTurnOff()
    {
      try
      {
        m_Modifier?.Remove();
        m_Modifier = null;
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("CovertMage: Mesmerizing penalty removal failed.", e);
      }
    }
  }

  /// <summary>
  /// Spell Trick: when casting a 1-standard-action spell at an adjacent enemy, roll
  /// a feint (Persuasion vs 15 + target Perception). On success the target is
  /// "Feinted" (cannot make attacks of opportunity for 1 round - the spell does not
  /// provoke) and its saving throw against that spell is rolled twice, taking the
  /// lesser result (the Favorable Magic reroll hook).
  /// </summary>
  [TypeId(Guids.CovertMageSpellTrick)]
  internal class CovertMageSpellTrick : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCastSpell>, IRulebookHandler<RuleCastSpell>,
    IInitiatorRulebookSubscriber, IWasRoll, IGlobalSubscriber, ISubscriber
  {
    public BlueprintBuff NoAoO;

    /// <summary>Spells whose target was successfully feinted, by cast instance.</summary>
    private static readonly Dictionary<AbilityData, string> Marked = new();

    public void OnEventAboutToTrigger(RuleCastSpell evt)
    {
      try
      {
        var spell = evt.Spell;
        if (spell?.Blueprint is null)
        {
          return;
        }
        if (spell.Blueprint.Type != AbilityType.Spell)
        {
          return;
        }
        if (spell.Blueprint.ActionType != UnitCommand.CommandType.Standard)
        {
          return;
        }
        var target = evt.SpellTarget?.Unit;
        if (target is null || target.HPLeft <= 0 || !target.IsEnemy(Owner))
        {
          return;
        }
        // Adjacent: within 5 feet.
        if (Vector3.Distance(Owner.Position, target.Position) > 1.6f)
        {
          return;
        }
        // Feint: Persuasion vs 15 + target Perception (Bluff vs 10 + BAB + Sense
        // Motive on the tabletop; WOTR exposes neither Bluff nor BAB).
        int dc = 15 + target.Stats.SkillPerception.Value;
        var check = new RuleSkillCheck(Owner, StatType.SkillPersuasion, dc);
        Rulebook.Trigger(check);
        if (!check.Success)
        {
          return;
        }
        if (Marked.Count > 8)
        {
          Marked.Clear();
        }
        Marked[spell] = target.UniqueId;
        if (NoAoO != null)
        {
          target.AddBuff(
            NoAoO, Context, duration: ContextDuration.Fixed(1).Calculate(Context).Seconds);
        }
        MissionFeats.Logger.Info(
          $"[covert-mage] Spell Trick: feint succeeded vs {target.CharacterName} " +
          $"(DC {dc}) - {spell.Blueprint.name} will not provoke and saves twice.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("CovertMage: Spell Trick failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleCastSpell evt) { }

    public void WasRoll(RulebookEvent ruleEvent, RuleRollD20 ruleRoll)
    {
      try
      {
        if (!(ruleEvent is RuleSavingThrow save))
        {
          return;
        }
        if (ruleRoll.Reason.Caster != Owner)
        {
          return;
        }
        var ability = save.Reason.Ability;
        if (ability is null || !Marked.TryGetValue(ability, out var targetUid)
          || save.Initiator.UniqueId != targetUid)
        {
          return;
        }
        Marked.Remove(ability);
        ruleRoll.Reroll(Fact, takeBest: false);
        MissionFeats.Logger.Info(
          "[covert-mage] Spell Trick: the target saves twice and takes the lesser result.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("CovertMage: Spell Trick reroll failed.", e);
      }
    }
  }

  /// <summary>
  /// Illusion Spotter: her saving throws against illusion-school effects are rolled
  /// twice and take the better result (the tabletop's free disbelieve save).
  /// </summary>
  [TypeId(Guids.CovertMageIllusionSpotter)]
  internal class CovertMageIllusionSpotter : UnitFactComponentDelegate,
    IWasRoll, IGlobalSubscriber, ISubscriber
  {
    public void WasRoll(RulebookEvent ruleEvent, RuleRollD20 ruleRoll)
    {
      try
      {
        if (!(ruleEvent is RuleSavingThrow save))
        {
          return;
        }
        if (save.Initiator != Owner)
        {
          return;
        }
        var ability = save.Reason.Ability?.Blueprint ?? save.Reason.Context?.SourceAbility;
        var school = ability?.GetComponent<SpellComponent>()?.School;
        if (school == SpellSchool.Illusion)
        {
          ruleRoll.Reroll(Fact, takeBest: true);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("CovertMage: Illusion Spotter failed.", e);
      }
    }
  }
}

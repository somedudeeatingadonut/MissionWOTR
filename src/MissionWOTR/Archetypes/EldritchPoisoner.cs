using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Eldritch Poisoner (Alchemist archetype, tabletop port from Pathfinder Player Companion:
  /// Black Markets) - a master of toxic arts who trades bombs, Throw Anything and mutagen for
  /// a supernatural poison all her own.
  ///
  /// REAL LEVEL PLAN (used when LevelPlan.AllAtLevelOne is false):
  ///   L1  Arcanotoxin (replaces bomb) - brew doses (level + Int per day), coat weapon,
  ///       weapon hits deliver: Fort save DC 10 + 1/2 alch level + Int or 1d2 Str damage
  ///   L1  Toxicologist (replaces Throw Anything) - +2 Lore (Nature)
  ///   L1  Sneak attack 1d6 (replaces mutagen; also replaces persistent mutagen)
  ///   L4/8/12/16/20  Sneak attack +1d6 each (RogueSneakAttack ranks)
  ///   (Future, per tabletop: Careful Injection at L4 replaces the 4th-level discovery;
  ///    arcanotoxin discoveries - Sickening, Mind-Altering, Paralytic, Lethal, etc.)
  ///
  /// WRATH ADAPTATIONS (documented in docs/ARCHETYPES.md):
  ///   - Arcanotoxin is SUPERNATURAL: it bypasses poison immunity entirely, but creatures
  ///     immune to poison resist it partially and receive a +4 bonus on the saving throw.
  ///   - V1 applies the damage once on a failed save (2-round lockout on the target)
  ///     instead of the tabletop's recurring 1/round-for-2-rounds frequency; the game's
  ///     BuffPoisonStatDamage component can restore full frequency later.
  /// </summary>
  public class EldritchPoisoner
  {
    internal const string ArchetypeName = "EldritchPoisoner";
    internal const string DisplayName = "EldritchPoisoner.Name";
    internal const string Description = "EldritchPoisoner.Description";

    internal const string ArcanotoxinFeatName = "EldritchPoisonerArcanotoxin";
    internal const string ArcanotoxinDisplayName = "EldritchPoisonerArcanotoxin.Name";
    internal const string ArcanotoxinDescription = "EldritchPoisonerArcanotoxin.Description";
    internal const string DosesResourceName = "EldritchPoisonerToxinDoses";
    internal const string BrewAbilityName = "EldritchPoisonerBrewToxin";
    internal const string BrewDisplayName = "EldritchPoisonerBrewToxin.Name";
    internal const string BrewDescription = "EldritchPoisonerBrewToxin.Description";
    internal const string SwiftBrewAbilityName = "EldritchPoisonerSwiftBrew";
    internal const string SwiftBrewDisplayName = "EldritchPoisonerSwiftBrew.Name";
    internal const string SwiftBrewDescription = "EldritchPoisonerSwiftBrew.Description";
    internal const string CoatingBuffName = "EldritchPoisonerCoatingBuff";
    internal const string CoatingDisplayName = "EldritchPoisonerCoating.Name";
    internal const string CoatingDescription = "EldritchPoisonerCoating.Description";
    internal const string ToxinBuffName = "EldritchPoisonerToxinDebuff";
    internal const string ToxinDisplayName = "EldritchPoisonerToxin.Name";
    internal const string ToxinDescription = "EldritchPoisonerToxin.Description";
    internal const string ToxicologistFeatName = "EldritchPoisonerToxicologist";
    internal const string ToxicologistDisplayName = "EldritchPoisonerToxicologist.Name";
    internal const string ToxicologistDescription = "EldritchPoisonerToxicologist.Description";
    internal const string MythicName = "ExpeditedSynthesis";
    internal const string MythicDisplayName = "ExpeditedSynthesis.Name";
    internal const string MythicDescription = "ExpeditedSynthesis.Description";

    public static void Configure()
    {
      var icon = AbilityRefs.BombStandart.Reference.Get().Icon;

      // ----- Toxin debuff: 1d2 Strength damage when it lands, 2-round lockout -----
      var toxin = BuffConfigurator.New(ToxinBuffName, Guids.EldritchPoisonerToxinDebuff)
        .SetDisplayName(ToxinDisplayName)
        .SetDescription(ToxinDescription)
        .SetIcon(icon)
        .AddFactContextActions(activated: ActionsBuilder.New().Add(new DealStatDamage
        {
          Stat = StatType.Strength,
          DamageDice = new DiceFormula(1, DiceType.D2),
        }))
        .Configure();

      // ----- Weapon coating: carries the delivery trigger -----
      var coating = BuffConfigurator.New(CoatingBuffName, Guids.EldritchPoisonerCoatingBuff)
        .SetDisplayName(CoatingDisplayName)
        .SetDescription(CoatingDescription)
        .SetIcon(icon)
        .AddComponent(new ArcanotoxinDelivery(toxin))
        .Configure();

      // ----- Dose pool: alchemist level + Intelligence modifier per day -----
      var doses = AbilityResourceConfigurator.New(DosesResourceName, Guids.EldritchPoisonerToxinDoses)
        .SetMaxAmount(
          ResourceAmountBuilder.New(0)
            .IncreaseByLevel(new[] { CharacterClassRefs.AlchemistClass.ToString() }, 1)
            .IncreaseByStat(StatType.Intelligence))
        .Configure();

      // ----- Brew Arcanotoxin: standard action, spend 1 dose, coat for 1 minute -----
      AbilityConfigurator.New(BrewAbilityName, Guids.EldritchPoisonerBrewToxin)
        .SetDisplayName(BrewDisplayName)
        .SetDescription(BrewDescription)
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Standard)
        .AddAbilityResourceLogic(requiredResource: doses, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().ApplyBuff(coating, ContextDuration.Fixed(10), toCaster: true))
        .Configure();

      // ----- Arcanotoxin feature (L1, replaces bomb) -----
      FeatureConfigurator.New(ArcanotoxinFeatName, Guids.EldritchPoisonerArcanotoxin)
        .SetDisplayName(ArcanotoxinDisplayName)
        .SetDescription(ArcanotoxinDescription)
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { BrewAbilityName })
        .AddAbilityResources(resource: doses, restoreAmount: true)
        .Configure();

      // ----- Toxicologist (L1, replaces Throw Anything) -----
      FeatureConfigurator.New(ToxicologistFeatName, Guids.EldritchPoisonerToxicologist)
        .SetDisplayName(ToxicologistDisplayName)
        .SetDescription(ToxicologistDescription)
        .SetIcon(FeatureRefs.AlchemistThrowAnything.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.SkillLoreNature, value: 2, descriptor: ModifierDescriptor.UntypedStackable)
        .Configure();

      // ----- Mythic ability: Expedited Synthesis (swift brew at an HP cost) -----
      AbilityConfigurator.New(SwiftBrewAbilityName, Guids.EldritchPoisonerSwiftBrew)
        .SetDisplayName(SwiftBrewDisplayName)
        .SetDescription(SwiftBrewDescription)
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Swift)
        .AddAbilityResourceLogic(requiredResource: doses, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(
          ActionsBuilder.New()
            .Add(new ContextActionExpeditedSynthesisCost())
            .ApplyBuff(coating, ContextDuration.Fixed(10), toCaster: true))
        .Configure();

      FeatureConfigurator.New(MythicName, Guids.ExpeditedSynthesisAbility)
        .SetDisplayName(MythicDisplayName)
        .SetDescription(MythicDescription)
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { SwiftBrewAbilityName })
        .AddToFeatureSelection(
          FeatureSelectionRefs.MythicAbilitySelection.Cast<BlueprintFeatureSelectionReference>(),
          FeatureSelectionRefs.ExtraMythicAbilityMythicFeat.Cast<BlueprintFeatureSelectionReference>())
        .Configure();

      // ----- The archetype itself -----
      ArchetypeConfigurator.New(ArchetypeName, Guids.EldritchPoisonerArchetype, CharacterClassRefs.AlchemistClass)
        .SetLocalizedName(DisplayName)
        .SetLocalizedDescription(Description)
        // Replaces bomb (all of the class's bomb-granting entries).
        .AddToRemoveFeatures(1,
          FeatureRefs.AlchemistBombsFeature.ToString(),
          FeatureRefs.AlchemistBombs.ToString())
        // Replaces Throw Anything.
        .AddToRemoveFeatures(1, FeatureRefs.AlchemistThrowAnything.ToString())
        // Replaces mutagen (and, implicitly, persistent mutagen which improves it).
        .AddToRemoveFeatures(1, FeatureRefs.AlchemistMutagen.ToString())
        // Test mode: everything lands at L1; with LevelPlan off these are the real levels.
        .AddToAddFeatures(LevelPlan.L(1), ArcanotoxinFeatName, ToxicologistFeatName)
        .AddToAddFeatures(LevelPlan.L(1), FeatureRefs.RogueSneakAttack.ToString())
        .AddToAddFeatures(LevelPlan.L(4), FeatureRefs.RogueSneakAttack.ToString())
        .AddToAddFeatures(LevelPlan.L(8), FeatureRefs.RogueSneakAttack.ToString())
        .AddToAddFeatures(LevelPlan.L(12), FeatureRefs.RogueSneakAttack.ToString())
        .AddToAddFeatures(LevelPlan.L(16), FeatureRefs.RogueSneakAttack.ToString())
        .AddToAddFeatures(LevelPlan.L(20), FeatureRefs.RogueSneakAttack.ToString())
        .Configure();
    }

    /// <summary>
    /// Delivers the arcanotoxin on the owner's weapon hits while the coating is active.
    /// Arcanotoxin is supernatural: it ignores poison immunity, but creatures normally
    /// immune to poison resist it and gain a +4 bonus on the save.
    /// </summary>
    [TypeId(Guids.ArcanotoxinDelivery)]
    private class ArcanotoxinDelivery :
      UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleAttackWithWeapon>
    {
      private static readonly BlueprintCharacterClass AlchemistClass =
        CharacterClassRefs.AlchemistClass.Reference.Get();

      private static readonly BlueprintFeature PoisonImmunityFeature =
        FeatureRefs.ImmunityToPoison.Reference.Get();

      private static readonly BlueprintFeature PoisonImmunityFeatureAlt =
        FeatureRefs.PoisonImmunity.Reference.Get();

      private readonly BlueprintBuff Toxin;

      public ArcanotoxinDelivery(BlueprintBuff toxin)
      {
        Toxin = toxin;
      }

      public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

      public void OnEventDidTrigger(RuleAttackWithWeapon evt)
      {
        try
        {
          // Only actual hits with a weapon...
          if (evt.AttackRoll is null || !evt.AttackRoll.IsHit)
          {
            return;
          }
          var target = evt.Target;
          if (target is null || target.HPLeft <= 0)
          {
            return;
          }
          // No reapplication while the toxin is already in the target's system.
          if (target.Buffs.GetBuff(Toxin) != null)
          {
            return;
          }

          var alchemistLevel = Owner.Descriptor.Progression.GetClassLevel(AlchemistClass);
          var dc = 10 + alchemistLevel / 2 + Owner.Stats.Intelligence.Bonus;

          // Supernatural toxin: bypasses poison immunity, but such creatures resist (+4 save).
          if (target.HasFact(PoisonImmunityFeature) || target.HasFact(PoisonImmunityFeatureAlt))
          {
            dc -= 4;
          }

          var save = new RuleSavingThrow(target, SavingThrowType.Fortitude, dc) { Reason = Fact };
          if (Rulebook.Trigger(save).IsPassed)
          {
            return;
          }

          // 2 rounds of toxin in the veins.
          target.AddBuff(Toxin, Context, duration: ContextDuration.Fixed(2).Calculate(Context).Seconds);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("EldritchPoisoner: failed to deliver arcanotoxin.", e);
        }
      }
    }

    /// <summary>
    /// The price of haste: the caster loses 25% of maximum HP, or only 15% if they succeed
    /// at a DC 15 Fortitude save. Never reduces the caster below 1 HP.
    /// </summary>
    [TypeId(Guids.ExpeditedSynthesisCost)]
    private class ContextActionExpeditedSynthesisCost : ContextAction
    {
      public override string GetCaption() => "Expedited Synthesis HP cost";

      public override void RunAction()
      {
        try
        {
          var unit = Target.Unit;
          if (unit is null)
          {
            return;
          }
          var save = new RuleSavingThrow(unit, SavingThrowType.Fortitude, 15);
          var fraction = Rulebook.Trigger(save).IsPassed ? 0.15 : 0.25;
          var amount = Math.Max(1, (int)Math.Round(unit.Descriptor.MaxHP * fraction));
          // Never let the synthesis kill its own maker.
          amount = Math.Min(amount, Math.Max(0, unit.HPLeft - 1));
          if (amount > 0)
          {
            unit.Descriptor.Damage += amount;
          }
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("EldritchPoisoner: failed to apply synthesis HP cost.", e);
        }
      }
    }
  }
}

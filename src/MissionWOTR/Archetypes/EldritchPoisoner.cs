using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MissionWOTR.Feats;
using System;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Eldritch Poisoner (Alchemist archetype, tabletop port from Pathfinder Player Companion:
  /// Black Markets) - a master of toxic arts who trades bombs, Throw Anything and mutagen for
  /// a supernatural poison all her own. Discoveries live in EldritchPoisonerDiscoveries.cs.
  ///
  /// REAL LEVEL PLAN (used when LevelPlan.AllAtLevelOne is false):
  ///   L1  Arcanotoxin (replaces bomb), Toxicologist (replaces Throw Anything),
  ///       Sneak attack 1d6 (replaces mutagen)
  ///   L4/8/12/16/20  Sneak attack +1d6 each
  ///   L4  Careful Injection (tabletop: replaces the 4th-level discovery)
  ///   Discoveries (Sickening, Mind-Altering, Paralytic, Lethal, Combine, Contact, Envenom,
  ///   Antidote, Apothecary, Toxic Fumes) are ordinary alchemist discovery picks, level-gated
  ///   per the tabletop; they are auto-granted only in test mode.
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
    internal const string ToxinStrDiceName = "EldritchPoisonerToxinStrDice";
    internal const string ToxinStrFlatName = "EldritchPoisonerToxinStrFlat";
    internal const string ToxinDexFlatName = "EldritchPoisonerToxinDexFlat";
    internal const string ToxinConDiceName = "EldritchPoisonerToxinConDice";
    internal const string ToxinConFlatName = "EldritchPoisonerToxinConFlat";
    internal const string ToxinDisplayName = "EldritchPoisonerToxin.Name";
    internal const string ToxinDescription = "EldritchPoisonerToxin.Description";
    internal const string ToxicologistFeatName = "EldritchPoisonerToxicologist";
    internal const string ToxicologistDisplayName = "EldritchPoisonerToxicologist.Name";
    internal const string ToxicologistDescription = "EldritchPoisonerToxicologist.Description";
    internal const string MythicName = "ExpeditedSynthesis";
    internal const string MythicDisplayName = "ExpeditedSynthesis.Name";
    internal const string MythicDescription = "ExpeditedSynthesis.Description";

    // Runtime-accessible blueprints (set during Configure; used by the delivery logic).
    internal static BlueprintBuff Coating;
    internal static BlueprintBuff ToxinStrDice;
    internal static BlueprintBuff ToxinStrFlat;
    internal static BlueprintBuff ToxinDexFlat;
    internal static BlueprintBuff ToxinConDice;
    internal static BlueprintBuff ToxinConFlat;
    internal static BlueprintAbilityResource Doses;

    public static void Configure()
    {
      var icon = AbilityRefs.BombStandart.Reference.Get().Icon;

      // ----- Toxin debuff variants -----
      // Base: 1d2 Strength. Combine Toxins: flat 1 to two scores. Lethal: Constitution.
      ToxinStrDice = NewToxin(ToxinStrDiceName, Guids.EldritchPoisonerToxinDebuff,
        StatType.Strength, new DiceFormula(1, DiceType.D2), 0);
      ToxinStrFlat = NewToxin(ToxinStrFlatName, Guids.EldritchPoisonerToxinStrFlat,
        StatType.Strength, new DiceFormula(0, DiceType.Zero), 1);
      ToxinDexFlat = NewToxin(ToxinDexFlatName, Guids.EldritchPoisonerToxinDexFlat,
        StatType.Dexterity, new DiceFormula(0, DiceType.Zero), 1);
      ToxinConDice = NewToxin(ToxinConDiceName, Guids.EldritchPoisonerToxinConDice,
        StatType.Constitution, new DiceFormula(1, DiceType.D2), 0);
      ToxinConFlat = NewToxin(ToxinConFlatName, Guids.EldritchPoisonerToxinConFlat,
        StatType.Constitution, new DiceFormula(0, DiceType.Zero), 1);

      // ----- Weapon coating: carries the delivery trigger -----
      Coating = BuffConfigurator.New(CoatingBuffName, Guids.EldritchPoisonerCoatingBuff)
        .SetDisplayName(CoatingDisplayName)
        .SetDescription(CoatingDescription)
        .SetIcon(icon)
        .AddComponent<ArcanotoxinDelivery>()
        .Configure();

      // ----- Dose pool: alchemist level + Intelligence modifier per day -----
      Doses = AbilityResourceConfigurator.New(DosesResourceName, Guids.EldritchPoisonerToxinDoses)
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
        .AddAbilityResourceLogic(requiredResource: Doses, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().ApplyBuff(Coating, ContextDuration.Fixed(10), toCaster: true))
        .Configure();

      // ----- Arcanotoxin feature (L1, replaces bomb) -----
      FeatureConfigurator.New(ArcanotoxinFeatName, Guids.EldritchPoisonerArcanotoxin)
        .SetDisplayName(ArcanotoxinDisplayName)
        .SetDescription(ArcanotoxinDescription)
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { BrewAbilityName })
        .AddAbilityResources(resource: Doses, restoreAmount: true)
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
        .AddAbilityResourceLogic(requiredResource: Doses, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(
          ActionsBuilder.New()
            .Add(new ContextActionExpeditedSynthesisCost())
            .ApplyBuff(Coating, ContextDuration.Fixed(10), toCaster: true))
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

      // Discoveries must exist before the archetype references their names.
      EldritchPoisonerDiscoveries.Configure();

      // ----- The archetype itself -----
      var archetype =
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
          .AddToAddFeatures(LevelPlan.L(1), ArcanotoxinFeatName, ToxicologistFeatName)
          .AddToAddFeatures(LevelPlan.L(1), FeatureRefs.RogueSneakAttack.ToString())
          .AddToAddFeatures(LevelPlan.L(4), FeatureRefs.RogueSneakAttack.ToString())
          .AddToAddFeatures(LevelPlan.L(8), FeatureRefs.RogueSneakAttack.ToString())
          .AddToAddFeatures(LevelPlan.L(12), FeatureRefs.RogueSneakAttack.ToString())
          .AddToAddFeatures(LevelPlan.L(16), FeatureRefs.RogueSneakAttack.ToString())
          .AddToAddFeatures(LevelPlan.L(20), FeatureRefs.RogueSneakAttack.ToString());

      if (LevelPlan.AllAtLevelOne)
      {
        // TEST MODE: the whole kit, discoveries included, from the first fight.
        archetype.AddToAddFeatures(
          1,
          EldritchPoisonerDiscoveries.AllFeatNames
            .Select(f => (Blueprint<BlueprintFeatureBaseReference>)f)
            .ToArray());
      }
      else
      {
        // NORMAL MODE: discoveries are ordinary alchemist picks (level-gated per the
        // tabletop); Careful Injection arrives with the 4th-level feature slot.
        archetype.AddToAddFeatures(LevelPlan.L(4), EldritchPoisonerDiscoveries.CarefulInjectionFeatName);
      }

      archetype.Configure();
    }

    private static BlueprintBuff NewToxin(
      string name, string guid, StatType stat, DiceFormula dice, int bonus)
    {
      return BuffConfigurator.New(name, guid)
        .SetDisplayName(ToxinDisplayName)
        .SetDescription(ToxinDescription)
        .SetIcon(AbilityRefs.BombStandart.Reference.Get().Icon)
        .AddFactContextActions(activated: ActionsBuilder.New().Add(new DealStatDamage
        {
          Stat = stat,
          DamageDice = dice,
          DamageBonus = bonus,
        }))
        .Configure();
    }
  }

  /// <summary>
  /// Shared arcanotoxin delivery logic: save DC, immunity bypass, variant selection based
  /// on the poisoner's discoveries, and secondary effects. Used by the weapon-coating
  /// trigger and the thrown-toxin abilities.
  /// </summary>
  internal static class ArcanotoxinApply
  {
    private static BlueprintCharacterClass AlchemistClass;
    private static BlueprintFeature PoisonImmunityA;
    private static BlueprintFeature PoisonImmunityB;

    private static readonly BlueprintBuff[] NoReapplyCache = new BlueprintBuff[5];

    public static void Apply(
      UnitEntityData owner,
      UnitEntityData target,
      MechanicsContext context,
      EntityFact sourceFact,
      int dcModifier,
      bool sneakAttack)
    {
      try
      {
        if (target is null || target.HPLeft <= 0)
        {
          return;
        }

        AlchemistClass ??= CharacterClassRefs.AlchemistClass.Reference.Get();
        PoisonImmunityA ??= FeatureRefs.ImmunityToPoison.Reference.Get();
        PoisonImmunityB ??= FeatureRefs.PoisonImmunity.Reference.Get();

        // No reapplication while any strain of the toxin is already in the system.
        var strains = new[]
        {
          EldritchPoisoner.ToxinStrDice, EldritchPoisoner.ToxinStrFlat,
          EldritchPoisoner.ToxinDexFlat, EldritchPoisoner.ToxinConDice, EldritchPoisoner.ToxinConFlat,
        };
        foreach (var strain in strains)
        {
          if (strain is not null && target.Buffs.GetBuff(strain) is not null)
          {
            return;
          }
        }

        var alchemistLevel = owner.Descriptor.Progression.GetClassLevel(AlchemistClass);
        var dc = 10 + alchemistLevel / 2 + owner.Stats.Intelligence.Bonus + dcModifier;

        // Careful Injection: sneak attacks deliver the toxin more precisely.
        if (sneakAttack && owner.HasFact(EldritchPoisonerDiscoveries.CarefulInjection))
        {
          dc += 2;
        }

        // Supernatural toxin: bypasses poison immunity, but such creatures resist it.
        if (target.HasFact(PoisonImmunityA) || target.HasFact(PoisonImmunityB))
        {
          dc -= 4;
        }

        var save = new RuleSavingThrow(target, SavingThrowType.Fortitude, dc) { Reason = sourceFact };
        if (Rulebook.Trigger(save).IsPassed)
        {
          return;
        }

        // Pick the strain(s) based on the poisoner's discoveries.
        var lethal = owner.HasFact(EldritchPoisonerDiscoveries.LethalToxin);
        var combine = owner.HasFact(EldritchPoisonerDiscoveries.CombineToxins);
        BlueprintBuff primary;
        BlueprintBuff extra = null;
        if (lethal && combine)
        {
          primary = EldritchPoisoner.ToxinConFlat;
          extra = EldritchPoisoner.ToxinDexFlat;
        }
        else if (lethal)
        {
          primary = EldritchPoisoner.ToxinConDice;
        }
        else if (combine)
        {
          primary = EldritchPoisoner.ToxinStrFlat;
          extra = EldritchPoisoner.ToxinDexFlat;
        }
        else
        {
          primary = EldritchPoisoner.ToxinStrDice;
        }

        var seconds = ContextDuration.Fixed(2).Calculate(context).Seconds;
        target.AddBuff(primary, context, duration: seconds);
        if (extra is not null)
        {
          target.AddBuff(extra, context, duration: seconds);
        }

        // One secondary effect per dose (worst first): Paralytic > Mind-Altering > Sickening.
        var secondary = GetSecondary(owner, alchemistLevel);
        if (secondary is not null)
        {
          target.AddBuff(secondary, context, duration: seconds);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("EldritchPoisoner: failed to deliver arcanotoxin.", e);
      }
    }

    private static BlueprintBuff GetSecondary(UnitEntityData owner, int alchemistLevel)
    {
      if (owner.HasFact(EldritchPoisonerDiscoveries.ParalyticToxin))
      {
        return (alchemistLevel >= 15 ? BuffRefs.Paralyzed : BuffRefs.Staggered).Reference.Get();
      }
      if (owner.HasFact(EldritchPoisonerDiscoveries.MindAlteringToxin))
      {
        return (alchemistLevel >= 10 ? BuffRefs.Confusion : BuffRefs.DazzledBuff).Reference.Get();
      }
      if (owner.HasFact(EldritchPoisonerDiscoveries.SickeningToxin))
      {
        return (alchemistLevel >= 12 ? BuffRefs.Nauseated : BuffRefs.Sickened).Reference.Get();
      }
      return null;
    }
  }

  /// <summary>
  /// Delivers the arcanotoxin on the owner's weapon hits while the coating is active.
  /// </summary>
  [TypeId(Guids.ArcanotoxinDelivery)]
  internal class ArcanotoxinDelivery :
    UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleAttackWithWeapon>
  {
    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt)
    {
      if (evt.AttackRoll is null || !evt.AttackRoll.IsHit)
      {
        return;
      }
      ArcanotoxinApply.Apply(
        Owner, evt.Target, Context, Fact, 0, evt.AttackRoll.IsSneakAttack);
    }
  }

  /// <summary>
  /// The price of haste: the caster loses 25% of maximum HP, or only 15% if they succeed
  /// at a DC 15 Fortitude save. Never reduces the caster below 1 HP.
  /// </summary>
  [TypeId(Guids.ExpeditedSynthesisCost)]
  internal class ContextActionExpeditedSynthesisCost : ContextAction
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

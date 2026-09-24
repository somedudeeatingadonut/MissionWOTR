using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.Configurators.Items.Weapons;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Spellblade (faithful port of the Paizo magus archetype, Ultimate Magic).
  ///
  /// "A spellblade magus can manifest a ghostly blade of force that can be used
  /// as an off-hand weapon."
  ///
  /// - Force Athame (Sp, 2nd - replaces spellstrike): sacrifice a prepared magus
  ///   spell of 1st level or higher as a swift action to create a dagger of
  ///   force in his off hand. The athame lasts 1 minute or until dismissed, has
  ///   an enhancement bonus equal to the level of the spell sacrificed (max +5),
  ///   acts as a dagger, and attacks with it are force attacks dealing force
  ///   damage. The hand is still free for spell combat (and delivering touch
  ///   spells). Engine mapping: the sacrifice is spontaneous spell conversion
  ///   (the pplus SpireDefender mechanism - converting a prepared magus slot
  ///   into "Force Athame" consumes the slot natively and carries its level in
  ///   Context.SpellLevel); the athame is five conjured item weapons (+1..+5,
  ///   TemporaryEnhancement enchants, native force damage via damage-type
  ///   override) granted as secondary/off-hand attacks (the vanilla
  ///   AddSecondaryAttacks pattern, DemonicFirstAscensionBuff) - the off-hand
  ///   slot stays empty, so spell combat keeps working (the tabletop's
  ///   "hand still free" clause, adapted: the either/or choice is not enforced).
  /// - Pool-Sourced Athame (arcana): create the athame by spending 3 points from
  ///   the arcane pool instead of a spell (adaptation of "enhancement equal to
  ///   points spent" - the engine has no choose-an-amount UI; 3 points = +3,
  ///   matching a 3rd-level sacrifice. Tuning candidate).
  /// - Spellblade Parry (arcana): when an enemy makes a melee attack against the
  ///   magus, the athame shatters into a shield - it ends immediately and grants
  ///   a deflection bonus to AC until the end of his next turn equal to the
  ///   sacrificed spell's level (max +5). (Adaptation: the tabletop immediate
  ///   action is automatic here - it triggers on the first qualifying attack,
  ///   and ending the athame is its own cost.)
  /// - Throw Athame (arcana): standard action, throw the athame as a ranged
  ///   attack (Long range; tabletop says 60 ft with no range penalty); on a hit
  ///   it deals its damage and its duration ends, on a miss it returns and
  ///   stays. Throw Athame (Empowered) spends 2 arcane pool points for +2d6
  ///   force damage on a hit (adaptation of the optional "up to 2 points" spend
  ///   as a separate ability). Implemented as a triggered RuleAttackWithWeapon
  ///   with a runtime-spawned athame entity (the DarkCodex ContextActionAttack
  ///   pattern).
  ///
  /// Log prefix: [spellblade].
  /// </summary>
  internal static class Spellblade
  {
    internal const string ArchetypeName = "SpellbladeArchetype";
    internal const string DisplayName = "Spellblade.Name";
    internal const string Description = "Spellblade.Description";

    internal const string AthameFeatureName = "SpellbladeForceAthame";
    internal const string AthameCastName = "SpellbladeForceAthameCast";
    internal const string AthameBuffName = "SpellbladeAthameBuff";
    internal const string AthameWeaponName = "SpellbladeAthameWeapon";
    internal const string PoolArcanaName = "SpellbladePoolAthame";
    internal const string PoolCastName = "SpellbladePoolAthameCast";
    internal const string ParryArcanaName = "SpellbladeParry";
    internal const string ParryBuffName = "SpellbladeParryBuff";
    internal const string ThrowArcanaName = "SpellbladeThrowAthame";
    internal const string ThrowAbilityName = "SpellbladeThrowAthameAbility";
    internal const string ThrowEmpoweredName = "SpellbladeThrowAthameEmpowered";

    // Vanilla: the arcane pool resource.
    private const string ArcanePoolResourceGuid = "effc3e386331f864e9e06d19dc218b37";
    // Vanilla: the magus arcana selection.
    private const string MagusArcanaSelectionGuid = "e9dc4dfc73eaaf94aae27e0ed6cc9ada";
    // Vanilla: TemporaryEnhancement1..5 weapon enchants (the arcane pool's own
    // temporary weapon enhancement family - right for a conjured blade).
    private static readonly string[] TemporaryEnhancement =
    {
      "d704f90f54f813043a525f304f6c0050",
      "9e9bab3020ec5f64499e007880b37e52",
      "d072b841ba0668846adeb007f623bd6c",
      "6a6a0901d799ceb49b33d4851ff72132",
      "746ee366e50611146821d61e391edf16",
    };

    private static string WeaponGuid(int i) => i switch
    {
      0 => Guids.SpellbladeAthameWeapon1,
      1 => Guids.SpellbladeAthameWeapon2,
      2 => Guids.SpellbladeAthameWeapon3,
      3 => Guids.SpellbladeAthameWeapon4,
      _ => Guids.SpellbladeAthameWeapon5,
    };
    private static string AthameBuffGuid(int i) => i switch
    {
      0 => Guids.SpellbladeAthameBuff1,
      1 => Guids.SpellbladeAthameBuff2,
      2 => Guids.SpellbladeAthameBuff3,
      3 => Guids.SpellbladeAthameBuff4,
      _ => Guids.SpellbladeAthameBuff5,
    };
    private static string ParryBuffGuid(int i) => i switch
    {
      0 => Guids.SpellbladeParryBuff1,
      1 => Guids.SpellbladeParryBuff2,
      2 => Guids.SpellbladeParryBuff3,
      3 => Guids.SpellbladeParryBuff4,
      _ => Guids.SpellbladeParryBuff5,
    };

    public static void Configure()
    {
      var magus = CharacterClassRefs.MagusClass.Reference.Get();
      var athameIcon = AbilityRefs.ForcePunchCast.Reference.Get().Icon;

      // ----- The five athames: force daggers, enhancement +1..+5 -----
      var weapons = new BlueprintItemWeapon[5];
      for (int i = 0; i < 5; i++)
      {
        weapons[i] = ItemWeaponConfigurator.New(AthameWeaponName + (i + 1), WeaponGuid(i))
          .SetType(WeaponTypeRefs.Dagger.ToString())
          .SetDisplayNameText($"Force Athame +{i + 1}")
          .SetDescriptionText(
            "A dagger of pure force conjured by the spellblade. It strikes as a dagger " +
            "of force with an enhancement bonus equal to the level of the spell sacrificed.")
          .SetOverrideDamageDice()
          .SetDamageDice(new DiceFormula(1, DiceType.D4))
          .SetOverrideDamageType()
          .SetDamageType(new DamageTypeDescription
          {
            Type = DamageType.Energy,
            Energy = DamageEnergyType.Magic,
          })
          .SetEnchantments(TemporaryEnhancement[i])
          .Configure();
      }

      // ----- The five athame buffs: each grants the off-hand force attack -----
      var athameBuffs = new BlueprintBuff[5];
      for (int i = 0; i < 5; i++)
      {
        athameBuffs[i] = BuffConfigurator.New(AthameBuffName + (i + 1), AthameBuffGuid(i))
          .SetDisplayName("SpellbladeAthameBuff.Name")
          .SetDescription("SpellbladeAthameBuff.Description")
          .SetIcon(athameIcon)
          .AddSecondaryAttacks(weapons[i])
          .Configure();
      }

      // ----- Parry buffs: deflection +1..+5 for one round -----
      var parryBuffs = new BlueprintBuff[5];
      for (int i = 0; i < 5; i++)
      {
        parryBuffs[i] = BuffConfigurator.New(ParryBuffName + (i + 1), ParryBuffGuid(i))
          .SetDisplayName("SpellbladeParryBuff.Name")
          .SetDescription("SpellbladeParryBuff.Description")
          .SetIcon(athameIcon)
          .AddStatBonus(stat: StatType.AC, value: i + 1, descriptor: ModifierDescriptor.Deflection)
          .Configure();
      }

      // The athame buffs as fact references (implicit per-element conversions;
      // Cast<> would skip the implicit operators and fail at runtime).
      var athameFactRefs = new List<Blueprint<BlueprintUnitFactReference>>();
      foreach (var buff in athameBuffs)
      {
        athameFactRefs.Add(buff);
      }

      // ----- Force Athame: the conversion target (cast by sacrificing a slot) -----
      var athameCast = AbilityConfigurator.New(AthameCastName, Guids.SpellbladeAthameCast)
        .SetDisplayName("SpellbladeForceAthame.Name")
        .SetDescription("SpellbladeForceAthame.Description")
        .SetIcon(athameIcon)
        .SetType(AbilityType.Spell)
        .SetRange(AbilityRange.Personal)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
        .AllowTargeting(self: true)
        .AddAbilityEffectRunAction(ActionsBuilder.New()
          .Add(new SpellbladeCreateAthame { AthameBuffs = athameBuffs })
          .Build())
        .Configure();

      // ----- Force Athame feature (2nd, replaces spellstrike) -----
      var athameFeature = FeatureConfigurator.New(AthameFeatureName, Guids.SpellbladeForceAthameFeature)
        .SetDisplayName("SpellbladeForceAthame.Name")
        .SetDescription("SpellbladeForceAthame.Description")
        .SetIcon(athameIcon)
        .SetIsClassFeature()
        .AddSpontaneousSpellConversion(
          CharacterClassRefs.MagusClass.ToString(),
          new() { athameCast, athameCast, athameCast, athameCast, athameCast,
                  athameCast, athameCast, athameCast, athameCast, athameCast })
        .Configure();

      // ----- Pool-Sourced Athame arcana: 3 pool points, +3 athame -----
      var poolCast = AbilityConfigurator.New(PoolCastName, Guids.SpellbladePoolAthameCast)
        .SetDisplayName("SpellbladePoolAthame.Name")
        .SetDescription("SpellbladePoolAthame.Description")
        .SetIcon(athameIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
        .AllowTargeting(self: true)
        .AddAbilityResourceLogic(
          requiredResource: ArcanePoolResourceGuid, amount: 3, isSpendResource: true)
        .AddAbilityEffectRunAction(ActionsBuilder.New()
          .Add(new SpellbladeCreateAthame { AthameBuffs = athameBuffs, FixedLevel = 3 })
          .Build())
        .Configure();

      var poolArcana = FeatureConfigurator.New(PoolArcanaName, Guids.SpellbladePoolAthameFeature)
        .SetDisplayName("SpellbladePoolAthame.Name")
        .SetDescription("SpellbladePoolAthame.Description")
        .SetIcon(athameIcon)
        .SetIsClassFeature()
        .AddFacts(new() { poolCast })
        .AddPrerequisiteFeature(athameFeature)
        .Configure();

      // ----- Spellblade Parry arcana -----
      var parryArcana = FeatureConfigurator.New(ParryArcanaName, Guids.SpellbladeParryFeature)
        .SetDisplayName("SpellbladeParry.Name")
        .SetDescription("SpellbladeParry.Description")
        .SetIcon(athameIcon)
        .SetIsClassFeature()
        .AddComponent(new SpellbladeParryWard
        {
          AthameBuffs = athameBuffs,
          ParryBuffs = parryBuffs,
        })
        .AddPrerequisiteFeature(athameFeature)
        .Configure();

      // ----- Throw Athame arcana -----
      var throwAbility = AbilityConfigurator.New(ThrowAbilityName, Guids.SpellbladeThrowAthameAbility)
        .SetDisplayName("SpellbladeThrowAthame.Name")
        .SetDescription("SpellbladeThrowAthame.Description")
        .SetIcon(athameIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard)
        .AllowTargeting(enemies: true)
        .SetEffectOnEnemy(AbilityEffectOnUnit.Harmful)
        .AddAbilityCasterHasFacts(facts: athameFactRefs, needsAll: false)
        .AddAbilityEffectRunAction(ActionsBuilder.New()
          .Add(new SpellbladeThrowAthameAction
          {
            AthameBuffs = athameBuffs,
            Weapons = weapons,
          })
          .Build())
        .Configure();

      var throwEmpowered = AbilityConfigurator.New(ThrowEmpoweredName, Guids.SpellbladeThrowEmpowered)
        .SetDisplayName("SpellbladeThrowAthameEmpowered.Name")
        .SetDescription("SpellbladeThrowAthameEmpowered.Description")
        .SetIcon(athameIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard)
        .AllowTargeting(enemies: true)
        .SetEffectOnEnemy(AbilityEffectOnUnit.Harmful)
        .AddAbilityCasterHasFacts(facts: athameFactRefs, needsAll: false)
        .AddAbilityResourceLogic(
          requiredResource: ArcanePoolResourceGuid, amount: 2, isSpendResource: true)
        .AddAbilityEffectRunAction(ActionsBuilder.New()
          .Add(new SpellbladeThrowAthameAction
          {
            AthameBuffs = athameBuffs,
            Weapons = weapons,
            BonusDice = 2,
          })
          .Build())
        .Configure();

      var throwArcana = FeatureConfigurator.New(ThrowArcanaName, Guids.SpellbladeThrowAthameFeature)
        .SetDisplayName("SpellbladeThrowAthame.Name")
        .SetDescription("SpellbladeThrowAthame.Description")
        .SetIcon(athameIcon)
        .SetIsClassFeature()
        .AddFacts(new() { throwAbility, throwEmpowered })
        .AddPrerequisiteFeature(athameFeature)
        .Configure();

      // ----- Register the three arcana in the magus arcana selection -----
      FeatureSelectionConfigurator.For(MagusArcanaSelectionGuid)
        .AddToAllFeatures(poolArcana, parryArcana, throwArcana)
        .Configure();

      // ----- Archetype: trades spellstrike for the force athame -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.SpellbladeArchetype, CharacterClassRefs.MagusClass)
          .SetLocalizedName(DisplayName)
          .SetLocalizedDescription(Description);

      // Spellstrike is granted at 2nd level; both known blueprint spellings are
      // attempted (AddRemovals warns and skips any name the progression lacks).
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, magus,
        FeatureRefs.MagusSpellStrike.ToString(),
        "SpellStrikeFeature");

      archetype
        .AddToAddFeatures(LevelPlan.L(2), AthameFeatureName)
        .Configure(delayed: true);
    }
  }

  /// <summary>
  /// Creates the force athame: applies the athame buff matching the sacrificed
  /// spell's level (Context.SpellLevel - the spontaneous-conversion payload
  /// pattern; FixedLevel overrides for the pool-sourced variant) for one minute.
  /// </summary>
  [TypeId(Guids.SpellbladeCreateAthameAction)]
  public class SpellbladeCreateAthame : ContextAction
  {
    public BlueprintBuff[] AthameBuffs;
    public int FixedLevel;

    public override string GetCaption() => "SpellbladeCreateAthame";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        if (caster is null || AthameBuffs is null || AthameBuffs.Length == 0)
        {
          return;
        }
        int level = FixedLevel > 0 ? FixedLevel : Context.SpellLevel;
        if (level < 1)
        {
          MissionFeats.Logger.Warn(
            "[spellblade] athame creation refused: no spell level sacrificed " +
            "(cantrips cannot power the athame).");
          return;
        }
        int index = Math.Min(AthameBuffs.Length, level) - 1;
        caster.AddBuff(AthameBuffs[index], Context,
          ContextDuration.Fixed(10).Calculate(Context).Seconds);
        MissionFeats.Logger.Info(
          $"[spellblade] force athame created (+{index + 1}) for {caster.CharacterName}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Spellblade: athame creation failed.", e);
      }
    }
  }

  /// <summary>
  /// Spellblade Parry: the first enemy melee attack against the magus while an
  /// athame is active shatters it - the athame ends and the magus gains a
  /// deflection bonus to AC (equal to the sacrificed spell's level) for one
  /// round. Applied in AboutToTrigger so it counts against the current attack.
  /// </summary>
  [TypeId(Guids.SpellbladeParryComponent)]
  internal class SpellbladeParryWard : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleAttackWithWeapon>
  {
    public BlueprintBuff[] AthameBuffs;
    public BlueprintBuff[] ParryBuffs;

    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
    {
      try
      {
        if (evt.Target != Owner || evt.Initiator is null || !evt.Initiator.IsEnemy(Owner))
        {
          return;
        }
        if (evt.Weapon?.Blueprint is null || !evt.Weapon.Blueprint.IsMelee)
        {
          return;
        }
        for (int i = 0; i < AthameBuffs.Length; i++)
        {
          var athame = Owner.Buffs.GetBuff(AthameBuffs[i]);
          if (athame is null)
          {
            continue;
          }
          // Shatter the athame and raise the guard.
          Owner.RemoveFact(athame);
          if (i < ParryBuffs.Length)
          {
            Owner.AddBuff(ParryBuffs[i], Context,
              ContextDuration.Fixed(1).Calculate(Context).Seconds);
          }
          MissionFeats.Logger.Info(
            $"[spellblade] parry: {Owner.CharacterName}'s athame (+{i + 1}) shatters " +
            $"against {evt.Initiator.CharacterName}'s attack.");
          return;
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Spellblade: parry failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt) { }
  }

  /// <summary>
  /// Throw Athame: hurls the active athame at the target as a ranged weapon
  /// attack with a runtime-spawned copy of the athame item (the DarkCodex
  /// ContextActionAttack pattern). On a hit the athame's damage is dealt and
  /// its duration ends (plus BonusDice d6 of force for the empowered throw);
  /// on a miss it returns and remains.
  /// </summary>
  [TypeId(Guids.SpellbladeThrowAction)]
  public class SpellbladeThrowAthameAction : ContextAction
  {
    public BlueprintBuff[] AthameBuffs;
    public BlueprintItemWeapon[] Weapons;
    public int BonusDice;

    public override string GetCaption() => "SpellbladeThrowAthame";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        var target = Target.Unit;
        if (caster is null || target is null || AthameBuffs is null || Weapons is null)
        {
          return;
        }
        int active = -1;
        for (int i = 0; i < AthameBuffs.Length; i++)
        {
          if (caster.Buffs.GetBuff(AthameBuffs[i]) != null)
          {
            active = i;
            break;
          }
        }
        if (active < 0)
        {
          MissionFeats.Logger.Warn(
            "[spellblade] throw refused: no active athame (it may have just expired).");
          return;
        }
        var weapon = Weapons[active].CreateEntity<ItemEntityWeapon>();
        var rule = new RuleAttackWithWeapon(caster, target, weapon, 0);
        Context.TriggerRule(rule);
        if (rule.AttackRoll is not null && rule.AttackRoll.IsHit)
        {
          var athame = caster.Buffs.GetBuff(AthameBuffs[active]);
          if (athame != null)
          {
            caster.RemoveFact(athame);
          }
          if (BonusDice > 0)
          {
            var bundle = new DamageBundle();
            bundle.Add(new EnergyDamage(
              new DiceFormula(BonusDice, DiceType.D6), 0, DamageEnergyType.Magic));
            Rulebook.Trigger(new RuleDealDamage(caster, target, bundle));
          }
          MissionFeats.Logger.Info(
            $"[spellblade] throw hit: {target.CharacterName} takes the athame's " +
            $"force (+{active + 1}){(BonusDice > 0 ? $" plus {BonusDice}d6" : "")}; athame ends.");
        }
        else
        {
          MissionFeats.Logger.Info(
            "[spellblade] throw missed: the athame returns to "
            + caster.CharacterName + "'s hand.");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Spellblade: throw failed.", e);
      }
    }
  }
}

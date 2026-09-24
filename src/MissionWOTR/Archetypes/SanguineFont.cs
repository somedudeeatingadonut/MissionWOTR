using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.Configurators.Items.Weapons;
using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
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
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Sanguine Font (homebrew, user-commissioned bloodrager archetype - working
  /// title "Crimson Heart"; renamed: a font is a wellspring, which is both the
  /// healing fantasy and the blood one). An AoE-healing bloodrager with
  /// kineticist powers in place of her highest spells.
  ///
  /// - Vital Blood (1st): while bloodraging, a 10-ft aura; at the start of each
  ///   of her turns, every ally in the aura below maximum HP regains 1d4 + 1/3
  ///   bloodrager level HP.
  /// - Bloodletting Pulse (2nd, swift): while bloodraging, expend 1 additional
  ///   round of bloodrage to release a wave - all allies within 20 ft (including
  ///   herself) regain 1d8 + 1/3 level HP.
  /// - Shared Vitality (5th): the shared pulse - same cost, but allies gain
  ///   fast healing (2 at 5th, 3 at 11th, 5 at 16th) for 3 rounds while the
  ///   Font bleeds for the same amount for 3 rounds (unremovable).
  /// - Greater Bloodletting Pulse (8th): the pulse reaches 30 ft and heals
  ///   2d8 + bloodrager level + Con modifier; allies healed from below 0 HP are
  ///   treated as stabilized.
  /// - Kinetic Blade (6th): choose an element; a swift-action toggle grants an
  ///   off-hand blade of that element (the kinetic knight's own weapon type),
  ///   dealing 1d6 + Con per hit plus one extra d6 per two (class - 2) levels
  ///   beyond 1st - kineticist level = bloodrager level - 2.
  /// - Kinetic Blast (11th): a ranged touch attack (the kineticist's energy
  ///   blast weapon type, Dex to attack, Con to damage, same -2-levels scaling)
  ///   with no infusions - the downside. Replaces the spellbook's highest tier:
  ///   the archetype's spellbook (a clone of the bloodrager's) caps at 3rd-
  ///   level spells.
  /// - Sanguine Apotheosis (20th): the aura becomes 40 ft and heals 2d8 + Con +
  ///   Cha + 1/8 CURRENT HP + level; the pulse becomes a FREE action usable any
  ///   number of times per round (each use still costs a bloodrage round); and
  ///   once per bloodrage, when an ally in the aura would die, she can expend
  ///   ALL remaining bloodrage rounds to leave that ally at 1 HP with a
  ///   10d8 + level heal.
  ///
  /// Trades: proficiencies (simple weapons + light armor only), damage
  /// reduction, uncanny dodge (both flavors), and the 4th-level spell tier.
  ///
  /// Anti-infinite-rage guard: the vanilla mythic ability LimitlessRage is
  /// prerequisite-blocked for Sanguine Fonts - the archetype's whole interactivity
  /// is the bloodrage-round economy, and an endless rage would delete it.
  ///
  /// Implementation notes: per-round effects use ITickEachRound (the pplus
  /// RagingDrunkStuff pattern - proven on bloodrager features); healing via
  /// Rulebook.Trigger(new RuleHealDamage(...)); ally enumeration via
  /// Game.Instance.State.Units + IsAlly + DistanceTo (the pplus Nocticula
  /// pattern); dice via UnityEngine.Random.Range (the game's own roller); the
  /// once-per-round pulse guard is a hidden 1-round marker buff; the blade and
  /// blast use the vanilla KineticBlastEnergyBlade / KineticBlastEnergy weapon
  /// types. Log prefix: [sanguine]. All numbers are tuning candidates.
  /// </summary>
  internal static class SanguineFont
  {
    internal const string ArchetypeName = "SanguineFontArchetype";
    internal const string DisplayName = "SanguineFont.Name";
    internal const string Description = "SanguineFont.Description";

    internal const string VitalBloodName = "SanguineVitalBlood";
    internal const string PulseFeatureName = "SanguineBloodlettingPulseFeature";
    internal const string PulseAbilityName = "SanguineBloodlettingPulse";
    internal const string PulseSharedAbilityName = "SanguineBloodlettingPulseShared";
    internal const string PulseFreeAbilityName = "SanguineBloodlettingPulseFree";
    internal const string PulseUsedBuffName = "SanguinePulseUsedBuff";
    internal const string SharedVitalityName = "SanguineSharedVitality";
    internal const string GreaterPulseName = "SanguineGreaterPulse";
    internal const string FastHealingBuffName = "SanguineFastHealingBuff";
    internal const string BleedBuffName = "SanguineBleedBuff";
    internal const string ElementSelectionName = "SanguineElementSelection";
    internal const string ElementFeatureName = "SanguineElement";
    internal const string BladeToggleName = "SanguineKineticBladeToggle";
    internal const string BladeBuffName = "SanguineKineticBladeBuff";
    internal const string BladeWeaponName = "SanguineKineticBladeWeapon";
    internal const string BlastFeatureName = "SanguineKineticBlastFeature";
    internal const string BlastAbilityName = "SanguineKineticBlast";
    internal const string BlastWeaponName = "SanguineKineticBlastWeapon";
    internal const string ApotheosisName = "SanguineApotheosis";
    internal const string ProficienciesName = "SanguineProficiencies";

    // Vanilla: the bloodrager's standard rage buff, rounds resource, class,
    // book, list and proficiencies; the kineticist weapon types; the mythic
    // ability that would break the rage economy.
    private const string BloodragerRageBuffGuid = "5eac31e457999334b98f98b60fc73b2f";
    private const string BloodragerRageResourceGuid = "4aec9ec9d9cd5e24a95da90e56c72e37";
    private const string BloodragerDamageReductionGuid = "07eba4bb72c2e3845bb442dce85d3b58";
    private const string UncannyDodgeGuid = "3c08d842e802c3e4eb19d15496145709";
    private const string ImprovedUncannyDodgeGuid = "485a18c05792521459c7d06c63128c79";
    private const string LimitlessRageGuid = "5cb58e6e406525342842a073fb70d068";
    private const string BloodragerClassGuid = "d77e67a814d686842802c9cfd8ef8499";

    private static readonly (string Key, DamageEnergyType Energy)[] Elements =
    {
      ("Fire", DamageEnergyType.Fire),
      ("Cold", DamageEnergyType.Cold),
      ("Electricity", DamageEnergyType.Electricity),
      ("Acid", DamageEnergyType.Acid),
      ("Force", DamageEnergyType.Magic),
    };

    private static string ElementFeatureGuid(int i) => i switch
    {
      0 => Guids.SanguineElementFire,
      1 => Guids.SanguineElementCold,
      2 => Guids.SanguineElementElectricity,
      3 => Guids.SanguineElementAcid,
      _ => Guids.SanguineElementForce,
    };
    private static string BladeToggleGuid(int i) => i switch
    {
      0 => Guids.SanguineBladeToggleFire,
      1 => Guids.SanguineBladeToggleCold,
      2 => Guids.SanguineBladeToggleElectricity,
      3 => Guids.SanguineBladeToggleAcid,
      _ => Guids.SanguineBladeToggleForce,
    };
    private static string BladeBuffGuid(int i) => i switch
    {
      0 => Guids.SanguineBladeBuffFire,
      1 => Guids.SanguineBladeBuffCold,
      2 => Guids.SanguineBladeBuffElectricity,
      3 => Guids.SanguineBladeBuffAcid,
      _ => Guids.SanguineBladeBuffForce,
    };
    private static string BladeWeaponGuid(int i) => i switch
    {
      0 => Guids.SanguineBladeWeaponFire,
      1 => Guids.SanguineBladeWeaponCold,
      2 => Guids.SanguineBladeWeaponElectricity,
      3 => Guids.SanguineBladeWeaponAcid,
      _ => Guids.SanguineBladeWeaponForce,
    };
    private static string FastHealingBuffGuid(int value) => value switch
    {
      3 => Guids.SanguineFastHealingBuff3,
      5 => Guids.SanguineFastHealingBuff5,
      _ => Guids.SanguineFastHealingBuff2,
    };
    private static string BleedBuffGuid(int value) => value switch
    {
      3 => Guids.SanguineBleedBuff3,
      5 => Guids.SanguineBleedBuff5,
      _ => Guids.SanguineBleedBuff2,
    };

    internal static int Roll(int dice, int sides)
    {
      int total = 0;
      for (int i = 0; i < dice; i++)
      {
        total += UnityEngine.Random.Range(1, sides + 1);
      }
      return total;
    }

    public static void Configure()
    {
      var bloodrager = CharacterClassRefs.BloodragerClass.Reference.Get();
      var bloodragerBook = SpellbookRefs.BloodragerSpellbook.Reference.Get();
      var bloodragerList = SpellListRefs.BloodragerSpellList.Reference.Get();
      var rageBuff = BlueprintTool.Get<BlueprintBuff>(BloodragerRageBuffGuid);
      var rageResource = BlueprintTool.Get<BlueprintAbilityResource>(BloodragerRageResourceGuid);
      var bladeIcon = ItemWeaponRefs.FireKineticBladeWeapon.Reference.Get().Icon;
      var blastIcon = AbilityRefs.ForcePunchCast.Reference.Get().Icon;

      // ----- Spellbook: the bloodrager's own book, minus its highest tier -----
      // (the kinetic blast replaces the top spells; max spell level 3)
      var trimmedList = BlueprintTool.Create<BlueprintSpellList>(
        "SanguineFontSpellList", Guids.SanguineFontSpellList);
      var trimmed = new SpellLevelList[4];
      for (int i = 0; i < 4; i++)
      {
        trimmed[i] = new SpellLevelList(i) { SpellLevel = i };
        var source = bloodragerList.SpellsByLevel?.FirstOrDefault(e => e != null && e.SpellLevel == i);
        if (source != null)
        {
          foreach (BlueprintAbility spell in source.Spells)
          {
            ElementalObsessor.AddSpellToEntry(trimmed[i], spell);
          }
        }
      }
      trimmedList.SpellsByLevel = trimmed;
      var book = SpellbookConfiguratorFor(bloodragerBook, trimmedList);

      // ----- Proficiencies: simple weapons + light armor only -----
      var simple = FeatureRefs.SimpleWeaponProficiency.Reference.Get();
      var lightArmor = FeatureRefs.LightArmorProficiency.Reference.Get();
      var proficiencies = FeatureConfigurator.New(ProficienciesName, Guids.SanguineProficiencies)
        .SetDisplayName("SanguineProficiencies.Name")
        .SetDescription("SanguineProficiencies.Description")
        .SetIcon(simple.Icon)
        .SetIsClassFeature()
        .AddFacts(new() { simple, lightArmor })
        .Configure();

      // ----- Hidden once-per-round marker for the pulse -----
      var pulseUsed = BuffConfigurator.New(PulseUsedBuffName, Guids.SanguinePulseUsedBuff)
        .SetDisplayName("SanguinePulseUsedBuff.Name")
        .SetDescription("SanguinePulseUsedBuff.Description")
        .SetIcon(blastIcon)
        .Configure();

      // ----- Fast healing + bleed buff pairs (Shared Vitality) -----
      var fastHealing = new Dictionary<int, BlueprintBuff>();
      var bleed = new Dictionary<int, BlueprintBuff>();
      foreach (var value in new[] { 2, 3, 5 })
      {
        fastHealing[value] = BuffConfigurator.New(
            $"{FastHealingBuffName}{value}", FastHealingBuffGuid(value))
          .SetDisplayName("SanguineFastHealing.Name")
          .SetDescription("SanguineFastHealing.Description")
          .SetIcon(blastIcon)
          .AddComponent(new SanguineFastHeal { Amount = value })
          .Configure();
        bleed[value] = BuffConfigurator.New(
            $"{BleedBuffName}{value}", BleedBuffGuid(value))
          .SetDisplayName("SanguineBleed.Name")
          .SetDescription("SanguineBleed.Description")
          .SetIcon(BuffRefs.Bleed1d4Buff.Reference.Get().Icon)
          .AddComponent(new SanguineBleed { Amount = value })
          .Configure();
      }

      // ----- Greater Pulse feature (built early: pulse components capture it) -----
      var greaterPulse = FeatureConfigurator.New(GreaterPulseName, Guids.SanguineGreaterPulse)
        .SetDisplayName("SanguineGreaterPulse.Name")
        .SetDescription("SanguineGreaterPulse.Description")
        .SetIcon(blastIcon)
        .SetIsClassFeature()
        .Configure();

      // ----- The three pulse abilities -----
      BlueprintAbility BuildPulse(
        string name, string guid, string displayBase, bool shared, bool allowRepeat)
      {
        return AbilityConfigurator.New(name, guid)
          .SetDisplayName(displayBase + ".Name")
          .SetDescription(displayBase + ".Description")
          .SetIcon(blastIcon)
          .SetType(AbilityType.Special)
          .SetRange(AbilityRange.Personal)
          .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Swift)
          .AllowTargeting(self: true)
          .AddAbilityEffectRunAction(ActionsBuilder.New()
            .Add(new SanguinePulse
            {
              CharacterClass = bloodrager,
              RageBuff = rageBuff,
              RageResource = rageResource,
              GreaterFeature = greaterPulse,
              UsedBuff = pulseUsed,
              AllowRepeat = allowRepeat,
              Shared = shared,
            })
            .Build())
          .Configure();
      }

      var pulseFeature = FeatureConfigurator.New(PulseFeatureName, Guids.SanguinePulseFeature)
        .SetDisplayName("SanguinePulse.Name")
        .SetDescription("SanguinePulse.Description")
        .SetIcon(blastIcon)
        .SetIsClassFeature()
        .AddFacts(new()
        {
          BuildPulse(PulseAbilityName, Guids.SanguinePulseAbility, "SanguinePulse", false, false),
        })
        .Configure();

      var sharedVitality = FeatureConfigurator.New(SharedVitalityName, Guids.SanguineSharedVitality)
        .SetDisplayName("SanguineSharedVitality.Name")
        .SetDescription("SanguineSharedVitality.Description")
        .SetIcon(blastIcon)
        .SetIsClassFeature()
        .AddPrerequisiteFeature(pulseFeature)
        .AddFacts(new()
        {
          BuildPulse(PulseSharedAbilityName, Guids.SanguinePulseSharedAbility,
            "SanguinePulseShared", true, false),
        })
        .Configure();

      // Restore the defensive prerequisite now that pulseFeature exists.
      FeatureConfigurator.For(greaterPulse)
        .AddPrerequisiteFeature(pulseFeature)
        .Configure();

      // ----- Apotheosis (20th): free-action pulse + death save + aura upgrade -----
      var apotheosis = FeatureConfigurator.New(ApotheosisName, Guids.SanguineApotheosis)
        .SetDisplayName("SanguineApotheosis.Name")
        .SetDescription("SanguineApotheosis.Description")
        .SetIcon(blastIcon)
        .SetIsClassFeature()
        .AddComponent(new SanguineDeathSave
        {
          CharacterClass = bloodrager,
          RageBuff = rageBuff,
          RageResource = rageResource,
        })
        .AddFacts(new()
        {
          AbilityConfigurator.New(PulseFreeAbilityName, Guids.SanguinePulseFreeAbility)
            .SetDisplayName("SanguinePulseFree.Name")
            .SetDescription("SanguinePulseFree.Description")
            .SetIcon(blastIcon)
            .SetType(AbilityType.Special)
            .SetRange(AbilityRange.Personal)
            .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free)
            .AllowTargeting(self: true)
            .AddAbilityEffectRunAction(ActionsBuilder.New()
              .Add(new SanguinePulse
              {
                CharacterClass = bloodrager,
                RageBuff = rageBuff,
                RageResource = rageResource,
                GreaterFeature = greaterPulse,
                UsedBuff = pulseUsed,
                AllowRepeat = true,
                Shared = false,
              })
              .Build())
            .Configure(),
        })
        .Configure();

      // ----- Vital Blood (1st): the aura -----
      var vitalBlood = FeatureConfigurator.New(VitalBloodName, Guids.SanguineVitalBlood)
        .SetDisplayName("SanguineVitalBlood.Name")
        .SetDescription("SanguineVitalBlood.Description")
        .SetIcon(blastIcon)
        .SetIsClassFeature()
        .AddComponent(new SanguineAura
        {
          CharacterClass = bloodrager,
          RageBuff = rageBuff,
          ApotheosisFeature = apotheosis,
        })
        .Configure();

      // ----- Kinetic Blade (6th): element selection + per-element packages -----
      var elementFeatures = new List<BlueprintFeature>();
      for (int i = 0; i < Elements.Length; i++)
      {
        var e = Elements[i];
        var weapon = ItemWeaponConfigurator.New(BladeWeaponName + e.Key, BladeWeaponGuid(i))
          .SetType(WeaponTypeRefs.KineticBlastEnergyBlade.ToString())
          .SetDisplayNameText($"Kinetic Blade ({e.Key})")
          .SetDescriptionText(
            "A blade of living elemental force, functioning as the kinetic knight's blade. " +
            "Its damage grows with the Sanguine Font's level (as a kineticist two levels lower).")
          .SetOverrideDamageDice()
          .SetDamageDice(new DiceFormula(1, DiceType.D6))
          .SetOverrideDamageType()
          .SetDamageType(new DamageTypeDescription
          {
            Type = DamageType.Energy,
            Energy = e.Energy,
          })
          .Configure();

        var buff = BuffConfigurator.New(BladeBuffName + e.Key, BladeBuffGuid(i))
          .SetDisplayName("SanguineKineticBladeBuff.Name")
          .SetDescription("SanguineKineticBladeBuff.Description")
          .SetIcon(bladeIcon)
          .AddSecondaryAttacks(weapon)
          .AddComponent(new SanguineBladeRider
          {
            CharacterClass = bloodrager,
            BladeWeapon = weapon,
            Energy = e.Energy,
          })
          .Configure();

        var toggle = ActivatableAbilityConfigurator.New(
            BladeToggleName + e.Key, BladeToggleGuid(i))
          .SetDisplayName($"SanguineKineticBlade{e.Key}.Name")
          .SetDescription("SanguineKineticBlade.Description")
          .SetIcon(bladeIcon)
          .SetBuff(buff)
          .Configure();

        var feature = FeatureConfigurator.New(
            ElementFeatureName + e.Key, ElementFeatureGuid(i))
          .SetDisplayName($"SanguineElement{e.Key}.Name")
          .SetDescription($"SanguineElement{e.Key}.Description")
          .SetIcon(bladeIcon)
          .SetIsClassFeature()
          .AddFacts(new() { toggle })
          .Configure();
        elementFeatures.Add(feature);
      }

      var elementSelection = FeatureSelectionConfigurator.New(
          ElementSelectionName, Guids.SanguineElementSelection)
        .SetDisplayName("SanguineElement.Name")
        .SetDescription("SanguineElement.Description")
        .SetIcon(bladeIcon)
        .SetObligatory(true)
        .SetHideNotAvailibleInUI(true)
        .SetAllFeatures(elementFeatures.Cast<Blueprint<BlueprintFeatureReference>>().ToArray())
        .Configure();

      // ----- Kinetic Blast (11th) -----
      var blastWeapon = ItemWeaponConfigurator.New(BlastWeaponName, Guids.SanguineKineticBlastWeapon)
        .SetType(WeaponTypeRefs.KineticBlastEnergy.ToString())
        .SetDisplayNameText("Kinetic Blast")
        .SetDescriptionText(
          "A bolt of elemental force, as a kineticist two levels lower would fire it - " +
          "with none of a kineticist's infusions or extra effects.")
        .SetOverrideDamageDice()
        .SetDamageDice(new DiceFormula(1, DiceType.D6))
        .Configure();

      var elementFeatureArray = elementFeatures.ToArray();
      var blastAbility = AbilityConfigurator.New(BlastAbilityName, Guids.SanguineKineticBlastAbility)
        .SetDisplayName("SanguineKineticBlast.Name")
        .SetDescription("SanguineKineticBlast.Description")
        .SetIcon(blastIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Close)
        .SetActionType(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard)
        .AllowTargeting(enemies: true)
        .SetEffectOnEnemy(AbilityEffectOnUnit.Harmful)
        .AddAbilityEffectRunAction(ActionsBuilder.New()
          .Add(new SanguineBlastAction
          {
            CharacterClass = bloodrager,
            BlastWeapon = blastWeapon,
            ElementFeatures = elementFeatureArray,
            Energies = Elements.Select(x => x.Energy).ToArray(),
          })
          .Build())
        .Configure();

      var blastFeature = FeatureConfigurator.New(BlastFeatureName, Guids.SanguineKineticBlastFeature)
        .SetDisplayName("SanguineKineticBlast.Name")
        .SetDescription("SanguineKineticBlast.Description")
        .SetIcon(blastIcon)
        .SetIsClassFeature()
        .AddFacts(new() { blastAbility })
        .Configure();

      // ----- Limitless Rage is off-limits (the rage economy IS the class) -----
      FeatureConfigurator.For(LimitlessRageGuid)
        .AddPrerequisiteNoArchetype(Guids.SanguineFontArchetype, BloodragerClassGuid)
        .Configure();
      MissionFeats.Logger.Info("[sanguine] LimitlessRage blocked for Sanguine Fonts.");

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.SanguineFontArchetype, CharacterClassRefs.BloodragerClass)
          .SetLocalizedName(DisplayName)
          .SetLocalizedDescription(Description)
          .SetReplaceSpellbook(Guids.SanguineFontSpellbook);

      archetype = ArchetypeRemovals.AddRemovals(
        archetype, bloodrager,
        FeatureRefs.BloodragerProficiencies.ToString(),
        BloodragerDamageReductionGuid,
        UncannyDodgeGuid,
        ImprovedUncannyDodgeGuid);

      archetype
        .AddToAddFeatures(LevelPlan.L(1), VitalBloodName, ProficienciesName)
        .AddToAddFeatures(LevelPlan.L(2), PulseFeatureName)
        .AddToAddFeatures(LevelPlan.L(5), SharedVitalityName)
        .AddToAddFeatures(LevelPlan.L(6), ElementSelectionName)
        .AddToAddFeatures(LevelPlan.L(8), GreaterPulseName)
        .AddToAddFeatures(LevelPlan.L(11), BlastFeatureName)
        .AddToAddFeatures(LevelPlan.L(20), ApotheosisName)
        .Configure(delayed: true);
    }

    /// <summary>Clone of the bloodrager spellbook with the trimmed list.</summary>
    private static BlueprintSpellbook SpellbookConfiguratorFor(
      BlueprintSpellbook source, BlueprintSpellList list)
    {
      var book = BlueprintCore.Blueprints.Configurators.Classes.Spells.SpellbookConfigurator
        .New("SanguineFontSpellbook", Guids.SanguineFontSpellbook)
        .Configure();
      const System.Reflection.BindingFlags flags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly;
      foreach (var field in typeof(BlueprintSpellbook).GetFields(flags))
      {
        try
        {
          field.SetValue(book, field.GetValue(source));
        }
        catch
        {
          // Init-only or compiler-generated members are skipped.
        }
      }
      foreach (var field in typeof(BlueprintScriptableObject).GetFields(flags))
      {
        if (field.Name == "m_AssetGuid")
        {
          continue;
        }
        try
        {
          field.SetValue(book, field.GetValue(source));
        }
        catch
        {
          // Skipped.
        }
      }
      var listField = typeof(BlueprintSpellbook).GetFields(
          System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
          System.Reflection.BindingFlags.NonPublic)
        .FirstOrDefault(f => f.FieldType == typeof(BlueprintSpellListReference));
      if (listField != null)
      {
        listField.SetValue(book, list.ToReference<BlueprintSpellListReference>());
      }
      else
      {
        MissionFeats.Logger.Warn("[sanguine] spellbook list field not found - book uses source list!");
      }
      return book;
    }

    /// <summary>True while the owner is bloodraging.</summary>
    private static bool IsRaging(UnitEntityData unit, BlueprintBuff rageBuff) =>
      rageBuff != null && unit.HasFact(rageBuff);

    /// <summary>
    /// Every living ally of the owner within range, including the owner.
    /// </summary>
    internal static List<UnitEntityData> AlliesWithin(UnitEntityData owner, int feet)
    {
      var result = new List<UnitEntityData>();
      float meters = feet.Feet().Meters;
      using var enumerator = Game.Instance.State.Units.GetEnumerator();
      while (enumerator.MoveNext())
      {
        var unit = enumerator.Current;
        if (unit is null || unit.Descriptor.State.IsDead)
        {
          continue;
        }
        if (unit != owner && !unit.IsAlly(owner))
        {
          continue;
        }
        if (unit.DistanceTo(owner) > meters)
        {
          continue;
        }
        result.Add(unit);
      }
      return result;
    }
  }

  /// <summary>
  /// Vital Blood: while bloodraging, at the start of each of the Font's turns,
  /// every ally in the aura below maximum HP regains HP (1d4 + 1/3 level within
  /// 10 ft; at apotheosis 2d8 + Con + Cha + 1/8 current HP + level within 40 ft).
  /// </summary>
  [TypeId(Guids.SanguineAuraComponent)]
  internal class SanguineAura : UnitFactComponentDelegate, ITickEachRound
  {
    public BlueprintCharacterClass CharacterClass;
    public BlueprintBuff RageBuff;
    public BlueprintFeature ApotheosisFeature;

    private bool Raging => RageBuff != null && Owner.HasFact(RageBuff);

    void ITickEachRound.OnNewRound()
    {
      try
      {
        if (!Raging)
        {
          return;
        }
        int level = Owner.Descriptor.Progression.GetClassLevel(CharacterClass);
        bool apotheosis = ApotheosisFeature != null && Owner.HasFact(ApotheosisFeature);
        int healed = 0;
        foreach (var ally in SanguineFont.AlliesWithin(Owner, apotheosis ? 40 : 10))
        {
          if (ally.HPLeft >= ally.Descriptor.Stats.HitPoints)
          {
            continue; // only allies below maximum HP
          }
          int amount = apotheosis
            ? SanguineFont.Roll(2, 8) + Owner.Stats.Constitution.Bonus
              + Owner.Stats.Charisma.Bonus + Math.Max(0, Owner.HPLeft) / 8 + level
            : SanguineFont.Roll(1, 4) + level / 3;
          if (amount <= 0)
          {
            continue;
          }
          Rulebook.Trigger(new RuleHealDamage(Owner, ally, amount));
          healed++;
        }
        if (healed > 0)
        {
          MissionFeats.Logger.Info(
            $"[sanguine] vital blood: {healed} all(y/ies) healed by {Owner.CharacterName}.");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("SanguineFont: aura tick failed.", e);
      }
    }
  }

  /// <summary>
  /// Bloodletting Pulse: swift action (free at apotheosis) while bloodraging;
  /// expends 1 additional round of bloodrage and heals all allies within 20 ft
  /// (30 ft from 8th) for 1d8 + 1/3 level (2d8 + level + Con from 8th). The
  /// shared variant additionally grants fast healing (2/3/5 by level) for 3
  /// rounds to allies while the Font takes an equal unremovable bleed for
  /// 3 rounds.
  /// </summary>
  [TypeId(Guids.SanguinePulseComponent)]
  public class SanguinePulse : ContextAction
  {
    public BlueprintCharacterClass CharacterClass;
    public BlueprintBuff RageBuff;
    public BlueprintAbilityResource RageResource;
    public BlueprintFeature GreaterFeature;
    public BlueprintBuff UsedBuff;
    public bool AllowRepeat;
    public bool Shared;

    public override string GetCaption() => "SanguinePulse";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        if (caster is null || RageBuff is null || !caster.HasFact(RageBuff))
        {
          MissionFeats.Logger.Warn(
            "[sanguine] pulse refused: the Font is not bloodraging.");
          return;
        }
        if (!AllowRepeat && UsedBuff != null && caster.Buffs.GetBuff(UsedBuff) != null)
        {
          MissionFeats.Logger.Warn(
            "[sanguine] pulse refused: once per round (apotheosis lifts this).");
          return;
        }
        int rounds = caster.Descriptor.Resources.GetResourceAmount(RageResource);
        if (rounds < 1)
        {
          MissionFeats.Logger.Warn(
            "[sanguine] pulse refused: no bloodrage rounds left to expend.");
          return;
        }
        caster.Descriptor.Resources.Spend(RageResource, 1);
        if (UsedBuff != null)
        {
          caster.AddBuff(UsedBuff, Context, ContextDuration.Fixed(1).Calculate(Context).Seconds);
        }

        int level = caster.Descriptor.Progression.GetClassLevel(CharacterClass);
        bool greater = GreaterFeature != null && caster.HasFact(GreaterFeature);
        int amount = greater
          ? SanguineFont.Roll(2, 8) + level + caster.Stats.Constitution.Bonus
          : SanguineFont.Roll(1, 8) + level / 3;
        int radius = greater ? 30 : 20;
        int healedCount = 0;
        int fhValue = level >= 16 ? 5 : level >= 11 ? 3 : 2;
        foreach (var ally in SanguineFont.AlliesWithin(caster, radius))
        {
          if (ally.HPLeft >= ally.Descriptor.Stats.HitPoints && !Shared)
          {
            continue; // full-health allies skip the plain pulse (shared still applies FH)
          }
          if (ally.HPLeft < ally.Descriptor.Stats.HitPoints)
          {
            Rulebook.Trigger(new RuleHealDamage(caster, ally, amount));
            healedCount++;
          }
          if (Shared && ally != caster)
          {
            ally.AddBuff(BlueprintTool.Get<BlueprintBuff>(FastHealingGuid(fhValue)), Context,
              ContextDuration.Fixed(3).Calculate(Context).Seconds);
          }
        }
        if (Shared)
        {
          var selfBleed = caster.Descriptor.AddBuff(
            BlueprintTool.Get<BlueprintBuff>(BleedGuid(fhValue)), Context,
            ContextDuration.Fixed(3).Calculate(Context).Seconds);
          if (selfBleed != null)
          {
            selfBleed.IsNotDispelable = true;
          }
        }
        MissionFeats.Logger.Info(
          $"[sanguine] bloodletting pulse{(Shared ? " (shared)" : "")}: " +
          $"{healedCount} healed for {amount} within {radius} ft (-1 bloodrage round).");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("SanguineFont: pulse failed.", e);
      }
    }

    private static string FastHealingGuid(int value) => value switch
    {
      3 => Guids.SanguineFastHealingBuff3,
      5 => Guids.SanguineFastHealingBuff5,
      _ => Guids.SanguineFastHealingBuff2,
    };

    private static string BleedGuid(int value) => value switch
    {
      3 => Guids.SanguineBleedBuff3,
      5 => Guids.SanguineBleedBuff5,
      _ => Guids.SanguineBleedBuff2,
    };
  }

  /// <summary>
  /// Kinetic Blade rider: attacks with the conjured blade deal its element's
  /// damage plus one extra d6 per two (class - 2) levels beyond 1st and the
  /// Constitution modifier (kineticist level = class level - 2).
  /// </summary>
  [TypeId(Guids.SanguineBladeComponent)]
  internal class SanguineBladeRider : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass CharacterClass;
    public BlueprintItemWeapon BladeWeapon;
    public DamageEnergyType Energy;

    public void OnEventAboutToTrigger(RulePrepareDamage evt) { }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.DamageBundle.Weapon?.Blueprint != BladeWeapon)
        {
          return;
        }
        int level = Owner.Descriptor.Progression.GetClassLevel(CharacterClass);
        int kineticist = Math.Max(1, level - 2);
        int dice = 1 + Math.Max(0, (kineticist - 1) / 2); // 1st: 1d6, 3rd: 2d6, 5th: 3d6...
        int extraDice = Math.Max(0, dice - 1);
        var bonus = new EnergyDamage(
          extraDice > 0 ? new DiceFormula(extraDice, DiceType.D6) : DiceFormula.Zero,
          Owner.Stats.Constitution.Bonus, Energy)
        {
          SourceFact = Fact,
        };
        evt.Add(bonus);
        MissionFeats.Logger.Info(
          $"[sanguine] kinetic blade: +{extraDice}d6+{Owner.Stats.Constitution.Bonus} {Energy}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("SanguineFont: blade rider failed.", e);
      }
    }
  }

  /// <summary>
  /// Kinetic Blast: a ranged touch attack (the kineticist's own blast weapon
  /// type) dealing the chosen element's damage - 1d6 per odd (class - 2) level
  /// plus Con - with no infusions or other effects.
  /// </summary>
  [TypeId(Guids.SanguineBlastComponent)]
  public class SanguineBlastAction : ContextAction
  {
    public BlueprintCharacterClass CharacterClass;
    public BlueprintItemWeapon BlastWeapon;
    public BlueprintFeature[] ElementFeatures;
    public DamageEnergyType[] Energies;

    public override string GetCaption() => "SanguineBlast";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        var target = Target.Unit;
        if (caster is null || target is null || BlastWeapon is null)
        {
          return;
        }
        int element = -1;
        for (int i = 0; i < ElementFeatures.Length; i++)
        {
          if (ElementFeatures[i] != null && caster.HasFact(ElementFeatures[i]))
          {
            element = i;
            break;
          }
        }
        if (element < 0)
        {
          MissionFeats.Logger.Warn(
            "[sanguine] blast refused: no element chosen.");
          return;
        }
        var weapon = BlastWeapon.CreateEntity<ItemEntityWeapon>();
        var rule = new RuleAttackRoll(caster, target, weapon, 0);
        Context.TriggerRule(rule);
        if (!rule.IsHit)
        {
          MissionFeats.Logger.Info("[sanguine] kinetic blast missed.");
          return;
        }
        int level = caster.Descriptor.Progression.GetClassLevel(CharacterClass);
        int kineticist = Math.Max(1, level - 2);
        int dice = 1 + Math.Max(0, (kineticist - 1) / 2);
        var bundle = new DamageBundle();
        bundle.Add(new EnergyDamage(
          new DiceFormula(dice, DiceType.D6),
          caster.Stats.Constitution.Bonus, Energies[element]));
        Rulebook.Trigger(new RuleDealDamage(caster, target, bundle));
        MissionFeats.Logger.Info(
          $"[sanguine] kinetic blast hit {target.CharacterName}: " +
          $"{dice}d6+{caster.Stats.Constitution.Bonus} {Energies[element]}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("SanguineFont: blast failed.", e);
      }
    }
  }

  /// <summary>
  /// Sanguine Apotheosis death save: once per bloodrage, when an ally in the
  /// 40-ft aura would die (HP brought to 0 or below), the Font may expend ALL
  /// remaining bloodrage rounds; the ally instead remains at 1 HP and is healed
  /// for 10d8 + bloodrager level.
  /// </summary>
  [TypeId(Guids.SanguineDeathSaveComponent)]
  internal class SanguineDeathSave : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    ITargetRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass CharacterClass;
    public BlueprintBuff RageBuff;
    public BlueprintAbilityResource RageResource;

    private bool usedThisRage;

    private bool Raging => RageBuff != null && Owner.HasFact(RageBuff);

    public void OnEventAboutToTrigger(RuleDealDamage evt)
    {
      try
      {
        // The rage ending resets the once-per-rage usage.
        if (!Raging)
        {
          usedThisRage = false;
        }
      }
      catch
      {
        // Never let the reset check throw.
      }
    }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        if (!Raging || usedThisRage)
        {
          return;
        }
        var ally = evt.Target;
        if (ally is null || ally.Descriptor.State.IsDead)
        {
          return;
        }
        if (ally != Owner && !ally.IsAlly(Owner))
        {
          return;
        }
        if (ally.DistanceTo(Owner) > 40.Feet().Meters)
        {
          return;
        }
        if (ally.HPLeft > 0)
        {
          return; // not falling
        }
        int rounds = Owner.Descriptor.Resources.GetResourceAmount(RageResource);
        if (rounds < 1)
        {
          return;
        }
        Owner.Descriptor.Resources.Spend(RageResource, rounds);
        usedThisRage = true;
        int level = Owner.Descriptor.Progression.GetClassLevel(CharacterClass);
        int heal = (1 - ally.HPLeft) + SanguineFont.Roll(10, 8) + level;
        Rulebook.Trigger(new RuleHealDamage(Owner, ally, heal));
        MissionFeats.Logger.Info(
          $"[sanguine] apotheosis death save: {ally.CharacterName} saved at 1 HP " +
          $"(+{heal} healed); {rounds} bloodrage round(s) expended.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("SanguineFont: death save failed.", e);
      }
    }
  }

  /// <summary>Fast healing N: heals the owner each round.</summary>
  [TypeId(Guids.SanguineFastHealComponent)]
  internal class SanguineFastHeal : UnitBuffComponentDelegate, ITickEachRound
  {
    public int Amount;

    void ITickEachRound.OnNewRound()
    {
      try
      {
        Rulebook.Trigger(new RuleHealDamage(Owner, Owner, Amount));
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("SanguineFont: fast healing tick failed.", e);
      }
    }
  }

  /// <summary>The Shared Vitality self-bleed: damages the owner each round.</summary>
  [TypeId(Guids.SanguineBleedComponent)]
  internal class SanguineBleed : UnitBuffComponentDelegate, ITickEachRound
  {
    public int Amount;

    void ITickEachRound.OnNewRound()
    {
      try
      {
        var bundle = new DamageBundle();
        bundle.Add(new DirectDamage(DiceFormula.Zero, Amount));
        Rulebook.Trigger(new RuleDealDamage(Owner, Owner, bundle));
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("SanguineFont: bleed tick failed.", e);
      }
    }
  }
}

using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.Configurators;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker;
using Kingmaker.AI.Blueprints;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.TurnBasedModifiers;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Construct Crafter (homebrew Alchemist archetype, user design)
  ///
  /// A metallics-themed engineer who deploys construct "summons" - deliberately NOT animal
  /// companions: they are uncontrollable (AI-driven), up to three can be active at once
  /// (one per base), and they are far less customizable than a companion.
  ///
  /// REAL LEVEL PLAN (used when LevelPlan.AllAtLevelOne is false):
  ///   L1  Deploy Clockwork Sentry (dog base), extra combat feat, Basic Core selection,
  ///       Basic Program selection
  ///   L7  Deploy Humanoid Construct (humanoid base: fighter with AL-2 levels)
  ///   L16 Deploy Clay Golem (golem base: tabletop clay golem minus berserk, -20 HP, -2 Str)
  ///
  /// A base is deployed as a standard action; the construct lasts until destroyed or until
  /// the same base is deployed again (the old one is replaced). Cores and programs are
  /// chosen before deployment (selections); v1 ships the Basic core (the stat package
  /// baked into each base) and the Basic program (stock run-at-enemy AI).
  ///
  /// Chassis notes: keeps the alchemist's BAB/HD; removes mutagen and poison
  /// resistance/poison use/poison immunity features. See docs/ARCHETYPES.md for the full
  /// adaptation list (extracts CANNOT be removed by an archetype - class-level data).
  /// </summary>
  public class ConstructCrafter
  {
    internal static readonly LogWrapper Logger = LogWrapper.Get("MissionWOTR.ConstructCrafter");

    internal const string ArchetypeName = "ConstructCrafter";
    internal const string DisplayName = "ConstructCrafter.Name";
    internal const string Description = "ConstructCrafter.Description";

    internal const string SentryUnitName = "ConstructCrafterIronSentry";
    internal const string HumanoidUnitName = "ConstructCrafterHumanoidConstruct";
    internal const string GolemUnitName = "ConstructCrafterClayGolem";

    internal const string DeploySentryFeatureName = "ConstructCrafterDeploySentry";
    internal const string DeployHumanoidFeatureName = "ConstructCrafterDeployHumanoid";
    internal const string DeployGolemFeatureName = "ConstructCrafterDeployGolem";
    internal const string DeploySentryAbilityName = "ConstructCrafterDeploySentryAbility";
    internal const string DeployHumanoidAbilityName = "ConstructCrafterDeployHumanoidAbility";
    internal const string DeployGolemAbilityName = "ConstructCrafterDeployGolemAbility";

    internal const string CoreSelectionName = "ConstructCrafterCoreSelection";
    internal const string ProgramSelectionName = "ConstructCrafterProgramSelection";
    internal const string BasicCoreName = "ConstructCrafterBasicCore";
    internal const string BasicProgramName = "ConstructCrafterBasicProgram";

    internal const string PlatingBuffName = "ConstructCrafterClockworkPlating";
    internal const string ProficienciesName = "ConstructCrafterProficiencies";

    // Runtime handles for the deploy action.
    internal static BlueprintUnit SentryUnit;
    internal static BlueprintUnit HumanoidUnit;
    internal static BlueprintUnit GolemUnit;
    internal static BlueprintBuff ClockworkPlatingBuff;
    internal static BlueprintBuff CrafterMarkerBuff;

    public static void Configure()
    {
      // Every step gets its own try/catch: a failure in one step is logged and the
      // rest still run, so a single playtest log reveals ALL broken steps at once
      // instead of one per build. (Cores crashing used to hide Abilities/Chassis/
      // selections/archetype, none of which have ever executed yet.)
      Step("units", ConfigureUnits);
      Step("programs", ConstructCrafterPrograms.Configure);
      Step("cores", ConstructCrafterCores.Configure);
      Step("abilities", ConstructCrafterAbilities.Configure);
      Step("chassis", ConfigureChassis);
      Step("selections", ConfigureCoresAndPrograms);
      Step("deploy-bases", ConfigureDeployBases);
      Step("archetype", ConfigureArchetype);
    }

    private static void Step(string name, Action action)
    {
      try
      {
        action();
        Logger.Info($"[CC] step {name}: ok.");
      }
      catch (Exception e)
      {
        Logger.Error($"[CC] step {name}: FAILED - {e.Message}", e);
      }
    }

    private static void ConfigureDeployBases()
    {
      DeployBase(
        DeploySentryFeatureName, Guids.ConstructCrafterDeploySentryFeature,
        DeploySentryAbilityName, Guids.ConstructCrafterDeploySentryAbility,
        "DeploySentry.Name", "DeploySentry.Description", SentryUnit,
        applyPlating: true, addFighterLevels: false, icon: FeatureRefs.RideAnimalCompanionFeature,
        baseKind: 0);

      DeployBase(
        DeployHumanoidFeatureName, Guids.ConstructCrafterDeployHumanoidFeature,
        DeployHumanoidAbilityName, Guids.ConstructCrafterDeployHumanoidAbility,
        "DeployHumanoid.Name", "DeployHumanoid.Description", HumanoidUnit,
        applyPlating: false, addFighterLevels: true,
        icon: FeatureRefs.MartialWeaponProficiency, baseKind: 1);

      DeployBase(
        DeployGolemFeatureName, Guids.ConstructCrafterDeployGolemFeature,
        DeployGolemAbilityName, Guids.ConstructCrafterDeployGolemAbility,
        "DeployGolem.Name", "DeployGolem.Description", GolemUnit,
        applyPlating: false, addFighterLevels: false, icon: FeatureRefs.HeavyArmorProficiency,
        baseKind: 2);

    }

    private static void ConfigureArchetype()
    {
      // ----- The archetype itself -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.ConstructCrafterArchetype, CharacterClassRefs.AlchemistClass)
          .SetLocalizedName(DisplayName)
          .SetLocalizedDescription(Description);

      // Chassis removals (mutagen + the poison-resistance chain). Levels are derived
      // from the live alchemist progression: the char-gen gate (CanAddArchetype)
      // silently rejects archetypes whose removals do not match what the class
      // actually grants at that level - hardcoded guesses made CC apply as a plain
      // alchemist. (See docs/ARCHETYPES.md for the poison-feature notes.)
      archetype = ArchetypeRemovals.AddRemovals(
        archetype,
        CharacterClassRefs.AlchemistClass.Reference.Get(),
        FeatureRefs.AlchemistMutagen.ToString(),
        FeatureRefs.PoisonResistance.ToString(),
        FeatureRefs.PoisonResistance4Feature.ToString(),
        FeatureRefs.ImmunityToPoison.ToString());

      archetype = archetype
          // Core kit.
          .AddToAddFeatures(LevelPlan.L(1), FeatureSelectionRefs.FighterFeatSelection.ToString())
          .AddToAddFeatures(LevelPlan.L(1), CoreSelectionName, ProgramSelectionName)
          // Bases.
          .AddToAddFeatures(LevelPlan.L(1), DeploySentryFeatureName)
          .AddToAddFeatures(LevelPlan.L(7), DeployHumanoidFeatureName)
          .AddToAddFeatures(LevelPlan.L(16), DeployGolemFeatureName)
          // Chassis: proficiencies (plus vanilla simple-weapon proficiency).
          .AddToAddFeatures(LevelPlan.L(1),
            ProficienciesName, FeatureRefs.SimpleWeaponProficiency.ToString());
      // Dampened synthesis: one step every 2 levels (extracts cast at ~half level).
      for (int i = 1; i <= 9; i++)
      {
        archetype = archetype.AddToAddFeatures(
          LevelPlan.L(i * 2), $"ConstructCrafterDampenedSynthesis{i}");
      }
      // A new program every 4th level.
      for (int level = 4; level <= 20; level += 4)
      {
        archetype = archetype.AddToAddFeatures(LevelPlan.L(level), ProgramSelectionName);
      }
      // A new core at 3/8/13/19 (one pick each - purposefully limited).
      foreach (var coreLevel in new[] { 3, 8, 13, 19 })
      {
        archetype = archetype.AddToAddFeatures(LevelPlan.L(coreLevel), CoreSelectionName);
      }
      if (LevelPlan.AllAtLevelOne)
      {
        // TEST MODE: every program, every core, every caster-level step at level 1.
        archetype = archetype.AddToAddFeatures(
          1,
          ConstructCrafterPrograms.AllFeatureNames
            .Select(f => (Blueprint<BlueprintFeatureBaseReference>)f)
            .ToArray());
        archetype = archetype.AddToAddFeatures(
          1,
          ConstructCrafterCores.AllFeatureNames
            .Select(f => (Blueprint<BlueprintFeatureBaseReference>)f)
            .ToArray());
        // Dampened synthesis steps are intentionally NOT dumped at level 1: they are
        // nine stackable -1 extract-caster-level penalties (one per two real levels).
        // Granting all nine at level 1 drove the extract caster level negative and
        // produced a bizarre spellbook in the first CC playtest. The penalty mechanic
        // is a pure numeric modifier and is exercised by real-level play instead.
      }
      archetype.Configure();
    }

    private static void ConfigureUnits()
    {
      // Summons belong to the game's own "Summoned" faction - the same faction every
      // stock summon monster and ExpandedContent's golem summons use. (The animal
      // companion's faction used previously is for party pets, not combat summons.)
      var summonFaction = FactionRefs.Summoned.Reference.Get();

      // Units are built by FULL-CLONING one of the game's own SUMMON-VARIANT units
      // (the units stock summon spells use, e.g. GolemWoodSummon). Critical lessons
      // from the first playtests: BPCore's CopyFrom(blueprint) with no matcher binds
      // to the params-Type[] overload and copies NOTHING (zero types = zero
      // components), and even with a matcher it never touches FIELDS - so the old
      // units had no model prefab, no brain wiring, no attack routines (the visible
      // "dog" was a fallback render, and the sentry just stood there). CloneUnit
      // copies every component AND every BlueprintUnit field (prefab/model, size,
      // stats, brain, sounds); the summon variants additionally bring the summon
      // brain, Summoned faction baseline and a pre-tuned statblock; overrides are
      // applied on top via configurator setters.
      var woodSummon = UnitRefs.GolemWoodSummon.Reference.Get();

      // --- Iron Sentry: wrought-iron scout construct (wood golem model, gunmetal
      // tint). The wood golem is the only dog-shaped construct model in the game -
      // the grey tint reads it as forged metal. --- 
      SentryUnit = CloneUnit(SentryUnitName, Guids.ConstructCrafterSentryUnit, woodSummon);
      UnitConfigurator.For(SentryUnitName)
        .SetMaxHP(Math.Max(8, woodSummon.MaxHP / 3))
        .SetStrength(woodSummon.Strength - 4)
        .SetDexterity(woodSummon.Dexterity - 2)
        .SetFaction(summonFaction)
        .SetColor(new UnityEngine.Color(0.42f, 0.45f, 0.48f))
        .Configure();

      // --- Humanoid Construct: fighter with (AL-2) levels, applied at deploy time ---
      var bandit = UnitRefs.CR0_5_Bandit_Human_FighterMelee_Male.Reference.Get();
      HumanoidUnit = CloneUnit(HumanoidUnitName, Guids.ConstructCrafterHumanoidUnit, bandit);
      UnitConfigurator.For(HumanoidUnitName)
        .SetFaction(summonFaction)
        .Configure();

      // --- Clay Golem: tabletop chassis (no berserk, -20 HP, -2 Str) ---
      // Built on the game's stone golem SUMMON variant (GolemStoneSummon); the slow
      // breath component is stripped and the golem's physical DR replaced with our
      // constant 5/adamantine package (see adaptation notes in docs/ARCHETYPES.md).
      var stoneSummon = UnitRefs.GolemStoneSummon.Reference.Get();
      GolemUnit = CloneUnit(GolemUnitName, Guids.ConstructCrafterGolemUnit, stoneSummon,
        c => !c.name.Contains("Slow")
          && c is not Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical);
      UnitConfigurator.For(GolemUnitName)
        .SetStrength(32 - 2)
        .SetMaxHP(107 - 20)
        .SetFaction(summonFaction)
        .AddDamageResistancePhysical(
          value: 5, bypassedByMaterial: true, material: PhysicalDamageMaterial.Adamantite)
        .Configure();

      // --- Clockwork Plating: the sentry's scaling DR (half alchemist level). ---
      ClockworkPlatingBuff = BuffConfigurator.New(PlatingBuffName, Guids.ConstructCrafterPlatingBuff)
        .SetDisplayName("ClockworkPlating.Name")
        .SetDescription("ClockworkPlating.Description")
        .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
        .AddDamageResistancePhysical(value: ContextValues.Rank(), bypassedByMaterial: true,
          material: PhysicalDamageMaterial.Adamantite)
        .AddContextRankConfig(
          ContextRankConfigs.ClassLevel(new[] { CharacterClassRefs.AlchemistClass.ToString() })
            .WithDiv2Progression())
        .Configure();

      // --- Crafter marker: sits on the crafter while constructs are deployed so the
      // AI follow actions can home in on them (the program markers only exist while a
      // program toggle is on; without a marker the follow action has no target). ---
      CrafterMarkerBuff = BuffConfigurator.New(
          "ConstructCrafterCrafterMarker", Guids.CrafterMarkerBuff)
        .SetDisplayName("CrafterMarker.Name")
        .SetDescription("CrafterMarker.Description")
        .SetIcon(FeatureRefs.CombatReflexes.Reference.Get().Icon)
        .Configure();
    }

    /// <summary>
    /// Creates a new unit blueprint that is a full clone of a stock unit: every
    /// component (via CopyFrom with an all-matcher) plus every field declared on
    /// BlueprintUnit itself (model prefab, size, base stats, brain, sounds) copied
    /// by reflection. Callers then re-apply their overrides with configurator
    /// setters - field copies would otherwise clobber them.
    /// </summary>
    internal static Kingmaker.Blueprints.BlueprintUnit CloneUnit(
      string name, string guid, Kingmaker.Blueprints.BlueprintUnit source,
      Predicate<Kingmaker.Blueprints.BlueprintComponent> componentMatcher = null)
    {
      var configurator = UnitConfigurator.New(name, guid);
      if (componentMatcher != null)
      {
        configurator = configurator.CopyFrom(source, componentMatcher);
      }
      else
      {
        configurator = configurator.CopyFrom(source, _ => true);
      }
      var unit = configurator.Configure();

      const System.Reflection.BindingFlags flags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly;
      int copied = 0;
      foreach (var field in typeof(Kingmaker.Blueprints.BlueprintUnit).GetFields(flags))
      {
        try
        {
          field.SetValue(unit, field.GetValue(source));
          copied++;
        }
        catch
        {
          // Init-only or compiler-generated members are skipped.
        }
      }
      Logger.Info(
        $"[units] {name} cloned from {source.name}: {copied} fields, " +
        $"prefab={(unit.Prefab != null ? "set" : "NULL")}, " +
        $"brain={(unit.DefaultBrain != null ? unit.DefaultBrain.name : "NONE")}.");
      return unit;
    }

    private static void ConfigureChassis()
    {
      // Proficiencies: light armor, bows, throwing axes, flail, heavy flail, warhammer,
      // greatclub (simple weapons come from the vanilla proficiency feature).
      FeatureConfigurator.New(ProficienciesName, Guids.ConstructCrafterProficiencies)
        .SetDisplayName("CrafterProficiencies.Name")
        .SetDescription("CrafterProficiencies.Description")
        .SetIcon(FeatureRefs.LightArmorProficiency.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddProficiencies(
          armorProficiencies: new[] { ArmorProficiencyGroup.Light },
          weaponProficiencies: new[]
          {
            WeaponCategory.Longbow, WeaponCategory.Shortbow, WeaponCategory.ThrowingAxe,
            WeaponCategory.Flail, WeaponCategory.HeavyFlail, WeaponCategory.Warhammer,
            WeaponCategory.Greatclub,
          })
        .Configure();

      // Dampened synthesis: extracts are cast at roughly half the alchemist's level
      // (nine stackable -1 caster-level steps, one per two levels).
      for (int i = 1; i <= 9; i++)
      {
        FeatureConfigurator.New($"ConstructCrafterDampenedSynthesis{i}", DampenedSynthesisGuid(i))
          .SetDisplayName("DampenedSynthesis.Name")
          .SetDescription("DampenedSynthesis.Description")
          .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
          .SetIsClassFeature()
          .AddCasterLevelForSpellbook(
            bonus: -1,
            spellbooks: new List<Blueprint<BlueprintSpellbookReference>>
            {
              SpellbookRefs.AlchemistSpellbook.Cast<BlueprintSpellbookReference>(),
            })
          .Configure();
      }
    }

    private static string DampenedSynthesisGuid(int step)
    {
      return step switch
      {
        1 => Guids.DampenedSynthesis1,
        2 => Guids.DampenedSynthesis2,
        3 => Guids.DampenedSynthesis3,
        4 => Guids.DampenedSynthesis4,
        5 => Guids.DampenedSynthesis5,
        6 => Guids.DampenedSynthesis6,
        7 => Guids.DampenedSynthesis7,
        8 => Guids.DampenedSynthesis8,
        _ => Guids.DampenedSynthesis9,
      };
    }

    private static void ConfigureCoresAndPrograms()
    {
      // Basic Core: the default stat package (baked into each base's blueprint).
      var basicCore = FeatureConfigurator.New(BasicCoreName, Guids.ConstructCrafterBasicCore)
        .SetDisplayName("BasicCore.Name")
        .SetDescription("BasicCore.Description")
        .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      // Basic Program: the default behavior package (stock run-at-enemy AI).
      var basicProgram = FeatureConfigurator.New(BasicProgramName, Guids.ConstructCrafterBasicProgram)
        .SetDisplayName("BasicProgram.Name")
        .SetDescription("BasicProgram.Description")
        .SetIcon(FeatureRefs.CombatReflexes.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      // Core & Program selections: chosen before deployment; all cores/programs slot
      // into the same selections. (Created exactly once - a second New on the same
      // name+guid throws "Already in use".)
      var coreFeatures = new List<Blueprint<BlueprintFeatureReference>> { basicCore };
      coreFeatures.AddRange(
        ConstructCrafterCores.Cores.Select(c => (Blueprint<BlueprintFeatureReference>)c.Feature));
      FeatureSelectionConfigurator.New(CoreSelectionName, Guids.ConstructCrafterCoreSelection)
        .SetDisplayName("CoreSelection.Name")
        .SetDescription("CoreSelection.Description")
        .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
        .SetAllFeatures(coreFeatures.ToArray())
        .Configure();

      var programFeatures = new List<Blueprint<BlueprintFeatureReference>> { basicProgram };
      programFeatures.AddRange(
        ConstructCrafterPrograms.Programs.Select(p => (Blueprint<BlueprintFeatureReference>)p.Feature));
      FeatureSelectionConfigurator.New(ProgramSelectionName, Guids.ConstructCrafterProgramSelection)
        .SetDisplayName("ProgramSelection.Name")
        .SetDescription("ProgramSelection.Description")
        .SetIcon(FeatureRefs.CombatReflexes.Reference.Get().Icon)
        .SetAllFeatures(programFeatures.ToArray())
        .Configure();
    }

    private static void DeployBase(
      string featureName,
      string featureGuid,
      string abilityName,
      string abilityGuid,
      string displayKey,
      string descriptionKey,
      BlueprintUnit unit,
      bool applyPlating,
      bool addFighterLevels,
      Blueprint<BlueprintReference<BlueprintFeature>> icon,
      int baseKind)
    {
      var deploy = ElementTool.Create<ContextActionDeployConstruct>();
      deploy.Unit = unit;
      deploy.ApplyPlating = applyPlating;
      deploy.AddFighterLevels = addFighterLevels;
      deploy.BaseKind = baseKind;

      var ability = AbilityConfigurator.New(abilityName, abilityGuid)
        .SetDisplayName(displayKey)
        .SetDescription(descriptionKey)
        .SetIcon(icon.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetAbilityIsFullRoundInTurnBased(
          new AbilityIsFullRoundInTurnBased { FullRoundIfTurnBased = true })
        .SetCanTargetSelf()
        // Explicitly player-facing: both flags default to false on a fresh
        // blueprint, but the deploy abilities were reported invisible in every
        // ability list while existing as facts - pin the flags and log them from
        // the area probe so the next log shows exactly what the UI sees.
        .SetHidden(false)
        .SetActionBarAutoFillIgnored(false)
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().Add(deploy))
        .Configure();

      FeatureConfigurator.New(featureName, featureGuid)
        .SetDisplayName(displayKey)
        .SetDescription(descriptionKey)
        .SetIcon(icon.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { ability })
        .Configure();
    }
  }

  /// <summary>
  /// Deploys a construct: replaces the crafter's existing construct of the same base,
  /// then spawns the new one nearby. Spawning uses the same engine call as ToyBox's unit
  /// browser (Game.Instance.EntityCreator.SpawnUnit with the loaded area's main state).
  /// </summary>
  [TypeId(Guids.DeployConstructAction)]
  internal class ContextActionDeployConstruct : ContextAction
  {
    /// <summary>The construct's unit blueprint (one per base).</summary>
    public BlueprintUnit Unit;

    /// <summary>Apply the scaling DR buff (clockwork sentry).</summary>
    public bool ApplyPlating;

    /// <summary>Give the construct fighter levels equal to (alchemist level - 2).</summary>
    public bool AddFighterLevels;

    /// <summary>
    /// The stock "summoned creature" processing buff every game summon receives
    /// (DarkCodex's Wrath helper uses the same id).
    /// </summary>
    private const string StockSummonBuffGuid = "8728e884eeaa8b047be04197ecf1a0e4";

    /// <summary>
    /// The game's own SummonMonsterPool - what DarkCodex registers its summons in
    /// (UseLimitFromSummonPool stays false, so the pool is bookkeeping only).
    /// </summary>
    private const string SummonPoolGuid = "d94c93e7240f10e41ae41db4c83d1cbe";

    /// <summary>Which base is deploying: 0 sentry, 1 humanoid, 2 golem.</summary>
    public int BaseKind;

    private static BlueprintUnit ResolveUnit(
      ConstructCrafterCores.CoreDef core, int baseKind, BlueprintUnit fallback)
    {
      if (core is null)
      {
        return fallback;
      }
      var arbalest = core.Name == "ConstructCrafterArbalest";
      var flaming = core.Name == "ConstructCrafterFlaming";
      var cold = core.Name == "ConstructCrafterCold";
      var soft = core.Name == "ConstructCrafterSoft";
      if (baseKind == 0)
      {
        return arbalest || soft ? ConstructCrafterAbilities.SentryRangedUnit : fallback;
      }
      if (baseKind == 1)
      {
        return arbalest ? ConstructCrafterAbilities.HumanoidArcherUnit
          : flaming || soft ? ConstructCrafterAbilities.HumanoidCasterUnit
          : fallback;
      }
      return arbalest || cold || soft ? ConstructCrafterAbilities.GolemCasterUnit : fallback;
    }

    internal static System.Collections.Generic.IEnumerable<BlueprintAbility> ResolveCoreAbilities(
      ConstructCrafterCores.CoreDef core, int baseKind)
    {
      if (core is null)
      {
        yield break;
      }
      var arbalest = core.Name == "ConstructCrafterArbalest";
      var flaming = core.Name == "ConstructCrafterFlaming";
      var cold = core.Name == "ConstructCrafterCold";
      var soft = core.Name == "ConstructCrafterSoft";
      var infernal = core.Name == "ConstructCrafterInfernal";
      // The archer humanoid uses its bow; other bases spit bolts.
      if (arbalest && baseKind != 1)
      {
        yield return ConstructCrafterAbilities.BoltSpit;
      }
      // Infernal constructs blink to their prey instead of walking.
      if (infernal)
      {
        yield return ConstructCrafterAbilities.BlinkStrike;
      }
      if (flaming && baseKind == 1)
      {
        yield return ConstructCrafterAbilities.FireBlast;
      }
      if (cold && baseKind == 2)
      {
        yield return ConstructCrafterAbilities.IceRay;
      }
      if (soft)
      {
        yield return ConstructCrafterAbilities.Mend;
      }
    }

    internal static void ApplySneakAttackRanks(
      UnitEntityData construct, int alchemistLevel, int dicePerLevels)
    {
      if (dicePerLevels <= 0)
      {
        return;
      }
      var dice = Math.Max(1, alchemistLevel / dicePerLevels);
      var sneakAttack = FeatureRefs.RogueSneakAttack.Reference.Get();
      var saFact = construct.AddFact(sneakAttack) as Feature;
      for (int i = 1; i < dice && saFact != null; i++)
      {
        saFact.AddRank();
      }
    }

    public override string GetCaption() => $"Deploy construct ({Unit?.name})";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        if (caster is null || Unit is null)
        {
          return;
        }

        var program = ConstructCrafterPrograms.GetActiveProgram(caster);
        var core = ConstructCrafterCores.GetActiveCore(caster);

        // Overdrive's raw power cannot coexist with the Chaos program.
        if (core?.IsOverdrive == true && program?.IsChaos == true)
        {
          MissionFeats.Logger.Error(
            "ConstructCrafter: Overdrive core cannot be combined with the Chaos program; deployment blocked.");
          return;
        }

        var alchemistLevel = caster.Descriptor.Progression
          .GetClassLevel(CharacterClassRefs.AlchemistClass.Reference.Get());

        var state = Game.Instance.State.LoadedAreaState.MainState;
        var existing = state.AllEntityData.OfType<UnitEntityData>().ToList();

        var baseMarker = BaseKind == 0 ? ConstructCrafterAbilities.SentryBaseMarker
          : BaseKind == 1 ? ConstructCrafterAbilities.ManBaseMarker
          : ConstructCrafterAbilities.GolemBaseMarker;

        // Chaos program: while a chaos construct of this base lives, it cannot be redeployed.
        if (program?.IsChaos == true)
        {
          var chaosAlive = existing.Any(
            u => u.HPLeft > 0 && u.Buffs.GetBuff(baseMarker) != null
              && u.Buffs.GetBuff(ConstructCrafterPrograms.ChaosMarker) != null);
          if (chaosAlive)
          {
            MissionFeats.Logger.Error(
              "ConstructCrafter: a chaos construct of this base still lives; deployment blocked.");
            return;
          }
        }

        // Replace the previous construct of this base (marker-based: any variant counts).
        foreach (var old in existing.Where(u => u.HPLeft > 0 && u.Buffs.GetBuff(baseMarker) != null))
        {
          old.IsInGame = false;
        }

        // Blueprint sweep of the same base family: saves made before 0.4.11 can hold
        // half-initialized construct units (their restore crashed in ItemEntity..ctor
        // before buffs ever loaded), which the marker check above cannot see. Any
        // living unit whose blueprint is one of this base's constructs goes away.
        var family = BaseKind == 0
          ? new[] { SentryUnitName, "ConstructCrafterSentryRanged" }
          : BaseKind == 1
            ? new[] { HumanoidUnitName, "ConstructCrafterHumanoidArcher", "ConstructCrafterHumanoidCaster" }
            : new[] { GolemUnitName, "ConstructCrafterGolemCaster" };
        foreach (var old in existing.Where(
          u => u.HPLeft > 0 && u.Blueprint != null &&
            family.Any(n => string.Equals(n, u.Blueprint.name,
              StringComparison.OrdinalIgnoreCase))))
        {
          MissionFeats.Logger.Info(
            $"[deploy] removing stray construct {old.Blueprint.name} (uid={old.UniqueId}).");
          old.IsInGame = false;
        }

        // Role variant: the active core may swap in a specialized chassis (archer,
        // caster, ranged) instead of the default base unit.
        var spawnUnit = ResolveUnit(core, BaseKind, Unit);

        // Spawn through the engine's own summon action - the same mechanism every
        // summon ability uses (field-for-field the way DarkCodex's Wrath helper and
        // ExpandedContent's WoodenPhalanx build it): it places the unit on reachable
        // ground, LINKS it to the caster (SummonedUnitsController then moves any
        // linked unit to its summoner while out of combat - the pre-combat utility
        // this archetype needs) and leaves it AI-controlled (IsDirectlyControllable=
        // false: our constructs are never controllable). All construct configuration
        // runs in AfterSpawn, whose action list executes in the fresh unit's data
        // scope, so the finisher reads Target.Unit to find the construct.
        var finisher = ElementTool.Create<ContextActionDeployFinish>();
        finisher.BaseKind = BaseKind;
        finisher.ApplyPlating = ApplyPlating;
        finisher.AddFighterLevels = AddFighterLevels;

        var spawnType = typeof(Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnMonster);
        const System.Reflection.BindingFlags fieldFlags =
          System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
          System.Reflection.BindingFlags.Instance;
        var summonAction =
          ElementTool.Create<Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSpawnMonster>();
        // m_Blueprint is private in current Wrath builds (DarkCodex's helper sets it
        // against a publicized assembly; we don't publicize, hence reflection).
        var blueprintField = spawnType.GetField("m_Blueprint", fieldFlags);
        blueprintField?.SetValue(summonAction, spawnUnit.ToReference<BlueprintUnitReference>());
        // 0.4.11 lesson: the engine dereferences CountValue unconditionally
        // (Unity's serializer normally auto-creates these fields on deserialized
        // blueprints, but a code-created action leaves them null) - leaving it unset
        // NRE'd inside ContextActionSpawnMonster.RunAction and no construct ever
        // spawned. Exactly one construct per cast: zero dice + bonus 1.
        summonAction.CountValue = new ContextDiceValue
        {
          DiceType = DiceType.Zero,
          DiceCountValue = ContextValues.Constant(0),
          BonusValue = ContextValues.Constant(1),
        };
        summonAction.DurationValue = ContextDuration.Fixed(100000);
        summonAction.DoNotLinkToCaster = false;
        summonAction.IsDirectlyControllable = false;
        // Summon-pool registration (DarkCodex's default pool) and the level value:
        // same reflection treatment as m_Blueprint.
        spawnType.GetField("m_SummonPool", fieldFlags)?.SetValue(
          summonAction, BlueprintTool.GetRef<BlueprintSummonPoolReference>(SummonPoolGuid));
        spawnType.GetField("LevelValue", fieldFlags)?.SetValue(
          summonAction, ContextValues.Constant(0));
        summonAction.AfterSpawn = ActionsBuilder.New()
          .ApplyBuff(
            BlueprintTool.Get<BlueprintBuff>(StockSummonBuffGuid),
            ContextDuration.Fixed(100000))
          .Add(finisher)
          .Build();
        // Prove the wiring before running: if this logs NULL the reflection set
        // silently failed and the action would crash again.
        var wiredBlueprint = blueprintField?.GetValue(summonAction) as BlueprintUnitReference;
        MissionFeats.Logger.Info(
          $"[deploy] spawn action: unit={spawnUnit.name}, " +
          $"wired={(wiredBlueprint != null && wiredBlueprint.Get() != null ? wiredBlueprint.Get().name : "NULL")}, count=1.");
        summonAction.RunAction();
        MissionFeats.Logger.Info(
          $"[deploy] spawn action run for {spawnUnit.name} (base {BaseKind}).");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ConstructCrafter: deploy failed.", e);
      }
    }
  }
  /// <summary>
  /// Runs inside ContextActionSpawnMonster's AfterSpawn list, where the action
  /// context's current target is the freshly summoned construct. Applies the base
  /// marker (replacement tracking), program and core packages, fighter levels for
  /// the humanoid base, and the deploy-time brain.
  /// </summary>
  [TypeId(Guids.DeployFinishAction)]
  internal class ContextActionDeployFinish : ContextAction
  {
    /// <summary>Which base is deploying: 0 sentry, 1 humanoid, 2 golem.</summary>
    public int BaseKind;

    /// <summary>Apply the scaling DR buff (iron sentry).</summary>
    public bool ApplyPlating;

    /// <summary>Give the construct fighter levels equal to (alchemist level - 2).</summary>
    public bool AddFighterLevels;

    public override string GetCaption() => "Configure deployed construct";

    public override void RunAction()
    {
      try
      {
        var construct = Target.Unit;
        var caster = Context.MaybeCaster;
        if (construct is null || caster is null)
        {
          MissionFeats.Logger.Error(
            "ConstructCrafter: deploy finisher could not resolve construct or caster.");
          return;
        }

        var program = ConstructCrafterPrograms.GetActiveProgram(caster);
        var core = ConstructCrafterCores.GetActiveCore(caster);
        var alchemistLevel = caster.Descriptor.Progression
          .GetClassLevel(CharacterClassRefs.AlchemistClass.Reference.Get());
        var baseMarker = BaseKind == 0 ? ConstructCrafterAbilities.SentryBaseMarker
          : BaseKind == 1 ? ConstructCrafterAbilities.ManBaseMarker
          : ConstructCrafterAbilities.GolemBaseMarker;

        // Humanoid base: a fighter with (alchemist level - 2) levels.
        if (AddFighterLevels)
        {
          var fighter = CharacterClassRefs.FighterClass.Reference.Get();
          construct.Descriptor.Progression.AddFakeClassLevels(fighter, Math.Max(1, alchemistLevel - 2));
        }

        // Base identity marker (replacement tracking across variants).
        construct.AddBuff(baseMarker, Context);

        // Program application: stat package + markers.
        if (program?.ConstructBuff != null)
        {
          construct.AddBuff(program.ConstructBuff, Context);
        }
        if (program?.IsChaos == true)
        {
          construct.AddBuff(ConstructCrafterPrograms.ChaosMarker, Context);
        }
        ContextActionDeployConstruct.ApplySneakAttackRanks(
          construct, alchemistLevel, program?.IsFlank == true ? 2 : 0);

        // Core application: per-base stat package + role abilities.
        var isSentry = BaseKind == 0;
        var isHumanoid = BaseKind == 1;
        var grantedAbilities =
          ContextActionDeployConstruct.ResolveCoreAbilities(core, BaseKind).ToList();
        foreach (var ability in grantedAbilities)
        {
          construct.AddFact(ability);
        }
        var coreBuff = core is null
          ? null
          : isSentry ? core.SentryBuff : isHumanoid ? core.HumanoidBuff : core.GolemBuff;
        if (coreBuff != null)
        {
          construct.AddBuff(coreBuff, Context);
        }

        // Core sneak attack: one die per N alchemist levels (N from the core def).
        var saDivisor = core is null
          ? 0
          : isSentry ? core.SaSentry : isHumanoid ? core.SaHumanoid : core.SaGolem;
        ContextActionDeployConstruct.ApplySneakAttackRanks(construct, alchemistLevel, saDivisor);

        // Flaming golem: no attacks of opportunity - unless the Guard program runs
        // (Guard trades the aura's ferocity for a disciplined watch).
        if (core?.GolemNoAoO == true && !isSentry && !isHumanoid
          && program?.IsGuard != true)
        {
          construct.AddBuff(ConstructCrafterCores.NoAoOBuff, Context);
        }

        // Iron sentry: scaling damage reduction (half alchemist level).
        if (ApplyPlating && ConstructCrafter.ClockworkPlatingBuff is not null)
        {
          construct.AddBuff(ConstructCrafter.ClockworkPlatingBuff, Context);
        }

        // Deploy-time brain: program behaviors take priority over the caster role.
        // With neither, the construct KEEPS its stock summon brain (cloned from the
        // game's own summon-variant units): the engine's SummonedUnitsController
        // moves any summon-linked unit to its summoner out of combat, and the stock
        // brain fights - the exact behavior of an ordinary summon, which needs no
        // help from us. (The old code force-swapped in our custom DefaultBrain here;
        // that brain family is still unproven in play, so it is no longer load-
        // bearing for the default case.)
        BlueprintBrain chosenBrain = null;
        if (program?.IsPassive == true)
        {
          chosenBrain = ConstructCrafterAbilities.PassiveBrain;
        }
        else if (program?.IsGuard == true)
        {
          chosenBrain = ConstructCrafterAbilities.GuardBrain;
        }
        else if (program?.IsDistance == true)
        {
          chosenBrain = ConstructCrafterAbilities.DistanceBrain;
        }
        else if (grantedAbilities.Count > 0)
        {
          chosenBrain = ConstructCrafterAbilities.CasterBrain;
        }
        if (chosenBrain != null)
        {
          if (construct.Brain != null)
          {
            construct.Brain.SetBrain(chosenBrain);
            construct.Brain.RestoreAvailableActions();
            MissionFeats.Logger.Info(
              $"[deploy] {construct.Blueprint.name}: brain set to {chosenBrain.name}.");
          }
          else
          {
            MissionFeats.Logger.Warn(
              $"[deploy] {construct.Blueprint.name}: brain instance missing, cannot set {chosenBrain.name}.");
          }
        }
        else
        {
          MissionFeats.Logger.Info(
            $"[deploy] {construct.Blueprint.name}: no program/role brain - " +
            $"keeping stock summon brain (engine handles follow + combat).");
        }

        // Mark the crafter so follow actions can home in on them even when no
        // program toggle is active (see CrafterMarkerBuff).
        if (ConstructCrafter.CrafterMarkerBuff != null &&
          caster.Buffs.GetBuff(ConstructCrafter.CrafterMarkerBuff) is null)
        {
          caster.AddBuff(ConstructCrafter.CrafterMarkerBuff, Context);
        }

        MissionFeats.Logger.Info(
          $"[deploy] done: {construct.Blueprint.name} uid={construct.UniqueId} " +
          $"brain={(construct.Brain != null ? "present" : "NULL")}, " +
          $"master={(construct.Master != null ? "set" : "none")}, " +
          $"hp={construct.HPLeft}/{construct.MaxHP}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ConstructCrafter: deploy finish failed.", e);
      }
    }
  }
}

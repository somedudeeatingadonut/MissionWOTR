using BlueprintCore.Actions.Builder;
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
  ///   L1  Deploy Clockwork Hound (dog base), extra combat feat, Basic Core selection,
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

    internal const string HoundUnitName = "ConstructCrafterClockworkHound";
    internal const string HumanoidUnitName = "ConstructCrafterHumanoidConstruct";
    internal const string GolemUnitName = "ConstructCrafterClayGolem";

    internal const string DeployHoundFeatureName = "ConstructCrafterDeployHound";
    internal const string DeployHumanoidFeatureName = "ConstructCrafterDeployHumanoid";
    internal const string DeployGolemFeatureName = "ConstructCrafterDeployGolem";
    internal const string DeployHoundAbilityName = "ConstructCrafterDeployHoundAbility";
    internal const string DeployHumanoidAbilityName = "ConstructCrafterDeployHumanoidAbility";
    internal const string DeployGolemAbilityName = "ConstructCrafterDeployGolemAbility";

    internal const string CoreSelectionName = "ConstructCrafterCoreSelection";
    internal const string ProgramSelectionName = "ConstructCrafterProgramSelection";
    internal const string BasicCoreName = "ConstructCrafterBasicCore";
    internal const string BasicProgramName = "ConstructCrafterBasicProgram";

    internal const string PlatingBuffName = "ConstructCrafterClockworkPlating";
    internal const string ProficienciesName = "ConstructCrafterProficiencies";

    // Runtime handles for the deploy action.
    internal static BlueprintUnit HoundUnit;
    internal static BlueprintUnit HumanoidUnit;
    internal static BlueprintUnit GolemUnit;
    internal static BlueprintBuff ClockworkPlatingBuff;

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
        DeployHoundFeatureName, Guids.ConstructCrafterDeployHoundFeature,
        DeployHoundAbilityName, Guids.ConstructCrafterDeployHoundAbility,
        "DeployHound.Name", "DeployHound.Description", HoundUnit,
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
          .SetLocalizedDescription(Description)
          // Chassis removals (best-effort levels; see docs for the poison-feature notes).
          .AddToRemoveFeatures(1, FeatureRefs.AlchemistMutagen.ToString())
          .AddToRemoveFeatures(2, FeatureRefs.PoisonResistance.ToString())
          .AddToRemoveFeatures(5, FeatureRefs.PoisonResistance4Feature.ToString())
          .AddToRemoveFeatures(10, FeatureRefs.ImmunityToPoison.ToString())
          // Core kit.
          .AddToAddFeatures(LevelPlan.L(1), FeatureSelectionRefs.FighterFeatSelection.ToString())
          .AddToAddFeatures(LevelPlan.L(1), CoreSelectionName, ProgramSelectionName)
          // Bases.
          .AddToAddFeatures(LevelPlan.L(1), DeployHoundFeatureName)
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
      // Player-friendly faction taken from the game's own dog companion.
      var dogFaction = UnitRefs.AnimalCompanionUnitDog.Reference.Get().Faction;
      var dog = UnitRefs.AnimalCompanionUnitDog.Reference.Get();

      // --- Clockwork Hound: slightly worse than a normal dog ---
      HoundUnit = UnitConfigurator.New(HoundUnitName, Guids.ConstructCrafterHoundUnit)
        .CopyFrom(UnitRefs.AnimalCompanionUnitDog)
        .SetStrength(dog.Strength - 2)
        .SetDexterity(dog.Dexterity - 2)
        .SetMaxHP(Math.Max(4, dog.MaxHP - 4))
        .SetFaction(dogFaction)
        .Configure();

      // --- Humanoid Construct: fighter with (AL-2) levels, applied at deploy time ---
      HumanoidUnit = UnitConfigurator.New(HumanoidUnitName, Guids.ConstructCrafterHumanoidUnit)
        .CopyFrom(UnitRefs.CR0_5_Bandit_Human_FighterMelee_Male)
        .SetFaction(dogFaction)
        .Configure();

      // --- Clay Golem: tabletop chassis (no berserk, -20 HP, -2 Str) ---
      // Built on the stone golem body; the slow breath component is stripped where
      // possible (see adaptation notes in docs/ARCHETYPES.md).
      var stoneGolem = UnitRefs.CR11_GolemStone.Reference.Get();
      GolemUnit = UnitConfigurator.New(GolemUnitName, Guids.ConstructCrafterGolemUnit)
        .CopyFrom(
          UnitRefs.CR11_GolemStone,
          c => !c.name.Contains("Slow")
            && c is not Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical)
        .SetStrength(32 - 2)
        .SetMaxHP(107 - 20)
        .SetFaction(dogFaction)
        .AddDamageResistancePhysical(
          value: 5, bypassedByMaterial: true, material: PhysicalDamageMaterial.Adamantite)
        .Configure();

      // --- Clockwork Plating: the hound's scaling DR (half alchemist level). ---
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

    /// <summary>Apply the scaling DR buff (clockwork hound).</summary>
    public bool ApplyPlating;

    /// <summary>Give the construct fighter levels equal to (alchemist level - 2).</summary>
    public bool AddFighterLevels;

    /// <summary>Which base is deploying: 0 hound, 1 humanoid, 2 golem.</summary>
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
        return arbalest || soft ? ConstructCrafterAbilities.HoundRangedUnit : fallback;
      }
      if (baseKind == 1)
      {
        return arbalest ? ConstructCrafterAbilities.HumanoidArcherUnit
          : flaming || soft ? ConstructCrafterAbilities.HumanoidCasterUnit
          : fallback;
      }
      return arbalest || cold || soft ? ConstructCrafterAbilities.GolemCasterUnit : fallback;
    }

    private static System.Collections.Generic.IEnumerable<BlueprintAbility> ResolveCoreAbilities(
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

    private static void ApplySneakAttackRanks(
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

        var baseMarker = BaseKind == 0 ? ConstructCrafterAbilities.HoundBaseMarker
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

        // Spawn near the caster (small random offset, ToyBox-style).
        var offset = 5f * UnityEngine.Random.insideUnitSphere;
        var spawnPosition = new Vector3(
          caster.Position.x + offset.x, caster.Position.y, caster.Position.z + offset.z);

        // Role variant: the active core may swap in a specialized chassis (archer,
        // caster, ranged) instead of the default base unit.
        var spawnUnit = ResolveUnit(core, BaseKind, Unit);
        var knownIds = existing.Select(u => u.UniqueId).ToHashSet();
        Game.Instance.EntityCreator.SpawnUnit(spawnUnit, spawnPosition, Quaternion.identity, state);

        // Find the freshly spawned unit (works regardless of SpawnUnit's return type).
        var construct = state.AllEntityData.OfType<UnitEntityData>()
          .FirstOrDefault(u => !knownIds.Contains(u.UniqueId)
            && u.Buffs.GetBuff(baseMarker) is null);
        if (construct is null)
        {
          MissionFeats.Logger.Error("ConstructCrafter: spawned unit could not be found.");
          return;
        }

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
        ApplySneakAttackRanks(
          construct, alchemistLevel, program?.IsFlank == true ? 2 : 0);

        // Core application: per-base stat package + role abilities.
        var isHound = BaseKind == 0;
        var isHumanoid = BaseKind == 1;
        var grantedAbilities = ResolveCoreAbilities(core, BaseKind).ToList();
        foreach (var ability in grantedAbilities)
        {
          construct.AddFact(ability);
        }
        var coreBuff = core is null
          ? null
          : isHound ? core.HoundBuff : isHumanoid ? core.HumanoidBuff : core.GolemBuff;
        if (coreBuff != null)
        {
          construct.AddBuff(coreBuff, Context);
        }

        // Core sneak attack: one die per N alchemist levels (N from the core def).
        var saDivisor = core is null
          ? 0
          : isHound ? core.SaHound : isHumanoid ? core.SaHumanoid : core.SaGolem;
        ApplySneakAttackRanks(construct, alchemistLevel, saDivisor);

        // Flaming golem: no attacks of opportunity - unless the Guard program runs
        // (Guard trades the aura's ferocity for a disciplined watch).
        if (core?.GolemNoAoO == true && !isHound && !isHumanoid
          && program?.IsGuard != true)
        {
          construct.AddBuff(ConstructCrafterCores.NoAoOBuff, Context);
        }

        // Clockwork hound: scaling damage reduction (half alchemist level).
        if (ApplyPlating && ConstructCrafter.ClockworkPlatingBuff is not null)
        {
          construct.AddBuff(ConstructCrafter.ClockworkPlatingBuff, Context);
        }

        // Brains v2: assign the brain at deploy time. Program behaviors take priority
        // over the caster role; without either, the variant's baked brain stays.
        // UnitBrain.SetBrain rebuilds the action list at runtime, and
        // RestoreAvailableActions filters cast actions down to owned abilities
        // (abilities were granted above, before this call).
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
        if (chosenBrain != null && construct.Brain != null)
        {
          construct.Brain.SetBrain(chosenBrain);
          construct.Brain.RestoreAvailableActions();
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ConstructCrafter: deploy failed.", e);
      }
    }
  }
}

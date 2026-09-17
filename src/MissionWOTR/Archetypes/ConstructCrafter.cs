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
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
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

    // Runtime handles for the deploy action.
    internal static BlueprintUnit HoundUnit;
    internal static BlueprintUnit HumanoidUnit;
    internal static BlueprintUnit GolemUnit;
    internal static BlueprintBuff ClockworkPlatingBuff;

    public static void Configure()
    {
      ConfigureUnits();
      ConfigureCoresAndPrograms();

      DeployBase(
        DeployHoundFeatureName, Guids.ConstructCrafterDeployHoundFeature,
        DeployHoundAbilityName, Guids.ConstructCrafterDeployHoundAbility,
        "DeployHound.Name", "DeployHound.Description", HoundUnit,
        applyPlating: true, addFighterLevels: false, icon: FeatureRefs.RideAnimalCompanionFeature);

      DeployBase(
        DeployHumanoidFeatureName, Guids.ConstructCrafterDeployHumanoidFeature,
        DeployHumanoidAbilityName, Guids.ConstructCrafterDeployHumanoidAbility,
        "DeployHumanoid.Name", "DeployHumanoid.Description", HumanoidUnit,
        applyPlating: false, addFighterLevels: true,
        icon: FeatureRefs.MartialWeaponProficiency);

      DeployBase(
        DeployGolemFeatureName, Guids.ConstructCrafterDeployGolemFeature,
        DeployGolemAbilityName, Guids.ConstructCrafterDeployGolemAbility,
        "DeployGolem.Name", "DeployGolem.Description", GolemUnit,
        applyPlating: false, addFighterLevels: false, icon: FeatureRefs.HeavyArmorProficiency);

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
          .AddToAddFeatures(LevelPlan.L(16), DeployGolemFeatureName);
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

      // Core & Program selections: chosen before deployment; v1 has the Basic options,
      // future cores/programs slot into the same selections.
      FeatureSelectionConfigurator.New(CoreSelectionName, Guids.ConstructCrafterCoreSelection)
        .SetDisplayName("CoreSelection.Name")
        .SetDescription("CoreSelection.Description")
        .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
        .SetAllFeatures(basicCore)
        .Configure();

      FeatureSelectionConfigurator.New(ProgramSelectionName, Guids.ConstructCrafterProgramSelection)
        .SetDisplayName("ProgramSelection.Name")
        .SetDescription("ProgramSelection.Description")
        .SetIcon(FeatureRefs.CombatReflexes.Reference.Get().Icon)
        .SetAllFeatures(basicProgram)
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
      Blueprint<BlueprintReference<BlueprintFeature>> icon)
    {
      var ability = AbilityConfigurator.New(abilityName, abilityGuid)
        .SetDisplayName(displayKey)
        .SetDescription(descriptionKey)
        .SetIcon(icon.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetSelf()
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().Add(new ContextActionDeployConstruct
          {
            Unit = unit,
            ApplyPlating = applyPlating,
            AddFighterLevels = addFighterLevels,
          }))
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

        var state = Game.Instance.State.LoadedAreaState.MainState;
        var existing = state.AllEntityData.OfType<UnitEntityData>().ToList();

        // Replace the previous construct of this base (same blueprint = same base).
        foreach (var old in existing.Where(u => u.Blueprint == Unit && u.HPLeft > 0))
        {
          old.IsInGame = false;
        }

        // Spawn near the caster (small random offset, ToyBox-style).
        var offset = 5f * UnityEngine.Random.insideUnitSphere;
        var spawnPosition = new Vector3(
          caster.Position.x + offset.x, caster.Position.y, caster.Position.z + offset.z);

        var knownIds = existing.Select(u => u.UniqueId).ToHashSet();
        Game.Instance.EntityCreator.SpawnUnit(Unit, spawnPosition, Quaternion.identity, state);

        // Find the freshly spawned unit (works regardless of SpawnUnit's return type).
        var construct = state.AllEntityData.OfType<UnitEntityData>()
          .FirstOrDefault(u => u.Blueprint == Unit && !knownIds.Contains(u.UniqueId));
        if (construct is null)
        {
          MissionFeats.Logger.Error("ConstructCrafter: spawned unit could not be found.");
          return;
        }

        // Humanoid base: a fighter with (alchemist level - 2) levels.
        if (AddFighterLevels)
        {
          var alchemistLevel = caster.Descriptor.Progression
            .GetClassLevel(CharacterClassRefs.AlchemistClass.Reference.Get());
          var fighter = CharacterClassRefs.FighterClass.Reference.Get();
          construct.Descriptor.Progression.AddFakeClassLevels(fighter, Math.Max(1, alchemistLevel - 2));
        }

        // Clockwork hound: scaling damage reduction (half alchemist level).
        if (ApplyPlating && ConstructCrafter.ClockworkPlatingBuff is not null)
        {
          construct.AddBuff(ConstructCrafter.ClockworkPlatingBuff, Context);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ConstructCrafter: deploy failed.", e);
      }
    }
  }
}

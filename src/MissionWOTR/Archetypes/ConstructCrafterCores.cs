using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Utils;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Construct Crafter cores (user design + two originals).
  ///
  /// A core is acquired at alchemist levels 3, 8, 13 and 19 (one pick each - purposefully
  /// limited) through the Core selection. The active core is a crafter-side toggle; the
  /// deploy action applies the core's construct package. Stat bonuses scale +1 per 2 AL
  /// (min 1); on-hit riders and damage-over-time effects use fixed values (see
  /// docs/ARCHETYPES.md). Cores marked per-base behave differently on hound/humanoid/golem.
  ///
  /// Cores: Overdrive, Hardened, Flaming (per-base), Cold (per-base), Bloody, Soft,
  /// Infernal, Lightless (per-base), Booming, Quick, Galvanized (original), Magnetized
  /// (original).
  /// </summary>
  internal static class ConstructCrafterCores
  {
    internal class CoreDef
    {
      public string Name;
      public string DisplayKey;
      public BlueprintFeature Feature;
      public BlueprintBuff Marker;
      public BlueprintBuff HoundBuff;
      public BlueprintBuff HumanoidBuff;
      public BlueprintBuff GolemBuff;
      public bool IsOverdrive;
      public int SaHound;
      public int SaHumanoid;
      public int SaGolem;
      public bool GolemNoAoO;
    }

    internal static readonly List<CoreDef> Cores = new();
    internal static BlueprintBuff NoAoOBuff;

    internal static string[] AllFeatureNames => Cores.Select(c => c.Name).ToArray();

    internal static void Configure()
    {
      NoAoOBuff = BuffConfigurator.New("ConstructCrafterNoAoO", Guids.CoreNoAoOBuff)
        .SetDisplayName("CoreNoAoO.Name")
        .SetDescription("CoreNoAoO.Description")
        .SetIcon(FeatureRefs.CombatReflexes.Reference.Get().Icon)
        .AddContextStatBonus(
          Kingmaker.EntitySystem.Stats.StatType.AttackOfOpportunityCount,
          ContextValues.Constant(-50), ModifierDescriptor.Penalty)
        .Configure();

      // ----- Overdrive: high bonuses, burns itself, cannot be healed, no Chaos -----
      Cores.Add(CreateCore(
        "ConstructCrafterOverdrive", Guids.OverdriveCoreFeat, Guids.OverdriveCoreToggle,
        Guids.OverdriveCoreMarker, "OverdriveCore.Name", "OverdriveCore.Description",
        buff => buff
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalAttackBonus,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalDamage,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.Speed,
            ContextValues.Constant(10), ModifierDescriptor.UntypedStackable)
          .AddComponent(new PeriodicSelfDamage { DamagePerRound = 2 })
          .AddPreventHealing(),
        isOverdrive: true));

      // ----- Hardened: the tank core -----
      Cores.Add(CreateCore(
        "ConstructCrafterHardened", Guids.HardenedCoreFeat, Guids.HardenedCoreToggle,
        Guids.HardenedCoreMarker, "HardenedCore.Name", "HardenedCore.Description",
        buff => buff
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AC,
            ContextValues.Rank(), ModifierDescriptor.Dodge)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.SaveWill,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.SaveReflex,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.SaveFortitude,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalDamage,
            ContextValues.Rank(), ModifierDescriptor.Penalty)
          .AddDamageResistancePhysical(value: 2)));

      // ----- Flaming: per-base -----
      Cores.Add(CreateCore(
        "ConstructCrafterFlaming", Guids.FlamingCoreFeat, Guids.FlamingCoreToggle,
        Guids.FlamingCoreMarker, "FlamingCore.Name", "FlamingCore.Description",
        houndBuff: buff => buff
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.Speed,
            ContextValues.Constant(10), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalDamage,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AC,
            ContextValues.Rank(), ModifierDescriptor.Penalty)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.SaveWill,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.SaveReflex,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.SaveFortitude,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddComponent(new PeriodicSelfDamage { DamagePerRound = 1 }),
        humanoidBuff: buff => buff
          .AdditionalDamageOnHit(
            element: DamageEnergyType.Fire, energyDamageDice: new DiceFormula(2, DiceType.D6))
          .AddComponent(new ConstructOnHitBuff { Buff = BuffRefs.Burning.Reference.Get(), Rounds = 3 }),
        golemBuff: buff => buff
          .AddAreaEffect(areaEffect: AbilityAreaEffectRefs.FireDamageAreaEffect.Cast<BlueprintAbilityAreaEffectReference>())
          .AddComponent(new PeriodicSelfDamage { DamagePerRound = 2 })
          .AddComponent(new ConstructOnHitBuff { Buff = BuffRefs.Burning.Reference.Get(), Rounds = 3 }),
        saHound: 3, golemNoAoO: true));

      // ----- Cold: per-base -----
      Cores.Add(CreateCore(
        "ConstructCrafterCold", Guids.ColdCoreFeat, Guids.ColdCoreToggle,
        Guids.ColdCoreMarker, "ColdCore.Name", "ColdCore.Description",
        houndBuff: buff => buff
          .AddComponent(new ConstructOnHitBuff { Buff = BuffRefs.Slowed.Reference.Get(), Rounds = 6 })
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.SaveWill,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.SaveFortitude,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.Speed,
            ContextValues.Constant(-10), ModifierDescriptor.Penalty),
        humanoidBuff: buff => buff
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.Speed,
            ContextValues.Constant(20), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalDamage,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable),
        golemBuff: buff => buff
          .AdditionalDamageOnHit(
            element: DamageEnergyType.Cold, energyDamageDice: new DiceFormula(1, DiceType.D6))
          .AddComponent(new ConstructOnHitBuff { Buff = BuffRefs.Slowed.Reference.Get(), Rounds = 6 })));

      // ----- Bloody: bleed, debuffs, a little healing -----
      Cores.Add(CreateCore(
        "ConstructCrafterBloody", Guids.BloodyCoreFeat, Guids.BloodyCoreToggle,
        Guids.BloodyCoreMarker, "BloodyCore.Name", "BloodyCore.Description",
        buff => buff
          .AddComponent(new ConstructOnHitBuff { Buff = BuffRefs.Bleed1d4Buff.Reference.Get(), Rounds = 6 })
          .AddEffectFastHealing(heal: 2)));

      // ----- Soft: the support/sponge core -----
      Cores.Add(CreateCore(
        "ConstructCrafterSoft", Guids.SoftCoreFeat, Guids.SoftCoreToggle,
        Guids.SoftCoreMarker, "SoftCore.Name", "SoftCore.Description",
        buff => buff
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AC,
            ContextValues.Rank(), ModifierDescriptor.Dodge)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.SaveWill,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.SaveReflex,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.SaveFortitude,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalDamage,
            ContextValues.Rank(), ModifierDescriptor.Penalty)
          .AddEffectFastHealing(heal: 1)));

      // ----- Infernal: hellfire and fear -----
      Cores.Add(CreateCore(
        "ConstructCrafterInfernal", Guids.InfernalCoreFeat, Guids.InfernalCoreToggle,
        Guids.InfernalCoreMarker, "InfernalCore.Name", "InfernalCore.Description",
        buff => buff
          .AdditionalDamageOnHit(
            element: DamageEnergyType.Fire, energyDamageDice: new DiceFormula(2, DiceType.D6))
          .AddComponent(new ConstructOnHitBuff { Buff = BuffRefs.Shaken.Reference.Get(), Rounds = 6 })
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.Initiative,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.Speed,
            ContextValues.Constant(10), ModifierDescriptor.UntypedStackable)));

      // ----- Lightless: per-base -----
      Cores.Add(CreateCore(
        "ConstructCrafterLightless", Guids.LightlessCoreFeat, Guids.LightlessCoreToggle,
        Guids.LightlessCoreMarker, "LightlessCore.Name", "LightlessCore.Description",
        houndBuff: buff => buff
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalAttackBonus,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AC,
            ContextValues.Rank(), ModifierDescriptor.Dodge),
        humanoidBuff: buff => buff
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalAttackBonus,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AC,
            ContextValues.Rank(), ModifierDescriptor.Dodge),
        golemBuff: buff => buff
          .AddComponent(new ConstructOnHitBuff { Buff = BuffRefs.Frightened.Reference.Get(), Rounds = 6 })
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.SaveWill,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable),
        saHound: 3, saHumanoid: 3));

      // ----- Booming: walking catastrophe, glass chassis -----
      Cores.Add(CreateCore(
        "ConstructCrafterBooming", Guids.BoomingCoreFeat, Guids.BoomingCoreToggle,
        Guids.BoomingCoreMarker, "BoomingCore.Name", "BoomingCore.Description",
        buff => buff
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalDamage,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalDamage,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddComponent(new ConstructSonicBoom())
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AC,
            ContextValues.Rank(), ModifierDescriptor.Penalty)
          .AddStatBonus(stat: Kingmaker.EntitySystem.Stats.StatType.SaveWill, value: -2,
            descriptor: ModifierDescriptor.Penalty)
          .AddStatBonus(stat: Kingmaker.EntitySystem.Stats.StatType.SaveReflex, value: -2,
            descriptor: ModifierDescriptor.Penalty)
          .AddStatBonus(stat: Kingmaker.EntitySystem.Stats.StatType.SaveFortitude, value: -2,
            descriptor: ModifierDescriptor.Penalty)));

      // ----- Quick: extra attack, speed, initiative -----
      Cores.Add(CreateCore(
        "ConstructCrafterQuick", Guids.QuickCoreFeat, Guids.QuickCoreToggle,
        Guids.QuickCoreMarker, "QuickCore.Name", "QuickCore.Description",
        buff => buff
          .AddBuffExtraAttack(number: 1, haste: true)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.Speed,
            ContextValues.Constant(10), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.Initiative,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalDamage,
            ContextValues.Rank(), ModifierDescriptor.Penalty)));

      // ----- Galvanized (original): storm in a bottle -----
      Cores.Add(CreateCore(
        "ConstructCrafterGalvanized", Guids.GalvanizedCoreFeat, Guids.GalvanizedCoreToggle,
        Guids.GalvanizedCoreMarker, "GalvanizedCore.Name", "GalvanizedCore.Description",
        buff => buff
          .AdditionalDamageOnHit(
            element: DamageEnergyType.Electricity, energyDamageDice: new DiceFormula(1, DiceType.D6))
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.Initiative,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.SaveReflex,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.Speed,
            ContextValues.Constant(10), ModifierDescriptor.UntypedStackable)));

      // ----- Arbalest (original): the archery playstyle -----
      Cores.Add(CreateCore(
        "ConstructCrafterArbalest", Guids.ArbalestCoreFeat, Guids.ArbalestCoreToggle,
        Guids.ArbalestCoreMarker, "ArbalestCore.Name", "ArbalestCore.Description",
        buff => buff
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalAttackBonus,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalDamage,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.Speed,
            ContextValues.Constant(10), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AC,
            ContextValues.Rank(), ModifierDescriptor.Penalty)));

      // ----- Magnetized (original): living lodestone -----
      Cores.Add(CreateCore(
        "ConstructCrafterMagnetized", Guids.MagnetizedCoreFeat, Guids.MagnetizedCoreToggle,
        Guids.MagnetizedCoreMarker, "MagnetizedCore.Name", "MagnetizedCore.Description",
        buff => buff
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalCMB,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(Kingmaker.EntitySystem.Stats.StatType.AdditionalCMD,
            ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddComponent(new ConstructOnHitBuff { Buff = BuffRefs.Slowed.Reference.Get(), Rounds = 4 })));
      ConfigureCoreCommand();
    }

    /// <summary>Hub ability name for the archetype grant.</summary>
    internal const string CoreCommandName = "ConstructCrafterCoreCommand";
    internal const string CoreCommandFeatureName = "ConstructCrafterCoreCommandFeature";

    private static readonly Dictionary<string, string> CoreCommandGuidMap = new()
    {
      { "ConstructCrafterOverdrive", "141C4B1D-E717-4DAB-A392-776C280D681A" },
      { "ConstructCrafterHardened", "400374AB-0184-472A-AA55-C61F1DD58847" },
      { "ConstructCrafterFlaming", "CAB61670-4B01-459A-894A-9835A2818973" },
      { "ConstructCrafterCold", "D78FB201-6650-4A87-A610-97185E8C2DFC" },
      { "ConstructCrafterBloody", "1EB173B5-6587-42FE-8721-D2704752276E" },
      { "ConstructCrafterSoft", "0D64860A-3DA8-44C0-B66C-4D779B823A4A" },
      { "ConstructCrafterInfernal", "DA81C8B9-1201-4281-ACE2-CF4B0D1005D7" },
      { "ConstructCrafterLightless", "1BCE5310-8C45-4B6E-A4CD-7273626CA553" },
      { "ConstructCrafterBooming", "0DCC6C77-7F73-4146-B687-B82E628DFA32" },
      { "ConstructCrafterQuick", "892EEA41-3CE1-4466-90D2-B3C4306C2837" },
      { "ConstructCrafterGalvanized", "C016286B-43CD-48C4-B3DE-2022FC809DE1" },
      { "ConstructCrafterMagnetized", "F0751F53-D51C-408B-A485-DF4961B7A8EF" },
      { "ConstructCrafterArbalest", "48625D62-024E-4E54-842F-CC5BB37746BD" },
    };

    /// <summary>
    /// Builds the Core Command hub (0.16.0): ONE bar icon; clicking it opens
    /// the game's variant submenu listing every core, plus the base entry
    /// (casting the hub directly returns to the Basic core). The menu lists
    /// all cores - the same blueprint-static variant-list limitation the
    /// Riftstalker menu documents - so the gating lives in the action: a
    /// core only arms if the crafter owns its feature. Arming one core
    /// clears the others (the old toggles allowed junk states; the menu is
    /// exclusive by construction).
    /// </summary>
    private static void ConfigureCoreCommand()
    {
      var icon = FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon;
      var variants = new List<Blueprint<BlueprintAbilityReference>>();
      foreach (var core in Cores)
      {
        var set = ElementTool.Create<ContextActionSetConstructDirective>();
        set.DirectiveName = core.Name;
        set.IsProgram = false;
        variants.Add(AbilityConfigurator.New(core.Name + "Command", CoreCommandGuidMap[core.Name])
          .SetDisplayName(core.DisplayKey)
          .SetDescription(core.DisplayKey)
          .SetIcon(icon)
          .SetType(AbilityType.Special)
          .SetRange(AbilityRange.Personal)
          .SetActionType(UnitCommand.CommandType.Free)
          .SetCanTargetSelf()
          .AddAbilityEffectRunAction(ActionsBuilder.New().Add(set).Build())
          .Configure());
      }

      var basic = ElementTool.Create<ContextActionSetConstructDirective>();
      basic.DirectiveName = null; // null = Basic core: markers cleared
      basic.IsProgram = false;
      AbilityConfigurator.New(CoreCommandName, Guids.CrafterCoreCommand)
        .SetDisplayName("CoreCommand.Name")
        .SetDescription("CoreCommand.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Free)
        .SetCanTargetSelf()
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(basic).Build())
        .AddAbilityVariants(variants)
        .Configure();

      // 0.60.0: the archetype grants features, not abilities. Handing an ability blueprint to
      // AddToAddFeatures resolves to NULL-REF and the crafter silently never gets the menu, so
      // the command lives inside a grantable class feature (the Riftstalker Guided Command
      // pattern).
      FeatureConfigurator.New(CoreCommandFeatureName, Guids.CrafterCoreCommandFeature)
        .SetDisplayName("CoreCommand.Name")
        .SetDescription("CoreCommand.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .SetHideInUI(true)
        .SetHideInCharacterSheetAndLevelUp(true)
        .AddFacts(new() { CoreCommandName })
        .Configure();
    }

    private static CoreDef CreateCore(
      string featName,
      string featGuid,
      string toggleGuid,
      string markerGuid,
      string displayKey,
      string descriptionKey,
      Action<BuffConfigurator> buff = null,
      Action<BuffConfigurator> houndBuff = null,
      Action<BuffConfigurator> humanoidBuff = null,
      Action<BuffConfigurator> golemBuff = null,
      bool isOverdrive = false,
      int saHound = 0,
      int saHumanoid = 0,
      int saGolem = 0,
      bool golemNoAoO = false)
    {
      var alchemist = CharacterClassRefs.AlchemistClass;
      // Cores that share one package across bases map every suffix to the SAME guid;
      // the shared package must be a single blueprint, so creations are deduped by guid
      // (the second suffix reuses the first blueprint instead of colliding on the guid).
      var buffsByGuid = new Dictionary<string, BlueprintBuff>();
      Func<Action<BuffConfigurator>, string, BlueprintBuff> makeBuff = (configure, suffix) =>
      {
        var buffGuid = BuffGuidFor(featName, suffix);
        if (buffsByGuid.TryGetValue(buffGuid, out var shared))
        {
          return shared;
        }
        var builder = BuffConfigurator.New(featName + suffix, buffGuid)
          .SetDisplayName(displayKey)
          .SetDescription(descriptionKey)
          .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
          .AddContextRankConfig(ContextRankConfigs.ClassLevel(
            new[] { alchemist.ToString() }, min: 1).WithDiv2Progression());
        configure?.Invoke(builder);
        var made = builder.Configure();
        buffsByGuid[buffGuid] = made;
        return made;
      };

      var marker = BuffConfigurator.New(featName + "Marker", markerGuid)
        .SetDisplayName(displayKey)
        .SetDescription(descriptionKey)
        .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
        .Configure();

      var activatable = ActivatableAbilityConfigurator.New(featName + "Toggle", toggleGuid)
        .SetDisplayName(displayKey)
        .SetDescription(descriptionKey)
        .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
        .SetBuff(marker)
        .Configure();

      // 0.16.0: the toggle is no longer granted as a separate bar icon - the
      // core is armed through the Core Command menu (see Configure below).
      var feature = FeatureConfigurator.New(featName, featGuid)
        .SetDisplayName(displayKey)
        .SetDescription(descriptionKey)
        .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      return new CoreDef
      {
        Name = featName,
        DisplayKey = displayKey,
        Feature = feature,
        Marker = marker,
        HoundBuff = makeBuff(houndBuff ?? buff, "HoundBuff"),
        HumanoidBuff = makeBuff(humanoidBuff ?? buff, "HumanoidBuff"),
        GolemBuff = makeBuff(golemBuff ?? buff, "GolemBuff"),
        IsOverdrive = isOverdrive,
        SaHound = saHound,
        SaHumanoid = saHumanoid,
        SaGolem = saGolem,
        GolemNoAoO = golemNoAoO,
      };
    }

    private static readonly Dictionary<(string, string), string> BuffGuidMap = new()
    {
      { ("ConstructCrafterArbalest", "HoundBuff"), Guids.ArbalestCoreBuff },
      { ("ConstructCrafterArbalest", "HumanoidBuff"), Guids.ArbalestCoreBuff },
      { ("ConstructCrafterArbalest", "GolemBuff"), Guids.ArbalestCoreBuff },
      { ("ConstructCrafterOverdrive", "HoundBuff"), Guids.OverdriveCoreBuff },
      { ("ConstructCrafterOverdrive", "HumanoidBuff"), Guids.OverdriveCoreBuff },
      { ("ConstructCrafterOverdrive", "GolemBuff"), Guids.OverdriveCoreBuff },
      { ("ConstructCrafterHardened", "HoundBuff"), Guids.HardenedCoreBuff },
      { ("ConstructCrafterHardened", "HumanoidBuff"), Guids.HardenedCoreBuff },
      { ("ConstructCrafterHardened", "GolemBuff"), Guids.HardenedCoreBuff },
      { ("ConstructCrafterFlaming", "HoundBuff"), Guids.FlamingHoundBuff },
      { ("ConstructCrafterFlaming", "HumanoidBuff"), Guids.FlamingHumanoidBuff },
      { ("ConstructCrafterFlaming", "GolemBuff"), Guids.FlamingGolemBuff },
      { ("ConstructCrafterCold", "HoundBuff"), Guids.ColdHoundBuff },
      { ("ConstructCrafterCold", "HumanoidBuff"), Guids.ColdHumanoidBuff },
      { ("ConstructCrafterCold", "GolemBuff"), Guids.ColdGolemBuff },
      { ("ConstructCrafterBloody", "HoundBuff"), Guids.BloodyCoreBuff },
      { ("ConstructCrafterBloody", "HumanoidBuff"), Guids.BloodyCoreBuff },
      { ("ConstructCrafterBloody", "GolemBuff"), Guids.BloodyCoreBuff },
      { ("ConstructCrafterSoft", "HoundBuff"), Guids.SoftCoreBuff },
      { ("ConstructCrafterSoft", "HumanoidBuff"), Guids.SoftCoreBuff },
      { ("ConstructCrafterSoft", "GolemBuff"), Guids.SoftCoreBuff },
      { ("ConstructCrafterInfernal", "HoundBuff"), Guids.InfernalCoreBuff },
      { ("ConstructCrafterInfernal", "HumanoidBuff"), Guids.InfernalCoreBuff },
      { ("ConstructCrafterInfernal", "GolemBuff"), Guids.InfernalCoreBuff },
      { ("ConstructCrafterLightless", "HoundBuff"), Guids.LightlessHoundBuff },
      { ("ConstructCrafterLightless", "HumanoidBuff"), Guids.LightlessHoundBuff },
      { ("ConstructCrafterLightless", "GolemBuff"), Guids.LightlessGolemBuff },
      { ("ConstructCrafterBooming", "HoundBuff"), Guids.BoomingCoreBuff },
      { ("ConstructCrafterBooming", "HumanoidBuff"), Guids.BoomingCoreBuff },
      { ("ConstructCrafterBooming", "GolemBuff"), Guids.BoomingCoreBuff },
      { ("ConstructCrafterQuick", "HoundBuff"), Guids.QuickCoreBuff },
      { ("ConstructCrafterQuick", "HumanoidBuff"), Guids.QuickCoreBuff },
      { ("ConstructCrafterQuick", "GolemBuff"), Guids.QuickCoreBuff },
      { ("ConstructCrafterGalvanized", "HoundBuff"), Guids.GalvanizedCoreBuff },
      { ("ConstructCrafterGalvanized", "HumanoidBuff"), Guids.GalvanizedCoreBuff },
      { ("ConstructCrafterGalvanized", "GolemBuff"), Guids.GalvanizedCoreBuff },
      { ("ConstructCrafterMagnetized", "HoundBuff"), Guids.MagnetizedCoreBuff },
      { ("ConstructCrafterMagnetized", "HumanoidBuff"), Guids.MagnetizedCoreBuff },
      { ("ConstructCrafterMagnetized", "GolemBuff"), Guids.MagnetizedCoreBuff },
    };

    private static string BuffGuidFor(string featName, string suffix)
    {
      return BuffGuidMap[(featName, suffix)];
    }

    internal static CoreDef GetActiveCore(UnitEntityData crafter)
    {
      if (crafter is null)
      {
        return null;
      }
      foreach (var core in Cores)
      {
        if (crafter.Buffs.GetBuff(core.Marker) != null)
        {
          return core;
        }
      }
      return null;
    }
  }

  /// <summary>
  /// Applies a buff to enemies the construct hits with a weapon attack. Custom component
  /// (the game's own ApplyBuffOnHit changed shape between game versions).
  /// </summary>
  [TypeId(Guids.ConstructOnHitBuff)]
  internal class ConstructOnHitBuff : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>
  {
    public BlueprintBuff Buff;
    public int Rounds;

    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt)
    {
      try
      {
        if (evt.AttackRoll is null || !evt.AttackRoll.IsHit)
        {
          return;
        }
        var target = evt.Target;
        if (target is null || target.HPLeft <= 0)
        {
          return;
        }
        if (target.Buffs.GetBuff(Buff) != null)
        {
          return;
        }
        target.AddBuff(
          Buff, Context, duration: ContextDuration.Fixed(Rounds).Calculate(Context).Seconds);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ConstructCrafter: on-hit buff failed.", e);
      }
    }
  }

  /// <summary>
  /// Damage-over-time on the construct itself (Overdrive / Flaming). Flat value; the
  /// construct cannot be healed while it runs (paired with PreventHealing where required).
  /// </summary>
  [TypeId(Guids.PeriodicSelfDamage)]
  internal class PeriodicSelfDamage : UnitFactComponentDelegate, ITickEachRound
  {
    public int DamagePerRound;

    public void OnNewRound()
    {
      try
      {
        var amount = Math.Min(DamagePerRound, Math.Max(0, Owner.HPLeft - 1));
        if (amount > 0)
        {
          Owner.Descriptor.Damage += amount;
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("ConstructCrafter: PeriodicSelfDamage failed.", e);
      }
    }
  }

  /// <summary>
  /// Sets the crafter's active core or program (0.16.0 command menus).
  /// Null DirectiveName = the Basic entry for that axis (markers cleared).
  /// Clearing is exclusive per axis: arming a core never touches programs
  /// and vice versa. The learned gate lives here: the directive only takes
  /// effect if the crafter owns the corresponding feature (the menu lists
  /// every entry - the blueprint-static variant-list limitation documented
  /// on the Riftstalker menu).
  /// </summary>
  [TypeId(Guids.SetConstructDirectiveComponent)]
  internal class ContextActionSetConstructDirective : NamedContextAction
  {
    public string DirectiveName;
    public bool IsProgram;

    public override string GetCaption() => "Set Construct Directive";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        if (caster is null)
        {
          return;
        }

        if (IsProgram)
        {
          foreach (var program in ConstructCrafterPrograms.Programs)
          {
            var marker = caster.Buffs.GetBuff(program.Marker);
            if (marker is not null)
            {
              caster.RemoveFact(marker);
            }
          }
          if (DirectiveName is null)
          {
            return; // Basic program
          }
          foreach (var program in ConstructCrafterPrograms.Programs)
          {
            if (program.Name != DirectiveName)
            {
              continue;
            }
            if (!caster.HasFact(program.Feature))
            {
              MissionFeats.Logger.Info(
                $"[CC] program command: {DirectiveName} not learned - ignored.");
              CombatLog.Write("That program was never learned.", caster);
              return;
            }
            caster.AddBuff(program.Marker, Context);
            MissionFeats.Logger.Info($"[CC] program command: {DirectiveName} set.");
            CombatLog.Write($"Program set: {DirectiveName}.", caster);
            return;
          }
        }
        else
        {
          foreach (var core in ConstructCrafterCores.Cores)
          {
            var marker = caster.Buffs.GetBuff(core.Marker);
            if (marker is not null)
            {
              caster.RemoveFact(marker);
            }
          }
          if (DirectiveName is null)
          {
            return; // Basic core
          }
          foreach (var core in ConstructCrafterCores.Cores)
          {
            if (core.Name != DirectiveName)
            {
              continue;
            }
            if (!caster.HasFact(core.Feature))
            {
              MissionFeats.Logger.Info(
                $"[CC] core command: {DirectiveName} not learned - ignored.");
              CombatLog.Write("That core was never learned.", caster);
              return;
            }
            caster.AddBuff(core.Marker, Context);
            MissionFeats.Logger.Info($"[CC] core command: {DirectiveName} armed.");
            CombatLog.Write($"Core armed: {DirectiveName}.", caster);
            return;
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[CC] construct directive failed.", e);
      }
    }
  }

}

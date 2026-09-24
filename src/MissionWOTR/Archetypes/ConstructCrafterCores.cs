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

      var feature = FeatureConfigurator.New(featName, featGuid)
        .SetDisplayName(displayKey)
        .SetDescription(descriptionKey)
        .SetIcon(FeatureRefs.AlchemistBombsFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { activatable })
        .Configure();

      return new CoreDef
      {
        Name = featName,
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
}

using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using System.Collections.Generic;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Construct Crafter programs (user design).
  ///
  /// A program is acquired every 4th alchemist level (4/8/12/16/20) through the Program
  /// selection; the Basic program (stock behavior) is granted at level 1. The ACTIVE
  /// program is selected before deployment via toggle abilities; the deploy action applies
  /// the corresponding stat package to the construct. All stat changes scale with the
  /// crafter's alchemist level (AL): +1 per 2 AL (minimum 1), unless noted.
  ///
  /// Programs:
  ///   Passive   - AL/2: -attack, -initiative, +AC, +all saves; AI should stay out of harm
  ///   Aggressive- AL/2: +attack, +initiative, -AC; flat -2 saves; damage-focused
  ///   Flank     - sneak attack ceil(AL/2)d6 on the construct; -BAB AL/2 (non-sneak damage
  ///               lower). Sneak attack follows the game's native rules (applies on attack
  ///               rolls, including rays - so it works with spells that roll attacks).
  ///   Guard     - AL/2: +AC, +attacks of opportunity; -AL damage (two half-rank penalties);
  ///               AI should protect and support
  ///   Distance  - AL/2: +BAB, -AC; flat -2 attack and -2 saves; ranged/long-range focus
  ///   Chaos     - AL/2: +attack, +AC, +all saves, +initiative; while a chaos construct of
  ///               a base lives, that base cannot be deployed again
  ///
  /// Behavior: the Passive, Guard, and Distance programs also switch the construct to a
  /// matching behavior brain at deploy time (see ConstructCrafterAbilities); the other
  /// programs' identity is their stat package plus the stock charge AI.
  /// </summary>
  internal static class ConstructCrafterPrograms
  {
    internal class ProgramDef
    {
      public string Name;
      public BlueprintFeature Feature;
      public BlueprintBuff Marker;
      public BlueprintBuff ConstructBuff;
      public bool IsChaos;
      public bool IsFlank;
      public bool IsGuard;
      public bool IsPassive;
      public bool IsDistance;
    }

    // Priority when several toggles are active at once.
    internal static readonly List<ProgramDef> Programs = new();
    internal static BlueprintBuff ChaosMarker;

    internal static string[] AllFeatureNames => Programs.Select(p => p.Name).ToArray();

    internal static void Configure()
    {
      ChaosMarker = BuffConfigurator.New("ConstructCrafterChaosMarker", Guids.ChaosMarkerBuff)
        .SetDisplayName("ChaosMarker.Name")
        .SetDescription("ChaosMarker.Description")
        .SetIcon(BuffRefs.Confusion.Reference.Get().Icon)
        .Configure();
      // Chaos
      Programs.Add(CreateProgram(
        "ConstructCrafterChaos", Guids.ChaosProgramFeat, Guids.ChaosProgramActivatable,
        Guids.ChaosProgramMarker, Guids.ChaosProgramBuff,
        "ChaosProgram.Name", "ChaosProgram.Description",
        BuffRefs.Confusion.Reference.Get().Icon,
        buff => buff
          .AddContextStatBonus(StatType.AdditionalAttackBonus, ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(StatType.AC, ContextValues.Rank(), ModifierDescriptor.Dodge)
          .AddContextStatBonus(StatType.Initiative, ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(StatType.SaveWill, ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(StatType.SaveReflex, ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(StatType.SaveFortitude, ContextValues.Rank(), ModifierDescriptor.UntypedStackable),
        isChaos: true));

      // Distance
      Programs.Add(CreateProgram(
        "ConstructCrafterDistance", Guids.DistanceProgramFeat, Guids.DistanceProgramActivatable,
        Guids.DistanceProgramMarker, Guids.DistanceProgramBuff,
        "DistanceProgram.Name", "DistanceProgram.Description",
        FeatureRefs.PointBlankShot.Reference.Get().Icon,
        buff => buff
          .AddContextStatBonus(StatType.BaseAttackBonus, ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(StatType.AC, ContextValues.Rank(), ModifierDescriptor.Penalty)
          .AddStatBonus(stat: StatType.AdditionalAttackBonus, value: -2, descriptor: ModifierDescriptor.Penalty)
          .AddStatBonus(stat: StatType.SaveWill, value: -2, descriptor: ModifierDescriptor.Penalty)
          .AddStatBonus(stat: StatType.SaveReflex, value: -2, descriptor: ModifierDescriptor.Penalty)
          .AddStatBonus(stat: StatType.SaveFortitude, value: -2, descriptor: ModifierDescriptor.Penalty),
        isDistance: true));
      // Guard
      Programs.Add(CreateProgram(
        "ConstructCrafterGuard", Guids.GuardProgramFeat, Guids.GuardProgramActivatable,
        Guids.GuardProgramMarker, Guids.GuardProgramBuff,
        "GuardProgram.Name", "GuardProgram.Description",
        FeatureRefs.CombatReflexes.Reference.Get().Icon,
        buff => buff
          .AddContextStatBonus(StatType.AC, ContextValues.Rank(), ModifierDescriptor.Dodge)
          .AddContextStatBonus(StatType.AttackOfOpportunityCount, ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(StatType.AdditionalDamage, ContextValues.Rank(), ModifierDescriptor.Penalty)
          .AddContextStatBonus(StatType.AdditionalDamage, ContextValues.Rank(), ModifierDescriptor.Penalty)
          .AddDamageResistancePhysical(value: 2),
        isGuard: true));
      // Flank (sneak attack ranks are granted at deploy time; see the deploy action)
      Programs.Add(CreateProgram(
        "ConstructCrafterFlank", Guids.FlankProgramFeat, Guids.FlankProgramActivatable,
        Guids.FlankProgramMarker, Guids.FlankProgramBuff,
        "FlankProgram.Name", "FlankProgram.Description",
        FeatureRefs.RogueSneakAttack.Reference.Get().Icon,
        buff => buff
          .AddContextStatBonus(StatType.BaseAttackBonus, ContextValues.Rank(), ModifierDescriptor.Penalty)
          // Flank rider: the construct's casting is sapped while flanking - every
          // ability it uses resolves at -AL caster level (weaker DCs, weaker
          // level-scaled effects). The construct is a killer, not a spellcaster.
          .AddComponent(new ConstructFlankCasterLevelPenalty()),
        isFlank: true));
      // Aggressive
      Programs.Add(CreateProgram(
        "ConstructCrafterAggressive", Guids.AggressiveProgramFeat, Guids.AggressiveProgramActivatable,
        Guids.AggressiveProgramMarker, Guids.AggressiveProgramBuff,
        "AggressiveProgram.Name", "AggressiveProgram.Description",
        FeatureRefs.PowerAttackFeature.Reference.Get().Icon,
        buff => buff
          .AddContextStatBonus(StatType.AdditionalAttackBonus, ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(StatType.Initiative, ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(StatType.AC, ContextValues.Rank(), ModifierDescriptor.Penalty)
          .AddStatBonus(stat: StatType.SaveWill, value: -2, descriptor: ModifierDescriptor.Penalty)
          .AddStatBonus(stat: StatType.SaveReflex, value: -2, descriptor: ModifierDescriptor.Penalty)
          .AddStatBonus(stat: StatType.SaveFortitude, value: -2, descriptor: ModifierDescriptor.Penalty)));
      // Passive
      Programs.Add(CreateProgram(
        "ConstructCrafterPassive", Guids.PassiveProgramFeat, Guids.PassiveProgramActivatable,
        Guids.PassiveProgramMarker, Guids.PassiveProgramBuff,
        "PassiveProgram.Name", "PassiveProgram.Description",
        FeatureRefs.Dodge.Reference.Get().Icon,
        buff => buff
          .AddContextStatBonus(StatType.AdditionalAttackBonus, ContextValues.Rank(), ModifierDescriptor.Penalty)
          .AddContextStatBonus(StatType.Initiative, ContextValues.Rank(), ModifierDescriptor.Penalty)
          .AddContextStatBonus(StatType.AC, ContextValues.Rank(), ModifierDescriptor.Dodge)
          .AddContextStatBonus(StatType.SaveWill, ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(StatType.SaveReflex, ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
          .AddContextStatBonus(StatType.SaveFortitude, ContextValues.Rank(), ModifierDescriptor.UntypedStackable),
        isPassive: true));
    }

    private static ProgramDef CreateProgram(
      string featName,
      string featGuid,
      string abilityGuid,
      string markerGuid,
      string buffGuid,
      string displayKey,
      string descriptionKey,
      UnityEngine.Sprite icon,
      System.Action<BuffConfigurator> configureConstructBuff,
      bool isChaos = false,
      bool isFlank = false,
      bool isGuard = false,
      bool isPassive = false,
      bool isDistance = false)
    {
      // Crafter-side marker: shows which program is active.
      var marker = BuffConfigurator.New(featName + "Marker", markerGuid)
        .SetDisplayName(displayKey)
        .SetDescription(descriptionKey)
        .SetIcon(icon)
        .Configure();

      // Construct-side stat package (ranks evaluate against the crafter's alchemist level
      // because the buff is applied with the deploy action's context).
      var alchemist = CharacterClassRefs.AlchemistClass;
      var rank = ContextRankConfigs.ClassLevel(
        new[] { alchemist.ToString() }, min: 1).WithDiv2Progression();
      var buffBuilder = BuffConfigurator.New(featName + "Buff", buffGuid)
        .SetDisplayName(displayKey)
        .SetDescription(descriptionKey)
        .SetIcon(icon)
        .AddContextRankConfig(rank);
      configureConstructBuff(buffBuilder);
      var constructBuff = buffBuilder.Configure();

      var activatable = ActivatableAbilityConfigurator.New(featName + "Toggle", abilityGuid)
        .SetDisplayName(displayKey)
        .SetDescription(descriptionKey)
        .SetIcon(icon)
        .SetBuff(marker)
        .Configure();

      var feature = FeatureConfigurator.New(featName, featGuid)
        .SetDisplayName(displayKey)
        .SetDescription(descriptionKey)
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { activatable })
        .Configure();

      return new ProgramDef
      {
        Name = featName,
        Feature = feature,
        Marker = marker,
        ConstructBuff = constructBuff,
        IsChaos = isChaos,
        IsFlank = isFlank,
        IsGuard = isGuard,
        IsPassive = isPassive,
        IsDistance = isDistance,
      };
    }

    /// <summary>
    /// Returns the active program (priority: Chaos &gt; Distance &gt; Guard &gt; Flank &gt;
    /// Aggressive &gt; Passive), or null for the Basic program.
    /// </summary>
    internal static ProgramDef GetActiveProgram(UnitEntityData crafter)
    {
      if (crafter is null)
      {
        return null;
      }
      foreach (var program in Programs)
      {
        if (crafter.Buffs.GetBuff(program.Marker) != null)
        {
          return program;
        }
      }
      return null;
    }
  }
}

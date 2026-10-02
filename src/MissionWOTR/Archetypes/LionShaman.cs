using BlueprintCore.Blueprints.Configurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Lion Shaman (faithful port of the totemic shaman druid archetype,
  /// Pathfinder RPG - the lion totem).
  ///
  /// Tabletop:
  /// - Nature Bond: lion companion, or the Animal/Glory/Nobility/Sun domains
  ///   (NOT ported: the vanilla nature bond covers the companion; the domain
  ///   list restriction would need a custom selection - the vanilla one is
  ///   left in place).
  /// - Wild Empathy: +4 with felines (NOT ported: WOTR wild empathy cannot
  ///   be narrowed by creature kind; the vanilla feature stays).
  /// - Totem Transformation (2nd): adopt an aspect of the lion while
  ///   retaining her form - movement (+20 enhancement speed), senses
  ///   (low-light vision, scent) or natural weapons (bite 1d4, 2 claws 1d4,
  ///   rake, +2 grapple). Replaces woodland stride and trackless step.
  /// - Totemic Summons (5th): summon nature's ally casts as a standard
  ///   action when summoning felines, and those creatures gain temporary hit
  ///   points equal to her druid level. Replaces a thousand faces.
  /// - Wild Shape (6th): functions at her druid level - 2, except when she
  ///   takes a feline form, which uses her druid level + 2.
  /// - Bonus Feat (9th and every 4 levels): Dodge, Lunge, Improved Iron
  ///   Will, Iron Will, or Skill Focus (Acrobatics). Replaces venom immunity.
  ///
  /// WOTR adaptation notes:
  /// - Trackless step and a thousand faces do not exist in WOTR; the 2nd and
  ///   5th level trades are mapped onto woodland stride and resist nature's
  ///   lure (real removals against the live progression, by guid).
  /// - Totem Transformation: the tabletop's minutes-per-day pool is
  ///   simplified into three toggleable aspects (the Untouchable Rager
  ///   control-toggle pattern); activating one aspect sheds the others, as
  ///   on the tabletop. Senses grants the vanilla scent feature (low-light
  ///   vision is near-universal in WOTR); natural weapons grants the
  ///   vanilla Animal Fury bite.
  /// - Wild shape -2/+2: the vanilla wild shape features (animal + elemental
  ///   sizes) are removed at their live levels and re-added two levels later
  ///   (ArchetypeRemovals.DelayFeatures); the feline form instead arrives
  ///   EARLY - the shifter's tiger (smilodon) form at 4th, its tier-8
  ///   variant at 8th and tier-15 at 14th (granting the DLC shifter class
  ///   form abilities wholesale, the Feral Champion cross-class pattern).
  /// - Totemic Summons: WOTR summons are already standard action; the feline
  ///   temp-HP rider needs a summon hook the engine does not expose. The
  ///   feature grants the vanilla Augment Summoning feat instead (a real,
  ///   always-on summon upgrade) - the adaptation is documented here and in
  ///   docs/ARCHETYPES.md.
  /// - Bonus feats: Lunge was not found among the game's feature blueprints;
  ///   the selection offers Dodge, Iron Will, Improved Iron Will and Skill
  ///   Focus (Acrobatics), granted at 9th/13th/17th.
  /// Log prefix: [removals] carries the trade diagnostics; [lion] the rest.
  /// </summary>
  internal static class LionShaman
  {
    // 0.53.0 — RENAMED. ExpandedContent ships its Lion Totem Druid under the
    // identical blueprint asset name "LionShamanArchetype" (ka-dyn/ExpandedContent,
    // ExpandedContent/Tweaks/Archetypes/LionShaman.cs). The guids differ so both
    // load, but any name-keyed lookup — the game's own or BlueprintCore's
    // GuidByName — could resolve to the wrong mod's blueprint. Ours is now
    // unambiguous, and the display name was already distinct ("Lion Shaman"
    // vs. their "Lion Totem Druid").
    internal const string ArchetypeName = "MissionLionShamanArchetype";
    internal const string AspectName = "LionShamanTotemTransformation";
    internal const string MovementName = "LionShamanAspectMovement";
    internal const string SensesName = "LionShamanAspectSenses";
    internal const string WeaponsName = "LionShamanAspectWeapons";
    internal const string FelineName = "LionShamanFelineWildShape";
    internal const string TotemicSummonsName = "LionShamanTotemicSummons";
    internal const string BonusFeatsName = "LionShamanBonusFeats";

    public static void Configure()
    {
      var druid = CharacterClassRefs.DruidClass.Reference.Get();
      var tigerIcon = AbilityRefs.ShifterWildShapeTigerAbillity.Reference.Get().Icon;

      // ----- Totem Transformation (2nd): three lion aspects -----
      // 0.53.0 — the three aspects now SCALE with druid level (tier 1 at
      // 2nd, 2 at 8th, 3 at 14th) instead of being flat. This is what
      // separates the Lion Shaman from ExpandedContent's Lion Totem
      // Druid, which is a wild-shape-timing archetype with nothing
      // comparable. The movement aspect's old flat +20 speed is gone:
      // the rider grants +10/+20/+30 instead, so it is not double-dipped.
      var movementBuff = BuffConfigurator.New(MovementName + "Buff", Guids.LionAspectMovementBuff)
        .SetDisplayName("LionShamanAspectMovement.Name")
        .SetDescription("LionShamanAspectMovement.Description")
        .SetIcon(AbilityRefs.ExpeditiousRetreat.Reference.Get().Icon)
        .AddComponent(new LionAspectScaling { DruidClass = druid, Aspect = 1 })
        .SetIsClassFeature()
        .Configure();
      var sensesBuff = BuffConfigurator.New(SensesName + "Buff", Guids.LionAspectSensesBuff)
        .SetDisplayName("LionShamanAspectSenses.Name")
        .SetDescription("LionShamanAspectSenses.Description")
        .SetIcon(FeatureRefs.AnimalCompanionScent30.Reference.Get().Icon)
        .AddFacts(new() { FeatureRefs.AnimalCompanionScent30.Reference.Get() })
        .AddComponent(new LionAspectScaling { DruidClass = druid, Aspect = 2 })
        .SetIsClassFeature()
        .Configure();
      var weaponsBuff = BuffConfigurator.New(WeaponsName + "Buff", Guids.LionAspectWeaponsBuff)
        .SetDisplayName("LionShamanAspectWeapons.Name")
        .SetDescription("LionShamanAspectWeapons.Description")
        .SetIcon(FeatureRefs.AnimalFuryFeature.Reference.Get().Icon)
        .AddFacts(new() { FeatureRefs.AnimalFuryFeature.Reference.Get() })
        .AddComponent(new LionAspectScaling { DruidClass = druid, Aspect = 3 })
        .SetIsClassFeature()
        .Configure();

      // The three aspects are mutually exclusive, as on the tabletop:
      // activating one sheds the other two.
      BuffConfigurator.For(MovementName + "Buff")
        .AddComponent(new LionAspectExclusivity { First = sensesBuff, Second = weaponsBuff })
        .Configure();
      BuffConfigurator.For(SensesName + "Buff")
        .AddComponent(new LionAspectExclusivity { First = movementBuff, Second = weaponsBuff })
        .Configure();
      BuffConfigurator.For(WeaponsName + "Buff")
        .AddComponent(new LionAspectExclusivity { First = movementBuff, Second = sensesBuff })
        .Configure();

      var movementAspect = ActivatableAbilityConfigurator.New(
          MovementName + "Activatable", Guids.LionAspectMovementActivatable)
        .SetDisplayName("LionShamanAspectMovement.Name")
        .SetDescription("LionShamanAspectMovement.Description")
        .SetIcon(AbilityRefs.ExpeditiousRetreat.Reference.Get().Icon)
        .SetBuff(movementBuff)
        .Configure();
      var sensesAspect = ActivatableAbilityConfigurator.New(
          SensesName + "Activatable", Guids.LionAspectSensesActivatable)
        .SetDisplayName("LionShamanAspectSenses.Name")
        .SetDescription("LionShamanAspectSenses.Description")
        .SetIcon(FeatureRefs.AnimalCompanionScent30.Reference.Get().Icon)
        .SetBuff(sensesBuff)
        .Configure();
      var weaponsAspect = ActivatableAbilityConfigurator.New(
          WeaponsName + "Activatable", Guids.LionAspectWeaponsActivatable)
        .SetDisplayName("LionShamanAspectWeapons.Name")
        .SetDescription("LionShamanAspectWeapons.Description")
        .SetIcon(FeatureRefs.AnimalFuryFeature.Reference.Get().Icon)
        .SetBuff(weaponsBuff)
        .Configure();

      var aspect = FeatureConfigurator.New(AspectName, Guids.LionAspectFeature)
        .SetDisplayName("LionShamanTotemTransformation.Name")
        .SetDescription("LionShamanTotemTransformation.Description")
        .SetIcon(tigerIcon)
        .SetIsClassFeature()
        .AddFacts(new() { movementAspect, sensesAspect, weaponsAspect })
        .Configure();

      // ----- Feline Wild Shape: early, and better (4th / 8th / 14th) -----
      // The feline form is granted whole from the shifter's tiger (smilodon)
      // family - two levels before even the delayed wild shape starts.
      var feline = FeatureConfigurator.New(FelineName, Guids.LionFelineForm)
        .SetDisplayName("LionShamanFelineWildShape.Name")
        .SetDescription("LionShamanFelineWildShape.Description")
        .SetIcon(tigerIcon)
        .SetIsClassFeature()
        .AddFacts(new() { AbilityRefs.ShifterWildShapeTigerAbillity.Reference.Get() })
        .Configure();
      var feline8 = FeatureConfigurator.New(FelineName + "8", Guids.LionFelineForm8)
        .SetDisplayName("LionShamanFelineWildShape.Name")
        .SetDescription("LionShamanFelineWildShape.Description")
        .SetIcon(tigerIcon)
        .SetIsClassFeature()
        .AddFacts(new() { AbilityRefs.ShifterWildShapeTigerAbillity8.Reference.Get() })
        .Configure();
      var feline15 = FeatureConfigurator.New(FelineName + "15", Guids.LionFelineForm15)
        .SetDisplayName("LionShamanFelineWildShape.Name")
        .SetDescription("LionShamanFelineWildShape.Description")
        .SetIcon(tigerIcon)
        .SetIsClassFeature()
        .AddFacts(new() { AbilityRefs.ShifterWildShapeTigerAbillity15.Reference.Get() })
        .Configure();

      // ----- Totemic Summons (5th) -----
      var totemic = FeatureConfigurator.New(TotemicSummonsName, Guids.LionTotemicSummons)
        .SetDisplayName("LionShamanTotemicSummons.Name")
        .SetDescription("LionShamanTotemicSummons.Description")
        .SetIcon(FeatureRefs.AugmentSummoning.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { FeatureRefs.AugmentSummoning.Reference.Get() })
        .Configure();

      // ----- Bonus feats (9th and every 4 levels) -----
      var selection = FeatureSelectionConfigurator.New(BonusFeatsName, Guids.LionBonusFeatSelection)
        .SetDisplayName("LionShamanBonusFeats.Name")
        .SetDescription("LionShamanBonusFeats.Description")
        .SetIcon(FeatureRefs.Dodge.Reference.Get().Icon)
        .AddToAllFeatures(
          FeatureRefs.Dodge.Reference.Get(),
          FeatureRefs.IronWill.Reference.Get(),
          FeatureRefs.IronWillImproved.Reference.Get(),
          FeatureRefs.SkillFocusAcrobatics.Reference.Get())
        .Configure();

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.LionShamanArchetype, CharacterClassRefs.DruidClass)
          .SetLocalizedName("LionShaman.Name")
          .SetLocalizedDescription("LionShaman.Description")
          .AddToAddFeatures(LevelPlan.L(2), aspect)
          .AddToAddFeatures(LevelPlan.L(4), feline)
          .AddToAddFeatures(LevelPlan.L(5), totemic)
          .AddToAddFeatures(LevelPlan.L(8), feline8)
          .AddToAddFeatures(LevelPlan.L(9), selection)
          .AddToAddFeatures(LevelPlan.L(13), selection)
          .AddToAddFeatures(LevelPlan.L(14), feline15)
          .AddToAddFeatures(LevelPlan.L(17), selection);

      // Trades: woodland stride (2nd), resist nature's lure (5th, standing in
      // for the nonexistent a thousand faces), venom immunity (9th).
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, druid,
        "4c1419ef6cfc430a9071405788da4a73", // DruidWoodlandStride
        "ad6a5b0e1a65c3540986cf9a7b006388", // ResistNaturesLure
        "5078622eb5cecaf4683fa16a9b948c2c"); // VenomImmunity

      // Wild shape -2: the vanilla animal and elemental wild shape features
      // are delayed two levels each (the feline form above arrives early).
      archetype = ArchetypeRemovals.DelayFeatures(
        archetype, druid, 2,
        "e9d4d569f5354fac88e26b058c6a1de8",  // DruidWildShape
        "1186fc7362560c94bad3de6338cc509e",  // WildShapeElementaLarge
        "fe58dd496a36e274b86958f4677071b2"); // WildShapeElementaHuge

      archetype.Configure();

      MissionFeats.Logger.Info("LionShaman: configured.");
    }
  }

  /// <summary>
  /// 0.53.0 — the lion aspects scale with druid level. This is what
  /// separates the Lion Shaman from ExpandedContent's Lion Totem Druid,
  /// which is a wild-shape-timing archetype with nothing comparable: the
  /// overlap between the two was the wild shape, and the totem is what
  /// is actually ours.
  ///
  /// Tier 1 at 2nd, 2 at 8th, 3 at 14th.
  /// - Movement: +10 ft speed and +1 dodge AC per tier.
  /// - Senses: +1 Will and Reflex per tier, on top of the scent.
  /// - Weapons: +1 attack and damage per tier, on top of the bite.
  ///
  /// Modifiers are removed-then-reapplied on activate and every round and
  /// keyed to this component's Runtime, so they never stack and never
  /// outlive the aspect — the BeastboundLinkRider idiom, including the
  /// 0.52.0 lesson that the key must be the component Runtime, not Fact.
  /// </summary>
  [TypeId(Guids.LionAspectScalingRider)]
  internal class LionAspectScaling : UnitFactComponentDelegate, ITickEachRound
  {
    public BlueprintCharacterClass DruidClass;

    /// <summary>1 = movement, 2 = senses, 3 = natural weapons.</summary>
    public int Aspect;

    protected override void OnActivate() => Refresh();

    public void OnNewRound() => Refresh();

    protected override void OnDeactivate() => Release();

    private void Release()
    {
      try
      {
        var stats = Owner.Stats;
        stats.Speed.RemoveModifiersFrom(Runtime);
        stats.AC.RemoveModifiersFrom(Runtime);
        stats.SaveWill.RemoveModifiersFrom(Runtime);
        stats.SaveReflex.RemoveModifiersFrom(Runtime);
        stats.AdditionalAttackBonus.RemoveModifiersFrom(Runtime);
        stats.AdditionalDamage.RemoveModifiersFrom(Runtime);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[lion] aspect release failed.", e);
      }
    }

    private void Refresh()
    {
      try
      {
        var level = DruidClass is null
          ? 1
          : Owner.Descriptor.Progression.GetClassLevel(DruidClass);
        var tier = level >= 14 ? 3 : level >= 8 ? 2 : 1;
        Release();
        var stats = Owner.Stats;
        switch (Aspect)
        {
          case 1:
            stats.Speed.AddModifierUnique(
              10 * tier, Runtime, ModifierDescriptor.Enhancement);
            stats.AC.AddModifierUnique(tier, Runtime, ModifierDescriptor.Dodge);
            break;
          case 2:
            stats.SaveWill.AddModifierUnique(tier, Runtime, ModifierDescriptor.UntypedStackable);
            stats.SaveReflex.AddModifierUnique(tier, Runtime, ModifierDescriptor.UntypedStackable);
            break;
          default:
            stats.AdditionalAttackBonus.AddModifierUnique(
              tier, Runtime, ModifierDescriptor.UntypedStackable);
            stats.AdditionalDamage.AddModifierUnique(
              tier, Runtime, ModifierDescriptor.UntypedStackable);
            break;
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[lion] aspect scaling failed.", e);
      }
    }
  }

  /// <summary>
  /// Totem aspect exclusivity: when one lion aspect activates, the other two
  /// are shed (the tabletop allows only one aspect at a time).
  /// </summary>
  [TypeId(Guids.LionAspectExclusivity)]
  internal class LionAspectExclusivity : UnitBuffComponentDelegate
  {
    public BlueprintBuff First;
    public BlueprintBuff Second;

    protected override void OnActivate()
    {
      try
      {
        Shed(First);
        Shed(Second);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[lion] aspect exclusivity failed.", e);
      }
    }

    private void Shed(BlueprintBuff buff)
    {
      if (buff is null)
      {
        return;
      }
      var active = Owner.Buffs.GetBuff(buff);
      if (active != null)
      {
        Owner.RemoveFact(active);
      }
    }
  }
}

using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Spells;
using BlueprintCore.Blueprints.CustomConfigurators.Facts;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// THE CHIMERA (witch homebrew - the user's design, 0.33.0).
  ///
  /// The user's brief: the parked ranger idea ("the Chimera's Diet"
  /// - a companion that takes a piece of what it kills) moves to
  /// the witch, "since witch has familiars but not really animal
  /// companions to work with. This archetype should remove those
  /// familiars, as well as 8 levels of hexes, and lower the amount
  /// of spells you can cast each day by 1 starting from level 6."
  /// (Revised from the first sketch, where the witch herself was
  /// the chimera - the user's correction: "for a pure spellcasting
  /// class it wouldnt make much sense, for something like a magus
  /// or warpriest it could work though." Noted for a future gish.)
  ///
  /// The trades:
  /// - The FAMILIAR goes (WitchFamiliarSelection, 1st). The bond
  ///   becomes something with teeth instead.
  /// - EIGHT HEX LEVELS go: the witch's hex grants are found in her
  ///   progression at configure time and removed at every level
  ///   EXCEPT the first two (the new AddRemovalsExceptFirstN -
  ///   whatever the exact grant schedule, eight of them go).
  /// - THE SPELLS THIN: the archetype carries a REPLACED SPELLBOOK
  ///   (the COP WinterWitch idiom - ArchetypeConfigurator
  ///   .SetReplaceSpellbook): a copy of the vanilla witch book
  ///   whose spells-per-day table is rebuilt with every spell
  ///   level's slot count reduced by 1 at class levels 6 and above
  ///   (floor 0). Documented limitation: prestige classes that
  ///   advance the witch book (Mystic Theurge and friends) will not
  ///   advance the chimera book - their replace-spellbook features
  ///   look for the vanilla WitchSpellbook (COP's WinterWitch had
  ///   to patch each one; documented rather than patched here).
  ///
  /// The grants:
  /// - THE COMPANION (1st): the full vanilla selection - every
  ///   species, preorder variants included - each one a CLONE whose
  ///   AddPet level-rank is retargeted to ChimeraCompanionRank (the
  ///   TTTB Animal Ally idiom), a hidden rank feature the
  ///   ARCHETYPE grants at every level 1-20: the beast grows at the
  ///   witch's full class level, no druid tax. The selection also
  ///   carries the vanilla mount-target and companion-archetype
  ///   features (the Animal Ally wiring).
  /// - THE DIET: when the COMPANION kills a creature, it takes a
  ///   piece of it - one permanent trait per creature type, twelve
  ///   types in all (Aberration, Animal, Construct, Dragon, Fey,
  ///   Lycanthrope, Magical Beast, Monstrous Humanoid, Outsider,
  ///   Plant, Undead, Vermin):
  ///     Alien Mind +2 Will / Beast's Vigor +10 speed /
  ///     Stone Guts DR 2/adamantine / Dragon's Eye +1 attack /
  ///     Fey Step +2 Reflex / Silverhide DR 2/silver /
  ///     Monster's Hide +2 natural AC / Hunter's Instinct +2 init /
  ///     Hellhound's Blood fire resist 10 / Rooted Flesh +2 Fort /
  ///     Grave's Gift +1 all saves / Swarm-Joints entangled immunity.
  ///   The diet is recorded as visible MARKER BUFFS on the beast
  ///   (its buff bar shows what it has eaten) and the traits are
  ///   features re-applied on the bond's tick if a reload loses
  ///   them - the record persists either way.
  ///   Documented cuts: humanoids leave no piece worth taking (no
  ///   HumanoidType fact exists to detect them - and the chimera
  ///   would agree); only the COMPANION's kills feed the diet (the
  ///   witch's own kills are hers, not the beast's).
  /// Log prefix: [chimera].
  /// </summary>
  internal static class Chimera
  {
    internal const string ArchetypeName = "ChimeraArchetype";

    public static void Configure()
    {
      var witch = CharacterClassRefs.WitchClass.Reference.Get();

      // ----- The rank feature: the beast's level (granted 1-20) -----
      var rank = FeatureConfigurator.New(
        "ChimeraCompanionRank", Guids.ChimeraCompanionRank)
        .SetRanks(20)
        .SetIsClassFeature()
        .SetHideInUI()
        .Configure();

      // ----- The companion clones (the Animal Ally idiom) -----
      var mountTarget = "cb06f0e72ffb5c640a156bd9f8000c1d";
      var companionArchetypes = "65af7290b4efd5f418132141aaa36c1b";
      var clones = new List<BlueprintFeature>
      {
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureDog, Guids.ChimeraDogCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureElk, Guids.ChimeraElkCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureHorse, Guids.ChimeraHorseCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureHorse_PreorderBonus, Guids.ChimeraHorsePreorderCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureLeopard, Guids.ChimeraLeopardCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureMonitor, Guids.ChimeraMonitorCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureWolf, Guids.ChimeraWolfCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureBoar, Guids.ChimeraBoarCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureBear, Guids.ChimeraBearCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureCentipede, Guids.ChimeraCentipedeCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureMammoth, Guids.ChimeraMammothCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureSmilodon, Guids.ChimeraSmilodonCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureSmilodon_PreorderBonus, Guids.ChimeraSmilodonPreorderCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureTriceratops, Guids.ChimeraTriceratopsCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureTriceratops_PreorderBonus, Guids.ChimeraTriceratopsPreorderCompanion, rank),
        CloneCompanion(FeatureRefs.AnimalCompanionFeatureVelociraptor, Guids.ChimeraVelociraptorCompanion, rank),
      };

      // ----- The selection -----
      var selection = FeatureSelectionConfigurator.New(
        "ChimeraCompanionSelection", Guids.ChimeraCompanionSelection)
        .SetDisplayName("ChimeraCompanionSelection.Name")
        .SetDescription("ChimeraCompanionSelection.Description")
        .SetIcon(FeatureSelectionRefs.AnimalCompanionSelectionRanger.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFeatureOnApply(mountTarget)
        .AddFeatureOnApply(companionArchetypes)
        .Configure();
      foreach (var clone in clones)
      {
        FeatureSelectionConfigurator.For(selection)
          .AddToAllFeatures(clone.name)
          .Configure();
      }

      // ----- The diet: markers and traits -----
      var markers = new List<BlueprintBuff>();
      var traits = new List<BlueprintFeature>();
      var typeFacts = new List<BlueprintFeature>();
      Diet(
        FeatureRefs.AberrationType, Guids.ChimeraAberrationMarker, Guids.ChimeraAberrationTrait,
        "ChimeraAberration", "Alien Mind",
        b => b.AddStatBonus(stat: StatType.SaveWill, value: 2, descriptor: ModifierDescriptor.Insight),
        markers, traits, typeFacts);
      Diet(
        FeatureRefs.AnimalType, Guids.ChimeraAnimalMarker, Guids.ChimeraAnimalTrait,
        "ChimeraAnimal", "Beast's Vigor",
        b => b.AddStatBonus(stat: StatType.Speed, value: 10, descriptor: ModifierDescriptor.Enhancement),
        markers, traits, typeFacts);
      Diet(
        FeatureRefs.ConstructType, Guids.ChimeraConstructMarker, Guids.ChimeraConstructTrait,
        "ChimeraConstruct", "Stone Guts",
        b => b.AddDamageResistancePhysical(
          value: ContextValues.Constant(2),
          material: PhysicalDamageMaterial.Adamantine, bypassedByMaterial: true),
        markers, traits, typeFacts);
      Diet(
        FeatureRefs.DragonType, Guids.ChimeraDragonMarker, Guids.ChimeraDragonTrait,
        "ChimeraDragon", "Dragon's Eye",
        b => b.AddStatBonus(stat: StatType.AdditionalAttackBonus, value: 1, descriptor: ModifierDescriptor.Competence),
        markers, traits, typeFacts);
      Diet(
        FeatureRefs.FeyType, Guids.ChimeraFeyMarker, Guids.ChimeraFeyTrait,
        "ChimeraFey", "Fey Step",
        b => b.AddStatBonus(stat: StatType.SaveReflex, value: 2, descriptor: ModifierDescriptor.Insight),
        markers, traits, typeFacts);
      Diet(
        FeatureRefs.LycanthropeType, Guids.ChimeraLycanthropeMarker, Guids.ChimeraLycanthropeTrait,
        "ChimeraLycanthrope", "Silverhide",
        b => b.AddDamageResistancePhysical(
          value: ContextValues.Constant(2),
          material: PhysicalDamageMaterial.Silver, bypassedByMaterial: true),
        markers, traits, typeFacts);
      Diet(
        FeatureRefs.MagicalBeastType, Guids.ChimeraMagicalBeastMarker, Guids.ChimeraMagicalBeastTrait,
        "ChimeraMagicalBeast", "Monster's Hide",
        b => b.AddStatBonus(stat: StatType.AC, value: 2, descriptor: ModifierDescriptor.NaturalArmor),
        markers, traits, typeFacts);
      Diet(
        FeatureRefs.MonstrousHumanoidType, Guids.ChimeraMonstrousHumanoidMarker, Guids.ChimeraMonstrousHumanoidTrait,
        "ChimeraMonstrousHumanoid", "Hunter's Instinct",
        b => b.AddStatBonus(stat: StatType.Initiative, value: 2, descriptor: ModifierDescriptor.Competence),
        markers, traits, typeFacts);
      Diet(
        FeatureRefs.OutsiderType, Guids.ChimeraOutsiderMarker, Guids.ChimeraOutsiderTrait,
        "ChimeraOutsider", "Hellhound's Blood",
        b => b.AddDamageResistanceEnergy(
          type: DamageEnergyType.Fire, value: ContextValues.Constant(10)),
        markers, traits, typeFacts);
      Diet(
        FeatureRefs.PlantType, Guids.ChimeraPlantMarker, Guids.ChimeraPlantTrait,
        "ChimeraPlant", "Rooted Flesh",
        b => b.AddStatBonus(stat: StatType.SaveFortitude, value: 2, descriptor: ModifierDescriptor.Insight),
        markers, traits, typeFacts);
      Diet(
        FeatureRefs.UndeadType, Guids.ChimeraUndeadMarker, Guids.ChimeraUndeadTrait,
        "ChimeraUndead", "Grave's Gift",
        b => b
          .AddStatBonus(stat: StatType.SaveFortitude, value: 1, descriptor: ModifierDescriptor.Insight)
          .AddStatBonus(stat: StatType.SaveReflex, value: 1, descriptor: ModifierDescriptor.Insight)
          .AddStatBonus(stat: StatType.SaveWill, value: 1, descriptor: ModifierDescriptor.Insight),
        markers, traits, typeFacts);
      Diet(
        FeatureRefs.VerminType, Guids.ChimeraVerminMarker, Guids.ChimeraVerminTrait,
        "ChimeraVermin", "Swarm-Joints",
        b => b.AddConditionImmunity(condition: UnitCondition.Entangled),
        markers, traits, typeFacts);

      // ----- The bond (the diet logic, on the witch) -----
      var bond = FeatureConfigurator.New(
        "ChimeraBondFeature", Guids.ChimeraBondFeature)
        .SetDisplayName("ChimeraBond.Name")
        .SetDescription("ChimeraBond.Description")
        .SetIcon(FeatureSelectionRefs.AnimalCompanionSelectionRanger.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new ChimeraCore
        {
          TypeFacts = typeFacts.ToArray(),
          Markers = markers.ToArray(),
          Traits = traits.ToArray(),
        })
        .Configure();

      // ----- The thinned spellbook (the COP WinterWitch idiom) -----
      // The COP ArrowsongMinstrel recipe, verbatim in spirit: clone the
      // witch's slots table with every count reduced (floor 0). Rows are
      // keyed by array position = class level (COP ships without setting
      // Level on the entries); the tax starts at row 5 = class level 6.
      var vanillaBook = SpellbookRefs.WitchSpellbook.Reference.Get();
      var vanillaTable = vanillaBook.m_SpellsPerDay.Get();
      var entries = vanillaTable.Levels;
      var levels = new List<SpellsLevelEntry>();
      for (int row = 0; row < entries.Length; row++)
      {
        var slots = new int[entries[row].Count.Length];
        for (int i = 0; i < entries[row].Count.Length; i++)
        {
          slots[i] = row >= 5 ? Math.Max(0, entries[row].Count[i] - 1) : entries[row].Count[i];
        }
        levels.Add(new SpellsLevelEntry { Count = slots });
      }
      var table = SpellsTableConfigurator.New(
        "ChimeraSpellsTable", Guids.ChimeraSpellsTable)
        .SetLevels(levels.ToArray())
        .Configure();
      var book = SpellbookConfigurator.New(
        "ChimeraSpellbook", Guids.ChimeraSpellbook)
        .CopyFrom(SpellbookRefs.WitchSpellbook)
        .SetSpellsPerDay(table)
        .Configure();

      // ----- The archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.ChimeraArchetype, CharacterClassRefs.WitchClass)
          .SetLocalizedName("Chimera.Name")
          .SetLocalizedDescription("Chimera.Description")
          .SetReplaceSpellbook(book)
          .AddToAddFeatures(LevelPlan.L(1), selection)
          .AddToAddFeatures(LevelPlan.L(1), bond);
      for (int level = 1; level <= 20; level++)
      {
        archetype = archetype.AddToAddFeatures(LevelPlan.L(level), rank);
      }

      // The trades: the familiar, and eight of the hex levels (the
      // first two hex grants are kept).
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, witch, FeatureSelectionRefs.WitchFamiliarSelection.ToString());
      archetype = ArchetypeRemovals.AddRemovalsExceptFirstN(
        archetype, witch, FeatureSelectionRefs.WitchHexSelection.ToString(), keepFirst: 2);

      archetype.Configure();
      MissionFeats.Logger.Info("[chimera] configured: " + ArchetypeName + ".");
    }

    /// <summary>
    /// The Animal Ally clone recipe (TTTB): copy the vanilla
    /// companion feature and retarget its AddPet level-rank to the
    /// chimera's own rank feature - the beast then levels with the
    /// witch's class levels instead of a druid's.
    /// </summary>
    private static BlueprintFeature CloneCompanion(
      Blueprint<BlueprintReference<BlueprintFeature>> source,
      string cloneGuid,
      BlueprintFeature rankFeature)
    {
      var cloneName = "Chimera" + source.Reference.Get().name + "Feature";
      return FeatureConfigurator.New(cloneName, cloneGuid)
        .CopyFrom(source)
        .EditComponent<AddPet>(
          c => c.m_LevelRank = rankFeature.ToReference<BlueprintFeatureReference>())
        .Configure();
    }

    /// <summary>
    /// One course of the diet: a visible marker buff (the record on
    /// the beast) and a permanent trait feature, for one creature
    /// type.
    /// </summary>
    private static void Diet(
      Blueprint<BlueprintReference<BlueprintFeature>> typeFact,
      string markerGuid,
      string traitGuid,
      string key,
      string traitName,
      Action<FeatureConfigurator> traitComponents,
      List<BlueprintBuff> markers,
      List<BlueprintFeature> traits,
      List<BlueprintFeature> typeFacts)
    {
      var icon = typeFact.Reference.Get().Icon;
      markers.Add(BuffConfigurator.New(key + "MarkerBuff", markerGuid)
        .SetDisplayName(key + "Marker.Name")
        .SetDescription(key + "Marker.Description")
        .SetIcon(icon)
        .Configure());
      var trait = FeatureConfigurator.New(key + "TraitFeature", traitGuid)
        .SetDisplayName(key + "Trait.Name")
        .SetDescription(key + "Trait.Description")
        .SetIcon(icon)
        .SetIsClassFeature();
      traitComponents(trait);
      traits.Add(trait.Configure());
      typeFacts.Add(typeFact.Reference.Get());
    }
  }

  /// <summary>
  /// The bond, on the witch: watches her companion's kills. When
  /// the beast slays a creature of a type it has not yet eaten, the
  /// diet is recorded (a visible marker buff on the beast) and the
  /// corresponding permanent trait is granted. The tick re-applies
  /// any trait a reload may have dropped - the markers are the
  /// persisted record.
  /// </summary>
  [TypeId(Guids.ChimeraCoreComponent)]
  internal class ChimeraCore : UnitFactComponentDelegate,
    Kingmaker.Controllers.Units.ITickEachRound,
    IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintFeature[] TypeFacts;
    public BlueprintBuff[] Markers;
    public BlueprintFeature[] Traits;

    public void OnNewRound()
    {
      try
      {
        Repair();
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[chimera] bond tick failed.", e);
      }
    }

    protected override void OnActivate()
    {
      Repair();
    }

    private void Repair()
    {
      var pet = Wildbond.PetOf(Owner);
      if (pet is null)
      {
        return;
      }
      for (int i = 0; i < Markers.Length; i++)
      {
        if (pet.Buffs.GetBuff(Markers[i]) is not null && pet.GetFact(Traits[i]) is null)
        {
          pet.AddFact(Traits[i]);
        }
      }
    }

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        var pet = Wildbond.PetOf(Owner);
        if (pet is null || evt.Initiator != pet)
        {
          return;
        }
        var victim = evt.Target;
        if (victim is null ||
          (!victim.Descriptor.State.IsDead && victim.HPLeft > 0))
        {
          return; // not a kill
        }
        for (int i = 0; i < TypeFacts.Length; i++)
        {
          if (!victim.HasFact(TypeFacts[i]))
          {
            continue;
          }
          if (pet.Buffs.GetBuff(Markers[i]) is not null)
          {
            return; // this course was already eaten
          }
          pet.Descriptor.AddBuff(Markers[i], Fact.MaybeContext, new Rounds(6000).Seconds);
          pet.AddFact(Traits[i]);
          CombatLog.Write("The chimera takes a piece of the kill.", Owner);
          return;
        }
        // Humanoids and untyped victims leave no piece worth taking
        // (documented cut: no HumanoidType fact exists to detect).
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[chimera] diet failed.", e);
      }
    }
  }
}

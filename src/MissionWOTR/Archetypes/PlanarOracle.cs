using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using MissionWOTR.Feats;
using System;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Planar Oracle (tabletop port - Ultimate Magic pg. 59; 0.24.0, the
  /// user's correction: "I meant a tabletop archetype, not a homebrew
  /// one"). "A planar oracle has an affinity with one of the Outer
  /// Planes. She is at home on the multiple planes of the Great
  /// Beyond, and can eventually become an outsider herself."
  ///
  /// Coverage check: vanilla WOTR oracle archetypes are Seeker,
  /// Dual-Cursed, Enlightened Philosopher, Possessed and Divine
  /// Herbalist; the content mods add none. The tabletop Planar Oracle
  /// is unclaimed.
  ///
  /// The tabletop kit, and its Wrath adaptations (every cut
  /// documented, per house rules):
  /// - 1st: choose an Outer Plane (a selection of four - Heaven, Hell,
  ///   the Abyss, the Maelstrom - each bound to one energy: the
  ///   tabletop leaves the plane-energy association to the GM; our
  ///   mapping follows each plane's native outsiders, documented).
  /// - 3rd: Planar Resistance - energy resistance 10 against the
  ///   chosen plane's energy. Replaces the 3rd-level revelation.
  /// - 11th: the resistance rises to 20 (+10 more).
  /// - 20th: apotheosis - immunity to the plane's energy and damage
  ///   reduction 10/magic; the final revelation is traded for it.
  /// - Bonus spells at 2nd/4th/.../18th (endure elements, elemental
  ///   speech, tongues, planar adaptation, plane shift, mass planar
  ///   adaptation, shadow walk, etherealness, gate - NONE of which
  ///   exist in WOTR's engine). Every one is substituted with a
  ///   native-level-matched planar pilgrim's spell (see the table
  ///   below), granted as oracle spells known.
  ///
  /// Adaptation honesty (the two structural deviations):
  /// - The tabletop trades the MYSTERY's bonus spells for the planar
  ///   list. In WOTR the mystery's spells arrive through the mystery's
  ///   own grant chain, which class-level archetype removal cannot
  ///   intercept (the vanilla Enlightened Philosopher swaps the entire
  ///   mystery selection - a heavier surgery than this port's scope).
  ///   The planar spells are therefore granted ADDITIVELY, and the
  ///   trade is carried by an EXTRA revelation removal (the 7th) as
  ///   compensation. Documented.
  /// - The final-revelation trade is attempted against the generic
  ///   OracleFinalRevelation plus every per-mystery final (the
  ///   archetype-removal no-op trick: entries not in the class
  ///   progression warn and are skipped at load - the [removals] log
  ///   tells the truth at runtime).
  ///
  /// The plane-energy mapping (our adaptation of the tabletop's
  /// GM-discretionary association, each after the plane's natives):
  /// Heaven - electricity (the archons); Hell - fire (the devils);
  /// the Abyss - cold (the demons); the Maelstrom - acid (the
  /// proteans).
  /// Log prefix: [planar].
  /// </summary>
  internal static class PlanarOracle
  {
    internal const string ArchetypeName = "PlanarOracleArchetype";

    /// <summary>
    /// The four planes: marker feature, the three hidden tier features,
    /// and the plane's energy. Hidden features are granted by the
    /// attunement component (below) when the tier feature activates.
    /// </summary>
    internal static readonly (string Marker, string Res10, string Res20, string Immunity,
      DamageEnergyType Energy)[] Planes =
    {
      (Guids.PlanarHeavenFeature, Guids.PlanarHeavenRes10, Guids.PlanarHeavenRes20,
        Guids.PlanarHeavenImmunity, DamageEnergyType.Electricity),
      (Guids.PlanarHellFeature, Guids.PlanarHellRes10, Guids.PlanarHellRes20,
        Guids.PlanarHellImmunity, DamageEnergyType.Fire),
      (Guids.PlanarAbyssFeature, Guids.PlanarAbyssRes10, Guids.PlanarAbyssRes20,
        Guids.PlanarAbyssImmunity, DamageEnergyType.Cold),
      (Guids.PlanarMaelstromFeature, Guids.PlanarMaelstromRes10, Guids.PlanarMaelstromRes20,
        Guids.PlanarMaelstromImmunity, DamageEnergyType.Acid),
    };

    /// <summary>
    /// The planar pilgrim's spells: class level, our blueprint guid, the
    /// WOTR substitute, and the tabletop original it stands in for.
    /// Substitutes are native-level-matched (e.g. Banishment is a 7th-
    /// level spell granted at the 14th planar slot, exactly as the
    /// tabletop's shadow walk would be).
    ///
    /// 0.53.0 FIX — the Spell column used to hold the spell's ASSET NAME
    /// ("ProtectionFromEvil"). `BlueprintTool.Get<T>(nameOrGuid)` in
    /// BlueprintCore 2.8.x calls `Guid.Parse` on the string, so every one
    /// of these threw FormatException("Guid should contain 32 digits...")
    /// and killed Configure() at the first entry — which is why neither
    /// oracle archetype reached the game (in-game log 0.52.1:
    /// "Failed to configure feat: PlanarOracle", inner frame
    /// BlueprintTool.Get[T] <- PlanarOracle.Configure). The column now
    /// holds the resolved guid via AbilityRefs, the same
    /// `.ToString()`-yields-guid idiom Doomsayer and SanguineFont use.
    /// </summary>
    internal static readonly (int Level, string Guid, string Spell,
      string TabletopOriginal)[] BonusSpells =
    {
      (2, Guids.PlanarBonusSpell2, AbilityRefs.ProtectionFromEvil.ToString(), "endure elements"),
      (4, Guids.PlanarBonusSpell4, AbilityRefs.SeeInvisibility.ToString(), "elemental speech"),
      (6, Guids.PlanarBonusSpell6, AbilityRefs.ProtectionFromEnergy.ToString(), "tongues"),
      (8, Guids.PlanarBonusSpell8, AbilityRefs.FreedomOfMovement.ToString(), "planar adaptation"),
      (10, Guids.PlanarBonusSpell10, AbilityRefs.Dismissal.ToString(), "plane shift"),
      (12, Guids.PlanarBonusSpell12, AbilityRefs.ChainLightning.ToString(), "mass planar adaptation"),
      (14, Guids.PlanarBonusSpell14, AbilityRefs.Banishment.ToString(), "shadow walk"),
      (16, Guids.PlanarBonusSpell16, AbilityRefs.MindBlank.ToString(), "etherealness"),
      (18, Guids.PlanarBonusSpell18, AbilityRefs.ElementalSwarm.ToString(), "gate"),
    };

    public static void Configure()
    {
      var oracle = CharacterClassRefs.OracleClass.Reference.Get();

      // ----- The hidden tier features: resistances and immunities -----
      foreach (var plane in Planes)
      {
        FeatureConfigurator.New("PlanarHiddenRes10" + plane.Energy, plane.Res10)
          .SetIsClassFeature()
          .AddDamageResistanceEnergy(type: plane.Energy, value: ContextValues.Constant(10))
          .Configure();
        FeatureConfigurator.New("PlanarHiddenRes20" + plane.Energy, plane.Res20)
          .SetIsClassFeature()
          .AddDamageResistanceEnergy(type: plane.Energy, value: ContextValues.Constant(10))
          .Configure();
        FeatureConfigurator.New("PlanarHiddenImmunity" + plane.Energy, plane.Immunity)
          .SetIsClassFeature()
          .AddEnergyImmunity(type: plane.Energy)
          .Configure();
      }

      // ----- The four planes (1st-level selection options) -----
      var heaven = FeatureConfigurator.New("PlanarHeavenFeature", Guids.PlanarHeavenFeature)
        .SetDisplayName("PlanarHeaven.Name")
        .SetDescription("PlanarHeaven.Description")
        .SetIcon(AbilityRefs.Bless.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();
      var hell = FeatureConfigurator.New("PlanarHellFeature", Guids.PlanarHellFeature)
        .SetDisplayName("PlanarHell.Name")
        .SetDescription("PlanarHell.Description")
        .SetIcon(AbilityRefs.HorridWilting.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();
      var abyss = FeatureConfigurator.New("PlanarAbyssFeature", Guids.PlanarAbyssFeature)
        .SetDisplayName("PlanarAbyss.Name")
        .SetDescription("PlanarAbyss.Description")
        .SetIcon(AbilityRefs.EnergyDrain.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();
      var maelstrom = FeatureConfigurator.New("PlanarMaelstromFeature", Guids.PlanarMaelstromFeature)
        .SetDisplayName("PlanarMaelstrom.Name")
        .SetDescription("PlanarMaelstrom.Description")
        .SetIcon(AbilityRefs.ProtectionFromEnergy.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      var planeRefs = new System.Collections.Generic.List<Blueprint<BlueprintFeatureReference>>();
      foreach (var marker in new[] { heaven, hell, abyss, maelstrom })
      {
        planeRefs.Add(marker);
      }
      var selection = FeatureSelectionConfigurator.New(
        "PlanarPlaneSelection", Guids.PlanarPlaneSelection)
        .SetDisplayName("PlanarPlaneSelection.Name")
        .SetDescription("PlanarPlaneSelection.Description")
        .SetIcon(AbilityRefs.ProtectionFromEnergy.Reference.Get().Icon)
        .SetObligatory(true)
        .SetAllFeatures(planeRefs.ToArray())
        .Configure();

      // ----- Planar Resistance (3rd) and its surge (11th) -----
      var resistance = FeatureConfigurator.New(
        "PlanarResistanceFeature", Guids.PlanarResistanceFeature)
        .SetDisplayName("PlanarResistance.Name")
        .SetDescription("PlanarResistance.Description")
        .SetIcon(AbilityRefs.ProtectionFromEnergy.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new PlanarAttunement { Tier = 1 })
        .Configure();
      var surge = FeatureConfigurator.New(
        "PlanarResistanceSurgeFeature", Guids.PlanarResistanceSurgeFeature)
        .SetDisplayName("PlanarResistanceSurge.Name")
        .SetDescription("PlanarResistanceSurge.Description")
        .SetIcon(AbilityRefs.ProtectionFromEnergy.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new PlanarAttunement { Tier = 2 })
        .Configure();

      // ----- Apotheosis (20th) -----
      var apotheosis = FeatureConfigurator.New(
        "PlanarApotheosisFeature", Guids.PlanarApotheosisFeature)
        .SetDisplayName("PlanarApotheosis.Name")
        .SetDescription("PlanarApotheosis.Description")
        .SetIcon(AbilityRefs.EnergyDrain.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new PlanarAttunement { Tier = 3 })
        // DR 10/magic (the tabletop's outsider gift).
        .AddDamageResistancePhysical(bypassedByMagic: true, value: ContextValues.Constant(10))
        .Configure();

      // ----- The planar pilgrim's bonus spells (2nd through 18th) -----
      foreach (var entry in BonusSpells)
      {
        var spell = BlueprintTool.Get<BlueprintAbility>(entry.Spell);
        if (spell is null)
        {
          MissionFeats.Logger.Warn(
            $"[planar] bonus spell {entry.Spell} (for {entry.TabletopOriginal}) missing - skipped.");
          continue;
        }
        FeatureConfigurator.New("PlanarOracleBonusSpell" + entry.Level, entry.Guid)
          .SetDisplayName("PlanarBonusSpell" + entry.Level + ".Name")
          .SetDescription("PlanarBonusSpell.Description")
          .SetIcon(spell.Icon)
          .SetIsClassFeature()
          .AddKnownSpell(characterClass: oracle, spell: spell, spellLevel: entry.Level / 2)
          .Configure();
      }

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.PlanarOracleArchetype, CharacterClassRefs.OracleClass)
          .SetLocalizedName("PlanarOracle.Name")
          .SetLocalizedDescription("PlanarOracle.Description")
          .AddToAddFeatures(LevelPlan.L(1), selection)
          .AddToAddFeatures(LevelPlan.L(3), resistance)
          .AddToAddFeatures(LevelPlan.L(11), surge)
          .AddToAddFeatures(LevelPlan.L(20), apotheosis);
      foreach (var entry in BonusSpells)
      {
        var bonus = BlueprintTool.Get<BlueprintFeature>(entry.Guid);
        if (bonus is null)
        {
          continue; // its spell was missing - the feature was never built
        }
        archetype = archetype.AddToAddFeatures(LevelPlan.L(entry.Level), bonus);
      }

      // The trades: the 3rd revelation (tabletop) plus the 7th (our
      // documented compensation for the additive spells)...
      archetype = ArchetypeRemovals.RemoveAtLevel(
        archetype, oracle.Progression, 3, "OracleRevelationSelection");
      archetype = ArchetypeRemovals.RemoveAtLevel(
        archetype, oracle.Progression, 7, "OracleRevelationSelection");
      // ...and the final revelation (attempted against the generic and
      // every per-mystery final - unchosen entries are no-ops).
      archetype = ArchetypeRemovals.AddRemovals(archetype, oracle,
        FeatureRefs.OracleFinalRevelation.ToString(),
        FeatureRefs.OracleAncestorFinalRevelation.ToString(),
        FeatureRefs.OracleBattleFinalRevelation.ToString(),
        FeatureRefs.OracleBonesFinalRevelation.ToString(),
        FeatureRefs.OracleFlameFinalRevelation.ToString(),
        FeatureRefs.OracleLifeFinalRevelation.ToString(),
        FeatureRefs.OracleNatureFinalRevelation.ToString(),
        FeatureRefs.OracleStoneFinalRevelation.ToString(),
        FeatureRefs.OracleWavesFinalRevelation.ToString(),
        FeatureRefs.OracleWindFinalRevelation.ToString());

      archetype.Configure();

      MissionFeats.Logger.Info("PlanarOracle: configured.");
    }
  }

  /// <summary>
  /// The attunement bridge: when a tier feature (3rd resistance, 11th
  /// surge, 20th immunity) activates, it finds the owner's chosen
  /// plane marker and grants that plane's hidden feature for its tier;
  /// when the tier feature is lost, the hidden feature goes with it
  /// (the StormcallerSwiftGrant add/remove pattern).
  /// </summary>
  [TypeId(Guids.PlanarAttunementComponent)]
  internal class PlanarAttunement : UnitFactComponentDelegate
  {
    /// <summary>1 = resistance 10, 2 = +10 more, 3 = immunity.</summary>
    public int Tier;

    protected override void OnActivate()
    {
      try
      {
        var granted = HiddenFor(out var plane);
        if (granted is not null && Owner.GetFact(granted) is null)
        {
          Owner.AddFact(granted);
          MissionFeats.Logger.Info(
            $"[planar] tier {Tier} attunement granted ({plane}).");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[planar] attunement failed.", e);
      }
    }

    protected override void OnDeactivate()
    {
      try
      {
        var granted = HiddenFor(out _);
        if (granted is not null && Owner.GetFact(granted) is { } fact)
        {
          Owner.RemoveFact(fact);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[planar] attunement removal failed.", e);
      }
    }

    private BlueprintFeature HiddenFor(out string planeName)
    {
      foreach (var plane in PlanarOracle.Planes)
      {
        var marker = BlueprintTool.Get<BlueprintFeature>(plane.Marker);
        if (marker is not null && Owner.HasFact(marker))
        {
          planeName = plane.Energy.ToString();
          return BlueprintTool.Get<BlueprintFeature>(
            Tier == 1 ? plane.Res10 : Tier == 2 ? plane.Res20 : plane.Immunity);
        }
      }
      planeName = "no plane";
      return null;
    }
  }
}

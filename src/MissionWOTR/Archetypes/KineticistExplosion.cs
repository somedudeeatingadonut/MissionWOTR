using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Explosion infusion (0.18.0) - the tabletop's 5th-level form infusion,
  /// missing from WOTR (the mod's first kineticist class content; user
  /// scope: "expanding the existing elements, more choices to work with,"
  /// with Extreme Range and Kinetic Whip skipped by request and Impale
  /// deferred - see the notes below).
  ///
  /// Your kinetic blast detonates in a 20-foot radius, hitting every enemy
  /// in the area. Accepts 3 points of burn.
  ///
  /// Architecture (all on verified surfaces):
  /// - ONE new ability per blast (17 total), each a full clone of that
  ///   blast's Extended Range sibling - the clone carries the blast's
  ///   entire damage wiring (Con scaling, elemental overflow, gather
  ///   power, burn) untouched. Overrides: the range returns to the base
  ///   blast's (Extended Range's 480-ft reach is this sibling's whole
  ///   point), a 20-ft radius AoE component is added (the same builder
  ///   ConstructCrafter's Fire Blast uses), and the burn cost is set to 3
  ///   via a reflection hunt for the infusion-cost member (logged).
  /// - ONE feature, registered into the vanilla InfusionSelection via
  ///   AddToAllFeatures, prerequisite kineticist level 9 (a 5th-level
  ///   infusion under the tabletop's level = (class level + 1) / 2 slots).
  /// - Gating: the feature's granter component grants only the explosion
  ///   abilities whose base blast the kineticist actually owns (checked
  ///   as a fact), so the bar never shows explosions for blasts she
  ///   cannot throw. Documented edge: blasts gained AFTER learning the
  ///   infusion (Expanded Element) appear on the next area load, when
  ///   facts re-activate and the granter re-syncs.
  ///
  /// Sibling research notes (why THIS infusion shipped first):
  /// - Composites: vanilla already ships all eleven tabletop composites
  ///   for the four base elements - nothing to add there.
  /// - Metakinesis: Empower/Maximize/Quicken all ship with the game
  ///   (features, buffs and the mythic Master selection) - user's
  ///   correction, verified against the blueprint references.
  /// - Burning infusion: also already shipped (the earlier missing-list
  ///   was wrong about it).
  /// - Impale (4th-level form, 30-ft line, physical blasts): blocked for
  ///   now - bpcore exposes no line-shape builder (only radius), so the
  ///   only inheritable line blueprint family is Torrent, whose
  ///   physical-blast damage halving lives in internals we cannot audit;
  ///   shipping a clone of Torrent would be a redundant ability, not
  ///   Impale. Deferred until a verifiable line surface exists.
  /// Log prefix: [explosion].
  /// </summary>
  internal static class KineticistExplosion
  {
    /// <summary>
    /// Per blast: display token, the base blast ability (owned = the
    /// kineticist can throw it; also the range source), the Extended
    /// Range sibling (the clone source), and the new explosion ability.
    /// </summary>
    internal static readonly (string Blast, string BlastGuid, string SourceGuid, string AbilityGuid)[] Blasts =
    {
      ("Air", "31f668b12011e344aa542aa07ab6c8d9", "cae4cb39eb87a5d47b8ff35fd948dc4f", Guids.ExplosionAirBlastAbility),
      ("Blizzard", "27f582dcef8206142b01e27ad521e6a4", "db6b0fd6a1337814e9d9868b30b1495b", Guids.ExplosionBlizzardBlastAbility),
      ("BlueFlame", "322911b79eabdb64f8b079c7a2d95e68", "cb8c6e1c78e29444285e6fd97d9ef6ee", Guids.ExplosionBlueFlameBlastAbility),
      ("ChargedWater", "40681ea748d98f54ba7f5dc704507f39", "79da95d61c5b2de40a8f82a3b5d88928", Guids.ExplosionChargedWaterBlastAbility),
      ("Cold", "f6d32ecd20ebacb4e964e2ece1c70826", "b6b4836858298a2499ea8f7748fb9511", Guids.ExplosionColdBlastAbility),
      ("Earth", "b28c336c10eb51c4a8ded0258d5742e1", "7d4712812818f094297f7d7920d130b1", Guids.ExplosionEarthBlastAbility),
      ("Electric", "24f26ac07d21a0e4492899085d1302f6", "3af9f0b8c187f1d44874f71685da7678", Guids.ExplosionElectricBlastAbility),
      ("Fire", "7b4f0c9a06db79345b55c39b2d5fb510", "7bc1270b5bb78834192215bc03f161cc", Guids.ExplosionFireBlastAbility),
      ("Ice", "519e36decde7c964d87c2ffe4d3d8459", "0a6c7f854285c834f81bf90eb1421b37", Guids.ExplosionIceBlastAbility),
      ("Magma", "a0f05637428cbca4bab8bc9122b9e3b9", "77ed869ab8012df40b64a68ca5125960", Guids.ExplosionMagmaBlastAbility),
      ("Metal", "665cfd3718c4f284d80538d85a2791c9", "d88c351a3425ee64f80e2fb836a8acf7", Guids.ExplosionMetalBlastAbility),
      ("Mud", "3236a9e26e23b364e8951ee9e92554e8", "12e1aa0f2cc5ca34c80055110190eafe", Guids.ExplosionMudBlastAbility),
      ("Plasma", "a5631955254ae5c4d9cc2d16870448a2", "f238bef4aa0a7514f9f96fb17ec61261", Guids.ExplosionPlasmaBlastAbility),
      ("Sandstorm", "7b8a4a256d4f3dc4d99192bbaabcb307", "8ebb22bc257c01b489a20836a2c71792", Guids.ExplosionSandstormBlastAbility),
      ("Steam", "08eb2ade31670b843879d8841b32d629", "2f37688defd32d740be8bfc21b3b00fe", Guids.ExplosionSteamBlastAbility),
      ("Thunderstorm", "fc432e7a63f5a3545a93118af13bcb89", "ae0fbfd4d646d34439512b44f9d9ffd5", Guids.ExplosionThunderstormBlastAbility),
      ("Water", "e3f41966c2d662a4e9582a0497621c46", "11eba1184c7108846a665d8ca317963f", Guids.ExplosionWaterBlastAbility),
    };

    public static void Configure()
    {
      var kineticist = CharacterClassRefs.KineticistClass.Reference.Get();
      var icon = AbilityRefs.DetonationFireBlastAbility.Reference.Get().Icon;

      foreach (var entry in Blasts)
      {
        var source = BlueprintTool.Get<BlueprintAbility>(entry.SourceGuid);
        var baseBlast = BlueprintTool.Get<BlueprintAbility>(entry.BlastGuid);
        if (source is null || baseBlast is null)
        {
          MissionFeats.Logger.Warn(
            $"[explosion] {entry.Blast}: source or base blast blueprint missing - skipped.");
          continue;
        }

        // Full clone of the Extended Range sibling (fields + components),
        // then the infusion's own shape on top.
        var ability = CloneAbility(
          "Explosion" + entry.Blast + "BlastAbility", entry.AbilityGuid, source);
        AbilityConfigurator.For(ability.name)
          .SetDisplayName("Explosion" + entry.Blast + ".Name")
          .SetDescription("ExplosionInfusion.Description")
          .SetIcon(icon)
          // Back to the base blast's range: the 480-ft reach is the
          // sibling's feature, not ours.
          .SetRange(baseBlast.Range)
          // 20-ft radius, enemies only - the same builder the Construct
          // Crafter's Fire Blast uses.
          .AddAbilityAoERadius(
            diameterInCells: 4,
            targetType: Kingmaker.UnitLogic.Abilities.Components.TargetType.Enemy)
          .Configure();
        SetBurn(ability, 3);
      }

      var feature = FeatureConfigurator.New("ExplosionInfusionFeature", Guids.ExplosionInfusionFeature)
        .SetDisplayName("ExplosionInfusion.Name")
        .SetDescription("ExplosionInfusion.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        // A 5th-level infusion: the tabletop's infusion slots run
        // level = (class level + 1) / 2, so 5th-level infusions open at
        // kineticist 9.
        .AddPrerequisiteClassLevel(kineticist, 9)
        .AddComponent(new ExplosionGrantComponent())
        .Configure();

      // Register into the vanilla InfusionSelection.
      FeatureSelectionConfigurator.For(FeatureSelectionRefs.InfusionSelection.ToString())
        .AddToAllFeatures(feature)
        .Configure();

      MissionFeats.Logger.Info("KineticistExplosion: configured.");
    }

    /// <summary>
    /// Clone recipe straight from the Construct Crafter's units: bpcore's
    /// CopyFrom with an all-matcher brings the components, and a
    /// reflection field copy brings everything CopyFrom cannot touch.
    /// </summary>
    private static BlueprintAbility CloneAbility(
      string name, string guid, BlueprintAbility source)
    {
      var configurator = AbilityConfigurator.New(name, guid).CopyFrom(source, _ => true);
      var ability = configurator.Configure();

      const System.Reflection.BindingFlags flags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly;
      int copied = 0;
      foreach (var field in typeof(BlueprintAbility).GetFields(flags))
      {
        try
        {
          field.SetValue(ability, field.GetValue(source));
          copied++;
        }
        catch
        {
          // Init-only or compiler-generated members are skipped.
        }
      }
      MissionFeats.Logger.Info(
        $"[explosion] {name} cloned from {source.name}: {copied} fields.");
      return ability;
    }

    /// <summary>
    /// Sets the infusion's burn cost on a cloned ability. The cost lives
    /// inside one of the infusion's components, whose exact type this
    /// build's references do not name - so the setter hunts for an int
    /// member with "Burn" in its name and assigns it, logging the hit (a
    /// miss is logged loudly and leaves the sibling's cost in place -
    /// documented, not faked).
    /// </summary>
    private static void SetBurn(BlueprintAbility ability, int burn)
    {
      foreach (var component in ability.Components)
      {
        var type = component.GetType();
        foreach (var field in type.GetFields())
        {
          if (field.FieldType == typeof(int) && field.Name.Contains("Burn"))
          {
            field.SetValue(component, burn);
            MissionFeats.Logger.Info(
              $"[explosion] {ability.name}: burn set to {burn} via {type.Name}.{field.Name}.");
            return;
          }
        }
        foreach (var property in type.GetProperties())
        {
          if (property.PropertyType == typeof(int) && property.Name.Contains("Burn") &&
            property.CanWrite)
          {
            property.SetValue(component, burn);
            MissionFeats.Logger.Info(
              $"[explosion] {ability.name}: burn set to {burn} via {type.Name}.{property.Name}.");
            return;
          }
        }
      }
      MissionFeats.Logger.Warn(
        $"[explosion] {ability.name}: no burn member found - the Extended " +
        "Range sibling's cost remains. Report if the in-game cost reads 1.");
    }
  }

  /// <summary>
  /// Grants the explosion abilities the kineticist has earned: one per
  /// OWNED base blast (the blast abilities are facts - ownership is a
  /// HasFact check). On save loads and area transitions facts deactivate
  /// and re-activate, which re-syncs the set - the one documented edge is
  /// a blast learned after the infusion, which appears after the next
  /// such transition.
  /// </summary>
  [TypeId(Guids.ExplosionGrantComponent)]
  internal class ExplosionGrantComponent : UnitFactComponentDelegate
  {
    protected override void OnActivate()
    {
      try
      {
        foreach (var entry in KineticistExplosion.Blasts)
        {
          var blast = BlueprintTool.Get<BlueprintAbility>(entry.BlastGuid);
          var ability = BlueprintTool.Get<BlueprintAbility>(entry.AbilityGuid);
          if (blast is null || ability is null)
          {
            continue;
          }
          if (Owner.HasFact(blast) && Owner.GetFact(ability) is null)
          {
            Owner.AddFact(ability);
            MissionFeats.Logger.Info(
              $"[explosion] granted {ability.name} (blast owned).");
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[explosion] grant failed.", e);
      }
    }

    protected override void OnDeactivate()
    {
      try
      {
        foreach (var entry in KineticistExplosion.Blasts)
        {
          var ability = BlueprintTool.Get<BlueprintAbility>(entry.AbilityGuid);
          if (ability is null)
          {
            continue;
          }
          var fact = Owner.GetFact(ability);
          if (fact is not null)
          {
            Owner.RemoveFact(fact);
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[explosion] removal failed.", e);
      }
    }
  }
}

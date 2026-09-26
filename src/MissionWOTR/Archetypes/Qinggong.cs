using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using MissionWOTR.Feats;
using System.Collections.Generic;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Qinggong Monk (faithful port of the APG monk archetype - THE ki-power
  /// archetype: the monk who trades every fixed gift of her order for
  /// choice, learning to channel her ki in whatever shape the moment needs).
  ///
  /// WOTR context: the base monk already carries Owlcat's selectable ki
  /// powers (the MonkKiPowerSelection - true strike, restoration, barkskin,
  /// abundant step, diamond body and soul, cold ice strike, wholeness of
  /// body...). The qinggong port rides that system:
  /// - Trades (real removals against the live progression): still mind,
  ///   fast movement and purity of body - the fixed gifts - become three
  ///   EXTRA ki power selections at 3rd, 4th and 5th. Everything later
  ///   follows the vanilla ki cadence; the qinggong simply chooses instead
  ///   of receiving, and chooses EARLY.
  /// - The shared vanilla selection is extended with four NEW ki powers,
  ///   built the way Owlcat builds its own (spell clones that spend ki -
  ///   the KiTrueStrike / KiRestoration pattern): ki invisibility (2 ki,
  ///   8th), ki neutralize poison (2 ki, 8th), ki freedom of movement
  ///   (2 ki, 10th) and ki holy aura (4 ki, 16th). Every monk in the game
  ///   gains access to them - the user's "add more ki powers" - and the
  ///   qinggong reaches them first.
  ///
  /// Tabletop deviations, documented: the tabletop's per-level trade list
  /// (slow fall, high jump, timeless body, tongue of sun and moon, empty
  /// body) maps poorly because WOTR's monk never had those features - its
  /// ki selection already swallowed most of the tabletop's qinggong list
  /// (abundant step, diamond body, diamond soul, wholeness of body are
  /// vanilla ki powers here). Still mind, fast movement and purity of body
  /// are the fixed features that remain to trade.
  /// Log prefix: [removals] carries the trade diagnostics; [qinggong] the rest.
  /// </summary>
  internal static class Qinggong
  {
    internal const string ArchetypeName = "QinggongArchetype";

    private static readonly (string Name, string AbilityGuid, string FeatureGuid,
      Blueprint<BlueprintReference<BlueprintAbility>> Spell, int Cost, int MinLevel)[] Powers =
    {
      ("Invisibility", "83330253-DD5D-4E02-A143-2F1EC6D0E1A8",
        "E581EEEF-4CE0-42B5-8B04-3BA15E25E29F", AbilityRefs.Invisibility, 2, 8),
      ("NeutralizePoison", "B7EEB509-4443-4111-9422-F465764C02FD",
        "8E91B2FF-8098-4C0C-8EEB-F7FCB880103E", AbilityRefs.NeutralizePoison, 2, 8),
      ("FreedomOfMovement", "D6F1F2E8-F3CD-4D69-92F4-7E0F14C7C238",
        "828EB2E1-61F3-48C4-BDFA-B642DBF57853", AbilityRefs.FreedomOfMovement, 2, 10),
      ("HolyAura", "CF8F656D-97E4-49DE-9C4C-73D5BFBD3CA6",
        "D9C85A4C-5C48-4490-BE48-01347709A83B", AbilityRefs.HolyAura, 4, 16),
    };

    public static void Configure()
    {
      var monk = CharacterClassRefs.MonkClass.Reference.Get();

      // ----- The new ki powers (Owlcat's own spell-clone pattern) -----
      var powerFeatures = new List<BlueprintFeature>();
      foreach (var power in Powers)
      {
        var spell = power.Spell.Reference.Get();
        var ability = AbilityConfigurator.New("Qinggong" + power.Name, power.AbilityGuid)
          .CopyFrom(spell, component => !(component is SpellListComponent))
          .AddAbilityResourceLogic(
            requiredResource: AbilityResourceRefs.KiPowerResource.Reference.Get(),
            amount: power.Cost,
            costIsCustom: true,
            isSpendResource: true)
          .Configure();
        var feature = FeatureConfigurator.New("Qinggong" + power.Name + "Feature", power.FeatureGuid)
          .SetDisplayName("Qinggong" + power.Name + ".Name")
          .SetDescription("Qinggong" + power.Name + ".Description")
          .SetIcon(spell.Icon)
          .SetIsClassFeature()
          .AddFacts(new() { ability })
          .AddPrerequisiteClassLevel(CharacterClassRefs.MonkClass.Reference.Get(), power.MinLevel)
          .Configure();
        powerFeatures.Add(feature);
        MissionFeats.Logger.Info(
          $"[qinggong] ki power: {power.Name} ({power.Cost} ki, monk {power.MinLevel}+).");
      }

      // Extend the SHARED vanilla selection - every monk gains the powers;
      // the qinggong simply reaches them first with her extra picks.
      var selection = FeatureSelectionConfigurator.For(FeatureSelectionRefs.MonkKiPowerSelection);
      foreach (var feature in powerFeatures)
      {
        selection = selection.AddToAllFeatures(feature);
      }
      selection.Configure();

      // ----- Archetype: the fixed gifts become chosen ones -----
      var kiSelection = FeatureSelectionRefs.MonkKiPowerSelection.Reference.Get();
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.QinggongArchetype, CharacterClassRefs.MonkClass)
          .SetLocalizedName("Qinggong.Name")
          .SetLocalizedDescription("Qinggong.Description")
          .AddToAddFeatures(LevelPlan.L(3), kiSelection)
          .AddToAddFeatures(LevelPlan.L(4), kiSelection)
          .AddToAddFeatures(LevelPlan.L(5), kiSelection);

      archetype = ArchetypeRemovals.AddRemovals(
        archetype, monk,
        "b8933d223d87087418a627e61ea42ae6", // Still Mind
        "ac18d5741dfb2f541b222e3a416c5942", // Monk Fast Movement
        "9b02f77c96d6bba4daf9043eff876c76"); // Purity of Body

      archetype.Configure();

      MissionFeats.Logger.Info("Qinggong: configured.");
    }
  }
}

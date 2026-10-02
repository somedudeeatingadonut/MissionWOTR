using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// The Apocryphal (original homebrew oracle archetype - 0.24.0 as the
  /// Blood-Scribed; RENAMED 0.25.0 per the user: "I dont like the
  /// flavor of the blood-scribed (generally you make many things
  /// blood-something, its a bit strange)" - the mechanics and every
  /// blueprint guid are unchanged; only names and prose moved).
  /// The user's brief: "an oracle focused on more offensive casting
  /// (gaining a spell from the wizard spell book every level) in
  /// exchange for doing damage to themselves, or strengthening their
  /// curses."
  ///
  /// The chosen price is the first reading - damage to themselves.
  /// Her scripture is not canon: every spell she copies from the
  /// wizard's book is a page of apocrypha, and every page is paid for
  /// in years of her own life. Each theft permanently costs 2 maximum
  /// hit points (the price rides the stolen spell itself, so it can
  /// never be dispelled, healed, or bargained away - only paid).
  /// Twenty spells by 20th level is forty hit points of book. The
  /// curse reading was the offered alternative and is declined,
  /// documented here: the oracle curse is her god's wound, and the
  /// theft is hers.
  ///
  /// Coverage check: no offensive-casting or wizard-spell-stealing
  /// oracle exists - vanilla (Seeker, Dual-Cursed, Enlightened
  /// Philosopher, Possessed, Divine Herbalist), the content mods (none
  /// add oracle archetypes), or this mod. The tabletop Ancient
  /// Lorekeeper (elf-only, wizard spells for bonus spells) is not in
  /// WOTR; this is the user's own design, not a port of it.
  ///
  /// The kit:
  /// - The Stolen Grimoire (1st, and every oracle level thereafter):
  ///   at EVERY level (1 through 20) she copies ONE wizard spell of a
  ///   level she can cast into her spells known - cast as oracle
  ///   spells, with her own magic (Charisma, her own slots). The
  ///   options are read live from the vanilla WizardSpellList at
  ///   configure time (cantrips included), so the grimoire offers
  ///   exactly what the wizard's book holds.
  /// - The price: each stolen spell feature carries its own permanent
  ///   cost - 2 maximum hit points, untyped and stacking (the
  ///   Stats.HitPoints modifier stat, the Solipsist precedent, as an
  ///   AddStatBonus on the feature).
  ///
  /// The trades (hefty): ALL FIVE revelations (3/7/11/15/19). Her
  /// mystery still answers her prayers - spells, curse, bonus spells,
  /// final revelation - but its wonders are not hers to invoke; she
  /// writes her own.
  ///
  /// Honesty notes (documented, not faked):
  /// - A stolen pick that duplicates a spell the oracle could already
  ///   know is a wasted pick (the wizard and oracle lists barely
  ///   overlap; the player's discretion is the guard).
  /// - The spell-level gate is the oracle's own casting (an
  ///   AddPrerequisiteClassSpellLevel on every option), not the
  ///   wizard's.
  /// - Option blueprints are generated in a loop with deterministic
  ///   guids (MD5 of the spell's asset guid), stable across runs and
  ///   saves. The seed string predates the rename and is deliberately
  ///   NOT renamed: the generated guids must stay stable so existing
  ///   characters keep their stolen pages.
  /// Log prefix: [apocryphal].
  /// </summary>
  internal static class Apocryphal
  {
    internal const string ArchetypeName = "ApocryphalArchetype";
    internal const string SelectionName = "ApocryphalGrimoireSelection";

    public static void Configure()
    {
      var oracle = CharacterClassRefs.OracleClass.Reference.Get();
      var wizardList = SpellListRefs.WizardSpellList.Reference.Get();

      // ----- The stolen pages: one option per wizard spell -----
      var optionRefs = new List<Blueprint<BlueprintFeatureReference>>();
      int created = 0;
      // 0.53.0 FIX — the wizard's list contains more than one distinct
      // blueprint sharing an asset name, and BPCore keys blueprints by
      // NAME. The second "InflictPainAbility" aborted Configure() with
      // "Duplicate GuidByName. ApocryphalSpellInflictPainAbility ...
      // already exists", so the whole archetype never reached the game
      // (in-game log 0.52.1: "Failed to configure feat: Apocryphal").
      // Names are now de-duplicated; the GUID still derives from the
      // spell's own AssetGuid, so pages already stolen in an existing
      // save keep their identity.
      var usedNames = new HashSet<string>();
      var usedGuids = new HashSet<string>();
      foreach (var levelList in wizardList.SpellsByLevel)
      {
        int level = levelList.SpellLevel;
        // The list holds resolved blueprints, not references.
        foreach (var spell in levelList.Spells)
        {
          if (spell is null)
          {
            continue;
          }
          var pageGuid = StableGuid("bloodscribed:" + spell.AssetGuid);
          if (!usedGuids.Add(pageGuid))
          {
            continue; // the same spell reached us twice - one page is enough
          }
          var pageName = "ApocryphalSpell" + spell.name;
          if (!usedNames.Add(pageName))
          {
            pageName += "_" + pageGuid.Replace("-", "").Substring(0, 8);
            usedNames.Add(pageName);
          }
          var option = FeatureConfigurator.New(pageName, pageGuid)
            // The display key is the humanized spell name - an unregistered
            // key shows as itself, which is exactly the wanted label.
            .SetDisplayName(HumanName(spell.name))
            .SetDescription("ApocryphalSpell.Description")
            .SetIcon(spell.Icon)
            .SetIsClassFeature()
            // The theft: the spell joins her oracle spells known.
            .AddKnownSpell(characterClass: oracle, spell: spell, spellLevel: level)
            // The price: the page is written in her blood, and the scar
            // rides the spell forever (untyped, stacking, permanent).
            .AddStatBonus(
              stat: StatType.HitPoints, value: -2, descriptor: ModifierDescriptor.UntypedStackable);
          if (level > 0)
          {
            // No stealing fireballs before she could cast one herself.
            option = option.AddPrerequisiteClassSpellLevel(oracle, requiredSpellLevel: level);
          }
          optionRefs.Add(option.Configure());
          created++;
        }
      }
      MissionFeats.Logger.Info(
        $"[apocryphal] {created} stolen pages generated from the wizard's book.");

      var grimoire = FeatureSelectionConfigurator.New(SelectionName, Guids.ApocryphalGrimoireSelection)
        .SetDisplayName("ApocryphalGrimoire.Name")
        .SetDescription("ApocryphalGrimoire.Description")
        .SetIcon(AbilityRefs.HorridWilting.Reference.Get().Icon)
        .SetObligatory(true)
        .SetAllFeatures(optionRefs.ToArray())
        .Configure();

      // ----- The archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.ApocryphalArchetype, CharacterClassRefs.OracleClass)
          .SetLocalizedName("Apocryphal.Name")
          .SetLocalizedDescription("Apocryphal.Description");
      // A spell from the wizard's book at EVERY oracle level.
      for (int level = 1; level <= 20; level++)
      {
        archetype = archetype.AddToAddFeatures(LevelPlan.L(level), grimoire);
      }

      // The hefty trade: every revelation of her mystery.
      foreach (var level in new[] { 3, 7, 11, 15, 19 })
      {
        archetype = ArchetypeRemovals.RemoveAtLevel(
          archetype, oracle.Progression, level, "OracleRevelationSelection");
      }

      archetype.Configure();

      MissionFeats.Logger.Info("Apocryphal: configured.");
    }

    /// <summary>
    /// Humanizes an asset name for display ("MagicMissileAbility" ->
    /// "Magic Missile"; the common Ability suffix is dropped). Used as a
    /// loc KEY: an unregistered key displays as itself, which is the
    /// wanted label without 200 hand-written loc entries.
    /// </summary>
    internal static string HumanName(string assetName)
    {
      var trimmed = assetName.EndsWith("Ability", StringComparison.Ordinal)
        ? assetName.Substring(0, assetName.Length - "Ability".Length)
        : assetName;
      var sb = new StringBuilder();
      foreach (var c in trimmed)
      {
        if (char.IsUpper(c) && sb.Length > 0)
        {
          sb.Append(' ');
        }
        sb.Append(c);
      }
      return sb.ToString();
    }

    /// <summary>
    /// A deterministic guid for a generated blueprint: MD5 of the seed,
    /// formatted as a GUID. Stable across runs and saves (generated
    /// blueprints must never re-roll their identity).
    /// </summary>
    internal static string StableGuid(string seed)
    {
      using (var md5 = MD5.Create())
      {
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(seed));
        return $"{ToHex(hash, 0, 4)}-{ToHex(hash, 4, 2)}-{ToHex(hash, 6, 2)}-" +
          $"{ToHex(hash, 8, 2)}-{ToHex(hash, 10, 6)}";
      }
    }

    private static string ToHex(byte[] hash, int start, int count)
    {
      var sb = new StringBuilder();
      for (int i = start; i < start + count; i++)
      {
        sb.Append(hash[i].ToString("X2"));
      }
      return sb.ToString();
    }
  }
}

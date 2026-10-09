using BlueprintCore.Actions.Builder;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.Configurators.Classes.Spells;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MissionWOTR.Feats;
using System;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.42.0 — the Overchanneler (sorcerer homebrew, this mod's own
  /// design; the user asked for "a homebrew one of your own").
  ///
  /// Some sorcerers let the blood carry them. The overchanneler carries
  /// the blood: she opens the channel wider than her body was meant to
  /// bear, and every spell thrown through the open channel burns her as
  /// it leaves.
  ///
  /// THE TRADE: one fewer spell slot per day at every spell level (a
  /// cloned sorcerer spellbook with a taxed per-day table). Endurance
  /// for peaks - the session's design language (the Crescendo's ramp,
  /// the Spirit-Ridden's -2), now in caster form.
  ///
  /// THE GAIN:
  /// - Overchannel (1st): a swift action, at will, opening the channel
  ///   for 1 round. While open: +2 caster level (4 at 9th, 6 at 17th -
  ///   the AddCasterLevel component, the one the Aeon ascension buff
  ///   uses) and her spells carry a flat +2 bonus damage (+4 at 9th,
  ///   +6 at 17th - the 0.43.0 heavy nerf; no spell-level multiplier).
  ///   The price: the moment a spell leaves the open channel, she takes
  ///   3 damage per spell level.
  /// - Blood Clot (9th): the backlash can no longer drop her below 1 HP.
  /// - Apex of the Channel (17th): once per day, the channel opens to
  ///   its apex - +8 caster level, +8 flat bonus damage, and no blood
  ///   price at all.
  ///
  /// ENGINE NOTES:
  /// - The spellbook clone: all fields reflection-copied from the vanilla
  ///   sorcerer book (the ConstructCrafter reflection idiom; the probe
  ///   dumped BlueprintSpellbook's full field list), except the per-day
  ///   table, which is a fresh BlueprintSpellsTable with one fewer slot
  ///   per level (min 1). Known spells, slots, list, attribute, and
  ///   spontaneous-ness are untouched.
  /// - The rider: one component on each channel buff. It watches
  ///   RuleCastSpell (the CovertMage idiom) to record the spell level
  ///   and deal the backlash, and RulePrepareDamage (the SanguineFont
  ///   initiator idiom) to add the bonus damage to her spell damage.
  /// </summary>
  internal class Overchanneler
  {
    internal const string ArchetypeName = "Overchanneler";

    internal static void Configure()
    {
      var sorcerer = CharacterClassRefs.SorcererClass.Reference.Get();
      var source = SpellbookRefs.SorcererSpellbook.Reference.Get();
      var icon = AbilityRefs.Fireball.Reference.Get().Icon;

      // ----- The trade: the taxed spellbook. -----
      var book = CloneSpellbookWithPerDayTax(
        source, "MissionWOTR.OverchannelerSpellbook", Guids.OverchannelerSpellbook,
        "MissionWOTR.OverchannelerPerDayTable", Guids.OverchannelerPerDayTable, -1);

      // ----- Blood Clot (granted at 9th): the riders check for it. -----
      var bloodClot = FeatureConfigurator.New(
        "OverchannelerBloodClotFeature", Guids.OverchannelerBloodClotFeature)
        .SetDisplayName("OverchannelBloodClot.Name")
        .SetDescription("OverchannelBloodClot.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .Configure();

      // ----- The channel buffs: three tiers + the apex. -----
      var tierBuffs = new BlueprintBuff[3];
      var tierGuids = new[]
      {
        Guids.OverchannelerBuff1, Guids.OverchannelerBuff2, Guids.OverchannelerBuff3,
      };
      for (var i = 0; i < 3; i++)
      {
        var tier = 2 * (i + 1);
        tierBuffs[i] = BuffConfigurator.New("OverchannelerBuff" + (i + 1), tierGuids[i])
          .SetDisplayName("OverchannelBuff.Name")
          .SetDescription("OverchannelBuff.Description")
          .SetIcon(icon)
          .SetIsClassFeature()
          .AddCasterLevel(bonus: tier, descriptor: ModifierDescriptor.UntypedStackable)
          .AddComponent(new OverchannelerRider
          {
            Tier = tier,
            BloodClotFeature = bloodClot,
          })
          .Configure();
      }
      var apexBuff = BuffConfigurator.New("OverchannelerApexBuff", Guids.OverchannelerApexBuff)
        .SetDisplayName("OverchannelApexBuff.Name")
        .SetDescription("OverchannelApexBuff.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddCasterLevel(bonus: 8, descriptor: ModifierDescriptor.UntypedStackable)
        .AddComponent(new OverchannelerRider
        {
          Tier = 8,
          NoBacklash = true,
          BloodClotFeature = null,
        })
        .Configure();

      // ----- The apex resource (1/day). -----
      var apexResource = AbilityResourceConfigurator.New(
        "OverchannelerResource", Guids.OverchannelerResource)
        .SetMax(1)
        .Configure();

      // ----- Overchannel: the ability. -----
      var apexFeature = FeatureConfigurator.New(
        "OverchannelerApexFeature", Guids.OverchannelerApexFeature)
        .SetDisplayName("OverchannelApex.Name")
        .SetDescription("OverchannelApex.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddAbilityResources(resource: apexResource, restoreAmount: true)
        .Configure();

      var open = new OverchannelerOpenAction
      {
        TierBuffs = tierBuffs,
        ApexBuff = apexBuff,
        ApexResource = apexResource,
        ApexFeature = apexFeature,
        SorcererClass = sorcerer,
      };
      var ability = AbilityConfigurator.New("OverchannelerAbility", Guids.OverchannelerAbility)
        .SetDisplayName("Overchannel.Name")
        .SetDescription("Overchannel.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Swift)
        .SetCanTargetSelf()
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(open).Build())
        .Configure();

      // ----- The kit (1st): the ability. -----
      var kit = FeatureConfigurator.New("OverchannelerKitFeature", Guids.OverchannelerKitFeature)
        .SetDisplayName("Overchannel.Name")
        .SetDescription("Overchannel.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { ability })
        .Configure();

      // ----- The archetype. -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.OverchannelerArchetype, CharacterClassRefs.SorcererClass)
          .SetLocalizedName("Overchanneler.Name")
          .SetLocalizedDescription("Overchanneler.Description")
          .SetReplaceSpellbook(Guids.OverchannelerSpellbook)
          .AddToAddFeatures(LevelPlan.L(1), kit)
          .AddToAddFeatures(LevelPlan.L(9), bloodClot)
          .AddToAddFeatures(LevelPlan.L(17), apexFeature);
      archetype.Configure();
      MissionFeats.Logger.Info("[overchanneler] configured: " + ArchetypeName + ".");
    }

    /// <summary>
    /// Clones a spellbook, replacing its spells-per-day table with a
    /// taxed copy (each level's count shifted by delta, minimum 1). All
    /// other fields are reflection-copied from the source (guid/name
    /// excluded). The field list comes from the CI metadata probe's
    /// BlueprintSpellbook dump.
    /// </summary>
    /// <summary>
    /// Clones a spellbook with a per-day tax applied.
    ///
    /// 0.53.0 FIX — this used hard casts and unguarded reflection:
    /// `(BlueprintSpellsTableReference)`, `(SpellsLevelEntry[])` and,
    /// worst, `(int)countField?.GetValue(...)`, which unboxes and throws
    /// InvalidCastException if the field is not exactly an `int`. The
    /// result was "Specified cast is not valid" out of Configure(), so
    /// the Overchanneler never reached the game (in-game log 0.52.1:
    /// "Failed to configure feat: Overchanneler", inner frame
    /// CloneSpellbookWithPerDayTax). Every reflective step is now a soft
    /// `as`/`Convert` with a logged fallback, the field copy skips what
    /// it cannot set (the ElementalObsessor idiom), and the discovered
    /// field types are logged so the next playtest log names the real
    /// shape instead of guessing. If the tax cannot be built the source
    /// book is returned UNtaxed rather than losing the archetype.
    /// </summary>
    private static BlueprintSpellbook CloneSpellbookWithPerDayTax(
      BlueprintSpellbook source,
      string bookName, string bookGuid,
      string tableName, string tableGuid,
      int delta)
    {
      const System.Reflection.BindingFlags flags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance;

      // 1. The taxed per-day table.
      var perDayField = typeof(BlueprintSpellbook).GetField("m_SpellsPerDay", flags);
      if (perDayField is null)
      {
        MissionFeats.Logger.Warn(
          "[overchanneler] no m_SpellsPerDay field on BlueprintSpellbook - using the " +
          "source book untaxed. Fields present: " +
          string.Join(", ", typeof(BlueprintSpellbook).GetFields(flags).Select(f => f.Name)));
        return source;
      }
      var perDayRef = perDayField.GetValue(source) as BlueprintSpellsTableReference;
      var perDay = perDayRef?.Get();
      var levelsField = typeof(BlueprintSpellsTable).GetField("Levels", flags);
      var sourceLevels = levelsField?.GetValue(perDay) as SpellsLevelEntry[];
      if (sourceLevels is null)
      {
        MissionFeats.Logger.Warn(
          "[overchanneler] per-day table shape unexpected (Levels=" +
          (levelsField?.GetValue(perDay)?.GetType().Name ?? "null") +
          ") - using the source book untaxed.");
        return source;
      }
      var levels = new SpellsLevelEntry[sourceLevels.Length];
      for (var i = 0; i < sourceLevels.Length; i++)
      {
        // 0.61.0 — SpellsLevelEntry.Count is `public int[] Count`, one spells-per-day figure per
        // SPELL level, not a single scalar. Every earlier version read it as an int:
        // Convert.ToInt32(int[]) throws "Specified cast is not valid", which is the exact
        // exception in the playtest log and the reason the whole archetype never reached the
        // game. No reflection is needed here - the field is public and typed.
        var entry = new SpellsLevelEntry();
        var sourceCount = sourceLevels[i]?.Count;
        if (sourceCount is not null)
        {
          var taxed = new int[sourceCount.Length];
          for (var s = 0; s < sourceCount.Length; s++)
          {
            // Tax each slot, but never invent one: a level that granted no spells of a given
            // spell level still grants none.
            taxed[s] = sourceCount[s] <= 0 ? 0 : Math.Max(1, sourceCount[s] + delta);
          }
          entry.Count = taxed;
        }
        levels[i] = entry;
      }
      MissionFeats.Logger.Info(
        $"[overchanneler] taxed per-day table built: {levels.Length} caster levels.");
      var table = SpellsTableConfigurator.New(tableName, tableGuid)
        .SetLevels(levels)
        .Configure();

      // 2. The spellbook clone.
      var book = SpellbookConfigurator.New(bookName, bookGuid).Configure();
      foreach (var field in typeof(BlueprintSpellbook).GetFields(flags))
      {
        if (field.Name.Equals("name", StringComparison.OrdinalIgnoreCase) ||
          field.Name.IndexOf("guid", StringComparison.OrdinalIgnoreCase) >= 0)
        {
          continue;
        }
        try
        {
          field.SetValue(book, field.GetValue(source));
        }
        catch
        {
          // Init-only or compiler-generated member - skipped, as ElementalObsessor does.
        }
      }
      try
      {
        perDayField.SetValue(book, table.ToReference<BlueprintSpellsTableReference>());
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error(
          "[overchanneler] per-day tax could not be pinned - book untaxed.", e);
        return source;
      }
      MissionFeats.Logger.Info(
        $"[overchanneler] spellbook cloned: {source.name} -> {bookName} (per-day {delta}).");
      return book;
    }
  }

  /// <summary>
  /// Opens the channel: applies the tier buff her level has earned (+2 at
  /// 1st, +4 at 9th, +6 at 17th) - or, at 17th+ with the daily charge
  /// unspent, the apex channel (no blood price).
  /// </summary>
  [TypeId(Guids.OverchannelerAction)]
  internal class OverchannelerOpenAction : NamedContextAction
  {
    public BlueprintBuff[] TierBuffs;
    public BlueprintBuff ApexBuff;
    public BlueprintAbilityResource ApexResource;
    public BlueprintFeature ApexFeature;
    public BlueprintCharacterClass SorcererClass;

    public override void RunAction()
    {
      try
      {
        var caster = Context?.MaybeCaster;
        if (caster is null)
        {
          return;
        }
        int level = caster.Descriptor.Progression.GetClassLevel(SorcererClass);
        var hasApex = ApexFeature is null ||
          caster.HasFact(ApexFeature);
        if (level >= 17 && hasApex && ApexResource != null &&
          caster.Resources.GetResourceAmount(ApexResource) > 0)
        {
          caster.Resources.Spend(ApexResource, 1);
          caster.AddBuff(ApexBuff, Context, TimeSpan.FromSeconds(6));
          MissionFeats.Logger.Info("[overchanneler] the apex channel opens (no blood price).");
          return;
        }
        var tier = level < 9 ? 0 : level < 17 ? 1 : 2;
        caster.AddBuff(TierBuffs[tier], Context, TimeSpan.FromSeconds(6));
        MissionFeats.Logger.Info($"[overchanneler] the channel opens: +{2 * (tier + 1)}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[overchanneler] could not open the channel.", e);
      }
    }

    public override string GetCaption() => "Overchannel";
  }

  /// <summary>
  /// The open channel's price and its gift, on one rider. Watches
  /// RuleCastSpell (the CovertMage idiom): the moment a real spell leaves
  /// an open channel, she pays 3 damage per spell level (no backlash on the
  /// apex channel; Blood Clot keeps her at 1 HP). Watches
  /// RulePrepareDamage (the SanguineFont initiator idiom): her spell
  /// damage carries the tier bonus per spell level.
  /// </summary>
  [TypeId(Guids.OverchannelerRiderComponent)]
  internal class OverchannelerRider : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCastSpell>, IRulebookHandler<RuleCastSpell>,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public int Tier;
    public bool NoBacklash;
    public BlueprintFeature BloodClotFeature;

    private int m_SpellLevel;

    public void OnEventAboutToTrigger(RuleCastSpell evt) { }

    public void OnEventDidTrigger(RuleCastSpell evt)
    {
      try
      {
        if (evt.Initiator != Owner ||
          evt.Spell?.Blueprint?.Type != AbilityType.Spell)
        {
          return;
        }
        m_SpellLevel = evt.Spell.SpellLevel;
        if (NoBacklash || m_SpellLevel <= 0)
        {
          return;
        }
        // The blood price: 3 damage per spell level, flat and unresistable.
        // Blood Clot (9th) clamps it so it can never drop her below 1 HP -
        // clamped BEFORE the damage rule, so death cannot sneak through.
        var backlash = 3 * m_SpellLevel;
        if (BloodClotFeature is not null && Owner.HasFact(BloodClotFeature))
        {
          backlash = Math.Min(backlash, Math.Max(0, Owner.HPLeft - 1));
        }
        if (backlash > 0)
        {
          var bundle = new DamageBundle();
          bundle.Add(new DirectDamage(DiceFormula.Zero, backlash));
          Rulebook.Trigger(new RuleDealDamage(Owner, Owner, bundle) { Reason = Fact });
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[overchanneler] backlash failed.", e);
      }
    }

    public void OnEventAboutToTrigger(RulePrepareDamage evt) { }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.DamageBundle is null ||
          evt.DamageBundle.Weapon is not null || m_SpellLevel <= 0)
        {
          return;
        }
        // 0.43.0, the user's nerf: "does too much damage to enemies,
        // that should be nerfed heavily." The damage rider no longer
        // multiplies by spell level - it is a FLAT bonus equal to the
        // tier (+2/+4/+6, apex +8) per damage event. The caster-level
        // bonus (the real prize: dice caps, durations, penetration)
        // and the blood price are unchanged.
        var bonus = Tier;
        if (bonus > 0)
        {
          evt.Add(new DirectDamage(DiceFormula.Zero, bonus) { SourceFact = Fact });
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[overchanneler] channel bonus failed.", e);
      }
    }
  }
}

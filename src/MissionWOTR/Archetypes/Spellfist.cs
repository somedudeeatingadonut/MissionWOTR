using BlueprintCore.Blueprints.Configurators.Classes;
using BlueprintCore.Blueprints.Configurators.Classes.Spells;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
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
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Spellfist (homebrew, user-commissioned: the magus/monk fusion the feat batches
  /// were built around - the "class that focuses on using all of them").
  ///
  /// The fantasy: an ascetic battlefield caster who fights with bare, spell-charged
  /// fists. Wiser rather than smarter, armored in nothing but ki and magic.
  ///
  /// - Spellfist Spellbook (1st, archetype ReplaceSpellbook): Wisdom-casting; the
  ///   book contains EVERY touch-range spell from every non-mythic spellbook in the
  ///   game (each at its lowest level anywhere) - the Elemental Fixation pattern.
  /// - Spell Cascade (1st, additive - it re-routes Spell Strike's payload): touch
  ///   spell damage is never dealt as one cast. Instead the delivery's dice are
  ///   captured and zeroed, divided by the number of attacks in the full attack
  ///   (2 + BAB iteratives, capped 5), and each successful unarmed hit pays one
  ///   share of the same dice/energy. A cast outside a full attack dissipates
  ///   (zeroed, warned) - spells only truly land through the fists. Non-damaging
  ///   touch effects apply normally on delivery.
  /// - Bare Fist Discipline (1st, trades proficiencies): Improved Unarmed Strike +
  ///   the monk's 1st-level fist die. No weapon or armor proficiencies remain, so
  ///   manufactured weapons swing at non-proficient penalties.
  /// - Ki Flurry (1st, additive): while unarmored and fighting with monk weapons
  ///   (bare fists count), gains the flurry buff: one extra attack, -2 on attacks,
  ///   and Wisdom to AC (the vanilla MonkNoArmorAndMonkWeaponFeatureUnlock gate,
  ///   the TTT MonkACBonus recipe for the stat bonus).
  /// - Fist dice: a FEW of the monk's upgrades only - 1st/8th/16th (1d6 -> 1d10 ->
  ///   2d8); a 20th-level Spellfist never reaches a 20th-level monk's fists.
  /// - Sundering Blows (3rd): each unarmed hit stacks "Sundered" on the target
  ///   (max ranks scale with level); each rank lets ALL incoming weapon damage
  ///   treat the target's damage reduction as 2 lower (DamageValue.ReductionPenalty,
  ///   the COP Divine Fighting Technique mechanism).
  /// - Casting Carapace (7th): while fists are charged (a spell cast this round),
  ///   gains DR equal to a quarter of class level - casting literally hardens them.
  /// - The Mission WOTR feat line, granted across 20 levels (plus the vanilla
  ///   feats some of them switch on): Resonant Strikes, Arcane Momentum, Tunnel
  ///   Fighter, Vengeful Counterstrike, Guarded Momentum, Taunting Blows, Second
  ///   Wind, Warded Soul, Battlefield Scavenger, Steadfast Aim.
  ///
  /// Kept vanilla features: Spell Combat and Spell Strike (the delivery vehicle
  /// the cascade hijacks), Arcane Pool and the full arcana selection (weapon
  /// enchants apply to whatever is wielded - bare fists included when the game
  /// treats them as the current weapon), cantrips, spell recall, fighter training,
  /// True Magus.
  ///
  /// Implementation notes: split shares are re-emitted as EnergyDamage/DirectDamage
  /// riders keyed to the feature fact; the arm window is a visible 2-round charge
  /// buff (so a standard-action cast can still unload on the NEXT turn's full
  /// attack - holding the charge, monk-style); sunder is a rank-stacking debuff
  /// (GuardedMomentum pattern) whose component injects a ReductionPenalty modifier
  /// during RuleCalculateDamage for every attacker. All numbers are tuning
  /// candidates. Log prefix: [spellfist].
  /// </summary>
  internal static class Spellfist
  {
    internal const string ArchetypeName = "SpellfistArchetype";
    internal const string DisplayName = "Spellfist.Name";
    internal const string Description = "Spellfist.Description";

    internal const string SpellListName = "SpellfistSpellList";
    internal const string SpellbookName = "SpellfistSpellbook";
    internal const string CascadeName = "SpellfistCascade";
    internal const string ChargeBuffName = "SpellfistChargeBuff";
    internal const string BareFistName = "SpellfistBareFist";
    internal const string KiFlurryName = "SpellfistKiFlurry";
    internal const string FlurryBuffName = "SpellfistFlurryBuff";
    internal const string SunderName = "SpellfistSunder";
    internal const string SunderedBuffName = "SpellfistSunderedBuff";
    internal const string CarapaceName = "SpellfistCarapace";
    internal const string CarapaceBuffName = "SpellfistCarapaceBuff";

    /// <summary>Every touch spell found in the game, for delivery matching.</summary>
    internal static BlueprintAbility[] TouchSpells;

    public static void Configure()
    {
      var magus = CharacterClassRefs.MagusClass.Reference.Get();
      var magusBook = SpellbookRefs.MagusSpellbook.Reference.Get();

      // ----- The touch spellbook (Elemental Fixation pattern, Range filter) -----
      var (list, spellLevels) = BuildTouchList(SpellListName, Guids.SpellfistSpellList);
      TouchSpells = spellLevels.Keys.ToArray();
      var book = BuildSpellbook(SpellbookName, Guids.SpellfistSpellbook, magusBook, list);
      MissionFeats.Logger.Info(
        $"[spellfist] touch spellbook built: {spellLevels.Count} spells.");

      // ----- Charge buff: the visible arm window (2 rounds) -----
      var chargeBuff = BuffConfigurator.New(ChargeBuffName, Guids.SpellfistChargeBuff)
        .SetDisplayName("SpellfistCharge.Name")
        .SetDescription("SpellfistCharge.Description")
        .SetIcon(AbilityRefs.ShockingGraspCast.Reference.Get().Icon)
        .Configure();

      // ----- Carapace buff: DR while charged -----
      var carapaceBuff = BuffConfigurator.New(CarapaceBuffName, Guids.SpellfistCarapaceBuff)
        .SetDisplayName("SpellfistCarapaceBuff.Name")
        .SetDescription("SpellfistCarapaceBuff.Description")
        .SetIcon(AbilityRefs.StoneFist.Reference.Get().Icon)
        .AddDamageResistancePhysical(value: ContextValues.Rank())
        .AddContextRankConfig(
          ContextRankConfigs.ClassLevel(new[] { CharacterClassRefs.MagusClass.ToString() })
            .WithDivStepProgression(4))
        .Configure();

      // ----- Flurry buff: extra attack, -2 attacks, Wisdom to AC -----
      var flurryBuff = BuffConfigurator.New(FlurryBuffName, Guids.SpellfistFlurryBuff)
        .SetDisplayName("SpellfistFlurryBuff.Name")
        .SetDescription("SpellfistFlurryBuff.Description")
        .SetIcon(FeatureRefs.FlurryOfBlows.Reference.Get().Icon)
        .AddBuffExtraAttack(number: 1, haste: false, penalized: false)
        .AddStatBonus(stat: StatType.AdditionalAttackBonus, value: -2, descriptor: ModifierDescriptor.UntypedStackable)
        .AddContextStatBonus(StatType.AC, ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
        .AddContextRankConfig(ContextRankConfigs.StatBonus(stat: StatType.Wisdom, min: 0))
        .AddRecalculateOnStatChange(stat: StatType.Wisdom)
        .Configure();

      // ----- Sundered debuff: rank-stacking DR reduction for everyone -----
      var sunderedBuff = BuffConfigurator.New(SunderedBuffName, Guids.SpellfistSunderedBuff)
        .SetDisplayName("SpellfistSundered.Name")
        .SetDescription("SpellfistSundered.Description")
        .SetIcon(AbilityRefs.SunderAction.Reference.Get().Icon)
        .SetStacking(StackingType.Rank)
        .SetRanks(5)
        .Configure();
      BuffConfigurator.For(SunderedBuffName)
        .AddComponent(new SpellfistSunderedWard { SelfBuff = sunderedBuff, AmountPerRank = 2 })
        .Configure();

      // ----- Features -----
      var carapaceFeature = FeatureConfigurator.New(CarapaceName, Guids.SpellfistCarapace)
        .SetDisplayName("SpellfistCarapace.Name")
        .SetDescription("SpellfistCarapace.Description")
        .SetIcon(AbilityRefs.StoneFist.Reference.Get().Icon)
        .SetIsClassFeature()
        .Configure();

      var cascade = FeatureConfigurator.New(CascadeName, Guids.SpellfistCascade)
        .SetDisplayName("SpellfistCascade.Name")
        .SetDescription("SpellfistCascade.Description")
        .SetIcon(AbilityRefs.ShockingGraspCast.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new SpellfistCascade
        {
          CharacterClass = magus,
          Spellbook = book,
          ChargeBuff = chargeBuff,
          CarapaceFeature = carapaceFeature,
          CarapaceBuff = carapaceBuff,
          TouchSpells = TouchSpells,
        })
        .Configure();

      var ius = FeatureRefs.ImprovedUnarmedStrike.Reference.Get();
      var fistDie1 = FeatureRefs.MonkUnarmedStrikeLevel1.Reference.Get();
      var bareFist = FeatureConfigurator.New(BareFistName, Guids.SpellfistBareFist)
        .SetDisplayName("SpellfistBareFist.Name")
        .SetDescription("SpellfistBareFist.Description")
        .SetIcon(ius.Icon)
        .SetIsClassFeature()
        .AddFacts(new() { ius, fistDie1 })
        .Configure();

      var kiFlurry = FeatureConfigurator.New(KiFlurryName, Guids.SpellfistKiFlurry)
        .SetDisplayName("SpellfistKiFlurry.Name")
        .SetDescription("SpellfistKiFlurry.Description")
        .SetIcon(FeatureRefs.FlurryOfBlows.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddMonkNoArmorAndMonkWeaponFeatureUnlock(newFact: flurryBuff)
        .Configure();

      var sunder = FeatureConfigurator.New(SunderName, Guids.SpellfistSunder)
        .SetDisplayName("SpellfistSunder.Name")
        .SetDescription("SpellfistSunder.Description")
        .SetIcon(AbilityRefs.SunderAction.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddComponent(new SpellfistSunderTrigger
        {
          CharacterClass = magus,
          SunderedBuff = sunderedBuff,
        })
        .Configure();

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.SpellfistArchetype, CharacterClassRefs.MagusClass)
          .SetLocalizedName(DisplayName)
          .SetLocalizedDescription(Description)
          // The class's spellbook itself is replaced, so Spell Combat's
          // UnitPartMagus resolves OUR book (the Eldritch Scion mechanism).
          .SetReplaceSpellbook(Guids.SpellfistSpellbook);

      // Trades all weapon/armor proficiencies (plus any later armor training
      // features, if the progression names them - unknown names warn and skip).
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, magus,
        FeatureRefs.MagusProficiencies.ToString(),
        "MagusArmorProficiency",
        "ImprovedMagusArmorProficiency",
        "MagusMediumArmorProficiency",
        "MagusHeavyArmorProficiency");

      archetype
        .AddToAddFeatures(LevelPlan.L(1), CascadeName, BareFistName, KiFlurryName)
        .AddToAddFeatures(LevelPlan.L(2),
          ResonantStrikes.FeatName, FeatureRefs.CombatReflexes.ToString())
        .AddToAddFeatures(LevelPlan.L(3), SunderName)
        .AddToAddFeatures(LevelPlan.L(4), ArcaneMomentum.FeatName)
        .AddToAddFeatures(LevelPlan.L(5), VengefulCounterstrike.FeatName)
        .AddToAddFeatures(LevelPlan.L(6),
          GuardedMomentum.FeatName, FeatureRefs.CombatExpertiseFeature.ToString())
        .AddToAddFeatures(LevelPlan.L(7),
          TauntingBlows.FeatName, FeatureRefs.PowerAttackFeature.ToString(), CarapaceName)
        .AddToAddFeatures(LevelPlan.L(8),
          TunnelFighter.FeatName, FeatureRefs.MonkUnarmedStrikeLevel8.ToString())
        .AddToAddFeatures(LevelPlan.L(9),
          SecondWind.FeatName, FeatureRefs.Endurance.ToString())
        .AddToAddFeatures(LevelPlan.L(11), WardedSoul.FeatName)
        .AddToAddFeatures(LevelPlan.L(13), BattlefieldScavenger.FeatName)
        .AddToAddFeatures(LevelPlan.L(15), SteadfastAim.FeatName)
        .AddToAddFeatures(LevelPlan.L(16), FeatureRefs.MonkUnarmedStrikeLevel16.ToString())
        .Configure(delayed: true);
    }

    /// <summary>
    /// Every touch-range spell from every non-mythic spellbook, each at its
    /// lowest level anywhere (the Elemental Fixation enumerator with a Range
    /// filter instead of a descriptor filter).
    /// </summary>
    private static (BlueprintSpellList list, Dictionary<BlueprintAbility, int> levels)
      BuildTouchList(string name, string guid)
    {
      var minLevel = new Dictionary<BlueprintAbility, int>();
      foreach (var book in ElementalObsessor.AllBlueprints<BlueprintSpellbook>())
      {
        if (book is null || book.IsMythic)
        {
          continue;
        }
        var list = book.SpellList;
        if (list?.SpellsByLevel is null)
        {
          continue;
        }
        foreach (var levelEntry in list.SpellsByLevel)
        {
          if (levelEntry is null)
          {
            continue;
          }
          int level = Math.Max(0, Math.Min(9, levelEntry.SpellLevel));
          foreach (BlueprintAbility spell in levelEntry.Spells)
          {
            if (spell is null || spell.Range != AbilityRange.Touch)
            {
              continue;
            }
            if (!minLevel.TryGetValue(spell, out var current) || level < current)
            {
              minLevel[spell] = level;
            }
          }
        }
      }
      var byLevel = new SpellLevelList[10];
      for (int i = 0; i < 10; i++)
      {
        byLevel[i] = new SpellLevelList(i) { SpellLevel = i };
      }
      foreach (var pair in minLevel)
      {
        ElementalObsessor.AddSpellToEntry(byLevel[Math.Max(0, Math.Min(9, pair.Value))], pair.Key);
      }
      BlueprintTool.Create<BlueprintSpellList>(name, guid);
      var result = BlueprintTool.Get<BlueprintSpellList>(guid);
      result.SpellsByLevel = byLevel;
      return (result, minLevel);
    }

    /// <summary>
    /// Clones the magus spellbook and swaps in the touch list; the casting
    /// attribute is pinned to Wisdom (the Obsessor's INT-pin, different stat).
    /// </summary>
    private static BlueprintSpellbook BuildSpellbook(
      string name, string guid, BlueprintSpellbook source, BlueprintSpellList list)
    {
      var book = SpellbookConfigurator.New(name, guid).Configure();

      const System.Reflection.BindingFlags flags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly;
      foreach (var field in typeof(BlueprintSpellbook).GetFields(flags))
      {
        try
        {
          field.SetValue(book, field.GetValue(source));
        }
        catch
        {
          // Init-only or compiler-generated members are skipped.
        }
      }
      foreach (var field in typeof(BlueprintScriptableObject).GetFields(flags))
      {
        if (field.Name == "m_AssetGuid")
        {
          continue; // blueprint identity must stay its own
        }
        try
        {
          field.SetValue(book, field.GetValue(source));
        }
        catch
        {
          // Skipped.
        }
      }
      try
      {
        book.CastingAttribute = StatType.Wisdom;
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Spellfist: casting attribute pin failed.", e);
      }
      var listField = typeof(BlueprintSpellbook).GetFields(
          System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
          System.Reflection.BindingFlags.NonPublic)
        .FirstOrDefault(f => f.FieldType == typeof(BlueprintSpellListReference));
      if (listField != null)
      {
        listField.SetValue(book, list.ToReference<BlueprintSpellListReference>());
      }
      else
      {
        MissionFeats.Logger.Warn("[spellfist] spellbook list field not found - book uses source list!");
      }
      return book;
    }
  }

  /// <summary>
  /// Spell Cascade: arms on casting a spell from the Spellfist book, captures and
  /// zeroes the spell's delivery damage, banks it as N shares (N = expected
  /// full-attack unarmed hits) and pays one share per successful unarmed hit
  /// while the charge buff lasts. Shares are instance state, so two Spellfists
  /// never share a cascade.
  /// </summary>
  [TypeId(Guids.SpellfistCascadeComponent)]
  internal class SpellfistCascade : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCastSpell>, IRulebookHandler<RuleCastSpell>,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>, IRulebookHandler<RuleAttackWithWeapon>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass CharacterClass;
    public BlueprintSpellbook Spellbook;
    public BlueprintBuff ChargeBuff;
    public BlueprintFeature CarapaceFeature;
    public BlueprintBuff CarapaceBuff;
    public BlueprintAbility[] TouchSpells;

    private class Share
    {
      public int Rolls;
      public DiceType Die;
      public int Flat;
      public DamageEnergyType? Energy;
    }

    private readonly List<Share> Shares = new();
    private HashSet<BlueprintAbility> touchSet;
    private bool warnedDissipate;

    protected override void OnTurnOn()
    {
      touchSet = TouchSpells is null
        ? new HashSet<BlueprintAbility>()
        : new HashSet<BlueprintAbility>(TouchSpells);
    }

    private bool Armed => Shares.Count > 0 && Owner.Buffs.GetBuff(ChargeBuff) != null;

    // ----- Arm on cast -----
    public void OnEventAboutToTrigger(RuleCastSpell evt) { }

    public void OnEventDidTrigger(RuleCastSpell evt)
    {
      try
      {
        if (evt.Initiator != Owner || !evt.Success)
        {
          return;
        }
        if (evt.Spell?.Spellbook?.Blueprint != Spellbook)
        {
          return;
        }
        // Expected unarmed hits in a full attack: flurry extra + iteratives.
        int bab = Owner.Stats.BaseAttackBonus.ModifiedValue;
        int n = 2 + (bab >= 6 ? 1 : 0) + (bab >= 11 ? 1 : 0) + (bab >= 16 ? 1 : 0);
        Shares.Clear();
        // Re-arm the window (2 rounds: the full attack this action, or the next
        // turn's if the spell was cast as a standard action - held charge).
        Owner.AddBuff(ChargeBuff, Context, ContextDuration.Fixed(2).Calculate(Context).Seconds);
        if (CarapaceFeature != null && Owner.HasFact(CarapaceFeature))
        {
          Owner.AddBuff(CarapaceBuff, Context, ContextDuration.Fixed(2).Calculate(Context).Seconds);
        }
        MissionFeats.Logger.Info(
          $"[spellfist] cascade armed: {evt.Spell.Blueprint.name} across {Math.Min(5, n)} attacks.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Spellfist: arm failed.", e);
      }
    }

    // ----- Capture + zero delivery damage, bank shares -----
    public void OnEventAboutToTrigger(RulePrepareDamage evt) { }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.Reason.Fact == Fact)
        {
          return;
        }
        var data = evt.Reason.Ability;
        var bp = data?.Blueprint ?? evt.Reason.Context?.SourceAbility;
        if (data?.Spellbook?.Blueprint != Spellbook
          && (bp is null || !touchSet.Contains(bp)))
        {
          return;
        }
        // Capture the spell's dice before zeroing.
        var captured = new List<Share>();
        foreach (BaseDamage damage in evt.DamageBundle)
        {
          int rolls = damage.Dice.ModifiedValue.Rolls;
          var die = damage.Dice.ModifiedValue.Dice;
          int flat = damage.Bonus + damage.BonusTargetRelated;
          if (rolls <= 0 && flat <= 0)
          {
            continue;
          }
          captured.Add(new Share
          {
            Rolls = rolls,
            Die = die,
            Flat = flat,
            Energy = (damage as EnergyDamage)?.EnergyType,
          });
        }
        // The delivery itself deals nothing - only the fists do (dice modified
        // to zero in place - the DarkCodex KineticBlastDiceIncrease pattern;
        // flat bonuses canceled with a negative modifier - TTT's
        // OutgoingWeaponDamageBonus pattern; Bonus/BonusTargetRelated are
        // read-only properties in this build).
        foreach (BaseDamage damage in evt.DamageBundle)
        {
          damage.Dice.Modify(DiceFormula.Zero, Fact);
          int flat = damage.Bonus + damage.BonusTargetRelated;
          if (flat != 0)
          {
            damage.AddModifier(new Modifier(-flat, Fact, ModifierDescriptor.UntypedStackable));
          }
        }

        if (Owner.Buffs.GetBuff(ChargeBuff) is null)
        {
          if (!warnedDissipate && bp != null)
          {
            MissionFeats.Logger.Warn(
              $"[spellfist] {bp.name} dissipated - no charge window (cast outside a full attack).");
            warnedDissipate = true;
          }
          return;
        }
        if (Shares.Count > 0)
        {
          // A second delivery of a multi-charge cast (e.g. Chill Touch): zeroed,
          // but only the first charge's spread is banked.
          return;
        }
        int bab = Owner.Stats.BaseAttackBonus.ModifiedValue;
        int n = Math.Min(5, Math.Max(1,
          2 + (bab >= 6 ? 1 : 0) + (bab >= 11 ? 1 : 0) + (bab >= 16 ? 1 : 0)));
        foreach (var c in captured)
        {
          for (int i = 0; i < n; i++)
          {
            int rolls = c.Rolls / n + (i < c.Rolls % n ? 1 : 0);
            int flat = c.Flat / n + (i < c.Flat % n ? 1 : 0);
            if (rolls <= 0 && flat <= 0)
            {
              continue;
            }
            Shares.Add(new Share { Rolls = rolls, Die = c.Die, Flat = flat, Energy = c.Energy });
          }
        }
        LogShareBank(captured.Count, Shares.Count);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Spellfist: capture failed.", e);
      }
    }

    private void LogShareBank(int chunks, int shares)
    {
      // Kept off the hot path's string building: only informational.
      try
      {
        MissionFeats.Logger.Info(
          $"[spellfist] cascade banked: {chunks} damage chunk(s) -> {shares} share(s).");
      }
      catch
      {
        // Never let logging break the cascade.
      }
    }

    // ----- Pay one share per unarmed hit -----
    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.AttackRoll is null || !evt.AttackRoll.IsHit)
        {
          return;
        }
        var weapon = evt.Weapon?.Blueprint;
        if (weapon is null || !weapon.IsUnarmed)
        {
          return;
        }
        if (!Armed || evt.Target is null || !evt.Target.IsEnemy(Owner))
        {
          return;
        }
        var share = Shares[0];
        Shares.RemoveAt(0);
        var bundle = new DamageBundle();
        if (share.Energy is DamageEnergyType energy)
        {
          bundle.Add(new EnergyDamage(
            share.Rolls > 0 ? new DiceFormula(share.Rolls, share.Die) : DiceFormula.Zero,
            share.Flat, energy) { SourceFact = Fact });
        }
        else
        {
          bundle.Add(new DirectDamage(
            share.Rolls > 0 ? new DiceFormula(share.Rolls, share.Die) : DiceFormula.Zero,
            share.Flat));
        }
        Rulebook.Trigger(new RuleDealDamage(Owner, evt.Target, bundle) { Reason = Fact });
        MissionFeats.Logger.Info(
          $"[spellfist] share paid to {evt.Target.CharacterName}: {share.Rolls}d{share.Die}" +
          $"{(share.Flat != 0 ? $"+{share.Flat}" : "")} {share.Energy}, {Shares.Count} left.");
        if (Shares.Count == 0 && Owner.Buffs.GetBuff(ChargeBuff) != null)
        {
          Owner.RemoveFact(Owner.Buffs.GetBuff(ChargeBuff));
          var carapace = Owner.Buffs.GetBuff(CarapaceBuff);
          if (carapace != null)
          {
            Owner.RemoveFact(carapace);
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Spellfist: share payment failed.", e);
      }
    }
  }

  /// <summary>
  /// Sundering Blows: each unarmed hit against an enemy stacks the Sundered
  /// debuff, up to a level-scaled rank cap (1 + level/4, max 5).
  /// </summary>
  [TypeId(Guids.SpellfistSunderComponent)]
  internal class SpellfistSunderTrigger : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>, IRulebookHandler<RuleAttackWithWeapon>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass CharacterClass;
    public BlueprintBuff SunderedBuff;

    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.AttackRoll is null || !evt.AttackRoll.IsHit)
        {
          return;
        }
        var weapon = evt.Weapon?.Blueprint;
        if (weapon is null || !weapon.IsUnarmed)
        {
          return;
        }
        if (evt.Target is null || !evt.Target.IsEnemy(Owner))
        {
          return;
        }
        int level = Owner.Descriptor.Progression.GetClassLevel(CharacterClass);
        int cap = Math.Min(5, Math.Max(1, 1 + level / 4));
        int rank = evt.Target.Buffs.GetBuff(SunderedBuff)?.GetRank() ?? 0;
        if (rank >= cap)
        {
          return;
        }
        evt.Target.AddBuff(SunderedBuff, Context,
          ContextDuration.Fixed(1).Calculate(Context).Seconds);
        MissionFeats.Logger.Info(
          $"[spellfist] sunder applied to {evt.Target.CharacterName} " +
          $"(rank {rank + 1}/{cap}).");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Spellfist: sunder failed.", e);
      }
    }
  }

  /// <summary>
  /// The Sundered debuff's runtime effect: every weapon-damage calculation
  /// against the owner treats the owner's damage reduction as 2 lower per rank
  /// (DamageValue.ReductionPenalty - the COP Divine Fighting Technique
  /// mechanism, mirrored on the defender so the whole party benefits).
  /// </summary>
  [TypeId(Guids.SpellfistSunderRefundComponent)]
  internal class SpellfistSunderedWard : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleCalculateDamage>
  {
    public BlueprintBuff SelfBuff;
    public int AmountPerRank = 2;

    public void OnEventAboutToTrigger(RuleCalculateDamage evt) { }

    public void OnEventDidTrigger(RuleCalculateDamage evt)
    {
      try
      {
        if (evt.DamageBundle?.WeaponDamage is null)
        {
          return;
        }
        int rank = Owner.Buffs.GetBuff(SelfBuff)?.GetRank() ?? 0;
        if (rank <= 0)
        {
          return;
        }
        evt.DamageBundle.WeaponDamage.ReductionPenalty.Add(
          new Modifier(rank * AmountPerRank, Fact, ModifierDescriptor.UntypedStackable));
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Spellfist: sundered ward failed.", e);
      }
    }
  }
}

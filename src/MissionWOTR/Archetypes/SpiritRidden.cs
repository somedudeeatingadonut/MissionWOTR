using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// THE SPIRIT-RIDDEN (shaman homebrew - the user's design, 0.36.0).
  ///
  /// The user's brief: "someone actually being taken over by spirits,
  /// granting them different ability sets, almost none of the normal
  /// shaman stuff remains, no spells nor hexes, no familiar, and
  /// spirit is replaced with a different version which lets her
  /// select a spirit that completely inhabits her body, gaining more
  /// choices over time. The spirits function as a form she can change
  /// at will, which makes her take the role of a basic class with -2
  /// levels (no downside for the first 2 levels), alongside level
  /// appropriate spirit equipment (the shaman must not be wearing
  /// anything for the equipment to apply)."
  ///
  /// The trades (everything found in the live progression by scan):
  /// - SPELLS: the archetype carries RemoveSpellbook (the official
  ///   BlueprintArchetype flag, exposed by bpcore - no Owlcat
  ///   archetype ever shipped without casting, so the field sat
  ///   unused; DarkCodex's helper surfaces it too). This also kills
  ///   spirit magic for free - it has nowhere left to live.
  /// - HEXES: every ShamanHexSelection grant in the progression.
  /// - FAMILIAR: ShamanSpiritAnimalSelection.
  /// - SPIRIT: the vanilla ShamanSPiritSelection (Owlcat's typo is
  ///   in the ref name) - the selection is removed; nothing of the
  ///   vanilla spirit chain survives.
  ///
  /// The gains:
  /// - INHABITING SPIRITS: a selection re-granted at 1st, 6th, 12th
  ///   and 18th (OnlyNew - each spirit once). Two spirits answer at
  ///   1st (the Weapon Saint, the Red Warlord), two more unlock at
  ///   6th (the Barefoot Master, the Cutthroat), one at 12th (the
  ///   Grey Hunter), one at 18th (the First Knight) - "gaining more
  ///   choices over time".
  /// - THE FORMS: each spirit is a hero of a dead age who takes the
  ///   reins AT WILL - a swift action that swaps which form buff is
  ///   active (each channel ability removes the other five first;
  ///   exclusivity by construction). While channeled she fights as
  ///   that spirit's CLASS at her shaman level MINUS TWO - the
  ///   user's exact rule "no downside for the first 2 levels" means
  ///   effective = level at 1st and 2nd, level - 2 from 3rd on -
  ///   with the class's real proficiencies, a full-BAB top-up where
  ///   the class has one (AdditionalAttackBonus), and a signature
  ///   package (see each spirit's loc text).
  /// - SPIRIT EQUIPMENT: while a form is channeled AND her body is
  ///   BARE (every equipment slot empty - the user's rule), the
  ///   spirit's regalia manifests: a level-appropriate enhancement
  ///   (1 + (eff-1)/4, so +1 at 1st-4th effective up to +5 at 17th+)
  ///   to attack, damage and armor class. Wear anything and the
  ///   spirit's regalia refuses to appear; the check re-runs every
  ///   tick, so unequipping mid-fight lets it surface.
  ///
  /// Documented scope cuts (v1): the spirits are stat-and-proficiency
  /// packages, not full class progressions - feat selections,
  /// rage-round resources, ki pools, sneak-attack-vs-flat-footed
  /// detection and favored-enemy picks are not portable as buff
  /// payloads without deep new UI; the signatures are hand-rolled
  /// equivalents. The Cutthroat's rider (first hit each round deals
  /// +1d6 per 3 effective levels) uses the repo-proven direct-damage
  /// rider idiom instead of the game's flat-footed detection.
  /// Log prefix: [spiritridden].
  /// </summary>
  internal static class SpiritRidden
  {
    internal const string ArchetypeName = "SpiritRiddenArchetype";

    public static void Configure()
    {
      var shaman = CharacterClassRefs.ShamanClass.Reference.Get();

      // ----- The six spirits -----
      // Each: a form buff (proficiencies + signature via the core
      // component), a channel ability (swift, at will, exclusive),
      // and a spirit feature that grants the ability and gates on
      // shaman level. Two answer at 1st; the rest unlock over time.
      var saint = Spirit(
        "Saint", Guids.SpiritRiddenSaintFeature, Guids.SpiritRiddenSaintAbility, Guids.SpiritRiddenSaintBuff,
        FeatureRefs.FighterProficiencies, minLevel: 1,
        c => { c.FullBab = true; c.AttackPer4 = 1; c.DamagePer4 = 1; });
      var warlord = Spirit(
        "Warlord", Guids.SpiritRiddenWarlordFeature, Guids.SpiritRiddenWarlordAbility, Guids.SpiritRiddenWarlordBuff,
        FeatureRefs.BarbarianProficiencies, minLevel: 1,
        c => { c.FullBab = true; c.WarlordSurge = true; });
      var master = Spirit(
        "Master", Guids.SpiritRiddenMasterFeature, Guids.SpiritRiddenMasterAbility, Guids.SpiritRiddenMasterBuff,
        FeatureRefs.MonkWeaponProficiency, minLevel: 6,
        c => { c.AcPer4 = 1; c.SpeedFlat = 20; });
      var cutthroat = Spirit(
        "Cutthroat", Guids.SpiritRiddenCutthroatFeature, Guids.SpiritRiddenCutthroatAbility, Guids.SpiritRiddenCutthroatBuff,
        FeatureRefs.RogueProficiencies, minLevel: 6,
        c => { c.SneakRider = true; },
        b => b.AddStatBonus(stat: StatType.Initiative, value: 2, descriptor: ModifierDescriptor.Competence));
      var hunter = Spirit(
        "Hunter", Guids.SpiritRiddenHunterFeature, Guids.SpiritRiddenHunterAbility, Guids.SpiritRiddenHunterBuff,
        FeatureRefs.RangerProficiencies, minLevel: 12,
        c => { c.FullBab = true; c.AttackPer4 = 1; c.SpeedFlat = 10; });
      var knight = Spirit(
        "Knight", Guids.SpiritRiddenKnightFeature, Guids.SpiritRiddenKnightAbility, Guids.SpiritRiddenKnightBuff,
        FeatureRefs.PaladinProficiencies, minLevel: 18,
        c => { c.FullBab = true; c.AcPer4 = 1; c.SavePer3 = 1; },
        b => b
          .AddConditionImmunity(condition: UnitCondition.Shaken)
          .AddConditionImmunity(condition: UnitCondition.Frightened));

      // ----- The selection -----
      var selection = FeatureSelectionConfigurator.New(
        "SpiritRiddenSpiritSelection", Guids.SpiritRiddenSpiritSelection)
        .SetDisplayName("SpiritRiddenSpirits.Name")
        .SetDescription("SpiritRiddenSpirits.Description")
        .SetIcon(FeatureRefs.OracleAncestorsMysteryFeature.Reference.Get().Icon)
        .SetIsClassFeature()
        .SetMode(SelectionMode.OnlyNew)
        .Configure();
      FeatureSelectionConfigurator.For(selection)
        .AddToAllFeatures(
          Guids.SpiritRiddenSaintFeature, Guids.SpiritRiddenWarlordFeature,
          Guids.SpiritRiddenMasterFeature, Guids.SpiritRiddenCutthroatFeature,
          Guids.SpiritRiddenHunterFeature, Guids.SpiritRiddenKnightFeature)
        .Configure();

      // ----- The archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.SpiritRiddenArchetype, CharacterClassRefs.ShamanClass)
          .SetLocalizedName("SpiritRidden.Name")
          .SetLocalizedDescription("SpiritRidden.Description")
          // The whole spellbook goes (the official flag; spirit magic
          // dies with it - it has nowhere left to live).
          .SetRemoveSpellbook(true)
          .AddToAddFeatures(LevelPlan.L(1), selection)
          .AddToAddFeatures(LevelPlan.L(6), selection)
          .AddToAddFeatures(LevelPlan.L(12), selection)
          .AddToAddFeatures(LevelPlan.L(18), selection);

      // The trades: every hex, the spirit animal, the vanilla spirit.
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, shaman,
        FeatureSelectionRefs.ShamanHexSelection.ToString(),
        FeatureSelectionRefs.ShamanSpiritAnimalSelection.ToString(),
        FeatureSelectionRefs.ShamanSPiritSelection.ToString());

      archetype.Configure();
      MissionFeats.Logger.Info("[spiritridden] configured: " + ArchetypeName + ".");
    }

    /// <summary>
    /// One inhabiting spirit: form buff + channel ability + gated
    /// spirit feature. The channel is a swift, at-will, personal
    /// action that removes every OTHER spirit's form buff and applies
    /// this one - exclusivity by construction, switching at will.
    /// </summary>
    private static BlueprintFeature Spirit(
      string key,
      string featureGuid,
      string abilityGuid,
      string buffGuid,
      Blueprint<BlueprintReference<BlueprintFeature>> proficiencies,
      int minLevel,
      Action<SpiritRiddenForm> configureForm,
      Action<BuffConfigurator> buffExtras = null)
    {
      var prof = proficiencies.Reference.Get();
      var icon = prof.Icon;
      var others = new (string Key, string BuffGuid)[]
      {
        ("Saint", Guids.SpiritRiddenSaintBuff),
        ("Warlord", Guids.SpiritRiddenWarlordBuff),
        ("Master", Guids.SpiritRiddenMasterBuff),
        ("Cutthroat", Guids.SpiritRiddenCutthroatBuff),
        ("Hunter", Guids.SpiritRiddenHunterBuff),
        ("Knight", Guids.SpiritRiddenKnightBuff),
      };

      // The form buff: the class's real proficiencies plus the core
      // component (BAB top-up, signature package, spirit equipment).
      var buffConfig = BuffConfigurator.New("SpiritRidden" + key + "Buff", buffGuid)
        .SetDisplayName("SpiritRidden" + key + ".Name")
        .SetDescription("SpiritRidden" + key + ".Description")
        .SetIcon(icon)
        .AddFacts(new() { prof });
      buffExtras?.Invoke(buffConfig);
      var form = new SpiritRiddenForm
      {
        ShamanClass = CharacterClassRefs.ShamanClass.Reference.Get(),
      };
      if (configureForm != null)
      {
        configureForm(form);
      }
      var buff = buffConfig.AddComponent(form).Configure();

      // The channel: swift, at will, exclusive.
      var channel = ActionsBuilder.New();
      foreach (var other in others)
      {
        if (other.Key == key)
        {
          continue;
        }
        channel = channel.RemoveBuff(other.BuffGuid, toCaster: true);
      }
      channel = channel.ApplyBuff(buff, ContextDuration.Fixed(100000, DurationRate.Hours), toCaster: true);
      var ability = AbilityConfigurator.New("SpiritRidden" + key + "Ability", abilityGuid)
        .SetDisplayName("SpiritRidden" + key + ".Name")
        .SetDescription("SpiritRidden" + key + ".Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Swift)
        .SetCanTargetSelf()
        .AddAbilityEffectRunAction(channel)
        .Configure();

      // The spirit feature: the gate. All six are in the selection;
      // the Master, Cutthroat, Hunter and Knight also carry shaman
      // level prerequisites so they only OFFER themselves in time.
      var feature = FeatureConfigurator.New("SpiritRidden" + key + "Feature", featureGuid)
        .SetDisplayName("SpiritRidden" + key + ".Name")
        .SetDescription("SpiritRidden" + key + ".Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { ability });
      if (minLevel > 1)
      {
        feature = feature.AddPrerequisiteClassLevel(
          CharacterClassRefs.ShamanClass.Cast<BlueprintCharacterClassReference>(), minLevel);
      }
      return feature.Configure();
    }
  }

  /// <summary>
  /// The engine of every spirit form. While the buff is on:
  /// - the vessel fights as the spirit's class at shaman level minus
  ///   two (no reduction for the first two levels) - a full-BAB
  ///   top-up where the class has one, versus the shaman's 3/4;
  /// - the spirit's signature package scales with that effective
  ///   level (attack/damage/AC/saves per 3-4 levels, the warlord's
  ///   str/con surge, the master's stride, the cutthroat's rider);
  /// - and if her body is BARE - every equipment slot empty - the
  ///   spirit's regalia manifests: a level-appropriate enhancement
  ///   bonus to attack, damage and AC. Wear anything of your own and
  ///   the spirit's regalia refuses to appear.
  /// Everything is recomputed each tick and torn down on turn-off.
  /// </summary>
  [TypeId(Guids.SpiritRiddenFormComponent)]
  internal class SpiritRiddenForm : UnitFactComponentDelegate,
    Kingmaker.Controllers.Units.ITickEachRound,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass ShamanClass;
    public bool FullBab;
    public int AttackPer4;
    public int DamagePer4;
    public int AcPer4;
    public int SavePer3;
    public bool WarlordSurge;
    public int SpeedFlat;
    public bool SneakRider;

    private readonly List<ModifiableValue.Modifier> m_Added = new();
    private bool m_RiderUsed;

    public void OnNewRound()
    {
      m_RiderUsed = false;
      Recalculate();
    }

    protected override void OnTurnOn()
    {
      m_RiderUsed = false;
      Recalculate();
    }

    protected override void OnTurnOff()
    {
      Clear();
    }

    private int EffectiveLevel()
    {
      int level = Owner.Descriptor.Progression.GetClassLevel(ShamanClass);
      // The user's rule: "-2 levels (no downside for the first 2
      // levels)" - full parity at 1st and 2nd, level minus two after.
      return level <= 2 ? level : level - 2;
    }

    private void Recalculate()
    {
      try
      {
        Clear();
        int level = Owner.Descriptor.Progression.GetClassLevel(ShamanClass);
        int eff = EffectiveLevel();
        if (level <= 0 || eff <= 0)
        {
          return;
        }

        // The role: full BAB where the class has one, versus the
        // shaman's 3/4 progression.
        if (FullBab)
        {
          int shamanBab = 3 * level / 4;
          Add(Owner.Stats.AdditionalAttackBonus, Math.Max(0, eff - shamanBab), ModifierDescriptor.UntypedStackable);
        }

        // The signature package.
        Add(Owner.Stats.AdditionalAttackBonus, AttackPer4 * eff / 4, ModifierDescriptor.Competence);
        Add(Owner.Stats.AdditionalDamage, DamagePer4 * eff / 4, ModifierDescriptor.Competence);
        Add(Owner.Stats.AC, AcPer4 * eff / 4, ModifierDescriptor.Dodge);
        int save = SavePer3 * eff / 3;
        Add(Owner.Stats.SaveFortitude, save, ModifierDescriptor.Resistance);
        Add(Owner.Stats.SaveReflex, save, ModifierDescriptor.Resistance);
        Add(Owner.Stats.SaveWill, save, ModifierDescriptor.Resistance);
        Add(Owner.Stats.Speed, SpeedFlat, ModifierDescriptor.Enhancement);

        // The warlord's unquiet fury.
        if (WarlordSurge)
        {
          int surge = eff >= 16 ? 6 : eff >= 11 ? 4 : 2;
          Add(Owner.Stats.Strength, surge, ModifierDescriptor.Morale);
          Add(Owner.Stats.Constitution, surge, ModifierDescriptor.Morale);
          Add(Owner.Stats.SaveWill, 2, ModifierDescriptor.Morale);
          Add(Owner.Stats.AC, -2, ModifierDescriptor.Penalty);
        }

        // The spirit's regalia - only on a bare body.
        if (BodyIsBare())
        {
          int gear = 1 + Math.Max(0, eff - 1) / 4;
          Add(Owner.Stats.AdditionalAttackBonus, gear, ModifierDescriptor.Enhancement);
          Add(Owner.Stats.AdditionalDamage, gear, ModifierDescriptor.Enhancement);
          Add(Owner.Stats.AC, gear, ModifierDescriptor.Enhancement);
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[spiritridden] form recalculation failed.", e);
      }
    }

    /// <summary>
    /// The user's rule: "the shaman must not be wearing anything for
    /// the equipment to apply" - every equipment slot must be empty.
    /// </summary>
    private bool BodyIsBare()
    {
      try
      {
        if (Owner?.Body is null)
        {
          return false;
        }
        return !Owner.Body.EquipmentSlots.Any(slot => slot.HasItem);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[spiritridden] bare-body check failed.", e);
        return false;
      }
    }

    private void Add(ModifiableValue stat, int value, ModifierDescriptor descriptor)
    {
      if (value == 0)
      {
        return;
      }
      m_Added.Add(stat.AddModifier(value, Runtime, descriptor));
    }

    private void Clear()
    {
      foreach (var modifier in m_Added)
      {
        modifier?.Remove();
      }
      m_Added.Clear();
    }

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    /// <summary>
    /// The Cutthroat's rider: the first wound she lands each round
    /// runs deeper - +1d6 per three effective levels (the
    /// repo-proven direct-damage idiom, not the game's flat-footed
    /// detection, which is not exposed where we can read it).
    /// </summary>
    public void OnEventDidTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (!SneakRider || m_RiderUsed || evt.Initiator != Owner || !evt.IsHit)
        {
          return;
        }
        var target = evt.Target;
        if (target is null || target == Owner)
        {
          return;
        }
        int eff = EffectiveLevel();
        int dice = Math.Max(1, eff / 3);
        m_RiderUsed = true;
        var bundle = new DamageBundle();
        bundle.Add(new DirectDamage(new DiceFormula(dice, DiceType.D6), 0));
        Rulebook.Trigger(new RuleDealDamage(Owner, target, bundle) { Reason = Fact });
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[spiritridden] cutthroat rider failed.", e);
      }
    }
  }
}

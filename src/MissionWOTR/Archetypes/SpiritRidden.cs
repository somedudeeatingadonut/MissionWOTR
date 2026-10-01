using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
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

      // ----- 0.38.0: the wolf's long-rest rule. The user: "if the wolf
      // dies, it cannot come back until long rest, otherwise you could
      // just switch forms to something then back, and acquire it again."
      // The lockout is an ability resource with max 1: spent when the
      // wolf dies, restored by the engine's rest processing (every
      // ability resource in the game refills on rest) - no rest-detection
      // code needed at all.
      var wolfResource = AbilityResourceConfigurator.New(
        "SpiritRiddenWolfResource", Guids.SpiritRiddenWolfResource)
        .SetMax(1)
        .Configure();
      // The death watch rides the wolf itself: a guard buff applied at
      // spawn whose rider listens for the killing RuleDealDamage (the
      // SanguineFont target-side idiom) and spends the summoner's resource
      // the moment the wolf dies.
      var wolfGuard = BuffConfigurator.New(
        "SpiritRiddenWolfGuardBuff", Guids.SpiritRiddenWolfGuardBuff)
        .SetIsClassFeature()
        .AddComponent(new SpiritRiddenWolfGuard { WolfResource = wolfResource })
        .Configure();

      // ----- 0.37.0: the four caster spirits (the user's expansion - -----
      // non-ranged role choices; NO buffing spirit by the user's design
      // rule: "an easy free before combat team buffing machine, would be
      // way too strong").
      var thorn = SpiritSpell("SpiritRiddenAntleredThorn", Guids.SpiritRiddenAntleredThornAbility,
        AbilityRefs.Grease, pretendLevel: 1, AbilityRange.Long,
        a => a.SetCanTargetEnemies()
          .AddAbilityEffectRunAction(ActionsBuilder.New().DealDamage(
            DamageTypes.Physical(), DiceByRank(), halfIfSaved: true)));
      var mend = SpiritSpell("SpiritRiddenAntleredMend", Guids.SpiritRiddenAntleredMendAbility,
        AbilityRefs.CureLightWounds, pretendLevel: 1, AbilityRange.Close,
        a => a.SetCanTargetFriends()
          .AddAbilityEffectRunAction(ActionsBuilder.New().HealTarget(DiceByRank())));
      var antlered = Spirit(
        "Antlered", Guids.SpiritRiddenAntleredFeature, Guids.SpiritRiddenAntleredAbility, Guids.SpiritRiddenAntleredBuff,
        FeatureRefs.MonkWeaponProficiency, minLevel: 6,
        c => { c.TracksWolf = true; c.WolfResource = wolfResource; },
        b => b
          .AddFacts(new() { thorn, mend })
          .AddFactContextActions(deactivated: ActionsBuilder.New().Add(
            new SpiritRiddenDespawnCompanionAction())),
        channel => channel.Add(new SpiritRiddenSummonCompanionAction
        {
          WolfResource = wolfResource,
          GuardBuff = wolfGuard,
        }),
        f => f.AddAbilityResources(resource: wolfResource, restoreAmount: true));

      var bolt = SpiritSpell("SpiritRiddenPyreBolt", Guids.SpiritRiddenPyreBoltAbility,
        AbilityRefs.ScorchingRay, pretendLevel: 2, AbilityRange.Long,
        a => a.SetCanTargetEnemies()
          .AddAbilityEffectRunAction(ActionsBuilder.New().DealDamage(
            DamageTypes.Energy(DamageEnergyType.Fire), DiceByRank(), halfIfSaved: true)));
      var burst = SpiritSpell("SpiritRiddenPyreBurst", Guids.SpiritRiddenPyreBurstAbility,
        AbilityRefs.Fireball, pretendLevel: 3, AbilityRange.Long,
        a => a.SetCanTargetEnemies()
          .AddAbilityAoERadius(
            diameterInCells: 4,
            targetType: Kingmaker.UnitLogic.Abilities.Components.TargetType.Enemy)
          .AddAbilityEffectRunAction(ActionsBuilder.New().DealDamage(
            DamageTypes.Energy(DamageEnergyType.Fire), DiceByRank(), halfIfSaved: true)));
      var pyre = Spirit(
        "Pyre", Guids.SpiritRiddenPyreFeature, Guids.SpiritRiddenPyreAbility, Guids.SpiritRiddenPyreBuff,
        FeatureRefs.RogueProficiencies, minLevel: 6,
        c => { },
        b => b.AddFacts(new() { bolt, burst }));

      var dread = SpiritSpell("SpiritRiddenArchivistDread", Guids.SpiritRiddenArchivistDreadAbility,
        AbilityRefs.CauseFear, pretendLevel: 3, AbilityRange.Long,
        a => a.SetCanTargetEnemies()
          .AddAbilityEffectRunAction(ActionsBuilder.New().SavingThrow(
            SavingThrowType.Will,
            onResult: ActionsBuilder.New().ApplyBuff(
              BuffRefs.Frightened.Cast<BlueprintBuffReference>(), ContextDuration.Fixed(2)))));
      var mien = SpiritSpell("SpiritRiddenArchivistMien", Guids.SpiritRiddenArchivistMienAbility,
        AbilityRefs.Fear, pretendLevel: 4, AbilityRange.Long,
        a => a.SetCanTargetEnemies()
          .AddAbilityAoERadius(
            diameterInCells: 6,
            targetType: Kingmaker.UnitLogic.Abilities.Components.TargetType.Enemy)
          .AddAbilityEffectRunAction(ActionsBuilder.New().SavingThrow(
            SavingThrowType.Will,
            onResult: ActionsBuilder.New().ApplyBuff(
              BuffRefs.Shaken.Cast<BlueprintBuffReference>(), ContextDuration.Fixed(2)))));
      var archivist = Spirit(
        "Archivist", Guids.SpiritRiddenArchivistFeature, Guids.SpiritRiddenArchivistAbility, Guids.SpiritRiddenArchivistBuff,
        FeatureRefs.RogueProficiencies, minLevel: 12,
        c => { },
        b => b.AddFacts(new() { dread, mien }));

      var spellblade = Spirit(
        "Spellblade", Guids.SpiritRiddenSpellbladeFeature, Guids.SpiritRiddenSpellbladeAbility, Guids.SpiritRiddenSpellbladeBuff,
        FeatureRefs.MagusProficiencies, minLevel: 18,
        c => { c.EnergyRiderPer5 = 1; });

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
          Guids.SpiritRiddenHunterFeature, Guids.SpiritRiddenKnightFeature,
          Guids.SpiritRiddenAntleredFeature, Guids.SpiritRiddenPyreFeature,
          Guids.SpiritRiddenArchivistFeature, Guids.SpiritRiddenSpellbladeFeature)
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
    /// One spirit-taught spell: an at-will spell-like ability with DC
    /// and caster level from the shaman class + Wisdom (the class
    /// params component), dice scaling at half the class level
    /// (rank config, min 1 max 10), and a pretend spell level for
    /// the DC math. The monster-caster pattern: real spells as
    /// facts, no spellbook needed - the vessel HAS no spellbook.
    /// </summary>
    private static Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility SpiritSpell(
      string name,
      string guid,
      Blueprint<BlueprintReference<BlueprintAbility>> iconSource,
      int pretendLevel,
      AbilityRange range,
      Action<AbilityConfigurator> configure)
    {
      var builder = AbilityConfigurator.New(name, guid)
        .SetDisplayName(name + ".Name")
        .SetDescription(name + ".Description")
        .SetIcon(iconSource.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(range)
        .SetActionType(UnitCommand.CommandType.Standard)
        .AddContextCalculateAbilityParamsBasedOnClass(
          characterClass: CharacterClassRefs.ShamanClass.Cast<BlueprintCharacterClassReference>(),
          statType: StatType.Wisdom)
        .AddPretendSpellLevel(spellLevel: pretendLevel)
        .AddContextRankConfig(HalfLevelDice());
      configure(builder);
      return builder.Configure();
    }

    /// <summary>
    /// Dice that grow with half the shaman class level: 1 die at 1st,
    /// +1 every 2 levels, capped at 10 (odd levels round up). Built
    /// through bpcore's factory (the Anatomist-proven idiom) - the
    /// raw component's m_Class field is private in the raw DLLs.
    /// </summary>
    private static ContextRankConfig HalfLevelDice()
    {
      return ContextRankConfigs.ClassLevel(
          new[] { CharacterClassRefs.ShamanClass.ToString() }, min: 1, max: 10)
        .WithCustomProgression(
          (1, 1), (3, 2), (5, 3), (7, 4), (9, 5),
          (11, 6), (13, 7), (15, 8), (17, 9), (19, 10));
    }

    private static ContextDiceValue DiceByRank()
    {
      return new ContextDiceValue
      {
        DiceType = DiceType.D6,
        DiceCountValue = ContextValues.Rank(),
        BonusValue = ContextValues.Constant(0),
      };
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
      Action<BuffConfigurator> buffExtras = null,
      Func<ActionsBuilder, ActionsBuilder> channelExtras = null,
      Action<FeatureConfigurator> featureExtras = null)
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
        ("Antlered", Guids.SpiritRiddenAntleredBuff),
        ("Pyre", Guids.SpiritRiddenPyreBuff),
        ("Archivist", Guids.SpiritRiddenArchivistBuff),
        ("Spellblade", Guids.SpiritRiddenSpellbladeBuff),
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
      if (channelExtras != null)
      {
        channel = channelExtras(channel);
      }
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
      if (featureExtras != null)
      {
        feature = featureExtras(feature);
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
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
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
    public int AttackPer6;
    public int EnergyRiderPer5;
    public bool TracksWolf;
    public BlueprintAbilityResource WolfResource;

    private readonly List<ModifiableValue.Modifier> m_Added = new();
    private bool m_RiderUsed;

    public void OnNewRound()
    {
      m_RiderUsed = false;
      Recalculate();
      // The wolf's long-rest rule, fallback catch: if the wolf died
      // without the guard rider seeing it (death not via RuleDealDamage),
      // the round tick notices the corpse and locks the resource.
      if (TracksWolf && WolfResource != null)
      {
        try
        {
          SpiritRiddenCompanionSweep.MarkIfDead(Owner, WolfResource);
        }
        catch (Exception e)
        {
          MissionFeats.Logger.Error("[spiritridden] wolf death check failed.", e);
        }
      }
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
        Add(Owner.Stats.AdditionalAttackBonus, AttackPer6 * eff / 6, ModifierDescriptor.Competence);
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

    public void OnEventAboutToTrigger(RulePrepareDamage evt) { }

    /// <summary>
    /// The Spellblade's parting fire: every weapon hit carries +1d6
    /// fire per five effective levels (the SanguineFont kinetic-blade
    /// rider idiom).
    /// </summary>
    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (EnergyRiderPer5 <= 0 || evt.Initiator != Owner ||
          evt.DamageBundle?.Weapon is null)
        {
          return;
        }
        int eff = EffectiveLevel();
        int dice = Math.Max(1, EnergyRiderPer5 * eff / 5);
        evt.Add(new EnergyDamage(
          new DiceFormula(dice, DiceType.D6), 0, DamageEnergyType.Fire)
        {
          SourceFact = Fact,
        });
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[spiritridden] spellblade rider failed.", e);
      }
    }

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
  /// <summary>
  /// The Antlered One's wolf: summoned while the spirit holds the
  /// reins (the user's design: "have it be a summon while the spirit
  /// is being channeled"). Spawn uses the ConstructCrafter deploy
  /// recipe - the engine's own ContextActionSpawnMonster, built by
  /// reflection (raw DLLs: m_Blueprint/m_SummonPool/LevelValue are
  /// private), with the 0.4.11 lesson applied (CountValue must be
  /// non-null: zero dice, bonus one). The wolf is linked to the
  /// caster and AI-controlled, like every engine summon.
  /// </summary>
  [TypeId(Guids.SpiritRiddenSummonCompanionAction)]
  internal class SpiritRiddenSummonCompanionAction : ContextAction
  {
    internal const string WolfGuid = "03dd28e92faf2e44eb9564a6ba01fdd0";
    internal const string StockSummonBuffGuid = "8728e884eeaa8b047be04197ecf1a0e4";
    internal const string SummonPoolGuid = "d94c93e7240f10e41ae41db4c83d1cbe";

    /// <summary>One charge per rest: spent when the wolf dies.</summary>
    public BlueprintAbilityResource WolfResource;

    /// <summary>The death-watch buff applied to the wolf at spawn.</summary>
    public BlueprintBuff GuardBuff;

    public override void RunAction()
    {
      try
      {
        var caster = Context?.MaybeCaster;
        if (caster is null)
        {
          MissionFeats.Logger.Warn("[spiritridden] wolf summon: no caster in context.");
          return;
        }
        // The long-rest rule: if the wolf already died today, it does not
        // answer again until the vessel rests (the user's fix for
        // form-cycling a fresh wolf).
        if (WolfResource != null && caster.Resources.GetResourceAmount(WolfResource) <= 0)
        {
          MissionFeats.Logger.Warn(
            "[spiritridden] the wolf fell this day; it answers only after a long rest.");
          return;
        }
        // Never two wolves: re-channeling sweeps the old one first. The
        // sweep may find a corpse it had not yet charged for - so the
        // gate is checked again below.
        SpiritRiddenCompanionSweep.DespawnAll(caster, WolfResource);
        if (WolfResource != null && caster.Resources.GetResourceAmount(WolfResource) <= 0)
        {
          MissionFeats.Logger.Warn(
            "[spiritridden] the wolf fell this day; it answers only after a long rest.");
          return;
        }
        var wolf = BlueprintTool.Get<Kingmaker.Blueprints.BlueprintUnit>(WolfGuid);
        if (wolf is null)
        {
          MissionFeats.Logger.Error("[spiritridden] wolf summon: DireWolfSummon blueprint not found.");
          return;
        }

        var spawnType = typeof(ContextActionSpawnMonster);
        const System.Reflection.BindingFlags fieldFlags =
          System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
          System.Reflection.BindingFlags.Instance;
        var summonAction = ElementTool.Create<ContextActionSpawnMonster>();
        spawnType.GetField("m_Blueprint", fieldFlags)?.SetValue(
          summonAction, wolf.ToReference<Kingmaker.Blueprints.BlueprintUnitReference>());
        summonAction.CountValue = new ContextDiceValue
        {
          DiceType = DiceType.Zero,
          DiceCountValue = ContextValues.Constant(0),
          BonusValue = ContextValues.Constant(1),
        };
        summonAction.DurationValue = ContextDuration.Fixed(100000);
        summonAction.DoNotLinkToCaster = false;
        summonAction.IsDirectlyControllable = false;
        spawnType.GetField("m_SummonPool", fieldFlags)?.SetValue(
          summonAction, BlueprintTool.GetRef<Kingmaker.Blueprints.BlueprintSummonPoolReference>(SummonPoolGuid));
        spawnType.GetField("LevelValue", fieldFlags)?.SetValue(
          summonAction, ContextValues.Constant(0));
        // The guard buff rides the wolf so its death is charged the
        // instant it happens (the same ApplyBuff idiom as the stock
        // summon buff above it).
        summonAction.AfterSpawn = ActionsBuilder.New()
          .ApplyBuff(
            BlueprintTool.Get<BlueprintBuff>(StockSummonBuffGuid),
            ContextDuration.Fixed(100000))
          .ApplyBuff(GuardBuff, ContextDuration.Fixed(100000))
          .Build();
        summonAction.RunAction();
        MissionFeats.Logger.Info("[spiritridden] the Antlered One's wolf answers.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[spiritridden] wolf summon failed.", e);
      }
    }

    public override string GetCaption() => "Spirit-Ridden: the Antlered One's wolf";
  }

  /// <summary>
  /// The wolf fades: hooked to the Antlered One's form buff DEACTIVE
  /// date, so the wolf despawns on ANY path away from the form -
  /// switching spirits, dispelling, whatever removes the buff.
  /// </summary>
  [TypeId(Guids.SpiritRiddenDespawnCompanionAction)]
  internal class SpiritRiddenDespawnCompanionAction : ContextAction
  {
    /// <summary>One charge per rest: spent when the wolf dies.</summary>
    public BlueprintAbilityResource WolfResource;

    public override void RunAction()
    {
      var caster = Context?.MaybeCaster;
      if (caster is null)
      {
        return;
      }
      SpiritRiddenCompanionSweep.DespawnAll(caster, WolfResource);
    }

    public override string GetCaption() => "Spirit-Ridden: the wolf fades";
  }

  /// <summary>
  /// The despawn sweep (the ConstructCrafter recipe): find this
  /// caster's wolves in the game's SummonMonsterPool - the engine's
  /// registry of live summons, which covers saved and fresh units
  /// alike - then remove the stock summon buff (how the game itself
  /// ends summons), hide, and destroy.
  /// </summary>
  internal static class SpiritRiddenCompanionSweep
  {
    /// <summary>
    /// The fallback death catch: scans the pool for the caster's wolf; if
    /// it is dead, charges the long-rest resource and sweeps the corpse.
    /// Called every round while the Antlered One holds the reins.
    /// </summary>
    internal static void MarkIfDead(UnitEntityData caster, BlueprintAbilityResource wolfResource)
    {
      foreach (var wolf in FindWolves(caster))
      {
        if (wolf.HPLeft <= 0 || wolf.Descriptor.State.IsDead)
        {
          ChargeWolfDeath(caster, wolfResource);
          DespawnWolf(wolf, "round-tick corpse sweep");
        }
      }
    }

    /// <summary>Charges the long-rest resource once for a dead wolf.</summary>
    private static void ChargeWolfDeath(UnitEntityData caster, BlueprintAbilityResource wolfResource)
    {
      if (wolfResource == null || caster is null)
      {
        return;
      }
      if (caster.Resources.GetResourceAmount(wolfResource) > 0)
      {
        caster.Resources.Spend(wolfResource, 1);
        MissionFeats.Logger.Info(
          "[spiritridden] the wolf's death is felt; it cannot return until a long rest.");
      }
    }

    private static System.Collections.Generic.IEnumerable<UnitEntityData> FindWolves(
      UnitEntityData caster)
    {
      var pool = Game.Instance.SummonPools.GetPool(
        BlueprintTool.Get<Kingmaker.Blueprints.BlueprintSummonPool>(
          SpiritRiddenSummonCompanionAction.SummonPoolGuid));
      if (pool is null)
      {
        yield break;
      }
      var wolfName = BlueprintTool.Get<Kingmaker.Blueprints.BlueprintUnit>(
        SpiritRiddenSummonCompanionAction.WolfGuid)?.name;
      foreach (var old in pool.Units.ToList())
      {
        if (old is null || old.Blueprint is null ||
          !string.Equals(old.Blueprint.name, wolfName, StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }
        var summoner = old
          .Get<Kingmaker.UnitLogic.Parts.UnitPartSummonedMonster>()?.Summoner;
        if (summoner is null || summoner.UniqueId != caster.UniqueId)
        {
          continue;
        }
        yield return old;
      }
    }

    internal static void DespawnAll(UnitEntityData caster, BlueprintAbilityResource wolfResource = null)
    {
      try
      {
        foreach (var old in FindWolves(caster))
        {
          // If this wolf is a corpse, its death is charged before the
          // body is cleared (the deactivate-path death catch).
          if (old.HPLeft <= 0 || old.Descriptor.State.IsDead)
          {
            ChargeWolfDeath(caster, wolfResource);
          }
          DespawnWolf(old, "sweep");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[spiritridden] wolf sweep failed.", e);
      }
    }

    /// <summary>The engine's despawn, all in one place (the ConstructCrafter recipe).</summary>
    private static void DespawnWolf(UnitEntityData old, string source)
    {
      MissionFeats.Logger.Info(
        $"[spiritridden] the wolf (uid={old.UniqueId}) leaves ({source}).");
      try
      {
        old.Buffs.RemoveFact(
          Game.Instance.BlueprintRoot.SystemMechanics.SummonedUnitBuff);
      }
      catch (Exception buffEx)
      {
        MissionFeats.Logger.Warn(
          $"[spiritridden] wolf summon-buff removal failed: {buffEx.Message}");
      }
      old.IsInGame = false;
      try
      {
        old.MarkForDestroy();
      }
      catch (Exception destroyEx)
      {
        MissionFeats.Logger.Warn(
          $"[spiritridden] wolf destroy failed: {destroyEx.Message}");
      }
    }
  }

  /// <summary>
  /// The wolf's death watch, riding the wolf itself (the SanguineFont
  /// target-side RuleDealDamage idiom). The guard buff is applied at
  /// spawn; the moment the killing damage lands, the summoner's
  /// long-rest resource is spent - so switching spirits and back cannot
  /// conjure a fresh wolf (the user's exact rule).
  /// </summary>
  [TypeId(Guids.SpiritRiddenWolfGuardComponent)]
  internal class SpiritRiddenWolfGuard : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    ITargetRulebookSubscriber, ISubscriber
  {
    public BlueprintAbilityResource WolfResource;

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        if (Owner.HPLeft > 0 && !Owner.Descriptor.State.IsDead)
        {
          return;
        }
        var summoner = Owner
          .Get<Kingmaker.UnitLogic.Parts.UnitPartSummonedMonster>()?.Summoner;
        if (summoner is null || WolfResource == null)
        {
          return;
        }
        if (summoner.Resources.GetResourceAmount(WolfResource) > 0)
        {
          summoner.Resources.Spend(WolfResource, 1);
          MissionFeats.Logger.Info(
            "[spiritridden] the wolf's death is felt; it cannot return until a long rest.");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[spiritridden] wolf death watch failed.", e);
      }
    }
  }

}

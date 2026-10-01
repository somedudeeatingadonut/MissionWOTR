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
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.51.0 — the Holy Vindicator (prestige class, Advanced Player's
  /// Guide pg. 263). The user's request: "add 2 prestige classes." The
  /// port of the pair. "Paragons of battle, eschewing sermons for
  /// steel... living conduits of divine power, down to their very
  /// blood, which they happily shed."
  ///
  /// The blood price is this project's oldest theme (the Overchanneler
  /// 0.42.0, the Mending Blade's tithe 0.45.0) — the vindicator is
  /// where it becomes a CLASS.
  ///
  /// ENTRY (adapted, documented): BAB +5 (PrerequisiteFullStatValue,
  /// the dcx BAB idiom) and the channel energy feature (any of the
  /// channel family). The tabletop's skill ranks and the Alignment/
  /// Elemental Channel feat have no engine prereq components here —
  /// documented simplification.
  ///
  /// THE GAIN (10 levels, d10, full BAB, strong Fort/Will):
  /// - Vindicator's Shield (1st, adapted): swift action, 1 minute:
  ///   +2 sacred AC, growing to +3 at 4th, +4 at 7th, +5 at 10th (the
  ///   tabletop's channel-dice-fueled 24-hour shield becomes a
  ///   level-scaled stance; the "until struck" clause is cut).
  /// - Stigmata (2nd, adapted): the tabletop's pick-one-of-five
  ///   becomes TWO swift stances, each 1 minute: Wrath (+ attacks and
  ///   weapon damage) or the Martyr (+ saves and AC). The bonus is
  ///   half class level (round up); the price is bleeding the same
  ///   amount every round (the ITickEachRound idiom, the DarkCodex
  ///   BleedBuff pattern). The "immune to other bleed" clause is cut.
  /// - Faith Healing (3rd, adapted): the vindicator's self-targeted
  ///   healing gains a bonus equal to half his class level (round up);
  ///   at 8th the bonus doubles (the tabletop's empower-then-maximize,
  ///   done as flat bonuses on the engine's own heal rule).
  /// - Bloodfire (5th, adapted): while any stigmata bleeds, his melee
  ///   weapon attacks deal +1d6 (the tabletop's Channel-Smite rider,
  ///   done as a direct-damage rider; Channel Smite itself does not
  ///   exist in this game).
  /// - Divine spellcasting: +1 level of an existing divine class at
  ///   2nd, 3rd, 4th, 6th, 7th, 8th, and 10th — the game's own
  ///   Loremaster-style LevelUp features (cleric, druid, inquisitor,
  ///   hunter), granted through a selection the vindicator re-picks
  ///   each time (the game's own prestige pattern; FeaturesRankIncrease
  ///   ranks the re-picks). Warpriest and oracle advancement does not
  ///   exist in the engine — documented limitation.
  ///
  /// DOCUMENTED CUTS: channel energy stacking (WOTR channel scaling is
  /// class-bound), Divine Wrath / Divine Judgment / Divine Retribution
  /// (crit-triggered spell sacrifice has no spell-slot hook), Versatile
  /// Channel (no cone/line channel shapes), Bloodrain.
  /// </summary>
  internal class HolyVindicator
  {
    internal const string ClassName = "HolyVindicator";

    internal static void Configure()
    {
      var icon = AbilityRefs.Bless.Reference.Get().Icon;
      var rankClass = new string[] { Guids.HolyVindicatorClass };

      // ----- Vindicator's Shield: the stance and its buff. -----
      var shieldBuff = BuffConfigurator.New("HVShieldBuff", Guids.HVShieldBuff)
        .SetDisplayName("HVShield.Name")
        .SetDescription("HVShield.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddContextStatBonus(StatType.AC, ContextValues.Rank(), ModifierDescriptor.Sacred)
        .AddContextRankConfig(
          ContextRankConfigs.ClassLevel(rankClass).WithCustomProgression((1, 2), (4, 3), (7, 4), (10, 5)))
        .Configure();
      var shieldAbility = AbilityConfigurator.New("HVShieldAbility", Guids.HVShieldAbility)
        .SetDisplayName("HVShield.Name")
        .SetDescription("HVShield.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Swift)
        .SetCanTargetSelf()
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(new HVStanceAction
        {
          Buff = shieldBuff,
        }).Build())
        .Configure();
      var shieldFeature = FeatureConfigurator.New("HVShieldFeature", Guids.HVShieldFeature)
        .SetDisplayName("HVShield.Name")
        .SetDescription("HVShield.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { shieldAbility })
        .Configure();

      // ----- Stigmata: two stances, one shared rider. -----
      var stigmataBuffs = new BlueprintBuff[2];
      var stigmataGuids = new[] { Guids.HVStigmataWrathBuff, Guids.HVStigmataMartyrBuff };
      // Wrath: attacks and weapon damage. Martyr: saves and AC.
      var stigmataStats = new[]
      {
        new[] { StatType.AdditionalAttackBonus, StatType.AdditionalDamage },
        new[] { StatType.SaveFortitude, StatType.SaveReflex, StatType.SaveWill, StatType.AC },
      };
      for (var i = 0; i < 2; i++)
      {
        var buff = BuffConfigurator.New(i == 0 ? "HVStigmataWrathBuff" : "HVStigmataMartyrBuff", stigmataGuids[i])
          .SetDisplayName("HVStigmata.Name")
          .SetDescription("HVStigmata.Description")
          .SetIcon(icon)
          .SetIsClassFeature();
        foreach (var stat in stigmataStats[i])
        {
          buff.AddContextStatBonus(stat, ContextValues.Rank(),
            i == 0 ? ModifierDescriptor.Sacred : ModifierDescriptor.Sacred);
        }
        buff.AddContextRankConfig(
            ContextRankConfigs.ClassLevel(rankClass).WithCustomProgression((1, 1), (3, 2), (5, 3), (7, 4), (9, 5)));
        stigmataBuffs[i] = buff.Configure();
      }
      // The stigmata rider goes on after the loop: it resolves the
      // class by GUID at runtime (the class blueprint is created below).
      foreach (var buff in stigmataBuffs)
      {
        BuffConfigurator.For(buff)
          .AddComponent(new HVStigmataRider
          {
            ClassGuid = Guids.HolyVindicatorClass,
          })
          .Configure();
      }
      var stigmataAbilities = new BlueprintAbility[2];
      var stigmataAbilityGuids = new[] { Guids.HVStigmataWrathAbility, Guids.HVStigmataMartyrAbility };
      for (var i = 0; i < 2; i++)
      {
        stigmataAbilities[i] = AbilityConfigurator.New(
            i == 0 ? "HVStigmataWrathAbility" : "HVStigmataMartyrAbility", stigmataAbilityGuids[i])
          .SetDisplayName(i == 0 ? "HVStigmataWrath.Name" : "HVStigmataMartyr.Name")
          .SetDescription("HVStigmata.Description")
          .SetIcon(icon)
          .SetType(AbilityType.Special)
          .SetRange(AbilityRange.Personal)
          .SetActionType(UnitCommand.CommandType.Swift)
          .SetCanTargetSelf()
          .AddAbilityEffectRunAction(ActionsBuilder.New().Add(new HVStanceAction
          {
            Buff = stigmataBuffs[i],
          }).Build())
          .Configure();
      }
      var stigmataFeature = FeatureConfigurator.New("HVStigmataFeature", Guids.HVStigmataFeature)
        .SetDisplayName("HVStigmata.Name")
        .SetDescription("HVStigmata.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { stigmataAbilities[0], stigmataAbilities[1] })
        .Configure();

      // ----- Faith Healing (3rd): the self-heal rider. -----
      var faithFeature = FeatureConfigurator.New("HVFaithHealingFeature", Guids.HVFaithHealingFeature)
        .SetDisplayName("HVFaithHealing.Name")
        .SetDescription("HVFaithHealing.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new HVFaithHealingRider
        {
          ClassGuid = Guids.HolyVindicatorClass,
        })
        .Configure();

      // ----- Bloodfire (5th): +1d6 while the stigmata bleed. -----
      var bloodfireFeature = FeatureConfigurator.New("HVBloodfireFeature", Guids.HVBloodfireFeature)
        .SetDisplayName("HVBloodfire.Name")
        .SetDescription("HVBloodfire.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new HVBloodfireRider
        {
          StigmataWrathBuff = stigmataBuffs[0],
          StigmataMartyrBuff = stigmataBuffs[1],
        })
        .Configure();

      // ----- Divine spellcasting: the Loremaster-style selection. -----
      var castingSelection = FeatureSelectionConfigurator.New(
        "HVSpellcastingSelection", Guids.HVSpellcastingSelection)
        .SetDisplayName("HVSpellcasting.Name")
        .SetDescription("HVSpellcasting.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .SetMode(SelectionMode.Default)
        .Configure();
      FeatureSelectionConfigurator.For(castingSelection)
        .AddToAllFeatures(
          FeatureRefs.LoremasterClericLevelUp.Cast<BlueprintFeatureReference>(),
          FeatureRefs.LoremasterDruidLevelUp.Cast<BlueprintFeatureReference>(),
          FeatureRefs.LoremasterInquisitorLevelUp.Cast<BlueprintFeatureReference>(),
          FeatureRefs.LoremasterHunterLevelUp.Cast<BlueprintFeatureReference>())
        .Configure();

      // ----- The progression. -----
      LevelEntry Entry(int level, params BlueprintFeature[] features) =>
        new LevelEntry
        {
          Level = level,
          m_Features = features.Select(f => f.ToReference<BlueprintFeatureBaseReference>()).ToList(),
        };
      var progression = ProgressionConfigurator.New("HolyVindicatorProgression", Guids.HolyVindicatorProgression)
        .SetLevelEntries(
          Entry(1, shieldFeature),
          Entry(2, stigmataFeature, castingSelection),
          Entry(3, faithFeature, castingSelection),
          Entry(4, castingSelection),
          Entry(5, bloodfireFeature),
          Entry(6, castingSelection),
          Entry(7, castingSelection),
          Entry(8, castingSelection),
          Entry(10, castingSelection))
        .SetFeaturesRankIncrease(
          FeatureRefs.LoremasterClericLevelUp.Cast<BlueprintFeatureReference>(),
          FeatureRefs.LoremasterDruidLevelUp.Cast<BlueprintFeatureReference>(),
          FeatureRefs.LoremasterInquisitorLevelUp.Cast<BlueprintFeatureReference>(),
          FeatureRefs.LoremasterHunterLevelUp.Cast<BlueprintFeatureReference>())
        .Configure();

      // ----- The class. -----
      var clazz = CharacterClassConfigurator.New(ClassName, Guids.HolyVindicatorClass)
        .SetLocalizedName("HolyVindicator.Name")
        .SetLocalizedDescription("HolyVindicator.Description")
        .SetIcon(icon)
        .SetPrestigeClass()
        .SetHitDie(DiceType.D10)
        .SetBaseAttackBonus(StatProgressionRefs.BABFull.Cast<BlueprintStatProgressionReference>())
        .SetFortitudeSave(StatProgressionRefs.SavesHigh.Cast<BlueprintStatProgressionReference>())
        .SetReflexSave(StatProgressionRefs.SavesLow.Cast<BlueprintStatProgressionReference>())
        .SetWillSave(StatProgressionRefs.SavesHigh.Cast<BlueprintStatProgressionReference>())
        .SetSkillPoints(2)
        .SetClassSkills(StatType.SkillLoreReligion, StatType.CheckIntimidate, StatType.CheckDiplomacy)
        .SetProgression(progression)
        .AddComponent(new PrerequisiteFullStatValue
        {
          Stat = StatType.BaseAttackBonus,
          Value = 5,
        })
        .AddComponent(new PrerequisiteFeaturesFromList
        {
          m_Features = new[]
          {
            FeatureRefs.ChannelEnergyFeature.Cast<BlueprintFeatureReference>(),
            FeatureRefs.ChannelEnergyHospitalerFeature.Cast<BlueprintFeatureReference>(),
            FeatureRefs.ChannelEnergyEmpyrealFeature.Cast<BlueprintFeatureReference>(),
          },
          Group = Prerequisite.GroupType.Any,
          Amount = 1,
        })
        .Configure();

      // The reverse link: the progression belongs to the class.
      ProgressionConfigurator.For(progression)
        .SetClasses(new BlueprintProgression.ClassWithLevel
        {
          m_Class = clazz.ToReference<BlueprintCharacterClassReference>(),
          AdditionalLevel = 0,
        })
        .Configure();
      MissionFeats.Logger.Info("[holyvindicator] configured: " + ClassName + ".");
    }
  }

  /// <summary>
  /// A swift stance: apply its buff for 1 minute (the Inkbound quill
  /// idiom).
  /// </summary>
  [TypeId(Guids.HVStanceAction)]
  internal class HVStanceAction : ContextAction
  {
    public BlueprintBuff Buff;

    public override void RunAction()
    {
      try
      {
        var caster = Context?.MaybeCaster;
        if (caster is null)
        {
          return;
        }
        caster.AddBuff(Buff, Context, TimeSpan.FromSeconds(60));
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[holyvindicator] stance failed.", e);
      }
    }

    public override string GetCaption() => "Stance";
  }

  /// <summary>
  /// The stigmata's price: every round the blood flows, the vindicator
  /// takes damage equal to half his class level (round up) - the
  /// ITickEachRound idiom, the DarkCodex BleedBuff pattern.
  /// </summary>
  [TypeId(Guids.HVStigmataRider)]
  internal class HVStigmataRider : UnitBuffComponentDelegate, ITickEachRound
  {
    public string ClassGuid;

    public void OnNewRound()
    {
      try
      {
        var clazz = BlueprintTool.Get<BlueprintCharacterClass>(ClassGuid);
        var level = clazz is null ? 1 :
          Owner.Descriptor.Progression.GetClassLevel(clazz);
        var bleed = Math.Max(1, (level + 1) / 2);
        var bundle = new DamageBundle();
        bundle.Add(new DirectDamage(DiceFormula.Zero, bleed));
        Rulebook.Trigger(new RuleDealDamage(Owner, Owner, bundle) { Reason = Fact });
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[holyvindicator] stigmata bleed failed.", e);
      }
    }
  }

  /// <summary>
  /// Faith Healing: self-targeted healing the vindicator casts gains a
  /// bonus - half his class level (round up), doubled at 8th - via the
  /// heal rule's own AddModifierBonus (probe-verified method).
  /// </summary>
  [TypeId(Guids.HVFaithHealingRider)]
  internal class HVFaithHealingRider : UnitFactComponentDelegate,
    ITargetRulebookHandler<RuleHealDamage>, IRulebookHandler<RuleHealDamage>,
    ITargetRulebookSubscriber, ISubscriber
  {
    public string ClassGuid;

    public void OnEventAboutToTrigger(RuleHealDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner || evt.Target != Owner)
        {
          return;
        }
        var clazz = BlueprintTool.Get<BlueprintCharacterClass>(ClassGuid);
        if (clazz is null)
        {
          return;
        }
        var level = Owner.Descriptor.Progression.GetClassLevel(clazz);
        if (level <= 0)
        {
          return;
        }
        var bonus = Math.Max(1, (level + 1) / 2);
        if (level >= 8)
        {
          bonus *= 2;
        }
        evt.AddModifierBonus(bonus, Fact);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[holyvindicator] faith healing failed.", e);
      }
    }

    public void OnEventDidTrigger(RuleHealDamage evt) { }
  }

  /// <summary>
  /// Bloodfire: while any stigmata bleeds, his melee weapon attacks
  /// deal +1d6 (the strike-rider pattern).
  /// </summary>
  [TypeId(Guids.HVBloodfireRider)]
  internal class HVBloodfireRider : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintBuff StigmataWrathBuff;
    public BlueprintBuff StigmataMartyrBuff;

    public void OnEventAboutToTrigger(RulePrepareDamage evt) { }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        if (evt.Initiator != Owner ||
          evt.DamageBundle?.Weapon?.Blueprint?.IsMelee != true)
        {
          return;
        }
        var bleeding = (StigmataWrathBuff is not null && Owner.HasFact(StigmataWrathBuff)) ||
          (StigmataMartyrBuff is not null && Owner.HasFact(StigmataMartyrBuff));
        if (!bleeding)
        {
          return;
        }
        evt.Add(new DirectDamage(new DiceFormula(1, DiceType.D6), 0) { SourceFact = Fact });
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[holyvindicator] bloodfire failed.", e);
      }
    }
  }
}

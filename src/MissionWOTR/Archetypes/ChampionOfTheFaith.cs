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
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.43.0 — the Champion of the Faith (warpriest archetype, Advanced
  /// Class Guide pg. 128). The user's requested tabletop warpriest port.
  ///
  /// "Champions of the faith are crusaders who use the power of their
  /// divine patron to annihilate the faith's enemies." The natural
  /// warpriest archetype for the Worldwound - and the first warpriest
  /// archetype in the game (the class shipped with none).
  ///
  /// THE TRADES (found in the live progression by scan):
  /// - the 3rd-level bonus feat (Detect Alignment);
  /// - every Channel Energy grant (Smite) - the class's channel
  ///   selection goes;
  /// - attempted: the sacred-weapon enhancement upgrades above 4th
  ///   (RemoveEveryGrant "SacredWeaponEnhancement" - logged and skipped
  ///   if the progression names them differently).
  ///
  /// THE GAIN:
  /// - Chosen Alignment (1st): a selection of four - good, evil, law,
  ///   chaos. Each choice makes his weapon damage count as that
  ///   alignment for overcoming DR (AddOutgoingPhysicalDamageProperty,
  ///   the FortunesFool-proven component) and sets his opposed
  ///   alignment for smite, detect, and the align-weapon miracle.
  /// - Detect Alignment (3rd): a move action focused on one creature
  ///   within 60 feet - whether it bears the opposed alignment.
  /// - Smite (4th): a swift action marking one target (a hidden mark,
  ///   the Dreadnaught pattern) and lighting the champion's wrath for
  ///   1 minute: +Charisma on attack rolls (a context stat bonus on
  ///   the self buff - the Doomsayer Wisdom idiom), and his attacks
  ///   against the marked target deal bonus damage equal to his
  ///   warpriest level while it bears the opposed alignment. Uses:
  ///   1/day, +1 at 8th/12th/16th/20th.
  /// - Align Weapon (12th): a swift action infusing his weapon with
  ///   his chosen alignment for 1 minute - holy, unholy, axiomatic, or
  ///   anarchic (the four VANILLA weapon enchantments, delivered by
  ///   BuffEnchantAnyWeapon - the component the Spell Warrior's ally
  ///   buffs use). Uses: 1/day, +1 at 16th and 20th.
  ///
  /// DOCUMENTED ADAPTATIONS (the low-cut port rule):
  /// - The attack bonus applies against all foes while the smite burns
  ///   (a stat bonus on the self buff), not only the smitten target -
  ///   the engine's conditional attack modifiers are a poor fit.
  /// - The tabletop's DR bypass on smite and the Charisma deflection
  ///   bonus are cut; the outsider damage-doubling is cut (no
  ///   outsider-flag API in the raw DLLs).
  /// - The 4th-level "sacred weapon counts as aligned" arrives with the
  ///   1st-level choice instead (the aligned-damage component rides
  ///   the chosen-alignment features).
  /// </summary>
  internal class ChampionOfTheFaith
  {
    internal const string ArchetypeName = "ChampionOfTheFaith";

    internal static void Configure()
    {
      var warpriest = CharacterClassRefs.WarpriestClass.Reference.Get();
      var smiteIcon = AbilityRefs.BlessWeapon.Reference.Get().Icon;
      var detectIcon = AbilityRefs.Bless.Reference.Get().Icon;
      var alignIcon = AbilityRefs.MagicWeaponGreater.Reference.Get().Icon;

      // ----- Chosen Alignment (1st): the four markers. Each carries the
      // aligned-damage component (FortunesFool-proven) and is the
      // runtime marker the smite and align-weapon actions read. -----
      var good = FeatureConfigurator.New(
        "ChampionChosenGoodFeature", Guids.ChampionChosenGoodFeature)
        .SetDisplayName("ChampionChosenGood.Name")
        .SetDescription("ChampionChosenGood.Description")
        .SetIcon(smiteIcon)
        .SetIsClassFeature()
        .AddOutgoingPhysicalDamageProperty(
          addAlignment: true, alignment: DamageAlignment.Good)
        .Configure();
      var evil = FeatureConfigurator.New(
        "ChampionChosenEvilFeature", Guids.ChampionChosenEvilFeature)
        .SetDisplayName("ChampionChosenEvil.Name")
        .SetDescription("ChampionChosenEvil.Description")
        .SetIcon(smiteIcon)
        .SetIsClassFeature()
        .AddOutgoingPhysicalDamageProperty(
          addAlignment: true, alignment: DamageAlignment.Evil)
        .Configure();
      var law = FeatureConfigurator.New(
        "ChampionChosenLawFeature", Guids.ChampionChosenLawFeature)
        .SetDisplayName("ChampionChosenLaw.Name")
        .SetDescription("ChampionChosenLaw.Description")
        .SetIcon(smiteIcon)
        .SetIsClassFeature()
        .AddOutgoingPhysicalDamageProperty(
          addAlignment: true, alignment: DamageAlignment.Lawful)
        .Configure();
      var chaos = FeatureConfigurator.New(
        "ChampionChosenChaosFeature", Guids.ChampionChosenChaosFeature)
        .SetDisplayName("ChampionChosenChaos.Name")
        .SetDescription("ChampionChosenChaos.Description")
        .SetIcon(smiteIcon)
        .SetIsClassFeature()
        .AddOutgoingPhysicalDamageProperty(
          addAlignment: true, alignment: DamageAlignment.Chaotic)
        .Configure();
      var selection = FeatureSelectionConfigurator.New(
        "ChampionAlignmentSelection", Guids.ChampionAlignmentSelection)
        .SetDisplayName("ChampionAlignment.Name")
        .SetDescription("ChampionAlignment.Description")
        .SetIcon(smiteIcon)
        .SetIsClassFeature()
        .SetMode(SelectionMode.Default)
        .Configure();
      FeatureSelectionConfigurator.For(selection)
        .AddToAllFeatures(
          Guids.ChampionChosenGoodFeature, Guids.ChampionChosenEvilFeature,
          Guids.ChampionChosenLawFeature, Guids.ChampionChosenChaosFeature)
        .Configure();

      // ----- Detect Alignment (3rd). -----
      var detectAction = new ChampionDetectAction
      {
        GoodFeature = good,
        EvilFeature = evil,
        LawFeature = law,
        ChaosFeature = chaos,
      };
      var detectAbility = AbilityConfigurator.New(
        "ChampionDetectAbility", Guids.ChampionDetectAbility)
        .SetDisplayName("ChampionDetect.Name")
        .SetDescription("ChampionDetect.Description")
        .SetIcon(detectIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(UnitCommand.CommandType.Move)
        .AllowTargeting(enemies: true)
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(detectAction).Build())
        .Configure();
      var detectFeature = FeatureConfigurator.New(
        "ChampionDetectFeature", Guids.ChampionDetectFeature)
        .SetDisplayName("ChampionDetect.Name")
        .SetDescription("ChampionDetect.Description")
        .SetIcon(detectIcon)
        .SetIsClassFeature()
        .AddFacts(new() { detectAbility })
        .Configure();

      // ----- Smite (4th): the mark, the wrath buff, the resource. -----
      var mark = BuffConfigurator.New(
        "ChampionSmiteMarkBuff", Guids.ChampionSmiteMarkBuff)
        .SetDisplayName("ChampionSmiteMark.Name")
        .SetDescription("ChampionSmiteMark.Description")
        .SetIcon(smiteIcon)
        .SetIsClassFeature()
        .AddComponent(new ChampionSmiteRider
        {
          WarpriestClass = warpriest,
          GoodFeature = good,
          EvilFeature = evil,
          LawFeature = law,
          ChaosFeature = chaos,
        })
        .Configure();
      var smiteBuff = BuffConfigurator.New(
        "ChampionSmiteBuff", Guids.ChampionSmiteBuff)
        .SetDisplayName("ChampionSmiteBuff.Name")
        .SetDescription("ChampionSmiteBuff.Description")
        .SetIcon(smiteIcon)
        .SetIsClassFeature()
        // +Charisma on attack rolls - the Doomsayer Wisdom-to-Persuasion
        // idiom, aimed at attacks.
        .AddContextStatBonus(StatType.AdditionalAttackBonus, ContextValues.Rank(),
          ModifierDescriptor.UntypedStackable)
        .AddContextRankConfig(ContextRankConfigs.StatBonus(StatType.Charisma))
        .Configure();
      var smiteResource = AbilityResourceConfigurator.New(
        "ChampionSmiteResource", Guids.ChampionSmiteResource)
        .SetMax(1)
        .Configure();
      var smiteAction = new ChampionSmiteAction
      {
        MarkBuff = mark,
        SmiteBuff = smiteBuff,
        SmiteResource = smiteResource,
      };
      var smiteAbility = AbilityConfigurator.New(
        "ChampionSmiteAbility", Guids.ChampionSmiteAbility)
        .SetDisplayName("ChampionSmite.Name")
        .SetDescription("ChampionSmite.Description")
        .SetIcon(smiteIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Long)
        .SetActionType(UnitCommand.CommandType.Swift)
        .AllowTargeting(enemies: true)
        .SetEffectOnEnemy(AbilityEffectOnUnit.Harmful)
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(smiteAction).Build())
        .Configure();
      var smiteFeature = FeatureConfigurator.New(
        "ChampionSmiteFeature", Guids.ChampionSmiteFeature)
        .SetDisplayName("ChampionSmite.Name")
        .SetDescription("ChampionSmite.Description")
        .SetIcon(smiteIcon)
        .SetIsClassFeature()
        .AddFacts(new() { smiteAbility })
        .AddAbilityResources(resource: smiteResource, restoreAmount: true)
        .Configure();
      // Extra uses: +1 at 8th, 12th, 16th, 20th.
      var extraGuids = new[]
      {
        Guids.ChampionSmiteExtra8, Guids.ChampionSmiteExtra12,
        Guids.ChampionSmiteExtra16, Guids.ChampionSmiteExtra20,
      };
      var extras = new Kingmaker.Blueprints.Classes.BlueprintFeature[4];
      for (var i = 0; i < 4; i++)
      {
        extras[i] = FeatureConfigurator.New(
          "ChampionSmiteExtra" + new[] { 8, 12, 16, 20 }[i], extraGuids[i])
          .SetIsClassFeature()
          .AddIncreaseResourceAmount(
            resource: smiteResource.Cast<BlueprintAbilityResourceReference>(), value: 1)
          .Configure();
      }

      // ----- Align Weapon (12th): four enchant buffs + the picker. -----
      var enchantRefs = new[]
      {
        WeaponEnchantmentRefs.Holy, WeaponEnchantmentRefs.Unholy,
        WeaponEnchantmentRefs.Axiomatic, WeaponEnchantmentRefs.Anarchic,
      };
      var alignBuffGuids = new[]
      {
        Guids.ChampionAlignGoodBuff, Guids.ChampionAlignEvilBuff,
        Guids.ChampionAlignLawBuff, Guids.ChampionAlignChaosBuff,
      };
      var alignBuffs = new BlueprintBuff[4];
      for (var i = 0; i < 4; i++)
      {
        alignBuffs[i] = BuffConfigurator.New(
          "ChampionAlign" + new[] { "Good", "Evil", "Law", "Chaos" }[i] + "Buff",
          alignBuffGuids[i])
          .SetDisplayName("ChampionAlignWeapon.Name")
          .SetDescription("ChampionAlignWeapon.Description")
          .SetIcon(alignIcon)
          .SetIsClassFeature()
          .AddBuffEnchantAnyWeapon(
            enchantmentBlueprint: enchantRefs[i].Cast<BlueprintItemEnchantmentReference>())
          .Configure();
      }
      var alignResource = AbilityResourceConfigurator.New(
        "ChampionAlignResource", Guids.ChampionAlignResource)
        .SetMax(1)
        .Configure();
      var alignAction = new ChampionAlignWeaponAction
      {
        AlignBuffs = alignBuffs,
        GoodFeature = good,
        EvilFeature = evil,
        LawFeature = law,
        ChaosFeature = chaos,
      };
      var alignAbility = AbilityConfigurator.New(
        "ChampionAlignWeaponAbility", Guids.ChampionAlignWeaponAbility)
        .SetDisplayName("ChampionAlignWeapon.Name")
        .SetDescription("ChampionAlignWeapon.Description")
        .SetIcon(alignIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Swift)
        .SetCanTargetSelf()
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(alignAction).Build())
        .Configure();
      var alignFeature = FeatureConfigurator.New(
        "ChampionAlignWeaponFeature", Guids.ChampionAlignWeaponFeature)
        .SetDisplayName("ChampionAlignWeapon.Name")
        .SetDescription("ChampionAlignWeapon.Description")
        .SetIcon(alignIcon)
        .SetIsClassFeature()
        .AddFacts(new() { alignAbility })
        .AddAbilityResources(resource: alignResource, restoreAmount: true)
        .Configure();
      var alignExtra16 = FeatureConfigurator.New(
        "ChampionAlignExtra16", Guids.ChampionAlignExtra16)
        .SetIsClassFeature()
        .AddIncreaseResourceAmount(
          resource: alignResource.Cast<BlueprintAbilityResourceReference>(), value: 1)
        .Configure();
      var alignExtra20 = FeatureConfigurator.New(
        "ChampionAlignExtra20", Guids.ChampionAlignExtra20)
        .SetIsClassFeature()
        .AddIncreaseResourceAmount(
          resource: alignResource.Cast<BlueprintAbilityResourceReference>(), value: 1)
        .Configure();

      // ----- The archetype. -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.ChampionOfTheFaithArchetype, CharacterClassRefs.WarpriestClass)
          .SetLocalizedName("ChampionOfTheFaith.Name")
          .SetLocalizedDescription("ChampionOfTheFaith.Description")
          .AddToAddFeatures(LevelPlan.L(1), selection)
          .AddToAddFeatures(LevelPlan.L(3), detectFeature)
          .AddToAddFeatures(LevelPlan.L(4), smiteFeature)
          .AddToAddFeatures(LevelPlan.L(8), extras[0])
          .AddToAddFeatures(LevelPlan.L(12), extras[1])
          .AddToAddFeatures(LevelPlan.L(12), alignFeature)
          .AddToAddFeatures(LevelPlan.L(16), extras[2])
          .AddToAddFeatures(LevelPlan.L(16), alignExtra16)
          .AddToAddFeatures(LevelPlan.L(20), extras[3])
          .AddToAddFeatures(LevelPlan.L(20), alignExtra20);
      // The trades: the 3rd-level bonus feat, every channel grant, and
      // (attempted) the sacred-weapon enhancement upgrades.
      archetype = ArchetypeRemovals.RemoveAtLevel(
        archetype, warpriest.Progression, 3,
        FeatureSelectionRefs.WarpriestFeatSelection.ToString());
      archetype = ArchetypeRemovals.RemoveEveryGrant(
        archetype, warpriest,
        FeatureSelectionRefs.WarpriestChannelEnergySelection.ToString());
      archetype = ArchetypeRemovals.RemoveEveryGrant(
        archetype, warpriest, "SacredWeaponEnhancement");
      archetype.Configure();
      MissionFeats.Logger.Info("[champion] configured: " + ArchetypeName + ".");
    }

    /// <summary>
    /// The champion's alignment axis, read from his chosen-alignment
    /// features at runtime (the Doomsayer FinalVerdict idiom).
    /// </summary>
    internal static AlignmentMaskType OpposedMask(
      UnitEntityData unit,
      BlueprintFeature good, BlueprintFeature evil,
      BlueprintFeature law, BlueprintFeature chaos)
    {
      if (unit.HasFact(evil))
      {
        return AlignmentMaskType.Good;
      }
      if (unit.HasFact(good))
      {
        return AlignmentMaskType.Evil;
      }
      if (unit.HasFact(law))
      {
        return AlignmentMaskType.Chaotic;
      }
      if (unit.HasFact(chaos))
      {
        return AlignmentMaskType.Lawful;
      }
      return AlignmentMaskType.None;
    }

    /// <summary>Does the unit bear the given alignment component?</summary>
    internal static bool BearsComponent(UnitEntityData unit, AlignmentMaskType component)
    {
      try
      {
        var value = unit?.Descriptor?.Alignment?.Value ?? AlignmentMaskType.None;
        return (value & component) != 0;
      }
      catch
      {
        return false;
      }
    }
  }

  /// <summary>
  /// Detect Alignment: a focused read of one creature against the
  /// champion's opposed alignment (the tabletop's 3-round-strength
  /// detail simplified to a plain yes/no).
  /// </summary>
  [TypeId(Guids.ChampionDetectAction)]
  internal class ChampionDetectAction : ContextAction
  {
    public BlueprintFeature GoodFeature;
    public BlueprintFeature EvilFeature;
    public BlueprintFeature LawFeature;
    public BlueprintFeature ChaosFeature;

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        var target = Target.Unit;
        if (caster is null || target is null)
        {
          return;
        }
        var opposed = ChampionOfTheFaith.OpposedMask(
          caster, GoodFeature, EvilFeature, LawFeature, ChaosFeature);
        if (opposed == AlignmentMaskType.None)
        {
          return;
        }
        var bears = ChampionOfTheFaith.BearsComponent(target, opposed);
        var axis = opposed switch
        {
          AlignmentMaskType.Good => "good",
          AlignmentMaskType.Evil => "evil",
          AlignmentMaskType.Lawful => "law",
          _ => "chaos",
        };
        CombatLog.Write(bears
          ? $"{target.CharacterName} bears the taint of {axis}."
          : $"{target.CharacterName} bears no trace of {axis}.", caster);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[champion] detect alignment failed.", e);
      }
    }

    public override string GetCaption() => "Detect Alignment";
  }

  /// <summary>
  /// Smite: sweeps any earlier mark (one smitten foe at a time; the
  /// Dreadnaught one-condemned sweep, with its documented cross-champion
  /// edge), spends a daily use, marks the target, and lights the wrath.
  /// </summary>
  [TypeId(Guids.ChampionSmiteAction)]
  internal class ChampionSmiteAction : ContextAction
  {
    public BlueprintBuff MarkBuff;
    public BlueprintBuff SmiteBuff;
    public BlueprintAbilityResource SmiteResource;

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        var target = Target.Unit;
        if (caster is null || target is null || target.HPLeft <= 0)
        {
          return;
        }
        if (caster.Resources.GetResourceAmount(SmiteResource) <= 0)
        {
          return;
        }
        using (var enumerator = Kingmaker.Game.Instance.State.Units.GetEnumerator())
        {
          while (enumerator.MoveNext())
          {
            var unit = enumerator.Current;
            unit?.Buffs.RemoveFact(MarkBuff);
          }
        }
        caster.Resources.Spend(SmiteResource, 1);
        target.AddBuff(MarkBuff, Context,
          duration: ContextDuration.Fixed(100).Calculate(Context).Seconds);
        caster.AddBuff(SmiteBuff, Context, TimeSpan.FromSeconds(60));
        MissionFeats.Logger.Info(
          $"[champion] {caster.CharacterName} smites {target.CharacterName}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[champion] smite failed.", e);
      }
    }

    public override string GetCaption() => "Smite";
  }

  /// <summary>
  /// The mark's teeth, riding the marked target (the Dreadnaught mark
  /// idiom): the champion's attacks against this creature deal bonus
  /// damage equal to his warpriest level while it bears his opposed
  /// alignment. (The tabletop's outsider doubling and DR bypass are
  /// documented cuts.)
  /// </summary>
  [TypeId(Guids.ChampionSmiteRiderComponent)]
  internal class ChampionSmiteRider : UnitFactComponentDelegate,
    ITargetRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    ITargetRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass WarpriestClass;
    public BlueprintFeature GoodFeature;
    public BlueprintFeature EvilFeature;
    public BlueprintFeature LawFeature;
    public BlueprintFeature ChaosFeature;

    public void OnEventAboutToTrigger(RulePrepareDamage evt) { }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        var champion = Context?.MaybeCaster;
        if (champion is null || evt.Initiator != champion ||
          evt.DamageBundle?.Weapon is null)
        {
          return;
        }
        var opposed = ChampionOfTheFaith.OpposedMask(
          champion, GoodFeature, EvilFeature, LawFeature, ChaosFeature);
        if (opposed == AlignmentMaskType.None ||
          !ChampionOfTheFaith.BearsComponent(Owner, opposed))
        {
          return;
        }
        int level = champion.Descriptor.Progression.GetClassLevel(WarpriestClass);
        if (level > 0)
        {
          evt.Add(new DirectDamage(DiceFormula.Zero, level) { SourceFact = Fact });
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[champion] smite rider failed.", e);
      }
    }
  }

  /// <summary>
  /// Align Weapon: infuses the champion's weapon with his chosen
  /// alignment for 1 minute - the buff is picked from his chosen
  /// features (holy, unholy, axiomatic, or anarchic).
  /// </summary>
  [TypeId(Guids.ChampionAlignWeaponAction)]
  internal class ChampionAlignWeaponAction : ContextAction
  {
    public BlueprintBuff[] AlignBuffs;
    public BlueprintFeature GoodFeature;
    public BlueprintFeature EvilFeature;
    public BlueprintFeature LawFeature;
    public BlueprintFeature ChaosFeature;

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        if (caster is null)
        {
          return;
        }
        BlueprintBuff picked = null;
        if (caster.HasFact(GoodFeature))
        {
          picked = AlignBuffs[0];
        }
        else if (caster.HasFact(EvilFeature))
        {
          picked = AlignBuffs[1];
        }
        else if (caster.HasFact(LawFeature))
        {
          picked = AlignBuffs[2];
        }
        else if (caster.HasFact(ChaosFeature))
        {
          picked = AlignBuffs[3];
        }
        if (picked is null)
        {
          MissionFeats.Logger.Warn("[champion] align weapon: no chosen alignment.");
          return;
        }
        caster.AddBuff(picked, Context, TimeSpan.FromSeconds(60));
        MissionFeats.Logger.Info("[champion] the weapon is aligned.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[champion] align weapon failed.", e);
      }
    }

    public override string GetCaption() => "Align Weapon";
  }
}

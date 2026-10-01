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
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
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
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.49.0 — the Elementalist Shifter (shifter archetype, Ultimate
  /// Wilderness pg. 78). The final class round's port (the user's pick
  /// of what to homebrew comes later). "Rather than drawing power from
  /// bestial aspects, elementalist shifters channel power from the
  /// Inner Sphere... the planes of Air, Earth, Fire, and Water."
  ///
  /// THE GAIN:
  /// - Elemental Aspect (1st, then 5th/10th/15th): a selection of four
  ///   REAL elemental aspects. Each grants its minor form as an
  ///   always-on passive (Air/Fire +2 Dex, Earth +2 Con, Water +2 Str;
  ///   +4 at 8th, +6 at 15th - the class-level rank-config idiom) AND
  ///   its Elemental Strike.
  /// - Elemental Strike (1st, replaces shifter claws): a swift action
  ///   charging the shifter's melee attacks with elemental energy for
  ///   1 round: +1d6, +1d6 per 4 levels (6d6 at 20th). Air =
  ///   electricity, Earth = acid, Fire = fire, Water = cold. Only the
  ///   elements she has chosen (each strike ability is granted BY its
  ///   aspect feature, so it cannot exist unchosen).
  /// - Elemental Form (4th, replaces wild shape): assume elemental
  ///   form - the REAL elemental body I spells (Air/Earth/Fire/Water),
  ///   cast at will as a standard action. Each form ability is
  ///   restricted to casters who own the matching aspect (the
  ///   engine's own AbilityCasterHasFacts restriction).
  ///
  /// THE TRADE (live-progression scans): the shifter aspect selection
  /// (every grant - which carries the animal aspects and their major
  /// forms away with it), the shifter claws line (all eleven feature
  /// names), chimeric aspect, and greater chimeric aspect.
  ///
  /// DOCUMENTED CUTS: Elemental Speech (tongues with matching
  /// elementals - no engine hook) and Omnielementalist's six weather
  /// auras (six bespoke area effects; noted as the obvious homebrew
  /// candidate for a future round). Wild empathy is kept (the
  /// tabletop's replacement for it was Elemental Speech). Languages
  /// are not modeled by this engine.
  ///
  /// ENGINE NOTES: the minor-form stat bonus is the tutorial's
  /// ContextRankConfigs.ClassLevel().WithCustomProgression() idiom;
  /// the strike rider is the Overchanneler's RulePrepareDamage watcher
  /// mirrored to weapon attacks (Weapon.Blueprint.IsMelee - the
  /// DarkCodex chain); EnergyDamage's (dice, bonus, energyType) ctor
  /// is probe-verified; the forms cast via the CastSpell builder (the
  /// Undead Master's Command-the-Dead idiom); the selection is the
  /// withdrawn Champion's FeatureSelectionConfigurator pattern.
  /// </summary>
  internal class ElementalistShifter
  {
    internal const string ArchetypeName = "ElementalistShifter";

    internal static void Configure()
    {
      var shifter = CharacterClassRefs.ShifterClass.Reference.Get();
      var rankClass = new string[] { CharacterClassRefs.ShifterClass.ToString() };

      // ----- The four elements. -----
      var elements = new[]
      {
        new {
          Key = "Air",   Stat = StatType.Dexterity,     Energy = DamageEnergyType.Electricity,
          Spell = AbilityRefs.ElementalBodyIAir,   Icon = AbilityRefs.LightningBolt,
        },
        new {
          Key = "Earth", Stat = StatType.Constitution,  Energy = DamageEnergyType.Acid,
          Spell = AbilityRefs.ElementalBodyIEarth, Icon = AbilityRefs.AcidArrow,
        },
        new {
          Key = "Fire",  Stat = StatType.Dexterity,     Energy = DamageEnergyType.Fire,
          Spell = AbilityRefs.ElementalBodyIFire,  Icon = AbilityRefs.Fireball,
        },
        new {
          Key = "Water", Stat = StatType.Strength,      Energy = DamageEnergyType.Cold,
          Spell = AbilityRefs.ElementalBodyIWater, Icon = AbilityRefs.IceStorm,
        },
      };

      var aspects = new BlueprintFeature[4];
      var strikeAbilities = new BlueprintAbility[4];
      var aspectGuids = new[]
      {
        Guids.ElementalistAspectAirFeature, Guids.ElementalistAspectEarthFeature,
        Guids.ElementalistAspectFireFeature, Guids.ElementalistAspectWaterFeature,
      };
      var strikeBuffGuids = new[]
      {
        Guids.ElementalistStrikeAirBuff, Guids.ElementalistStrikeEarthBuff,
        Guids.ElementalistStrikeFireBuff, Guids.ElementalistStrikeWaterBuff,
      };
      var strikeAbilityGuids = new[]
      {
        Guids.ElementalistStrikeAirAbility, Guids.ElementalistStrikeEarthAbility,
        Guids.ElementalistStrikeFireAbility, Guids.ElementalistStrikeWaterAbility,
      };
      var formAbilityGuids = new[]
      {
        Guids.ElementalistFormAirAbility, Guids.ElementalistFormEarthAbility,
        Guids.ElementalistFormFireAbility, Guids.ElementalistFormWaterAbility,
      };

      for (var i = 0; i < elements.Length; i++)
      {
        var e = elements[i];

        // ----- The strike buff: 1 round of charged melee. -----
        var buff = BuffConfigurator.New("ElementalistStrike" + e.Key + "Buff",
            strikeBuffGuids[i])
          .SetDisplayName("ElementalistStrike.Name")
          .SetDescription("ElementalistStrike.Description")
          .SetIcon(e.Icon.Reference.Get().Icon)
          .SetIsClassFeature()
          .AddComponent(new ElementalistStrikeRider
          {
            ShifterClass = shifter,
            Energy = e.Energy,
          })
          .Configure();

        // ----- The strike ability (swift; granted BY its aspect, so it
        // cannot exist without the chosen element). -----
        strikeAbilities[i] = AbilityConfigurator.New(
            "ElementalistStrike" + e.Key + "Ability", strikeAbilityGuids[i])
          .SetDisplayName("ElementalistStrike.Name")
          .SetDescription("ElementalistStrike.Description")
          .SetIcon(e.Icon.Reference.Get().Icon)
          .SetType(AbilityType.Special)
          .SetRange(AbilityRange.Personal)
          .SetActionType(UnitCommand.CommandType.Swift)
          .SetCanTargetSelf()
          .AddAbilityEffectRunAction(ActionsBuilder.New().Add(new ElementalistStrikeAction
          {
            Buff = buff,
          }).Build())
          .Configure();

        // ----- The aspect feature: minor-form passive + its strike. -----
        aspects[i] = FeatureConfigurator.New(
            "ElementalistAspect" + e.Key + "Feature", aspectGuids[i])
          .SetDisplayName("ElementalistAspect" + e.Key + ".Name")
          .SetDescription("ElementalistAspect" + e.Key + ".Description")
          .SetIcon(e.Spell.Reference.Get().Icon)
          .SetIsClassFeature()
          .AddContextStatBonus(e.Stat, ContextValues.Rank(), ModifierDescriptor.Enhancement)
          .AddContextRankConfig(
            ContextRankConfigs.ClassLevel(rankClass).WithCustomProgression((1, 2), (8, 4), (15, 6)))
          .AddFacts(new() { strikeAbilities[i] })
          .Configure();
      }

      // ----- Elemental Form (4th): the REAL elemental body I spells. -----
      var formAbilities = new BlueprintAbility[4];
      for (var i = 0; i < elements.Length; i++)
      {
        var e = elements[i];
        formAbilities[i] = AbilityConfigurator.New(
            "ElementalistForm" + e.Key + "Ability", formAbilityGuids[i])
          .SetDisplayName("ElementalistForm.Name")
          .SetDescription("ElementalistForm.Description")
          .SetIcon(e.Spell.Reference.Get().Icon)
          .SetType(AbilityType.Special)
          .SetRange(AbilityRange.Personal)
          .SetActionType(UnitCommand.CommandType.Standard)
          .SetCanTargetSelf()
          .AddAbilityCasterHasFacts(new System.Collections.Generic.List<Blueprint<BlueprintUnitFactReference>>
          {
            aspects[i],
          })
          .AddAbilityEffectRunAction(ActionsBuilder.New().CastSpell(
            e.Spell.Cast<BlueprintAbilityReference>()).Build())
          .Configure();
      }
      var form = FeatureConfigurator.New("ElementalistFormFeature", Guids.ElementalistFormFeature)
        .SetDisplayName("ElementalistForm.Name")
        .SetDescription("ElementalistForm.Description")
        .SetIcon(AbilityRefs.ElementalBodyIBase.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddFacts(new() { formAbilities[0], formAbilities[1], formAbilities[2], formAbilities[3] })
        .Configure();

      // ----- The aspect selection: 1st, then 5th/10th/15th. -----
      var selection = FeatureSelectionConfigurator.New(
        "ElementalistAspectSelection", Guids.ElementalistAspectSelection)
        .SetDisplayName("ElementalistAspect.Name")
        .SetDescription("ElementalistAspect.Description")
        .SetIcon(AbilityRefs.ElementalBodyIBase.Reference.Get().Icon)
        .SetIsClassFeature()
        .SetMode(SelectionMode.Default)
        .Configure();
      FeatureSelectionConfigurator.For(selection)
        .AddToAllFeatures(
          aspectGuids[0], aspectGuids[1], aspectGuids[2], aspectGuids[3])
        .Configure();

      // ----- The archetype. -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.ElementalistShifterArchetype, CharacterClassRefs.ShifterClass)
          .SetLocalizedName("ElementalistShifter.Name")
          .SetLocalizedDescription("ElementalistShifter.Description")
          .AddToAddFeatures(LevelPlan.L(1), selection)
          .AddToAddFeatures(LevelPlan.L(4), form)
          .AddToAddFeatures(LevelPlan.L(5), selection)
          .AddToAddFeatures(LevelPlan.L(9), omnielementalist)
          .AddToAddFeatures(LevelPlan.L(10), selection)
          .AddToAddFeatures(LevelPlan.L(15), selection)
          .AddToAddFeatures(LevelPlan.L(15), omnielementalist);
      // The trades: the aspect selection (every grant - the animal
      // aspects and their major forms go with it), the claws line,
      // chimeric aspect, and greater chimeric aspect.
      archetype = ArchetypeRemovals.RemoveEveryGrant(
        archetype, shifter, FeatureSelectionRefs.ShifterAspectSelectionFeature.ToString());
      var clawsNames = new[]
      {
        "ShifterClawsFeatureLevel1", "ShifterClawsFeatureLevel11", "ShifterClawsFeatureLevel13",
        "ShifterClawsFeatureLevel17", "ShifterClawsFeatureLevel19", "ShifterClawsFeatureAddLevel",
        "ShifterClawsFeatureAddLevel1", "ShifterClawsFeatureAddLevel2", "ShifterClawsFeatureAddLevel3",
        "ShifterClawsFeatureAddLevel4", "ShifterClawsFeatureAddLevel5", "ShifterClawsFeatureAddLevel6",
      };
      foreach (var claws in clawsNames)
      {
        archetype = ArchetypeRemovals.RemoveEveryGrant(archetype, shifter, claws);
      }
      archetype = ArchetypeRemovals.RemoveEveryGrant(
        archetype, shifter, FeatureRefs.ChimericAspectFeature.ToString());
      archetype = ArchetypeRemovals.RemoveEveryGrant(
        archetype, shifter, FeatureRefs.GreaterChimericAspectFeature.ToString());
      archetype.Configure();
      MissionFeats.Logger.Info("[elementalist] configured: " + ArchetypeName + ".");
    }
  }

  /// <summary>
  /// Elemental Strike: one swift action, one round of charged melee.
  /// (The Crescendo/Inkbound apply-buff idiom.)
  /// </summary>
  [TypeId(Guids.ElementalistStrikeAction)]
  internal class ElementalistStrikeAction : ContextAction
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
        caster.AddBuff(Buff, Context, TimeSpan.FromSeconds(6));
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[elementalist] strike failed.", e);
      }
    }

    public override string GetCaption() => "Elemental Strike";
  }

  /// <summary>
  /// The charged-melee rider: while the strike buff lasts, the
  /// shifter's melee weapon attacks deal bonus energy dice (the
  /// Overchanneler's RulePrepareDamage watcher, mirrored from spells
  /// to weapons). +1d6 at 1st, +1d6 per 4 levels, 6d6 at 20th.
  /// </summary>
  [TypeId(Guids.ElementalistStrikeRider)]
  internal class ElementalistStrikeRider : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass ShifterClass;
    public DamageEnergyType Energy;

    public void OnEventAboutToTrigger(RulePrepareDamage evt) { }

    public void OnEventDidTrigger(RulePrepareDamage evt)
    {
      try
      {
        // Melee weapon attacks only - the tabletop charges melee, and
        // the IsMelee read is the DarkCodex chain.
        if (evt.Initiator != Owner ||
          evt.DamageBundle?.Weapon?.Blueprint?.IsMelee != true)
        {
          return;
        }
        var level = Owner.Descriptor.Progression.GetClassLevel(ShifterClass);
        var dice = Math.Min(6, 1 + level / 4);
        evt.Add(new EnergyDamage(new DiceFormula(dice, DiceType.D6), 0, Energy)
        {
          SourceFact = Fact,
        });
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[elementalist] strike rider failed.", e);
      }
    }
  }

  /// <summary>
  /// Enters an elemental fusion: applies its buff for 1 minute (the
  /// tabletop's "while she maintains the forms" becomes an at-will
  /// swift-action stance).
  /// </summary>
  [TypeId(Guids.ElementalistFusionAction)]
  internal class ElementalistFusionAction : ContextAction
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
        MissionFeats.Logger.Error("[elementalist] fusion failed.", e);
      }
    }

    public override string GetCaption() => "Elemental Fusion";
  }

  /// <summary>
  /// Sandstorm's round-tick: each round, enemies within 20 feet of the
  /// shifter take 1d6 damage (the ITickEachRound interface, the
  /// DarkCodex BleedBuff idiom; the sweep is the Mending Blade idiom).
  /// The tabletop's nonlethal/light-dimming is a documented cut.
  /// </summary>
  [TypeId(Guids.ElementalistSandstormRider)]
  internal class ElementalistSandstormRider : UnitBuffComponentDelegate, ITickEachRound
  {
    private static readonly float RadiusMeters = new Feet(20).Meters;

    public void OnNewRound()
    {
      try
      {
        using (var enumerator = Kingmaker.Game.Instance.State.Units.GetEnumerator())
        {
          while (enumerator.MoveNext())
          {
            var unit = enumerator.Current;
            if (unit is null || unit.Descriptor.State.IsDead ||
              unit.IsPlayerFaction ||
              Owner.DistanceTo(unit) > RadiusMeters)
            {
              continue;
            }
            var bundle = new DamageBundle();
            bundle.Add(new DirectDamage(new DiceFormula(1, DiceType.D6), 0));
            Rulebook.Trigger(new RuleDealDamage(Owner, unit, bundle) { Reason = Fact });
          }
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[elementalist] sandstorm tick failed.", e);
      }
    }
  }

  /// <summary>
  /// Volcanic Stride's heat: her melee weapon attacks deal +1d6 fire
  /// while the fusion lasts (the strike-rider pattern, flat dice).
  /// </summary>
  [TypeId(Guids.ElementalistVolcanicRider)]
  internal class ElementalistVolcanicRider : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
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
        evt.Add(new EnergyDamage(new DiceFormula(1, DiceType.D6), 0, DamageEnergyType.Fire)
        {
          SourceFact = Fact,
        });
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[elementalist] volcanic stride failed.", e);
      }
    }
  }
}

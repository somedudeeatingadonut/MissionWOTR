using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.Configurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Conditions.Builder;
using BlueprintCore.Conditions.Builder.ContextEx;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.38.0 — the Spell Warrior (skald archetype, Advanced Class Guide pg. 116).
  ///
  /// "The spell warrior uses his arcane knowledge rather than his rage to turn
  /// the tide of battle in favor of himself and his allies. With a clash of
  /// bracers and a sonorous chant, the Spell Warrior's song reaches out to
  /// touch the weapons of his allies, lending them arcane power."
  ///
  /// THE TRADES (found in the live progression by scan, via ArchetypeRemovals):
  /// - Inspired Rage: the rage-granting song is gone.
  /// - Raging Song (the vanilla activate button + its rounds grant): removed
  ///   too — with no songs left to sing it would be a dead (and crash-prone)
  ///   button. The rounds pool itself is re-granted by our feature.
  /// - Dirge of Doom, Master Skald: removed.
  /// - Scribe Scroll and Spell Kenning do not exist in WOTR (TabletopTweaks
  ///   adds the latter as its own content) — nothing to remove.
  ///
  /// THE GAIN:
  /// - Enhance Weapons (1st): a standard-action raging song that enchants the
  ///   weapons of all allies within 60 feet — +1 at 1st, +1 more at 5th and
  ///   every 5 levels thereafter (max +5 at 20th). The bonus OVERLAPS (does
  ///   not stack with) existing weapon enhancement — the engine's own
  ///   enhancement rules — so it shines brightest on unenhanced steel:
  ///   early weapons, summoned allies, animal companions. Every round it
  ///   plays drains one raging-song round; when the pool runs dry the song
  ///   ends itself.
  ///
  /// ENGINE NOTES:
  /// - The ally enchantment rides the game's own BuffEnchantAnyWeapon
  ///   component — the same one the Aasimar Red Mask and Inquisitor Liotr's
  ///   greater-magic-weapon buffs use — with the VANILLA +1..+5 weapon
  ///   enchantment blueprints. No new enchantment content.
  /// - The 60-foot radius rides the game's area-effect system (the same
  ///   mechanism the Doomsayer's dread mien auras use): the area follows the
  ///   singer, allies entering late are enchanted too, and everything cleans
  ///   itself up when the song ends.
  /// - The rounds economy is our own tiny ITickEachRound component (the
  ///   Spirit-Ridden form-tick idiom): spend one round per round, end the
  ///   song at zero.
  ///
  /// DOCUMENTED SCOPE CUTS (the low-cut port rule):
  /// - The tabletop's counterspell line (Improved Counterspell 1st, Greater
  ///   Counterspell 5th/11th/17th, Parry Spell 17th) is cut entirely: WOTR
  ///   has no counterspelling mechanic at all.
  /// - The "bonus by number of weapons affected" limiter (+5 one weapon /
  ///   +4 two / +3 three / +2 four+) is cut: counting live weapons per ally
  ///   is not worth the machinery; the flat per-level scaling stands.
  /// - The weapon special-ability picks (flaming, keen, speed...) are cut
  ///   for v1: they need a selection UI at song start.
  /// - The "wielder counts as under inspired rage for rage powers" rider is
  ///   cut (the skald's remaining rage powers simply have no song to ride).
  /// </summary>
  internal class SpellWarrior
  {
    internal const string ArchetypeName = "SpellWarrior";

    internal static void Configure()
    {
      var skald = CharacterClassRefs.SkaldClass.Reference.Get();
      var icon = AbilityRefs.MagicWeapon.Reference.Get().Icon;
      var rounds = AbilityResourceRefs.RagingSongResource.Reference.Get();

      // ----- The five ally buffs: one per enhancement tier. Each enchants
      // every weapon its holder wields with the VANILLA +N enhancement
      // enchantment (BuffEnchantAnyWeapon - the component the game's own
      // NPC greater-magic-weapon buffs use). -----
      var enchantments = new[]
      {
        WeaponEnchantmentRefs.Enhancement1,
        WeaponEnchantmentRefs.Enhancement2,
        WeaponEnchantmentRefs.Enhancement3,
        WeaponEnchantmentRefs.Enhancement4,
        WeaponEnchantmentRefs.Enhancement5,
      };
      var allyBuffGuids = new[]
      {
        Guids.SpellWarriorSongAllyBuff1, Guids.SpellWarriorSongAllyBuff2,
        Guids.SpellWarriorSongAllyBuff3, Guids.SpellWarriorSongAllyBuff4,
        Guids.SpellWarriorSongAllyBuff5,
      };
      var allyBuffs = new BlueprintBuff[5];
      for (var i = 0; i < 5; i++)
      {
        allyBuffs[i] = BuffConfigurator.New(
          "SpellWarriorSongAllyBuff" + (i + 1), allyBuffGuids[i])
          .SetDisplayName("SpellWarriorSong.Name")
          .SetDescription("SpellWarriorSong.Description")
          .SetIcon(icon)
          .SetIsClassFeature()
          .AddBuffEnchantAnyWeapon(
            enchantmentBlueprint: enchantments[i].Cast<BlueprintItemEnchantmentReference>())
          .Configure();
      }
      // NOTE: the guid consts are declared in order
      // (SongAllyBuff1..SongAllyBuff5), so +i walks them.

      // ----- The five areas: 60-foot cylinders that follow the singer and
      // enchant every ally inside (the Doomsayer dread-mien pattern, with
      // the ally condition instead of the enemy one). -----
      var areaGuids = new[]
      {
        Guids.SpellWarriorSongArea1, Guids.SpellWarriorSongArea2,
        Guids.SpellWarriorSongArea3, Guids.SpellWarriorSongArea4,
        Guids.SpellWarriorSongArea5,
      };
      var areas = new BlueprintAbilityAreaEffect[5];
      for (var i = 0; i < 5; i++)
      {
        areas[i] = AbilityAreaEffectConfigurator.New(
          "SpellWarriorSongArea" + (i + 1), areaGuids[i])
          .AddAbilityAreaEffectBuff(
            buff: allyBuffs[i],
            condition: ConditionsBuilder.New().IsAlly())
          .SetSize(new(60))
          .SetShape(AreaEffectShape.Cylinder)
          .Configure();
      }

      // ----- The five song buffs (the "singing" state on the skald):
      // rounds economy + the area. -----
      var songBuffGuids = new[]
      {
        Guids.SpellWarriorSongBuff1, Guids.SpellWarriorSongBuff2,
        Guids.SpellWarriorSongBuff3, Guids.SpellWarriorSongBuff4,
        Guids.SpellWarriorSongBuff5,
      };
      var songBuffs = new BlueprintBuff[5];
      for (var i = 0; i < 5; i++)
      {
        songBuffs[i] = BuffConfigurator.New(
          "SpellWarriorSongBuff" + (i + 1), songBuffGuids[i])
          .SetDisplayName("SpellWarriorSong.Name")
          .SetDescription("SpellWarriorSong.Description")
          .SetIcon(icon)
          .SetIsClassFeature()
          .Configure();
      }
      // The rounds component needs its own blueprint as a field, so it is
      // added in a second pass (the FeatureSelectionConfigurator.For idiom).
      // The song's own lifetime is bounded by the ability's one-hour
      // application and cut short by the rounds component at zero.
      for (var i = 0; i < 5; i++)
      {
        BuffConfigurator.For(songBuffs[i])
          .AddAreaEffect(areaEffect: areas[i])
          .AddComponent(new SpellWarriorSongRounds
          {
            SelfBuff = songBuffs[i],
            RoundResource = rounds,
          })
          .Configure();
      }

      // ----- Enhance Weapons: the activate ability. -----
      var startSong = new SpellWarriorStartSongAction
      {
        SongBuffs = songBuffs,
        RoundResource = rounds,
        SkaldClass = skald,
      };
      var ability = AbilityConfigurator.New("SpellWarriorAbility", Guids.SpellWarriorAbility)
        .SetDisplayName("SpellWarriorEnhance.Name")
        .SetDescription("SpellWarriorEnhance.Description")
        .SetIcon(AbilityRefs.MagicWeaponGreater.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Standard)
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(startSong).Build())
        .Configure();

      // ----- The feature (1st): the song + the re-granted rounds pool. -----
      var feature = FeatureConfigurator.New("SpellWarriorFeature", Guids.SpellWarriorFeature)
        .SetDisplayName("SpellWarriorEnhance.Name")
        .SetDescription("SpellWarriorEnhance.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { ability })
        .AddAbilityResources(
          resource: AbilityResourceRefs.RagingSongResource, restoreAmount: true)
        .Configure();

      // ----- The archetype. -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.SpellWarriorArchetype, CharacterClassRefs.SkaldClass)
          .SetLocalizedName("SpellWarrior.Name")
          .SetLocalizedDescription("SpellWarrior.Description")
          .AddToAddFeatures(LevelPlan.L(1), feature);
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, skald,
        FeatureRefs.RagingSong.ToString(),
        FeatureRefs.InspiredRage.ToString(),
        FeatureRefs.DirgeOfDoom.ToString(),
        FeatureRefs.DirgeOfDoomFeature.ToString(),
        FeatureRefs.MasterSkald.ToString());
      archetype.Configure();
      MissionFeats.Logger.Info("[spellwarrior] configured: " + ArchetypeName + ".");
    }
  }

  /// <summary>
  /// Starts the weapon song: checks the raging-song pool, spends the first
  /// round, and applies the tier of song buff the skald has earned (+1 at
  /// 1st, +2 at 5th, +3 at 10th, +4 at 15th, +5 at 20th).
  /// </summary>
  [TypeId(Guids.SpellWarriorStartSongAction)]
  internal class SpellWarriorStartSongAction : ContextAction
  {
    public BlueprintBuff[] SongBuffs;
    public BlueprintAbilityResource RoundResource;
    public BlueprintCharacterClass SkaldClass;

    public override void RunAction()
    {
      try
      {
        var caster = Context?.MaybeCaster;
        if (caster is null)
        {
          MissionFeats.Logger.Warn("[spellwarrior] no caster in context.");
          return;
        }
        if (caster.Resources.GetResourceAmount(RoundResource) <= 0)
        {
          MissionFeats.Logger.Warn("[spellwarrior] no raging-song rounds left today.");
          return;
        }
        int level = caster.Descriptor.Progression.GetClassLevel(SkaldClass);
        var tier = level < 5 ? 0 : level < 10 ? 1 : level < 15 ? 2 : level < 20 ? 3 : 4;
        caster.Resources.Spend(RoundResource, 1);
        caster.AddBuff(SongBuffs[tier], Context, TimeSpan.FromHours(1));
        MissionFeats.Logger.Info(
          $"[spellwarrior] weapon song begins: tier +{tier + 1} at skald level {level}.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[spellwarrior] could not start the weapon song.", e);
      }
    }

    public override string GetCaption() => "Enhance Weapons";
  }

  /// <summary>
  /// The song's rounds economy: every round the song plays, one raging-song
  /// round drains; at zero the song ends itself. The ITickEachRound idiom
  /// proven by the Spirit-Ridden form component.
  /// </summary>
  [TypeId(Guids.SpellWarriorSongRounds)]
  internal class SpellWarriorSongRounds : UnitFactComponentDelegate,
    Kingmaker.Controllers.Units.ITickEachRound
  {
    public BlueprintBuff SelfBuff;
    public BlueprintAbilityResource RoundResource;

    public void OnNewRound()
    {
      try
      {
        if (Owner.Resources.GetResourceAmount(RoundResource) <= 0)
        {
          MissionFeats.Logger.Info("[spellwarrior] the weapon song ends: no rounds remain.");
          Owner.Buffs.RemoveFact(SelfBuff);
          return;
        }
        Owner.Resources.Spend(RoundResource, 1);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[spellwarrior] song round cost failed.", e);
      }
    }
  }
}

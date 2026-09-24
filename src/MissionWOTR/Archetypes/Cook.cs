using BlueprintCore.Actions.Builder;
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
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// The Cook (homebrew bard archetype, user-commissioned).
  ///
  /// One of the older meanings of "bard" is to barding - wrapping meat in bacon
  /// or fat to keep it moist. This bard took that definition personally. She has
  /// no performances, no bardic knowledge, no dirge of doom, no jack of all
  /// trades and no mass suggestion - she has a fire, a knife, and three meals a
  /// day to feed an army.
  ///
  /// - Hearty Cooking (1st): 3 meal charges per long rest. Each "Serve" ability
  ///   spends one charge and feeds EVERY ally within 30 feet a meal whose buff
  ///   lasts 8 hours and cannot be dispelled. Meals are a bit weaker than the
  ///   old single-serve ingredients (playtest rework: ingredients fed one ally;
  ///   meals feed the whole camp, so each serving is worth less per head).
  /// - Recipe Book (4th and every 4 levels): learn one new meal.
  ///   Starting menu: Bacon Wrap, Chicken Breast, Rice, Beans, Lettuce.
  ///   Recipe picks: Garlic, Chili Pepper, Cheese, Mushroom, Potato, Onion,
  ///   Coffee, Butter.
  ///
  /// Implementation notes: meal charges are an ability resource restored on rest
  /// (the EldritchPoisoner doses pattern); each meal buff is untyped (WOTR has no
  /// food-buff category; the native cooking-recipe party buffs are plain buffs
  /// too) and is flagged non-dispelable when served. Party-wide serving iterates
  /// Game.Instance.State.Units with IsAlly + DistanceTo (the Sanguine Font aura
  /// idiom). Values are flat + one rank per N bard levels (ContextRankConfig
  /// with StepLevel) - tuning candidates.
  /// </summary>
  internal static class Cook
  {
    internal const string ArchetypeName = "CookArchetype";
    internal const string DisplayName = "Cook.Name";
    internal const string Description = "Cook.Description";

    internal const string HeartyName = "CookHeartyCooking";
    internal const string ResourceName = "CookMealCharges";
    internal const string PantryName = "CookRecipeSelection";

    /// <summary>Meal buff duration: 8 hours.</summary>
    internal static readonly TimeSpan MealDuration = TimeSpan.FromHours(8.0);

    /// <summary>Meal key -> (feature, buff, serve ability) GUIDs.</summary>
    private static readonly Dictionary<string, string[]> GuidsByKey = new()
    {
      { "BaconWrap", new[] { Guids.CookIngredientBaconWrap, Guids.CookBuffBaconWrap, Guids.CookServeBaconWrap } },
      { "ChickenBreast", new[] { Guids.CookIngredientChickenBreast, Guids.CookBuffChickenBreast, Guids.CookServeChickenBreast } },
      { "Rice", new[] { Guids.CookIngredientRice, Guids.CookBuffRice, Guids.CookServeRice } },
      { "Beans", new[] { Guids.CookIngredientBeans, Guids.CookBuffBeans, Guids.CookServeBeans } },
      { "Lettuce", new[] { Guids.CookIngredientLettuce, Guids.CookBuffLettuce, Guids.CookServeLettuce } },
      { "Garlic", new[] { Guids.CookIngredientGarlic, Guids.CookBuffGarlic, Guids.CookServeGarlic } },
      { "ChiliPepper", new[] { Guids.CookIngredientChiliPepper, Guids.CookBuffChiliPepper, Guids.CookServeChiliPepper } },
      { "Cheese", new[] { Guids.CookIngredientCheese, Guids.CookBuffCheese, Guids.CookServeCheese } },
      { "Mushroom", new[] { Guids.CookIngredientMushroom, Guids.CookBuffMushroom, Guids.CookServeMushroom } },
      { "Potato", new[] { Guids.CookIngredientPotato, Guids.CookBuffPotato, Guids.CookServePotato } },
      { "Onion", new[] { Guids.CookIngredientOnion, Guids.CookBuffOnion, Guids.CookServeOnion } },
      { "Coffee", new[] { Guids.CookIngredientCoffee, Guids.CookBuffCoffee, Guids.CookServeCoffee } },
      { "Butter", new[] { Guids.CookIngredientButter, Guids.CookBuffButter, Guids.CookServeButter } },
    };

    public static void Configure()
    {
      var bard = CharacterClassRefs.BardClass.Reference.Get();

      // ----- Meal charges: 3 per long rest -----
      var charges = AbilityResourceConfigurator.New(ResourceName, Guids.CookMealCharges)
        .SetMax(3)
        .Configure();

      // ----- Ingredients: (name key, stat lines). Each line: stat, flat bonus,
      // one rank per Step levels (0 = flat only). -----
      // Meals (playtest rework): weaker per-head than the old single-serve
      // ingredients - party-wide servings, so the flat values drop and only a
      // few meals keep (slower) level scaling.
      var starters = new (string Key, (StatType Stat, int Flat, int Step)[] Stats)[]
      {
        ("BaconWrap", new[] { (StatType.HitPoints, 1, 4) }),
        ("ChickenBreast", new[] { (StatType.AdditionalAttackBonus, 1, 0) }),
        ("Rice", new[]
        {
          (StatType.SaveFortitude, 1, 0),
          (StatType.SaveReflex, 1, 0),
          (StatType.SaveWill, 1, 0),
        }),
        ("Beans", new[] { (StatType.Speed, 5, 0) }),
        ("Lettuce", new[] { (StatType.AC, 0, 12) }),
      };
      var recipePicks = new (string Key, (StatType Stat, int Flat, int Step)[] Stats)[]
      {
        ("Garlic", new[] { (StatType.SkillPersuasion, 2, 0) }),
        ("ChiliPepper", new[] { (StatType.AdditionalDamage, 1, 0) }),
        ("Cheese", new[]
        {
          (StatType.SkillKnowledgeArcana, 2, 0),
          (StatType.SkillLoreReligion, 2, 0),
          (StatType.SkillLoreNature, 2, 0),
          (StatType.SkillKnowledgeWorld, 2, 0),
        }),
        ("Mushroom", new[] { (StatType.Initiative, 2, 0) }),
        ("Potato", new[] { (StatType.SaveFortitude, 2, 0) }),
        ("Onion", new[] { (StatType.SkillPerception, 2, 0) }),
        ("Coffee", new[]
        {
          (StatType.Initiative, 2, 0),
          (StatType.Speed, 5, 0),
        }),
        ("Butter", new[]
        {
          (StatType.SaveFortitude, 1, 0),
          (StatType.SaveReflex, 1, 0),
          (StatType.SaveWill, 1, 0),
        }),
      };

      var starterFeatures = new List<BlueprintFeature>();
      foreach (var (key, stats) in starters)
      {
        starterFeatures.Add(BuildMeal(key, stats, bard, charges));
      }
      var recipeFeatures = new List<BlueprintFeature>();
      foreach (var (key, stats) in recipePicks)
      {
        recipeFeatures.Add(BuildMeal(key, stats, bard, charges));
      }

      // ----- Hearty Cooking (1st): charges + the starting pantry -----
      var hearty = FeatureConfigurator.New(HeartyName, Guids.CookHeartyCooking)
        .SetDisplayName("CookHeartyCooking.Name")
        .SetDescription("CookHeartyCooking.Description")
        .SetIcon(FeatureRefs.Toughness.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddAbilityResources(resource: charges, restoreAmount: true)
        .AddFacts(starterFeatures.Select(f => (Blueprint<BlueprintUnitFactReference>)f).ToList())
        .Configure();

      // ----- Recipe Book (4th and every 4 levels): learn a new meal -----
      var recipes = FeatureSelectionConfigurator.New(PantryName, Guids.CookPantrySelection)
        .SetDisplayName("CookPantry.Name")
        .SetDescription("CookPantry.Description")
        .SetIcon(FeatureRefs.Toughness.Reference.Get().Icon)
        .SetIsClassFeature()
        .SetAllFeatures(recipeFeatures.Select(f => (Blueprint<BlueprintFeatureReference>)f).ToArray())
        .Configure();

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.CookArchetype, CharacterClassRefs.BardClass)
          .SetLocalizedName(DisplayName)
          .SetLocalizedDescription(Description);

      // Trades everything performative: the performance kit, bardic knowledge,
      // dirge of doom, jack of all trades, and the mass-suggestion suite.
      archetype = ArchetypeRemovals.AddRemovals(
        archetype,
        bard,
        FeatureRefs.BardPerformance.ToString(),
        FeatureRefs.BardicPerformanceResourceFact.ToString(),
        FeatureRefs.BardMovePerformance.ToString(),
        FeatureRefs.BardSwiftPerformance.ToString(),
        FeatureRefs.DirgeOfDoomFeature.ToString(),
        FeatureRefs.BardicKnowledge.ToString(),
        FeatureRefs.BardJackOfAllTrades.ToString(),
        FeatureRefs.SoothingPerformanceFeature.ToString(),
        "MassSuggestion",
        "MassSuggestionFeature");

      archetype = archetype
        .AddToAddFeatures(LevelPlan.L(1), HeartyName)
        .AddToAddFeatures(LevelPlan.L(4), PantryName)
        .AddToAddFeatures(LevelPlan.L(8), PantryName)
        .AddToAddFeatures(LevelPlan.L(12), PantryName)
        .AddToAddFeatures(LevelPlan.L(16), PantryName)
        .AddToAddFeatures(LevelPlan.L(20), PantryName);

      archetype.Configure();

      MissionFeats.Logger.Info("Cook: configured.");
    }

    /// <summary>
    /// One meal: a meal buff (untyped, flat + one rank per step levels), a serve
    /// ability (standard action, one meal charge, feeds EVERY ally within 30
    /// feet, 8-hour non-dispelable buff), and the recipe feature carrying the
    /// ability.
    /// </summary>
    private static BlueprintFeature BuildMeal(
      string key, (StatType Stat, int Flat, int Step)[] stats,
      BlueprintCharacterClass bard, BlueprintAbilityResource charges)
    {
      var guids = GuidsByKey[key];
      var icon = GetIcon(key);

      var buffCfg = BuffConfigurator.New($"CookMealBuff{key}", guids[1])
        .SetDisplayName($"CookIngredient{key}.Name")
        .SetDescription($"CookIngredient{key}.Description")
        .SetIcon(icon)
        .SetIsClassFeature();
      int step = 0;
      foreach (var (stat, flat, s) in stats)
      {
        buffCfg = buffCfg.AddContextStatBonus(
          stat, ContextValues.Constant(flat), ModifierDescriptor.UntypedStackable);
        if (s > 0)
        {
          step = s; // ingredients use a single shared step
        }
      }
      if (step > 0)
      {
        buffCfg = buffCfg.AddContextRankConfig(
          ContextRankConfigs.ClassLevel(new[] { CharacterClassRefs.BardClass.ToString() })
            .WithDivStepProgression(step));
        foreach (var (stat, _, s) in stats)
        {
          if (s > 0)
          {
            buffCfg = buffCfg.AddContextStatBonus(
              stat, ContextValues.Rank(), ModifierDescriptor.UntypedStackable);
          }
        }
      }
      var buff = buffCfg.Configure();

      var serveAction = ElementTool.Create<CookServeMeal>();
      serveAction.Buff = buff;
      var serve = AbilityConfigurator.New($"CookServe{key}", guids[2])
        .SetDisplayName($"CookIngredient{key}.Name")
        .SetDescription($"CookIngredient{key}.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Standard)
        .AllowTargeting(self: true)
        .AddAbilityResourceLogic(requiredResource: charges, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(serveAction).Build())
        .Configure();

      return FeatureConfigurator.New($"CookMeal{key}", guids[0])
        .SetDisplayName($"CookIngredient{key}.Name")
        .SetDescription($"CookIngredient{key}.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { serve })
        .Configure();
    }

    private static UnityEngine.Sprite GetIcon(string key)
    {
      return key switch
      {
        "ChiliPepper" => AbilityRefs.BombStandart.Reference.Get().Icon,
        "ChickenBreast" => FeatureRefs.CriticalFocus.Reference.Get().Icon,
        "Lettuce" => FeatureRefs.CombatReflexes.Reference.Get().Icon,
        "Mushroom" => FeatureRefs.ImprovedSunder.Reference.Get().Icon,
        _ => FeatureRefs.Toughness.Reference.Get().Icon,
      };
    }
  }

  /// <summary>
  /// Serves a meal to the whole camp: every ally within 30 feet of the cook
  /// (herself included) gains the meal buff for 8 hours, non-dispelable - a
  /// good meal cannot be undone, only digested.
  /// </summary>
  [TypeId(Guids.CookServeMealAction)]
  internal class CookServeMeal : ContextAction
  {
    public BlueprintBuff Buff;

    public override string GetCaption() => "Serve Meal";

    public override void RunAction()
    {
      try
      {
        var caster = Context.MaybeCaster;
        if (caster is null || Buff is null)
        {
          return;
        }
        int fed = 0;
        foreach (var ally in SanguineFont.AlliesWithin(caster, 30))
        {
          var applied = ally.Descriptor.AddBuff(Buff, Context, Cook.MealDuration);
          if (applied != null)
          {
            applied.IsNotDispelable = true;
            fed++;
          }
        }
        MissionFeats.Logger.Info(
          $"[cook] served {Buff.name} to {fed} allies (8 hours).");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("Cook: serve meal failed.", e);
      }
    }
  }
}

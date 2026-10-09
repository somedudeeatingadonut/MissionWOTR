using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using MissionWOTR.Feats;
using System;
using UnityEngine;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// The Eldritch Poisoner's arcanotoxin discoveries (tabletop port) plus Careful Injection.
  ///
  /// REAL LEVELS (when LevelPlan.AllAtLevelOne is false): these are ordinary alchemist
  /// discovery picks, level-gated per the tabletop - Antidote/Apothecary/Combine/Envenom/
  /// Sickening any level, Contact 4th, Mind-Altering/Toxic Fumes 6th, Paralytic 8th,
  /// Lethal 10th. Careful Injection is granted by the archetype at level 4. In TEST MODE
  /// the archetype grants all of them at level 1.
  ///
  /// Adaptations (see docs/ARCHETYPES.md): Tailored Toxin is deferred (creature types are
  /// arbitrary blueprints in Wrath, not an enum); Toxic Fumes keeps the 2-round toxin
  /// duration; Antidote's 10th-level neutralize-poison upgrade is deferred; Careful
  /// Injection is a flat +2 DC on sneak attacks instead of per-die forgone damage.
  /// </summary>
  internal static class EldritchPoisonerDiscoveries
  {
    internal const string SickeningToxinFeatName = "EldritchPoisonerSickeningToxin";
    internal const string MindAlteringToxinFeatName = "EldritchPoisonerMindAlteringToxin";
    internal const string ParalyticToxinFeatName = "EldritchPoisonerParalyticToxin";
    internal const string LethalToxinFeatName = "EldritchPoisonerLethalToxin";
    internal const string CombineToxinsFeatName = "EldritchPoisonerCombineToxins";
    internal const string ContactToxinFeatName = "EldritchPoisonerContactToxin";
    internal const string ToxicFumesFeatName = "EldritchPoisonerToxicFumes";
    internal const string EnvenomFeatName = "EldritchPoisonerEnvenom";
    internal const string AntidoteFeatName = "EldritchPoisonerAntidote";
    internal const string ApothecaryFeatName = "EldritchPoisonerApothecary";
    internal const string CarefulInjectionFeatName = "EldritchPoisonerCarefulInjection";

    internal const string ContactThrowAbilityName = "EldritchPoisonerContactThrow";
    internal const string FumesThrowAbilityName = "EldritchPoisonerFumesThrow";
    internal const string EnvenomAllyAbilityName = "EldritchPoisonerEnvenomAlly";
    internal const string AntidoteAbilityName = "EldritchPoisonerAntidoteAbility";

    // Runtime fact checks (ArcanotoxinApply).
    internal static BlueprintFeature SickeningToxin;
    internal static BlueprintFeature MindAlteringToxin;
    internal static BlueprintFeature ParalyticToxin;
    internal static BlueprintFeature LethalToxin;
    internal static BlueprintFeature CombineToxins;
    internal static BlueprintFeature CarefulInjection;

    internal static string[] AllFeatNames =>
      new[]
      {
        SickeningToxinFeatName, MindAlteringToxinFeatName, ParalyticToxinFeatName,
        LethalToxinFeatName, CombineToxinsFeatName, ContactToxinFeatName,
        ToxicFumesFeatName, EnvenomFeatName, AntidoteFeatName, ApothecaryFeatName,
        CarefulInjectionFeatName,
      };

    internal static void Configure()
    {
      var bombIcon = AbilityRefs.BombStandart.Reference.Get().Icon;

      // --- Passive discoveries: pure feat blueprints; the delivery logic reads them. ---
      SickeningToxin = DiscoveryBase(SickeningToxinFeatName, Guids.SickeningToxinFeat,
        "SickeningToxin.Name", "SickeningToxin.Description",
        BuffRefs.Sickened.Reference.Get().Icon).Configure();

      MindAlteringToxin = DiscoveryBase(MindAlteringToxinFeatName, Guids.MindAlteringToxinFeat,
        "MindAlteringToxin.Name", "MindAlteringToxin.Description",
        BuffRefs.DazzledBuff.Reference.Get().Icon, requiredLevel: 6).Configure();

      ParalyticToxin = DiscoveryBase(ParalyticToxinFeatName, Guids.ParalyticToxinFeat,
        "ParalyticToxin.Name", "ParalyticToxin.Description",
        BuffRefs.Staggered.Reference.Get().Icon, requiredLevel: 8).Configure();

      LethalToxin = DiscoveryBase(LethalToxinFeatName, Guids.LethalToxinFeat,
        "LethalToxin.Name", "LethalToxin.Description", bombIcon, requiredLevel: 10).Configure();

      CombineToxins = DiscoveryBase(CombineToxinsFeatName, Guids.CombineToxinsFeat,
        "CombineToxins.Name", "CombineToxins.Description", bombIcon).Configure();

      CarefulInjection = DiscoveryBase(CarefulInjectionFeatName, Guids.CarefulInjectionFeat,
        "CarefulInjection.Name", "CarefulInjection.Description",
        FeatureRefs.SneakAttack.Reference.Get().Icon).Configure();

      // --- Contact Toxin (4th): throw a vial at one enemy, DC -2. ---
      var contactThrow = ElementTool.Create<ContextActionDeliverToxin>();
      contactThrow.DcModifier = -2;
      AbilityConfigurator.New(ContactThrowAbilityName, Guids.ContactThrowAbility)
        .SetDisplayName("ContactToxinThrow.Name")
        .SetDescription("ContactToxinThrow.Description")
        .SetIcon(bombIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Close)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetEnemies()
        .AddAbilityResourceLogic(requiredResource: EldritchPoisoner.Doses, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().Add(contactThrow))
        .Configure();
      DiscoveryBase(ContactToxinFeatName, Guids.ContactToxinFeat,
        "ContactToxin.Name", "ContactToxin.Description", bombIcon, requiredLevel: 4)
        .AddFacts(new() { ContactThrowAbilityName })
        .Configure();

      // --- Toxic Fumes (6th): inhaled vial, 10-ft area, DC -4. ---
      var fumesThrow = ElementTool.Create<ContextActionDeliverToxin>();
      fumesThrow.DcModifier = -4;
      AbilityConfigurator.New(FumesThrowAbilityName, Guids.FumesThrowAbility)
        .SetDisplayName("ToxicFumesThrow.Name")
        .SetDescription("ToxicFumesThrow.Description")
        .SetIcon(bombIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Close)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetEnemies()
        .AddAbilityAoERadius(
          diameterInCells: 2,
          targetType: Kingmaker.UnitLogic.Abilities.Components.TargetType.Enemy)
        .AddAbilityResourceLogic(requiredResource: EldritchPoisoner.Doses, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().Add(fumesThrow))
        .Configure();
      DiscoveryBase(ToxicFumesFeatName, Guids.ToxicFumesFeat,
        "ToxicFumes.Name", "ToxicFumes.Description", bombIcon, requiredLevel: 6)
        .AddFacts(new() { FumesThrowAbilityName })
        .Configure();

      // --- Envenom: coat an adjacent ally's weapon as a move action. ---
      AbilityConfigurator.New(EnvenomAllyAbilityName, Guids.EnvenomAllyAbility)
        .SetDisplayName("EnvenomAlly.Name")
        .SetDescription("EnvenomAlly.Description")
        .SetIcon(bombIcon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Touch)
        .SetActionType(UnitCommand.CommandType.Move)
        .SetCanTargetFriends()
        .SetCanTargetSelf()
        .AddAbilityResourceLogic(requiredResource: EldritchPoisoner.Doses, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().ApplyBuff(EldritchPoisoner.Coating, ContextDuration.Fixed(10)))
        .Configure();
      DiscoveryBase(EnvenomFeatName, Guids.EnvenomFeat,
        "Envenom.Name", "Envenom.Description", bombIcon)
        .AddFacts(new() { EnvenomAllyAbilityName })
        .Configure();

      // --- Antidote: sacrifice a dose to delay poison on an ally. ---
      AbilityConfigurator.New(AntidoteAbilityName, Guids.AntidoteAbility)
        .SetDisplayName("AntidoteAbility.Name")
        .SetDescription("AntidoteAbility.Description")
        .SetIcon(BuffRefs.DelayPoisonBuff.Reference.Get().Icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Touch)
        .SetActionType(UnitCommand.CommandType.Standard)
        .SetCanTargetFriends()
        .SetCanTargetSelf()
        .AddAbilityResourceLogic(requiredResource: EldritchPoisoner.Doses, amount: 1, isSpendResource: true)
        .AddAbilityEffectRunAction(
          ActionsBuilder.New().ApplyBuff(BuffRefs.DelayPoisonBuff.Reference.Get(), ContextDuration.Fixed(10)))
        .Configure();
      DiscoveryBase(AntidoteFeatName, Guids.AntidoteFeat,
        "Antidote.Name", "Antidote.Description", BuffRefs.DelayPoisonBuff.Reference.Get().Icon)
        .AddFacts(new() { AntidoteAbilityName })
        .Configure();

      // --- Apothecary: half alchemist level on Lore (Nature) (Heal adaptation). ---
      DiscoveryBase(ApothecaryFeatName, Guids.ApothecaryFeat,
        "Apothecary.Name", "Apothecary.Description",
        FeatureRefs.AlchemistThrowAnything.Reference.Get().Icon)
        .AddContextStatBonus(
          StatType.SkillLoreNature, ContextValues.Rank(), ModifierDescriptor.UntypedStackable)
        .AddContextRankConfig(
          ContextRankConfigs.ClassLevel(new[] { CharacterClassRefs.AlchemistClass.ToString() })
            .WithDiv2Progression())
        .Configure();
    }

    /// <summary>
    /// Base builder for a discovery feature: registers it as a normal alchemist discovery
    /// pick (with an optional class-level gate). The caller chains extras and calls
    /// Configure().
    /// </summary>
    private static FeatureConfigurator DiscoveryBase(
      string name,
      string guid,
      string displayKey,
      string descriptionKey,
      Sprite icon,
      int requiredLevel = 0)
    {
      var builder = FeatureConfigurator.New(name, guid)
        .SetDisplayName(displayKey)
        .SetDescription(descriptionKey)
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddToFeatureSelection(
          FeatureSelectionRefs.DiscoverySelection.Cast<BlueprintFeatureSelectionReference>());
      if (requiredLevel > 0)
      {
        builder.AddPrerequisiteClassLevel(
          CharacterClassRefs.AlchemistClass.Cast<BlueprintCharacterClassReference>(), requiredLevel);
      }
      return builder;
    }
  }

  /// <summary>
  /// Thrown arcanotoxin delivery (Contact Toxin / Toxic Fumes): applies the full toxin
  /// logic with an adjusted save DC.
  /// </summary>
  [TypeId(Guids.DeliverToxinAction)]
  internal class ContextActionDeliverToxin : ContextAction
  {
    /// <summary>Added to the toxin's save DC (negative = weaker throw).</summary>
    public int DcModifier;

    public override string GetCaption() => "Deliver arcanotoxin";

    public override void RunAction()
    {
      // 0.60.0: the throws reached the action bar but produced nothing, and every exit below
      // was silent. 0.62.0: Context and Target are BOTH ambient - they read
      // ContextData<MechanicsContext.Data>.Current rather than anything handed to the action
      // (Kingmaker.UnitLogic.Mechanics.Actions.ContextAction defines both that way) - so if no
      // data scope was pushed, both are null and the first dereference threw before any of the
      // 0.60.0 logging could run. Every step is null-safe now, and a missing data scope is
      // reported as its own case because it means something different from "no target".
      var context = Context;
      var targetWrapper = Target;
      var caster = context?.MaybeCaster;
      var target = targetWrapper?.Unit;
      MissionFeats.Logger.Info(
        $"[toxin] throw fired. scope={(context is null ? "NONE" : "ok")} " +
        $"caster={caster?.CharacterName ?? "NULL"} " +
        $"target={target?.CharacterName ?? "NULL"} " +
        $"mainTarget={(context?.MainTarget is null ? "NULL" : context.MainTarget.ToString())} " +
        $"dcMod={DcModifier}");
      if (caster is null || target is null)
      {
        MissionFeats.Logger.Warn(
          context is null
            ? "[toxin] throw aborted: no mechanics data scope - the action ran outside one."
            : "[toxin] throw aborted: no caster or no target.");
        return;
      }
      ArcanotoxinApply.Apply(caster, target, context, null, DcModifier, false);
    }
  }
}

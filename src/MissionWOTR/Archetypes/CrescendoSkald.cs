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
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.39.0 — the Crescendo Skald (homebrew, the user's design).
  ///
  /// "A skald buff that focuses on momentum instead of rage, giving
  /// increasing bonuses per turn in combat as long as at least one
  /// attack/spell lands. Being overall 30% stronger than a rage buff,
  /// without the no casting downside of rage, but requiring time to get
  /// to potential, and completely resetting if no attack or spell lands
  /// for the ally getting the effect (doesn't reset for all of them,
  /// just that one ally who missed)."
  ///
  /// THE TRADES: Inspired Rage and the vanilla raging-song button (the
  /// rounds pool is re-granted by our feature). Everything else about the
  /// skald stands — this replaces the rage song, not the class.
  ///
  /// THE CRESCENDO: a standard-action raging song with a 60-foot aura.
  /// Every ally inside builds MOMENTUM, tracked per ally:
  /// - each round an ally lands at least one attack OR completes at least
  ///   one real spell, their Momentum grows by 1 (max 5);
  /// - a round with nothing landed resets THAT ally's Momentum to zero —
  ///   the others keep theirs (per-ally state, the user's exact rule);
  /// - Momentum grants morale bonuses to Strength and Constitution
  ///   (+1 at 1 stack, +2 at 3, +3 at 5), +1 Will, and −1 AC while it
  ///   lasts.
  ///
  /// THE MATH (documented, tunable in one table):
  /// - rage (inspired rage ally effect) = +2 Str, +2 Con, +1 Will, −1 AC,
  ///   and raging allies cannot cast;
  /// - peak Momentum = +3 Str, +3 Con, +1 Will, −1 AC, casting always
  ///   allowed — the nearest whole step to "30% stronger" (+2.6 → +3);
  /// - rage parity arrives at 3 stacks and the peak at 5: the ramp, the
  ///   per-ally reset risk, and the round cost are the price of the peak.
  ///
  /// ENGINE NOTES:
  /// - The song machinery is the 0.38.0 Spell Warrior pattern: a start
  ///   action that checks and spends raging-song rounds, a song buff
  ///   carrying the 60-foot area (the Doomsayer aura idiom) and the
  ///   rounds component (SpellWarriorSongRounds, reused verbatim), which
  ///   ends the song at zero rounds.
  /// - "Landed an attack" = RuleAttackRoll with IsHit (the Spirit-Ridden
  ///   rider idiom); "completed a spell" = RuleCastSpell of a real spell
  ///   (the CovertMage spell-trick idiom). Documented simplification: any
  ///   completed spell cast counts — the engine does not expose
  ///   "the target failed its save", so buffs and heals also carry
  ///   momentum.
  /// - The per-round stack tick rides ITickEachRound on the ally buff
  ///   (the Spirit-Ridden form idiom); bonuses are applied as stat
  ///   modifiers and torn down cleanly on buff removal.
  /// </summary>
  internal class CrescendoSkald
  {
    internal const string ArchetypeName = "CrescendoSkald";

    internal static void Configure()
    {
      var skald = CharacterClassRefs.SkaldClass.Reference.Get();
      var icon = AbilityRefs.Haste.Reference.Get().Icon;
      var rounds = AbilityResourceRefs.RagingSongResource.Reference.Get();

      // ----- The ally buff: one blueprint, its component owns the -----
      // per-ally momentum state and the stat table.
      var momentumBuff = BuffConfigurator.New(
        "CrescendoMomentumBuff", Guids.CrescendoMomentumBuff)
        .SetDisplayName("CrescendoMomentum.Name")
        .SetDescription("CrescendoMomentum.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new CrescendoMomentum())
        .Configure();

      // ----- The 60-foot aura: follows the singer, catches allies who
      // walk in late, hands each of them their OWN momentum (the
      // Doomsayer dread-mien pattern with the ally condition). -----
      var area = AbilityAreaEffectConfigurator.New(
        "CrescendoSongArea", Guids.CrescendoSongArea)
        .AddAbilityAreaEffectBuff(
          buff: momentumBuff,
          condition: ConditionsBuilder.New().IsAlly())
        .SetSize(new(60))
        .SetShape(AreaEffectShape.Cylinder)
        .Configure();

      // ----- The song buff on the skald: the aura plus the rounds ----
      // economy (the Spell Warrior rounds component, reused verbatim:
      // one raging-song round per round, self-ending at zero).
      var songBuff = BuffConfigurator.New("CrescendoSongBuff", Guids.CrescendoSongBuff)
        .SetDisplayName("Crescendo.Name")
        .SetDescription("Crescendo.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .Configure();
      BuffConfigurator.For(songBuff)
        .AddAreaEffect(areaEffect: area)
        .AddComponent(new SpellWarriorSongRounds
        {
          SelfBuff = songBuff,
          RoundResource = rounds,
        })
        .Configure();

      // ----- The ability: start the song. -----
      var startSong = new CrescendoStartSongAction
      {
        SongBuff = songBuff,
        RoundResource = rounds,
      };
      var ability = AbilityConfigurator.New("CrescendoSkaldAbility", Guids.CrescendoSkaldAbility)
        .SetDisplayName("Crescendo.Name")
        .SetDescription("Crescendo.Description")
        .SetIcon(icon)
        .SetType(AbilityType.Special)
        .SetRange(AbilityRange.Personal)
        .SetActionType(UnitCommand.CommandType.Standard)
        .AddAbilityEffectRunAction(ActionsBuilder.New().Add(startSong).Build())
        .Configure();

      // ----- The feature (1st): the song + the re-granted rounds pool. -----
      var feature = FeatureConfigurator.New("CrescendoSkaldFeature", Guids.CrescendoSkaldFeature)
        .SetDisplayName("Crescendo.Name")
        .SetDescription("Crescendo.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddFacts(new() { ability })
        .AddAbilityResources(
          resource: AbilityResourceRefs.RagingSongResource
            .Cast<BlueprintAbilityResourceReference>(),
          restoreAmount: true)
        .Configure();

      // ----- The archetype: trades the rage song for the crescendo. -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.CrescendoSkaldArchetype, CharacterClassRefs.SkaldClass)
          .SetLocalizedName("CrescendoSkald.Name")
          .SetLocalizedDescription("CrescendoSkald.Description")
          .AddToAddFeatures(LevelPlan.L(1), feature);
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, skald,
        FeatureRefs.RagingSong.ToString(),
        FeatureRefs.InspiredRage.ToString());
      // 0.41.0: RAGE POWERS NOW WORK (the user's challenge: "can you make
      // rage powers work?"). Two pieces, both on verified engine parts:
      // 1) the momentum buff carries AddFactsFromCaster pointed at the
      //    skald's rage-power selection - every ally with momentum is
      //    granted the skald's SELECTED rage powers for as long as their
      //    momentum lasts (this is the vanilla inspired-rage carrier
      //    component, found via the CI metadata probe); and
      // 2) every skald rage power's BuffExtraEffects payload gate is
      //    cloned to ALSO fire while momentum is on the holder - so the
      //    payloads (the actual rage-power effects) trigger for anyone
      //    the crescendo carries, not just the singer.
      // The skald keeps her rage-power grants (the 0.40.0 removal is
      // reverted); the Spell Warrior keeps its removal (its trade is
      // weapon enhancement, and the tabletop rage-power rider was cut).
      AttachRagePowers(momentumBuff);
      archetype.Configure();
      MissionFeats.Logger.Info("[crescendo] configured: " + ArchetypeName + ".");
    }

    /// <summary>
    /// Wires the skald's rage powers to MOMENTUM (the user's design
    /// challenge, 0.41.0). Uses only engine parts verified by the CI
    /// metadata probe: AddFactsFromCaster (public class; private fields
    /// set via reflection - the ConstructCrafter idiom) and
    /// BuffExtraEffects (public; fields m_CheckedBuff/m_ExtraEffectBuff
    /// confirmed by the same probe).
    /// </summary>
    private static void AttachRagePowers(BlueprintBuff momentumBuff)
    {
      try
      {
        const System.Reflection.BindingFlags flags =
          System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
          System.Reflection.BindingFlags.Instance;

        // 1. Allies with momentum receive the skald's selected rage
        // powers (and lose them again when their momentum ends).
        var selection = FeatureSelectionRefs.SkaldRagePowerSelection.Reference.Get();
        var fromCaster = new Kingmaker.UnitLogic.FactLogic.AddFactsFromCaster();
        var fromCasterType = fromCaster.GetType();
        fromCasterType.GetField("m_Facts", flags)?.SetValue(
          fromCaster, new BlueprintUnitFactReference[0]);
        var selectionField = fromCasterType.GetField("m_Selection", flags);
        if (selectionField != null)
        {
          object selectionRef;
          if (typeof(BlueprintFeatureSelectionReference).IsAssignableFrom(selectionField.FieldType))
          {
            selectionRef = FeatureSelectionRefs.SkaldRagePowerSelection.Reference;
          }
          else
          {
            selectionRef = selection.ToReference<BlueprintFeatureReference>();
          }
          selectionField.SetValue(fromCaster, selectionRef);
        }
        fromCasterType.GetField("FeatureFromSelection", flags)?.SetValue(fromCaster, true);
        BuffConfigurator.For(momentumBuff).AddComponent(fromCaster).Configure();
        MissionFeats.Logger.Info("[crescendo] momentum now carries the skald's rage powers.");

        // 2. Each skald rage power's payload gate (BuffExtraEffects)
        // gets a twin that fires on momentum instead of the rage song.
        // Note: these feature blueprints are shared with barbarians; the
        // twin only ever matters for a unit that holds BOTH the feature
        // and a momentum buff - i.e. the skald and her carried allies.
        int patched = 0;
        var features = ReadSelectionFeatures(selection);
        foreach (var feature in features)
        {
          if (feature?.ComponentsArray is null)
          {
            continue;
          }
          foreach (var gate in feature.ComponentsArray
            .OfType<Kingmaker.Designers.Mechanics.Facts.BuffExtraEffects>())
          {
            if (gate is null)
            {
              continue;
            }
            var clone = new Kingmaker.Designers.Mechanics.Facts.BuffExtraEffects();
            foreach (var field in clone.GetType().GetFields(flags))
            {
              if (field.Name == "m_CheckedBuff")
              {
                field.SetValue(clone, momentumBuff.ToReference<BlueprintBuffReference>());
              }
              else if (field.Name == "m_CheckedBuffList")
              {
                field.SetValue(clone, new BlueprintBuffReference[0]);
              }
              else
              {
                field.SetValue(clone, field.GetValue(gate));
              }
            }
            FeatureConfigurator.For(feature).AddComponent(clone).Configure();
            patched++;
          }
        }
        MissionFeats.Logger.Info(
          $"[crescendo] {patched} rage-power payload gate(s) now also fire on momentum.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[crescendo] rage-power wiring failed.", e);
      }
    }

    /// <summary>Reads a feature selection's feature list (prop or field).</summary>
    private static System.Collections.Generic.IEnumerable<BlueprintFeature> ReadSelectionFeatures(
      object selection)
    {
      const System.Reflection.BindingFlags flags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance;
      var raw = selection.GetType().GetProperty("AllFeatures", flags)?.GetValue(selection, null)
        ?? selection.GetType().GetField("m_AllFeatures", flags)?.GetValue(selection);
      if (raw is System.Collections.IEnumerable list)
      {
        foreach (var item in list)
        {
          var feature = (item as BlueprintFeatureReference)?.Get();
          if (feature is not null)
          {
            yield return feature;
          }
        }
      }
    }
  }

  /// <summary>
  /// Starts the crescendo: checks the raging-song pool, spends the first
  /// round, and applies the song buff (the Spell Warrior start idiom,
  /// minus the tier pick — momentum has no tiers, it has a ramp).
  /// </summary>
  [TypeId(Guids.CrescendoStartSongAction)]
  internal class CrescendoStartSongAction : ContextAction
  {
    public BlueprintBuff SongBuff;
    public BlueprintAbilityResource RoundResource;

    public override void RunAction()
    {
      try
      {
        var caster = Context?.MaybeCaster;
        if (caster is null)
        {
          MissionFeats.Logger.Warn("[crescendo] no caster in context.");
          return;
        }
        if (caster.Resources.GetResourceAmount(RoundResource) <= 0)
        {
          MissionFeats.Logger.Warn("[crescendo] no raging-song rounds left today.");
          return;
        }
        caster.Resources.Spend(RoundResource, 1);
        caster.AddBuff(SongBuff, Context, TimeSpan.FromHours(1));
        MissionFeats.Logger.Info("[crescendo] the crescendo begins.");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[crescendo] could not start the song.", e);
      }
    }

    public override string GetCaption() => "Crescendo";
  }

  /// <summary>
  /// One ally's momentum, tracked on that ally alone. Every round:
  /// landed something (+1 stack, max 5) or nothing (reset to zero —
  /// ONLY this ally; the rest of the party keeps theirs). The stat
  /// table is right here, one line to tune.
  /// </summary>
  [TypeId(Guids.CrescendoMomentumComponent)]
  internal class CrescendoMomentum : UnitFactComponentDelegate,
    Kingmaker.Controllers.Units.ITickEachRound,
    IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>,
    IInitiatorRulebookHandler<RuleCastSpell>, IRulebookHandler<RuleCastSpell>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    /// <summary>
    /// Cumulative Strength/Constitution bonus by Momentum stack
    /// (index = stacks): +1 at 1, +1 at 2, +2 at 3, +2 at 4, +3 at 5.
    /// Rage parity at 3, the peak at 5 — the nearest whole step to
    /// "30% stronger than rage" (+2.6 → +3).
    /// </summary>
    private static readonly int[] StrConByStack = { 0, 1, 1, 2, 2, 3 };

    private const int MaxStacks = 5;
    private const int WillBonus = 1;
    private const int AcPenalty = -1;

    private int m_Stacks;
    private bool m_LandedThisRound;
    private readonly List<ModifiableValue.Modifier> m_Added = new();

    public void OnNewRound()
    {
      try
      {
        if (m_LandedThisRound)
        {
          m_Stacks = Math.Min(m_Stacks + 1, MaxStacks);
        }
        else
        {
          // The reset: a round with nothing landed spends this ally's
          // momentum entirely — and only this ally's (the user's rule).
          m_Stacks = 0;
        }
        m_LandedThisRound = false;
        Recalculate();
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[crescendo] momentum tick failed.", e);
      }
    }

    protected override void OnTurnOn()
    {
      m_Stacks = 0;
      m_LandedThisRound = false;
      Recalculate();
    }

    protected override void OnTurnOff()
    {
      Clear();
    }

    public void OnEventAboutToTrigger(RuleAttackRoll evt) { }

    public void OnEventDidTrigger(RuleAttackRoll evt)
    {
      try
      {
        if (evt.Initiator == Owner && evt.IsHit)
        {
          m_LandedThisRound = true;
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[crescendo] attack landing check failed.", e);
      }
    }

    public void OnEventAboutToTrigger(RuleCastSpell evt) { }

    public void OnEventDidTrigger(RuleCastSpell evt)
    {
      try
      {
        // A completed real spell carries momentum (the CovertMage
        // spell-trick filter). Documented simplification: any completed
        // cast counts — the engine does not expose save outcomes.
        if (evt.Initiator == Owner &&
          evt.Spell?.Blueprint?.Type == AbilityType.Spell)
        {
          m_LandedThisRound = true;
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[crescendo] spell landing check failed.", e);
      }
    }

    private void Recalculate()
    {
      try
      {
        Clear();
        if (m_Stacks <= 0)
        {
          return;
        }
        var strCon = StrConByStack[m_Stacks];
        Add(Owner.Stats.Strength, strCon, ModifierDescriptor.Morale);
        Add(Owner.Stats.Constitution, strCon, ModifierDescriptor.Morale);
        Add(Owner.Stats.SaveWill, WillBonus, ModifierDescriptor.Morale);
        Add(Owner.Stats.AC, AcPenalty, ModifierDescriptor.Penalty);
        MissionFeats.Logger.Info(
          $"[crescendo] {Owner.CharacterName}'s momentum: {m_Stacks} (+{strCon} Str/Con).");
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[crescendo] momentum recalculate failed.", e);
      }
    }

    private void Add(ModifiableValue stat, int value, ModifierDescriptor descriptor)
    {
      if (value == 0)
      {
        return;
      }
      var modifier = stat.AddModifier(value, Runtime, descriptor);
      if (modifier != null)
      {
        m_Added.Add(modifier);
      }
    }

    private void Clear()
    {
      foreach (var modifier in m_Added)
      {
        modifier?.Remove();
      }
      m_Added.Clear();
    }
  }
}

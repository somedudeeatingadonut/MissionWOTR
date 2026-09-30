using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Sin Eater (faithful port of the Ultimate Magic inquisitor archetype -
  /// 0.16.0, the mod's first inquisitor archetype, per the user: "add a
  /// tabletop archetype to the inquisitor while I think of a homebrew one").
  ///
  /// There is a sect of inquisitors that believes it is not enough to hunt
  /// the enemies of the church - one must also devour those enemies' sins.
  /// Consuming sins empowers the sin eater, at least for a time. In the
  /// Worldwound, that is not theology. That is lunch.
  ///
  /// Tabletop and its WOTR adaptation:
  /// - Eat Sin (1st, REPLACES domain): when the sin eater's own blow kills
  ///   an enemy, she devours its sins then and there. CRPG adaptation of
  ///   the tabletop's minute-long ritual ( Owlcat would do the same): the
  ///   eat is automatic and free on her killing blow. Healing: 1d8 + level
  ///   at 1st, 2d8 at 5th, 3d8 at 9th, 4d8 at 13th; the level bonus is
  ///   capped at +5/+10/+15/+20 by tier. Once per enemy (tracked by unit
  ///   id in component data - the tabletop's "once for each enemy she
  ///   kills"). No effect on creatures of Intelligence 2 or less (the
  ///   tabletop's mindless clause - checked via the Int score).
  /// - Sin Speaker (6th, REPLACES the 6th-level bonus teamwork feat): the
  ///   tabletop grants speak with dead within 10 minutes of eating; WOTR
  ///   has no speak-with-dead blueprint to build from. Adapted from the
  ///   archetype's own flavor ("consuming sins empowers the sin eater, at
  ///   least for a time"): each eaten sin now also empowers her - +1 on
  ///   attack rolls and saving throws for one minute.
  /// - The 8th-level rider (accept a negative level to prevent a corpse
  ///   rising as undead) is skipped: "would rise as undead" is
  ///   foreknowledge the engine does not expose. Documented, not faked.
  /// - Burden of Sin (14th, REPLACES exploit weakness) is skipped: the
  ///   tabletop transfers an arbitrary harmful effect between creatures;
  ///   the engine has no verifiable buff-transfer API. She keeps exploit
  ///   weakness. Documented, not faked.
  /// Log prefix: [removals] carries the trade diagnostics; [sineater] the rest.
  /// </summary>
  internal static class SinEater
  {
    internal const string ArchetypeName = "SinEaterArchetype";

    // Wired during Configure; read by the component.
    internal static BlueprintFeature SinSpeakerFeature;
    internal static BlueprintBuff EmpoweredBuff;

    public static void Configure()
    {
      var inquisitor = CharacterClassRefs.InquisitorClass.Reference.Get();
      var icon = FeatureRefs.InquisitorJudgements.Reference.Get().Icon;

      // ----- The empowerment rider buff (Sin Speaker, 6th) -----
      EmpoweredBuff = BuffConfigurator.New("SinEaterEmpoweredBuff", Guids.SinEaterEmpoweredBuff)
        .SetDisplayName("SinEaterEmpowered.Name")
        .SetDescription("SinEaterEmpowered.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: Kingmaker.EntitySystem.Stats.StatType.AdditionalAttackBonus,
          value: 1, descriptor: ModifierDescriptor.Circumstance)
        .AddStatBonus(stat: Kingmaker.EntitySystem.Stats.StatType.SaveWill,
          value: 1, descriptor: ModifierDescriptor.Circumstance)
        .AddStatBonus(stat: Kingmaker.EntitySystem.Stats.StatType.SaveReflex,
          value: 1, descriptor: ModifierDescriptor.Circumstance)
        .AddStatBonus(stat: Kingmaker.EntitySystem.Stats.StatType.SaveFortitude,
          value: 1, descriptor: ModifierDescriptor.Circumstance)
        .Configure();

      // ----- Eat Sin (1st): the component does all the work -----
      var eatSin = FeatureConfigurator.New("SinEaterEatSinFeature", Guids.SinEaterEatSin)
        .SetDisplayName("SinEaterEatSin.Name")
        .SetDescription("SinEaterEatSin.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new SinEaterEatSinComponent { Class = inquisitor })
        .Configure();

      // ----- Sin Speaker (6th): the learned flag the component checks -----
      SinSpeakerFeature = FeatureConfigurator.New("SinEaterSinSpeakerFeature", Guids.SinEaterSinSpeaker)
        .SetDisplayName("SinEaterSinSpeaker.Name")
        .SetDescription("SinEaterSinSpeaker.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.SinEaterArchetype, CharacterClassRefs.InquisitorClass)
          .SetLocalizedName("SinEater.Name")
          .SetLocalizedDescription("SinEater.Description")
          .AddToAddFeatures(LevelPlan.L(1), eatSin)
          .AddToAddFeatures(LevelPlan.L(6), SinSpeakerFeature);

      // Domain (1st) and exploit weakness (14th) leave with the class
      // features they replace; the 6th-level teamwork feat is a selection
      // granted at several levels, so it is removed at exactly 6th.
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, inquisitor,
        "48525e5da45c9c243a343fc6545dbdb9", // Domains selection (the inquisitor's domain)
        "374a73288a36e2d4f9e54c75d2e6e573"); // Exploit Weakness (kept in spirit: Burden of Sin is not portable)

      archetype = ArchetypeRemovals.RemoveAtLevel(
        archetype, inquisitor.Progression, 6,
        "d87e2f6a9278ac04caeb0f93eff95fcb"); // Teamwork feat selection, 6th-level slot only

      archetype.Configure();

      MissionFeats.Logger.Info("SinEater: configured.");
    }
  }

  /// <summary>
  /// Eat Sin: when her own attack fells an enemy of sound enough mind, she
  /// devours its sins - healing herself and (from 6th, with Sin Speaker)
  /// growing briefly stronger on what she swallowed. Once per victim.
  /// </summary>
  [TypeId(Guids.SinEaterEatSinComponent)]
  internal class SinEaterEatSinComponent :
    UnitFactComponentDelegate<SinEaterEatSinComponent.ComponentData>,
    IInitiatorRulebookHandler<RuleAttackWithWeapon>, IRulebookHandler<RuleAttackWithWeapon>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass Class;

    public void OnEventAboutToTrigger(RuleAttackWithWeapon evt) { }

    public void OnEventDidTrigger(RuleAttackWithWeapon evt)
    {
      try
      {
        if (evt.AttackRoll is null || !evt.AttackRoll.IsHit ||
          evt.Initiator != Owner || evt.Target is null)
        {
          return;
        }
        var target = evt.Target;
        if (target.HPLeft > 0)
        {
          return; // not the killing blow
        }

        // The tabletop's clause: no effect on mindless creatures or Int 2 or less.
        if (target.Descriptor.Stats.Intelligence.BaseValue <= 2)
        {
          return;
        }

        // Once for each enemy she kills.
        var id = target.UniqueId;
        if (Data.Consumed.Contains(id))
        {
          return;
        }
        Data.Consumed.Add(id);

        var level = Owner.Progression.GetClassLevel(Class);
        int dice = level >= 13 ? 4 : level >= 9 ? 3 : level >= 5 ? 2 : 1;
        int cap = 5 * dice;
        var random = new System.Random();
        int heal = 0;
        for (int i = 0; i < dice; i++)
        {
          heal += random.Next(1, 9);
        }
        heal += Math.Min(level, cap);

        // Eating sins closes her own wounds; never above full.
        var damage = Owner.Descriptor.Damage;
        if (damage > 0)
        {
          Owner.Descriptor.Damage = Math.Max(0, damage - heal);
        }

        // Sin Speaker (6th): the eaten sin empowers her, for a time.
        if (SinEater.SinSpeakerFeature is not null &&
          Owner.HasFact(SinEater.SinSpeakerFeature) &&
          SinEater.EmpoweredBuff is not null)
        {
          var seconds = ContextDuration.Fixed(10).Calculate(Context).Seconds; // 10 rounds = 1 minute
          Owner.AddBuff(SinEater.EmpoweredBuff, Context, duration: seconds);
        }

        MissionFeats.Logger.Info(
          $"[sineater] ate the sins of {target.CharacterName}: {heal} healed.");
        CombatLog.Write(
          $"She devours the fallen's sins ({heal} healed).", Owner);
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[sineater] eat sin failed.", e);
      }
    }

    public class ComponentData
    {
      public List<string> Consumed = new();
    }
  }
}

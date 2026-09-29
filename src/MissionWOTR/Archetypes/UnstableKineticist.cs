using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Unstable Kineticist (original homebrew kineticist archetype - 0.19.0;
  /// concept by the user: they liked the random-element surge idea without
  /// the Worldwound theming, and named the archetype - spelling correct as
  /// written. Coverage check first, per docs/COVERAGE.md: vanilla ships
  /// Kinetic Knight, Dark Elementalist and Overwhelming Soul - this
  /// collides with nothing).
  ///
  /// Her kinetic gate never settled. Every blast she throws risks a surge -
  /// wild, uncontrolled elemental eruption. She never learned to contain
  /// the flow, and she cannot channel it into control.
  ///
  /// The trades (hefty, both vanilla features verified by guid):
  /// - Gather Power (the stabilizer - the burn economy's brake; she never
  ///   learned to contain the flow).
  /// - Elemental Overflow (the burn-payoff - chaos cannot be channeled).
  ///
  /// The kit:
  /// - Unstable Blast (1st): every kinetic blast she throws (any of the
  ///   17 vanilla blast abilities) has a 25% surge chance. Roll d6 on the
  ///   surge table:
  ///   1 Eruption - the target takes extra damage: 1d6 per 3 kineticist
  ///     levels (raw - see the honesty note below).
  ///   2 Chain Arc - the surge leaps: every enemy within 10 ft of the
  ///     target takes 1d6 per 4 levels.
  ///   3 Violent Discharge - the target is shaken for 1 round.
  ///   4 Rebound - the instability bites HER: 1d6 per 4 levels to self.
  ///   5 Overcharge - Eruption damage AND the shaken discharge.
  ///   6 Null Surge - the gate hiccups; nothing happens.
  /// - Rebound Control (8th): she has learned to shrug off the bite -
  ///   Rebound results become Null Surges.
  /// - Critical Mass (16th): the surge chance rises to 50%.
  ///
  /// Honesty note (documented, not faked): post-cast TYPED energy damage
  /// has no verifiable API in this build - every damage-dealing surface we
  /// have proven (Riftstalker's guided strike) deals raw damage after the
  /// fact. So the surge riders deal raw damage and the randomness lives in
  /// the EFFECTS table (dice, arc, discharge, self-harm) rather than in an
  /// energy type roll; the in-game text says "uncontrolled force", not
  /// "random element". If a typed post-hoc surface ever becomes verifiable,
  /// the table can grow energy types.
  /// Log prefix: [removals] carries trade diagnostics; [unstable] the rest.
  /// </summary>
  internal static class UnstableKineticist
  {
    internal const string ArchetypeName = "UnstableKineticistArchetype";

    // Wired during Configure; read by the component.
    internal static BlueprintFeature ReboundControlFeature;
    internal static BlueprintFeature CriticalMassFeature;

    public static void Configure()
    {
      var kineticist = CharacterClassRefs.KineticistClass.Reference.Get();
      var icon = AbilityRefs.AirBlastAbility.Reference.Get().Icon;

      var unstableBlast = FeatureConfigurator.New("UnstableBlastFeature", Guids.UnstableBlastFeature)
        .SetDisplayName("UnstableBlast.Name")
        .SetDescription("UnstableBlast.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new UnstableBlastComponent { Class = kineticist })
        .Configure();

      ReboundControlFeature = FeatureConfigurator.New("UnstableReboundControlFeature", Guids.UnstableReboundControlFeature)
        .SetDisplayName("UnstableReboundControl.Name")
        .SetDescription("UnstableReboundControl.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .Configure();

      CriticalMassFeature = FeatureConfigurator.New("UnstableCriticalMassFeature", Guids.UnstableCriticalMassFeature)
        .SetDisplayName("UnstableCriticalMass.Name")
        .SetDescription("UnstableCriticalMass.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.UnstableKineticistArchetype, CharacterClassRefs.KineticistClass)
          .SetLocalizedName("UnstableKineticist.Name")
          .SetLocalizedDescription("UnstableKineticist.Description")
          .AddToAddFeatures(LevelPlan.L(1), unstableBlast)
          .AddToAddFeatures(LevelPlan.L(8), ReboundControlFeature)
          .AddToAddFeatures(LevelPlan.L(16), CriticalMassFeature);

      // The hefty trades: the stabilizer and the burn-payoff.
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, kineticist,
        FeatureRefs.GatherPowerFeature.ToString(),
        FeatureRefs.ElementalOverflowFeature.ToString());

      archetype.Configure();

      MissionFeats.Logger.Info("UnstableKineticist: configured.");
    }
  }

  /// <summary>
  /// The surge engine: watches her spell casts; when the cast is one of
  /// the 17 vanilla kinetic blasts, rolls the surge chance and the d6
  /// surge table. All post-hoc surfaces are repo-proven: raw damage
  /// (Riftstalker's guided strike), short buffs, State.Units iteration
  /// (Scattering), System.Random.
  /// </summary>
  [TypeId(Guids.UnstableBlastComponent)]
  internal class UnstableBlastComponent : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleCastSpell>, IRulebookHandler<RuleCastSpell>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    public BlueprintCharacterClass Class;

    /// <summary>The 17 vanilla blast ability guids (from the Explosion work).</summary>
    private static readonly HashSet<string> BlastGuids =
      new HashSet<string>(KineticistExplosion.Blasts.Select(b => b.BlastGuid));

    public void OnEventAboutToTrigger(RuleCastSpell evt) { }

    public void OnEventDidTrigger(RuleCastSpell evt)
    {
      try
      {
        var spell = evt.Spell;
        if (spell?.Blueprint is null || evt.Initiator != Owner)
        {
          return;
        }
        if (!BlastGuids.Contains(spell.Blueprint.AssetGuid))
        {
          return; // not a kinetic blast
        }
        var target = evt.SpellTarget?.Unit;
        if (target is null || target.HPLeft <= 0)
        {
          return;
        }

        var level = Owner.Progression.GetClassLevel(Class);
        var random = new System.Random();
        var chance = UnstableKineticist.CriticalMassFeature is not null &&
          Owner.HasFact(UnstableKineticist.CriticalMassFeature) ? 50 : 25;
        if (random.Next(100) >= chance)
        {
          return; // the gate holds
        }

        int EruptionDice()
        {
          var dice = Math.Max(1, level / 3);
          var total = 0;
          for (int i = 0; i < dice; i++)
          {
            total += random.Next(1, 7);
          }
          return total;
        }

        int ArcDice()
        {
          var dice = Math.Max(1, level / 4);
          var total = 0;
          for (int i = 0; i < dice; i++)
          {
            total += random.Next(1, 7);
          }
          return total;
        }

        void Discharge(UnitEntityData victim)
        {
          var oneRound = ContextDuration.Fixed(1).Calculate(Context).Seconds;
          victim.AddBuff(BuffRefs.Shaken.Reference.Get(), Context, duration: oneRound);
        }

        var roll = random.Next(1, 7); // d6 surge table
        var hasReboundControl = UnstableKineticist.ReboundControlFeature is not null &&
          Owner.HasFact(UnstableKineticist.ReboundControlFeature);
        if (roll == 4 && hasReboundControl)
        {
          roll = 6; // the bite she no longer feels
        }

        switch (roll)
        {
          case 1: // Eruption
            target.Descriptor.Damage += EruptionDice();
            MissionFeats.Logger.Info("[unstable] surge: eruption.");
            break;

          case 2: // Chain Arc
            var arc = ArcDice();
            using (var enumerator = Kingmaker.Game.Instance.State.Units.GetEnumerator())
            {
              while (enumerator.MoveNext())
              {
                var unit = enumerator.Current;
                if (unit is null || unit.Descriptor.State.IsDead || unit == target ||
                  !unit.IsEnemy(Owner))
                {
                  continue;
                }
                if (unit.DistanceTo(target) <= 10.Feet().Meters)
                {
                  unit.Descriptor.Damage += arc;
                }
              }
            }
            MissionFeats.Logger.Info("[unstable] surge: chain arc.");
            break;

          case 3: // Violent Discharge
            Discharge(target);
            MissionFeats.Logger.Info("[unstable] surge: violent discharge.");
            break;

          case 4: // Rebound
            var bite = ArcDice();
            Owner.Descriptor.Damage += bite;
            MissionFeats.Logger.Info($"[unstable] surge: rebound ({bite}).");
            break;

          case 5: // Overcharge
            target.Descriptor.Damage += EruptionDice();
            Discharge(target);
            MissionFeats.Logger.Info("[unstable] surge: overcharge.");
            break;

          default: // Null Surge
            MissionFeats.Logger.Info("[unstable] surge: null - the gate hiccups.");
            break;
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[unstable] surge failed.", e);
      }
    }
  }
}

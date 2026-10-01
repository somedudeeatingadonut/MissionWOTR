using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// 0.45.0 — the Mending Blade (warpriest homebrew, this mod's own
  /// design; the user asked for "a warpriest that heals the team based
  /// on the damage he deals (a very slight amount)"). His god's grace
  /// no longer arrives in bursts from a holy symbol; it drips from
  /// every blow. Trades the entire Channel Energy line (every grant);
  /// Fervor untouched.
  ///
  /// 0.46.0 — the user's tuning pass, verbatim:
  /// - "Have the heal radius expand to 60 ft over time" — 30 ft at
  ///   1st, 45 ft at 10th (the blessings' major-power level), 60 ft at
  ///   20th (Aspect of War). Radius is recomputed from the warpriest's
  ///   level on every tithe, so it grows the moment he levels.
  /// - "the heal amount to 15% at level 20 for the first 2 hits, then
  ///   10% for the rest" — below 20th the tithe stays the 0.45.0
  ///   trickle (5%). At 20th, each round (a 6-second window on the
  ///   engine's own game clock, GameTime) his first two hits tithe
  ///   15% and every hit after 10%. The window anchors on the first
  ///   hit of the round and resets every 6 seconds of game time.
  /// - "make a feat specific to this class archetype that changes it
  ///   to negative energy, still affecting only allies... just in case
  ///   someone wants to do a complete undead lich party or something.
  ///   (Just make the feat requirement having blessed tithe)" — GRAVE
  ///   TITHE: a standalone feat (FeatureGroup.Feat, so it appears in
  ///   the normal feat list) whose only prerequisite is Blessed Tithe.
  ///
  /// ENGINE NOTES (everything probe-verified v6/v7 or proven in-repo):
  /// - evt.Result on RuleDealDamage = final dealt damage; evt.IsFake
  ///   skips preview triggers.
  /// - RuleHealDamage is a PURE restore-HP rule - it carries no energy
  ///   type and no polarity (probe v6: no such fields), because
  ///   polarity is the caller's choice: positive vs negative energy
  ///   live on the DAMAGE side (DamageEnergyType.PositiveEnergy /
  ///   .NegativeEnergy, probe v7). That is the engine's own model, and
  ///   Grave Tithe follows it exactly: undead allies get the restore
  ///   rule (negative energy heals the dead); living allies get
  ///   RuleDealDamage with an EnergyDamage of NegativeEnergy (negative
  ///   energy scorches the living) - with MinHPAfterDamage = 1 (probe
  ///   field on RuleDealDamage) so the scorch can hurt but never DOWN
  ///   an ally. Fallback if playtesting ever shows the rule inverting
  ///   on undead: swap the undead path to the Descriptor.Damage setter
  ///   (probe-verified set_Damage).
  /// - The scorch is itself outgoing damage from the warpriest aimed
  ///   at a player-faction unit, so the rider's existing
  ///   friendly-fire guard is also the recursion breaker: damage dealt
  ///   to allies pays no tithe, including the tithe's own scorch.
  /// - The round window: Kingmaker.Game.Instance.TimeController
  ///   .GameTime is a public System.TimeSpan (probe v7 + DarkCodex's
  ///   own PartCooldown stores it as one); it freezes when the game
  ///   pauses, so rounds do not tick by while the player is reading.
  /// - The ally sweep is Game.Instance.State.Units (proven green in
  ///   0.43.0), filtered to living player-faction units within
  ///   new Feet(r).Meters of the warpriest; unhurt allies are skipped
  ///   (no zero-value heal spam) - except under Grave Tithe, where the
  ///   scorch applies regardless (it is damage, not healing).
  /// - Grave Tithe's blueprint is referenced by the rider via GUID
  ///   (lazy BlueprintTool.Get, the ConstructCrafter idiom) because the
  ///   feat's prerequisite is Blessed Tithe itself - a blueprint cycle
  ///   the lazy lookup breaks cleanly.
  /// </summary>
  internal class MendingBlade
  {
    internal const string ArchetypeName = "MendingBlade";

    internal static void Configure()
    {
      var warpriest = CharacterClassRefs.WarpriestClass.Reference.Get();
      var icon = AbilityRefs.CureLightWounds.Reference.Get().Icon;

      // ----- Blessed Tithe (1st): the whole kit, one passive feature. -----
      var tithe = FeatureConfigurator.New(
        "MendingBladeTitheFeature", Guids.MendingBladeTitheFeature)
        .SetDisplayName("MendingBladeTithe.Name")
        .SetDescription("MendingBladeTithe.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddComponent(new MendingBladeTitheRider
        {
          WarpriestClass = warpriest,
          GraveTitheGuid = Guids.MendingBladeGraveTitheFeature,
        })
        .Configure();

      // ----- Grave Tithe: the standalone feat (0.46.0). -----
      // FeatureGroup.Feat is what puts a feature in the game's normal
      // feat list (the TunnelFighter pattern); the sole prerequisite is
      // Blessed Tithe, passed as the configured blueprint object (the
      // SanguineFont overload). The rider finds it by GUID at runtime,
      // so the feat can be configured delayed without a reference cycle.
      FeatureConfigurator.New(
        "MendingBladeGraveTitheFeature", Guids.MendingBladeGraveTitheFeature, FeatureGroup.Feat)
        .SetDisplayName("MendingBladeGraveTithe.Name")
        .SetDescription("MendingBladeGraveTithe.Description")
        .SetIcon(AbilityRefs.Harm.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddPrerequisiteFeature(tithe)
        .Configure(delayed: true);

      // ----- The archetype. -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.MendingBladeArchetype, CharacterClassRefs.WarpriestClass)
          .SetLocalizedName("MendingBlade.Name")
          .SetLocalizedDescription("MendingBlade.Description")
          .AddToAddFeatures(LevelPlan.L(1), tithe);
      // The trade: channel energy, the whole line, every grant.
      archetype = ArchetypeRemovals.RemoveEveryGrant(
        archetype, warpriest,
        FeatureSelectionRefs.WarpriestChannelEnergySelection.ToString());
      archetype.Configure();
      MissionFeats.Logger.Info("[mendingblade] configured: " + ArchetypeName + ".");
    }
  }

  /// <summary>
  /// The Blessed Tithe rider. Watches RuleDealDamage (initiator side):
  /// when the warpriest deals real damage to a non-ally, every living
  /// player-faction unit within his tithe radius is healed for a
  /// percent of it (rounded down, minimum 1). The percent: 5% below
  /// 20th level; at 20th, the first two hits of each round at 15% and
  /// the rest at 10%. The radius: 30 ft, 45 ft at 10th, 60 ft at 20th.
  /// With the Grave Tithe feat, the tithe is paid in negative energy:
  /// undead allies are healed as normal, living allies are scorched
  /// (never below 1 HP).
  /// </summary>
  [TypeId(Guids.MendingBladeTitheRider)]
  internal class MendingBladeTitheRider : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    /// <summary>The base tithe, in percent of damage dealt.</summary>
    public const int BasePercent = 5;

    /// <summary>The mature tithe at 20th+, in percent.</summary>
    public const int MaturePercent = 10;

    /// <summary>The surge tithe for the first hits of each round at
    /// 20th+, in percent.</summary>
    public const int SurgePercent = 15;

    /// <summary>How many hits each round get the surge percent.</summary>
    public const int SurgeHits = 2;

    /// <summary>A combat round, in seconds of game time.</summary>
    public const double RoundSeconds = 6.0;

    public BlueprintCharacterClass WarpriestClass;
    public string GraveTitheGuid;

    private TimeSpan m_WindowStart;
    private int m_HitsThisRound;
    private BlueprintFeature m_GraveTithe;

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        // Real damage only (skip the engine's preview/fake triggers),
        // positive amounts only, and no tithe for friendly fire: damage
        // dealt to the player's own side pays nothing. This also breaks
        // the Grave Tithe's own recursion: the scorch targets allies.
        if (evt.Initiator != Owner || evt.IsFake || evt.Result <= 0 ||
          evt.Target?.IsPlayerFaction == true)
        {
          return;
        }

        var level = Owner.Descriptor.Progression.GetClassLevel(WarpriestClass);
        var radiusFeet = level >= 20 ? 60 : level >= 10 ? 45 : 30;
        var radius = new Feet(radiusFeet).Meters;

        // The tithe percent: 5% until 20th; at 20th, the first two hits
        // of each 6-second round at 15%, the rest at 10%. The window
        // anchors on the first hit and resets on the game clock - which
        // freezes on pause, so no rounds tick by while the player reads.
        int percent;
        if (level < 20)
        {
          percent = BasePercent;
        }
        else
        {
          var now = Kingmaker.Game.Instance.TimeController.GameTime;
          if ((now - m_WindowStart).TotalSeconds >= RoundSeconds)
          {
            m_WindowStart = now;
            m_HitsThisRound = 0;
          }
          percent = m_HitsThisRound < SurgeHits ? SurgePercent : MaturePercent;
          m_HitsThisRound++;
        }

        // Percent of the damage dealt, rounded down, minimum 1.
        var heal = Math.Max(1, evt.Result * percent / 100);
        var grave = HasGraveTithe();

        var healed = 0;
        var scorched = 0;
        using (var enumerator = Kingmaker.Game.Instance.State.Units.GetEnumerator())
        {
          while (enumerator.MoveNext())
          {
            var unit = enumerator.Current;
            if (unit is null || unit.Descriptor.State.IsDead ||
              !unit.IsPlayerFaction)
            {
              continue;
            }
            if (Owner.DistanceTo(unit) > radius)
            {
              continue;
            }
            if (grave && !unit.Descriptor.IsUndead)
            {
              // Negative energy scorches the living - real, typed,
              // resistible damage through the engine's own rule (the
              // same model the game uses for positive vs negative
              // energy), but clamped so it can never DOWN an ally.
              var bundle = new DamageBundle();
              bundle.Add(new EnergyDamage(
                DiceFormula.Zero, heal, DamageEnergyType.NegativeEnergy));
              Rulebook.Trigger(new RuleDealDamage(Owner, unit, bundle)
              {
                Reason = Fact,
                MinHPAfterDamage = 1,
              });
              scorched++;
              continue;
            }
            // Skip the unhurt: a zero-value heal event per blow would be
            // pure floaty-text spam.
            if (unit.HPLeft >= unit.MaxHP)
            {
              continue;
            }
            // The engine's own heal rule: applies the healing and shows
            // it - floaty text, combat log, everything.
            Rulebook.Trigger(new RuleHealDamage(Owner, unit, heal)
            {
              SourceFact = Fact,
            });
            healed++;
          }
        }
        if (healed + scorched > 0)
        {
          MissionFeats.Logger.Info(
            $"[mendingblade] tithe: {heal} HP x {healed} healed, {scorched} scorched " +
            $"(damage {evt.Result}, {percent}%, radius {radiusFeet} ft).");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[mendingblade] tithe failed.", e);
      }
    }

    /// <summary>
    /// Grave Tithe lookup, by GUID and lazily: the feat's prerequisite
    /// is Blessed Tithe itself, so the rider cannot hold the blueprint
    /// at configure time without a cycle. BlueprintTool.Get is the
    /// ConstructCrafter idiom.
    /// </summary>
    private bool HasGraveTithe()
    {
      if (m_GraveTithe is null && !string.IsNullOrEmpty(GraveTitheGuid))
      {
        m_GraveTithe = BlueprintTool.Get<BlueprintFeature>(GraveTitheGuid);
      }
      return m_GraveTithe is not null && Owner.HasFact(m_GraveTithe);
    }
  }
}

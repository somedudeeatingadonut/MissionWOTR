using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
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
  /// on the damage he deals (a very slight amount)"). The fourth
  /// MissionWOTR homebrew, and the design brief is the user's own
  /// balance call, taken at face value as always: VERY SLIGHT. The
  /// tithe is 5% of damage dealt, rounded down, minimum 1 - a trickle,
  /// not a torrent. No scaling, no multiplier, no per-level growth: the
  /// 0.43.0 lesson (the Overchanneler's tier-times-spell-level rider)
  /// is why there is exactly ONE knob and it is small.
  ///
  /// THE CONCEPT: his god's grace no longer arrives in bursts from a
  /// holy symbol; it drips from every blow. Each wound the mending
  /// blade inflicts pays a small tithe of healing to the allies
  /// fighting beside him.
  ///
  /// THE GAIN - Blessed Tithe (1st): whenever the warpriest deals
  /// damage to an enemy, every living ally within 30 feet of him
  /// (himself included) is healed for 5% of the damage dealt (rounded
  /// down, minimum 1), as positive-energy healing through the game's
  /// standard healing rule. Weapon blows, spells, and lingering damage
  /// all count; a killing blow counts at the full damage rolled
  /// (overkill included - documented). Damage dealt to allies (friendly
  /// fire) pays no tithe.
  ///
  /// THE TRADE: the entire Channel Energy line - every grant (the same
  /// removal the withdrawn Champion of the Faith used). Fervor, the
  /// class's core self-buff engine, is untouched.
  ///
  /// ENGINE NOTES (everything probe-verified v6 or proven in-repo):
  /// - evt.Result on RuleDealDamage is the final dealt damage (the
  ///   bpcore AddOutgoingDamageTriggerFixed read).
  /// - evt.IsFake skips the game's preview/calculation triggers.
  /// - new RuleHealDamage(initiator, target, bonus) is the engine's
  ///   real heal rule (probe: .ctor (initiator, target, bonus));
  ///   triggering it applies the heal with the game's own floaty text
  ///   and combat log. UnitEntityData.HPLeft is getter-only (probe), so
  ///   the rule IS the path - which is also the honest one.
  /// - The ally sweep is Kingmaker.Game.Instance.State.Units (the idiom
  ///   that compiled green in 0.43.0), filtered to living player-faction
  ///   units within new Feet(30).Meters of the warpriest (bpcore's own
  ///   feet-to-world-units conversion).
  /// - The rider is a UnitFactComponentDelegate (the Overchanneler
  ///   idiom) placed directly on the feature - vanilla's own
  ///   AddOutgoingDamageTrigger lives on features the same way.
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
        .AddComponent(new MendingBladeTitheRider())
        .Configure();

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
  /// player-faction unit within 30 feet of him heals 5% of it (rounded
  /// down, minimum 1). The single balance knob is TithePercent = 5, and
  /// it stays 5.
  /// </summary>
  [TypeId(Guids.MendingBladeTitheRider)]
  internal class MendingBladeTitheRider : UnitFactComponentDelegate,
    IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>,
    IInitiatorRulebookSubscriber, ISubscriber
  {
    /// <summary>The tithe, in percent of damage dealt. Very slight, by
    /// the user's explicit design. Do not scale this.</summary>
    public const int TithePercent = 5;

    /// <summary>The mending radius, in feet from the warpriest.</summary>
    private static readonly float RadiusMeters = new Feet(30).Meters;

    public void OnEventAboutToTrigger(RuleDealDamage evt) { }

    public void OnEventDidTrigger(RuleDealDamage evt)
    {
      try
      {
        // Real damage only (skip the engine's preview/fake triggers),
        // positive amounts only, and no tithe for friendly fire: damage
        // dealt to the player's own side pays nothing.
        if (evt.Initiator != Owner || evt.IsFake || evt.Result <= 0 ||
          evt.Target?.IsPlayerFaction == true)
        {
          return;
        }
        // 5%, rounded down, minimum 1 - the "very slight amount."
        var heal = Math.Max(1, evt.Result * TithePercent / 100);
        var healed = 0;
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
            // Skip the unhurt: a zero-value heal event per blow would be
            // pure floaty-text spam.
            if (unit.HPLeft >= unit.MaxHP)
            {
              continue;
            }
            if (Owner.DistanceTo(unit) > RadiusMeters)
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
        if (healed > 0)
        {
          MissionFeats.Logger.Info(
            $"[mendingblade] tithe: {heal} HP x {healed} allies (damage {evt.Result}).");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("[mendingblade] tithe failed.", e);
      }
    }
  }
}

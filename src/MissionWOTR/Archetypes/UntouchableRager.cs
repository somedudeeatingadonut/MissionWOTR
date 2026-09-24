using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.ActivatableAbilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Units;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using MissionWOTR.Feats;
using System;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Untouchable Rager (faithful port of the Advanced Class Guide bloodrager
  /// archetype - the missing half of the bloodrager pair: Sanguine Font turns
  /// her magic outward to heal; the Untouchable Rager's bloodline turns
  /// inward and refuses magic entirely).
  ///
  /// Tabletop (ACG):
  /// - Raging Resistance (Ex) 4th: instead of gaining spells, she becomes
  ///   resistant to them. While bloodraging she gains spell resistance equal
  ///   to 8 + her bloodrager level; the SR cannot be voluntarily lowered
  ///   while the rage lasts. At 7th, 10th, 13th and 16th level (when other
  ///   bloodragers gain bloodline spells) the SR increases by 1 each time
  ///   (cap: SR 32 at 20th). This replaces the spells, blood casting, eschew
  ///   materials, and bloodline spells class features.
  /// - Resistance Control (Ex) 14th: she gains the spell resistance from
  ///   raging resistance even while not bloodraging - and while calm, she can
  ///   lower it at will.
  ///
  /// WOTR adaptations (engine gaps):
  /// - The spellcasting trade removes every progression feature whose
  ///   AddSpellbook component grants the bloodrager spellbook (found by
  ///   component scan - see ArchetypeRemovals.RemoveSpellcasting). Bloodline
  ///   bonus spells live in the bloodline progressions and simply have no
  ///   spellbook left to touch; blood casting and eschew materials are
  ///   baked into the same casting kit in WOTR and go with it.
  /// - WOTR has no "lower your SR" action, so Resistance Control is a
  ///   default-ON activatable toggle: off = the calm-state ward drops (rage
  ///   always forces the resistance back on, exactly as the tabletop
  ///   forbids lowering it mid-rage).
  /// - SR values live on two buffs (the rage-granted one and the toggle's
  ///   one); the maintenance component guarantees exactly ONE is active at
  ///   any moment so the two sources can never stack.
  ///
  /// Implementation notes: the SR buff uses the vanilla AddSpellResistance
  /// component (the "Spell Resistance" spell mechanism) with a custom rank
  /// progression baked to the tabletop table (8 + level + milestone bonus);
  /// maintenance runs on round ticks and on every RuleSpellResistanceCheck
  /// aimed at the rager, so the ward appears the moment it matters. Log
  /// prefix: [untouchable].
  /// </summary>
  internal static class UntouchableRager
  {
    internal const string ArchetypeName = "UntouchableRagerArchetype";
    internal const string DisplayName = "UntouchableRager.Name";
    internal const string Description = "UntouchableRager.Description";

    internal const string ResistanceName = "UntouchableRagingResistance";
    internal const string SrBuffName = "UntouchableRagingResistanceBuff";
    internal const string ControlName = "UntouchableResistanceControl";
    internal const string ControlBuffName = "UntouchableResistanceControlBuff";

    // Vanilla: the shared bloodrage buff (upgraded by greater/mighty bloodrage
    // in place, so this one fact covers every rage state).
    private const string BloodragerStandartRageBuffGuid =
      "5eac31e457999334b98f98b60fc73b2f";

    public static void Configure()
    {
      var bloodrager = CharacterClassRefs.BloodragerClass.Reference.Get();
      var rageBuff = BlueprintTool.Get<BlueprintBuff>(BloodragerStandartRageBuffGuid);
      var srIcon = FeatureRefs.ArcanistExploitSpellResistanceFeature.Reference.Get().Icon;

      // ----- The spell resistance itself (one config, two buff instances) -----
      // Tabletop table: 8 + bloodrager level, +1 at 7th/10th/13th/16th.
      BlueprintBuff SrBuff(string name, string guid, string displayBase)
      {
        return BuffConfigurator.New(name, guid)
          .SetDisplayName(displayBase + ".Name")
          .SetDescription(displayBase + ".Description")
          .SetIcon(srIcon)
          .AddSpellResistance(value: ContextValues.Rank())
          .AddContextRankConfig(
            ContextRankConfigs.ClassLevel(new[] { CharacterClassRefs.BloodragerClass.ToString() })
              .WithCustomProgression(
                (1, 9), (2, 10), (3, 11), (4, 12), (5, 13), (6, 14),
                (7, 16), (8, 17), (9, 18), (10, 20), (11, 21), (12, 22),
                (13, 24), (14, 25), (15, 26), (16, 28), (17, 29), (18, 30),
                (19, 31), (20, 32)))
          .SetIsClassFeature()
          .Configure();
      }

      var srBuff = SrBuff(SrBuffName, Guids.UntouchableSrBuff, "UntouchableRagingResistance");
      var controlBuff = SrBuff(
        ControlBuffName, Guids.UntouchableControlBuff, "UntouchableResistanceControl");

      // ----- Raging Resistance (4th): the ward while bloodraging -----
      var resistance = FeatureConfigurator.New(ResistanceName, Guids.UntouchableRagingResistance)
        .SetDisplayName("UntouchableRagingResistance.Name")
        .SetDescription("UntouchableRagingResistance.Description")
        .SetIcon(srIcon)
        .SetIsClassFeature()
        .AddComponent(new UntouchableResistanceMaintenance
        {
          RageBuff = rageBuff,
          SrBuff = srBuff,
          ControlBuff = controlBuff,
        })
        .Configure();

      // ----- Resistance Control (14th): the ward while calm - and a switch -----
      var controlToggle = ActivatableAbilityConfigurator.New(
          ControlName, Guids.UntouchableControlActivatable)
        .SetDisplayName("UntouchableResistanceControl.Name")
        .SetDescription("UntouchableResistanceControl.Description")
        .SetIcon(srIcon)
        .SetBuff(controlBuff)
        .SetIsOnByDefault(true)
        .Configure();

      var control = FeatureConfigurator.New(ControlName + "Feature", Guids.UntouchableControl)
        .SetDisplayName("UntouchableResistanceControl.Name")
        .SetDescription("UntouchableResistanceControl.Description")
        .SetIcon(srIcon)
        .SetIsClassFeature()
        .AddPrerequisiteFeature(resistance)
        .AddFacts(new() { controlToggle })
        .Configure();

      // ----- Archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.UntouchableRagerArchetype, CharacterClassRefs.BloodragerClass)
          .SetLocalizedName(DisplayName)
          .SetLocalizedDescription(Description);

      // Trades ALL spellcasting (the tabletop also names blood casting, eschew
      // materials, and bloodline spells - all of it rides the same kit here).
      archetype = ArchetypeRemovals.RemoveSpellcasting(
        archetype, bloodrager, SpellbookRefs.BloodragerSpellbook.Reference.Get());

      archetype
        .AddToAddFeatures(LevelPlan.L(4), ResistanceName)
        .AddToAddFeatures(LevelPlan.L(14), ControlName + "Feature")
        .Configure();

      MissionFeats.Logger.Info("UntouchableRager: configured.");
    }
  }

  /// <summary>
  /// Keeps exactly one spell-resistance source active at any moment:
  /// - while bloodraging: the rage SR buff is forced on (the toggle's buff may
  ///   also be on - then the rage buff is suppressed to avoid double SR);
  /// - while calm: the rage SR buff is removed; the toggle's buff governs.
  /// Runs on round ticks and whenever a spell-resistance check targets the
  /// rager, so the ward snaps on the moment it is needed.
  /// </summary>
  [TypeId(Guids.UntouchableMaintenanceComponent)]
  internal class UntouchableResistanceMaintenance : UnitFactComponentDelegate,
    ITickEachRound, ITargetRulebookHandler<RuleSpellResistanceCheck>,
    IRulebookHandler<RuleSpellResistanceCheck>, ITargetRulebookSubscriber, ISubscriber
  {
    public BlueprintBuff RageBuff;
    public BlueprintBuff SrBuff;
    public BlueprintBuff ControlBuff;

    public void OnNewRound()
    {
      Maintain();
    }

    public void OnEventAboutToTrigger(RuleSpellResistanceCheck evt)
    {
      Maintain();
    }

    public void OnEventDidTrigger(RuleSpellResistanceCheck evt)
    {
    }

    private void Maintain()
    {
      try
      {
        bool raging = Owner.HasFact(RageBuff);
        bool controlOn = ControlBuff != null && Owner.HasFact(ControlBuff);
        var sr = Owner.Buffs.GetBuff(SrBuff);
        if (raging)
        {
          if (controlOn)
          {
            // The toggle's buff already carries the ward - suppress the copy.
            if (sr != null)
            {
              Owner.RemoveFact(sr);
            }
          }
          else if (sr is null)
          {
            Owner.AddBuff(SrBuff, Context, 3600f);
            MissionFeats.Logger.Info(
              "[untouchable] raging resistance raised (spell resistance active).");
          }
        }
        else if (sr != null)
        {
          // Calm: rage-granted resistance always drops; the toggle decides.
          Owner.RemoveFact(sr);
          MissionFeats.Logger.Info(
            "[untouchable] rage ended - raging resistance lowered.");
        }
      }
      catch (Exception e)
      {
        MissionFeats.Logger.Error("UntouchableRager: resistance maintenance failed.", e);
      }
    }
  }
}

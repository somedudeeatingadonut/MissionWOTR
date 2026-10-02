using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using MissionWOTR.Feats;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Reckless Bloodrager — Advanced Class Guide pg. 22. The bloodrager who
  /// has no use for the magic in her blood and spends all of it on the swing.
  ///
  /// 0.54.0 replacement for the Untouchable Rager, withdrawn in 0.53.0 as a
  /// duplicate of HomebrewArchetypes. Trap-checked against the loaded-mod
  /// list in the 0.52.1 log (HomebrewArchetypes ships BloodyKnuckledRowdy and
  /// Untouchable Rager; PrestigePlus ships BloodConduit, DrunkenBrute and
  /// UntamedRager; vanilla ships Spelleater, Steelblood, Bloodrider,
  /// Greenrager, Primalist and Crossblooded). Reckless Bloodrager is the one
  /// Advanced Class Guide bloodrager archetype nobody else has.
  ///
  /// THE TRADE: blood casting. She gets no bloodrager spells at all — the
  /// same removal the Untouchable Rager used, component-based through
  /// ArchetypeRemovals.RemoveSpellcasting against the vanilla bloodrager
  /// book, so it does not depend on feature names surviving a build.
  ///
  /// DOCUMENTED ADAPTATION: the tabletop gates Reckless Abandon on being in a
  /// bloodrage. The engine's bloodrage buff is not reachable through any
  /// primitive this build has verified, and gating it wrong would have meant
  /// either a silent no-op or a bonus applied to every bloodrager in the
  /// game, so the bonuses apply at all times instead. In practice a
  /// bloodrager is raging for most of a fight, so the numbers land where the
  /// tabletop puts them; what changes is the out-of-combat case.
  /// </summary>
  internal static class RecklessBloodrager
  {
    internal const string ArchetypeName = "RecklessBloodragerArchetype";

    public static void Configure()
    {
      var bloodrager = CharacterClassRefs.BloodragerClass.Reference.Get();
      // Icon: Toughness, a ref this repo already resolves (Bloodstorm). The
      // bloodrager's own Rage ref is not used anywhere here and so is not a
      // verified lookup in this build.
      var icon = FeatureRefs.Toughness.Reference.Get().Icon;

      // Reckless Abandon (4th): the whole trade in one number. She swings
      // harder and leaves herself open doing it.
      var recklessAbandon = FeatureConfigurator.New("RecklessBloodragerAbandon", Guids.RecklessAbandon)
        .SetDisplayName("RecklessAbandon.Name")
        .SetDescription("RecklessAbandon.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.AdditionalAttackBonus, value: 2,
          descriptor: ModifierDescriptor.UntypedStackable)
        .AddStatBonus(stat: StatType.AdditionalDamage, value: 2,
          descriptor: ModifierDescriptor.UntypedStackable)
        .AddStatBonus(stat: StatType.AC, value: -2,
          descriptor: ModifierDescriptor.UntypedStackable)
        .Configure();

      // Greater Reckless Abandon (12th): the bonus rises to +4 and the
      // penalty stays at -2. Granted as a second +2/+2 rather than a
      // replacement feature, because untyped bonuses stack — the total is
      // the tabletop's +4/+4/-2 without any feature having to be removed.
      var greaterRecklessAbandon = FeatureConfigurator.New(
          "RecklessBloodragerGreaterAbandon", Guids.GreaterRecklessAbandon)
        .SetDisplayName("GreaterRecklessAbandon.Name")
        .SetDescription("GreaterRecklessAbandon.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.AdditionalAttackBonus, value: 2,
          descriptor: ModifierDescriptor.UntypedStackable)
        .AddStatBonus(stat: StatType.AdditionalDamage, value: 2,
          descriptor: ModifierDescriptor.UntypedStackable)
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.RecklessBloodragerArchetype,
            CharacterClassRefs.BloodragerClass)
          .SetLocalizedName("RecklessBloodrager.Name")
          .SetLocalizedDescription("RecklessBloodrager.Description")
          .AddToAddFeatures(LevelPlan.L(4), recklessAbandon)
          .AddToAddFeatures(LevelPlan.L(12), greaterRecklessAbandon);

      archetype = ArchetypeRemovals.RemoveSpellcasting(
        archetype, bloodrager, SpellbookRefs.BloodragerSpellbook.Reference.Get());

      archetype.Configure();
      MissionFeats.Logger.Info("[reckless] configured: " + ArchetypeName + ".");
    }
  }
}

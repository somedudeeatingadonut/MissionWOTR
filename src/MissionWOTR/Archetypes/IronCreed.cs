using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using MissionWOTR.Feats;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// The Iron Creed — MissionWOTR homebrew. The warpriest who gives up the
  /// armour his god blesses so that the weapon his god blesses hits harder.
  ///
  /// 0.54.0 replacement for the Sacred Fist, withdrawn in 0.53.0 as a
  /// duplicate of HomebrewArchetypes. This one is homebrew rather than a port
  /// because there is no tabletop archetype left to port: the Advanced Class
  /// Guide's eight warpriest archetypes are Champion of the Faith, Cult
  /// Leader, Disenchanter, Feral Champion, Mantis Zealot, Proclaimer, Sacred
  /// Fist and Shieldbearer — vanilla WOTR ships seven of them and
  /// HomebrewArchetypes ships the eighth. Verified against the loaded-mod list
  /// in the 0.52.1 log, which also shows Expanded Content patching Mantis
  /// Zealot rather than adding anything new.
  ///
  /// THE TRADE: sacred armor, at every level the progression grants it.
  /// Removed by name rather than guid, and the warpriest progression is
  /// dumped to the log at the same time — the 0.53.0 lesson is that a
  /// removal that silently misses is worse than one that logs, because the
  /// next playtest then says what the real entry was called.
  /// </summary>
  internal static class IronCreed
  {
    internal const string ArchetypeName = "IronCreedArchetype";

    public static void Configure()
    {
      var warpriest = CharacterClassRefs.WarpriestClass.Reference.Get();
      var icon = FeatureRefs.MartialWeaponProficiency.Reference.Get().Icon;

      // Three tiers of the same untyped bonus. Untyped, so they stack to
      // +1/+3/+4 without any earlier feature having to be removed — the same
      // shape the Solipsist's Focused Faith uses.
      var focus = FeatureConfigurator.New("IronCreedFocus", Guids.IronCreedFocus)
        .SetDisplayName("IronCreedFocus.Name")
        .SetDescription("IronCreedFocus.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.AdditionalAttackBonus, value: 1,
          descriptor: ModifierDescriptor.UntypedStackable)
        .AddStatBonus(stat: StatType.AdditionalDamage, value: 1,
          descriptor: ModifierDescriptor.UntypedStackable)
        .Configure();

      var conviction = FeatureConfigurator.New("IronCreedConviction", Guids.IronCreedConviction)
        .SetDisplayName("IronCreedConviction.Name")
        .SetDescription("IronCreedConviction.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.AdditionalAttackBonus, value: 2,
          descriptor: ModifierDescriptor.UntypedStackable)
        .AddStatBonus(stat: StatType.AdditionalDamage, value: 2,
          descriptor: ModifierDescriptor.UntypedStackable)
        .Configure();

      var mastery = FeatureConfigurator.New("IronCreedMastery", Guids.IronCreedMastery)
        .SetDisplayName("IronCreedMastery.Name")
        .SetDescription("IronCreedMastery.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.AdditionalAttackBonus, value: 1,
          descriptor: ModifierDescriptor.UntypedStackable)
        .AddStatBonus(stat: StatType.AdditionalDamage, value: 1,
          descriptor: ModifierDescriptor.UntypedStackable)
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.IronCreedArchetype,
            CharacterClassRefs.WarpriestClass)
          .SetLocalizedName("IronCreed.Name")
          .SetLocalizedDescription("IronCreed.Description")
          .AddToAddFeatures(LevelPlan.L(1), focus)
          .AddToAddFeatures(LevelPlan.L(8), conviction)
          // The creed pays for itself with steel: the last tier also buys a
          // combat feat off the fighter's list, the vanilla selection the
          // Solipsist's Martial Devotion uses.
          .AddToAddFeatures(LevelPlan.L(14), mastery)
          .AddToAddFeatures(LevelPlan.L(14),
            FeatureSelectionRefs.FighterFeatSelection.Reference.Get());

      archetype = ArchetypeRemovals.RemoveEveryGrant(archetype, warpriest, "SacredArmor");
      ArchetypeRemovals.DumpProgression(warpriest, "Sacred");

      archetype.Configure();
      MissionFeats.Logger.Info("[ironcreed] configured: " + ArchetypeName + ".");
    }
  }
}

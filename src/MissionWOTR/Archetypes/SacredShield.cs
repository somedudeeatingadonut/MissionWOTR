using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using MissionWOTR.Feats;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// Sacred Shield — Advanced Player's Guide pg. 116. The paladin who stops
  /// smiting and starts standing in the way.
  ///
  /// 0.54.0 replacement for the Shining Knight, withdrawn in 0.53.0 as a
  /// duplicate of PrestigePlus. Trap-checked against the loaded-mod list in
  /// the 0.52.1 log: vanilla ships Divine Guardian, Divine Hunter, Divine
  /// Scion, Hospitaler, Martyr, Stonelord, Tortured Crusader, Warrior of the
  /// Holy Light and Crusader; HomebrewArchetypes ships FaithfulWanderer,
  /// HolyGuide, Oath of the People's Council, Oath of Vengeance and
  /// Wilderness Warden; PrestigePlus ships Shining Knight and Divine
  /// Champion; Expanded Content ships Divine Scourge, Silver Champion,
  /// Temple Champion, Conqueror and Faithful Paragon. Sacred Shield is in
  /// none of them.
  ///
  /// THE TRADE: smite evil, every grant of it. Removed by NAME at every level
  /// the live progression grants one rather than by guid, which is what the
  /// 0.53.0 teamwork-feat fix established — the hardcoded guid route silently
  /// skips when the id churns, and smite is re-granted several times as its
  /// daily uses climb.
  ///
  /// DOCUMENTED CUTS: the tabletop's In Harm's Way (3rd, intercepting damage
  /// meant for an adjacent ally) and the Bastion of Faith damage reduction
  /// both need machinery this port does not use — a damage-interception
  /// handler and an alignment-qualified resistance respectively. What ships
  /// is the defensive numbers alone.
  /// </summary>
  internal static class SacredShield
  {
    internal const string ArchetypeName = "SacredShieldArchetype";

    public static void Configure()
    {
      var paladin = CharacterClassRefs.PaladinClass.Reference.Get();
      var icon = FeatureRefs.SmiteEvilFeature.Reference.Get().Icon;

      // Sacred Shield (3rd): the shield becomes the point.
      var guard = FeatureConfigurator.New("SacredShieldGuard", Guids.SacredShieldGuard)
        .SetDisplayName("SacredShieldGuard.Name")
        .SetDescription("SacredShieldGuard.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.AC, value: 2, descriptor: ModifierDescriptor.Shield)
        .Configure();

      // Bastion of Faith (11th): sacred, so it stacks with the shield bonus
      // rather than replacing it.
      var bastion = FeatureConfigurator.New("SacredShieldBastion", Guids.BastionOfFaith)
        .SetDisplayName("BastionOfFaith.Name")
        .SetDescription("BastionOfFaith.Description")
        .SetIcon(icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.SaveFortitude, value: 2, descriptor: ModifierDescriptor.Sacred)
        .AddStatBonus(stat: StatType.SaveReflex, value: 2, descriptor: ModifierDescriptor.Sacred)
        .AddStatBonus(stat: StatType.SaveWill, value: 2, descriptor: ModifierDescriptor.Sacred)
        .Configure();

      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.SacredShieldArchetype,
            CharacterClassRefs.PaladinClass)
          .SetLocalizedName("SacredShield.Name")
          .SetLocalizedDescription("SacredShield.Description")
          .AddToAddFeatures(LevelPlan.L(3), guard)
          .AddToAddFeatures(LevelPlan.L(11), bastion);

      archetype = ArchetypeRemovals.RemoveEveryGrant(archetype, paladin, "Smite");
      ArchetypeRemovals.DumpProgression(paladin, "Smite");

      archetype.Configure();
      MissionFeats.Logger.Info("[sacredshield] configured: " + ArchetypeName + ".");
    }
  }
}

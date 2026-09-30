using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.Classes.Selection;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using MissionWOTR.Feats;

namespace MissionWOTR.Archetypes
{
  /// <summary>
  /// THE JIUWEIHU (shaman tabletop port - Kitsune Compendium pg.
  /// 8, Everyman Gaming; the user's pick, replacing the Speaker for
  /// the Past, whose time-mystery half had to be cut - user:
  /// "with so many cuts, lets try to figure out a different shaman
  /// archetype... might need the jewel bound hex made, if possible
  /// to do, or find a suitable alternative, otherwise its viable").
  ///
  /// The tabletop brief: "Considered bringers of fortune and
  /// prosperity, a jiuweihu is a kitsune shaman who has been
  /// blessed with multiple tails by the spirits of unseen kami.
  /// In rare cases, evil jiuweihu who are instead championed by
  /// wicked oni are created." Only kitsune may select it - gated
  /// here on ChangeShapeKitsune, the one racial feature every
  /// kitsune carries.
  ///
  /// The tabletop has exactly two trades, and both sides of this
  /// port ride REAL content - the Magical Tail feats ship with the
  /// game (the user's own Player.log shows them: MagicalTail1-8,
  /// patched at runtime by TabletopTweaks for those who run it),
  /// each granting a spell-like ability (vanish, hideous laughter,
  /// blur, invisibility, heroism, displacement, confusion, dominate
  /// person - 2/day, Charisma-based):
  ///
  /// - STAR JEWEL (1st, replaces the spirit animal): "the jiuweihu
  ///   gains the jewel bound familiar witch hex" - the familiar
  ///   bonds into a grape-sized gem (item form, ioun orbit,
  ///   hardness; none of which the engine exposes). The SUITABLE
  ///   ALTERNATIVE the user asked for: the spirit animal selection
  ///   is removed (found in the live progression by name) and the
  ///   spirit simply RESTS IN THE JEWEL - it cannot be slain, and
  ///   as a bringer of fortune the jiuweihu gains a +1 luck bonus
  ///   on all saving throws. Documented in the ability text.
  ///
  /// - SPIRIT TAILS (1st and every two levels, replaces spirit
  ///   magic): Magical Tail as a bonus feat - the vanilla feats
  ///   MagicalTail1 through MagicalTail8 granted in order at 1st,
  ///   3rd, 5th, 7th, 9th, 11th, 13th and 15th. The after-eight
  ///   rider ("chooses one of her Magical Tail spell-like abilities
  ///   and increases the number of times per day that she can cast
  ///   it by one; she cannot select a spell-like ability more than
  ///   once") ports on the game's own
  ///   MagicalTail{n}IncreaseResource features as a pick-one
  ///   selection at 17th and 19th - OnlyNew mode enforces the
  ///   "never the same tail twice" rule exactly.
  ///
  /// The one port deviation (documented): the tabletop's spirit
  /// magic trade could not be made - spirit magic has no blueprint
  /// of its own (it is embedded in each spirit's components), so
  /// nothing can be removed from the progression. The jiuweihu
  /// keeps her spirit magic; the port is up that one trade.
  /// Log prefix: [jiuweihu].
  /// </summary>
  internal static class Jiuweihu
  {
    internal const string ArchetypeName = "JiuweihuArchetype";

    public static void Configure()
    {
      var shaman = CharacterClassRefs.ShamanClass.Reference.Get();

      // ----- Star Jewel (the spirit animal trade) -----
      // The jewel bound hex itself (familiar turns into an item,
      // orbits like an ioun stone, gains hardness) has no engine
      // support - the suitable alternative: the spirit rests in
      // the gem (no pet to slay) and lends its fortune.
      var starJewel = FeatureConfigurator.New(
        "JiuweihuStarJewel", Guids.JiuweihuStarJewel)
        .SetDisplayName("JiuweihuStarJewel.Name")
        .SetDescription("JiuweihuStarJewel.Description")
        .SetIcon(FeatureRefs.KitsuneSpellLike.Reference.Get().Icon)
        .SetIsClassFeature()
        .AddStatBonus(stat: StatType.SaveFortitude, value: 1, descriptor: ModifierDescriptor.Luck)
        .AddStatBonus(stat: StatType.SaveReflex, value: 1, descriptor: ModifierDescriptor.Luck)
        .AddStatBonus(stat: StatType.SaveWill, value: 1, descriptor: ModifierDescriptor.Luck)
        .Configure();

      // ----- Gift of the Ninth Tail (the after-eight rider) -----
      // The game's own per-tail resource-boost features as a
      // pick-one selection: OnlyNew is the tabletop's "she cannot
      // select a spell-like ability more than once in this way".
      var tailBlessing = FeatureSelectionConfigurator.New(
        "JiuweihuTailBlessingSelection", Guids.JiuweihuTailBlessing)
        .SetDisplayName("JiuweihuTailBlessing.Name")
        .SetDescription("JiuweihuTailBlessing.Description")
        .SetIcon(FeatureRefs.MagicalTail1.Reference.Get().Icon)
        .SetIsClassFeature()
        .SetMode(SelectionMode.OnlyNew)
        .Configure();
      FeatureSelectionConfigurator.For(tailBlessing)
        .AddToAllFeatures(
          FeatureRefs.MagicalTail1IncreaseResource.ToString(),
          FeatureRefs.MagicalTail2IncreaseResource.ToString(),
          FeatureRefs.MagicalTail3IncreaseResource.ToString(),
          FeatureRefs.MagicalTail4IncreaseResource.ToString(),
          FeatureRefs.MagicalTail5IncreaseResource.ToString(),
          FeatureRefs.MagicalTail6IncreaseResource.ToString(),
          FeatureRefs.MagicalTail7IncreaseResource.ToString(),
          FeatureRefs.MagicalTail8IncreaseResource.ToString())
        .Configure();

      // ----- The archetype -----
      var archetype =
        ArchetypeConfigurator.New(ArchetypeName, Guids.JiuweihuArchetype, CharacterClassRefs.ShamanClass)
          .SetLocalizedName("Jiuweihu.Name")
          .SetLocalizedDescription("Jiuweihu.Description")
          // Only kitsune may select this archetype - change shape is
          // the one racial feature every kitsune carries.
          .AddPrerequisiteFeature(FeatureRefs.ChangeShapeKitsune.Cast<BlueprintFeatureReference>())
          // Star Jewel at 1st; a new tail every two levels.
          .AddToAddFeatures(LevelPlan.L(1), starJewel)
          .AddToAddFeatures(LevelPlan.L(1), FeatureRefs.MagicalTail1.Reference.Get())
          .AddToAddFeatures(LevelPlan.L(3), FeatureRefs.MagicalTail2.Reference.Get())
          .AddToAddFeatures(LevelPlan.L(5), FeatureRefs.MagicalTail3.Reference.Get())
          .AddToAddFeatures(LevelPlan.L(7), FeatureRefs.MagicalTail4.Reference.Get())
          .AddToAddFeatures(LevelPlan.L(9), FeatureRefs.MagicalTail5.Reference.Get())
          .AddToAddFeatures(LevelPlan.L(11), FeatureRefs.MagicalTail6.Reference.Get())
          .AddToAddFeatures(LevelPlan.L(13), FeatureRefs.MagicalTail7.Reference.Get())
          .AddToAddFeatures(LevelPlan.L(15), FeatureRefs.MagicalTail8.Reference.Get())
          // After the eighth tail: the ninth tail's gift, twice.
          .AddToAddFeatures(LevelPlan.L(17), tailBlessing)
          .AddToAddFeatures(LevelPlan.L(19), tailBlessing);

      // The trade: the spirit animal (found in the live progression
      // by name; the spirit magic side could not be excised - see
      // the class comment).
      archetype = ArchetypeRemovals.AddRemovals(
        archetype, shaman, "ShamanSpiritAnimalSelection");

      archetype.Configure();
      MissionFeats.Logger.Info("[jiuweihu] configured: " + ArchetypeName + ".");
    }
  }
}

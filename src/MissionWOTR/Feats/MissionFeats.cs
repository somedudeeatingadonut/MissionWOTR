using BlueprintCore.Utils;
using BlueprintCore.Blueprints.References;
using Kingmaker.Blueprints.Classes;
using System.Collections.Generic;
using System.Linq;
using MissionWOTR.Archetypes;
using MissionWOTR.Mythics;
using System;

namespace MissionWOTR.Feats
{
  /// <summary>
  /// Entry point for all feat configuration. Each feat is configured in its own try/catch so a
  /// single broken feat can never hang or crash the game during blueprint loading.
  /// </summary>
  internal static class MissionFeats
  {
    internal static readonly LogWrapper Logger = LogWrapper.Get("MissionWOTR.Feats");

    internal static void ConfigureAll()
    {
      Logger.Info("Configuring MissionWOTR feats.");

      Configure(nameof(VengefulCounterstrike), VengefulCounterstrike.Configure);
      Configure(nameof(ArcaneMomentum), ArcaneMomentum.Configure);
      Configure(nameof(BattlefieldScavenger), BattlefieldScavenger.Configure);
      Configure(nameof(SecondWind), SecondWind.Configure);
      Configure(nameof(TauntingBlows), TauntingBlows.Configure);
      Configure(nameof(ResonantStrikes), ResonantStrikes.Configure);
      Configure(nameof(WardedSoul), WardedSoul.Configure);
      // Batch 2.
      Configure(nameof(SteadfastAim), SteadfastAim.Configure);
      Configure(nameof(GuardedMomentum), GuardedMomentum.Configure);
      Configure(nameof(TunnelFighter), TunnelFighter.Configure);

      // Mythic feats (tabletop ports) and mythic abilities (originals).
      Configure(nameof(AcrobaticMythic), AcrobaticMythic.Configure);
      Configure(nameof(PersuasiveMythic), PersuasiveMythic.Configure);
      Configure(nameof(MagicalAptitudeMythic), MagicalAptitudeMythic.Configure);
      Configure(nameof(IronWillMythic), IronWillMythic.Configure);
      Configure(nameof(LightningReflexesMythic), LightningReflexesMythic.Configure);
      Configure(nameof(EnduranceMythic), EnduranceMythic.Configure);
      Configure(nameof(Untouchable), Untouchable.Configure);
      Configure(nameof(SlayersVigor), SlayersVigor.Configure);
      Configure(nameof(AscendantEdge), AscendantEdge.Configure);
      Configure(nameof(LastStand), LastStand.Configure);
      Configure(nameof(DesperateFury), DesperateFury.Configure);
      Configure(nameof(DefiantSoul), DefiantSoul.Configure);
      Configure(nameof(RelentlessOnslaught), RelentlessOnslaught.Configure);
      Configure(nameof(AetherialBulwark), AetherialBulwark.Configure);
      Configure(nameof(TitansWrath), TitansWrath.Configure);
      Configure(nameof(TitanHide), TitanHide.Configure);

      // Class archetypes (level 1 test mode; see LevelPlan + docs/ARCHETYPES.md).
      Configure(nameof(EldritchPoisoner), EldritchPoisoner.Configure);
      Configure(nameof(ConstructCrafter), ConstructCrafter.Configure);
      Configure(nameof(Breaker), Breaker.Configure);
      Configure(nameof(Bloodstorm), Bloodstorm.Configure);
      Configure(nameof(CovertMage), CovertMage.Configure);
      Configure(nameof(ElementalObsessor), ElementalObsessor.Configure);
      Configure(nameof(MummerMage), MummerMage.Configure);
      Configure(nameof(Cook), Cook.Configure);
      Configure(nameof(Spellfist), Spellfist.Configure);
      Configure(nameof(Spellblade), Spellblade.Configure);
      Configure(nameof(SanguineFont), SanguineFont.Configure);
      Configure(nameof(SisterInArms), SisterInArms.Configure);
      Configure(nameof(Carousel), Carousel.Configure);

      // BUGFIX (0.11.0): the archetypes below shipped in 0.7.0-0.10.1 listed
      // in the diagnostics table but were never wired into ConfigureAll - they
      // compiled and passed CI, yet their blueprints were never created
      // in-game. Registered for real now, alongside the new Hammerfist.
      Configure(nameof(Cardinal), Cardinal.Configure);
      Configure(nameof(Solipsist), Solipsist.Configure);
      Configure(nameof(LionShaman), LionShaman.Configure);
      Configure(nameof(TrueShape), TrueShape.Configure);
      Configure(nameof(PolearmMaster), PolearmMaster.Configure);
      Configure(nameof(StrategicSoldier), StrategicSoldier.Configure);
      Configure(nameof(Exoticist), Exoticist.Configure);
      Configure(nameof(Qinggong), Qinggong.Configure);
      Configure(nameof(Hammerfist), Hammerfist.Configure);
      Configure(nameof(Venomblood), Venomblood.Configure);
      Configure(nameof(Riftstalker), Riftstalker.Configure);
      Configure(nameof(Verminous), Verminous.Configure);
      Configure(nameof(SinEater), SinEater.Configure);
      Configure(nameof(Doomsayer), Doomsayer.Configure);
      Configure(nameof(KineticistExplosion), KineticistExplosion.Configure);
      Configure(nameof(UnstableKineticist), UnstableKineticist.Configure);
      Configure(nameof(KineticChirurgeon), KineticChirurgeon.Configure);
      Configure(nameof(Stormcaller), Stormcaller.Configure);
      Configure(nameof(PlanarOracle), PlanarOracle.Configure);
      Configure(nameof(Apocryphal), Apocryphal.Configure);
      Configure(nameof(FortunesFool), FortunesFool.Configure);
      Configure(nameof(Guide), Guide.Configure);
      Configure(nameof(Wildbond), Wildbond.Configure);
      Configure(nameof(Scout), Scout.Configure);
      Configure(nameof(SteelRain), SteelRain.Configure);
      Configure(nameof(Anatomist), Anatomist.Configure);
      Configure(nameof(Chimera), Chimera.Configure);
      Configure(nameof(HedgeWitch), HedgeWitch.Configure);
      Configure(nameof(Jiuweihu), Jiuweihu.Configure);
      Configure(nameof(SpiritRidden), SpiritRidden.Configure);
      Configure(nameof(SpellWarrior), SpellWarrior.Configure);
      Configure(nameof(CrescendoSkald), CrescendoSkald.Configure);
      Configure(nameof(Dreadnaught), Dreadnaught.Configure);
      Configure(nameof(GraveWarden), GraveWarden.Configure);
      Configure(nameof(Overchanneler), Overchanneler.Configure);
      Configure(nameof(MendingBlade), MendingBlade.Configure);
      Configure(nameof(Inkbound), Inkbound.Configure);
      Configure(nameof(ElementalistShifter), ElementalistShifter.Configure);
      Configure(nameof(Beastbound), Beastbound.Configure);
      Configure(nameof(HolyVindicator), HolyVindicator.Configure);
      Configure(nameof(Bonewatch), Bonewatch.Configure);

      // 0.54.0 - the five replacements for the archetypes withdrawn in 0.53.0
      // as duplicates of mods the user already runs. Registered here AND in
      // the diagnostics table below; the 0.11.0 bug was archetypes that had
      // the diagnostics entry and no Configure call, so they compiled, passed
      // CI, and never existed in game.
      Configure(nameof(RecklessBloodrager), RecklessBloodrager.Configure);
      Configure(nameof(SacredShield), SacredShield.Configure);
      Configure(nameof(Bloodmarked), Bloodmarked.Configure);
      Configure(nameof(IronCreed), IronCreed.Configure);
      Configure(nameof(PactWizard), PactWizard.Configure);
      Configure(nameof(SacredVow), MissionWOTR.Mythics.SacredVow.Configure);

      // 0.53.0 — WITHDRAWN as duplicates of content the user already runs,
      // on the user's report and the loaded-mod list in the 0.52.1 log:
      //   UntouchableRager  - HomebrewArchetypes (BloodragerArchetypes/
      //                       Untouchable Rager/UntouchableRagerArchetype.jbp)
      //   SacredFist        - HomebrewArchetypes (WarpriestArchetypes/SacredFist/)
      //   UndeadMaster      - HomebrewArchetypes
      //   ShiningKnight     - PrestigePlus
      //   EldritchScrapper  - Ebon's Content Mod
      // Their Configure calls, diagnostics entries and source files are
      // gone; the Guids.cs constants stay, annotated, per the Intercessor
      // precedent. (Crusader was reported too, but this mod never shipped
      // one - the 0.7.0 cleric pair deliberately avoided it for exactly
      // this reason. Nothing to withdraw.)

      // The archetype references the feats above, so it is configured last.
      Configure(nameof(MissionVanguard), MissionVanguard.Configure);

      Logger.Info("MissionWOTR feat configuration complete.");
    }

    /// <summary>
    /// Post-configure state dump for every class archetype: proves whether each one was
    /// created, landed on its class, and passes the availability filter the char-gen UI
    /// uses. Written to the game log so playtest reports are self-diagnosing.
    /// </summary>
    internal static void LogArchetypeDiagnostics()
    {
      try
      {
        var alchemist = CharacterClassRefs.AlchemistClass.Reference.Get();
        var magus = CharacterClassRefs.MagusClass.Reference.Get();
        var barbarian = CharacterClassRefs.BarbarianClass.Reference.Get();
        var arcanist = CharacterClassRefs.ArcanistClass.Reference.Get();
        var bard = CharacterClassRefs.BardClass.Reference.Get();
        var bloodrager = CharacterClassRefs.BloodragerClass.Reference.Get();
        var cavalier = CharacterClassRefs.CavalierClass.Reference.Get();
        var cleric = CharacterClassRefs.ClericClass.Reference.Get();
        var paladin = CharacterClassRefs.PaladinClass.Reference.Get();
        var ranger = CharacterClassRefs.RangerClass.Reference.Get();
        var rogue = CharacterClassRefs.RogueClass.Reference.Get();
        var druid = CharacterClassRefs.DruidClass.Reference.Get();
        var fighter = CharacterClassRefs.FighterClass.Reference.Get();
        var monk = CharacterClassRefs.MonkClass.Reference.Get();
        var hunter = CharacterClassRefs.HunterClass.Reference.Get();
        var inquisitor = CharacterClassRefs.InquisitorClass.Reference.Get();
        var kineticist = CharacterClassRefs.KineticistClass.Reference.Get();
        var oracle = CharacterClassRefs.OracleClass.Reference.Get();
        var witch = CharacterClassRefs.WitchClass.Reference.Get();
        var shaman = CharacterClassRefs.ShamanClass.Reference.Get();
        var skald = CharacterClassRefs.SkaldClass.Reference.Get();
        var slayer = CharacterClassRefs.SlayerClass.Reference.Get();
        var sorcerer = CharacterClassRefs.SorcererClass.Reference.Get();
        var warpriest = CharacterClassRefs.WarpriestClass.Reference.Get();
        var wizard = CharacterClassRefs.WizardClass.Reference.Get();
        var shifter = CharacterClassRefs.ShifterClass.Reference.Get();
        var entries = new (string Name, string Guid, BlueprintCharacterClass Class)[]
        {
          ("EldritchPoisoner", Guids.EldritchPoisonerArchetype, alchemist),
          ("ConstructCrafter", Guids.ConstructCrafterArchetype, alchemist),
          ("MissionVanguard", Guids.MissionVanguardArchetype, magus),
          ("Breaker", Guids.BreakerArchetype, barbarian),
          ("Bloodstorm", Guids.BloodstormArchetype, barbarian),
          ("CovertMage", Guids.CovertMageArchetype, arcanist),
          ("ElementalObsessor", Guids.ElementObsessorArchetype, arcanist),
          ("MummerMage", Guids.MummerArchetype, bard),
          ("Cook", Guids.CookArchetype, bard),
          ("Spellfist", Guids.SpellfistArchetype, magus),
          ("Spellblade", Guids.SpellbladeArchetype, magus),
          ("SanguineFont", Guids.SanguineFontArchetype, bloodrager),
      ("RecklessBloodrager", Guids.RecklessBloodragerArchetype, bloodrager),
      ("SisterInArms", Guids.SisterArchetype, cavalier),
      ("Carousel", Guids.CarouselArchetype, cavalier),
      ("Cardinal", Guids.CardinalArchetype, cleric),
      ("Solipsist", Guids.SolipsistArchetype, cleric),
      ("LionShaman", Guids.LionShamanArchetype, druid),
      ("TrueShape", Guids.TrueShapeArchetype, druid),
      ("PolearmMaster", Guids.PolearmMasterArchetype, fighter),
      ("StrategicSoldier", Guids.StrategicSoldierArchetype, fighter),
      ("Exoticist", Guids.ExoticistArchetype, fighter),
      ("Qinggong", Guids.QinggongArchetype, monk),
      ("Hammerfist", Guids.HammerfistArchetype, monk),
      ("Venomblood", Guids.VenombloodArchetype, hunter),
      ("Riftstalker", Guids.RiftstalkerArchetype, hunter),
      ("Verminous", Guids.VerminousArchetype, hunter),
      ("SinEater", Guids.SinEaterArchetype, inquisitor),
      ("Doomsayer", Guids.DoomsayerArchetype, inquisitor),
      ("UnstableKineticist", Guids.UnstableKineticistArchetype, kineticist),
      ("KineticChirurgeon", Guids.KineticChirurgeonArchetype, kineticist),
      ("Stormcaller", Guids.StormcallerArchetype, kineticist),
      ("PlanarOracle", Guids.PlanarOracleArchetype, oracle),
      ("Apocryphal", Guids.ApocryphalArchetype, oracle),
      ("FortunesFool", Guids.FortunesFoolArchetype, paladin),
      ("SacredShield", Guids.SacredShieldArchetype, paladin),
      ("Guide", Guids.GuideArchetype, ranger),
      ("Wildbond", Guids.WildbondArchetype, ranger),
      ("Scout", Guids.ScoutArchetype, rogue),
      ("SteelRain", Guids.SteelRainArchetype, rogue),
      ("Anatomist", Guids.AnatomistArchetype, rogue),
      ("Chimera", Guids.ChimeraArchetype, witch),
      ("HedgeWitch", Guids.HedgeWitchArchetype, witch),
      ("Jiuweihu", Guids.JiuweihuArchetype, shaman),
      ("SpiritRidden", Guids.SpiritRiddenArchetype, shaman),
      ("SpellWarrior", Guids.SpellWarriorArchetype, skald),
      ("CrescendoSkald", Guids.CrescendoSkaldArchetype, skald),
      ("Dreadnaught", Guids.DreadnaughtArchetype, slayer),
      ("GraveWarden", Guids.GraveWardenArchetype, slayer),
      ("Overchanneler", Guids.OverchannelerArchetype, sorcerer),
      ("Bloodmarked", Guids.BloodmarkedArchetype, sorcerer),
      ("MendingBlade", Guids.MendingBladeArchetype, warpriest),
      ("IronCreed", Guids.IronCreedArchetype, warpriest),
      ("Inkbound", Guids.InkboundArchetype, wizard),
      ("PactWizard", Guids.PactWizardArchetype, wizard),
      ("ElementalistShifter", Guids.ElementalistShifterArchetype, shifter),
      ("Beastbound", Guids.BeastboundArchetype, shifter),
    };
        foreach (var entry in entries)
        {
          BlueprintArchetype archetype;
          try
          {
            archetype = BlueprintTool.Get<BlueprintArchetype>(entry.Guid);
          }
          catch (Exception)
          {
            // BlueprintTool.Get throws (does not return null) when the blueprint
            // was never created - i.e. that archetype's Configure failed.
            Logger.Warn(
              $"[diag] {entry.Name}: fetch failed - blueprint NOT created (its Configure crashed).");
            continue;
          }
          if (archetype is null)
          {
            Logger.Warn($"[diag] {entry.Name}: blueprint NOT created.");
            continue;
          }
          var inClass = entry.Class.Archetypes.Contains(archetype);
          var inAvailable = entry.Class.AvailableArchetypes.Contains(archetype);
          var componentNames = string.Join(",", archetype.ComponentsArray
            .Select(c => c.GetType().Name).OrderBy(n => n));
          // MinFeatureLevel is Min() over AddFeatures and throws when empty.
          var addLevels = archetype.AddFeatures?.Length ?? 0;
          var minLevel = addLevels > 0 ? archetype.MinFeatureLevel : 0;
          Logger.Info(
            $"[diag] {entry.Name}: created={true}, components={archetype.ComponentsArray.Length} [{componentNames}], " +
            $"addLevels={addLevels}, removeLevels={(archetype.RemoveFeatures?.Length ?? 0)}, " +
            $"minLevel={minLevel}, onClass={inClass}, inAvailableList={inAvailable}.");
          // Level-1 grant list with dereferenced names: a null here means a dangling
          // reference - the feature would silently never reach the character.
          // (Reflection: the game's LevelEntry feature-list member name varies by
          // version, so both spellings are tried.)
          var levelOneNames = new List<string>();
          foreach (var e in archetype.AddFeatures ?? Array.Empty<LevelEntry>())
          {
            if (e.Level != 1)
            {
              continue;
            }
            foreach (var name in LevelEntryFeatureNames(e))
            {
              levelOneNames.Add(name);
            }
          }
          levelOneNames.Sort();
          Logger.Info(
            $"[diag] {entry.Name} level-1 grants ({levelOneNames.Count}): " +
            string.Join(", ", levelOneNames));
        }
        Logger.Info(
          $"[diag] alchemist archetypes={alchemist.Archetypes.Length} " +
          $"(available={alchemist.AvailableArchetypes.Length}), magus archetypes={magus.Archetypes.Length}, " +
          $"barbarian archetypes={barbarian.Archetypes.Length} (available={barbarian.AvailableArchetypes.Length}), " +
          $"arcanist archetypes={arcanist.Archetypes.Length} (available={arcanist.AvailableArchetypes.Length}), " +
          $"bard archetypes={bard.Archetypes.Length} (available={bard.AvailableArchetypes.Length}).");
      }
      catch (Exception e)
      {
        Logger.Error("[diag] archetype diagnostics failed.", e);
      }
    }

    /// <summary>
    /// Reflection dump of a LevelEntry's feature reference list (names, or NULL-REF
    /// for dangling references). Falls back between member spellings.
    /// </summary>
    private static IEnumerable<string> LevelEntryFeatureNames(LevelEntry entry)
    {
      var list = ReflectMember(entry, "Features") ?? ReflectMember(entry, "m_Features");
      if (list is not System.Collections.IEnumerable items)
      {
        yield return "<no feature list member>";
        yield break;
      }
      foreach (var item in items)
      {
        if (item is null)
        {
          yield return "NULL-REF";
          continue;
        }
        var blueprint = ReflectMember(item, "Get") is System.Reflection.MethodInfo get
          ? get.Invoke(item, null)
          : item;
        yield return ReflectMember(blueprint, "name") as string ?? "NULL-REF";
      }
    }

    private static object ReflectMember(object obj, string name)
    {
      if (obj is null)
      {
        return null;
      }
      const System.Reflection.BindingFlags flags =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance;
      var type = obj as System.Type ?? obj.GetType();
      return (object)type.GetProperty(name, flags)?.GetValue(obj) ??
        (object)type.GetField(name, flags)?.GetValue(obj) ??
        (object)type.GetMethod(name, flags);
    }

    private static void Configure(string name, Action configure)
    {
      try
      {
        configure();
      }
      catch (Exception e)
      {
        Logger.Error($"Failed to configure feat: {name}", e);
      }
    }
  }
}

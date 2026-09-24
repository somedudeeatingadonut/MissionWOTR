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
      Configure(nameof(UntouchableRager), UntouchableRager.Configure);

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
          ("UntouchableRager", Guids.UntouchableRagerArchetype, bloodrager),
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

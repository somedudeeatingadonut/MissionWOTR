# Archetype & class-content coverage — ALWAYS CHECK BEFORE DESIGNING

This file exists because we have twice designed an archetype the game already
ships (Sanctified Slayer for inquisitor; Kinetic Knight / Dark Elementalist /
Overwhelming Soul for kineticist — all vanilla, caught by the user).

**Policy — before ANY new archetype or class-content piece:**

1. Read this file.
2. Run the vanilla check: in a fresh WW-Blueprint-Core clone, grep the
   archetype/class name against
   `BlueprintCore/BlueprintCore/Blueprints/References/ArchetypeRefs.cs`
   (and `FeatureRefs.cs` / `AbilityRefs.cs` for class content). Every
   blueprint the game ships is listed there — if the name (or its obvious
   synonyms) appears, it is already in the game.
3. Check the current release page of every mod listed below for the class
   in question.
4. Search Nexus for "<class> archetype" and skim the first page.
5. Record the verdict in this file before writing any code, and update this
   file whenever a release of ours or of a listed mod changes the picture.

---

## Vanilla WOTR archetypes (verified inventory)

| Class | Vanilla archetypes | Status |
|---|---|---|
| Hunter | Forester, Divine Hunter, Urban Hunter, Colluding Scoundrel, Divine Hound | ✓ verified (0.15.0 work + wiki check) |
| Inquisitor | Monster Tactician, Sacred Huntsmaster, Sanctified Slayer | ✓ verified (blueprint refs + user, 0.16.0); Sin Eater confirmed ABSENT (our port) |
| Kineticist | Kinetic Knight, Dark Elementalist, Overwhelming Soul | ✓ verified (user + refs, 0.19.0) |
| Alchemist | Not yet inventoried — run the policy check before designing | ⚠ method above |
| Fighter, Monk, Druid, Cavalier, Barbarian, others | Not yet inventoried — run the policy check before designing | ⚠ method above |

Vanilla kineticist class content: all 11 tabletop composite blasts for the
four base elements; metakinesis Empower/Maximize/Quicken (+ mythic Master);
Burning infusion; infusions listed in docs/ARCHETYPES.md's kineticist
section. Missing from vanilla: Extreme Range and Kinetic Whip (we skip by
user request), Impale (blocked: no line-shape builder), Draining,
Disintegrating.

## Mods that add archetypes / class content (check before designing)

| Mod | Adds | How to check |
|---|---|---|
| HomebrewArchetypes | Archetypes (our long-standing pre-design check) | Its current Nexus/GitHub list |
| TabletopTweaks (Vek17) | Archetypes in the MAIN mod (TTT-Core is just the library — cloning the Core repo shows none) | Main repo's NewContent folder / Nexus page |
| Call of the Wild | Many classes and archetypes | Nexus page / release notes |
| ExpandedContent | Content incl. units (already a Construct Crafter dependency reference) | Nexus page |
| Kineticist Elements Expanded | Aether/Void/Wood elements (abandoned) | Nexus page — do not duplicate regardless |

## What THIS mod ships (do not re-add)

- Alchemist: Construct Crafter (0.5.4+; command menus 0.16.0)
- Fighter: Hammerfist · Monk: Qinggong rework · Druid: PolearmMaster, LionShaman,
  TrueShape · Cavalier: StrategicSoldier, Exoticist · Barbarian: Cardinal? no —
  see docs/ARCHETYPES.md for the authoritative per-class list
- Hunter: Venomblood (0.12.0), Riftstalker (homebrew, 0.13.0–0.15.0),
  Verminous (tabletop port, 0.15.0)
- Inquisitor: Sin Eater (tabletop port, 0.16.0), Doomsayer (homebrew, 0.17.0)
- Kineticist: Explosion infusion (0.18.0), Unstable Kineticist (homebrew, 0.19.0)

The authoritative list of our content is docs/ARCHETYPES.md; this file is
the collision-check index.

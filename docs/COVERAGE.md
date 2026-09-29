# Archetype & class-content coverage — ALWAYS CHECK BEFORE DESIGNING

This file exists because we have repeatedly designed content the game already
ships (Sanctified Slayer for inquisitor; the kineticist trio Kinetic Knight /
Dark Elementalist / Overwhelming Soul — all vanilla, caught by the user).

**Policy — before ANY new archetype or class-content piece:**

1. Read this file AND docs/NOTES.md.
2. Run the vanilla check: in a fresh WW-Blueprint-Core clone, grep the
   archetype/class name against
   `BlueprintCore/BlueprintCore/Blueprints/References/ArchetypeRefs.cs`
   (and `FeatureRefs.cs` / `AbilityRefs.cs` for class content). Every
   blueprint the game ships is listed there — if the name (or its obvious
   synonyms) appears, it is already in the game. The full game list is 200+
   archetypes (see table below).
3. Check the current release page of every mod listed below for the class
   in question.
4. Search Nexus for "<class> archetype" and skim the first page.
5. Record the verdict in this file before writing any code, and update this
   file whenever a release of ours or of a listed mod changes the picture.

---

## Vanilla WOTR archetypes (from the game's own blueprint list — 202 entries)

Class attribution below was classified from the blueprint names; names marked
"~" are lower-confidence, and anything uncertain lives in the unclassified
bucket at the bottom. Before designing for a name, resolve it with the grep
in step 2 — that is authoritative, this table is a map.

| Class | Vanilla archetypes |
|---|---|
| Alchemist | Chirurgeon, Grenadier, Incense Synthesizer, Metamorph, Preservationist, Reanimator, Underground Chemist, Vivisectionist |
| Arcanist | Brown-Fur Transmuter, Eldritch Font, Spell Master, Nature Mage ~ |
| Barbarian | Armored Hulk, Beastkin Berserker, Flesh Eater, Instinctual Warrior ~, Invulnerable Rager, Mad Dog |
| Bard | Archaeologist, Chelish Diva, Daredevil, Dirge Bard, Flame Dancer, Thundercaller |
| Bloodrager | Bloodrider, Greenrager, Mixed Blood Rager, Primalist, Spelleater, Steelblood |
| Cavalier | Beast Rider, Gendarme, Standard Bearer (+ Order of the Paw is an ORDER, not an archetype) |
| Cleric | Angelfire Apostle, Crusader, Ecclesitheurge, Separatist |
| Druid | Drovier, Feyspeaker, Primal Druid ~ |
| Fighter | Bulwark, Knight of the Wall, Mutation Warrior, Swordlord, Titan Fighter, Two-Handed Fighter, Tower Shield Specialist |
| Hunter | Colluding Scoundrel, Divine Hunter, Forester, Urban Hunter |
| Inquisitor | Deliverer, Faith Hunter, Judge, Mantis Zealot, Monster Tactician, Sacred Huntsmaster, Sanctified Slayer |
| Kineticist | Blood Kineticist, Dark Elementalist, Elemental Engine, Kinetic Knight, Kinetic Sharpshooter, Overwhelming Soul, Psychokineticist |
| Magus | Bladebound, Eldritch Archer, Eldritch Scion, Hexcrafter, Spell Dancer, Sword Saint |
| Monk | Drunken Master, Quarterstaff Master, Scaled Fist, Sensei, Sohei, Student of Stone, Zen Archer |
| Oracle | Dual-Cursed, Enlightened Philosopher, Possessed, Seeker |
| Paladin | Divine Guardian, Hospitaler, Martyr, Stonelord, Warrior of the Holy Light |
| Ranger | Demonslayer, Flamewarden, Freebooter |
| Rogue | Dark Lurker, Eldritch Scoundrel, Knife Master, Master Spy, Sylvan Trickster, Thug |
| Shaman | Possessed, Shadow Shaman, Unsworn, Wildland, Witch Doctor |
| Shifter | Child of the Manticore, Dragonblood, Feyform, Fiendflesh, Griffonheart, Rageshaper, Wild Effigy |
| Skald | Battle Singer, Court Poet |
| Sorcerer | Crossblooded, Empyreal, Nine-Tailed Heir, Sage, Sylvan |
| Witch | Accursed, Cauldron, Elemental, Hagbound, Hex Channeler, Ley Line Guardian, Ray Master, Witch of the Veil |
| Wizard | Arcane Bomber, Exploiter, Scrollmaster, Thassilonian Specialist |

**Unclassified blueprint names (verify with the step-2 grep before designing
near them):** Aggressor, Arcane Enforcer, Armiger, Armored Battlemage,
Beast Tamer, Bodyguard, Bully, Bloodseeker, Cult Leader, Dark Sister,
Deathtouched, Demon Dancer, Divine Herbalist, Divine Scion, Executioner,
Geomancer, Hag-Riven, Herald Caller, Hunt Caller, Imitator, Nomad,
Phantasmal Mage, Priest of Balance, Prophet of Pestilence, Provocateur,
Purifier, Racer, Rowdy, Spirit Hunter, Stormwalker, Tactical Leader,
Tandem Executioner, Tranquil Whisperer, Unlettered Arcanist, Vanguard,
Wandering Marksman, Winter Child, Wind Whisperer. Non-class entries:
AnimalCompanion, the two Ousider* save entries, DragonAzata.

Vanilla kineticist class content: all 11 tabletop composite blasts for the
four base elements; metakinesis Empower/Maximize/Quicken (+ mythic Master);
Burning infusion; infusions per docs/ARCHETYPES.md's kineticist section.
Unused tabletop kineticist archetypes: **Elemental Annihilator** and
**Kinetic Chirurgeon** (Chirurgeon implemented by us, 0.20.0).

## Mods that add archetypes / class content (check before designing)

| Mod | Adds | How to check |
|---|---|---|
| HomebrewArchetypes | Archetypes (our long-standing pre-design check) | Its current Nexus/GitHub list |
| TabletopTweaks (Vek17) | Archetypes in the MAIN mod (TTT-Core is just the library — cloning the Core repo shows none) | Main repo's NewContent folder / Nexus page |
| ExpandedContent | Content incl. units (already a Construct Crafter dependency reference) | Nexus page |
| Kineticist Elements Expanded | Aether/Void/Wood elements (abandoned) | Nexus page — do not duplicate regardless |

NOTE: Call of the Wild is KINGMAKER-ONLY — it is not a WOTR mod. Do not
check against it, port from it, or list it. (Removed from this table at the
user's direction; also recorded in docs/NOTES.md.)

## What THIS mod ships (do not re-add)

- Alchemist: Construct Crafter (0.5.4+; command menus 0.16.0)
- Hunter: Venomblood (0.12.0), Riftstalker (homebrew, 0.13.0–0.15.0),
  Verminous (tabletop port, 0.15.0)
- Inquisitor: Sin Eater (tabletop port, 0.16.0), Doomsayer (homebrew, 0.17.0)
- Kineticist: Explosion infusion (0.18.0), Unstable Kineticist (homebrew,
  0.19.0), Kinetic Chirurgeon (tabletop port, 0.20.0)
- Plus earlier classes — the authoritative per-class list is
  docs/ARCHETYPES.md; this file is the collision-check index.

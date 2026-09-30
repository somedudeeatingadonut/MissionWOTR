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
3. Check the mod inventories below (verified 2026-09-30) AND the
   alterasc mod-content database: https://alterasc.github.io/ — an
   auto-collected blueprint inventory of ~60 content mods, with per-mod
   archetype/class pages. It is the fastest collision check there is.
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

## Mods that add archetypes / classes (verified 2026-09-30)

NOTE: Call of the Wild is KINGMAKER-ONLY — it is not a WOTR mod. Do not
check against it, port from it, or list it. (User direction; also in
docs/NOTES.md.)

NOTE: **Worldcrawl** adds a class and is widely incompatible with other
content mods (Ebon's ships a special Worldcrawl version that even drops an
archetype to cope). Per the user: do not design against it; assume anything
we ship is incompatible with Worldcrawl loads.

### Homebrew Archetypes (Nexus 279; author Sigurd; v1.1Zwi)

- Arcanist: Blood Arcanist, School Savant
- Barbarian: Barbarian (5e-style modded), Hateful Rager, Primal Hunter
- Bard: Arcane Duelist, Dancer, Dervish of Dawn, Stonesinger, Strategist
- Bloodrager: Bloody-Knuckled Rowdy, **Untouchable Rager** (name-twin of
  OUR barbarian UntouchableRager — different class, but do not reuse the
  name for anything else)
- Cavalier: Green Knight
- Cleric: Crusader (the vanilla cleric ALSO has a Crusader — both exist),
  Elder Mythos Cultist, Evangelist, Undead Lord
- Druid: Halcyon Druid
- Fighter: Berserker, Blackguard, Viking
- Inquisitor: Umbral Stalker
- Monk: Hellcat, Monk of the Mantis
- Oracle: Ancient Lorekeeper (elf and half-elf variants)
- Paladin: Faithful Wanderer, Holy Guide, Oathbound (Oath of People's
  Council), Oathbound (Oath of Vengeance), Wilderness Warden
- Ranger: Sword-Devil, Wild Stalker
- Rogue: Carnivalist, Espionage Expert, Thiefling
- Slayer: Contract Killer
- Sorcerer: Arcane Trickster, Dragon Disciple, Eldritch Knight
- Warpriest: Sacred Fist
- Witch: Scarred Witch Doctor
- Wizard: Eldritch Knight, Undead Master
- **New class: Investigator** (archetypes: Empiricist, Jinyiwei, Questioner)
- Also NPC classes (Adept, Aristocrat, Commoner, Expert, Warrior) and
  undead companions.

### Ebon's Content Mod (Nexus 758; GitHub EdgarEbonfowl/EbonsContentMod)

- Arcanist: Collegiate Initiate (absent from its Worldcrawl version)
- Bard: Tabletop Archaeologist (a fuller port than the vanilla
  Archaeologist — different blueprint, same turf)
- Monk: Hungry Ghost Monk
- Sorcerer: Eldritch Scrapper
- Also: many races, spells, bloodlines, wild talents, feats, masterpieces.
  Requires TTT-Core + WrathPatches.

### Expanded Content (Nexus 377; alterasc DB v0.13.35)

- Alchemist: Beastmorph, Mindchemist
- Arcanist: Draconic Scholar
- Cavalier: Drake Rider
- Cleric: Divine Scourge
- Druid: Bear Totem Druid, Draconic Druid, **Lion Totem Druid** (its
  blueprint is literally `LionShamanArchetype` — **COLLIDES with our
  LionShaman**, shipped before we knew), Mooncaller, Storm Druid,
  Treesinger, Urban Druid
- Fighter: Archer, Spear Fighter, Child of Acavna and Amaznen
- Hunter: Plant Master, Skulking Hunter
- Inquisitor: Ravener Hunter, Sworn of the Eldest
- Monk: Water Dancer
- Oracle: Ocean's Echo
- Paladin: Temple Champion, Silver Champion
- Ranger: Divine Tracker, Drake Warden
- Rogue: Clutch Thief
- Shaman: Draconic Shaman
- Skald: Wyrm Singer
- Slayer: Warhound
- Warpriest: Soldier of Gaia, Faithful Paragon
- New prestige class: Stargazer; new classes: Oathbreaker (+Castigator),
  DreadKnight (+Conqueror, Claw of the False Wyrm); rideable companions
  (Sapling Treant, Crawling Mound); Draconic animal archetype.

### Kinetic Archetypes (Nexus 549; GitHub NosVladimir/KineticArchetypes)

- Kineticist: Cinder Adept, Kinetic Duelist, Kinetic Lancer, Onslaught
  Blaster
- Fighter: Esoteric Blade
- Also: the Vital Blade infusion, and a restored supercharge for the DLC
  Kinetic Sharpshooter.

### Tome of Battle: Nine Swords (GitHub V0idhead/WOTRNineSwords)

- **New classes: Warblade, Swordsage** (no base-class archetypes).
  100+ maneuvers, 16 stances, 8 disciplines.

### Also relevant (seen in the alterasc database — check it before designing)

- TabletopTweaks (main mod): archetypes live there, not in TTT-Core.
- CharacterOptions+ (Arrowsong Minstrel bard, Winter Witch witch, ...).
- DarkCodex: **Elemental Ascetic and Elemental Scion (KINETICIST)**,
  Virtuous Bravo (paladin).
- Kineticist Elements Expanded (Nexus 344): Aether/Void/Wood (abandoned).
- PrestigePlus, Mesmerist (class), Swashbuckler (class), Psionics,
  Mythic Arcanist, Isekai Mod, and more — the database lists ~60.

## What THIS mod ships (do not re-add)

- Alchemist: Construct Crafter (0.5.4+; command menus 0.16.0)
- Hunter: Venomblood (0.12.0), Riftstalker (homebrew, 0.13.0–0.15.0),
  Verminous (tabletop port, 0.15.0)
- Inquisitor: Sin Eater (tabletop port, 0.16.0), Doomsayer (homebrew, 0.17.0)
- Kineticist: Explosion infusion (0.18.0), Unstable Kineticist (homebrew,
  0.19.0), Kinetic Chirurgeon (tabletop port, 0.20.0)
- Plus earlier classes — the authoritative per-class list is
  docs/ARCHETYPES.md; this file is the collision-check index.

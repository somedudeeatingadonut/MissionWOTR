# MissionWOTR

An original-feats mod for **Pathfinder: Wrath of the Righteous**. Adds new feats designed to be
mechanically distinct from anything in the base game or the major content mods
(TabletopTweaks, ExpandedContent, CharacterOptions+, ...).

> **Status: scaffolded, pre-first-build.** The code is complete for feat batch 1; the first
> successful compile happens once the game reference DLLs are uploaded —
> see [`docs/GAME-FILES-NEEDED.md`](docs/GAME-FILES-NEEDED.md).

## Feats (batch 1)

| Feat | Prerequisites | Effect |
|---|---|---|
| **Vengeful Counterstrike** | Dex 13, Combat Reflexes | When a melee attack misses you and the attacker is within reach, make an attack of opportunity against them |
| **Arcane Momentum** | Caster level 3 | Cast a spell from one of your two highest spell levels → scaling dodge AC (+1/+2/+3 by mightiest castable level) for 1 round |
| **Battlefield Scavenger** | Dex 13, Point-Blank Shot | Kill with a ranged weapon → your next ranged attack within 1 round gets +2 to hit |
| **Second Wind** | Endurance | Once per day (restored on rest), dropping to ≤ half HP grants temp HP (character level, min 2) and +2 Fort for 1 minute |
| **Taunting Blows** | Str 13, Power Attack | Melee hits while Power Attack is active → Will save or −2 attack for 1 round (no reapply while active) |
| **Resonant Strikes** | Wis 13, Improved Unarmed Strike | Confirmed unarmed crits → Fortitude save or −2 AC for 1 round (no reapply while active) |
| **Warded Soul** | Iron Will | +1 luck bonus to Fortitude, Reflex, and Will |

Batch 2+ designs (mounted, aura, hex-line, reaction feats, ...) live in
[`docs/FEAT-ROADMAP.md`](docs/FEAT-ROADMAP.md).

## Feats (batch 2)

| Feat | Prerequisites | Effect |
|------|---------------|--------|
| **Steadfast Aim** | Dex 13, Point-Blank Shot | +1 on ranged attacks vs. enemies engaged in melee with an ally |
| **Guarded Momentum** | Combat Expertise | Each melee miss against you: +1 dodge AC until your next turn (stacks to +3) |
| **Tunnel Fighter** | Combat Reflexes | Toggle stance: unlimited attacks of opportunity, but −2 on attack rolls |

## Mythic feats & abilities (batch 1)

Mythic **feats** (tabletop ports from *Mythic Adventures*, adapted to Wrath's merged skill
list, chosen to not overlap with the base game or other mods):

| Mythic feat | Prerequisites | Benefit |
|-------------|---------------|---------|
| **Acrobatic (Mythic)** | - | +4 Mobility, +4 Athletics |
| **Persuasive (Mythic)** | Persuasive | +4 Persuasion, +4 Perception |
| **Magical Aptitude (Mythic)** | - | +4 Knowledge (Arcana), +4 Use Magic Device |
| **Iron Will (Mythic)** | Iron Will | +1 Will per 2 mythic ranks (min +2) |
| **Lightning Reflexes (Mythic)** | Lightning Reflexes | +1 Reflex per 2 mythic ranks (min +2) |
| **Endurance (Mythic)** | Endurance | +1 Fortitude per 2 mythic ranks (min +2) |

Original mythic **abilities**:

| Mythic ability | Benefit |
|----------------|---------|
| **Untouchable** | +1 dodge AC per 2 mythic ranks (min +1) |
| **Slayer's Vigor** | Weapon kill → temp HP = mythic rank (min 2) for 1 min |
| **Ascendant Edge** | Cast a non-cantrip spell → +½ mythic rank (min +1) insight to attacks for 1 round |
| **Last Stand** | At ≤ half HP: +1 dodge AC per 2 mythic ranks (min +1) |
| **Desperate Fury** | At ≤ half HP: +1 insight to attacks per 2 mythic ranks (min +1) |
| **Defiant Soul** | Succeed on a save vs. another's effect → +½ mythic rank (min +2) morale to attacks for 1 round |
| **Relentless Onslaught** | Weapon kill → +½ mythic rank (min +2) insight to attacks for 1 round |
| **Aetherial Bulwark** | Cast a non-cantrip spell → +½ mythic rank (min +1) deflection AC for 1 round |
| **Titan's Wrath** | +1 mythic damage on weapon attacks per mythic rank |
| **Titan Hide** | +1 natural armor AC per 2 mythic ranks (min +1) |

All of them are selectable through the normal mythic feat/ability level-up picks (any mythic
path), and Mission Vanguard grants them at level 1 for testing.

## Class archetypes

Two archetypes per class is the long-term goal (one tabletop port, one homebrew) — plans and
real level tables live in [`docs/ARCHETYPES.md`](docs/ARCHETYPES.md). All archetypes ship in
**test mode**: every feature is granted at level 1; flip `LevelPlan.AllAtLevelOne` to false
for normal leveling.

| Class | Archetype | Kind | Summary |
|-------|-----------|------|---------|
| Alchemist | **Eldritch Poisoner** | Tabletop (Black Markets) | Trades bombs/Throw Anything/mutagen for arcanotoxin (supernatural poison that bypasses poison immunity at a +4 save penalty for the immune), toxicology, and rogue sneak attack. Mythic ability: **Expedited Synthesis** — swift-action brew for 25% max HP (15% on a Fort save) |

## Mission Vanguard (test archetype)

**Mission Vanguard** is a Magus archetype that grants all seven Mission WOTR feats - plus the
vanilla feats they normally require (Power Attack, Point-Blank Shot, Improved Unarmed Strike,
Iron Will, Endurance, Combat Reflexes) - at level 1. Features granted through an archetype
bypass prerequisite checks, so it exists to make playtesting the whole mod trivial: roll a
Magus, pick Mission Vanguard, and everything is live from the first fight - including the
mod's mythic feats and abilities. It removes nothing from the base Magus and is planned to
grow into a full class in a future version.

## For players

**Requirements:** [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) 0.21.3 or newer.

1. Install UMM, point it at Pathfinder: Wrath of the Righteous, click **Install**.
2. Download the `MissionWOTR` artifact from the [Actions tab](../../actions) (latest **Build**
   run → scroll down to **Artifacts**), unzip it once to get `MissionWOTR.zip`, or grab it
   from Releases once published.
3. In UMM's **Mods** tab, drag the zip onto **Install MOD** (or drop it into the game's
   `Mods/` folder).

**Save warning:** like all content mods, once you save a game with MissionWOTR enabled, keep it
enabled to load that save.

## For developers / building

The project is a .NET Framework 4.7.2 C# library using
[BlueprintCore](https://github.com/WittleWolfie/WW-Blueprint-Core) (fluent API over the game's
blueprint system) and [Harmony](https://harmony.pardeike.net/) for runtime patches.

- One-time setup (game reference DLLs): [`docs/GAME-FILES-NEEDED.md`](docs/GAME-FILES-NEEDED.md)
- Open `MissionWOTR.sln` in Visual Studio 2022 (or Rider), or build from CLI:

  ```
  dotnet build src/MissionWOTR/MissionWOTR.csproj -c Release
  ```

- Local convenience: define a `WrathPath` environment variable pointing at your game install
  (e.g. `C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Second Adventure`) and the
  build auto-deploys to `<game>\Mods\MissionWOTR\`.
- Every push triggers the GitHub Actions **Build** workflow, which compiles the mod and uploads
  `MissionWOTR.zip` as an artifact.

### Project layout

```
src/MissionWOTR/
├── Main.cs                  UMM entry point + Harmony patches that trigger blueprint setup
├── Guids.cs                 permanent GUID registry (never change released GUIDs!)
├── Info.json                UMM manifest
├── LocalizedStrings.json    feat names & descriptions (enGB)
└── Feats/                   one file per feat; MissionFeats.cs is the bootstrap
```

### How feats work

Wrath represents game mechanics as *blueprints*. Each feat is a `BlueprintFeature` created at
runtime (via BlueprintCore's `FeatureConfigurator`) when the game loads its blueprint cache —
the `BlueprintsCache.Init` Harmony postfix in `Main.cs` calls `MissionFeats.ConfigureAll()`.
Behaviors that the stock components can't express are custom `UnitFactComponentDelegate`
classes that subscribe to rulebook events (attack rolls, damage, casts, AC calculation).

## Credits

- [WittleWolfie](https://github.com/WittleWolfie) — BlueprintCore, the Owlcat modding wiki, and
  the CharacterOptions+ codebase that this project's patterns are based on.
- The WOTR modding community on the [Owlcat Discord](https://discord.gg/owlcat).
- Owlcat Games — for the game and its moddability.

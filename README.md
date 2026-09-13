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
| **Arcane Momentum** | Caster level 3 | Cast one of your highest-level spells → +1 dodge AC until your next turn |
| **Battlefield Scavenger** | Dex 13, Point-Blank Shot | Kill with a ranged weapon → your next ranged attack within 1 round gets +2 to hit |
| **Second Wind** | Endurance | Once per minute, dropping to ≤ half HP grants temp HP (½ level) and +2 Fort for 1 minute |
| **Taunting Blows** | Str 13, Power Attack | Melee hits while Power Attack is active apply −2 attack to the target for 1 round |
| **Resonant Strikes** | Wis 13, Improved Unarmed Strike | Confirmed unarmed crits apply −2 AC to the target for 1 round |
| **Warded Soul** | Iron Will | +1 luck bonus to Fortitude, Reflex, and Will |

Batch 2+ designs (mounted, aura, hex-line, reaction feats, ...) live in
[`docs/FEAT-ROADMAP.md`](docs/FEAT-ROADMAP.md).

## For players

**Requirements:** [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) 0.21.3 or newer.

1. Install UMM, point it at Pathfinder: Wrath of the Righteous, click **Install**.
2. Download the `MissionWOTR.zip` from the [Actions tab](../../actions) (latest **Build** run →
   **MissionWOTR** artifact) or from Releases once published.
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

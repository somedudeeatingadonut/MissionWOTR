# Game files needed to build MissionWOTR

The mod compiles **against the game's own code libraries**, so the build needs a one-time copy
of 11 DLL files from your Pathfinder: Wrath of the Righteous installation. They are too big for
the GitHub web upload limit on individual files, so the plan is to attach them (as a single zip)
to a GitHub **release**, where the CI build picks them up.

> ⚠️ **Important: make this repository PRIVATE first.**
> These DLLs are Owlcat's copyrighted game files. Uploading them to a *public* repo/release
> would be redistributing the game. Go to **Settings → General → Danger Zone → Change
> visibility → Private** before uploading. You can flip it back to public when the mod is
> released (the DLLs can then be removed from the release).

## Step 1 — Find your game install

| Store | Default install folder |
|---|---|
| Steam | `C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Second Adventure` |
| GOG | `C:\GOG Games\Pathfinder Wrath of the Righteous` |
| Epic | `C:\Program Files\Epic Games\PathfinderWrathOfTheRighteous` |

The folder we need is `<install folder>\Wrath_Data\Managed\`.

## Step 2 — Copy these 9 files from `Wrath_Data\Managed\`

| # | File | Notes |
|---|---|---|
| 1 | `Assembly-CSharp.dll` | The main game code (large, ~50 MB) |
| 2 | `Assembly-CSharp-firstpass.dll` | |
| 3 | `Newtonsoft.Json.dll` | |
| 4 | `Owlcat.Runtime.Core.dll` | |
| 5 | `Owlcat.Runtime.UI.dll` | |
| 6 | `Owlcat.Runtime.Validation.dll` | |
| 7 | `Owlcat.Runtime.Visual.dll` | |
| 8 | `UnityEngine.dll` | |
| 9 | `UnityEngine.CoreModule.dll` | |

All 9 must come from the **same game installation and patch version** (don't mix GOG + Steam
copies, etc.).

## Step 3 — Copy these 2 files from `Wrath_Data\Managed\UnityModManager\`

| # | File | Notes |
|---|---|---|
| 10 | `UnityModManager.dll` | The mod loader the mod hooks into |
| 11 | `0Harmony.dll` | Harmony patching library shipped with UMM |

The `UnityModManager` folder only exists **after you install Unity Mod Manager (UMM)**:

1. Download UMM from https://www.nexusmods.com/site/mods/21 (needs a free Nexus account).
2. Unzip it somewhere outside the game folder, run `UnityModManager.exe`.
3. In the dropdown pick **Pathfinder: Wrath of the Righteous**, set the game folder path, click **Install**.
4. The `UnityModManager` folder (containing both DLLs above) now exists in `Wrath_Data\Managed\`.

You'll need UMM installed anyway to *play* the mod — every major WOTR gameplay mod (TabletopTweaks,
ExpandedContent, etc.) uses it.

## Step 4 — Zip and upload

1. Put all 11 DLLs into a folder and zip it → name the zip **`Libs.zip`**.
   - Inside the zip, folder structure doesn't matter (the build sorts files by name), but
     keeping `UnityModManager.dll` and `0Harmony.dll` in a subfolder is fine too.
2. On GitHub, open this repo → **Releases** → find the release titled **"Game libraries (build
   inputs)"** → click the pencil (Edit) → drag `Libs.zip` into **Attach binaries by dropping them
   here** → **Update release**.
3. Go to the **Actions** tab → pick the latest **Build** run → **Run workflow** (or push any
   change) — the next build will compile the mod and produce a `MissionWOTR` artifact containing
   the installable zip.

### Alternative: commit instead (if you use git locally)

```bash
git checkout arena/01a09cf2-missionwotr
# copy the 9 Managed DLLs into Libs/ and the 2 UMM DLLs into Libs/UMM/
git add Libs/*.dll Libs/UMM/*.dll
git commit -m "Add game reference DLLs"
git push
```

(GitHub's limit is 100 MB per file, which these are all under; only the *web UI* has the
25 MB per-file limit.)

## Optional but nice to have

- **`blueprints.zip`** (if present in your game install root folder): a data dump of all game
  blueprints. It makes looking up existing GUIDs/names/icons much easier. If it exists and is
  under ~500 MB, attach it to the same `game-libs` release. Not required for the build.

## Where the files end up

```
Libs/                       ← the 9 game DLLs (either committed or fetched by CI from Libs.zip)
Libs/UMM/                   ← UnityModManager.dll + 0Harmony.dll
Libs/Publicized/            ← generated at build time (never uploaded)
```

The CI workflow (`.github/workflows/build.yml`) checks `Libs/` first, and only downloads the
release asset if the folder is empty — so either upload method works.

## Why these files can't just be downloaded

They're part of the game you own; there's no legal public source for them, and each patch
version differs. That's why a copy from *your* install is needed once. The repo itself only
ever contains the mod's own code plus the generated build output.

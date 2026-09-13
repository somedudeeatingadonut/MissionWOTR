# Libs/

This folder holds the **game reference DLLs** the mod compiles against.

It is currently **empty** — see [`docs/GAME-FILES-NEEDED.md`](../docs/GAME-FILES-NEEDED.md) for
the exact list of 11 files to provide and how to upload them.

Expected layout once populated:

```
Libs/
├── Assembly-CSharp.dll              ← from <game>/Wrath_Data/Managed/
├── Assembly-CSharp-firstpass.dll
├── Newtonsoft.Json.dll
├── Owlcat.Runtime.Core.dll
├── Owlcat.Runtime.UI.dll
├── Owlcat.Runtime.Validation.dll
├── Owlcat.Runtime.Visual.dll
├── UnityEngine.dll
├── UnityEngine.CoreModule.dll
├── UMM/
│   ├── UnityModManager.dll          ← from <game>/Wrath_Data/Managed/UnityModManager/
│   └── 0Harmony.dll
└── Publicized/                      ← generated at build time, do not add manually
```

CI fetches `Libs.zip` from the `game-libs` GitHub release if this folder is empty, so committing
the DLLs here is optional (but works too — note GitHub's web UI caps uploads at 25 MB per file,
which `Assembly-CSharp.dll` exceeds).

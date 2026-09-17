# MissionWOTR class archetypes

Goal: two archetypes for every base class in Wrath of the Righteous - one faithful tabletop
port, one homebrew. **Every archetype ships in TEST MODE**: all of its features are granted
at level 1. To switch the whole mod to normal leveling, set
`LevelPlan.AllAtLevelOne = false` in
[`src/MissionWOTR/Archetypes/LevelPlan.cs`](../src/MissionWOTR/Archetypes/LevelPlan.cs).
Each `LevelPlan.L(n)` call site then uses the real level `n` listed below.

## Alchemist

### Eldritch Poisoner (tabletop port — Pathfinder Player Companion: Black Markets)

| Feature | Real level | Replaces | Notes |
|---|---|---|---|
| Arcanotoxin | 1 | bomb | Dose pool = alch level + Int per day; standard action to brew + coat (1 min); weapon hits deliver Fort save (DC 10 + ½ alch level + Int) or 1d2 Str |
| Toxicologist | 1 | Throw Anything | +2 Lore (Nature) |
| Sneak Attack | 1, 4, 8, 12, 16, 20 | mutagen (+ persistent mutagen) | `RogueSneakAttack` granted once per listed level (+1d6 each) |
| Careful Injection | 4 | the 4th-level discovery | +2 toxin DC when delivered via sneak attack |
| Sickening Toxin | discovery pick | — | Failed save also sickens (nauseated at alch 12) |
| Mind-Altering Toxin | discovery pick (alch 6) | — | Failed save also dazzles (confused at alch 10) |
| Paralytic Toxin | discovery pick (alch 8) | — | Failed save also staggers (paralyzed at alch 15) |
| Lethal Toxin | discovery pick (alch 10) | — | Toxin deals Con damage instead of Str |
| Combine Toxins | discovery pick | — | 1 Str + 1 Dex instead of 1d2 Str (combine+lethal: 1 Con + 1 Dex) |
| Contact Toxin | discovery pick (alch 4) | — | "Throw Contact Toxin" ability: 30 ft, one enemy, DC −2 |
| Toxic Fumes | discovery pick (alch 6) | — | "Throw Toxic Fumes" ability: 10-ft AoE, DC −4 |
| Envenom | discovery pick | — | "Envenom Ally's Weapon" move-action ability (touch, ally/self) |
| Antidote | discovery pick | — | Standard action: spend a dose to apply delay poison (1 min) |
| Apothecary | discovery pick | — | +½ alch level Lore (Nature) |

All discoveries are registered as ordinary alchemist discovery picks (level-gated per the
tabletop) **and** granted at level 1 in test mode.

**Wrath adaptations:**

1. **Poison-immunity bypass (balance lever):** arcanotoxin is supernatural and functions
   against creatures immune to poison, but such creatures receive **+4 on the saving
   throw**. Poison immunity is near-universal among demons; without this bypass the
   archetype would be dead weight in the Worldwound.
2. **V1 toxin frequency:** tabletop is 1/round for 2 rounds with a save each round; v1
   applies the damage once on the failed initial save, with a 2-round reapplication
   lockout. The game's `BuffPoisonStatDamage` component (used by e.g. Wyvern Poison) can
   restore the full recurring frequency once its save-DC sourcing is confirmed in playtests.
4. **Discovery adaptations:** Tailored Toxin deferred (creature types are arbitrary
   blueprints in Wrath, not an enum — needs a type-selection UI feature); Toxic Fumes keeps
   the 2-round duration (already minimal); Antidote's 10th-level neutralize-poison upgrade
   deferred (needs casting the stock ability from code); Careful Injection is a flat +2 DC
   on sneak-attack deliveries instead of per-die forgone sneak damage; Combine Toxins hits
   Str+Dex (lethal: Con+Dex) rather than offering every stat pair; Contact Toxin skips the
   ranged-touch attack roll (save-only).
3. **Mythic ability — Expedited Synthesis** (original, per user design): brew becomes a
   **swift action** at the cost of **25% max HP, reduced to 15% on a successful DC 15
   Fortitude save** (never below 1 HP). Standard mythic ability pick.

### Alchemist homebrew archetype — Construct Crafter (working name, user concept)

Archetype revolves around building construct summons (NOT animal companions):
uncontrollable, up to three active at once, deliberately less customizable than a companion.

Flow: deploy a **base** (dog base at alch 1, humanoid at alch 7, golem/construct at alch 16),
then install a **core** and a **program** into it — the combination determines what the
construct becomes. A construct lasts until destroyed or until the same base type is
deployed again (replacing the old one).

**Feasibility (verified against Assembly-CSharp.dll + BPCore):**

- Summoning: the game's own `SummonUnitCopy` action (CopyFrom/Locator/OnSummon fields) +
  `UnitConfigurator.CopyFrom` for new construct units; `UnitPartSummonedMonster` links
  summon → summoner. Spawned units have no built-in duration → "lasts until it dies" is
  the default behavior.
- Uncontrollable: AI-driven behavior is the default for spawned units — no player control.
- Multiple/replacement: three different bases = three blueprints, no conflict;
  "replace same-base" is a small custom spawn action (find owner's prior summon of that
  base, remove it, spawn the new one).
- Textures: model *selection* is blueprint-level (`BlueprintUnit.Prefab` — dog, humanoid,
  stone/iron/brass/gold/adamantine golem prefabs all exist). TRUE custom
  textures/reskins require shipping a Unity asset bundle (needs Unity Editor + game art
  assets — outside this repo's code-only pipeline; can be wired to load a user-provided
  bundle later). V1: existing model variants.
- Core/program implementation shape (recommended): core & program chosen pre-deployment
  and applied as blueprint buffs via the spawn action's OnSummon list — core = stat/role
  package (scales with alchemist level via context ranks), program = behavior package.
  The dormant-base-then-activate flow is also possible but needs runtime state juggling
  (riskier).

**Open design questions:** core list, program list, base stat blocks, deploy action cost
(standard? resource?), whether cores/programs are swappable per rest or fixed choices.

## Level plan per class (test-mode rule)

All archetype features land at level 1 while `LevelPlan.AllAtLevelOne == true`. The table
above doubles as the source of truth for real levels; the same pattern (documented table +
`LevelPlan.L(n)` call sites) applies to every future archetype.

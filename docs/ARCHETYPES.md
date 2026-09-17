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

### Alchemist homebrew archetype

**Planned** — slot free.

## Level plan per class (test-mode rule)

All archetype features land at level 1 while `LevelPlan.AllAtLevelOne == true`. The table
above doubles as the source of truth for real levels; the same pattern (documented table +
`LevelPlan.L(n)` call sites) applies to every future archetype.

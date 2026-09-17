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
| *Careful Injection* | 4 | the 4th-level discovery | **Not yet implemented** |
| *Arcanotoxin discoveries* (Sickening, Mind-Altering, Paralytic, Lethal, Combine, Contact, Envenom, Tailored, Toxic Fumes, Antidote, Apothecary) | per tabletop | — | **Not yet implemented** — planned as a follow-up batch |

**Wrath adaptations:**

1. **Poison-immunity bypass (balance lever):** arcanotoxin is supernatural and functions
   against creatures immune to poison, but such creatures receive **+4 on the saving
   throw**. Poison immunity is near-universal among demons; without this bypass the
   archetype would be dead weight in the Worldwound.
2. **V1 toxin frequency:** tabletop is 1/round for 2 rounds with a save each round; v1
   applies 1d2 Str once on the failed initial save, with a 2-round reapplication lockout.
   The game's `BuffPoisonStatDamage` component (used by e.g. Wyvern Poison) can restore
   the full recurring frequency once its save-DC sourcing is confirmed in playtests.
3. **Mythic ability — Expedited Synthesis** (original, per user design): brew becomes a
   **swift action** at the cost of **25% max HP, reduced to 15% on a successful DC 15
   Fortitude save** (never below 1 HP). Standard mythic ability pick.

### Alchemist homebrew archetype

**Planned** — slot free.

## Level plan per class (test-mode rule)

All archetype features land at level 1 while `LevelPlan.AllAtLevelOne == true`. The table
above doubles as the source of truth for real levels; the same pattern (documented table +
`LevelPlan.L(n)` call sites) applies to every future archetype.

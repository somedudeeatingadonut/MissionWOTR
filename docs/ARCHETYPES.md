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

### Alchemist homebrew archetype — Construct Crafter (user design) — IMPLEMENTED (v1)

Construct summons, NOT animal companions: uncontrollable (AI-driven), up to three active
(one per base), less customizable than a companion. Deploy a base as a standard action;
the construct lasts until destroyed or until the same base is deployed again.

| Feature | Real level | Details |
|---|---|---|
| Deploy Clockwork Hound | 1 | Dog chassis slightly worse than a normal dog (−2 Str, −2 Dex, −4 HP); DR = ½ alchemist level, bypassed by adamantine (dynamic buff applied at deploy) |
| Extra combat feat | 1 | FighterFeatSelection (fighter bonus-feat list) |
| Basic Core + Basic Program selections | 1 | Core selection v1: Basic only; Program selection: Basic + six programs |
| Proficiencies | 1 | Light armor, longbow, shortbow, throwing axe, flail, heavy flail, warhammer, greatclub + vanilla simple weapons |
| Dampened Synthesis | 2–18 (one step per 2 levels) | Extract caster level reduced by 1 per step (≈ half level at cap) — the "not many spellcasting levels" balance lever |
| New program pick | 4/8/12/16/20 | Program selection: Passive, Aggressive, Flank, Guard, Distance, Chaos |

**Cores** (toggle on the crafter; applied at deploy; one new core pick at alchemist
levels 3/8/13/19 — 12 cores, 4 picks, purposefully limited):

| Core | Effect (AL/2 = +1 per 2 alchemist levels, min 1) |
|---|---|
| Overdrive | +AL/2 attack & damage, +10 speed, 2 dmg/round self-burn, cannot be healed; blocked with Chaos |
| Hardened | +AL/2 dodge AC, +AL/2 saves, DR 2/−, −AL/2 damage |
| Flaming (per-base) | Hound: +speed/+dmg/+saves/SA, −AC, 1 dmg/round · Humanoid: +2d6 fire on hit, burning on hit · Golem: fire aura + burning on hit, no AoOs (Guard restores them), 2 dmg/round |
| Cold (per-base) | Hound: prone + slow on hit, +saves, −speed · Humanoid: +20 speed, +dmg (ice-charge) · Golem: +1d6 cold on hit, slow on hit |
| Bloody | Bleed + shaken on hit, fast healing 2 |
| Soft | +AL/2 AC/saves, fast healing 1, −AL/2 damage (sponge/decoy) |
| Infernal | +2d6 fire on hit, shaken on hit, +10 speed, +init |
| Lightless (per-base) | Hound/Humanoid: SA 1/3 AL + attack/AC · Golem: shaken + frightened on hit, +Will |
| Booming | +AL damage, fire aura, −AL/2 AC, −2 saves (glass cannon) |
| Quick | Extra attack (haste-style), +10 speed, +init, −AL/2 damage |
| Galvanized *(original)* | +1d6 electricity on hit, +init/+Reflex, +10 speed |
| Magnetized *(original)* | +AL/2 CMB & CMD, slow on hit |

Core adaptations (v1): on-hit riders and self-burn use fixed values (not AL-scaled) —
`AdditionalDamageOnHit` takes static dice and the DoT is a flat component; casting-flavored
cores (fire mage, ice rays, necromancy) are delivered as on-hit riders + auras because
constructs can't cast without custom brains (next iteration); Infernal's teleport-attacks
are approximated with speed/init until brain support; Cold humanoid's charge immunity to
AoOs has no native hook (documented); the Flaming-golem/Guard interaction restores AoOs but
does not yet halve the aura damage; Soft's ally-support auras are approximated by a
tanky self-healing chassis. Auras use the stock `FireDamageAreaEffect`.

**Programs** (toggle on the crafter; applied to constructs at deploy; stat changes scale
+1 per 2 AL, min 1):

| Program | Effects |
|---|---|
| Passive | −attack, −initiative, +dodge AC, +all saves |
| Aggressive | +attack, +initiative, −AC, flat −2 saves |
| Flank | Sneak attack 1d6 per 2 AL (applies to attack rolls incl. rays — works with spells), −BAB |
| Guard | +dodge AC, +AoOs, DR 2/−, −damage (×2 the rate) |
| Distance | +BAB, −AC, flat −2 attack, flat −2 saves |
| Chaos | +attack, +AC, +initiative, +all saves; same-base redeploy blocked while the chaos construct lives |
| Deploy Humanoid Construct | 7 | Fighter with (alchemist level − 2) fake fighter levels, simple equipment chassis |
| Deploy Clay Golem | 16 | Tabletop clay golem: no berserk, −20 HP (87), Str 30, DR 5/adamantine |

Chassis: keeps alchemist BAB/HD; removes mutagen (L1), poison resistance (L2/L5), poison
immunity (L10, best-effort). Cores/programs are chosen before deployment (selections).

**Implementation (all engine calls verified):** spawn via
`Game.Instance.EntityCreator.SpawnUnit(bp, pos, rot, Game.Instance.State.LoadedAreaState.MainState)`
(the same call ToyBox's unit browser uses); replacement = scan the area state's
`AllEntityData` for a living unit with the same base blueprint and despawn it
(`IsInGame = false`); fighter levels via `Progression.AddFakeClassLevels` (chassis without
feat-selection fuss); scaling DR via a buff with `AddDamageResistancePhysical` +
`ContextRankConfig(ClassLevel(alchemist)/2)`; faction = the dog companion's player-friendly
faction on all three blueprints.

**Deferred from this batch (user-requested, engine-limited):**

1. **Bomb damage halving** — bomb damage is computed inside the ability's shared-value
   calculation; the damage bundle's flat bonus is not settable from a fact component
   (verified: DamageValue exposes no writable bonus). Needs a targeted Harmony patch or a
   bomb ability override — revisit with user.
2. **Flank's caster-level reduction** — AddCasterLevelForSpellbook takes a flat int, not a
   scaling value; Flank's −CL will ship with the spellcasting cores (next batch).
3. **Program AI behaviors** (stay-at-range, support, protect) — require custom brain
   blueprints; v1 delivers the stat packages, sneak attack, and the chaos restriction.
   Wrath has no "spell failure from being attacked" mechanic (Guard).

**v1 adaptations / known gaps (playtest watch-list):**

1. **Extracts cannot be removed** — spellcasting is `m_Spellbook` on the shared
   `BlueprintCharacterClass`; an archetype cannot remove it without modifying every
   alchemist. Options: leave as-is (current), or promote this to a full custom class later.
2. **Bombs are NOT removed** (not in the user's removal list) — halving their damage is
   deferred (see above).
3. **Clay golem chassis** is built on the stone golem's body (no clay golem exists in
   Wrath's unit list); slow-breath components are stripped by name where possible — if the
   golem occasionally slows a target, that's the leftover (report it).
4. **Unit names** show the source creature's name (SharedStringAsset authoring needs an
   asset bundle — same territory as custom textures).
5. Deploy has **no resource cost** yet (standard action only) — cost TBD with user.
6. Same-base replacement matches by blueprint (any crafter's construct); single-crafter
   parties are unaffected.
7. Poison-feature removal levels are best-effort (L2/L5/L10 per tabletop); verify in the
   character sheet that no poison rows remain.

**Open design questions:** deploy resource cost, future core list, future program list,
whether constructs persist through area transitions (they are scene-local today).

## Level plan per class (test-mode rule)

All archetype features land at level 1 while `LevelPlan.AllAtLevelOne == true`. The table
above doubles as the source of truth for real levels; the same pattern (documented table +
`LevelPlan.L(n)` call sites) applies to every future archetype.

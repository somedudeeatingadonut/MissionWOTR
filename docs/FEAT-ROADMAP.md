# MissionWOTR feat design roadmap

## Design principles

1. **Novelty first** — every feat must be something you cannot get from the base game or the
   major content mods. Before a feat is designed, check it against:
   - Base game (all CRB/APG feats + Owlcat additions)
   - [TabletopTweaks-Base](https://github.com/Vek17/TabletopTweaks) — tabletop-accurate fixes &
     additions (alternate capstones, advanced weapon/armor training, metamagic feats
     like Intensified/Rime/Flaring, Dervish Dance, Animal Ally, Dispel Focus, ...)
   - [ExpandedContent](https://github.com/ka-dyn/ExpandedContent) — Swift Aid, Opening Volley,
     Bullseye Shot, Pinpoint Targeting, Demon Hunter, Noble Scion, + domains/mysteries/deities
   - [CharacterOptionsPlus](https://github.com/WittleWolfie/CharacterOptionsPlus) — Furious
     Focus, Hurtful, Skald's Vigor, Dazing Assault, Eldritch Heritage, Glorious Heat, ...
   - Smaller mods (Come and Get Me!, PrestigePlus, Nine Swords, Madison's Metagaming, ...)
2. **Wrath-flavored** — feats should feel like they belong in a crusade against the Worldwound.
3. **Implementable** — mechanics that map cleanly onto the game's rulebook event system
   (attacks, damage, casts, saves, AC calculations, AoOs).
4. **Save-safe** — every blueprint has a permanent GUID (`Guids.cs`); custom components carry
   stable `[TypeId]`s.

## Batch 1 — implemented (awaiting first CI build)

| Feat | Prerequisites | Effect | Implementation notes |
|---|---|---|---|
| **Vengeful Counterstrike** | Dex 13, Combat Reflexes | When a melee attack misses you and the attacker is within reach, make an AoO against them (consumes a normal AoO per round) | `RuleAttackWithWeapon` (target) → `ForceAttackOfOpportunity` |
| **Arcane Momentum** | Caster level 3 | Cast a spell from one of your two highest spell levels → scaling dodge AC (+1/+2/+3 by mightiest castable level) until your next turn | `RuleCastSpell` (initiator) → 1-round buff w/ custom `RuleCalculateAC` component |
| **Battlefield Scavenger** | Dex 13, Point-Blank Shot | Kill with a ranged weapon → your next ranged attack within 1 round gets +2 to hit | `RuleDealDamage` (initiator) → 1-round buff with ranged-only attack bonus |
| **Second Wind** | Endurance | Once per day (restored on rest), when a hit drops you to ≤ half max HP → temp HP (character level, min 2) + +2 morale Fort saves for 1 min | `RuleDealDamage` (target) → `BlueprintAbilityResource` gate + buff with `TemporaryHitPointsFromAbilityValue` |
| **Taunting Blows** | Str 13, Power Attack | Melee hit while Power Attack is active → Will save (DC 10 + ½ lvl + Str) or −2 attack for 1 round; no reapply while active | `RuleAttackWithWeapon` (initiator) → `RuleSavingThrow` → debuff |
| **Resonant Strikes** | Wis 13, Improved Unarmed Strike | Confirmed unarmed crit → Fortitude save (DC 10 + ½ lvl + Wis) or −2 AC for 1 round; no reapply while active | `RuleAttackWithWeapon` (initiator) → `RuleSavingThrow` → debuff w/ custom `RuleCalculateAC` component |
| **Warded Soul** | Iron Will | +1 luck bonus to Fort/Reflex/Will (Kenabres wardstone flavor) | Pure stat components |

### Balance revision 1 (post-playtest feedback)

- **Second Wind**: was once-per-minute auto-proc at ½ level temp HP — too much passive sustain. Now **once per day** (rest resource) but grants **full character level** temp HP.
- **Taunting Blows**: was a no-save −2 attack on every Power Attack hit — near-permanent uptime on bosses. Now **Will save negates** (mind-affecting flavor) and cannot be re-applied while active.
- **Resonant Strikes**: was a guaranteed −2 AC on every unarmed crit. Now **Fortitude save negates** and cannot be re-applied while active.
- **Arcane Momentum**: was a flat +1 dodge AC — too weak for a feat slot. Now **scales to +3** with the mightiest spell level you can cast and triggers on your **two highest** spell levels instead of only the top one.

## Batch 2 — partially implemented

**Implemented:**

| Feat | Prerequisites | Effect |
|---|---|---|
| **Steadfast Aim** | Dex 13, Point-Blank Shot | +1 to hit with ranged attacks against enemies engaged in melee with one of your allies |
| **Guarded Momentum** | Combat Expertise | While fighting defensively, each melee attack that misses you grants +1 dodge AC until your next turn (stacks to +3, resets each turn) |
| **Momentum Breaker** | Str 13, Power Attack | After you move 10+ ft and then attack in the same turn, your first melee hit that turn deals +50% Str damage (precision bonus vs. demons?) — exact math TBD |
| **Bulwark of Faith** | Divine caster level 1 | You and adjacent allies gain +1 sacred bonus on saves vs. spells and spell-like abilities; +2 vs. fear at BAB +6 |
| **Zealous Charge** | Mounted Combat, ride a mount | While mounted and charging, you and your mount gain +1 dodge AC; your charge deals +1 damage die of the weapon's type |
| **Warcries** | Perform or Persuasion 5 | When combat starts, allies within 30 ft who can hear you gain +1 morale on initiative and +2 vs. fear for 1 minute (once per battle) |
| **Crushing Sweep** | Combat Reflexes, trip-capable weapon | When you trip a foe, you may immediately make a single melee attack at −2 against a different adjacent enemy that threatens them |
| **Alchemist's Retort** | Throw Anything | When an enemy scores a critical hit against you, you automatically splash-throw your current bomb formula at them (uses a bomb charge, once per round) |
| **Witchfire Hexes** (line) | ability to cast 1st-level spells | A mini "hex" line: 1/long-rest debuffs (evil eye −2 AC 1 round; misfortune reroll once; slumber-ish sleep on <4 HD enemies). Fills the hex gap WOTR never shipped for non-witches |
| **Tunnel Fighter stance** | Combat Reflexes | Toggleable: while active you may make any number of AoOs per round but take −2 on all attacks |

**Implementation notes (batch 2):**

- **Guarded Momentum**: adapted — Wrath exposes no "fighting defensively" state to mod code
  (verified against Assembly-CSharp.dll), so the feat triggers on any melee miss with Combat
  Expertise as the prerequisite instead of requiring the FD stance.
- **Tunnel Fighter**: implemented as an activatable stance (toggle in the ability bar) whose
  buff grants +50 attacks of opportunity and −2 attack rolls.

**Deferred (need riskier APIs — combat-start hooks, mount/charge state, bomb formulas,
aura blueprints, or performing attacks from code):** Momentum Breaker, Bulwark of Faith,
Zealous Charge, Warcries, Crushing Sweep, Alchemist's Retort, Witchfire Hexes.

## Batch 3 — ideas parking lot

- **Abyssal Scavenger** — loot/ranged hybrid: killing a demon grants a stacking +1 damage vs.
  demons for the battle (max +5), resets on rest. (Distinct from Favored Enemy / Demon Hunter:
  earned stacks, any class.)
- **Martyr's Interdiction** — when an adjacent ally is targeted by an attack, spend an AoO to
  become the target instead (once per round).
- **Cold Iron Purge** — your weapons count as cold iron for overcoming DR; +2 damage vs.
  demons with cold iron DR at BAB +8. (Checks: base game has no cold-alignment transmutation
  for weapons.)
- **Lesson of the Fifth Crusade** — Persuasion-based: demoralize via Intimidate as a swift
  action once per round when you drop an enemy below half HP.
- **Second Chance** — once per day, when you fail a saving throw, reroll it and take the
  better result.
- **Pin Them Down** — when an enemy within 30 ft stands up from prone, you may make an AoO
  with a ranged weapon (uses an AoO charge).

## Overlap notes (what the big mods already add, so we don't)

- **TabletopTweaks-Base**: alternate capstones, fighter AWT/AAT, many metamagic feats
  (Intensified, Rime, Flaring, Piercing, Burning, Encouraging, Solid Shadows, Elemental),
  Dervish Dance, Animal Ally, Celestial Servant, Dispel Focus, Erastil's Blessing, Ability
  Focus (Stunning Fist), rogue talents, alternate racial traits, several archetypes.
- **ExpandedContent**: Swift Aid, Opening Volley, Bullseye Shot, Pinpoint Targeting, Demon
  Hunter, Noble Scion, Aid Another rework, deities/domains/mysteries, drake/plant companions.
- **CharacterOptionsPlus**: Furious Focus, Hurtful, Skald's Vigor, Dazing Assault, Eldritch
  Heritage, Energy Channel, Glorious Heat, Paired Opportunists, Purifying Channel, Signature
  Skill, Divine Fighting Techniques.
- Our niche: **original, mechanically distinct feats** (reaction/trigger designs, resource
  conversions, stacking battle states) that none of the above ship.

# MissionWOTR class archetypes

Goal: two archetypes for every base class in Wrath of the Righteous - one faithful tabletop
port, one homebrew. **Every archetype ships in TEST MODE**: all of its features are granted
at level 1. To switch the whole mod to normal leveling, set
`LevelPlan.AllAtLevelOne = false` in
[`src/MissionWOTR/Archetypes/LevelPlan.cs`](../src/MissionWOTR/Archetypes/LevelPlan.cs).
Each `LevelPlan.L(n)` call site then uses the real level `n` listed below.

## 0.5.3 — first playtest patch (from in-game logs)

Fixes found in the first full in-game pass:

1. **Archetype trades were silently skipped for everyone.** The
   `ArchetypeRemovals` helper compared caller-supplied guids against
   `SimpleBlueprint.AssetGuid` — which is a `BlueprintGuid` struct, not a
   string, so the `as string` cast always produced null and EVERY removal was
   skipped ("things the class should have taken away are still there"). The
   helper now stringifies and dash-normalizes both sides and passes the resolved
   feature blueprint to `AddToRemoveFeatures` (the old code passed feature
   NAMES into BPCore's guid parser, which is what crashed Spellblade).
2. **No arcanist archetypes appeared.** CovertMage: the cast ability and its
   granting feature shared one asset name with two GUIDs — BlueprintCore threw
   "Duplicate GuidByName" and the whole archetype died (renamed the feature).
   ElementalObsessor: `.Cast<Blueprint<…>>()` over feature arrays throws at
   runtime (implicit conversions are not casts) — replaced with per-element
   conversion.
3. **Spellfist's spellbook (and the Obsessor's five elemental lists) built with
   0 spells.** The shared blueprint enumerator probed the cache dictionary but
   its values are cache-entry wrappers (`.Blueprint` / `.Offset`), not
   blueprints — nothing ever matched. Rewritten for the real
   `m_LoadedBlueprints` shape (DarkCodex blueprint-loader pattern).
4. **Sanguine Font never appeared**: it was never registered in
   `MissionFeats.ConfigureAll` (the diagnostics entry existed; the configure
   call did not).
5. **Spellfist was still granting the feats the 0.5.1 revision removed** — that
   revision's edit never actually landed in the code (verified against the
   playtest log's grant dump). The real schedule is now in: no Combat
   Reflexes / Combat Expertise / Endurance / Battlefield Scavenger / Steadfast
   Aim; Swiftness at 6th; monk fist dice Level4@8th / Level12@20th.
6. **Spellfist recommends Wis/Dex/Con** (archetype-level
   `OverrideAttributeRecommendations`; it casts with Wisdom).
7. **Mummer Mage**: versatile performance doesn't exist in WOTR, so Arcane
   Imitation's trade was void — it now trades **inspire competence**.
8. **Cook rework: ingredients -> meals.** Each charge now feeds EVERY ally
   within 30 ft a weaker meal (values rebalanced; see the Cook section).
9. Custom `ContextAction`s are now built with `ElementTool.Create` (raw `new`
   elements failed BlueprintCore validation — the "CookServeMeal failed
   validation" spam — and skip required init logic).
10. Literal `\n` removed from the Breaker/Bloodstorm descriptions; homebrew
    archetype descriptions rewritten to read like game text instead of spec
    sheets.


## 0.5.4 — Untouchable Rager

First post-playtest content drop: the faithful-port half of the bloodrager pair
(the Advanced Class Guide Untouchable Rager — no spells, SR 8+level while
bloodraging, permanent + toggleable at 14th). Chosen after checking the loaded
mod list: vanilla WOTR already ships Spelleater, Steelblood, Bloodrider,
Greenrager, Primalist and Crossblooded, and PrestigePlus adds Drunken Brute —
Untouchable Rager was the strongest Paizo pick nobody else covers. This also
completes "two archetypes per class" for the bloodrager (homebrew: Sanguine
Font).

## 0.6.0 — the cavalier pair

First new class since the playtest patch: the game's (DLC-era) cavalier now has
its two archetypes. Verified against the loaded mod list - vanilla WOTR ships
Gendarme, Order of the Paw and friends; nothing in the loadout covers these:

1. **Sister-in-Arms** (faithful port, Adventurer's Guide - the Gray Maiden
   commander): half-strength challenge, BOTH orders (the Lion from vanilla +
   the Dragon ported whole - WOTR never had it), Bodyguard, scaling loyalty
   saves, and lion's call at move/swift action. Full adaptation notes in the
   section below.
2. **The Carousel** (homebrew, user-commissioned): the charge archetype. A
   custom charge engine adapted from pplus's StagCharge (real navmesh movement
   at double speed, the vanilla charge buff, a true charge attack), AoO-free
   charging, a free-action "Wheel About" chain charge, a surefooted mount that
   ignores magical mud, allies that never block the line, and an unlimited
   wheel at the capstone.

## 0.7.0 — the cleric pair

Verified against the loaded mod list - vanilla WOTR already ships Crusader,
Divine Commander, Ecclesitheurge, Herald Caller, Angelfire Apostle and Priest
of Balance; HomebrewArchetypes adds Crusader, Divine Agent, Elder Mythos
Cultist, Evangelist and Undead Lord. Both picks below are free:

1. **Cardinal** (faithful port, Ultimate Intrigue - the church politician):
   light armor and no shields, ONE domain, no spontaneous casting, half base
   attack bonus - in exchange for 6 + Int skill ranks per level and the
   political class-skill package. Full adaptation notes in the section below.
2. **The Solipsist** (homebrew, user-commissioned: the selfish cleric): every
   ally-affecting cleric spell can only be cast on himself, the communal
   versions are denied to him outright, and every blessing he puts on himself
   settles twice - the second copy is an untyped echo buff that stacks with
   the original and cannot be overridden by same-type effects. Channel energy
   is traded away entirely.

## 0.7.1 — Solipsist redesign (user feedback)

The first cut left two holes the user called out: ground-aimed communal buffs
still served the whole party (no real downside late-game, where party buffing
rides almost entirely on communals), and the class had no martial presence.
Both closed:

- **Communals denied**: every point-targeted spell that is helpful on allies
  (Bless Communal, Resist Energy Communal, mass cures, Prayer and the whole
  family) is now UNCASTABLE for the solipsist - a caster restriction
  component, inert for everyone else. The selfishness trade now has teeth at
  every level of play.
- **Martial Devotion**: full (fighter) base attack bonus - the engine offers
  only the three tables, and the step up from the cleric's 3/4 is the full
  progression - plus martial weapon proficiency and one bonus combat feat
  from the fighter's list (the vanilla Crusader bonus-feat pattern).
- **Personal blessings echo too**: divine power, righteous might, frightful
  aspect and other personal-range battle blessings now settle twice as well
  (they were previously untouched - they never targeted allies).
- **Conditional-gated tiers now double correctly**: spells that scale inside
  a Conditional branch are deep-copied gate-and-all, so the echo re-evaluates
  the same conditions at cast time and doubles the correct tier.
- Engine hardening: the echo is appended to the spell's OWN run-action list
  instead of a second AbilityEffectRunAction component, so ordering after
  the originals is guaranteed and no multi-component semantics are assumed.

## 0.8.0 — the druid pair (and a Solipsist tune-up)

Coverage note: the user caught that Blight Druid is already a base-game WOTR
archetype (vanilla also ships Defender of the True World, Drovier, Elemental
Rampager, Feyspeaker and Herald of the Horn); HomebrewArchetypes adds only
Halcyon Druid. Both picks below are free:

1. **Lion Shaman** (faithful port, the totemic shaman): feline wild shape
   EARLY (tiger/smilodon at 4th, greater tiers at 8th/14th) while every other
   wild shape feature arrives two levels LATER; three toggleable lion aspects
   (movement / scent / bite); Augment Summoning as Totemic Summons; Dodge /
   Iron Will / Improved Iron Will / Skill Focus (Acrobatics) bonus feats at
   9th/13th/17th. Trades: woodland stride, resist nature's lure, venom
   immunity (stand-ins for the nonexistent trackless step / a thousand
   faces), delayed wild shape.
2. **True Shape** (homebrew, user-commissioned: the permanent wild shape): a
   Beast Soul toggle that replenishes his active form before it can lapse;
   the vanilla wild shape is REPLACED by an 11-form menagerie (bear, boar,
   dinosaur, elephant, fey, griffon, manticore, spider, tiger/smilodon, wolf,
   wolverine) with 8th/15th-tier upgrades; worn armor and shield keep their
   full AC in beast form. Cost: the spellbook stops at 4th-level spells and
   detect magic is filtered out by name.
3. **Solipsist tune-up**: the full BAB table (too coarse) is replaced by
   flat untyped bonuses — +1 attack at 5th / +2 at 15th, +5 HP at 10th /
   +10 at 20th.

## 0.8.1 — True Shape differentiation (user feedback)

The user called out that True Shape functioned too much like the DLC shifter
class - rightly: it granted the shifter's own form abilities wholesale.
Reworked along two axes (plus a correction: an earlier note referenced Call
of the Wild, which is a Kingmaker mod, not WOTR - the detect-magic filter is
simply a name-match safety net):

- **Fluid shifting**: every menagerie form is now a CLONE of its shifter
  source with the action cost rewritten to FREE. The shifter pays action
  economy to change shape; the true shape druid flows between forms mid-fight
  at no cost - and the shifter class itself is completely untouched (its
  blueprints are never modified).
- **Bestial casting**: Natural Spell is built into the archetype - he casts
  his (reduced, 4th-level-capped) spell list from inside the beast. The
  shifter has no magic at all; a spellcasting smilodon is the identity no
  shifter can copy.
- Identity summary: permanence + free-form fluidity + bestial casting + worn
  armor - a shape worn as a garment, not a shape borrowed for a fight.

## 0.8.2 — Bestial Casting scaled (user design)

Bestial casting alone would fade past the mid-levels (a 4th-level-capped list
runs out of road), so the user commissioned scaling riders:

- **Growing castings**: the cloned spellbook carries a cloned slot table (the
  pplus ConstructRider diminished-slots pattern, inverted) - 1st-4th level
  slot counts gain +1 at every odd class level from 5th, cumulative +8 per
  spell level by 19th-20th.
- **Primal caster level**: while polymorphed, his druid spells cast at
  caster level +1 per 2 druid levels - +10 at 20th, an effective CL of 30,
  beyond any mortal caster - via RuleCalculateAbilityParams
  AddBonusCasterLevel (the darkcodex AddCasterLevelParametrized pattern),
  gated on the spell coming from his own druid spellbook.

## 0.9.0 — the fighter trio

Three archetypes this round (user request: two tabletop ports - one of them
his first-ever TTRPG archetype - plus one deliberately simple homebrew).
Coverage: vanilla WOTR ships Aldori Defender, Armor Master, Mutation
Warrior, Trench Fighter, Two-Handed Fighter, Tower Shield Specialist and
Weapon Master; HomebrewArchetypes adds Berserker, Blackguard, Bruiser,
CavernSniper, Deathbringer and Viking. All three picks are free:

1. **Polearm Master** (faithful port, APG - the user's first TTRPG
   archetype): trades bravery, ALL armor training, ALL weapon training and
   armor mastery for pure polearm craft - close-quarters pole fighting,
   scaling AoO bonuses (the vanilla AttackOfOpportunityAttackBonus
   component), scaling polearm attack/damage, flanking wherever an ally
   threatens the target, Improved Trip + Improved Bull Rush, footwork AC,
   and a 19th-level parry guard that shields and blunts damage for nearby
   allies.
2. **Strategic Soldier** (faithful port, Orphaned Bookworm Productions'
   "Breath of Life: The Marshal"): the teamwork fighter - flanks from any
   position, sidesteps, takes hits meant for allies (the vanilla Bodyguard
   + In Harm's Way engine), guards adjacent allies, a reckless strike
   toggle (+5 attack/damage for -5 AC), a full-round knock-off-kilter that
   turns the target into a piñata for threatening allies, and a
   once-per-round punishing AoO when an enemy wounds an ally. Trades the
   1st-level bonus feat, all bravery, all armor training and armor mastery.
3. **Exoticist** (homebrew, user design - "really simple"): proficient with
   every exotic weapon in the game, and his weapon training is replaced by
   scaling attack/damage with exotic weapons only (+1 at 5th, +4 at 17th).

## 0.9.1 — exotic list corrected + Polearm Master fidelity pass (user feedback)

Two corrections from the user, who knows the game better than the compiler
errors suggested:

- **The two-bladed sword and gnome hooked hammer DO exist** - their
  WeaponCategory enum names are just DoubleSword / HookedHammer (and the
  urgrosh is Urgrosh, the double axe DoubleAxe). Verified against a
  compiling WOTR mod's exotic list, the Exotic Arsenal now covers all 21
  exotic categories in the game - including the previously missed elven
  curved blade and tongi. (Katanas and whips genuinely do not exist in
  WOTR; they stayed in Kingmaker.)
- **Polearm Master is closer to the tabletop**:
  - Steadfast Pike now applies only to attacks of opportunity MADE WITH A
    SPEAR OR POLEARM (AoO detection via the rule's Reason chain - the COP
    PairedOpportunists idiom), not to every AoO with any weapon.
  - Sweeping Fend no longer grants Improved Trip / Improved Bull Rush as
    free feats: it adds +2 to trip and bull rush maneuvers made while
    wielding a polearm (the tabletop's trip-feature benefit without the
    free feats).
  - Polearm Parry is no longer always-on: it is a SWIFT ACTION that braces
    the weapon for 1 round (the closest the engine has to the tabletop's
    immediate action), during which the +2 shield AC / DR 5 guard holds.
  - Remaining engine limits, documented: pole fighting cannot shorten a
    reach weapon's grip (see the 0.10.0 note below).

## 0.10.0 — Step Aside goes turn-based, and the Qinggong Monk

- **Step Aside is now a real 5-foot step - in turn-based mode.** The user
  corrected the record: five-foot steps DO exist in WOTR, in turn-based
  combat only. When an adjacent enemy's melee attack against the polearm
  master resolves, he steps 5 feet away (once per round, via the shared
  forced-path mover and TurnController.MetersOfFiveFootStep). In
  real-time-with-pause the feature remains the constant +2 dodge AC - the
  step does not work in that mode.
- **Pole Fighting's "temporarily remove reach" was investigated and ruled
  out** (for now): reach is baked into weapon-type blueprints; the only
  per-unit reach modifier in the engine, ReachMultiplicator (the vanilla
  Mighty Charge threat buff), has unverifiable int-multiplier semantics and
  no mod in the reference set dares touch it. Without the game's internals
  to test against, flipping it blind is crash risk - so the adjacent-target
  bonus that mirrors the tabletop penalty's math stands.
- **Qinggong Monk** (faithful port, APG - THE ki-power archetype): WOTR's
  base monk already carries Owlcat's selectable ki powers
  - MonkKiPowerSelection). The qinggong trades her fixed gifts - still mind,
  fast movement, purity of body - for three EXTRA ki power picks at
  3rd/4th/5th, and the shared selection is extended with four NEW ki powers
  built Owlcat's way (spell clones that spend ki): ki invisibility (2 ki,
  8th), ki neutralize poison (2 ki, 8th), ki freedom of movement (2 ki,
  10th), and ki holy aura (4 ki, 16th). Every monk in the game gains the
  new powers; the qinggong reaches them first.


## 0.10.1 — three more ki powers with real teeth (user feedback)

- The user's verdict on the first four: neutralize poison and freedom of
  movement are not the best ki powers - pick others from tabletop that can
  be more useful (the first four stay). Added to the shared
  MonkKiPowerSelection, all tabletop qinggong picks:
  - **Ki Glimpse of Truth** (2 ki, 10th): true seeing - WOTR is thick with
    mirror images, displacement and invisibility, and this puts true sight
    on a non-caster. Cloned from the true seeing spell.
  - **Ki Dragon Breath** (2 ki, 8th): cloned from the game's own dragon's
    breath spell (fire variant - blueprint copies do not carry the cast-time
    element selection, so the ki version breathes the classic flame).
  - **Ki Frightful Aspect** (3 ki, 16th): cloned from the frightful aspect
    spell. The tabletop gates it at 20th; WOTR's own ki cadence tops out at
    the 16th tier (quivering palm), so it is gated at 16th to stay
    selectable.
- **Why not the rest of the tabletop list:** most remaining qinggong
  spells have no WOTR blueprint to build from (checked against the game's
  ability reference set): gaseous form, deadly juggernaut, ki leech,
  discordant blast, shadow walk, dust form, stone shield, sonic thrust,
  strangling hair, earthquake and the occult psychic picks are all absent
  from the game. Ki powers here are clones of real spells, per Owlcat's own
  pattern - no spell, no ki power.
- Tooltips: the ki-power abilities now also carry their own display name,
  description and icon (previously only the selection feature did), so the
  action bar reads "Ki Invisibility" and friends instead of the raw
  blueprint name.

## 0.11.0 — Hammerfist Monk (homebrew: the one big hit) + a load-fix

- The user's brief: a homebrew monk focused on ONE big damage hit with the
  fists instead of the flurry machine - slightly lower overall output (it
  is easier to land one full attack than a whole flurry), quite a bit of
  the monk gimmickry stripped out, plus a differentiator of our own
  devising.
- **Crushing Fist** (1st, replaces Flurry of Blows + Stunning Fist and its
  upgrade line): once per round, the first unarmed strike that CONNECTS
  deals +2 +1½×monk level bonus damage (+32 at 20th). Tuning: a
  full-attack round stays slightly below the flurry monk's total (only the
  first connecting blow is boosted), while a move-and-punch round carries
  most of a flurry's weight - consistency instead of volume.
- **Rolling Thunder** (9th, replaces Improved Evasion - the
  differentiator): the first unarmed hit of each attack sequence slams the
  target with a free trip maneuver (CMB vs CMD, no action, no AoO). Built
  from the vanilla AddInitiatorAttackWithWeaponTrigger - the same wiring
  as the Two-Handed Fighter's Piledriver - with a knockdown chained on
  success. Scales with the maneuver training the monk keeps.
- **Also removed:** Evasion (2nd). **Kept:** AC bonus, maneuver training,
  fast movement, still mind, purity of body, the ki pool and every ki power
  (our 0.10.x additions included). Deliberately stackable with the Qinggong
  - the two remove disjoint features.
- **CRITICAL BUGFIX - eight archetypes were never loaded.** Cardinal,
  Solipsist, LionShaman, TrueShape (0.7.x), PolearmMaster, StrategicSoldier,
  Exoticist (0.9.x) and Qinggong (0.10.x) compiled and passed CI but were
  never wired into the mod's configuration pass - their blueprints were
  never created in-game. The diagnostics table that should have caught this
  only reports in the game log. All eight are registered for real now; the
  ConfigureAll list and the diagnostics table must stay in lockstep.

## 0.12.0 — two homebrew hunters (the user's open brief)

- The user's brief: "make 2 hunter archetypes, both completely of your
  own volition." No tabletop anchors - both are original designs, built to
  cover opposite halves of the hunter fantasy.
- **Venomblood Hunter (homebrew):** the serpent-touched attrition hunter.
  KEEPS the animal companion and the beast-aspects (the hound harries
  while the venom works); trades away the entire tactical half - hunter
  tactics, the teamwork-feat progression, woodland stride, one with the
  wild (both halves). Kit: Serpent's Gift (1st, once per round the first
  hit that connects injects venom: Fort save DC 10 + 1/2 level + Wis or
  1d2 Con damage; supernatural - works on poison-immune demons at +4 to
  their save, the same Worldwound lever the Eldritch Poisoner uses),
  Serpent's Skin (6th, poison immunity), Neurotoxin (9th, failed saves
  also sicken).
- **Riftstalker Hunter (homebrew):** the Worldwound loner. Trades the
  ENTIRE companion side (animal companion, raise companion, one with the
  wild) plus the teamwork-tactics side; keeps aspects, spells, woodland
  stride. Kit: Rift Mark (1st, first hit per round brands the target for
  a minute; +2 attack and damage vs the marked), Rift Stride (2nd,
  +10 ft speed), Riftstudy (5th, +2 further vs the marked, total +4),
  Hunter's Feast (12th, killing marked prey grants a minute of +2 attack
  and damage against everything).
- The two are mutually exclusive by construction (both remove Hunter
  Tactics, the teamwork feats and One with the Wild) - as same-class
  archetypes should be.
- **Coverage:** vanilla WOTR hunter archetypes are Forester (terrain
  focus, no companion), Divine Hunter (domain) and Urban Hunter (city
  cunning); neither of ours collides - venomblood is the poison-attrition
  concept, riftstalker the mark-focused solo hunter. HomebrewArchetypes
  adds no hunter content that clashes either.
- Engine notes: both deliveries ride the proven RuleAttackWithWeapon
  handler with the once-per-round component-data cooldown (Step Aside /
  Crushing Fist bookkeeping); the toxin strain deals its Con damage on
  buff activation (the Eldritch Poisoner strain wiring); mark and feast
  bonuses use global rulebook handlers (the Polearm Parry guard wiring);
  Rift Stride is the vanilla movement-speed component. Nothing here is
  mode-gated - real-time and turn-based behave identically.

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

## Alchemist homebrew archetype — Construct Crafter (user design) — IMPLEMENTED (v1)

Construct summons, NOT animal companions: uncontrollable (AI-driven), up to three active
(one per base), less customizable than a companion. Deploy a base as a standard action;
the construct lasts until destroyed or until the same base is deployed again.

| Feature | Real level | Details |
|---|---|---|
| Deploy Iron-Hearted Hound | 1 | Full clone of the game's own GolemWoodSummon (dog-shaped wood golem — no metal dog model exists and blueprints cannot retint model materials, so the look is honestly wooden), Summoned faction, 2 construct levels (weakest base), −4 Str, −2 Dex, HP/3; DR = ½ alchemist level, bypassed by adamantine (applied at deploy); engine summon link follows the crafter out of combat |
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
| Infernal | +2d6 fire on hit, shaken on hit, +10 speed, +init, **Blink Strike** (teleport to target + weapon attack, once per round) |
| Lightless (per-base) | Hound/Humanoid: SA 1/3 AL + attack/AC · Golem: shaken + frightened on hit, +Will |
| Booming | +AL damage, **every hit detonates: sonic damage to enemies near the target — starts 1d6, +1 die per 5 AL (max 5d6)**, −AL/2 AC, −2 saves (glass cannon) |
| Quick | Extra attack (haste-style), +10 speed, +init, −AL/2 damage |
| Galvanized *(original)* | +1d6 electricity on hit, +init/+Reflex, +10 speed |
| Magnetized *(original)* | +AL/2 CMB & CMD, slow on hit |
| Arbalest *(original)* | +AL/2 attack & damage, +10 speed, −AL/2 AC; **archery playstyle** — humanoid fields a composite longbow, hound/golem spit bolts from range |

Core adaptations (v1): on-hit riders and self-burn use fixed values (not AL-scaled) —
`AdditionalDamageOnHit` takes static dice and the DoT is a flat component; casting-flavored
cores are now real casters via **brains v1** (see below); Infernal constructs blink to
their prey (**Blink Strike**: teleport next to the target + a full weapon attack via the
rules pipeline — crits, sneak dice, and on-hit riders all apply, once per round); Cold
humanoid's charge immunity to AoOs has no native hook (documented); the
Flaming-golem/Guard interaction restores AoOs but does not halve the aura damage; Soft's
identity is a durable self-mending chassis (Mend + temp HP) — ally auras are not planned.
Auras use the stock `FireDamageAreaEffect`.

**Brains v1 (custom AI):** Wrath brains are ordered AiAction lists. Two custom brains
(`CrafterCasterBrain`: cast any granted construct ability then attack;
`CrafterRangedBrain`: bolt spit then attack) built from BPCore's AI configurators
(`BrainConfigurator`, `AiCastSpellConfigurator`) — no custom considerations yet
(defaults). Two game-version realities shaped the implementation (verified via a CI
metadata probe against the live game DLL): the game's `BlueprintAiAttack` is compiled
**internal**, so mod code cannot create a custom attack action — the brains instead
**lift the stock attack AiAction** out of a base-game unit's brain and reuse it as the
weapon-attack fallback; and `BlueprintUnit.m_Brain` is **private** (public
`DefaultBrain` getter only), so variant units get their custom brain assigned via
reflection. A further engine reality (verified by reading the game's IL): a cast action
is only added to a unit's available actions when the unit **owns** the ability
(`BlueprintAiCastSpell.ShouldBeInActionsList`), so one brain can safely list every cast —
unowned casts silently drop out. The deploy action now spawns **role-variant units** by core:
Flaming humanoid → caster with **Fire Blast** (15-ft burst, 6d6 fire, Reflex half); Cold
golem → caster with **Ice Ray** (4d6 cold + slow); Soft → casters with **Mend** (temp HP =
alch level patch — context-heal actions are evaluator-based, so healing ships as temp HP
for now); Arbalest → archer humanoid (composite longbow) / bolt-spitting hound & golem.
Base identity is tracked with marker buffs, so any variant replaces any construct of the
same base.

**Brains v2 (program behaviors):** the active program now picks the construct's brain at
deploy time via the engine's public runtime swap (`UnitBrain.SetBrain` +
`RestoreAvailableActions`). Three behavior brains, all built from stock action types:

- **Passive** — `CrafterPassiveBrain`: close escort (5 ft) of the crafter; never charges
  or casts. The engine's own run-away action flees toward the map exit (it would desert),
  so "stay out of harm" is expressed as sticking to the crafter's side instead.
- **Guard** — `CrafterGuardBrain`: casts (support, e.g. Mend) > weapon attacks > return to
  the crafter (10 ft). A bodyguard: fights while threats exist, falls back to the
  crafter's side when clear.
- **Distance** — `CrafterDistanceBrain`: casts > ranged attacks > loose escort (30 ft).
  Ranged constructs stop at weapon/ability range instead of charging.

The follow actions target the **crafter specifically**: friends are the candidate group,
and a `FactConsideration` scores only units carrying an active program marker buff (the
marker buffs sit on the crafter while a program is toggled on). Also in this batch:
cast actions now carry `BaseScore 20` (the default 1.0 lost to the stock attack action,
so v1 casters mostly melee'd); casts are once-per-round; **Mend only targets wounded
allies** (full-HP targets score 0 — previously it would spam the first healthy ally);
the Soft hound now actually casts Mend (its baked brain only referenced Bolt Spit); and
simultaneous program toggles now resolve in the documented priority order
(Chaos > Distance > Guard > Flank > Aggressive > Passive — the code previously resolved
in reverse). Program behaviors apply at deploy; re-deploy to change a live construct's
behavior. Aggressive/Flank/Chaos keep the stock charge AI (their identity is the stat
package).

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
| Deploy Humanoid Construct | 7 | Metal construct body (Kolyarut chassis, the android-looking inevitable) with 6 real fighter levels baked in + fake fighter levels at deploy so the final level is (alchemist level − 2); cold iron longsword fallback |
| Deploy Stone Golem | 16 | Tabletop clay-golem role on the stone golem chassis (renamed to match the model): no berserk, −20 HP (87), Str 30, 14 construct levels, DR 5/adamantine |

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

**Previously deferred, now resolved or dropped:**

- ~~Bomb damage halving~~ — dropped by user decision: bombs keep full damage.
- ~~Flank's caster-level reduction~~ — shipped: the Flank construct buff now carries a
  rider that subtracts the crafter's full alchemist level from the construct's effective
  caster level on every ability it uses (`RuleCalculateAbilityParams.AddBonusCasterLevel`),
  weakening ability DCs and level-scaled effects while the flank program runs.
- ~~Program AI behaviors~~ — shipped in brains v2 (see below). Wrath has no
  "spell failure from being attacked" mechanic (Guard's protect flavor stays stat-based).
- ~~Infernal teleport-attacks~~ — shipped as Blink Strike (see the core adaptations note).
- ~~Soft ally auras~~ — dropped by user decision; Soft keeps the self-mending chassis.

**v1 adaptations / known gaps (playtest watch-list):**

1. **Extracts cannot be removed** — spellcasting is `m_Spellbook` on the shared
   `BlueprintCharacterClass`; an archetype cannot remove it without modifying every
   alchemist. Options: leave as-is (current), or promote this to a full custom class later.
2. **Bombs are NOT removed** and keep full damage (final user decision).
3. **Clay golem chassis** is built on the game's stone golem SUMMON variant
   (GolemStoneSummon — no clay golem exists in Wrath's unit list); slow-breath components
   are stripped by name where possible — if the golem occasionally slows a target, that's
   the leftover (report it).
4. **Unit names** show the source creature's name (SharedStringAsset authoring needs an
   asset bundle — same territory as custom textures).
5. Deploy has **no resource cost** yet (standard action only) — cost TBD with user.
6. Same-base replacement matches by blueprint (any crafter's construct); single-crafter
   parties are unaffected.
7. Poison-feature removal levels are best-effort (L2/L5/L10 per tabletop); verify in the
   character sheet that no poison rows remain.

**Open design questions:** deploy resource cost, future core list, future program list,
whether constructs persist through area transitions (they are scene-local today).

## Cavalier

### Faithful port — Sister-in-Arms (Adventurer's Guide) — IMPLEMENTED (0.6.0)

| Feature | Real level | Details |
|---|---|---|
| Halfhearted Challenge | 1 | Challenge damage bonus is half level (negative untyped modifier while the target carries the vanilla challenge buff). Alters challenge |
| Maiden's Order | 1 | Both orders: the vanilla Lion progression + a ported Dragon progression, granted as facts. Replaces mount (CavalierMountSelection) and the order choice (CavalierOrderSelection) |
| Devoted Defender | 3 | Grants the vanilla Divine Guardian's Bodyguard feature (Wrath has no Bodyguard feat). Replaces cavalier's charge |
| Maiden's Loyalty | 4 | +2 Will vs compulsion/mind-affecting, +1 per 4 levels beyond 4th. Replaces expert trainer (name passed speculatively - warn-and-skip if absent) |
| Dedicated Commander | 11/20 | Lion's call as move action (swift at 20th) + Dragon's Strategy + Act as One. Replaces mighty charge and supreme charge |

**Ported Order of the Dragon** (WOTR never had it; built with the pplus
Inquisition custom-order recipe): class skills (Perception, Lore (Nature) as
Survival's stand-in); challenge benefit = ally attack aura vs the challenge
target (AddAttackBonusAgainstFactOwner on the vanilla challenge-target buff);
Aid Allies adapted to a challenge-gated +2/+3/+4 AC-and-saves aura (no aid
another action in Wrath); Strategy (8th) as a standard-action party buff; Act
as One (15th) as a dodge aura + immediate attacks for adjacent allies
(ForceAttackOfOpportunity - Wrath cannot move other units on command).

### Homebrew — The Carousel (user-commissioned) — IMPLEMENTED (0.6.0)

The charge archetype: strike one enemy, wheel, strike the next.

| Feature | Real level | Details |
|---|---|---|
| Carousel Training | 1 | Carousel Charge: standard-action charge (StagCharge engine - real movement, double speed, vanilla ChargeBuff, true charge attack) + Wind-Rider: no AoOs while charging (AddBuffExtraEffects on ChargeBuff) |
| Wheel About | 3 | Free-action second charge against a new enemy; primed by any charge that round (priming marker rides ChargeBuff), once per round. Replaces cavalier's charge |
| Surefooted Steed | 4 | Mount ignores difficult terrain (AddConditionImmunity via AddFeatureToPet) + +2 all saves; charges ignore difficult terrain too |
| Slip the Line | 5 | Allies never block her charge line (target-restriction bypass). Replaces banner |
| Grand Carousel | 11 | Wheel needs no priming (once/round, any round); +2 attack while charging. Replaces mighty charge |
| Eternal Carousel | 20 | The wheel is unlimited. Replaces supreme charge |

Engine notes: the charge is an AbilityCustomLogic coroutine adapted from
pplus's StagCharge (ForcedPath movement, charge state, UnitAttack with
IsCharge, turn-based and real-time branches, mounted command sync via
GetSaddledUnit/GetRider); ability wiring follows pplus's AerialAssault recipe
(CopyFrom the vanilla ChargeAbility, excluding its own behavior components).
The wheel is the same logic as a free action whose gates (priming/spent
markers) read the archetype's features at runtime. Kept: mount, challenge,
order, tactician.

## Cleric

### Faithful port — Cardinal (Ultimate Intrigue) — IMPLEMENTED (0.7.0)

| Level | Cardinal grants | Cleric normally grants |
|---|---|---|
| 1 | Political Skill (6+Int ranks, Trickery as class skill), light armor + simple weapons | ClericProficiencies (light/medium armor, shields, weapons), second domain, spontaneous casting |

Trades (all real removals against the live progression):
- ClericProficiencies → re-granted as LightArmorProficiency +
  SimpleWeaponProficiency (medium armor and ALL shields are gone; if the
  deity's favored-weapon proficiency rides the removed package it is lost -
  the honest cost of the armor trade).
- SecondDomainsSelection → nothing (ONE domain).
- ClericSpontaneousCast → nothing (no converting prepared spells into cures).
- Base attack bonus: the archetype carries the slow (wizard) table via
  `SetBaseAttackBonus`; skill ranks via `SetAddSkillPoints(4)` on top of the
  cleric's 2+Int.

Adaptation: WOTR folds Bluff/Intimidate into Persuasion and the Knowledges
into Knowledge (World) - both already cleric class skills - so the tabletop's
four extra class skills collapse into existing ones; Trickery (the game's
deception skill) is granted as the intrigue-flavored stand-in.

### Homebrew — The Solipsist (user design: the selfish cleric) — IMPLEMENTED (0.7.0, redesigned 0.7.1)

| Level | Solipsist grants | Cleric normally grants |
|---|---|---|
| 1 | Martial Devotion (martial weapons, bonus combat feat) | — |
| 1 (channel's level) | Solipsism | Channel Energy |
| 5 / 15 | Focused Faith (+1 / +2 flat untyped attack) | — |
| 10 / 20 | Toughened Faith (+5 / +10 flat untyped hit points) | — |

Solipsism, as commissioned (v2 after user feedback):
- Every cleric spell that can target an ally can now only target the
  solipsist himself (enemy targets pass - cure spells still sear undead).
- **Communal / ground-aimed ally blessings are uncastable for him** - the
  caster restriction is inert for everyone else, but for him the whole
  communal family (and mass cures, Prayer, and any point-targeted
  ally-helpful spell) is closed. This is the trade's real teeth late-game.
- Every blessing such a spell applies to him is laid down a SECOND time as an
  untyped echo: a clone of the buff whose bonus descriptors are all rewritten
  to None, so it stacks with the original's typed bonus and cannot be
  overridden by same-type effects. Two Blesses' worth of attack, two Shields
  of Faith' worth of deflection.
- Healing spells he casts on himself roll their healing twice (a second,
  independent heal roll in the same context).
- Personal-range battle blessings (divine power, righteous might, frightful
  aspect) echo too - no lock needed, they are already self-only.
- Channel energy is removed entirely - a congregation of one has no flock.

Martial Devotion: martial weapon proficiency and one bonus combat feat (the
vanilla FighterFeatSelection, granted the way the vanilla Crusader grants
its bonus feats). The 0.7.1 full-BAB table was replaced in 0.8.0 (per user
feedback) with flat untyped bonuses: Focused Faith (+1 attack at 5th, +2 at
15th, via a RuleCalculateAttackBonus handler - the darkcodex AddAttackBonus
pattern) and Toughened Faith (+5 HP at 10th, +10 at 20th, HitPoints stat
modifiers - the pplus ShadowDancerSpawn pattern).

Engine notes: the lock is a custom `IAbilityTargetRestriction` and the
communal denial a custom `IAbilityCasterRestriction`, both added to the
shared vanilla spell abilities (inert for every caster without the Solipsism
fact, so other classes are untouched); the scan walks the cleric spellbook's
spell list plus ability variants and classifies each spell as communal
(point-targeted + helpful → denied), personal (buffs → echo only) or
unit-targeted (helpful or heals → lock + echo). The echo is appended to the
spell's EXISTING AbilityEffectRunAction list as an element copy of the
spell's own apply-buff actions (buff reference swapped to the untyped clone)
and heal actions; Conditional-gated tiers are deep-copied whole so the same
conditions re-evaluate at echo time (the correct tier doubles, never both).
Echo-buff guids derive deterministically from the original buff's guid (XOR a
fixed mask), so they are save-stable. Domain spells are not part of the
scanned list; harmful point-target spells (selective fireballs etc.) are
untouched. The log lists every converted spell at load (`[solipsist]`
prefix), including each denied communal and each created echo buff.

## Druid

### Faithful port — Lion Shaman (totemic shaman) — IMPLEMENTED (0.8.0)

| Level | Lion Shaman grants | Druid normally grants |
|---|---|---|
| 2 | Totem Transformation (three aspects) | Woodland Stride |
| 4 | Feline Wild Shape (tiger/smilodon) | — (wild shape begins) |
| 5 | Totemic Summons (Augment Summoning) | Resist Nature's Lure |
| 6 | (wild shape re-added, 2 levels late) | Wild Shape |
| 8 / 14 | Greater/fiercest feline tiers | — |
| 9 / 13 / 17 | Shaman's Feats (Dodge, Iron Will, Improved Iron Will, Skill Focus (Acrobatics)) | Venom Immunity (9th) |

Trades (all real removals/delays against the live progression, matched by
guid): DruidWoodlandStride, ResistNaturesLure, VenomImmunity removed;
DruidWildShape and the elemental wild shape features delayed two levels each
(ArchetypeRemovals.DelayFeatures - remove at the live level, re-add at +2).

Adaptations (tabletop → WOTR):
- Trackless step and a thousand faces do not exist in WOTR; the 2nd and 5th
  level trades map onto woodland stride and resist nature's lure.
- Totem Transformation's minutes-per-day pool simplifies to three toggleable
  aspects (the Untouchable Rager control-toggle pattern), mutually exclusive
  via a buff component that sheds the other aspects on activation. Senses =
  the vanilla scent feature; natural weapons = the vanilla Animal Fury bite.
- Wild shape "-2 levels, +2 for felines": the vanilla animal + elemental
  shape features are delayed two levels, while the feline (smilodon) family
  arrives two levels EARLY via the shifter's tiger form abilities, granted
  whole with their 8th/14th tier variants (cross-class form granting, the
  Feral Champion pattern).
- Totemic Summons: WOTR summons are already standard action and the engine
  exposes no summon hook for the feline temp-HP rider - the feature grants
  vanilla Augment Summoning instead (a real, always-on summon upgrade).
- Bonus feats: Lunge was not found among the game's blueprints; the list is
  Dodge, Iron Will, Improved Iron Will, Skill Focus (Acrobatics).
- Nature bond's domain restriction and the feline wild-empathy bonus are not
  ported (the vanilla features stay).

### Homebrew — True Shape (user design: the permanent wild shape) — IMPLEMENTED (0.8.0)

| Level | True Shape grants | Druid normally grants |
|---|---|---|
| 1 | True Shape (permanence + attunement), The Menagerie (11 forms) | Wild Shape (4th) |
| 8 | Greater Menagerie (tier-8 forms) | — |
| 15 | Master Menagerie (tier-15 forms) | — |
| — | Spellbook capped at 4th-level spells, detect magic filtered | full druid spellcasting |

As commissioned:
- Permanence: while the Beast Soul toggle is on (default on), every
  polymorph buff on the druid is replenished with a long duration when its
  remaining time runs low - remove-then-add, so exactly one copy ever
  exists. Dropping the form (or toggling off) lets it lapse naturally.
- More forms for various roles: the vanilla animal wild shape is REPLACED by
  an 11-family menagerie - tank (bear, elephant, dinosaur), predator
  (smilodon, wolf, wolverine, boar), control/utility (spider, fey, manticore,
  griffon) - each form a complete CLONE of its shifter source (model,
  attacks, specials) recast as a FREE action, with the stronger family tier
  at 8th and the final tier at 15th. Elemental wild shape is a separate
  feature family and is kept. Clone guids derive deterministically from the
  source ability's guid (XOR a fixed mask), so they are save-stable.
- **Bestial casting**: Natural Spell is built in - the druid casts his
  reduced spell list from inside any form (0.8.1, the anti-shifter identity:
  the shifter has no magic at all). Scaled in 0.8.2 so it carries to 20th:
  1st-4th level castings grow +1 at every odd level from 5th (a cloned slot
  table, +8 per spell level by 19th), and while shaped his spells cast at
  caster level +1 per 2 druid levels (effective CL 30 at 20th - more than any
  other class would normally get, because the spells themselves are so weak).
- Stats from equipment: while polymorphed, the worn armor and shield keep
  their full AC (read live from the equipment by reflection, applied as an
  Armor-descriptor modifier on RuleCalculateAC - recomputed every attack, so
  gear swaps apply immediately). Rings, cloaks and belts already keep
  working under WOTR polymorph, so armor is the gap this closes.
- Significantly less spellcasting: a cloned druid spellbook (the Sanguine
  Font reflection-copy pattern) attached via the archetype's
  ReplaceSpellbook, carrying a cloned spell list capped at 4th-level spells.
  Higher-level slots atrophy with the list.
- Losing detect magic: any spell whose name matches DetectMagic is filtered
  from the cloned list by name. Vanilla WOTR has no detect magic cantrip, so
  on an unmodded game the filter is dormant - it exists so the trade holds
  under any spell-adding mod.

## Fighter

### Faithful port — Polearm Master (Advanced Player's Guide) — IMPLEMENTED (0.9.0)

| Level | Polearm Master grants | Fighter normally grants |
|---|---|---|
| 2 | Pole Fighting (close-quarters polearm bonus, +1→+5) | Bravery (all instances) |
| 3 | Steadfast Pike (AoO bonus with polearms, +1→+5) | Armor Training 1-4 (all four) |
| 5 | Polearm Training (+1→+4 at/dmg with polearms) | Weapon Training 1-4 (all four) |
| 9 | Flexible Flanker (+2 when an ally threatens the target) | — |
| 13 | Sweeping Fend (+2 trip / bull rush with polearm) | — |
| 17 | Step Aside (+2 dodge AC; TB-mode reactive 5-foot step) | — |
| 19 | Polearm Parry (swift action; 1-round +2 shield AC and DR 5 for allies within 10 ft) | Armor Mastery |

Adaptations (tabletop → WOTR, tightened in 0.9.1): no grip-shortening hook
exists, so Pole Fighting stays a growing bonus on polearm attacks against
adjacent enemies (the inverse of the shrinking penalty); Steadfast Pike is
now polearm-gated exactly as written (AoO detection via the Reason chain,
the COP PairedOpportunists idiom; the readied-attack half has no WOTR
analogue); Flexible Flanker's chosen-square flanking becomes +2 whenever any
ally threatens the target (the shared AllyFlankerBonus component); Sweeping
Fend adds +2 to trip and bull rush maneuvers made with a polearm (0.9.1:
the trip-feature benefit, no longer free Improved feats); Step Aside (0.10.0)
takes a real reactive 5-foot step in turn-based mode when an adjacent
enemy's melee attack resolves (the tabletop triggers on the enemy's step,
which has no engine event) plus the constant +2 dodge AC in both modes -
five-foot steps do not exist in real-time-with-pause; Polearm Parry is a
swift action
bracing the weapon for 1 round (0.9.1: the closest the engine has to the
tabletop's immediate action; +2 shield AC vs attackers in his reach, DR 5
from them - global rulebook handlers, the GoldenLegionnaire pattern). The
20th-level weapon mastery's polearm-only restriction is not enforced (the
vanilla selection already allows polearms).

### Faithful port — Strategic Soldier (Orphaned Bookworm Productions) — IMPLEMENTED (0.9.0)

| Level | Strategic Soldier grants | Fighter normally grants |
|---|---|---|
| 1 | Flanker (+2 when an ally threatens the target) | the 1st-level bonus combat feat |
| 2 | Sidestep → Mobility | Bravery (all instances) |
| 3 | Interpose → Bodyguard + In Harm's Way | Armor Training 1 |
| 7 | Defending Allies (+2 dodge AC to allies within 5 ft) | Armor Training 2 |
| 11 | Reckless Strike (swift toggle: +5 at/dmg, -5 AC, 1 round) | Armor Training 3 |
| 15 | Knock Off-kilter (full-round strike; threatening allies pile on) | Armor Training 4 |
| 19 | Punishing Strike (AoO vs ally-wounders, once/round, spends an AoO) | Armor Mastery |

Adaptations: Sidestep does not exist in WOTR (and the engine has no
5-foot-step reactions) - the vanilla Mobility feat carries the
evade-through-threatened-squares role; Interpose's X/day damage-taking is
carried by the vanilla Bodyguard + In Harm's Way package (unlimited, but it
occupies his reaction - the Sister-in-Arms Devoted Defender recipe);
Defending Allies' fighting-defensively condition has no engine hook, so the
+2 dodge AC for allies within 5 feet is constant; Reckless Strike's "every
hit on him is a critical threat" is averaged into a -5 AC penalty for the
round; Knock Off-kilter is realized as a standard-action strike where the
soldier and every threatening ally immediately attack (the Act as One
ForceAttackOfOpportunity recipe), size-restricted via a custom target
restriction; Vital Strike interplay is not implemented; Punishing Strike
consumes an attack of opportunity (the ttt AoO-budget pattern) and is
once-per-round.

### Homebrew — Exoticist (user design: the exotic-weapon specialist) — IMPLEMENTED (0.9.0)

| Level | Exoticist grants | Fighter normally grants |
|---|---|---|
| 1 | Exotic Arsenal (proficiency with every exotic weapon) | — |
| 5 | Exotic Training (+1→+4 at/dmg with exotic weapons) | Weapon Training 1-4 |

Deliberately simple, per the commission: one broad door (all exotic
proficiencies open at 1st via the vanilla AddProficiencies component) and
one narrow specialization (scaling attack/damage with exotic weapons only -
the shared WeaponSpecialistBonus component, the same engine as the Polearm
Master's training). Bravery and armor training stay. The exotic category
list (0.9.1, verified against a compiling WOTR mod) covers all 21 exotic
WeaponCategories in the game: bastard sword, dueling sword, dwarven waraxe,
elven curved blade, estoc, falcata, fauchard, sawtooth sabre, kama, sai,
tongi, siangham, nunchaku, sling staff, double axe, two-bladed sword,
dwarven urgrosh, gnome hooked hammer, heavy/light repeating crossbow,
shuriken. (WOTR has no katana or whip categories - those weapons stayed in
Kingmaker.) Mod-added weapons that reuse vanilla categories are covered;
exotic categories invented by other mods are not.

## Monk

### Faithful port — Qinggong Monk (Advanced Player's Guide) — IMPLEMENTED (0.10.0)

| Level | Qinggong grants | Monk normally grants |
|---|---|---|
| 3 | extra Ki Power selection | Still Mind, Fast Movement |
| 4 | extra Ki Power selection | — |
| 5 | extra Ki Power selection | Purity of Body |

WOTR's base monk already carries Owlcat's selectable ki powers
(MonkKiPowerSelection: true strike, restoration, barkskin, abundant step,
diamond body, diamond soul, cold ice strike, wholeness of body and more),
so the tabletop's qinggong - "trade fixed abilities for selectable ki
powers" - ports as: the three fixed gifts (still mind, fast movement,
purity of body) become three extra selections at 3rd/4th/5th. The
tabletop's other trade targets (slow fall, high jump, timeless body,
tongue of sun and moon, empty body) never existed in WOTR - its ki
selection already swallowed most of the qinggong list.

**New ki powers (added to the shared MonkKiPowerSelection - every monk
gains them):** ki invisibility (2 ki, 8th), ki neutralize poison (2 ki,
8th), ki freedom of movement (2 ki, 10th), ki holy aura (4 ki, 16th), plus
the 0.10.1 additions with more teeth: ki glimpse of truth (true seeing,
2 ki, 10th), ki dragon breath (fire, 2 ki, 8th) and ki frightful aspect
(3 ki, 16th). Each is built the way Owlcat builds its own ki powers - a
clone of the vanilla spell (minus its spell-list component) that spends
the Ki resource via AbilityResourceLogic.

### Homebrew — Hammerfist Monk — IMPLEMENTED (0.11.0)

The user's brief: a monk focused on one big damage hit with her fists
instead of many hits - slightly lower overall output (it is easier to land
one full attack than a whole flurry), a fair bit of the monk gimmickry
stripped out, and a little something extra to differentiate.

| Level | Hammerfist grants | Monk normally grants |
|---|---|---|
| 1 | Crushing Fist | Flurry of Blows, Stunning Fist |
| 2 | — | Evasion (removed) |
| 9 | Rolling Thunder | Improved Evasion (removed) |

- **Crushing Fist (1st):** once per round, the first unarmed strike that
  HITS deals +2 +1½×level extra damage. The flurry line (base feature +
  both unlock tiers) and the Stunning Fist line (base + fatigue + sickened
  upgrades) are removed outright. Tuning intent: a move-and-punch round
  lands roughly 85-95% of a flurry round's damage, but every point of the
  boost rides the first hit that connects - no full-attack dependency,
  nothing wasted on a miss.
- **Rolling Thunder (9th, the differentiator):** the first unarmed hit of
  each attack sequence triggers a free trip maneuver (CMB vs CMD, no
  action, no AoO) - the game's own on-hit trigger component (the Piledriver
  wiring), unarmed-gated via its weapon-category check, with the knockdown
  action chained on maneuver success. Feeds on the maneuver training the
  monk keeps.
- Engine notes: the once-per-round bookkeeping is the same component-data
  cooldown Step Aside uses; Rolling Thunder is pure vanilla components, so
  both features work in real-time and turn-based modes alike.

## Hunter

### Homebrew — Venomblood Hunter — IMPLEMENTED (0.12.0)

The serpent-touched attrition hunter: keeps the hound and the aspects,
trades the tactical half of the class for poison.

| Level | Venomblood grants | Hunter normally grants |
|---|---|---|
| 1 | Serpent's Gift | Hunter Tactics (removed) |
| 2 | — | teamwork-feat progression begins (removed) |
| 6 | Serpent's Skin | Woodland Stride (removed) |
| 9 | Neurotoxin | One with the Wild (removed, both halves) |

- **Serpent's Gift (1st):** once per round, the first weapon hit that
  connects injects venom - Fortitude (DC 10 + 1/2 hunter level + Wis) or
  1d2 Constitution damage; the strain burns out after a round, so every
  round brings a fresh dose and a fresh save. Supernatural: functions
  against poison-immune creatures (most of the Worldwound) at +4 to their
  save - the documented Eldritch Poisoner lever.
- **Serpent's Skin (6th):** immunity to poison.
- **Neurotoxin (9th):** failed saves also sicken for 1 round.

### Homebrew — Riftstalker Hunter — IMPLEMENTED (0.12.0)

The Worldwound loner: no beast, no pack tactics - just the mark, the
stride and the feast.

| Level | Riftstalker grants | Hunter normally grants |
|---|---|---|
| 1 | Rift Mark | Animal Companion (removed) |
| 2 | Rift Stride | Hunter Tactics (removed) |
| 5 | Riftstudy | teamwork-feat progression (removed) |
| 12 | Hunter's Feast | One with the Wild (removed, both halves) |

- **Rift Mark (1st):** first hit per round brands the target for one
  minute; +2 attack and damage against the branded.
- **Rift Stride (2nd):** +10 ft land speed (nothing to wait for).
- **Riftstudy (5th):** +2 further against the marked (total +4).
- **Hunter's Feast (12th):** a marked kill grants one minute of +2 attack
  and damage against everything.
- Also removed: Raise Companion (there is nothing to raise).

## Level plan per class (test-mode rule)

All archetype features land at level 1 while `LevelPlan.AllAtLevelOne == true`. The table
above doubles as the source of truth for real levels; the same pattern (documented table +
`LevelPlan.L(n)` call sites) applies to every future archetype.

## Barbarian

### Faithful port — Breaker (APG) — IMPLEMENTED (v1)

| Feature | Real level | Details |
|---|---|---|
| Destructive | 3 | +½ barbarian level (min +1) bonus damage on weapon attacks vs constructs. Replaces fast movement |
| Battle Scavenger | 3 | +1 damage with scavenged arms (club, greatclub, quarterstaff, spear, dagger, sickle, maces, flails) per 3 levels beyond 3rd |

Wrath adaptations (engine gaps, documented in-game):
- No sunder maneuver, no unattended-object combat targets, no improvised/broken weapon
  states exist. Destructive keys on constructs (detected via the ConstructType feature).
- Trap sense does not exist on the Wrath barbarian, so Battle Scavenger's half of the
  tabletop trade is void - the feature is additive.

### Homebrew — Bloodstorm (user-approved pitch) — IMPLEMENTED (v1)

| Feature | Real level | Details |
|---|---|---|
| Bloodstorm (Crimson Edge + Feed the Storm) | 2 | While raging: weapon hits apply a stacking Bleeding Wound (1d4/round per rank, max 5 ranks); fast healing 1/2/3 while 1/3+/5+ enemies within 30 ft are bleeding (any bleed source counts). Replaces uncanny dodge |
| Bloodspout | 5 | While raging: crits vs bleeding targets spray - enemies within 10 ft of the target gain a Bleeding Wound rank and are shaken 1 round. Replaces improved uncanny dodge |
| Open the Floodgate | 7 | Standard action while raging: consumes all Bleeding Wound stacks on enemies within 30 ft, heals 1d6 per rank consumed. Replaces the damage reduction chain (7/10/13/16/19) |

Implementation notes: bleed ticks and riders use direct damage rules (the ConstructSonicBoom /
PeriodicSelfDamage patterns); rank stacking is manual (first hit creates the buff, later hits
add stacks, cap 5 - stacks are runtime state, so after a save/reload an existing bleed ticks
as one rank until re-applied); class-feature components rely on feature contexts (same
mechanism as ContextRankConfig on class features).

## Arcanist

### Faithful port — Covert Mage (Flaming Crab Games, Advanced Archetypes) — IMPLEMENTED (v1)

| Feature | Real level | Details |
|---|---|---|
| Covert Training | 1 | Class skills: Persuasion, Perception, Stealth, Thievery; light armor proficiency; no arcane spell failure in light armor (-20 ASF via the engine's ArcaneSpellFailureIncrease, matching the heaviest light armor) |
| Mesmerizing Touch | 3 | Standard action, costs 1 arcane reservoir point: melee touch attack applies a Will-save penalty equal to arcanist level for max(1, Cha mod) rounds. Replaces the exploit gained at 3rd level |
| Spell Trick | 7 | Casting a 1-standard-action spell at an adjacent target allows a feint (Persuasion vs 15 + target Perception): on success the spell does not provoke an AoO from the target (1-round "Feinted" debuff zeroing its AoOs) and the target saves twice, taking the lesser result. Replaces the exploit gained at 7th level |
| Illusion Spotter | 11 | Her saves against illusion-school effects are rolled twice, taking the better result. Replaces the exploit gained at 11th level |

Wrath adaptations (engine gaps, documented in-game):
- WOTR has no Bluff, Disguise, Perform or Sleight of Hand skills: class skills map to
  Persuasion, Perception, Stealth and Thievery; the feint check uses Persuasion vs the
  Dazzling Display formula (10 + target Hit Dice + Wisdom modifier; WOTR exposes
  neither Bluff nor BAB).
- Save-twice-take-lesser uses the engine's d20 reroll hook (the same mechanism as the
  vanilla Azata Favorable Magic ability); the no-provoke clause uses the
  ConstructCrafter NoAoO pattern (1-round -50 AoO-count debuff on the target).
- WOTR has no generic illusion-disbelief system: Illusion Spotter's free disbelieve
  becomes take-the-better on saves vs illusion-school effects.
- The tabletop spellbook clause (two free spells per level) is a no-op - WOTR
  arcanists prepare from the full spell list.
- Exploit removals are level-specific (3/7/11) against the vanilla
  ArcanistExploitSelection (b8bf3d5023f2d8c428fdf6438cecaea7), which appears at every
  odd level; the reservoir resource is the vanilla ArcanistArcaneReservoir
  (cac948cbbe79b55459459dd6a8fe44ce).

### Homebrew — Elemental Obsessor (user-commissioned: DPS arcanist) — IMPLEMENTED (v1, replaces the retired Arcane Artillerist)

The fantasy: total, single-minded devotion to one element - the obsessor can ONLY
cast her element's spells, but her list draws from every non-mythic spellbook in
the game. Trades the exploits gained at 1st, 3rd and 7th level:

| Feature | Real level | Details |
|---|---|---|
| Elemental Fixation | 1 (free spellbook swap) | Choose Fire/Cold/Acid/Electricity/Sonic. Spellbook replaced by a per-element book containing every spell of that descriptor from every non-mythic spellbook (each at its lowest level anywhere; built at configure time from ResourcesLibrary) |
| Obsessive Focus | 1 (trades exploit) | One hit per cast of a damaging spell (first ray or first persistent tick - dedupe keyed on the cast's AbilityData) deals bonus damage equal to half arcanist level (min 1); cantrips at half bonus (min 1) |
| Unstoppable Obsession | 3 (trades exploit) | Immunity/resistance cannot fully deny the element: floor starts at 20% of the raw damage and rises +5% per level after 3rd, ending at 105% at 20th - like the enemy were weak to the element. Electricity starts at 40% (same 105% cap). Shortfall paid as untyped direct damage |
| Obsessive Wellspring | 5 (additive) | A kill with her element's magic restores 1 arcane reservoir point - the arcanist-identity hook, feeding the exploits kept from 9th level and Consume Spells |
| Cathartic Release | 7 (trades exploit) | Single-target spell kills erupt: other enemies within 10 ft take 1d4 per two levels beyond 7th (1d4 at 7th, 7d4 at 19th; 8d4 for fire/cold). AoE spells and persistent areas never detonate |
| Shattering Pitch | 7 (additive, sonic perk) | Single-target sonic spells splash 25% of the damage dealt to other enemies within 10 ft |
| Corrosive Adaptation | 12/16/20 (additive, acid perk) | Adopt a spell from another element's list: it joins the acid list and its damage converts to acid (ReplaceEnergy on RulePrepareDamage - the TTT Elemental Spell mechanism) |

The original Arcane Artillerist (Weaponized Magic / Overcharge / Detonation /
Annihilating Surge) was removed before playtest per user feedback: Overcharge and
Annihilating Surge deleted outright, the rider retuned (cantrip halving, once per
cast), Detonation retuned to 1d4-per-two-levels-past-7 single-target-only, and the
elemental-fixation spellbook + immunity bypass added.

Element perks (equalizers): fire/cold +1d4 on Cathartic Release; acid Corrosive
Adaptation (late-game spell access via adaptation); electricity floors at 40%
(early-game immunities); sonic Shattering Pitch splash (small-list multiplier).
All spells in the replaced book cast off Intelligence - the spellbook, not the
spell, owns the casting attribute (same as the game's merged spellbooks).

## Bard

### Faithful port — Mummer Mage (Legendary Games) — IMPLEMENTED (v1)

| Feature | Real level | Details |
|---|---|---|
| Shtick of the Magi | 1 | +2 circumstance Persuasion (the prop; Perform/Bluff do not exist in WOTR). Replaces bardic knowledge |
| Imperious Gestures | 2 | +2 concentration (CheckConcentration stat). Replaces well-versed |
| Arcane Imitation | 2 | LearnSpellParametrized from the wizard list; picks at 2/6/10/14/18. Replaces **inspire competence** (0.5.3: versatile performance does not exist in WOTR, so the wizard-spell theft now costs the closest skill-support feature instead of being free) |
| Shtick Attunement | 5 | Grants the vanilla BondedItem feature (cast any spell from her spellbook 1/day, full-round) |
| Method Actor | 5 | All Lore skills use Charisma (ReplaceStatBaseAttribute - "she remembers playing a scholar"). Replaces lore master |
| Eucatastrophe | 10/16/19 | Adapted: extra Arcane Imitation picks (no "cast any unlearned spell" UI outside the bond). Replaces jack-of-all-trades |

### Homebrew — The Cook (user-commissioned) — IMPLEMENTED (v1)

The barding pun, taken literally: no performances, no bardic knowledge, no dirge
of doom, no jack of all trades, no mass suggestion - just meals.

| Feature | Real level | Details |
|---|---|---|
| Hearty Cooking | 1 | 3 meal charges per rest (resource, restore on rest). Each Serve ability: standard action, 1 charge, feeds EVERY ally within 30 ft an 8-hour non-dispelable meal buff (untyped; WOTR has no food-buff category - native cooking-recipe buffs are plain buffs too) |
| Recipe Book | 4/8/12/16/20 | Learn one new meal each pick |

**0.5.3 playtest rework:** ingredients became meals. Old design: one charge fed
one ally a strong buff. New design: one charge feeds the whole camp (30 ft) a
weaker buff - flat values dropped (HP +2/+1-per-3 -> +1/+1-per-4, speed +10 ->
+5, attack/saves keep +1 but lose their rank scaling, Lettuce AC becomes
rank-only at 1/12 levels) and only Bacon Wrap and Lettuce keep (slower) level
scaling. Starting menu: Bacon Wrap (+HP), Chicken Breast (+attack), Rice
(+saves), Beans (+speed), Lettuce (+AC). Recipe picks: Garlic (+Persuasion),
Chili Pepper (+damage), Cheese (+Lore skills), Mushroom (+initiative), Potato
(+Fort), Onion (+Perception), Coffee (+initiative/+speed), Butter (+saves
flat). All meal values are tuning candidates for the playtest pass.

## Magus

### Homebrew — Spellfist (user-commissioned: the magus/monk fusion) — IMPLEMENTED (v1)

The "class that focuses on using all the MissionWOTR feats": an unarmored,
unarmed wisdom-caster whose spells only ever land through a flurry of punches.

**Spellbook (1st, archetype ReplaceSpellbook — the Eldritch Scion mechanism, so
Spell Combat's UnitPartMagus resolves it):** every touch-range spell from every
non-mythic spellbook in the game, each at its lowest level anywhere (the
Elemental Fixation enumerator). Casting attribute: **Wisdom** (pinned in the
clone).

| Feature | Real level | Details |
|---|---|---|
| Spell Cascade | 1 | Replaces Spell Strike's payload: the delivery's dice are captured and zeroed, then divided into one share per expected full-attack punch (2 + BAB iteratives at 6/11/16, cap 5); each successful unarmed hit pays one share as an energy rider. Casts with no follow-up attacks dissipate (zeroed, warn-once). Non-damaging touch effects apply normally. Charge window: 2 rounds (a standard-action cast can unload on the next turn's full attack) |
| Bare Fist Discipline | 1 | Trades ALL proficiencies (weapons/armor/shields — manufactured weapons swing at non-proficient penalties). Grants Improved Unarmed Strike + MonkUnarmedStrikeLevel1 |
| Ki Flurry | 1 | While unarmored/unshielded (vanilla MonkNoArmorAndMonkWeaponFeatureUnlock gate): extra unarmed attack, −2 attacks, Wisdom to AC (TTT MonkACBonus recipe) |
| Fist dice | 1 / 8 / 20 | MonkUnarmedStrikeLevel1/4/12 — "a few of the +damage dice features": 1d6 → 1d8 → 2d6 cap at 20th (0.5.1 correction) |
| Fleet Step | 6 | +5 ft movement speed |
| Sundering Blows | 3 | Each unarmed hit stacks Sundered (rank cap 1 + level/4, max 5); each rank = all weapon damage vs the target treats its DR as 2 lower (DamageValue.ReductionPenalty — the COP Divine Fighting Technique mechanism) |
| Casting Carapace | 7 | While charged (a spell cast this window): DR = class level / 4 (DivStep) |
| Feat line | 2–15 | Resonant Strikes (2), Tunnel Fighter (8), Arcane Momentum (4), Vengeful Counterstrike (5), Guarded Momentum (6), Taunting Blows (7), Second Wind (9), Warded Soul (11), Battlefield Scavenger (13), Steadfast Aim (15) — plus Combat Reflexes (2), Combat Expertise (6), Power Attack (7), Endurance (9) so the switch-on feats work. Scavenger/Steadfast Aim are ranged oddities, granted per the "all of them" brief |

Feat grant schedule as shipped (0.5.1 revision): L2 Resonant Strikes; L3
Sundering Blows; L4 Arcane Momentum; L5 Vengeful Counterstrike; L6 Guarded
Momentum + Fleet Step; L7 Taunting Blows + Power Attack (its mechanic requires
the stance) + Casting Carapace; L8 Tunnel Fighter + fist die (1d8); L9 Second
Wind; L11 Warded Soul; L20 fist die (2d6). Removed per user direction: the
ranged feats (Battlefield Scavenger, Steadfast Aim) and all gate-only
prerequisite grants (Combat Reflexes, Combat Expertise, Endurance, and with
them Point-Blank Shot) — archetype-granted features bypass prerequisite
checks anyway.

**Kept vanilla:** Spell Combat + Spell Strike (the delivery vehicle the cascade
hijacks), Arcane Pool and the full MagusArcanaSelection (weapon enchants apply
to the current weapon — bare fists included when the game treats them as such),
cantrips, spell recall, fighter training, True Magus.

**Playtest targets (log prefix [spellfist]):** cascade armed/banked/share-paid
lines on every cast and punch; the touch spellbook count on load; whether Spell
Strike delivers through unarmed attacks (if the engine merges spell+weapon
damage into one bundle, shares never bank — the spell then behaves vanilla,
which the logs will show as "armed" with no "banked" line); arcana weapon
buffs on fists; MonkNoArmorAndMonkWeaponFeatureUnlock activating for a
non-monk; the dissipate warn for standard-action casts.

**Tuning candidates:** share count N (BAB formula, ignores haste/rapid-style
extras); −2 flurry penalty; sunder 2/rank and rank caps; carapace DR curve;
charge window 2 rounds; fist-die levels (1/8/16).

## Magus (continued)

### Faithful port — Spellblade (Paizo, Ultimate Magic) — IMPLEMENTED (v1)

"A spellblade magus can manifest a ghostly blade of force that can be used as
an off-hand weapon."

| Feature | Real level | Details |
|---|---|---|
| Force Athame (Sp) | 2 | Swift action: sacrifice a prepared magus spell of 1st level+ to create a +N force dagger in the off hand (N = sacrificed spell's level, max +5); lasts 1 minute; force damage, dagger stats (1d4, 19–20/×2). **Replaces spellstrike** |
| Pool-Sourced Athame (arcana) | any arcana | Spend 3 arcane pool points instead of a spell → +3 athame (adaptation of "enhancement equal to points spent"; tuning candidate) |
| Spellblade Parry (arcana) | any arcana | First enemy melee attack while the athame is up shatters it: ends the athame, grants deflection AC = sacrificed spell's level (max +5) until end of next turn. (Tabletop immediate action made automatic — ending the athame is the cost) |
| Throw Athame (arcana) | any arcana | Standard action ranged attack with the athame; hit → deals its damage and ends; miss → returns untouched. Throw Athame (Empowered) spends 2 pool for +2d6 force on hit (the tabletop's optional 2-point spend as a separate ability) |

**Engine mappings:** the sacrifice is spontaneous spell conversion (pplus
SpireDefender mechanism — converting a prepared magus slot into the "Force
Athame" cast consumes the slot natively, and Context.SpellLevel carries the
sacrificed level); the athame is five conjured item weapons (force damage via
the item's damage-type override, TemporaryEnhancement1–5 enchants) granted as
off-hand attacks through vanilla AddSecondaryAttacks — the off-hand slot stays
empty so spell combat keeps working (the tabletop's "hand still free" clause;
the either/or restriction is not enforced). The throw is a triggered
RuleAttackWithWeapon with a runtime-spawned athame entity (DarkCodex
ContextActionAttack pattern). The three arcana are registered in
MagusArcanaSelection with a prerequisite on the archetype's Force Athame
feature, so only spellblades see them. Playtest log prefix: [spellblade].

**Playtest targets:** the conversion UI showing "Force Athame" in the magus
spellbook (convert-a-prepared-spell flow); secondary off-hand force attacks
appearing; slot consumption on conversion; throw hit/miss handling; parry
trigger + deflection value; pool costs.

## Bloodrager

### Faithful port — Untouchable Rager (Advanced Class Guide) — IMPLEMENTED (0.5.4)

The bloodrager pair completes: Sanguine Font pours her magic out as healing;
the Untouchable Rager's bloodline refuses magic entirely - hers included.

| Feature | Real level | Details |
|---|---|---|
| Raging Resistance | 4 | No bloodrager spells at all. While bloodraging: SR = 8 + bloodrager level, +1 at 7th/10th/13th/16th (SR 32 at 20th). Cannot be lowered while the rage lasts |
| Resistance Control | 14 | The SR persists while calm - and while calm it can be switched off (default-on activatable toggle) |

**Trades:** spells, blood casting, eschew materials, and bloodline spells —
implemented as a component scan that removes every progression feature whose
`AddSpellbook` grants the bloodrager spellbook (`ArchetypeRemovals.RemoveSpellcasting`;
bloodline bonus-spell features live in the bloodline progressions and have no
spellbook left to touch once the casting kit is gone).

**Wrath adaptations:**

1. WOTR has no "lower your SR" action, so Resistance Control is a default-ON
   activatable: switching it off drops the calm-state ward (mid-rage the blood
   refuses - the maintenance component forces the resistance back on, exactly
   as the tabletop forbids lowering it).
2. The SR lives on two buffs (rage-granted + toggle's); the maintenance
   component guarantees exactly ONE is active at any moment, so the two sources
   can never stack. Maintenance runs on round ticks and on every
   `RuleSpellResistanceCheck` aimed at the rager, so the ward snaps on the
   moment it matters.
3. SR value: vanilla `AddSpellResistance` component (the "Spell Resistance"
   spell mechanism) with a custom rank progression baked to the tabletop table.

### Homebrew — Sanguine Font (user-commissioned; working title "Crimson Heart") — IMPLEMENTED (v1)

An AoE-healing bloodrager whose rage economy powers her support: every ability
spends bloodrage rounds, and the mythic ability that would make rage endless
(LimitlessRage) is prerequisite-blocked for Sanguine Fonts — the resource
tension IS the class.

| Feature | Real level | Details |
|---|---|---|
| Vital Blood | 1 | While bloodraging, 10-ft aura; start of each of her turns, allies below max HP in the aura regain 1d4 + 1/3 level |
| Bloodletting Pulse | 2 | Swift, while bloodraging, expend 1 bloodrage round → all allies within 20 ft (incl. self) regain 1d8 + 1/3 level; once per round (1-round marker buff) |
| Shared Vitality | 5 | The Shared pulse: allies also gain fast healing 2 for 3 rounds (3 at 11th, 5 at 16th) while the Font takes an equal UNREMOVABLE bleed for 3 rounds |
| Kinetic Blade | 6 | Obligatory element pick (Fire/Cold/Electricity/Acid/Force); a toggle granting an off-hand blade on the vanilla KineticBlastEnergyBlade weapon type: 1d6 + Con element damage, +1d6 per two (level−2) kineticist levels beyond 1st |
| Greater Bloodletting Pulse | 8 | Pulse → 30 ft, 2d8 + level + Con; allies healed from below 0 HP are treated as stabilized (the heal itself restores them) |
| Kinetic Blast | 11 | Standard-action ranged touch (vanilla KineticBlastEnergy weapon type; Dex to hit, Con to damage): 1d6 per odd (level−2) kineticist level, chosen element, no infusions — the downside |
| Sanguine Apotheosis | 20 | Aura 40 ft healing 2d8 + Con + Cha + 1/8 CURRENT HP + level; the pulse becomes a FREE action, unlimited uses per round (each still costs a round); once per bloodrage, an ally in the aura about to die is instead left at 1 HP + 10d8 + level heal, costing ALL remaining bloodrage rounds |

**Trades:** all proficiencies (simple weapons + light armor only — vanilla
SimpleWeaponProficiency/LightArmorProficiency granted), Damage Reduction,
Uncanny Dodge + Improved Uncanny Dodge, and the spellbook's top tier (the
archetype's ReplaceSpellbook is a clone of the bloodrager book whose list caps
at 3rd-level spells — the blast replaces "some of the spells you could learn").

**Engine mappings:** per-round effects (aura, fast healing, bleed) use
ITickEachRound (the pplus RagingDrunkStuff pattern — proven on bloodrager
features); all healing via Rulebook.Trigger(new RuleHealDamage(...)); ally
enumeration via Game.Instance.State.Units + IsAlly + DistanceTo (pplus
Nocticula pattern; self included); dice via UnityEngine.Random.Range (the
game's own roller); the pulse's once-per-round guard is a visible 1-round
marker buff, lifted by the apotheosis free-action variant; the death save
watches RuleDealDamage on allies (DidTrigger, HP ≤ 0, not yet dead) and spends
the whole rage pool — dying allies in WOTR are rescued post-hit, which is the
playable reading of "would die". Kinetic powers use the kineticist's own
vanilla weapon types (KineticBlastEnergyBlade / KineticBlastEnergy) with
runtime-scaled riders. Log prefix: [sanguine].

**Playtest targets:** aura ticks on each round while raging (and only then);
pulse resource spend + once-per-round block; shared pulse FH on allies and
unremovable bleed on self (values 2/3/5 by level); blade toggle granting the
off-hand elemental attack and the rider dice; blast touch attack + Con damage;
spellbook clone capping at 3rd-level spells; apotheosis free pulse, 40-ft aura
formula, and the death save (spends ALL rounds, once per rage); LimitlessRage
hidden from Sanguine Fonts in mythic level-up.

**Tuning candidates:** all heal amounts and dice; aura/pulse radii; FH/bleed
tiers (2/3/5) and their level breakpoints (11/16); kineticist-level offset
(−2); blast range (Close); death-save heal (10d8 + level).
ings:** per-round effects (aura, fast healing, bleed) use
ITickEachRound (the pplus RagingDrunkStuff pattern — proven on bloodrager
features); all healing via Rulebook.Trigger(new RuleHealDamage(...)); ally
enumeration via Game.Instance.State.Units + IsAlly + DistanceTo (pplus
Nocticula pattern; self included); dice via UnityEngine.Random.Range (the
game's own roller); the pulse's once-per-round guard is a visible 1-round
marker buff, lifted by the apotheosis free-action variant; the death save
watches RuleDealDamage on allies (DidTrigger, HP ≤ 0, not yet dead) and spends
the whole rage pool — dying allies in WOTR are rescued post-hit, which is the
playable reading of "would die". Kinetic powers use the kineticist's own
vanilla weapon types (KineticBlastEnergyBlade / KineticBlastEnergy) with
runtime-scaled riders. Log prefix: [sanguine].

**Playtest targets:** aura ticks on each round while raging (and only then);
pulse resource spend + once-per-round block; shared pulse FH on allies and
unremovable bleed on self (values 2/3/5 by level); blade toggle granting the
off-hand elemental attack and the rider dice; blast touch attack + Con damage;
spellbook clone capping at 3rd-level spells; apotheosis free pulse, 40-ft aura
formula, and the death save (spends ALL rounds, once per rage); LimitlessRage
hidden from Sanguine Fonts in mythic level-up.

**Tuning candidates:** all heal amounts and dice; aura/pulse radii; FH/bleed
tiers (2/3/5) and their level breakpoints (11/16); kineticist-level offset
(−2); blast range (Close); death-save heal (10d8 + level).

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

## 0.13.0 — Riftstalker rework: the beast strikes by guided command (user feedback)

- The user liked the riftstalker concept but not the execution: "it just
  ends up being a lot of extra damage with a slayer-like ability." The
  mark no longer boosts the stalker's own numbers AT ALL.
- The new shape, per the user's two-part direction: the beast is NOT kept
  (it never came back through the rift), but it can help INVISIBLY, at
  ONE enemy, through a SPECIFIC GUIDED COMMAND - the mark is the tether.
- **Rift Mark (1st):** first hit per round brands the target for one
  minute; ONE creature bears the mark at a time (a new mark strips the
  old - enforced by scanning the unit list). Grants nothing by itself.
- **Guided Command (1st, new ability):** swift action, long range, usable
  ONLY against the marked. The unseen beast strikes: d20 + hunter level
  + Wisdom vs the target's REAL AC (a live RuleCalculateAC query, so
  every AC modifier applies - the "save" is AC, exactly as asked). On a
  hit: 1d6 + half level raw damage (no DR - the mauling is not of this
  world) + shaken 1 round. Natural 20 always hits, natural 1 always
  misses. The damage is the BEAST's - her weapon and gear are irrelevant.
- **Rift Stride (2nd):** unchanged (+10 ft).
- **Unseen Guardian (5th, replaces Riftstudy):** +2 dodge AC against
  attacks from the marked creature (the beast whispers warnings) - the
  exact AC-when-attacked handler shape Step Aside uses.
- **Blood in the Rift (12th, replaces Hunter's Feast):** when a marked
  creature dies by her hand, the beast feeds - for one minute, guided
  commands need no roll (it already knows the taste of the blood).
- Engine notes: the strike's d20/d6 use a plain System.Random (WOTR
  exposes no constructible raw-d20 rule in the reference set; RuleRollD20
  has no proven constructor). The AC query itself IS the game's own rule.
  Damage is applied as raw HP loss (Descriptor.Damage - the proven
  Expedited Synthesis mechanism), which honestly bypasses DR - the rift
  beast's mauling is not physical. All documented in the class header.

## 0.14.0 — Rift Commands: the beast learns (user feedback)

- The user's verdict on Blood in the Rift: replace it with "a new guided
  command she can give on level 4, 8, 12, 16, 20. With 10 choices for
  commands."
- **Rift Commands:** a selection of TEN directives; from 4th level and
  every four levels after (8/12/16/20) the riftstalker teaches the beast
  one command of her choice - five learned over a full career, from a
  list of ten. Every command is a swift action against the MARKED target
  (the tether), resolved with the beast's attack roll (d20 + hunter
  level + Wisdom vs the target's real AC) unless noted, and ALL commands
  (plus the base Guided Command) share one budget: the beast can be
  given only ONE directive per round, tracked by a hidden one-round
  buff. She picks the right tool each round; the toolbox is the build.
- The ten (selection gates in brackets):
  1. Mauling (4) - 2d6 + half level raw damage.
  2. Pinning (4) - entangled 1 round.
  3. Terrifying (4) - Will save or frightened 1 round.
  4. Guarding (4) - no roll: +4 dodge AC vs the marked for 1 round.
  5. Blinding (8) - Fort save or blind 1 round.
  6. Fatiguing (8) - Fort save or fatigued 1 minute.
  7. Staggering (12) - Fort save or staggered 1 round.
  8. Crippling (12) - -2 attack rolls for 1 minute (custom debuff buff).
  9. Rending (16) - two attack rolls, 1d6 + half level each.
  10. Cataclysm (16) - 1d6 per 2 levels raw damage, Fort for half.
- **Removed:** Blood in the Rift and its Bloodfed buff (12th-level
  no-roll window) - replaced by the command picks. Where saves apply,
  DC = 10 + half level + Wisdom.
- Engine notes: one ContextAction class drives all ten (an enum field
  picks the directive - enum fields on game actions are the
  ContextActionCombatManeuver pattern); Guarding reuses the Unseen
  Guardian handler at +4; Crippling adds a small -2-attack handler buff.
  The one-per-round budget is a hidden buff on HER, which also cleanly
  prevents real-time-with-pause command spam.

## 0.15.0 — command menu + damage tuning + the Verminous Hunter (user feedback)

- **The command menu (user ask #1).** Clicking Guided Command now opens
  the game's own click-to-choose submenu (the AbilityVariants component -
  the mechanism vanilla's MasterHunterAbility uses, confirmed compiling
  in CharacterOptionsPlus and TabletopTweaks-Core). Engine honesty: the
  variant list is blueprint-static in this build of the game - a CI probe
  proved the runtime-mutation member (m_Variants) is not exposed and the
  Variants property is a read-only proxy - so the menu lists ALL TEN
  directives and the gating lives in each command's action: a directive
  only fires if she owns its learned feature, checked before the
  once-per-round budget so an empty whisper costs nothing. Command
  abilities are no longer granted as separate action-bar icons; the base
  strike is cast directly through the same ability.
- **Damage tuning (user ask #2).** The base strike now deals 1d4 +
  half level, growing to 1d5 at 3rd and 1d6 at 4th (the die helper is
  level-read at strike time). Mauling is locked behind 12th level. Rending
  and Cataclysm are gone, replaced by two new 16th-tier commands that are
  about control, not damage: Paralyzing (strike + Will save or paralyzed
  1 round - the beast seizes the target bodily through the rift) and
  Scattering (strike; the marked is frightened 1 round on a failed Will
  save and every other enemy within 10 ft of it is shaken 1 round on a
  failed save).
- **Verminous Hunter (user ask #3) - the mod's first TABLETOP hunter
  archetype** (ACG, chosen by the user for a Swarm-that-Walks run):
  - Vermin Companion (1st, alters animal companion): must take a vermin -
    the centipede, the only creepy-crawly companion WOTR ships (companion
    selection swapped for a vermin-only one).
  - Vermin Empathy: WOTR's hunter has no wild empathy at all - nothing to
    alter; documented and skipped.
  - Vermin Focus (1st, REPLACES animal focus): permanent aspects per the
    game's own Forester precedent - one pick at 1st, a second at 8th, a
    third at 15th; each aspect scales at 8th and 15th. Five aspects
    adapted to what the engine can express: Ant (+1/2/3 attack and
    damage), Beetle (+2/4/6 natural armor), Mantis (+2/4/6 on AoOs),
    Scorpion (+2/4/6 on maneuvers), Worm (50% fortification, fixed -
    flattened from the tabletop's 25/50/75 because the engine's component
    takes one flat value). The other ten tabletop aspects ride on
    Climb/Swim/jump/web/skill mechanics the engine barely exercises -
    documented cuts.
  - Swarm Stride (5th, REPLACES woodland stride): adapted to poison and
    disease immunity. WOTR exposes no swarm-detection hook (no IsSwarm
    flag in any reference source), so the damage-immunity half of the
    tabletop ability cannot be gated honestly - the adaptation targets
    the swarm's actual weapons instead, with the impossibility documented.

## 0.16.0 — Construct Crafter command menus + the Sin Eater (user asks)

- **The Construct Crafter's cores and programs now live in command menus**
  (user ask #1, after two rounds of clarification: "I want the cores and
  programs to be in a command menu"). Two bar icons replace the pile of
  per-pick toggles: Core Command and Program Command. Clicking either
  opens the game's variant submenu (the same AbilityVariants mechanism as
  the Riftstalker's Guided Command). Base entry = Basic (markers
  cleared); each menu entry arms/sets the chosen core or program. The
  toggle blueprints are no longer granted by the pick features, so
  nothing accumulates on the bar. Same documented limitation as the
  Riftstalker menu: the list is blueprint-static, so it shows all
  entries and the gating lives in the action (an unlearned entry is an
  empty whisper). Arming is exclusive per axis - the old independent
  toggles allowed several markers at once (GetActiveCore returned the
  first match); the menus clear the axis before setting, which is both
  cleaner and self-documenting.
- **Sin Eater (user ask #2) - the mod's first inquisitor archetype**
  (Ultimate Magic port; the user noted the inquisitor should get a
  tabletop archetype while they think of a homebrew one. Sanctified
  Slayer - the first candidate - already ships with WOTR, so the port is
  Sin Eater instead):
  - Eat Sin (1st, REPLACES domain): her killing blow devours the fallen
    enemy's sins - automatic and free (the CRPG adaptation of the
    tabletop's minute-long ritual): heals 1d8 + level (cap +5), 2d8 at
    5th, 3d8 at 9th, 4d8 at 13th (caps +10/+15/+20). Once per enemy
    (unit-id list in component data). No effect on Int 2 or less.
  - Sin Speaker (6th, REPLACES the 6th-level teamwork feat): speak with
    dead has no WOTR blueprint - adapted from the archetype's own flavor
    ("consuming sins empowers the sin eater, at least for a time"):
    each eaten sin also grants +1 on attack rolls and saves for one
    minute.
  - Skipped, documented, not faked: the 8th-level rider (prevent a corpse
    rising as undead - foreknowledge the engine does not expose) and
    Burden of Sin at 14th (arbitrary harmful-effect transfer - no
    verifiable API). She keeps exploit weakness.
- Coverage: vanilla WOTR inquisitor archetypes include Monster Tactician,
  Sacred Huntsmaster and Sanctified Slayer (the user caught our first
  pick colliding with it); Sin Eater is ours alone.

## 0.17.0 — Doomsayer (homebrew inquisitor)

- **Doomsayer - the mod's first homebrew inquisitor archetype** (user
  concept: an inquisitor focused ENTIRELY on intimidation, flavored
  "mostly the doomsayer," combining the three proposed kit shapes across
  the levels, with HEFTY drawbacks per the user - "definitely no teamwork
  feats at all, for one"; mechanical lean: blend, the user's pick).
- The trades (hefty): the ENTIRE judgment line (1st/8th/16th/17th),
  solo tactics, EVERY bonus teamwork feat, stalwart (11th), exploit
  weakness (14th). She keeps domain, bane, slayer, monster lore, spells
  and the capstone.
- The kit: Pronounce Doom (1st, swift; Wisdom added to Intimidate;
  Intimidate check shakes the target for a minute and marks it CONDEMNED;
  +1 attack vs the condemned, +2 at 9th, +3 at 17th; escalation at 5th -
  shaken targets become frightened, at 12th frightened targets are also
  staggered by terror); Dreadful Certainty (3rd, every weapon hit makes a
  free Intimidate check to shake the victim for a round); Dread Mien
  (8th, 15-ft pressure aura via the game's own area-effect system:
  -2 attacks and saves AGAINST HER for enemies inside (0.17.1), NO save
  and NOT a fear effect - the fearless still feel doom's weight); Death's Echo (11th, when the
  condemned dies enemies within 30 ft save or are shaken a minute);
  Sentence of Ruin (14th, the condemned marker becomes visible and
  universal: -2 attacks, saves and AC vs everyone); Greater Dread Mien
  (16th, aura reaches 30 ft); Final Verdict (17th, the pronouncement
  needs no check - the sentence is simply read - and Death's Echo
  reaches 50 ft).
- Engine honesty: WOTR folds Intimidate into PERSUASION - every check
  the archetype makes is a Persuasion check (intimidation), with Wisdom
  added on top. WOTR's fear ladder tops out at Frightened (no Panicked
  buff), so the 12th-tier terror seizes the victim STAGGERED instead of
  panicked. The shaken/frightened tiers go through the game's own buffs
  and respect its immunity handling; the aura and the Sentence of Ruin
  are deliberately NOT fear effects (no save, no immunity interaction) -
  documented as doom, not fear. Documented edge: with two doomsayers in
  one party, a pronouncement clears the other's condemned marker (same
  policy as the other shared-blueprint designs).

## 0.17.1 — Dread Mien nerf (user ask)

- **Dread Mien/Greater Dread Mien now only debuff rolls made AGAINST the
  doomsayer** (user ask: "make dread mien only debuff attack and saves
  against the owner"). The -2 penalty applies when the enemy inside the
  mien attacks her, and to saves against her effects; against anyone
  else the enemy rolls at full strength. Implementation honesty: the
  attack penalty is a modifier on the attack-bonus calculation scoped to
  her; the save penalty rides HER DC (+2 DC against mien-bound enemies -
  the engine's own situational-save pattern), which is mechanically
  identical to -2 on the save.

## 0.18.0 — Explosion infusion (kineticist class content)

- **Explosion - the tabletop's 5th-level form infusion, previously
  missing** (user scope: expand the existing elements' choices; Extreme
  Range and Kinetic Whip skipped by request). Your kinetic blast
  detonates in a 20-foot radius, 3 burn, usable with every blast you
  possess - 17 new abilities (one per blast), each a full clone of that
  blast's Extended Range sibling so the entire damage wiring (Con
  scaling, overflow, gather power) is inherited, with the range returned
  to the base blast's and a 20-ft radius AoE added on top.
- Gating: the infusion feature (registered into the vanilla
  InfusionSelection, prerequisite kineticist 9) grants only the explosion
  abilities whose base blast the kineticist actually owns. Documented
  edge: a blast learned after the infusion (Expanded Element) appears
  after the next area transition, when facts re-activate and re-sync.
- Research corrections from the user (both verified): metakinesis
  Empower/Maximize/Quicken all ship with the game (the earlier worry was
  wrong), and Burning infusion is also already in (the earlier
  missing-list erred on it).
- **Impale deferred, honestly:** bpcore exposes no line-shape builder
  (radius only), so the only inheritable 30-ft line family is Torrent -
  whose physical-blast damage halving lives in internals we cannot
  audit. Cloning Torrent would ship a redundant ability, not Impale.
  Deferred until a verifiable line surface exists.

## 0.19.0 — Unstable Kineticist (homebrew) + the coverage file

- **Unstable Kineticist - homebrew kineticist archetype** (user concept:
  the random-surge idea without the Worldwound theming; user named it -
  spelling correct as written). Her gate never settled: every kinetic
  blast risks a SURGE (25%; 50% from 16th). d6 surge table: Eruption
  (extra damage to the target, 1d6/3 levels), Chain Arc (enemies within
  10 ft of the target take 1d6/4 levels), Violent Discharge (shaken 1
  round), Rebound (she takes 1d6/4 levels herself), Overcharge (Eruption
  + Discharge), Null Surge (nothing). Rebound Control (8th) turns
  Rebound results into Null Surges. Trades (hefty): GATHER POWER and
  ELEMENTAL OVERFLOW - the stabilizer and the burn-payoff, both vanilla
  features verified by guid before trading.
- Engine honesty: post-cast TYPED energy damage has no verifiable API
  here - surge riders deal RAW damage and the randomness lives in the
  effects table, not an energy-type roll (the game text says "uncontrolled
  force", never "random element"). Surges trigger only on unit-targeted
  blasts (self/ground-targeted forms skip them). Documented in the file
  header and the component.
- **docs/COVERAGE.md created** (user ask: "make a note somewhere you'll
  always check that lists what's in the base game and in mods that add
  archetypes"). It records the vanilla-archetype inventory per class
  (verified rows for hunter/inquisitor/kineticist, to-verify rows
  elsewhere), the mods that add archetypes (HomebrewArchetypes,
  TabletopTweaks main mod, Call of the Wild, ExpandedContent, Kineticist
  Elements Expanded), and the five-step check policy that now runs
  BEFORE any new archetype is designed - this after the user caught two
  collisions (Sanctified Slayer, and the kineticist trio: Kinetic
  Knight, Dark Elementalist and Overwhelming Soul are all vanilla).

## 0.20.0 — Kinetic Chirurgeon (tabletop port) + coverage table filled

- **Kinetic Chirurgeon - faithful port of the Occult Adventures
  kineticist archetype** (user pick of the two remaining unused tabletop
  kineticist archetypes, chosen via the coverage check: vanilla ships
  Blood Kineticist, Dark Elementalist, Elemental Engine, Kinetic Knight,
  Kinetic Sharpshooter, Overwhelming Soul, Psychokineticist; Elemental
  Annihilator remains unimplemented anywhere). The healer kineticist:
  - Kinetic Healer (1st): touch, standard action - 1d6 + Con per 2
    kineticist levels; each use also cures one condition via the mercy
    ladder (3rd: fatigued/shaken; 5th: staggered/entangled; 7th:
    blinded/frightened; 9th: stunned - auto-triaged, the CRPG adaptation
    of "one paladin mercy per use").
  - Metahealer (5th): +1 healing die (2 at 11th, 3 at 17th).
  - Swift Mending (13th): swift-action self-only kinetic healer.
  - Shared Mending (17th): the same use heals target AND chirurgeon.
  - Trades: the InfusionSelection at every level, Infusion
    Specialization, and all three metakinesis features (Empower,
    Maximize, Quicken) - she can never use infusions.
  - Adaptations (documented, not faked): element restriction skipped
    (no aether/wood in WOTR; the healer draws on her, not her element);
    the 9th-level breath-of-life revive and the doubled internal buffer
    skipped (no verifiable APIs - she keeps internal buffer); the burn
    cost waived by design (burn is grantable - see the burn API in
    docs/NOTES.md, verified via the KineticArchetypes mod - but the
    healer ships unlimited-use for reliability); poisons/diseases/curses are not buffs and are not
    in the mercy ladder.
- **docs/COVERAGE.md filled** (user ask): the full vanilla archetype
  inventory from the game's own 202-entry blueprint list, classified per
  class with an explicit unclassified bucket. **Call of the Wild removed
  from the mod list - it is KINGMAKER-ONLY** (user correction). **New
  file docs/NOTES.md** (user ask): the durable engineering-notes file -
  ecosystem facts (incl. Call of the Wild being Kingmaker-only), the
  command-menu recipe, the CI-verified engine-fact list (member drift,
  Persuasion, fear ladder, raw damage, area buffs...), and the clone/CI/
  push recipes.

## 0.21.0 — the burn API applied + combat-log feedback (technique survey)

- **Kinetic Chirurgeon now pays burn** (user ask, enabled by the mod
  survey): each use of kinetic healer - and of swift mending - costs 1
  point of burn, accepted through the real engine API
  (UnitPartKineticist.AcceptBurn; gather power and other burn reducers
  apply engine-side). No burn left, no healing. The tabletop's "the
  target may accept the burn instead" clause is simplified: only she
  pays. The earlier "unlimited-use" adaptation is retired.
- **Explosion infusion: typed burn cost.** The reflection-hunt cost
  setter is replaced with the real member
  (AbilityKineticist.InfusionBurnCost, identified via the
  KineticArchetypes mod's source) - the cost is now set the same way
  the engine itself reads it.
- **Combat-log feedback** (the NineSwords recipe, now our
  MissionWOTR.CombatLog helper): the mod's signature custom moments are
  player-visible in the combat log - kinetic healer mends and mercy
  cures, sin eating, doom pronouncements, unstable surges (all six
  results), and construct core/program directives (set or ignored).
  Best-effort by design: a log failure never breaks the mechanic.
- **Second technique survey** documented in docs/NOTES.md: TTT main
  (BlueprintsCache.Init Priority.First injection, TemporaryContext),
  CharacterOptions+ (variant menus for dual-output abilities), ToyBox
  (level-up/multiclass patching encyclopedia).

## 0.22.0 — Stormcaller (homebrew air/electric kineticist)

- **Stormcaller** (user concept: an air-damage specialist - "there is
  already a fire and water specialist" - with immunity override "as per
  other electric damage sources" and a speed identity: "more casts in
  exchange for less power"). Coverage-checked first: no air/electric
  specialist exists in vanilla (Blood Kineticist, Dark Elementalist,
  Elemental Engine, Kinetic Knight, Kinetic Sharpshooter, Overwhelming
  Soul, Psychokineticist) or mods (Kinetic Archetypes' Cinder Adept owns
  fire; DarkCodex's Elemental Scion is any-element generic).
- **Storm's Swiftness (1st):** +10 ft movement; a SWIFT BLAST for every
  air-family blast she owns (Air, Electric, Blizzard, Charged Water,
  Plasma, Sandstorm, Thunderstorm - 7 free-action clones with the full
  vanilla damage wiring), once per round, all damage dice HALVED; and
  **Ascendant Element (Electricity)** - the vanilla mythic feature's own
  component (the one ThunderingRageBuff also carries): her electricity
  ignores immunity AND resistance, the game's own answer to demon
  electricity immunity.
- **Tailwind (5th):** +10 ft more (20 total). **Lightning Step (8th):**
  swift blink to any creature's side within close range. **Riding the
  Current (12th):** two swift blasts per round. **Eye of the Storm
  (16th):** swift blasts no longer halve their dice; vanilla electricity
  immunity granted.
- **The trades:** Enveloping Winds (the air defense) and the entire
  element-expansion line (Secondary AND Greater Elemental Focus - air is
  all she is).
- Engine honesty: the swift budget is spent only when a bolt LANDS
  (RulePrepareDamage fires on the hit - a missed free bolt costs
  nothing); dice-halving uses the TTT MythicSneakAttack dice-modify
  pattern; infusions do NOT apply to the swift clones (their wiring
  lives in the vanilla blast variant system); the budget is a static
  per-unit registry (CovertMage static-dictionary precedent) read by an
  IAbilityCasterRestriction (the kinarch pattern) and cleared on feature
  loss.

## 0.22.1 — Stormcaller: missed bolts spend the charge

- **User request:** "can you make a missed bolt spend the charge?" Yes.
  A swift blast's round-charge is now spent when the bolt is LOOSED,
  not only when it lands: on a hit via RulePrepareDamage (as before),
  on a miss via a new RuleAttackRoll handler on the tracker. The storm
  does not refund a bolt that breaks on the wind.
- Why it is safe (verified against TTT's decompiled
  ContextActionDealDamage and its RuleAttackRoll handlers): one
  RuleAttackRoll event per attack - critical confirmation is a flag
  INSIDE the roll (IsCriticalConfirmed/AutoCriticalConfirmation), not a
  second event, so a hit cannot double-spend; RuleDealDamage.Reason is
  built FROM attackRoll.Reason (the same chain the hit path already
  uses); Reason.Ability is set for ability attacks (the
  InitiatorSpellCritAutoconfirm proof). The handler re-checks the
  caster and the clone GUIDs, so nothing but her own swift bolts can
  spend the charge.
- Swift blast description and in-code honesty notes updated to match
  ("it lands or breaks on the wind, the storm does not refund it").

## 0.23.0 — The Intercessor (homebrew oracle)

- **The Intercessor** (user brief: "pick an oracle archetype and make
  it"). Coverage-checked first: vanilla oracle archetypes are Seeker,
  Dual-Cursed, Enlightened Philosopher, Possessed and Divine
  Herbalist; the content mods add none (TTT-Base: channel fix +
  alternate capstone; CharacterOptions+: none; DarkCodex: channel
  patches). The vanilla paladin Martyr shares the sacrifice fantasy
  but is stigmata + bardic performances; the live wound-transfer is
  unoccupied. The name collides with nothing ("Martyr" is a paladin
  archetype, "Wound-Bearer" a vanilla item).
- **The Vow (1st, free - the curse is already her price):**
  Intercession (swift, close range, one ally, never herself) applies
  the Mark of the Vow: 25% of the damage the marked ally takes is
  TRANSFERRED to her - the ally healed the share, she taking it as
  DirectDamage (exact conservation; no resistance or immunity applies
  to what crosses the bond; a fallen intercessor carries nothing).
- **Well of Wounds (3rd):** her healing on OTHERS is empowered by her
  own broken body: +25% below 75% HP, +50% below half, +75% below a
  quarter (the TTT OutcomingAdditionalDamageAndHealingModifier
  idiom).
- **Death Refused (7th):** once per rest (a 1-charge rest-restoring
  resource), a killing blow leaves her AT 1 HP instead (the COP
  NineLives exact-heal-to-one death save) plus a 3-round fast
  healing 5 surge. Hit-point death only, documented.
- **The Redress (11th):** transfer 50%, two marks. **Saint of the
  Broken Body (15th):** the refusal charge is shared - a marked
  companion who would be slain stands at 1 HP and the REST OF THE
  BLOW is hers; because the charge is already spent, the wound she
  chose cannot be refused (by construction, not special-casing).
  **The Open Embrace (19th):** every ally within 30 ft carries the
  mark, refreshed each round (the SisterDragonAura tick idiom).
- **The trades (hefty):** ALL FIVE revelations (3/7/11/15/19,
  OracleRevelationSelection removed per level via RemoveAtLevel).
  Mystery, curse, bonus spells and the final revelation remain.
- Engine honesty: the transfer resolves after the marked ally's
  damage lands; the mark-capacity registry is session-static
  (post-reload documented); the 15th's redirected remainder shares
  the one charge by construction; every rule pair is a proven idiom
  (TTT DamageRetribution for the transfer, COP NineLives for the
  death save, TTT OutcomingAdditionalHealing for Well of Wounds,
  in-repo SisterDragonAura for the embrace).
- Docs fix: the 0.22.1 changelog section was lost to a same-file
  edit collision in the 0.22.1 push (the detailed-section bullet
  landed; the section did not). Restored here.
- **WITHDRAWN in 0.24.0** (user correction: "I meant a tabletop
  archetype, not a homebrew one"). The Vow survives as the Sacred Vow
  mythic ability; the rest of the kit is removed. (Postmortem note:
  the same parallel-edit collision that ate the 0.22.1 docs section
  also silently dropped the Intercessor's Configure registration in
  0.23.0 - the file compiled, but the archetype was never actually
  built. A fitting end; and a standing lesson: never parallel-edit
  one file.)

## 0.36.0 — the Spirit-Ridden (shaman homebrew: the possessed vessel)

- **The user's brief:** "someone actually being taken over by
  spirits, granting them different ability sets, almost none of the
  normal shaman stuff remains, no spells nor hexes, no familiar, and
  spirit is replaced with a different version which lets her select
  a spirit that completely inhabits her body, gaining more choices
  over time. The spirits function as a form she can change at will,
  which makes her take the role of a basic class with -2 levels (no
  downside for the first 2 levels), alongside level appropriate
  spirit equipment (the shaman must not be wearing anything for the
  equipment to apply)."
- **The trades:** the whole SPELLBOOK goes via the archetype's
  official `RemoveSpellbook` flag (bpcore exposes it; no Owlcat
  archetype ever shipped without casting, so the field sat unused —
  DarkCodex's helper surfaces it too) — which also kills spirit
  magic for free, since it has nowhere left to live. Every hex
  grant, the spirit animal, and the vanilla spirit selection are
  removed by live-progression scan. Almost none of the shaman
  remains, exactly as ordered.
- **Inhabiting Spirits:** a selection re-granted at 1st, 6th, 12th
  and 18th (OnlyNew — each spirit once). Two answer at 1st — the
  WEAPON SAINT (fighter) and the RED WARLORD (barbarian) — the
  BAREFOOT MASTER (monk) and the CUTTHROAT (rogue) unlock at 6th,
  the GREY HUNTER (ranger) at 12th, the FIRST KNIGHT (paladin) at
  18th.
- **The forms:** each spirit is a hero of a dead age who takes the
  reins AT WILL — a swift action that removes every other spirit's
  form and applies this one (exclusivity by construction). While
  channeled she fights as that spirit's class at shaman level minus
  two — the user's exact rule, effective = level at 1st and 2nd,
  level − 2 from 3rd on — with the class's REAL proficiencies
  (FighterProficiencies, BarbarianProficiencies, etc.), a full-BAB
  top-up where the class has one, and a signature package:
  the Saint's edge (+1 attack and damage per 4 effective), the
  Warlord's unquiet fury (Str/Con +2 → +4 → +6, Will +2, AC −2),
  the Master's dodge and stride (+1 AC per 4, +20 speed), the
  Cutthroat's deep first wound (+1d6 per 3 effective on the first
  hit each round, +2 initiative), the Hunter's eye and pace
  (+1 attack per 4, +10 speed), the Knight's aegis (+1 AC per 4,
  +1 saves per 3, immune to shaken and frightened).
- **Spirit equipment (the user's exact rule):** while a form is
  channeled AND her body is BARE — every equipment slot empty — the
  spirit's regalia manifests: a level-appropriate enhancement
  (1 + (eff−1)/4: +1 at effective 1st–4th, up to +5 at 17th+) to
  attack, damage and AC. Wear anything of your own and the spirit's
  regalia refuses to appear; the check re-runs every tick, so
  unequipping mid-fight lets it surface.
- **Documented scope cuts (v1):** the spirits are
  stat-and-proficiency packages, not full class progressions — feat
  selections, rage-round resources, ki pools, and favored-enemy
  picks are not portable as buff payloads; the signatures are
  hand-rolled equivalents. The Cutthroat's rider uses the
  repo-proven direct-damage idiom rather than the game's
  flat-footed detection (which is not exposed where we can read it).

## 0.35.0 — the Jiuweihu replaces the Speaker (user feedback: too many cuts)

- **The user's call:** "with so many cuts, lets try to figure out a
  different shaman archetype" - and their pick: the Jiuweihu
  (Kitsune Compendium pg. 8, Everyman Gaming), with "the jewel
  bound hex made, if possible to do, or find a suitable
  alternative, otherwise its viable." The Speaker for the Past is
  REMOVED (file, guids, loc); the Jiuweihu takes the shaman
  faithful-port slot. Where the Speaker had to cut half its
  tabletop content (no time mystery, seven of nine spells, no
  Linguistics, no Ancestral Weapon), the Jiuweihu cuts almost
  nothing - both of its trades ride real, in-game content.
- **Kitsune-only gate:** the archetype carries a prerequisite on
  ChangeShapeKitsune - the one racial feature every kitsune has.
  (The Magical Tail feats themselves ship with the game, which is
  how we know the race is fully supported.)
- **Spirit Tails (fully faithful):** Magical Tail as a bonus feat
  at 1st and every two levels - the VANILLA feats MagicalTail1-8,
  granted in order at 1/3/5/7/9/11/13/15. Each grants a
  spell-like ability (vanish, hideous laughter, blur, invisibility,
  heroism, displacement, confusion, dominate person - 2/day,
  Charisma-based, caster level = Hit Dice). The after-eight rider
  ("choose one of your Magical Tail spell-like abilities, +1 use
  per day, never the same one twice") ports on the game's own
  MagicalTail{n}IncreaseResource features as a pick-one selection
  at 17th and 19th - OnlyNew mode IS the never-twice rule.
  TabletopTweaks' Magical Tail fixes apply at runtime for users
  who run it.
- **Star Jewel (the jewel bound hex, the suitable alternative the
  user asked for):** the tabletop hex binds the familiar into a
  grape-sized gem (item form, ioun-stone orbit, hardness) - none of
  which the engine exposes. The port: the spirit animal selection
  is REMOVED (found in the live progression by name) and the
  spirit rests in the star jewel - it cannot be slain, and as a
  bringer of fortune the jiuweihu gains a +1 luck bonus on all
  saving throws. The adaptation is written into the ability text.
- **The one honest deviation:** the tabletop's spirit-magic trade
  could not be made - spirit magic has no blueprint of its own
  (it is embedded in each spirit's components), so there is
  nothing to remove from the progression. The jiuweihu keeps her
  spirit magic; the port is up that one trade, and says so in the
  archetype description.

## 0.34.0 — two tabletop ports: the Hedge Witch and the Speaker for the Past

- **The picks (wiki-verified first):** the game already ships
  Stigmatized Witch, Hagbound, Hex Channeler, Ley Line Guardian,
  Elemental Witch and Witch of the Veil for the witch, and
  Possessed/Spirit Hunter/Spirit Warden/Unsworn/Witch Doctor/
  Wildland/Shadow Shaman/Prophet of Pestilence for the shaman —
  so the Hedge Witch (Ultimate Magic pg. 84) and the Speaker for
  the Past (Advanced Class Guide pg. 111) fill the two
  faithful-port slots without touching anything the game covers.
  Other candidates were researched and rejected with reasons:
  Speaker for the Past's time-mystery half, Crystal Tender
  (ioun-stone sharing needs item-bonus plumbing the engine does
  not expose), Animist (spirit-magic replacement + seven of nine
  spells absent), Deep Shaman (buoyancy/pressure systems absent),
  Name-Keeper (wayfinder bond + daily-swap bonds), True Silvered
  Throne (occult rituals + construct pet), Serendipity (variant
  channeling + die-roll bumps), Visionary (augury/divination
  absent), Draconic Shaman (drake subsystem), Benefactor
  (spellcasting replacement).
- **Hedge Witch (witch, UM 84):** the smallest, sharpest trade in
  the port catalog - the hex at 4th becomes **Spontaneous Healing**
  (the engine's own `SpontaneousSpellConversion`, the cleric's
  mechanism, retargeted to the witch: lose any prepared spell,
  cast the cure spell of that level or lower), and the hex at 8th
  becomes **Empathic Healing** (a standard-action touch that draws
  out all poisons and diseases via the vanilla cure spells while
  the witch takes a 1-round 2d6 empathic pain - the tabletop's
  "suffer the failed save instead" redirect is not exposed to
  Wrath's data layer; the adaptation is documented in the ability
  text). The patron note ("normally a healing theme") is a
  recommendation, not a rule - patron untouched.
- **Speaker for the Past (shaman, ACG 111):** trades the spirit
  familiar (1st) and the entire wandering chain (wandering spirit,
  wandering hex - found in the live progression by name, the
  0.28-era scan idiom) for **Mysteries of the Past** (class
  skills: Knowledge World, Perception, Use Magic Device; plus the
  ancestors' spells as bonus known) and **Revelations of the Past**
  at 4/6/12/14/20: a selection of the ancestors mystery's
  revelations as clones (prerequisites stripped at copy time, the
  two 11th-level ones re-gated on shaman level 11). Her main
  spirit and ordinary hexes are untouched.
- **0.33.1-class bugfix riding along (Chimera):** bpcore's
  `CopyFrom` SHALLOW-COPIES components - the clone's component
  array holds the same instances as the vanilla blueprint - so the
  0.33.0 `EditComponent<AddPet>` on the companion clones was
  silently rewriting the VANILLA companion features too (every
  ranger's dog would have grown off the witch's rank feature -
  rank 0 for anyone but a chimera witch; a runtime bug no compile
  can catch, found reading bpcore's CopyFrom docs this round). The
  clones now copy everything except AddPet and add a FRESH AddPet
  built from the vanilla component's values (read through the
  established reflection helper, which gained a value-type getter).
- **Documented cuts this round:** the time mystery (does not exist
  in the game - half of the Speaker's tabletop content), the
  ancestors spells other than heroism/greater heroism (not in the
  game), Linguistics (no Wrath skill), Ancestral Weapon (no
  blueprint exists), regenerate (not in the game - 7th-level
  conversion slots fall back to heal, which the same-or-lower rule
  allows), and the Speaker's revelation-scaling caveat (clones
  reference the vanilla oracle abilities whose level hooks are
  Owlcat's; anything scaling strictly on oracle levels may
  under-scale for a shaman - the probe script now dumps the
  rank-config field maps so a playtest round can tune it).

## 0.33.0 — the Chimera (witch homebrew: the user's companion design)

- **The brief, corrected by the user:** the parked ranger idea ("the
  Chimera's Diet" - a companion that takes a piece of what it kills)
  moves to the witch, "since witch has familiars but not really animal
  companions to work with. This archetype should remove those familiars,
  as well as 8 levels of hexes, and lower the amount of spells you can
  cast each day by 1 starting from level 6." The user's correction
  settled the carrier: the witch GETS an animal companion that has the
  chimera stuff - she does not become one herself. "For a pure
  spellcasting class it wouldnt make much sense, for something like a
  magus or warpriest it could work though" (noted for a future gish).
- **The trades:** the familiar (WitchFamiliarSelection, 1st) is
  removed; EIGHT of the ten hex levels go (the first two hex grants
  are kept - the new `AddRemovalsExceptFirstN` finds every
  WitchHexSelection grant in the progression at configure time and
  removes all but the first two, so the exact vanilla grant schedule
  is followed rather than assumed); and the spellbook is REPLACED
  (the COP WinterWitch idiom, `SetReplaceSpellbook`) with a copy of
  the witch book whose spells-per-day table is rebuilt with one fewer
  slot at every spell level from class level 6 (floor 0).
  Documented limitation: prestige classes that advance the witch
  spellbook (Mystic Theurge and friends) will not advance the
  Chimera's thinned book.
- **The companion (1st):** the FULL vanilla selection - sixteen
  species, preorder variants included - each a clone whose AddPet
  level-rank is retargeted to a hidden rank feature the archetype
  grants at levels 1-20 (the TTTB Animal Ally idiom): the beast
  grows with the witch's FULL class level, no druid tax. The
  selection also carries the vanilla mount-target and
  companion-archetype features. (The pet-control rebuild of 0.29
  already guarantees the beast is player-controlled.)
- **The Chimera's Diet (the bond, on the witch):** when the
  COMPANION kills a creature of a type it has not yet eaten, it
  takes a piece of the kill - a visible marker buff on the beast
  (its buff bar shows what it has eaten) - and grows a permanent
  trait. Twelve courses, one per creature type: Alien Mind
  (aberration, +2 Will) / Beast's Vigor (animal, +10 speed) / Stone
  Guts (construct, DR 2/adamantine) / Dragon's Eye (dragon, +1
  attack) / Fey Step (fey, +2 Reflex) / Silverhide (lycanthrope, DR
  2/silver) / Monster's Hide (magical beast, +2 natural AC) /
  Hunter's Instinct (monstrous humanoid, +2 initiative) /
  Hellhound's Blood (outsider, fire resist 10) / Rooted Flesh
  (plant, +2 Fortitude) / Grave's Gift (undead, +1 all saves) /
  Swarm-Joints (vermin, immune to entangled). The bond's tick
  re-applies any trait a reload may drop - the markers are the
  persisted record.
- **Documented cuts:** humanoids leave no piece worth taking (no
  HumanoidType fact exists to detect them - and the chimera would
  agree); only the companion's kills feed the diet - the witch's
  own kills are hers, not the beast's.
- **Build note (raw DLLs):** the repo compiles against the RAW game
  DLLs - no publicizer - so Owlcat's serialized fields
  (BlueprintSpellbook.m_SpellsPerDay, BlueprintSpellsTable.Levels,
  SpellsLevelEntry.Count, AddPet.m_LevelRank) are compile-time
  invisible (CS1061). The table read/rebuild and the clone's
  level-rank retarget go through configure-time reflection
  (ChimeraPrivate), the repo's established workaround
  (ConstructCrafter's SetBrain, ElementalObsessor's cache walk).
  Alternative considered and deferred: wiring an AssemblyPublicizer
  into the csproj (the COP/TTTB approach) would allow direct field
  access, but changes the build chain - a decision left documented
  rather than taken mid-feature. The probe script's target list was
  extended to dump these types' real field lists.

## 0.32.0 — the Anatomist rebalance (user feedback)

- **Weak Point now scales** (user: "making weak point scale,
  reducing from 16 down to 10 at level 18. (as currently getting 5
  seams is impossible if you can hit the enemy without a natural
  d20 at all)"): the qualifying natural roll is 16 at 1st level,
  falling by 1 every three levels, to 10 from 18th (Math.Max(10,
  16 - level/3)). The user's design insight, honored in the docs: a
  FIXED 16 makes raising her attack a con - the better she hits,
  the rarer qualifying misses become. The falling threshold keeps
  study in pace with aim.
- **Risky Maneuver added (4th, the user's design):** "Give yourself
  -6 ac to find an opening in your opponents defenses, giving them
  a seam." A swift action: -6 AC for 1 round, and her next attack
  that round applies a seam to its target REGARDLESS of the roll -
  hit or miss, no threshold. The guaranteed answer to the
  attack-investment con: the dice can no longer starve the study.
- **Spell resistance lowered** (user: "4 + rogue level seems
  fine"): 11 + level → 4 + level (WithLinearProgression(1, 4)).
- **Learn the Seams removed** (user: "just be removed and replaced
  with something else entirely") → **Read the Tell (12th)**: a
  defensive study - against enemies carrying her seams she reads
  the telegraphs, a +2 dodge bonus to AC against each of them.
- **Vital Reading removed** (user): 20th carries no anatomist
  feature; the rogue's Master Strike remains the capstone.

## 0.31.0 — the Steel Rain rebalance, and the Anatomist (rogue homebrews)

- **Steel Rain rebalance (user):** Ricochet REMOVED (the glance was
  doing too much for a 4th-level feature); Flick of the Wrist moved
  from 8th to 6th; and a new downside, per the user's ask:
  **One Art** (1st) — a visible flaw feature, -2 on attack rolls
  with any weapon that is NOT a thrown weapon (melee blades and
  bows alike). Her hands only know the throw.
- **The Anatomist** — the user's design: "a 3rd rogue archetype,
  focused on finding the weak point of an enemy."
  - Weak Point (1st, replaces trapfinding): a miss that needed a
    natural 16+ to hit teaches a SEAM — a permanent, stacking +1
    attack bonus against that enemy (the needed roll read straight
    off the event: TargetAC − AttackBonus, the dcx PanacheDodge
    formula; the stacks ride a ranked per-enemy mark, the Wildbond
    idiom). Self-limiting by design: the stacks lower the needed
    roll until misses stop qualifying.
  - Student of Defenses (8th): attacking a spellcaster grants
    spell resistance (11 + rogue level, the vanilla
    AddSpellResistance component with a WithLinearProgression(1, 11)
    class-level rank; casters detected via Spellbooks) for one
    round, renewed by every attack on a caster — and damaging an
    enemy with DR grants +1 per five rogue levels, not stackable
    (DR read from the vanilla UnitPartDamageReduction chunks, the
    dcx idiom).
  - Learn the Seams (12th, ours): every seam also adds +1 damage
    against that enemy.
  - Perfect Strike (16th, ours): once per round, a hit against an
    enemy with 5+ seams is an automatic critical — flags set
    pre-resolution and only when the roll already shows a hit (the
    PanacheDodge roll-read), so a strike is never wasted on a miss.
  - Vital Reading (20th, ours): the seams are shared — allies gain
    her Weak Point attack bonus against marked enemies (a
    target-side rider on the mark buff, the COP
    SignatureStealthSurprise buff-rider shape).
  - Trades: trapfinding + danger sense (every rank) — the same
    skill-side price as the Steel Rain, making the two alternatives
    rather than companions (each stacks with the Scout).

## 0.30.0 — the Steel Rain (rogue homebrew: the throwing-weapon rogue)

- **The Steel Rain** — the user's design: "a rogue that focuses on
  throwing weapons. Keep it fairly simple." Deliberately lean: four
  features, clean trades.
  - Thrown-weapon detection: in WOTR, thrown weapons are RANGED
    weapon types (javelins, throwing axes, darts). A whitelist of
    vanilla thrown candidates, filtered AT CONFIGURE TIME to those
    whose AttackType is actually Ranged in this game (a dagger
    blueprint is melee here — stabs never count), the surviving set
    logged. Documented edge: modded thrown types unrecognized.
  - Quick Hands (1st, replaces trapfinding): +1 on attack rolls
    with thrown weapons, +2 at 8th, +3 at 16th (the GuideFocusBonus
    temporary-modifier pattern).
  - Ricochet (4th): once per round, a thrown HIT glances to the
    nearest other enemy within 10 feet for 1d6 per four rogue
    levels — a triggered RuleDealDamage, no second roll (simple by
    design).
  - Flick of the Wrist (8th): adds her Dexterity modifier to thrown
    damage, on top of the Strength the throw already carries (a
    design choice, documented).
  - Catch! (12th): once per round, a thrown KILL earns an immediate
    free throw at the nearest living enemy within 30 feet — a
    phantom RuleAttackRoll with the same weapon (the elk idiom),
    1d6 + Dex on a hit (the weapon's own dice not re-read — simple
    by design). The once-per-round gate is set BEFORE the free
    throw, so free-throw kills cannot chain.
  - The trades: trapfinding (1st) and danger sense (every rank,
    3rd–18th). Sneak attack, evasion, uncanny dodge, talents,
    debilitating injuries, master strike untouched.

## 0.29.0 — the Scout (rogue tabletop port), and the companion-control correction

- **The correction (user):** "animal companions CAN be controlled by
  the player unlike summons" — the Wildbond was built on the wrong
  assumption, and two things change:
  - **Mark Prey is now a commanded ability ON THE HOUND** — an
    active, targeted swift action on the dog itself (select the dog,
    mark the prey), replacing the automatic latest-struck-enemy
    designation. The dog keeps its prey until commanded otherwise;
    the ranger-side bonuses (the dog runs when she strikes the prey;
    prone prey = her attack/damage bonus) are unchanged.
  - The triceratops's second charge is RE-DOCUMENTED: companions are
    player-driven, so Break the Bulwark grants the momentum (+10
    speed, +2d6 next hit) and the player orders the charge directly.
- **The Scout (tabletop port — APG pg. 134)** — the rogue coverage
  check: vanilla WOTR ships EIGHT rogue archetypes (Eldritch
  Scoundrel, Knife Master, Thug, Master of All, Rowdy, Sylvan
  Trickster, Underground Chemist, Dark Lurker); TTT/COP/DarkCodex
  add none; the Scout was unclaimed.
  - Scout's Charge (4th, replaces uncanny dodge): a charge forces
    the target flat-footed (evt.ForceFlatFooted on the attack
    roll - the engine's own flag). Foes with uncanny dodge are
    immune (the tabletop's clause, checked against the vanilla
    feature).
  - Skirmisher (8th, replaces improved uncanny dodge): after
    moving more than 10 feet in a round, her first attack forces
    the target flat-footed (evt.ForceFlatFooted on the attack
    roll, the compile-proven flag). Position captured on the round
    tick (the Wildbond mastodon-momentum idiom).
  - DOCUMENTED STRENGTHENING: the APG text is sneak-only, but this
    game build exposes no settable sneak-only flag
    (RuleAttackRoll.IsSneakAttack is read-only here - CS0200; other
    mods' reference builds differ). Both abilities force full
    flat-footedness: sneak damage AND denied Dex to AC. Slightly
    stronger than the tabletop; documented rather than faked.
  - Documented adaptations: movement is NET displacement from the
    round-tick position (no distance-traveled accumulator in the
    engine); "first attack of the turn" resets on the round tick.
  - The rest of the rogue kit (sneak attack, trapfinding, danger
    sense, debilitating injuries, talents, master strike) is
    untouched.

## 0.28.0 — the Wildbond (ranger homebrew: the user's species-bond design)

- **The Wildbond** — the user's design: "granting specific buffs to
  its animal companion (which it gets at level 1) based on what the
  animal companion is, in exchange for favored enemy, terrain,
  evasion, and a significant nerf to spellcasting."
  - The companion arrives at 1st (hunter's bond subsumed); the
    species bond begins at 4th and improves at 8/12/16/20 (the
    user's wolf schedule generalized to every species).
  - The trades: favored enemy (all five ranks, AddRemovalsAtAll
    Levels), favored terrain (all four ranks), evasion — and the
    significant nerf: spellcasting REMOVED at 4th and RESTORED at
    12th (the new RemoveSpellcastingDelayed; the Skirmisher removes,
    the Wildbond delays).
  - The user's ten bonds, implemented: Dire Bear (Mauling Bond —
    mauled marks, a damage ward for the ranger, bear follow-up
    attacks via ForceAttackOfOpportunity), Dire Boar (Primal Fury —
    frenzy below half, scaling damage/fear immunity/kill-heals; temp
    HP adapted to a second-wind heal, documented), Dog (Heel & Hound
    — prey marks, speed on the ranger's strike, prone-prey bonuses),
    Elk (Stampede — a real phantom RuleAttackRoll at -8 closing to 0
    by tier 5), Leopard (Ambush Bond — once-per-round marks on
    vulnerable enemies, scaling precision; the concealment clause
    cut, +2 replacement documented), Mastodon (Siege Beast — ranked
    momentum, devastating AoE impact + slow), Smilodon (Predator's
    Flurry — ranked bloodshed stacks, damage per stack, extra attack
    at six stacks from tier 3), Velociraptor (Rending Relay — rend
    stacks the ranger's attack consumes for burst + armor shred),
    Wolf (Pack Howl — the user's exact five-tier schedule, ending in
    wolf-and-ranger retaliation), Triceratops (Iron Charge — bulwark
    after charges, live-scaled AC, ally shelter, a Break-the-Bulwark
    swift command; the forced re-charge adapted to momentum,
    documented).
  - The user asked for inventions for the uncovered pets: WOTR ships
    thirteen companion species — Centipede, Horse, and Monitor were
    outside the design. Ours: Toxic Symbiosis (centipede venom the
    ranger's strikes deepen, -1 saves per rank), Saddleborn (the
    horse bond: mounted speed + scaling charge damage), Serpent's
    Vigor (the monitor regenerates while its venom lingers). Any
    other companion (modded) falls back to Wild Vigor (+tier
    attack/AC).
  - Architecture: the bond rides the ranger (species detection from
    the ranger's own companion facts, including preorder variants)
    and the pet carries a stamped rider buff; all marks are ranked
    world buffs, so stacks persist through saves. Documented edges:
    the first pet only; momentum's moved/attacked flags are
    runtime-only (a load loses at most one tick).

## 0.27.0 — the ranger: the Guide, and the whimsy tier (user feedback)

- **Aura of Whimsy now scales** (user: "5 dr is too much for level 8,
  have it start at 2 and go up every 3 levels until it reaches 5 and
  stops"): allies' DR is 2 at 8th level, 3 at 11th, 4 at 14th, and 5
  at 17th, where it stops. Four tier buffs; the aura applies the one
  matching her level and swaps out a retired tier on the next tick
  after she levels.
- **The Guide (tabletop port — APG pg. 125)** — the ranger coverage
  check, CORRECTED: vanilla WOTR ships SEVEN ranger archetypes
  (Flamewarden, Freebooter, Stormwalker, Demonslayer, Espionage
  Expert, Nomad, Sable Company Marine — the user caught an earlier
  four-name count, a grep that only matched names containing
  "ranger"). TTT/COP/DarkCodex add none. The Guide was unclaimed.
  - Ranger's Focus (1st, replaces favored enemy AT ALL FIVE RANKS -
    the new AddRemovalsAtAllLevels helper): swift-action mark, +2
    attack/damage vs the focus, +2 more every five levels; 1/day,
    +1 per three levels after 1st; a new mark clears the old one.
  - Terrain Bond (4th, replaces hunter's bond): +2 initiative,
    Perception, Stealth, Lore (Nature) for the party within 30 ft.
    Documented cuts: the favored-terrain condition (no inspectable
    current-terrain API) and leave-no-trail (no tracking system).
  - Ranger's Luck (9th, replaces evasion): swift action - the next
    miss is rerolled (RuleRollD20.Reroll, the Azata FavorableMagic
    idiom); only misses are rerolled, so take-best and take-second
    are indistinguishable - documented. Cut: the defensive
    enemy-reroll mode (no interrupt-reaction API).
  - Improved Ranger's Luck (16th, replaces improved evasion): the
    reroll carries +4 luck.
  - Inspired Moment (11th, replaces quarry + improved quarry): free
    action, 1 round: +10 speed, +4 AC, +4 attacks, auto-confirm all
    critical threats (AutoCriticalConfirmation, the TTT
    CritAutoconfirmAgainstClass idiom). Cuts: the extra move/swift
    action and the skill/ability-check bonus (no engine support).
  - Master Hunter (20th) remains.

## 0.26.0 — Fortune's Fool rebalance (user feedback)

- **Aura of Whimsy is tamed** (user: "whimsy is too strong, make it
  give dr instead"): the scatter band (attacks against allies
  scattering on a natural 14-17 - a blanket +20% miss chance) is
  REPLACED by DR 5/- for allies within 10 ft (AddDamageResistance
  Physical with no bypass: nothing pierces it but damage in excess
  of it).
- **The Wandering Star is once per ally until her next turn** (user:
  "wandering star should be once per ally until the fortunes fool
  next turn/action"): when an ally's star surge fires, the star
  buff SPENDS itself (removed after the damage lands) and returns
  only with her next round tick (the aura's GetBuff-null refresh).
  Documented edge: if she falls, a spent star stays spent.
- **Smite and channel are gone** (user: "also get rid of channel and
  smite for an increased fear save chance (not immunity unlike
  normal paladin)"): SmiteEvilFeature and ChannelEnergyPaladin
  Feature are removed, and Fate's Wager gains the fool's answer to
  fear - a luck bonus on saves against fear equal to her Charisma
  modifier (minimum +2), descriptor-gated (SpellDescriptor.Fear),
  NOT the paladin's aura-of-courage immunity (the SisterLoyalty
  Saves pattern). Terror can still find her; it just blinks first.
- **Her weapon does chaotic damage** (user: "have your weapon do
  chaotic damage"): the vanilla AddOutgoingPhysicalDamageProperty
  component with DamageAlignment.Chaotic - the very mechanism the
  creature subtypes use (TTT's alignment-subtype fix patches the
  same component) - her physical damage counts as chaotic-aligned.
- Divine grace, spells and the divine bond remain her only
  paladin-ordered privileges.

## 0.25.0 — the paladins, and the Apocryphal rename

- **The Blood-Scribed is renamed The Apocryphal** (user: "I dont
  like the flavor of the blood-scribed (generally you make many
  things blood-something, its a bit strange)"). Mechanics,
  blueprint guids and the generated per-spell guids are all
  unchanged - only names and prose moved (the MD5 seed keeps its
  old prefix on purpose so existing characters keep their stolen
  pages). Her scripture is not canon; every page is paid for in
  years of her own life, not blood.
- **Shining Knight (tabletop port, APG pg. 117)** - the user's
  tabletop pick. Skilled Rider (3rd, replaces Divine Health): every
  pet she owns carries her Divine Grace (tick-managed grant of the
  vanilla feature; the Ride-skill clause is a documented cut - no
  Ride skill exists). Divine Bond (5th): the weapon option is
  removed, the vanilla mount selection granted as the fixed pick.
  Knight's Charge (11th, replaces Aura of Justice): a mounted
  weapon-charge hit on her smite target forces a Will save (DC 10 +
  half level + Cha) or panics for half level rounds (evt.IsCharge
  charge detection; the RuleSavingThrow trigger; the vanilla Eyebite
  panic buff; the no-provoke clause is a documented engine cut).
  Coverage: vanilla WOTR has Divine Guardian, Divine Hunter, Divine
  Scion, Hospitaler, Martyr, Stonelord, Tortured Crusader, Warrior
  of the Holy Light; the mods add none; Shining Knight was
  unclaimed - and deliberately contrasts the chaos homebrew.
- **Fortune's Fool (homebrew)** - the user's design: "a paladin
  focused on chaos? (more chance effects, higher miss chance with
  higher damage, etc) must be of chaotic alignment instead of the
  usual paladin alignment requirements, and doesnt get lay on
  hands, different support auras."
  - The alignment gate: PrerequisiteAlignment of the three chaotic
    alignments with ArchetypeAlignment = true - Owlcat's own rule
    (PF-485644): "the alignment restriction for an archetype always
    replaces the class's restriction." Only the chaotic may take
    her road.
  - Fate's Wager (1st): pure real dice, no hidden RNG - on a
    natural d20 of 1-4 her weapon attack scatters (forced miss even
    if modifiers would hit; the TricksterParry AutoMiss idiom); on
    a natural 17+ the dice surge (a DirectDamage entry of 1d6 per
    two paladin levels added via evt.Add, the
    AddAdditionalWeaponDamageOnHit idiom). The surge stacks with
    crits - chaos, documented.
  - The auras (all five vanilla ones traded, "different support
    auras", each a 10-ft tick-refreshed luck aura): Open Road (3rd,
    +1 luck attacks), Whimsy (8th, attacks against allies scatter
    on natural 14-17 - a true +20% miss band), the Wandering Star
    (11th, allies surge 2d6 on natural 17+), Fortune's Favor (14th,
    +2 luck saves), the Laughing Fool (17th - her companions carry
    Fate's Wager itself; she already has it, and the wager never
    stacks with itself).
  - The trades: Lay on Hands (the user's requirement) with the
    entire Mercy selection (mercies improve a pool she does not
    have), plus all five auras. Smite, grace, channel, spells and
    the bond remain.
  - Honest edges: the 1-4 band overlaps the natural-1 auto-miss
    (effective extra misses = the 2-4 band, 15%); the
    fallen-paladin logic was written for lawful good - what happens
    to a fool who abandons chaos is unverified, documented.

## 0.24.0 — the oracle correction: Planar Oracle (tabletop port), the Blood-Scribed (homebrew), Sacred Vow (mythic)

- **The Intercessor is withdrawn** (see the note above); The Vow
  survives as **Sacred Vow**, a mythic ability only oracles can take
  (prerequisite: one oracle level; TTT's Abundant*-style
  prerequisite-gated mythic abilities prove the selection respects
  it). Intercession marks one ally: half their damage transfers to
  her (DirectDamage, exact conservation, the TTT DamageRetribution
  rule pair); mythic rank 4 widens it to two marks (the
  in-repo Progression.MythicLevel read).
- **Planar Oracle (tabletop port, Ultimate Magic pg. 59)** - the
  user's actual ask. 1st: choose a plane (Heaven/electricity,
  Hell/fire, the Abyss/cold, the Maelstrom/acid - our documented
  mapping of the tabletop's GM-discretionary plane-energy
  association). 3rd: ER 10 (replaces the 3rd revelation); 11th: ER
  20; 20th: apotheosis - immunity + DR 10/magic (replaces the final
  revelation, attempted via the no-op trick against the generic and
  all nine per-mystery finals). Bonus spells at 2nd-18th: the
  tabletop's nine (endure elements ... gate) exist NONE in WOTR -
  every one substituted with a native-level-matched pilgrim's spell
  (protection from evil, see invisibility, protection from energy,
  freedom of movement, dismissal, chain lightning, banishment, mind
  blank, elemental swarm), granted as oracle spells known. The
  tabletop's mystery-bonus-spell TRADE is structurally
  uninterceptable in our build (the mystery's own grant chain; the
  vanilla Enlightened Philosopher swaps the whole mystery selection
  to do it) - so the planar spells are additive and the 7th-level
  revelation is ALSO removed as compensation. Every deviation
  documented in the code header.
- **The Blood-Scribed (homebrew)** - the user's design: "an oracle
  focused on more offensive casting (gaining a spell from the wizard
  spell book every level) in exchange for doing damage to
  themselves." At EVERY oracle level (1-20) she copies one wizard
  spell of a level she can cast (options read live from the vanilla
  WizardSpellList; each gated by AddPrerequisiteClassSpellLevel on
  the oracle's own casting) into her spells known - and each stolen
  page is a permanent scar: −2 maximum hit points, untyped and
  stacking, riding the spell feature itself (the Solipsist
  Stats.HitPoints precedent as an AddStatBonus). Twenty spells by
  20th is forty hit points of book. Trades ALL FIVE revelations.
  The "strengthening their curses" alternative price was declined,
  documented: the theft is hers, not her god's. Generated option
  blueprints carry deterministic MD5-guids (stable across saves).

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

### Homebrew — Riftstalker Hunter — IMPLEMENTED (0.12.0, REWORKED 0.13.0/0.14.0)

The Worldwound loner whose beast never came back through the rift - and
never left. The mark is the tether; the commands are the leash.

| Level | Riftstalker grants | Hunter normally grants |
|---|---|---|
| 1 | Rift Mark + Guided Command | Animal Companion (removed) |
| 2 | Rift Stride | Hunter Tactics (removed) |
| 4, 8, 12, 16, 20 | Rift Command (one of ten, each pick) | teamwork feats (removed) |
| 5 | Unseen Guardian | — |
| 12 | (command pick) | One with the Wild + Raise Companion (removed) |

- **Rift Mark (1st):** first hit per round brands the target for one
  minute; single active mark (new marks strip old ones). The tether.
- **Guided Command (1st):** swift action, marked-only - and the MENU:
  clicking it opens the game's variant submenu of every directive the
  beast has learned. The base strike: d20 + hunter level + Wisdom vs the
  target's real AC. Hit = 1d4 + half level raw damage (no DR; the die
  grows to 1d5 at 3rd, 1d6 at 4th) and shaken 1 round.
- **Rift Stride (2nd):** +10 ft land speed.
- **Unseen Guardian (5th):** +2 dodge AC against the marked's attacks.
- **Rift Commands (4/8/12/16/20):** five picks from ten directives -
  Pinning, Terrifying, Guarding (4th tier); Blinding, Fatiguing (8th);
  Mauling (12th, locked there by tuning), Staggering, Crippling (12th);
  Paralyzing, Scattering (16th). One directive per round, shared with
  Guided Command (hidden budget buff). All ten are listed in the Guided
  Command menu; only learned ones fire.
- Design note: the base command is the damage floor; every learned
  command trades raw damage for control, defense, or escalation. The
  once-per-round budget keeps real-time and turn-based honest.

### Faithful port — Verminous Hunter (ACG) — IMPLEMENTED (0.15.0)

The swarm-walker's hunter (the user's pick for a Swarm-that-Walks run):
vermin instead of beasts, the hive instead of the pack.

| Level | Verminous grants | Hunter normally grants |
|---|---|---|
| 1 | Vermin Companion (centipede selection) | Animal Companion (altered) |
| 1 | Vermin Focus (aspect pick) | Animal Focus (removed) |
| 5 | Swarm Stride | Woodland Stride (removed) |
| 8, 15 | additional Vermin Focus picks | — |

- **Vermin Companion:** the hunter's companion selection is swapped for a
  vermin-only one - the giant centipede, the only crawling companion WOTR
  ships. (No spider/scorpion/wasp companions exist in the game to offer.)
- **Vermin Focus:** permanent aspects (the Forester precedent), 1st/8th/
  15th picks, each scaling at 8th and 15th: Ant (+1/2/3 attack and
  damage), Beetle (+2/4/6 natural armor), Mantis (+2/4/6 on attacks of
  opportunity), Scorpion (+2/4/6 on maneuver rolls), Worm (50%
  fortification, fixed value). Ten tabletop aspects cut for engine
  reasons (Climb/Swim/jump/web checks and skill-stat bonuses the engine
  barely exercises) - documented above in the class header.
- **Swarm Stride:** poison and disease immunity - the honest adaptation:
  no swarm-detection hook exists in the engine, so the "no damage from
  swarms" half cannot be gated; the immunity targets the swarm's actual
  weapons instead.
- **Vermin Empathy:** skipped - WOTR's hunter never had wild empathy, so
  there is nothing to alter.

## Inquisitor

### Faithful port — Sin Eater (Ultimate Magic) — IMPLEMENTED (0.16.0)

The inquisitor who does not stop at the kill: she devours the sins of the
fallen, and grows briefly stronger on what she swallows.

| Level | Sin Eater grants | Inquisitor normally grants |
|---|---|---|
| 1 | Eat Sin | Domain (removed) |
| 6 | Sin Speaker | bonus teamwork feat (removed, 6th slot only) |
| 14 | — (Burden of Sin not portable) | Exploit Weakness (kept) |

- **Eat Sin (1st):** on her killing blow, automatic and free: heals
  1d8 + level (cap +5; 2d8/+10 at 5th, 3d8/+15 at 9th, 4d8/+20 at
  13th). Once per enemy; Int 3+ victims only.
- **Sin Speaker (6th):** each eaten sin also empowers her - +1
  circumstance on attack rolls and saves for one minute.
- **Not portable, documented:** the 8th-level negative-level rider
  (undead-rise foreknowledge) and Burden of Sin (14th, buff transfer) -
  no engine hooks; exploit weakness stays with her.

### Original homebrew — Doomsayer — IMPLEMENTED (0.17.0)

The inquisitor who does not argue. She reads the sentence, and the world
agrees with her. Three kit shapes in one class, per the user's direction:
the pronouncement (per-target ritual hunt), the pressure aura (dread
presence), and the escalating doom ladder.

| Level | Doomsayer grants | Inquisitor normally grants |
|---|---|---|
| 1 | Pronounce Doom (Wis to Intimidate; condemn + shake; escalation later) | Judgments (removed, entire line) |
| 3 | Dreadful Certainty (on-hit Intimidate) | Solo tactics + teamwork feats (all removed) |
| 8 | Dread Mien (15-ft aura: -2 atk/saves against her, no save, not fear) | Second judgment (removed) |
| 11 | Death's Echo (condemned's death shakes 30 ft) | Stalwart (removed) |
| 14 | Sentence of Ruin (condemned: -2 atk/saves/AC vs everyone) | Exploit Weakness (removed) |
| 16 | Greater Dread Mien (aura 30 ft) | Third judgment (removed) |
| 17 | Final Verdict (no-check pronouncement; echo 50 ft) | True judgment (removed) |

- **Pronounce Doom** escalation: 5th - shaken targets become frightened
  (3 rounds); 12th - frightened targets are also staggered by terror
  (1 round). Condemned attack bonus +1/+2/+3 at 1st/9th/17th.
- **Dreadful Certainty:** on-hit shaken 1 round (2 rounds at 10th).
- **Skill adaptation:** WOTR's Persuasion is the tabletop's Intimidate +
  Diplomacy - all the archetype's checks use Persuasion, Wisdom added.
- **Design notes:** the aura and Sentence of Ruin are not fear effects
  (they work on the fear-immune - the weight of doom, not fear); the
  pronouncement and certainty tiers ARE fear effects and respect the
  game's immunity handling. WOTR has no Panicked buff - the terror tier
  staggers instead. Hefty-trades philosophy per the user: the archetype
  gives up the class's signature resource (judgments), its teamwork
  identity, its defensive payoff and its crit capstone for one
  discipline perfected.

## Kineticist

### Class content — Explosion infusion — IMPLEMENTED (0.18.0)

Vanilla coverage notes: the game ships all eleven tabletop composite
blasts for the four base elements, and all three metakinesis tiers
(Empower, Maximize, Quicken) plus their mythic Master upgrades. The
tabletop infusions still missing after this release: Extreme Range and
Kinetic Whip (skipped by user request), Impale (deferred - no verifiable
line-shape surface), Draining and Disintegrating (not yet attempted).

- **Explosion (5th-level form infusion, learned via infusion selection
  at kineticist 9):** 20-ft radius, 3 burn, every blast you own. One
  ability per blast (17 total), cloned from each blast's Extended Range
  sibling (full damage wiring inherited) with the AoE added and the
  range restored to the base blast's. Burn is set by a reflection hunt
  for the infusion-cost member (logged at load; if the member is not
  found the sibling's cost remains and the log says so - report if the
  in-game cost reads 1 instead of 3).

### Original homebrew — Unstable Kineticist — IMPLEMENTED (0.19.0)

| Level | Unstable Kineticist grants | Kineticist normally grants |
|---|---|---|
| 1 | Unstable Blast (25% surge, d6 table) | Gather Power (removed) |
| 8 | Rebound Control (Rebound → Null Surge) | — |
| 16 | Critical Mass (surge 50%) | Elemental Overflow (removed) |

- Surge table: 1 Eruption (1d6/3 lvls extra), 2 Chain Arc (10 ft, 1d6/4
  lvls), 3 Violent Discharge (shaken 1 rd), 4 Rebound (self, 1d6/4
  lvls), 5 Overcharge (both), 6 Null Surge (nothing).
- Coverage: vanilla ships Kinetic Knight, Dark Elementalist, Overwhelming
  Soul - none collide. Checked per docs/COVERAGE.md before designing.

### Faithful port — Kinetic Chirurgeon (Occult Adventures) — IMPLEMENTED (0.20.0)

| Level | Kinetic Chirurgeon grants | Kineticist normally grants |
|---|---|---|
| 1 | Kinetic Healer (1d6+Con per 2 lvls; mercy cure) | Infusion selection (removed, all levels) |
| 5 | Metahealer (+1 die) | Infusion Specialization (removed) |
| 13 | Swift Mending (swift self-heal) | — |
| 17 | Shared Mending (heal target + self) | — |

- Trades also remove metakinesis Empower/Maximize/Quicken.
- **0.21.0:** every use of kinetic healer / swift mending costs 1 burn
  (AcceptBurn; gather power applies; no burn left = no healing).
- Mercy ladder: 3rd fatigued/shaken; 5th staggered/entangled; 7th
  blinded/frightened; 9th stunned - one cure per heal, auto-triaged
  worst-first.
- Coverage: the only other unused tabletop kineticist archetype is
  Elemental Annihilator. Checked per docs/COVERAGE.md + docs/NOTES.md.

### Original homebrew — Stormcaller — IMPLEMENTED (0.22.0)

| Level | Stormcaller grants | Kineticist normally grants |
|---|---|---|
| 1 | Storm's Swiftness (+10 ft; swift blasts 1/round at half dice; Ascendant Electricity) | Enveloping Winds (removed) |
| 5 | Tailwind (+10 ft more) | — |
| 8 | Lightning Step (swift blink, close range) | — |
| 12 | Riding the Current (2 swift blasts/round) | Secondary Elemental Focus (removed) |
| 16 | Eye of the Storm (full-dice swift blasts; electricity immunity) | Greater Elemental Focus (removed) |

- Swift blasts: free-action clones of the 7 air-family blasts, gated on
  owning the base blast; the round's charge is spent when the bolt is
  loosed - on a hit via RulePrepareDamage, on a miss via RuleAttackRoll
  (0.22.1; through 0.22.0 a miss cost nothing).
- Electricity ignores immunity/resistance via the Ascendant Element
  component itself (the vanilla mythic feature's mechanism, per the
  user's "as per other electric damage sources").

## Oracle

### Faithful port — Planar Oracle (Ultimate Magic) — IMPLEMENTED (0.24.0)

| Level | Planar Oracle grants | Oracle normally grants |
|---|---|---|
| 1 | Planar Attunement (choose: Heaven/electricity, Hell/fire, the Abyss/cold, the Maelstrom/acid) | — |
| 2 | Bonus spell: protection from evil (for endure elements) | — |
| 3 | Planar Resistance (ER 10 vs the plane's energy) | 3rd-level revelation (removed) |
| 4–18 | Bonus spells (see below), one per even level | — |
| 7 | — | 7th-level revelation (removed; our compensation, see below) |
| 11 | Planar Resistance surge (ER 20) | — |
| 20 | Apotheosis (immunity to the plane's energy; DR 10/magic) | Final revelation (removed) |

Bonus-spell substitutions (the tabletop's nine exist none in WOTR; each
substitute is native-level-matched to its planar slot): protection from
evil (2nd, for endure elements), see invisibility (4th, elemental
speech), protection from energy (6th, tongues), freedom of movement
(8th, planar adaptation), dismissal (10th, plane shift), chain
lightning (12th, mass planar adaptation), banishment (14th, shadow
walk), mind blank (16th, etherealness), elemental swarm (18th, gate).

Wrath adaptations (engine gaps, documented in the code header):
- The plane-energy association is GM-discretionary in the tabletop; ours
  follows each plane's native outsiders (archons/electricity,
  devils/fire, demons/cold, proteans/acid).
- The tabletop trades the MYSTERY's bonus spells; in WOTR those arrive
  through the mystery's own grant chain, which class-level archetype
  removal cannot intercept (the vanilla Enlightened Philosopher swaps
  the entire mystery selection to do it). The planar spells are granted
  additively instead, and the 7th-level revelation is also removed as
  compensation.
- The final-revelation trade is attempted against the generic
  OracleFinalRevelation plus all nine per-mystery finals (the no-op
  trick: entries absent from the class progression warn and skip at
  load - the [removals] log is the runtime truth).

### Homebrew — The Blood-Scribed (user design: the offensive caster) — IMPLEMENTED (0.24.0; RENAMED The Apocryphal in 0.25.0)

| Level | The Blood-Scribed grants | Oracle normally grants |
|---|---|---|
| 1–20 | The Stolen Grimoire: one wizard spell of a castable level per level, as oracle spells known; each page a permanent −2 max HP scar | — |
| 3, 7, 11, 15, 19 | — | All five revelations (removed) |

- The price is the user's first reading ("doing damage to themselves"),
  declined alternative ("strengthening their curses") documented: the
  curse is her god's wound; the theft is hers.
- The scar rides the stolen-spell feature itself (AddStatBonus,
  HitPoints, untyped stacking - the Solipsist Stats.HitPoints
  precedent), so it can never be dispelled or healed away: only paid.
- Options are read live from the vanilla WizardSpellList (cantrips
  included), each gated by the ORACLE's own spell level
  (AddPrerequisiteClassSpellLevel); generated blueprints carry
  deterministic MD5-guids, stable across runs and saves.
- Honest edges: a stolen pick duplicating an already-knowable spell is
  a wasted pick (the lists barely overlap; player's discretion is the
  guard).

### Mythic ability — Sacred Vow (the Intercessor's Vow, salvaged) — IMPLEMENTED (0.24.0)

- Only oracles can take it (prerequisite: one oracle level; TTT's
  prerequisite-gated mythic abilities prove the selection respects
  it). Intercession (swift, close range, one ally, never herself)
  marks a companion: half of the damage they take transfers to her as
  DirectDamage (exact conservation; no resistance or immunity; the TTT
  DamageRetribution rule pair). Mythic rank 4 widens the vow to two
  companions. A fallen carrier carries nothing.

## Paladin

### Faithful port — Shining Knight (Advanced Player's Guide) — IMPLEMENTED (0.25.0)

| Level | Shining Knight grants | Paladin normally grants |
|---|---|---|
| 3 | Skilled Rider (her pets carry her Divine Grace; Ride-skill clause cut - no Ride skill) | Divine Health (removed) |
| 5 | Divine Bond, fixed: the mount (the vanilla mount selection, no weapon option) | Divine Bond selection (weapon option removed) |
| 11 | Knight's Charge (mounted charge on her smite target: Will save or panicked, half level rounds) | Aura of Justice (removed) |

- Skilled Rider grants the VANILLA DivineGrace feature to every pet
  she owns, tick-refreshed (the SisterDragonAura idiom over
  Owner.Pets, the SetPetMinimumStat enumeration) and removed with the
  feature - so a later-acquired mount is never missed.
- Knight's Charge: charge detection via IsCharge on the weapon-attack
  rule (the TTT OnCharge pattern), mounted via her UnitPartRider's
  saddled mount (the TTT MountedCombatFixes API), the save via a
  triggered RuleSavingThrow (the DisjointEnchantments idiom), panic
  via the vanilla Eyebite panic buff. The smite-target check reads
  the vanilla SmiteEvilBuff's context target - best-effort,
  documented (no buff or no target, no rider).
- Documented cuts: the tabletop's Ride-armor clause (no Ride skill in
  WOTR) and its no-provoke-on-charge clause (no engine fact for it).
- (0.26.0 postmortem: this entire section was lost to a parallel-edit
  race in the 0.25.0 push - the changelog landed, the class section
  did not. Restored here. The rule is now absolute: one edit per
  file.)

### Homebrew — Fortune's Fool (user design: the chaos paladin) — IMPLEMENTED (0.25.0; REBALANCED 0.26.0)

| Level | Fortune's Fool grants | Paladin normally grants |
|---|---|---|
| 1 | Fate's Wager (natural 1-4: the attack scatters; natural 17+: extra 1d6/2 levels; +Cha luck vs fear, NOT immunity; weapon damage counts as chaotic); chaotic alignment required INSTEAD of lawful good | — |
| 3 | Aura of the Open Road (allies within 10 ft: +1 luck on attacks) | Aura of Courage (removed) |
| 8 | Aura of Whimsy (allies within 10 ft: DR 2 at 8th, 3 at 11th, 4 at 14th, 5 at 17th - and it stops there) | Aura of Resolve (removed) |
| 11 | Aura of the Wandering Star (allies: natural 17+ hits deal +2d6, once per ally until her next turn) | Aura of Justice (removed) |
| 14 | Aura of Fortune's Favor (allies: +2 luck on all saves) | Aura of Faith (removed) |
| 17 | Aura of the Laughing Fool (her companions carry Fate's Wager itself) | Aura of Righteousness (removed) |
| — | — | Lay on Hands, every Mercy, Smite Evil, Channel Positive Energy (all removed) |

- The alignment gate is Owlcat's own archetype-alignment mechanism
  (PrerequisiteAlignment with ArchetypeAlignment = true, tracker note
  PF-485644: an archetype's alignment restriction ALWAYS replaces the
  class's) - the three chaotic alignments replace lawful good.
- Fate's Wager is pure real dice (no hidden RNG): the attack roll's
  own natural die decides (evt.D20 read + evt.AutoMiss set, the
  TricksterParry idiom; the surge is a DirectDamage entry added via
  evt.Add, the AddAdditionalWeaponDamageOnHit idiom). The surge stacks
  with critical hits.
- 0.26.0 additions: the fool laughs at fear (a luck bonus on saves
  against fear equal to her Charisma modifier, minimum +2,
  descriptor-gated - NOT the aura-of-courage immunity), and her blade
  carries chaos (the vanilla AddOutgoingPhysicalDamageProperty with
  DamageAlignment.Chaotic - the creature-subtype mechanism). The
  prices: smite evil and channel positive energy removed.
- 0.26.0 rebalance: Whimsy is DR 5/- (the first draft's scatter band
  was too strong, per the user); the Wandering Star is once per ally
  until her next turn (the surge spends the ally's star; her round
  tick re-lights it - and if she falls, a spent star stays spent).
- The 17th aura excludes her (she already carries the wager - it never
  stacks with itself); the other auras include her (the paladin-aura
  convention).
- Divine grace, spells and the divine bond are untouched - chance did
  not ask for them back.

## Ranger

### Faithful port — The Guide (Advanced Player's Guide) — IMPLEMENTED (0.27.0)

| Level | The Guide grants | Ranger normally grants |
|---|---|---|
| 1 | Ranger's Focus (swift-action mark: +2 attack and damage vs the focus, +2 more every five levels; 1/day, +1 per three levels after 1st; a new mark clears the old) | Favored Enemy, ALL FIVE RANKS (removed at 1/5/10/15/20) |
| 4 | Terrain Bond (party within 30 ft: +2 initiative, Perception, Stealth, Lore (Nature)) | Hunter's Bond (removed) |
| 9 | Ranger's Luck (swift: the next missed attack is rerolled; 1/day, +1 at 14th and 19th) | Evasion (removed) |
| 11 | Inspired Moment (free action, 1 round: +10 speed, +4 AC, +4 attacks, auto-confirm every critical threat; 1/day, twice at 19th) | Quarry (removed) |
| 16 | Improved Ranger's Luck (the reroll carries +4 luck) | Improved Evasion (removed) |
| 19 | (second use of Inspired Moment) | Improved Quarry (removed) |
| 20 | Master Hunter — kept, not replaced | — |

- Coverage (corrected by the user, 0.27.0): vanilla WOTR ships SEVEN
  ranger archetypes — Flamewarden, Freebooter, Stormwalker,
  Demonslayer, Espionage Expert, Nomad, Sable Company Marine. The
  original check grepped archetype names for "ranger" and found only
  four; the WOTR-originals (Espionage Expert, Nomad, Sable Company
  Marine) carry no "ranger" in their names. TTT/COP/DarkCodex add
  none. The Guide (APG pg. 125) was unclaimed.
- Ranger's Focus removes favored enemy at every rank via the new
  AddRemovalsAtAllLevels helper (ArchetypeRemovals) — a single-level
  removal would have left the 5th/10th/15th/20th-rank grants alive.
  The mark is a permanent buff cleared by the next mark of the same
  ranger (a State.Units scan comparing buff casters); the bonus rides
  a temporary AdditionalAttackBonus modifier (the FoolFearless
  pattern) plus flat DirectDamage (DiceFormula.Zero, the TTT
  DamageRetribution idiom).
- Ranger's Luck rerolls only MISSES, so the Reroll(Fact, bool) flag's
  take-best/take-second ambiguity is harmless — documented, not
  faked. The buff is removed BEFORE the reroll so a synchronous
  re-trigger cannot recurse.
- Documented cuts (Terrain Bond): the favored-terrain condition (no
  inspectable current-terrain API in any reference — the bond is
  always on) and leave-no-trail (WOTR has no tracking). Survival →
  Lore (Nature), the WOTR wilderness skill — an adaptation, not a
  cut. "Line of sight and hearing" → 30 feet.
- Documented cuts (Ranger's Luck): the defensive mode — forcing an
  enemy's just-landed attack to reroll — needs an interrupt-reaction
  the engine does not generically expose.
- Documented cuts (Inspired Moment): the extra move/swift action (no
  generic extra-action API) and the +4 on skill and ability checks
  (no all-skills stat). Auto-confirm rides evt
  .AutoCriticalConfirmation (the TTT CritAutoconfirmAgainstClass
  idiom, minus the flanking condition).
- Homebrew contrast, deliberate: vanilla already HAS the companion
  archetypes (Nomad's horse, Sable Company's hippogriff), and the
  Guide is the ranger who walks alone — the stage for a genuinely
  strange companion homebrew is clear.

### Homebrew — The Wildbond (user design: the species bonds) — IMPLEMENTED (0.28.0)

| Level | The Wildbond grants | Ranger normally grants |
|---|---|---|
| 1 | The animal companion (the vanilla selection, eight levels early) | — |
| 4 | The species bond (see the table below) | Hunter's Bond (removed, subsumed) |
| 8 / 12 / 16 / 20 | The bond deepens (tier 2/3/4/5) — and at 12th, the spellbook returns | Favored Enemy (ranks, removed), Favored Terrain (ranks, removed), Evasion (removed), spellcasting at 4th (removed, restored at 12th) |

The bonds (tier = 4-7 / 8-11 / 12-15 / 16-19 / 20):

| Species | Bond | Tier growth |
|---|---|---|
| Dire Bear | Mauling Bond: hits MAUL (-10 speed, softer blows vs the ranger); the ranger's hit on a mauled enemy earns a bear attack, 1/round | ward 2→10; marks always on |
| Dire Boar | Primal Fury: frenzy below 50% HP — +dmg, second wind, fear immunity; kills heal | dmg 2→10, heals 5→25, frenzy at 75% at t5 |
| Dog | Heel & Hound: COMMANDED prey marks (Mark Prey - an active swift action on the hound itself, 0.29.0); ranger strikes send the dog running (+15 speed); prone prey = ranger +atk/dmg | bonus +1→5 |
| Elk | Stampede: charge hits add a phantom elk's attack roll (-8) and damage (1d8+Str-2) | penalty -8→0, dmg +0→+3 |
| Leopard | Ambush Bond: 1/round marks vulnerable (flanked/shaken/helpless) enemies; ranger precision vs marked | precision 1d6→5d6, +2 atk from t3 (concealment cut) |
| Mastodon | Siege Beast: momentum per moving/attacking round; at threshold the next hit is an AoE impact + slow | need 4→2, dmg 1d8+2→+10, radius 5→10 |
| Smilodon | Predator's Flurry: consecutive hits on one target stack bloodshed (+1 dmg/2 stacks); switch = reset | cap 4→12; extra attack at 6 stacks from t3 |
| Velociraptor | Rending Relay: hits stack rend; the ranger's attack consumes all stacks (burst + -2 AC shred) | cap 3→8, dice 1d4→1d8 |
| Wolf | Pack Howl: periodic howl buffs the party (vs enemies beside the wolf; enemies beset by 2 pack take penalties) | the user's schedule: t2 attack speed, t3 30ft +2/-2, t4 auto-howl, t5 wolf+ranger retaliation |
| Triceratops | Iron Charge: post-charge bulwark (+AC, trip immunity, allies +2 AC); Break the Bulwark = second charge | AC 2→6, second charge 2d6→4d6 |
| Centipede* | Toxic Symbiosis: bites stack venom (-1 saves/rank); ranger strikes deepen it | cap = tier |
| Horse* | Saddleborn: mounted +10 speed; mounted charge +1d6 per tier above 1 | charge 0→4d6 |
| Monitor* | Serpent's Vigor: bites leave lingering venom; while it lingers nearby, the monitor regenerates | regen 2→10, doubled vs 2+ |
| Other (modded) | Wild Vigor: the beast grows anyway — +tier attack, +tier AC | +1→5 |

*Our designs, per the user's "some of the pets dont have an ability,
make one up for them" — WOTR ships thirteen species; the user's brief
covered ten.

- Marks are ranked world buffs — stacks persist through saves. The
  pet carries a stamped rider buff refreshed on the ranger's round
  tick; species detection reads the ranger's own companion facts
  (preorder variants included).
- Engine notes: bear follow-ups and wolf retaliation ride
  ForceAttackOfOpportunity (the TTT SiezeTheMoment idiom); the
  phantom elk is a real second RuleAttackRoll (the dcx chain-attack
  ctor); Pack attack speed is BuffExtraAttack (the Haste idiom);
  frenzy/kill heals trigger RuleHealDamage (the COP NineLives ctor).
- Documented adaptations: the boar's temp HP is a second-wind heal;
  the leopard's concealment-ignore is +2 (no per-roll API); the
  triceratops's commanded re-charge is momentum, not a scripted
  movement (companions are player-controlled - the user's
  correction, 0.29.0: the player drives the charge, the command
  grants speed and damage); the mauled damage-ward is a negative
  DirectDamage rider; the raptor's bleed is instant damage.

## Rogue

### Faithful port — The Scout (Advanced Player's Guide) — IMPLEMENTED (0.29.0)

| Level | The Scout grants | Rogue normally grants |
|---|---|---|
| 4 | Scout's Charge (a charge forces the target flat-footed - sneak damage and denied Dex; uncanny-dodge foes immune) | Uncanny Dodge (removed) |
| 8 | Skirmisher (after moving 10+ feet in a round, her first attack forces the target flat-footed; uncanny-dodge foes immune) | Improved Uncanny Dodge (removed) |

- Coverage: vanilla WOTR ships EIGHT rogue archetypes — Eldritch
  Scoundrel, Knife Master, Thug, Master of All, Rowdy, Sylvan
  Trickster, Underground Chemist, and Dark Lurker (DLC). TTT, COP
  and DarkCodex add none. The Scout (APG pg. 134) was unclaimed —
  the rogue of the open road, companion piece to the Guide and the
  Wildbond.
- DOCUMENTED STRENGTHENING: the APG text is sneak-only ("deals
  sneak attack damage as if the target were flat-footed"), but this
  game build exposes no settable sneak-only flag
  (RuleAttackRoll.IsSneakAttack is read-only - CS0200). Both
  abilities force full flat-footedness instead: sneak damage AND a
  denied Dexterity bonus to AC. Both riders sit on the attack roll
  and set its ForceFlatFooted (compile-proven in this build; the
  check-event route's IsFlatFooted is read-only here). Charge
  detection rides the parent weapon-attack rule's IsCharge (the
  ShiningKnight/Wildbond idiom); Skirmisher's movement rides a
  round-tick position capture (the mastodon momentum idiom).
- The tabletop's immunity clause is honored: foes with the vanilla
  uncanny dodge feature are immune to both abilities.
- Documented adaptations: "moves more than 10 feet" is measured as
  net displacement from the round-tick position (no
  distance-traveled accumulator exists); "first attack of the turn"
  resets on the round tick.
- Sneak attack, trapfinding, danger sense, debilitating injuries,
  rogue talents, and master strike are untouched.

### Homebrew — The Steel Rain (user design: the throwing rogue) — IMPLEMENTED (0.30.0)

| Level | The Steel Rain grants | Rogue normally grants |
|---|---|---|
| 1 | Quick Hands (+1 attack with thrown weapons; +2 at 8th, +3 at 16th) — AND One Art: the flaw (see below) | Trapfinding (removed) |
| 6 | Flick of the Wrist (adds her Dex modifier to thrown damage, atop the throw's Strength) | Danger Sense (removed) |
| 12 | Catch! (once/round a thrown kill earns a free throw at the nearest enemy within 30 ft: 1d6 + Dex) | Danger Sense (removed) |
| 3–18 | — | Danger Sense (removed at every rank, 3/6/9/12/15/18) |

- 0.31.0 rebalance (user): Ricochet (the 4th-level glance) REMOVED;
  Flick of the Wrist moved 8th → 6th; and the added downside —
  **One Art** (1st, a visible flaw feature): -2 on attack rolls
  with any weapon that is NOT a thrown weapon. Her hands only know
  the throw; blades and bows alike suffer.

- Detection: thrown weapons in WOTR are RANGED weapon types — a
  whitelist of vanilla thrown candidates filtered at configure time
  by AttackType == Ranged (dagger stabs never count; the set is
  logged). Documented edge: modded thrown types unrecognized.
- Design choices, documented: Ricochet deals its damage without a
  second attack roll (simple); Flick of the Wrist stacks Dex ON TOP
  of the throw's Strength (throwing builds need the help); Catch!'s
  free throw deals fixed 1d6 + Dex rather than re-reading the
  weapon's dice (simple), and its once-per-round gate is set before
  the free throw so free-throw kills cannot chain.
- Sneak attack, evasion, uncanny dodge, improved uncanny dodge,
  rogue talents, debilitating injuries, and master strike are
  untouched — and the Scout's trades (uncanny dodge pair) do not
  overlap these, so the two archetypes remain combinable.

### Homebrew — The Anatomist (user design: the weak-point hunter) — IMPLEMENTED (0.31.0)

| Level | The Anatomist grants | Rogue normally grants |
|---|---|---|
| 1 | Weak Point (a miss that needed a high natural roll teaches a seam: permanent stacking +1 attack vs that enemy; the qualifying roll falls 16 → 10 by 18th) | Trapfinding (removed) |
| 4 | Risky Maneuver (swift: -6 AC for a round; her next attack that round applies a seam - hit or miss, no threshold) | Danger Sense (removed) |
| 8 | Student of Defenses (attacking a spellcaster: SR 4 + level for 1 round; damaging a DR enemy: +1 per 5 levels, not stackable) | Danger Sense (removed) |
| 12 | Read the Tell (+2 dodge AC against every enemy carrying her seams) | Danger Sense (removed) |
| 16 | Perfect Strike (once/round, a hit vs 5+ seams is an automatic critical) | Danger Sense (removed) |
| 3–18 | — | Danger Sense (removed at every rank) |

- 0.32.0 rebalance (user): the Weak Point threshold scales (16 at
  1st, -1 every three levels, 10 from 18th) - a fixed 16 made
  attack investment a CON (better hit rate = rarer qualifying
  misses = unreachable five seams); Risky Maneuver (the user's
  design) guarantees seams at the price of -6 AC for a round; SR
  lowered to 4 + level; Learn the Seams replaced by Read the Tell
  (a defensive tell-reading, +2 dodge AC vs seamed enemies); Vital
  Reading removed (20th is the rogue's own Master Strike).

- The needed-natural-roll is read straight off the attack event
  (TargetAC − AttackBonus — the dcx PanacheDodge formula attack.Roll
  + attack.AttackBonus >= attack.TargetAC); seams ride a ranked
  per-enemy mark (the Wildbond mark idiom), so stacks persist
  through saves.
- The self-limiting loop remains but the floor falls: a miss that
  needed the threshold teaches; each seam lowers the needed roll;
  when it drops below the (falling) threshold, misses stop
  teaching - and Risky Maneuver still finds seams on demand. A hard
  target educates her until it is no longer hard; a soft one can be
  baited.
- SR is the vanilla AddSpellResistance component with a class-level
  rank config (WithLinearProgression(1, 11) → 11 + rogue level);
  casters are detected via their Spellbooks; DR via the vanilla
  UnitPartDamageReduction's chunks (the dcx idiom).
- Perfect Strike reads the roll BEFORE resolution (the PanacheDodge
  idiom) and only spends itself on a roll that already shows a hit.
- Trades: trapfinding + danger sense (every rank) — the same
  skill-side price as the Steel Rain, so the two are alternatives
  rather than companions; each stacks with the Scout's uncanny-dodge
  trades.

## Witch

### Homebrew archetype — The Chimera (the user's design) — IMPLEMENTED (v1)

A witch of this bent does not keep a familiar. She keeps a beast, and the beast
keeps what it kills.

| Level | Feature | Effect |
| --- | --- | --- |
| 1 | Chimera's Beast | An animal companion: the full vanilla selection (16 species, preorder variants included), leveling with the witch's FULL class level (a hidden rank feature granted at every level 1-20, no druid tax). Carries the vanilla mount-target and companion-archetype features. |
| 1 | The Chimera's Bond | The diet (below). |

**The trades.** The familiar goes (WitchFamiliarSelection removed at 1st). Eight
of the ten hex levels go: every `WitchHexSelection` grant in the witch
progression is found at configure time and removed except the first two
(`AddRemovalsExceptFirstN`) - the exact vanilla grant schedule is followed
rather than assumed. And the spells thin: the archetype carries a REPLACED
SPELLBOOK (`SetReplaceSpellbook`, the COP WinterWitch idiom) - a copy of the
vanilla witch book whose spells-per-day table is rebuilt with every spell
level's slot count reduced by 1 at class levels 6 and above (floor 0).
Documented limitation: prestige classes that advance the witch spellbook
(Mystic Theurge and friends) will not advance the Chimera's thinned book.

**The Chimera's Diet.** When the companion kills a creature of a type it has
not yet eaten, it takes a piece of the kill: a visible marker buff appears on
the beast (its buff bar shows what it has eaten) and a permanent trait grows
from that flesh. One course per creature type, twelve types:

| Eaten | Trait | Effect |
| --- | --- | --- |
| Aberration | Alien Mind | +2 insight Will saves |
| Animal | Beast's Vigor | +10 ft. speed (enhancement) |
| Construct | Stone Guts | DR 2/adamantine |
| Dragon | Dragon's Eye | +1 competence attack rolls |
| Fey | Fey Step | +2 insight Reflex saves |
| Lycanthrope | Silverhide | DR 2/silver |
| Magical Beast | Monster's Hide | +2 natural armor AC |
| Monstrous Humanoid | Hunter's Instinct | +2 competence initiative |
| Outsider | Hellhound's Blood | fire resistance 10 |
| Plant | Rooted Flesh | +2 insight Fortitude saves |
| Undead | Grave's Gift | +1 insight all saves |
| Vermin | Swarm-Joints | immune to entangled |

The diet rides the bond feature on the WITCH (so it survives even a companion
swap), detects the victim's type facts at the moment of death, and records
each course as a marker buff on the BEAST - the markers persist in saves and
the bond's tick re-applies any trait a reload may drop. Documented cuts:
humanoids leave no piece worth taking (no HumanoidType fact exists to detect
them - and the chimera would agree); only the companion's kills feed the diet -
the witch's own kills are hers, not the beast's.

### Faithful port — Hedge Witch (Ultimate Magic pg. 84) — IMPLEMENTED (v1)

A witch who devotes herself to the care of others and restricts her practices to
the healing arts — the rural community's cleric.

| Level | Feature | Effect |
| --- | --- | --- |
| 4 | Spontaneous Healing | (replaces the 4th-level hex) Lose any prepared non-cantrip spell to cast any cure spell of the same spell level or lower (cure light wounds → mass heal; 7th-level slots convert to heal since regenerate is not in the game). The engine's own SpontaneousSpellConversion component, retargeted to the witch. |
| 8 | Empathic Healing | (replaces the 8th-level hex) Standard-action touch: removes all poisons and diseases from the target; the witch takes 2d6 empathic pain. Port adaptation of the tabletop's failed-save redirect, documented in the ability text. |

The patron selection is untouched (the tabletop's "normally a healing theme" is
a recommendation, not a rule).

*Witch coverage: 2 of 2 (homebrew: the Chimera, 0.33.0).*

## Shaman

### Faithful port — Jiuweihu (Kitsune Compendium pg. 8, Everyman Gaming) — IMPLEMENTED (v1)

A kitsune shaman blessed with multiple tails by the spirits of unseen kami — a bringer of
fortune and prosperity. Only kitsune may take it (gated on ChangeShapeKitsune).

| Level | Feature | Effect |
| --- | --- | --- |
| 1 | Star Jewel | (replaces the spirit animal) The spirit rests in a grape-sized star gem that cannot be slain; +1 luck bonus on all saving throws. The tabletop's jewel bound hex (familiar as an item — ioun orbit, hardness) has no engine support; this is the documented suitable alternative. |
| 1, 3, 5, 7, 9, 11, 13, 15 | Spirit Tails | (replaces spirit magic — see the deviation note) Magical Tail I–VIII as bonus feats, in order: vanish, hideous laughter, blur, invisibility, heroism, displacement, confusion, dominate person — each 2/day, Charisma-based, CL = HD. The vanilla feats, granted directly. |
| 17, 19 | Gift of the Ninth Tail | (the after-eight rider) Choose one Magical Tail spell-like ability: +1 use per day. OnlyNew mode enforces the tabletop's "cannot select the same tail twice." |

Port deviation (documented in the archetype description too): spirit magic has no
blueprint of its own — it is embedded in each spirit's components — so the tabletop's
spirit-magic trade could not be excised. The jiuweihu keeps her spirit magic; the port
is up that one trade. The Magical Tail feats are vanilla (TabletopTweaks' fixes to them
apply at runtime for users who run it).

*(Replaces the Speaker for the Past, removed this version per user feedback — its
time-mystery half could not be ported.)*

### Homebrew archetype — The Spirit-Ridden (the user's design) — IMPLEMENTED (v1)

Something else wears her face now. No spells, no hexes, no familiar, no spirit — the
heroes of dead ages inhabit her body instead, and she can let one of them take the
reins at will.

| Level | Feature | Effect |
| --- | --- | --- |
| 1 | Inhabiting Spirits | (replaces the spirit; the spellbook, all hexes and the spirit animal are all removed too) Choose a spirit to inhabit her: two answer at 1st — the Weapon Saint (fighter) and the Red Warlord (barbarian). Each is a form she can channel at will as a swift action, fighting as that class at shaman level − 2 (no reduction at 1st–2nd). |
| 6 | Inhabiting Spirits | The Barefoot Master (monk) and the Cutthroat (rogue) unlock. |
| 12 | Inhabiting Spirits | The Grey Hunter (ranger) unlocks. |
| 18 | Inhabiting Spirits | The First Knight (paladin) unlocks. |
| any | Spirit Equipment | While channeled and wearing NOTHING (every slot empty), the spirit's regalia manifests: enhancement to attack, damage and AC of 1 + (eff−1)/4 — +1 at effective 1st–4th, up to +5 at 17th+. Re-checked every tick. |

The six signatures: the Saint's edge (+1 attack/damage per 4 effective), the Warlord's
fury (Str/Con +2→+4→+6, Will +2, AC −2), the Master's dodge and stride (+1 AC per 4,
+20 speed), the Cutthroat's deep first wound (+1d6 per 3 effective, first hit each
round, +2 initiative), the Hunter's eye and pace (+1 attack per 4, +10 speed), the
Knight's aegis (+1 AC per 4, +1 saves per 3, fear immunity).

The forms are stat-and-proficiency packages rather than full class progressions (the
v1 scope cut — see the 0.36.0 changelog).

*Shaman coverage: 2 of 2 (faithful port: the Jiuweihu, 0.35.0; homebrew: the
Spirit-Ridden, 0.36.0).*

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
uleDealDamage on allies (DidTrigger, HP ≤ 0, not yet dead) and spends
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
n damage;
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
uleDealDamage on allies (DidTrigger, HP ≤ 0, not yet dead) and spends
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

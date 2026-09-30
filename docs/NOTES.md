# Durable notes — engine facts, recipes, ecosystem knowledge

The companion file to docs/COVERAGE.md: everything we learned the hard way
that is not obvious from the code. Read both before designing anything.
Add to this file whenever a new hard-won fact lands.

---

## Ecosystem facts

- **Call of the Wild is KINGMAKER-ONLY** (user correction, recorded 0.20.0).
  It is not a WOTR mod. Never check against it, port from it, or list it in
  coverage checks.
- **TabletopTweaks**: the archetypes live in the MAIN TabletopTweaks mod.
  `Vek17/TabletopTweaks-Core` (the repo we clone) is the LIBRARY only —
  cloning it shows zero archetypes. Check the main mod's list instead.
- **Kineticist Elements Expanded** (Nexus mod 344): adds Aether/Void/Wood,
  abandoned. Do not duplicate its elements, and do not lift its code or
  assets (permissions unknown).
- **alterasc mod-content database** (https://alterasc.github.io/): an
  auto-collected blueprint inventory of ~60 WOTR content mods with
  per-mod archetype/class pages. The fastest collision check available;
  now step 3 of the coverage policy. Caveat (its own): auto-collected,
  occasionally incomplete — cross-check the mod's own README/Nexus when a
  mod matters (Ebon's Bard archetype was missing from the DB, caught via
  its GitHub).
- **Worldcrawl**: adds a class; widely incompatible with other content
  mods (Ebon's ships a special WC build that drops an archetype to cope).
  Per the user: assume anything we ship is incompatible with Worldcrawl;
  never design against it.
- **WW-Blueprint-Core**: the csproj pins NuGet 2.8.6, which has NO matching
  git tag. The CI game DLL differs from bpcore's compile target — member
  drift is EXPECTED. bpcore source is a hint, never a guarantee; the CI
  probe is the arbiter (see Engine facts for the known drift list).
- `/tmp` clones are wiped between turns — re-clone on demand
  (`git clone --depth 1`). raw.githubusercontent.com and
  release-assets.githubusercontent.com are network-blocked; git clone,
  api.github.com and web fetches work.
- Vanilla kineticist coverage (verified): all 11 composites for the four
  base elements, metakinesis Empower/Maximize/Quicken, Burning infusion.
  Unused tabletop kineticist archetypes: Elemental Annihilator (still
  unimplemented anywhere), Kinetic Chirurgeon (ours since 0.20.0).

## The command menu recipe (variant menus)

The click-to-open submenu the user likes (Riftstalker Guided Command,
Construct Crafter Core/Program Command):

1. ONE hub ability per menu; its variants list is built at CONFIG time via
   `AddAbilityVariants(List<Blueprint<BlueprintAbilityReference>>)` — the
   same component vanilla's MasterHunterAbility uses.
2. The hub itself is the base entry (casting it directly = the basic
   option, e.g. Basic core).
3. **Hard limit (CI-probed, 0.15.0): the variant list is blueprint-static.**
   `m_Variants` is inaccessible (CS1061) and `Variants` is a read-only
   ReferenceArrayProxy (CS0200/CS0019). There is NO runtime mutation.
4. The pattern that ships: the menu lists ALL entries; each entry's action
   gates on ownership — `caster.HasFact(feature)` checked BEFORE any
   per-use cost; unlearned entries are empty whispers, and the game text
   says so.
5. Exclusive-set pattern (CC cores/programs): the action clears the axis
   first (remove every marker buff) then sets the chosen one; the old
   independent toggles allowed junk multi-marker states.
6. Ordering: declare the hub BEFORE any feature that `AddFacts` it —
   locals must be declared before use (CS0841).

## Engine facts (CI-verified — do not re-probe, just use)

- `OnActivate`/`OnDeactivate` on UnitFactComponentDelegate are **protected**
  → `protected override` (CS0507).
- `BlueprintAbility.ComponentsArray`, not `.Components` (CS1061).
- `RuleCastSpell` lives in `Kingmaker.RuleSystem.Rules.Abilities` (CS0246);
  the common rule types are in `Kingmaker.RuleSystem.Rules`.
- `AssetGuid` is a `BlueprintGuid` struct, not a string (CS1503) — compare
  with `HashSet<BlueprintGuid>` + `BlueprintGuid.Parse(...)`.
- `SetAllFeatures(FeatureRefs.X)` fails — use `.Reference.Get()` (CS1503).
- Blueprint areas: `SetTargetType`'s enum is inaccessible (CS0122) → filter
  via `AddAbilityAreaEffectBuff(buff, condition: IsEnemy())`. Area effects
  ride BUFFS (`BuffConfigurator.AddAreaEffect`); features cannot carry
  areas (CS1061) → a bearer component applies/removes the aura buff on
  gain/loss. No line-shape AoE builder exists (radius only).
- WOTR folds Intimidate into Persuasion: `StatType.SkillPersuasion`
  (there is no SkillIntimidate).
- The fear ladder tops out at Frightened — no Panicked buff; use Staggered
  for a top-tier terror effect.
- A unit's level: `Progression.CharacterLevel` (not TotalLevel).
- Save penalty against one source: no situational save modifier exists →
  `evt.AddBonusDC(2)` on her DCs (mechanically identical to −2 on the
  save; the TitanStrike pattern).
- Typed (energy) post-hoc damage: no verifiable API → raw damage via
  `target.Descriptor.Damage += amount` (bypasses DR — say so in game text).
  Post-hoc healing: `Damage = Math.Max(0, Damage - heal)`.
- Durations to seconds: `ContextDuration.Fixed(n).Calculate(Context).Seconds`.
- Skill checks from components: `new RuleSkillCheck(unit, StatType.X, dc)`
  + `Rulebook.Trigger<RuleSkillCheck>` + `.Success`.
- Subscriber interfaces live in `Kingmaker.PubSubSystem` (CS0246).
  Own-action events: `IInitiatorRulebookHandler<T> + IInitiatorRulebookSubscriber`;
  party/global: `IGlobalRulebookHandler<T> + IGlobalRulebookSubscriber`.
- Wisdom-modifier scaling: `AddContextStatBonus(stat,
  ContextValues.Rank(), desc)` + `AddContextRankConfig(ContextRankConfigs.StatBonus(StatType.Wisdom))`.
- Saves: `new RuleSavingThrow(target, SavingThrowType.Will, dc)` +
  TriggerRule + `.IsPassed`; the forcing caster is `evt.Reason.Caster`.
- Fear/debuff buff refs all exist: `BuffRefs.Shaken / Fatigued / Frightened
  / Staggered / Blind / Entangled / Stunned / Paralyzed / Slowed`.
- Burn/infusion cost members on cloned kineticist abilities are NOT named
  in refs — set via reflection hunt for an int member containing "Burn"
  (logged; fallback = the source's cost, log says so).


## Techniques from other mods (surveyed 2026-09-30 — clone, read, steal patterns)

Sources cloned at /tmp (wiped between turns — re-clone on demand):
kinarch = NosVladimir/KineticArchetypes, nineswords = V0idhead/WOTRNineSwords,
bb = factubsio/BubbleBuffs, darkcodex = Truinto/DarkCodex, tttc =
Vek17/TabletopTweaks-Core. Our csproj already references 0Harmony, so even
the Harmony-patch techniques below are adoptable if ever justified.

### Kinetic Archetypes — the burn API (CORRECTS an old note of ours)

- `unit.Parts.Get<UnitPartKineticist>()` exposes: `.AcceptBurn(cost,
  AbilityData)`, `.HealBurn(n)`, `.AcceptedBurn`, `.LeftBurn`,
  `.LeftBurnThisRound`; the burn resource is `kineticistPart.m_Settings.
  MaxBurn` (EsotericBlade.cs:435, :607; KineticDuelist.cs:565;
  KineticLancer.cs:788, :1105–1152; OnslaughtBlaster.cs:395).
- A blast's burn cost: `ability.GetComponent<AbilityKineticist>()
  .CalculateCost(abilityData)` — no reflection hunt needed.
- **This DISPROVES our earlier "burn cannot be granted from a verified
  API" note** (KineticChirurgeon's waiver is now a design choice, not an
  impossibility — recorded below). Burn-costed custom abilities are
  possible.
- Mount-granting recipe (Cinder Adept's horse): PetPart +
  `PetType.AnimalCompanion` + `FeatureRefs.AnimalCompanionFeatureHorse`
  + AnimalCompanionRank (CinderAdept.cs:153–212).
- Blast targeting helpers: `part.Blast.CanTarget(unit)`,
  `part.Blast.GetApproachDistance(unit)` (OnslaughtBlaster.cs:614).
- The vanilla kinetic-blade burn abilities exist as refs
  (`KineticBlade<Blast>BurnAbility` family) — the base for any blade work.

### Nine Swords — defensive hooks, forced misses, resources, combat log

- Target-side hooks (react to attacks AGAINST the owner — ours so far
  were initiator/global only): `ITargetRulebookHandler<RuleAttackRoll> +
  ITargetRulebookSubscriber` (Counters/ACIncreaseCounter.cs).
- Attack-roll surgery: `evt.AutoMiss = true` forces a miss; `evt.Roll`,
  `evt.TotalBonusValue`, `evt.TargetAC` are all readable — perfect for
  parry/counter design.
- Resources as a per-use budget: `Owner.Resources.HasEnoughResource(ref,
  n)` / `Owner.Resources.Spend(ref, n)` — cleaner than hidden buffs for
  N-per-rest or N-per-round budgets (their maneuver system runs on it).
- Player-visible feedback from custom components:
  `Helpers.WriteCombatLogMessage(msg, GameLogStrings.Instance.DefaultColor,
  Owner)` — we only log to the mod log today.

### BubbleBuffs — programmatic casting

- `UnitUseAbility.CreateCastCommand(abilityData, target)` drives a unit
  to cast any ability outside the action bar (AnimatedExecutionEngine.cs:20).
- Pipeline hooks: `IAbilityExecutionProcessHandler`,
  `IRulebookEventAboutToTriggerHook` (Handlers/EngineCastingHandler.cs).

### DarkCodex — lookups, spawning, a kineticist GUID index

- `ResourcesLibrary.TryGetBlueprint<T>(guid)` — direct blueprint lookup
  (alternative to BlueprintTool.Get); BpCache.cs is their caching layer.
- The pet-grafting recipe: `Game.Instance.EntityCreator.SpawnUnit(bp,
  pos, rot, owner.HoldingState, null)` (CodexLib/Components/
  AddUndeadCompanion.cs:145) — the same spawn surface our Construct
  Crafter uses.
- CodexLib/Classes/KineticistTree.cs — a hand-built GUID index of the
  entire vanilla kineticist tree (elements, blasts, talents). Reference
  material for any kineticist work.

### TabletopTweaks-Core — OwlcatReplacements and custom events

- OwlcatReplacements = reimplemented vanilla components, usable as
  patterns (or via TTT as a dependency): AddAbilityUseTriggerTTT (run
  actions on any ability use — the primitive we hand-roll via
  RuleCastSpell handlers), AddOutgoingDamageTriggerTTT,
  AddStatBonusIfHasFactTTT, AttackStatReplacementTTT (stat-to-damage
  swaps), ClassLevelsForPrerequisitesTTT (count class A's levels as
  class B for prerequisites — the multiclass-gateway trick).
- NewEvents (IDemoralizeHandler & co.): custom rulebook events raised
  via Harmony patches — needs a patcher (we have 0Harmony referenced,
  so the route is open; we have never shipped a patch).

### Second survey wave (2026-09-30): TTT main, CharacterOptions+, ToyBox

Sources additionally cloned: ttt = Vek17/TabletopTweaks (the MAIN mod;
content under TabletopTweaks-Base/NewContent/), cop =
WittleWolfie/CharacterOptionsPlus (bpcore-native, closest to our style),
toybox = xADDBx/ToyBox-Wrath.

- **TTT-Base ContentAdder.cs:** injects content at the earliest possible
  moment - a Harmony postfix on `BlueprintsCache.Init` with
  `[HarmonyPriority(Priority.First)]` and a static Initialized guard.
  The pattern for content that must exist before anything else reads
  blueprints. Also: `bp.TemporaryContext(bp => ...)` for safe mutation
  of EXISTING blueprints, and a NewContent taxonomy worth copying
  (AlternateCapstones, advanced weapon/armor trainings, per-class
  folders).
- **CharacterOptions+ variant menus:** the same AddAbilityVariants hub
  we use, applied to dual-output abilities - KeenEdge as one ability
  with main-hand/off-hand variants, EnergyChannel with class variants
  (Feats/EnergyChannel.cs:363, Spells/KeenEdge.cs:80). A clean fit for
  any future "same spell, two shapes" design.
- **ToyBox:** the encyclopedia of engine manipulation via Harmony -
  level-up controller patches (Classes/MonkeyPatchin/BagOfPatches/
  LevelUpPatchesWrath.cs) and an entire multiclass system built from
  patches (Classes/MonkeyPatchin/Multiclass/Archetypes.cs). Reference
  material if we ever need to touch the level-up flow.
- **kinarch typed construction (completes the burn entry):**
  `new AbilityKineticist { InfusionBurnCost = n }` - the typed cost
  member our Explosion infusion now uses (was a reflection hunt);
  `unit.Parts.Get<UnitPartKineticist>()` (the canonical access);
  `new AbilityCasterMainWeaponCheck { Category =
  WeaponCategory.KineticBlast }` (requires-a-kinetic-weapon restriction);
  `AddPrerequisiteNoArchetype` (blocks an archetype pick - mutual
  exclusivity between archetypes).
- **NineSwords combat log, full namespaces:** GameLogContext +
  LogThreadService + LogChannelType in Kingmaker.UI.Models.Log;
  CombatLogMessage in ...CombatLog_ThreadSystem; MessageLogThread in
  ...LogThreads.Common; GameLogStrings in
  Kingmaker.Blueprints.Root.Strings.GameLog. Implemented as our
  MissionWOTR.CombatLog helper (src/MissionWOTR/CombatLog.cs, 0.21.0).


### Technique corrections to our own past claims

- "Burn cannot be granted from a verified API" — WRONG, see the burn API
  above. FIXED IN 0.21.0: the Kinetic Chirurgeon now pays 1 burn per use
  (AcceptBurn, gather power applies engine-side), and the Explosion
  infusion's reflection-hunt cost setter was replaced with the typed
  AbilityKineticist.InfusionBurnCost.

## Recipes

- **Clone recipe** (units AND abilities — proven on Construct Crafter units
  0.5.x and blast abilities 0.18.0): `Configurator.New(name, guid)
  .CopyFrom(source, _ => true).Configure()`, then reflection-copy every
  field (Public | NonPublic | Instance | DeclaredOnly, skip init-only
  failures) of the blueprint type. CopyFrom alone is NOT enough (it never
  touches fields); bpcore's CopyFrom with zero types copies nothing.
- **CI triage**: `gh api repos/{owner}/{repo}/commits/<sha>/check-runs
  --jq '.check_runs[0].id'` → `gh api .../check-runs/<id>/annotations
  --paginate` → filter `error CS` → `sed 's/ \[D:.*csproj\]//'`.
- **Push ritual**: `git fetch -q origin && git rebase -q origin/arena/… &&
  git push` then `gh run watch <id> --exit-status --interval 30`.
- **Localization**: LocalizedStrings.json only via python json.load/dump,
  never raw append. Bump Info.json Version with every release.
- **Registration**: `Configure(nameof(X), X.Configure);` in MissionFeats +
  diagnostics tuple, in lockstep.
- **New ability GUIDs**: python `uuid.uuid4()`, uppercase in Guids.cs.

## Design policy (standing)

- Conservative adaptation; every cut documented in code AND docs —
  "documented, not faked."
- Game text is prose, not spec sheets.
- CI green is the shipping bar; the user does not playtest.
- Coverage check (docs/COVERAGE.md) runs BEFORE any new design.
- Hefty trades for hefty kits (the user's standing preference).

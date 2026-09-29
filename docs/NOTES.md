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

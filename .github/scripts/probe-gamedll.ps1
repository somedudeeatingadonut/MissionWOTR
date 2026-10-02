# TEMPORARY metadata probe + harvest report for the game DLL CI compiles against.
#
# Why: the sandbox cannot download the game-libs release asset (CDN blocked), so this is
# the only way to see (a) whether the "Locate game libraries" step actually gathered the
# DLLs and (b) the API surface of the Assembly-CSharp.dll being used.
# It dumps reflection metadata (no assembly loading needed) to probe-gamedll.txt and
# commits that file, so results are readable without run-log access.
#
# v13: the action vocabulary - whether a dialog can be OPENED from code (no
#      BlueprintUnit dialog field means there is no unit-level hook to hang one on).
# v12: the condition vocabulary (v11's Condition-prefix filter returned zero -
#      WOTR's condition types carry no prefix) + AddDamageResistanceBase, to confirm
#      where the DR Value field actually lives now that Sacred Shield sets one.
# v11: DR-vs-alignment + the whole dialog system. Two open questions: (a) whether
#      AddDamageResistancePhysical exposes an alignment bypass (Sacred Shield's DR 5/evil)
#      and (b) whether companion dialogue is reachable at all, and with what vocabulary.
#      Dumps every DialogSystem-namespaced blueprint with its fields, every Condition type
#      (the dialog condition vocabulary) and every Companion-named type (how a dialog finds
#      out who is in the party).
# v10b: retrigger - the v10 probe push lost a race with the code push; same targets (Concealment, WeaponRangeType, ITickEachRound).
# v10: Concealment + WeaponRangeType enum members (Omnielementalist's Ash Storm) + ITickEachRound (Sandstorm round-tick).
# v9: PetType + PetProgressionType enum members (the lich's undead pet for Undead Master's Corpse Bond).
# v8: TurnBasedCombatController + CombatController (turn-based round window - the user plays TB) + StatType (speed penalty checks).
# v7: Game / TimeController / GameTime (round window), ContextActionHeal + CalculationType (heal polarity), DamageEnergyType / EnergyDamage.
# v6: RuleDealDamage / RuleHealDamage / UnitEntityData member + ctor-param dump (the heal-by-damage homebrew).
# Remove this step + script once the API surface is known and the build is green.
$ErrorActionPreference = 'Continue'

$out = New-Object System.Collections.Generic.List[string]
function Log($s) { $out.Add($s); Write-Host $s }

Log "=== PROBE harvest report ==="
$requiredManaged = @(
  'Assembly-CSharp.dll', 'Assembly-CSharp-firstpass.dll', 'Newtonsoft.Json.dll',
  'Owlcat.Runtime.Core.dll', 'Owlcat.Runtime.UI.dll', 'Owlcat.Runtime.Validation.dll',
  'Owlcat.Runtime.Visual.dll', 'UnityEngine.dll', 'UnityEngine.CoreModule.dll')
$requiredUmm = @('UnityModManager.dll', '0Harmony.dll')

if (Test-Path _gamelibs-download-errors.txt) {
  Log "--- download errors ---"
  Get-Content _gamelibs-download-errors.txt | ForEach-Object { Log $_ }
}
Log "--- _gamelibs contents ---"
if (Test-Path _gamelibs) {
  $files = Get-ChildItem -Recurse _gamelibs -File -ErrorAction SilentlyContinue
  if ($files) {
    foreach ($f in $files) {
      Log ("_gamelibs: {0} size={1}" -f $f.FullName.Replace((Get-Location).Path + [IO.Path]::DirectorySeparatorChar, ''), $f.Length)
    }
  } else { Log "_gamelibs: (empty)" }
} else { Log "_gamelibs: (missing)" }
Log "--- Libs contents ---"
$libsFiles = Get-ChildItem -Recurse Libs -File -ErrorAction SilentlyContinue
if ($libsFiles) {
  foreach ($f in $libsFiles) { Log ("Libs: {0} size={1}" -f $f.Name, $f.Length) }
} else { Log "Libs: (empty)" }
Log "--- required DLL presence (Libs/) ---"
foreach ($d in $requiredManaged) { Log ("managed {0}: {1}" -f $d, (Test-Path "Libs/$d")) }
foreach ($d in $requiredUmm) { Log ("umm {0}: {1}" -f $d, (Test-Path "Libs/UMM/$d")) }
Log "--- required DLL presence (src/MissionWOTR/lib/) ---"
foreach ($d in $requiredUmm) { Log ("umm {0}: {1}" -f $d, (Test-Path "src/MissionWOTR/lib/$d")) }

$dll = 'Libs/Assembly-CSharp.dll'
if (-not (Test-Path $dll)) {
  Log "Assembly-CSharp.dll MISSING - no metadata dump possible."
  $out | Set-Content probe-gamedll.txt
} else {
  $item = Get-Item $dll
  $md5 = (Get-FileHash $item.FullName -Algorithm MD5).Hash
  Log ("=== PROBE fingerprint ===")
  Log ("Assembly-CSharp.dll size={0} md5={1}" -f $item.Length, $md5)

  try {
    $fs = [System.IO.File]::OpenRead((Join-Path (Get-Location) $dll))
    $pe = New-Object System.Reflection.PortableExecutable.PEReader($fs)
    $md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
  } catch {
    Log ("PROBE-ERROR metadata open: " + $_.Exception.ToString())
    $md = $null
  }

  if ($md -ne $null) {
    function BaseTypeName($md, $td) {
      try {
        $bt = $td.BaseType
        if ($bt.IsNil) { return '(nil)' }
        if ($bt.Kind -eq [System.Reflection.Metadata.HandleKind]::TypeReference) {
          $tr = $md.GetTypeReference([System.Reflection.Metadata.TypeReferenceHandle]$bt)
          return ('{0}.{1}' -f $md.GetString($tr.Namespace), $md.GetString($tr.Name))
        }
        if ($bt.Kind -eq [System.Reflection.Metadata.HandleKind]::TypeDefinition) {
          $tb = $md.GetTypeDefinition([System.Reflection.Metadata.TypeDefinitionHandle]$bt)
          return ('{0}.{1}' -f $md.GetString($tb.Namespace), $md.GetString($tb.Name))
        }
        return $bt.Kind.ToString()
      } catch { return ('(err ' + $_.Exception.Message + ')') }
    }

    $TARGETS = @('BlueprintUnit', 'BlueprintAiAttack', 'BlueprintAiCastSpell', 'BlueprintAiAction', 'BlueprintBrain',
      'BlueprintSpellbook', 'BlueprintSpellsTable', 'SpellsLevelEntry', 'AddPet',
      'PhysicalDamageMaterial', 'BlueprintFeatureSelection',
      'ContextRankConfig', 'SpontaneousSpellConversion', 'AddKnownSpell',
      'ContextCalculateAbilityParams', 'ContextCalculateAbilityParamsBasedOnClass', 'AddFacts',
      'BuffExtraEffects', 'UnitProgressionData', 'FeatureSelectionData', 'Feature',
      'UnitDescriptor', 'RuleAttackRoll', 'UnitAlignment',
      'RuleDealDamage', 'RuleHealDamage', 'UnitEntityData',
      'Game', 'TimeController', 'GameTime', 'ContextActionHeal',
      'CalculationType', 'HealCalculationType', 'DamageEnergyType', 'EnergyDamage', 'DirectDamage',
      'TurnBasedCombatController', 'CombatController', 'StatType',
      'PetType', 'PetProgressionType', 'Concealment', 'WeaponRangeType', 'ITickEachRound',
      'AddDamageResistancePhysical', 'AddDamageResistanceBase',
      'DamageResistancePhysical', 'DamageResistance',
      'BlueprintEtude', 'CueSelection', 'BlueprintCheck')
    $VIS = @{ 0 = 'internal'; 1 = 'public'; 2 = 'nested-public'; 3 = 'nested-private'; 4 = 'nested-family'; 5 = 'nested-internal'; 6 = 'nested-famand'; 7 = 'nested-famor' }
    $FACC = @{ 1 = 'private'; 2 = 'privatescope'; 3 = 'internal'; 4 = 'protected'; 5 = 'protandint'; 6 = 'protorint'; 7 = 'public' }

    foreach ($h in $md.TypeDefinitions) {
      $td = $md.GetTypeDefinition($h)
      $name = $md.GetString($td.Name)
      if ([string]::IsNullOrEmpty($name)) { continue }
      $ns = $md.GetString($td.Namespace)

      # 0.41.0: every Rage-named type with its fields - hunting the skald
      # rage-power grant component (BuffExtraEffects is known; the
      # ally-side carrier for inspired rage is not).
      if ($name -cmatch 'Rage|Caster|Align' -or $name -eq 'BuffExtraEvents') {
        $v = [int]$td.Attributes -band 7
        Log ("RAGE-TYPE: {0}.{1} vis={2} base={3}" -f $ns, $name, $VIS[$v], (BaseTypeName $md $td))
        if (-not $ns.StartsWith('Kingmaker.UI') -and -not $ns.Contains('Blueprints.References')) {
          foreach ($fh in $td.GetFields()) {
            $fd = $md.GetFieldDefinition($fh)
            $fa = [int]$fd.Attributes -band 7
            Log ("  FIELD: {0} [{1}]" -f $md.GetString($fd.Name), $FACC[$fa])
          }
        }
      }

      if ($ns -eq 'Kingmaker.AI.Blueprints') {
        $v = [int]$td.Attributes -band 7
        Log ("AI-TYPE: {0} vis={1}({2}) base={3}" -f $name, $VIS[$v], $v, (BaseTypeName $md $td))
      }

      # v11: the dialog system. Every DialogSystem-namespaced type, with fields on the
      # blueprint shapes - the node/cue/answer/etude vocabulary, before any line is written.
      if ($ns.Contains('DialogSystem')) {
        $v = [int]$td.Attributes -band 7
        Log ("DIALOG-TYPE: {0}.{1} vis={2} base={3}" -f $ns, $name, $VIS[$v], (BaseTypeName $md $td))
        if ($name.StartsWith('Blueprint') -or $name -eq 'CueSelection' -or $name -eq 'SequenceExit') {
          foreach ($fh in $td.GetFields()) {
            $fd = $md.GetFieldDefinition($fh)
            $fa = [int]$fd.Attributes -band 7
            Log ("  FIELD: {0} [{1}]" -f $md.GetString($fd.Name), $FACC[$fa])
          }
        }
      }

      # v13: can a conversation be OPENED from code? The condition vocabulary was
      # half of it; the other half is whether any action or context action starts a
      # dialog. Without that, a dialog we author has nothing to open it - BlueprintUnit
      # carries no dialog field, so there is no unit-level hook either. Dumping the
      # game-action vocabulary by name, and any dialog-ish context action.
      if ($ns -eq 'Kingmaker.Designers.EventConditionActionSystem.Actions') {
        Log ("GAME-ACTION: {0}.{1}" -f $ns, $name)
      }
      if ($ns -eq 'Kingmaker.UnitLogic.Mechanics.Actions' -and
          ($name.Contains('Dialog') -or $name.Contains('Cue') -or $name.Contains('Etude'))) {
        Log ("CTX-ACTION-DIALOG: {0}.{1} base={2}" -f $ns, $name, (BaseTypeName $md $td))
      }
      # v13: etude shapes - how a conversation gets triggered on chapter or area.
      if ($ns.Contains('Etudes')) {
        Log ("ETUDE-TYPE: {0}.{1} base={2}" -f $ns, $name, (BaseTypeName $md $td))
      }

      # v12: the dialog condition vocabulary. v11 filtered on names starting with
      # 'Condition' and returned ZERO, which is itself the finding: WOTR's condition
      # types carry no such prefix (DualCompanionInactive sits in the Conditions
      # namespace). So dump the whole namespace instead of guessing at the naming.
      if ($ns.Contains('Conditions')) {
        Log ("CONDITION-TYPE: {0}.{1}" -f $ns, $name)
      }

      # v11: how a dialog learns who is in the party.
      if ($name -cmatch 'Companion') {
        Log ("COMPANION-TYPE: {0}.{1} base={2}" -f $ns, $name, (BaseTypeName $md $td))
      }
      if ($name -eq 'UnitEntityData' -or $name -eq 'TemporaryHitPointsFromAbilityValue') {
        Log ("SEARCH-HIT: {0}.{1} vis={2}" -f $ns, $name, $VIS[([int]$td.Attributes -band 7)])
      }
      if ($TARGETS -contains $name) {
        $v = [int]$td.Attributes -band 7
        Log ("=== TYPE {0}.{1} vis={2} base={3} ===" -f $ns, $name, $VIS[$v], (BaseTypeName $md $td))
        foreach ($fh in $td.GetFields()) {
          $fd = $md.GetFieldDefinition($fh)
          $fa = [int]$fd.Attributes -band 7
          Log ("  FIELD: {0} [{1}]" -f $md.GetString($fd.Name), $FACC[$fa])
        }
        foreach ($ph in $td.GetProperties()) {
          Log ("  PROP: {0}" -f $md.GetString($md.GetPropertyDefinition($ph).Name))
        }
        foreach ($mh in $td.GetMethods()) {
          $mdo = $md.GetMethodDefinition($mh)
          $paramStr = ''
          try {
            $names = @()
            foreach ($ph in $mdo.GetParameters()) {
              $pd = $md.GetParameter($ph)
              if ($pd.SequenceNumber -gt 0) { $names += $md.GetString($pd.Name) }
            }
            if ($names.Count -gt 0) { $paramStr = ' (' + ($names -join ', ') + ')' }
          } catch { $paramStr = ' (params?)' }
          Log ("  METHOD: {0}{1}" -f $md.GetString($mdo.Name), $paramStr)
        }
      }
    }
    Log "=== PROBE done ==="
  }
  $out | Set-Content probe-gamedll.txt
}
Write-Host "probe-gamedll.txt written ($($out.Count) lines)"

# Commit the results so they are readable without run-log access.
# Guarded: no commit/push when the file is unchanged (prevents run loops).
git config user.name "ci-probe"
git config user.email "ci@users.noreply.github.com"
git add probe-gamedll.txt
git diff --cached --quiet
if ($LASTEXITCODE -ne 0) {
  git commit -m "Game DLL metadata probe results" | Out-Null
  $remote = "https://x-access-token:$($env:GH_TOKEN)@github.com/somedudeeatingadonut/MissionWOTR.git"
  git push $remote HEAD:$env:GITHUB_REF_NAME
  if ($LASTEXITCODE -ne 0) { Write-Host "::warning::probe push failed" }
}
exit 0

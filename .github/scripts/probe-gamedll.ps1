# TEMPORARY metadata probe for the game DLL CI compiles against.
#
# Why: the sandbox cannot download the game-libs release asset (CDN blocked), so this is
# the only way to see the API surface of the *user's current* Assembly-CSharp.dll.
# It dumps reflection metadata (no assembly loading needed) to probe-gamedll.txt and
# commits that file, so results are readable without run-log access.
#
# Remove this step + script once the API surface is known and the build is green.
$ErrorActionPreference = 'Continue'

$out = New-Object System.Collections.Generic.List[string]
function Log($s) { $out.Add($s); Write-Host $s }

Log "=== PROBE fingerprint ==="
$dll = 'Libs/Assembly-CSharp.dll'
if (-not (Test-Path $dll)) {
  Log "Assembly-CSharp.dll MISSING"
  $out | Set-Content probe-gamedll.txt
  exit 0
}
$item = Get-Item $dll
$md5 = (Get-FileHash $item.FullName -Algorithm MD5).Hash
Log ("Assembly-CSharp.dll size={0} md5={1}" -f $item.Length, $md5)
Get-ChildItem -Recurse _gamelibs -File -ErrorAction SilentlyContinue | ForEach-Object {
  Log ("_gamelibs: {0} size={1} md5={2}" -f $_.Name, $_.Length, (Get-FileHash $_.FullName -Algorithm MD5).Hash)
}

try {
  $fs = [System.IO.File]::OpenRead((Join-Path (Get-Location) 'Libs/Assembly-CSharp.dll'))
  $pe = New-Object System.Reflection.PortableExecutable.PEReader($fs)
  $md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
} catch {
  Log ("PROBE-ERROR metadata open: " + $_.Exception.ToString())
  $out | Set-Content probe-gamedll.txt
  exit 0
}

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

$TARGETS = @('BlueprintUnit', 'BlueprintAiAttack', 'BlueprintAiCastSpell', 'BlueprintAiAction', 'BlueprintBrain')
$VIS = @{ 0 = 'internal'; 1 = 'public'; 2 = 'nested-public'; 3 = 'nested-private'; 4 = 'nested-family'; 5 = 'nested-internal'; 6 = 'nested-famand'; 7 = 'nested-famor' }
$FACC = @{ 1 = 'private'; 2 = 'privatescope'; 3 = 'internal'; 4 = 'protected'; 5 = 'protandint'; 6 = 'protorint'; 7 = 'public' }

foreach ($h in $md.TypeDefinitions) {
  $td = $md.GetTypeDefinition($h)
  $name = $md.GetString($td.Name)
  if ([string]::IsNullOrEmpty($name)) { continue }
  $ns = $md.GetString($td.Namespace)

  if ($ns -eq 'Kingmaker.AI.Blueprints') {
    $v = [int]$td.Attributes -band 7
    Log ("AI-TYPE: {0} vis={1}({2}) base={3}" -f $name, $VIS[$v], $v, (BaseTypeName $md $td))
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
      Log ("  METHOD: {0}" -f $md.GetString($md.GetMethodDefinition($mh).Name))
    }
  }
}
Log "=== PROBE done ==="
$out | Set-Content probe-gamedll.txt
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

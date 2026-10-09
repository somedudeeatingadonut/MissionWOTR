# Catalogs the game's dialog system out of blueprints.zip on the game-libs release.
#
# WHY THIS EXISTS
#   Injecting companion dialogue means naming a vanilla BlueprintDialog, and the engine
#   offers no name-to-GUID lookup: BlueprintsCache.Init reads blueprints-pack.bbp as 16-byte
#   GUID + 4-byte offset per entry with no names, and both BlueprintsCache and ResourcesLibrary
#   are keyed purely by GUID. A wiki carries dialogue text, never AssetGuids. The blueprint
#   dump does - it is the same data the game loads, with names attached.
#
# v2 - CORRECTS A REAL DEFECT IN v1.
#   v1 decided an entry was a dialog with `$head -match 'BlueprintDialog'`, a raw substring
#   test. Any blueprint that merely REFERENCES a dialog matched, so 940 of the 1,712 rows it
#   published were actions and conditions - $PlayCustomMusic$, $Conditional$, $StartEtude$,
#   $SetObjectiveStatus$ - not conversations at all. The catalog was 55% wrong, and a search
#   for a character name hit almost nothing because the real dialogue content lives in cues
#   and answers, which v1 never emitted.
#
#   v2 parses the Data.$type discriminator and classifies on the actual type name, and emits
#   every dialog-system type, not just dialogs. A blueprint's name is what Owlcat gives its
#   serialized sub-elements ("$TypeName$guid"), which is the signature that exposed the bug.
$ErrorActionPreference = 'Continue'
$out = New-Object System.Collections.Generic.List[string]
function Log($s) { $out.Add($s); Write-Host $s }

$work = '_bpdump'
New-Item -ItemType Directory -Force -Path $work | Out-Null

$global:LASTEXITCODE = 0
if (-not (Test-Path "$work/blueprints.zip")) {
  Log "Downloading blueprints.zip"
  $dl = & gh release download game-libs --pattern blueprints.zip --dir $work --clobber 2>&1
  if ($LASTEXITCODE -ne 0) {
    Log ("DOWNLOAD-FAIL exited {0}: {1}" -f $LASTEXITCODE, ($dl -join ' | '))
    $out | Set-Content probe-blueprints.txt
    exit 0
  }
  $global:LASTEXITCODE = 0
}
$zipPath = Join-Path $work 'blueprints.zip'
if (-not (Test-Path $zipPath)) {
  Log "blueprints.zip not present - nothing to catalog."
  $out | Set-Content probe-blueprints.txt
  exit 0
}
Log ("archive: {0:N0} bytes" -f (Get-Item $zipPath).Length)

# The dialog system, by the type names Owlcat serializes them under.
$wanted = @(
  'BlueprintDialog', 'BlueprintCue', 'BlueprintAnswer', 'BlueprintAnswersList',
  'BlueprintCueSequence', 'BlueprintSequenceExit', 'BlueprintCheck'
)
$wantedSet = @{}
foreach ($w in $wanted) { $wantedSet[$w] = $true }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
  $entries = @($zip.Entries)
  Log "=== archive shape: $($entries.Count) entries ==="

  $types = @{}
  $rows = New-Object System.Collections.Generic.List[string]
  $noType = 0
  $scanned = 0

  foreach ($e in $entries) {
    if ($e.Length -eq 0) { continue }
    $ext = [System.IO.Path]::GetExtension($e.Name).ToLowerInvariant()
    if ($ext -ne '.jbp') { continue }
    $scanned++

    $sr = New-Object System.IO.StreamReader($e.Open())
    try {
      # 32 KB covers $type (always first in Data) and name for all but the largest blueprints.
      $take = [Math]::Min($e.Length, 32768)
      $buf = New-Object char[] $take
      $read = 0
      while ($read -lt $take) {
        $n = $sr.Read($buf, $read, $take - $read)
        if ($n -le 0) { break }
        $read += $n
      }
      if ($read -le 0) { continue }
      $head = -join $buf[0..($read - 1)]

      # The discriminator: "$type": "<32hex>, <TypeName>"
      if ($head -notmatch '"\$type"\s*:\s*"[0-9a-fA-F]{32},\s*([^"\.]+)"') { $noType++; continue }
      $type = $Matches[1].Trim()
      if ($types.ContainsKey($type)) { $types[$type]++ } else { $types[$type] = 1 }

      if (-not $wantedSet.ContainsKey($type)) { continue }

      $assetId = if ($head -match '"AssetId"\s*:\s*"([0-9a-fA-F]{32})"') { $Matches[1] } else { '<none>' }
      $name = if ($head -match '"name"\s*:\s*"([^"]{1,200})"') { $Matches[1] }
              else { [System.IO.Path]::GetFileNameWithoutExtension($e.Name) }
      $rows.Add(("{0}`t{1}`t{2}" -f $type, $name, $assetId))
    } catch {
      # One unreadable entry is not worth stopping a 236k-entry scan for.
    } finally { $sr.Dispose() }
  }

  Log ("scanned {0} .jbp entries; {1} had no parsable `$type" -f $scanned, $noType)

  Log "=== blueprint types present (top 25) ==="
  $types.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 25 |
    ForEach-Object { Log ("  {0,-42} {1}" -f $_.Key, $_.Value) }

  Log "=== dialog-system types found ==="
  foreach ($w in $wanted) {
    $n = if ($types.ContainsKey($w)) { $types[$w] } else { 0 }
    Log ("  {0,-26} {1}" -f $w, $n)
  }

  $rows | Sort-Object | Set-Content -Encoding UTF8 probe-dialogs.txt
  Log ("probe-dialogs.txt written: {0} rows" -f $rows.Count)

  # The case that started this: a character name should find the conversations they are in,
  # and v1 found almost none because it never emitted cues or answers.
  Log "=== rows matching 'lann' (any dialog-system type) ==="
  $hits = @($rows | Where-Object { $_ -match '(?i)lann' } | Sort-Object)
  Log ("  {0} matches" -f $hits.Count)
  $hits | Select-Object -First 60 | ForEach-Object { Log ("  " + $_) }

  Log "=== rows matching 'wenduag' ==="
  $wh = @($rows | Where-Object { $_ -match '(?i)wenduag' } | Sort-Object)
  Log ("  {0} matches" -f $wh.Count)
  $wh | Select-Object -First 30 | ForEach-Object { Log ("  " + $_) }
} finally {
  $zip.Dispose()
}

$out | Set-Content probe-blueprints.txt
Write-Host "probe-blueprints.txt written ($($out.Count) lines)"

git config user.name "ci-probe"
git config user.email "ci@users.noreply.github.com"
# Added separately: a missing probe-dialogs.txt must not stop probe-blueprints.txt landing.
foreach ($f in @('probe-blueprints.txt', 'probe-dialogs.txt')) {
  if (Test-Path $f) { git add $f }
}
git diff --cached --quiet
if ($LASTEXITCODE -ne 0) {
  git commit -m "Blueprint dump v2: classify by real type, catalog cues and answers" | Out-Null
  $remote = "https://x-access-token:$($env:GH_TOKEN)@github.com/somedudeeatingadonut/MissionWOTR.git"
  git push $remote HEAD:$env:GITHUB_REF_NAME
  if ($LASTEXITCODE -ne 0) { Write-Host "::warning::blueprint probe push failed" }
}
$global:LASTEXITCODE = 0
exit 0

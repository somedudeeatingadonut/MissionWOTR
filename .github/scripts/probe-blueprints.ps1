# Catalogs the game's own conversations out of blueprints.zip on the game-libs release.
#
# WHY THIS EXISTS
#   Injecting companion dialogue means naming a vanilla BlueprintDialog, and the engine
#   offers no name-to-GUID lookup at all: BlueprintsCache.Init reads blueprints-pack.bbp as
#   16-byte GUID + 4-byte offset per entry with no names, and both BlueprintsCache and
#   ResourcesLibrary are keyed purely by GUID. So a dialog's name only exists once it is
#   materialized at runtime, which is why the mod currently matches by name and guesses.
#
#   A wiki does not help - wikis document quests and dialogue text, not AssetGuids. The
#   blueprint dump does: it is the same data the game loads, with names attached. This turns
#   the guess into a lookup.
#
# v1 discovers the archive's shape first and commits it, because guessing the format is how
#   the .patch AssetId hunt went wrong. It reads entries by streaming, never extracting the
#   whole 269 MB archive, and only the head of each entry is parsed.
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

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
  $entries = @($zip.Entries)
  Log "=== archive shape: $($entries.Count) entries ==="

  $entries | ForEach-Object { [System.IO.Path]::GetExtension($_.Name) } |
    Group-Object | Sort-Object Count -Descending | Select-Object -First 12 |
    ForEach-Object { Log ("  ext {0,-10} {1}" -f ($_.Name -replace '^$', '<none>'), $_.Count) }

  Log "--- first 60 entry names ---"
  $entries | Select-Object -First 60 | ForEach-Object { Log ("  " + $_.FullName) }

  $total = ($entries | Measure-Object -Property Length -Sum).Sum
  Log ("--- uncompressed total: {0:N0} bytes ---" -f $total)

  # --- Sample the head of a few entries so the format is known, not assumed. ---
  Log "--- sample entry heads ---"
  $sampled = 0
  foreach ($e in $entries) {
    if ($sampled -ge 3) { break }
    if ($e.Length -eq 0) { continue }
    Log ("  ### " + $e.FullName)
    $sr = New-Object System.IO.StreamReader($e.Open())
    try {
      $head = $sr.ReadToEnd()
      if ($head.Length -gt 700) { $head = $head.Substring(0, 700) }
      ($head -split "`n") | Select-Object -First 14 | ForEach-Object { Log ("    " + $_.TrimEnd()) }
    } finally { $sr.Dispose() }
    $sampled++
  }

  # --- The catalog itself: dialog name -> AssetGuid. ---
  # Owlcat .jbp files are { "AssetId": "<32hex>", "Data": { "$type": "<hex>, TypeName", ... } },
  # and a blueprint's name sits in Data.name. Only the head of each entry is read, because
  # $type and name both appear near the top.
  Log "=== scanning for dialogs (streaming, head only) ==="
  $dialogs = New-Object System.Collections.Generic.List[string]
  $scanned = 0
  $jsonish = 0
  foreach ($e in $entries) {
    if ($e.Length -eq 0 -or $e.Length -gt 2000000) { continue }
    $ext = [System.IO.Path]::GetExtension($e.Name).ToLowerInvariant()
    if ($ext -notin @('.jbp', '.json', '.txt', '.bp')) { continue }
    $scanned++
    $sr = New-Object System.IO.StreamReader($e.Open())
    try {
      $buf = New-Object char[] 8192
      $n = $sr.Read($buf, 0, 8192)
      if ($n -le 0) { continue }
      $head = -join $buf[0..($n - 1)]
      if ($head -notmatch '"?\$?"?type') { continue }
      $jsonish++
      if ($head -notmatch 'BlueprintDialog') { continue }
      $assetId = if ($head -match '"AssetId"\s*:\s*"([0-9a-fA-F]{32})"') { $Matches[1] } else { '<no-assetid>' }
      $name = if ($head -match '"name"\s*:\s*"([^"]{1,160})"') { $Matches[1] }
              else { [System.IO.Path]::GetFileNameWithoutExtension($e.Name) }
      $dialogs.Add(("{0}`t{1}" -f $name, $assetId))
    } catch {
      # One unreadable entry is not worth stopping the scan for.
    } finally { $sr.Dispose() }
  }
  Log ("  scanned {0} entries, {1} looked like blueprints, {2} were dialogs" -f $scanned, $jsonish, $dialogs.Count)

  $dialogs | Sort-Object -Unique | Set-Content -Encoding UTF8 probe-dialogs.txt
  Log "probe-dialogs.txt written"
  $dialogs | Sort-Object -Unique | Select-Object -First 40 | ForEach-Object { Log ("  " + $_) }
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
  git commit -m "Blueprint dump: dialog name to GUID catalog" | Out-Null
  $remote = "https://x-access-token:$($env:GH_TOKEN)@github.com/somedudeeatingadonut/MissionWOTR.git"
  git push $remote HEAD:$env:GITHUB_REF_NAME
  if ($LASTEXITCODE -ne 0) { Write-Host "::warning::blueprint probe push failed" }
}
$global:LASTEXITCODE = 0
exit 0

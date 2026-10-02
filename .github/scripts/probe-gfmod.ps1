# Reference-mod probe: what can we learn from Gracious Friendships without shipping it?
#
# Why: the sandbox cannot reach release-assets.githubusercontent.com (the API works, the
# asset redirect does not), so the only place a 1.3MB upload can be read is CI. This
# downloads the Gracious Friendships assets from the 'game-libs' release, unpacks them,
# and writes a SUMMARY to probe-gfmod.txt - the mod itself is never committed.
#
# The point is not to copy their content. It is three facts we cannot get any other way:
#   1. the GUIDs of the vanilla dialogs/cues they hook into - blueprint GUIDs are game
#      content, so neither the DLL probe nor BlueprintCore's reference lists have them;
#   2. how a conversation is actually attached to a companion (they have working examples,
#      we have field lists);
#   3. what a populated ConditionsChecker / ActionsHolder looks like in the wild.
#
# v1: first pass.
$ErrorActionPreference = 'Continue'

$out = New-Object System.Collections.Generic.List[string]
function Log($s) { $out.Add($s); Write-Host $s }

$work = '_gfmod'
New-Item -ItemType Directory -Force -Path $work | Out-Null

$assets = @(& gh release view game-libs --json assets --jq '.assets[].name' |
  Where-Object { $_ -match 'GraciousFriendships' })
$global:LASTEXITCODE = 0
if ($assets.Count -eq 0) {
  Log "No GraciousFriendships assets on the game-libs release."
}
foreach ($asset in $assets) {
  if (Test-Path "$work/$asset") { Log "Already downloaded: $asset"; continue }
  Log "Downloading $asset"
  $dl = & gh release download game-libs --pattern $asset --dir $work --clobber 2>&1
  if ($LASTEXITCODE -ne 0) { Log ("DOWNLOAD-FAIL {0} exited {1}: {2}" -f $asset, $LASTEXITCODE, ($dl -join ' | ')) }
  $global:LASTEXITCODE = 0
}

Get-ChildItem -Path $work -Filter *.zip -File | ForEach-Object {
  Log "Extracting $($_.Name)"
  Expand-Archive -Path $_.FullName -DestinationPath (Join-Path $work 'x') -Force
}

$root = Join-Path $work 'x'
if (-not (Test-Path $root)) {
  Log "Nothing extracted - no analysis possible."
  $out | Set-Content probe-gfmod.txt
  exit 0
}

$files = @(Get-ChildItem -Path $root -Recurse -File)
Log "=== GF inventory: $($files.Count) files ==="
$files | Group-Object Extension | Sort-Object Count -Descending |
  ForEach-Object { Log ("  ext {0,-10} {1}" -f ($_.Name), $_.Count) }
Log "--- top-level layout ---"
Get-ChildItem -Path $root -Recurse -Directory | Select-Object -First 25 |
  ForEach-Object { Log ("  dir " + $_.FullName.Replace((Get-Location).Path + '\', '')) }

# --- Per-blueprint index -------------------------------------------------------
# Owlcat .jbp files are JSON. Each carries a $type (the blueprint class) and an
# AssetGuid (its own id). Collecting both lets us tell GF's own blueprints apart
# from the vanilla ones they point at.
$jbps = @($files | Where-Object { $_.Extension -eq '.jbp' })
Log "=== GF blueprints: $($jbps.Count) .jbp files ==="
$ownGuids = New-Object System.Collections.Generic.HashSet[string]
$bpLines = New-Object System.Collections.Generic.List[string]
foreach ($f in $jbps) {
  try {
    $j = Get-Content $f.FullName -Raw | ConvertFrom-Json
  } catch { Log ("  PARSE-FAIL " + $f.Name + " : " + $_.Exception.Message); continue }
  $type = ''
  try { $type = $j.'$type' } catch { }
  if (-not $type) { try { $type = $j.'Type' } catch { } }
  $guid = ''
  try { $guid = $j.'AssetGuid' } catch { }
  if (-not $guid) { try { $guid = $j.'assetGuid' } catch { } }
  $nm = $f.BaseName
  if ($guid) { [void]$ownGuids.Add($guid.ToLower()) }
  $short = ($type -split ',')[-1]
  $short = ($short -split '\.')[-1]
  $bpLines.Add(("  {0,-34} {1,-42} {2}" -f $short, $nm, $guid))
}
$bpLines | Sort-Object | Select-Object -First 500 | ForEach-Object { Log $_ }
if ($bpLines.Count -gt 500) { Log ("  ... and {0} more" -f ($bpLines.Count - 500)) }

# --- External references: the vanilla dialogs and cues -------------------------
# Any dashed GUID inside a .jbp that is NOT one of GF's own AssetGuids is a
# reference to something that already exists in the game. Those are the attach
# points - the thing we have no other way to obtain.
Log "=== external (vanilla) blueprint references ==="
$guidRe = [regex]'[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}'
$extByFile = @{}
foreach ($f in $jbps) {
  try { $raw = Get-Content $f.FullName -Raw } catch { continue }
  $seen = New-Object System.Collections.Generic.HashSet[string]
  foreach ($m in $guidRe.Matches($raw)) {
    $g = $m.Value.ToLower()
    if ($ownGuids.Contains($g)) { continue }
    [void]$seen.Add($g)
  }
  if ($seen.Count -gt 0) { $extByFile[$f.BaseName] = $seen }
}
$allExt = New-Object System.Collections.Generic.HashSet[string]
foreach ($k in $extByFile.Keys) { foreach ($g in $extByFile[$k]) { [void]$allExt.Add($g) } }
Log "  distinct external GUIDs: $($allExt.Count) across $($extByFile.Count) files"
Log "--- dialog- and cue-named files first (these are the attach points) ---"
$n = 0
foreach ($k in ($extByFile.Keys | Sort-Object { -($_ -match 'ialog|Cue|Answer') }, { $_ })) {
  if ($n -ge 160) { Log "  ... truncated"; break }
  Log ("  {0} -> {1}" -f $k, (($extByFile[$k] | Sort-Object) -join ' '))
  $n++
}

# --- One real example, so the wiring shape is visible --------------------------
$sample = $jbps | Where-Object { $_.BaseName -match 'ialog' } | Select-Object -First 1
if (-not $sample) { $sample = $jbps | Select-Object -First 1 }
if ($sample) {
  Log "=== sample blueprint: $($sample.Name) (first 130 lines) ==="
  $lines = Get-Content $sample.FullName
  $lines | Select-Object -First 130 | ForEach-Object { Log ("  " + $_) }
  if ($lines.Count -gt 130) { Log ("  ... {0} lines total" -f $lines.Count) }
}

$out | Set-Content probe-gfmod.txt
Write-Host "probe-gfmod.txt written ($($out.Count) lines)"

# Commit the summary (never the mod) so it is readable without run-log access.
git config user.name "ci-probe"
git config user.email "ci@users.noreply.github.com"
git add probe-gfmod.txt
git diff --cached --quiet
if ($LASTEXITCODE -ne 0) {
  git commit -m "Gracious Friendships reference-mod analysis" | Out-Null
  $remote = "https://x-access-token:$($env:GH_TOKEN)@github.com/somedudeeatingadonut/MissionWOTR.git"
  git push $remote HEAD:$env:GITHUB_REF_NAME
  if ($LASTEXITCODE -ne 0) { Write-Host "::warning::gf-mod probe push failed" }
}
exit 0

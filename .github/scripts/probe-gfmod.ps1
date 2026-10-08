# Reference-mod probe v2: what can we learn from Gracious Friendships without shipping it?
#
# Why: the sandbox cannot reach release-assets.githubusercontent.com (the API works, the
# asset redirect does not), so the only place this upload can be read is CI. Downloads the
# Gracious Friendships assets from the 'game-libs' release, unpacks them, and writes a
# SUMMARY to probe-gfmod.txt - the mod itself is never committed.
#
# v2: v1 got the .jbp shape wrong, and it mattered. A .jbp is
#     { "AssetId": "<32 hex, no dashes>", "Data": { "$type": "<32hex>, TypeName", ... } }
#     v1 looked for a top-level "AssetGuid" that does not exist, so the "own ids" set came
#     back empty and every internal reference was miscounted as an external one - hence the
#     bogus "1400 distinct external GUIDs". Internal refs are also written "!bp_<32hex>",
#     so the dashed GUIDs v1 counted were not blueprint references at all.
#     v2 reads AssetId + Data.$type, resolves "!bp_" refs against the real own-id set, and
#     samples a .patch file - v1 never looked at those, and the patches are how GF modifies
#     blueprints that already exist.
$ErrorActionPreference = 'Continue'

$out = New-Object System.Collections.Generic.List[string]
function Log($s) { $out.Add($s); Write-Host $s }

$work = '_gfmod'
New-Item -ItemType Directory -Force -Path $work | Out-Null

$assets = @(& gh release view game-libs --json assets --jq '.assets[].name' |
  Where-Object { $_ -match 'GraciousFriendships' })
$global:LASTEXITCODE = 0
foreach ($asset in $assets) {
  if (Test-Path "$work/$asset") { Log "Already downloaded: $asset"; continue }
  Log "Downloading $asset"
  $dl = & gh release download game-libs --pattern $asset --dir $work --clobber 2>&1
  if ($LASTEXITCODE -ne 0) { Log ("DOWNLOAD-FAIL {0} exited {1}: {2}" -f $asset, $LASTEXITCODE, ($dl -join ' | ')) }
  $global:LASTEXITCODE = 0
}

$root = Join-Path $work 'x'
Get-ChildItem -Path $work -Filter *.zip -File | ForEach-Object {
  Log "Extracting $($_.Name)"
  Expand-Archive -Path $_.FullName -DestinationPath $root -Force
}
if (-not (Test-Path $root)) {
  Log "Nothing extracted - no analysis possible."
  $out | Set-Content probe-gfmod.txt
  exit 0
}

$files = @(Get-ChildItem -Path $root -Recurse -File)
Log "=== GF inventory: $($files.Count) files ==="
$files | Group-Object Extension | Sort-Object Count -Descending |
  ForEach-Object { Log ("  ext {0,-10} {1}" -f $_.Name, $_.Count) }

$jbps = @($files | Where-Object { $_.Extension -eq '.jbp' })
$patches = @($files | Where-Object { $_.Extension -eq '.patch' })

# --- Blueprint index: type + name + AssetId ------------------------------------
Log "=== GF blueprints: $($jbps.Count) .jbp files ==="
$ownIds = New-Object System.Collections.Generic.HashSet[string]
$parsed = @()
foreach ($f in $jbps) {
  try { $j = Get-Content $f.FullName -Raw | ConvertFrom-Json } catch { continue }
  $id = ''
  try { $id = $j.AssetId } catch { }
  $type = ''
  try { $type = $j.Data.'$type' } catch { }
  if ($id) { [void]$ownIds.Add($id.ToLower()) }
  $short = ($type -split ',')[-1].Trim()
  $parsed += [pscustomobject]@{ File = $f.BaseName; Type = $short; Id = $id }
}
Log "--- blueprint types (histogram) ---"
$parsed | Group-Object Type | Sort-Object Count -Descending | Select-Object -First 20 |
  ForEach-Object { Log ("  {0,-30} {1}" -f $_.Name, $_.Count) }

# --- External references: !bp_ refs that are not GF's own -----------------------
# This is the real prize - the vanilla dialogs and cues GF hooks into. Blueprint
# GUIDs are game content, so neither the DLL probe nor BlueprintCore's reference
# lists contain them.
$bpRef = [regex]'!bp_([0-9a-fA-F]{32})'
$extByFile = @{}
foreach ($f in $jbps + $patches) {
  try { $raw = Get-Content $f.FullName -Raw } catch { continue }
  $seen = New-Object System.Collections.Generic.HashSet[string]
  foreach ($m in $bpRef.Matches($raw)) {
    $g = $m.Groups[1].Value.ToLower()
    if ($ownIds.Contains($g)) { continue }
    [void]$seen.Add($g)
  }
  if ($seen.Count -gt 0) { $extByFile[$f.BaseName] = $seen }
}
$allExt = New-Object System.Collections.Generic.HashSet[string]
foreach ($k in $extByFile.Keys) { foreach ($g in $extByFile[$k]) { [void]$allExt.Add($g) } }
Log "=== external (vanilla) blueprint references ==="
Log "  GF's own AssetIds: $($ownIds.Count)"
Log "  distinct external refs: $($allExt.Count) across $($extByFile.Count) files"
Log "--- files referencing vanilla blueprints, dialogs and patches first ---"
$n = 0
foreach ($k in ($extByFile.Keys | Sort-Object { -($_ -match 'ialog|Patch|Cue') }, { $_ })) {
  if ($n -ge 200) { Log "  ... truncated"; break }
  Log ("  {0} -> {1}" -f $k, (($extByFile[$k] | Sort-Object) -join ' '))
  $n++
}

# --- One dialog .jbp in full: the populated node shape -------------------------
$sample = $parsed | Where-Object { $_.Type -eq 'BlueprintDialog' } | Select-Object -First 1
if (-not $sample) { $sample = $parsed | Where-Object { $_.Type -match 'Cue' } | Select-Object -First 1 }
if ($sample) {
  $path = ($jbps | Where-Object { $_.BaseName -eq $sample.File } | Select-Object -First 1).FullName
  Log "=== sample dialog blueprint: $($sample.File) ($($sample.Type)) ==="
  Get-Content $path | Select-Object -First 90 | ForEach-Object { Log ("  " + $_) }
}

# --- Every .patch and the vanilla blueprint it targets --------------------------
# v3: a .patch file's own AssetId IS the AssetId of the vanilla blueprint it modifies.
# That is the missing link for companion dialogue - the mod cannot name Seelah's dialog
# by GUID because the startup dialog dump runs before vanilla dialogs are materialized,
# and this list gives it directly. Filename convention is gfr__<VanillaAssetName>.patch.
Log "=== patch targets: each .patch and the vanilla blueprint it modifies ==="
foreach ($p in ($patches | Sort-Object Name)) {
  $tid = $null
  try { $tid = (Get-Content $p.FullName -Raw | ConvertFrom-Json).AssetId } catch { }
  if (-not $tid) { $tid = '<unreadable>' }
  $vanilla = ($p.BaseName -replace '^gfr__', '')
  Log ("  {0,-52} {1}  ({2})" -f $vanilla, $tid, $p.Name)
}

# --- One .patch in full: how an existing blueprint gets modified ---------------
# GF injects reactions into conversations the game already has. The patches are
# the mechanism, and reading one is worth more than any amount of inference.
$patch = $patches | Where-Object { $_.Name -match 'ialog' } | Select-Object -First 1
if (-not $patch) { $patch = $patches | Select-Object -First 1 }
if ($patch) {
  Log "=== sample patch: $($patch.Name) ==="
  Get-Content $patch.FullName | Select-Object -First 90 | ForEach-Object { Log ("  " + $_) }
}

$out | Set-Content probe-gfmod.txt
Write-Host "probe-gfmod.txt written ($($out.Count) lines)"

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

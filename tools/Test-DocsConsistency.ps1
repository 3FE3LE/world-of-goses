<#
.SYNOPSIS
    Cheapest possible drift guard for the documentation that the rest of
    the repo takes as truth.

.DESCRIPTION
    Three checks, sub-second, no Godot, no network:

      * `WorldSave.CurrentVersion` is the only authority on the current
        save-schema number. If a doc claims a *current* schema number
        that disagrees with it (e.g. "Current schema: 34" when the code
        declares `CurrentVersion = 35`), the doc has drifted. A bare
        "v34" inside the historical-rollup entries of CHANGELOG.md is
        not drift; only the prose pattern that claims present-state
        schema is.
      * CI exists at `.github/workflows/ci.yml`. If
        `docs/engineering/conventions.md` still claims "no CI" /
        "no linter or CI", the conventions doc has drifted from
        reality and must be updated.
      * `docs/session-state/STATE.txt` exists, is non-empty, and parses
        a record that names `WorldSave.CurrentVersion = <N>` matching
        the code constant. A broken session-state machine-capture is
        itself a drift signal the rest of the repo depends on.

    The script's failure modes are the things issue #57 explicitly
    listed: stale schema, vanished CI, and missing state baseline.

.EXAMPLE
    pwsh ./tools/Test-DocsConsistency.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

$persistenceFile = Join-Path $repoRoot "src/WorldofGoses.Persistence/WorldSave.cs"
$worldSource = Get-Content -Raw -LiteralPath $persistenceFile
if ($worldSource -notmatch 'public const int CurrentVersion\s*=\s*(\d+)\s*;') {
    Write-Error ("WorldSave.cs must declare 'public const int CurrentVersion = <N>;' " +
        "for this guard to work. Add it before merging.")
    exit 1
}
$currentVersion = [int]$Matches[1]

$conventionsFile = Join-Path $repoRoot "docs/engineering/conventions.md"
$stateFile = Join-Path $repoRoot "docs/session-state/STATE.txt"
$ciFile = Join-Path $repoRoot ".github/workflows/ci.yml"
$changelogFile = Join-Path $repoRoot "CHANGELOG.md"

$errors = New-Object System.Collections.Generic.List[string]

# ── Check 1: any doc claiming a present-state schema number that disagrees ──
# Look for prose of the shape "Current schema: N", "Schema actual: N",
# "Schema vigente = N" — only the *label* half needs to be tight;
# the trailing prose may continue with extra context after the number,
# because real prose rarely terminates a "Current schema: 34 (per the
# legacy manifest)" line on the digit. CHANGELOG.md historical-rollup
# headers like "**2026-...· schema v32 (sin cambio)..." do not match
# because they do not assert a *present* state, only a past one.

$docFiles = @(
    $conventionsFile,
    (Join-Path $repoRoot "docs/README.md"),
    (Join-Path $repoRoot "docs/session-state/README.md"),
    (Join-Path $repoRoot "docs/history/decisions.md"),
    $changelogFile
) | Where-Object { Test-Path -LiteralPath $_ }

foreach ($file in $docFiles) {
    $lines = Get-Content -LiteralPath $file
    foreach ($i in 0..($lines.Count - 1)) {
        $line = $lines[$i]
        # Match an explicit "current/actual/vigente schema" label or a
        # bare "Schema: N" / "Schema vigente: N" line. Whitespace and
        # an optional leading bullet/indent are tolerated.
        $labelMatch = $line -match '(?i)^\s*(?:[-*]\s*)?(?:current|actual|vigente|currently active)\s+schema\b\s*:?\s*'
        $bareMatch  = $line -match '(?im)^\s*(?:[-*]\s*)?schema\s*(?:actual|vigente)?\s*:\s*'
        if (-not ($labelMatch -or $bareMatch)) { continue }
        # Now extract the FIRST number on the same line. Prose that
        # continues past the number (e.g. "Current schema: 34 (legacy)")
        # is still caught because the digit is what matters.
        $numberMatch = [regex]::Match($line, '(?:v)?(\d+)')
        if (-not $numberMatch.Success) { continue }
        $claimed = [int]$numberMatch.Groups[1].Value
        if ($claimed -ne $currentVersion) {
            $rel = $file.Substring($repoRoot.Path.Length).TrimStart('\','/')
            $lineNumber = $i + 1
            $errors.Add("$rel`:$lineNumber claims schema $claimed, but WorldSave.CurrentVersion is $currentVersion.")
        }
    }
}

# ── Check 2: the conventions doc must not deny CI ─────────────────────────
if (-not (Test-Path -LiteralPath $ciFile)) {
    $errors.Add(".github/workflows/ci.yml is missing; CI is the contract this script guards.")
}
$conventions = Get-Content -Raw -LiteralPath $conventionsFile
# Match the legacy "no CI" / "no linter or CI" phrasing on its own line. The
# replacement paragraph in conventions.md §3 names every step of the
# workflow and does not contain the denial phrase, so this regex has one
# false-positive risk: a future reviewer notes "previously no CI". We keep
# it tight by anchoring to "configured yet" or "no linter or CI".
if ($conventions -match '(?im)^\s*(?:[-*]?\s*)?there is no\s+(?:ci|linter or ci|linter nor ci)\b') {
    $rel = $conventionsFile.Substring($repoRoot.Path.Length).TrimStart('\','/')
    $errors.Add("$rel still asserts there is no CI. Update §3 to point at .github/workflows/ci.yml.")
}

# ── Check 3: session state baseline must exist and pin the schema ──────────
if (-not (Test-Path -LiteralPath $stateFile)) {
    $errors.Add("docs/session-state/STATE.txt is missing; rerun tools/New-SessionSnapshot.ps1 -Mode Full.")
}
else {
    $stateRaw = Get-Content -Raw -LiteralPath $stateFile
    if ([string]::IsNullOrWhiteSpace($stateRaw)) {
        $errors.Add("docs/session-state/STATE.txt is empty; rerun tools/New-SessionSnapshot.ps1 -Mode Full.")
    }
    elseif ($stateRaw -notmatch "WorldSave\.CurrentVersion\s*=\s*$currentVersion\b") {
        $errors.Add("docs/session-state/STATE.txt does not pin WorldSave.CurrentVersion = $currentVersion " +
            "(it should be regenerated by tools/New-SessionSnapshot.ps1 -Mode Full).")
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output ("Docs consistency OK: schema $currentVersion, CI surface present, state baseline pinned. " +
    "($($errors.Count) checks, $($docFiles.Count) docs inspected)")

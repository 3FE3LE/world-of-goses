<#
.SYNOPSIS
    The canonical "import every game asset with Godot before xUnit runs" step.

.DESCRIPTION
    xUnit guard #54 fails on a fresh clone because the
    `PromotedSpeedGlyphs_AreImportedTintableAndNativelyTwentyFourPixels`
    test asserts that the three speed-glyph SVGs have an `.import`
    sibling on disk. .gitignore correctly excludes `**/*.import` from
    source control — the contract here is "the editor regenerates them
    on first open", not "they live in the repo". CI therefore has to
    trigger that regeneration itself before xUnit runs, otherwise the
    test fails for the same reason every clean clone does: there is no
    prior Godot session on the runner.

    This script runs Godot 4.7 headless against `game/`, lets the
    project load (the import pipeline runs at project load), gives it
    one tick so any deferred import work settles, and then exits. The
    three promoted `.import` siblings are checked into existence; if
    any of them is missing afterwards, the script fails and the CI job
    goes red before xUnit ever sees the green-looking state.

    Mirrors the [CmdletBinding()] + $ErrorActionPreference style of
    Test-GodotBoot.ps1; resolution order for the Godot binary is the
    same, so local and CI invocations stay equivalent.

.PARAMETER GodotPath
    Godot 4.7.1 .NET binary. When omitted, resolution falls back to
    $env:GODOT (which chickensoft-games/setup-godot@v1.5.6 exports),
    the repository's documented local install path, and finally `godot`
    on PATH.

.PARAMETER LogDirectory
    Where to capture the importer's stdout/stderr. Defaults to a temp
    directory. The log is always written, and its path is always
    reported, so a CI failure can be diagnosed without re-running
    locally.

.EXAMPLE
    pwsh ./tools/Import-GameAssets.ps1
#>
[CmdletBinding()]
param(
    [string]$GodotPath,
    [string]$LogDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$gameDirectory = Join-Path $repoRoot "game"
$projectFile = Join-Path $gameDirectory "project.godot"

$promotedRelative = @(
    "game/assets/ui/icons/24/speed-slow.svg.import",
    "game/assets/ui/icons/24/speed-medium.svg.import",
    "game/assets/ui/icons/24/speed-fast.svg.import"
)

$failures = [System.Collections.Generic.List[string]]::new()

# Godot resolution — explicit argument, $GODOT env var the setup
# action exports, the repo's documented local install path, then PATH.
# An importer that silently does nothing because it could not find the
# engine is worse than no importer, so an unresolvable engine is a
# failure, never a skip.
$candidates = @(
    $GodotPath,
    $env:GODOT,
    "C:\Tools\Godot\Godot_v4.7.1-stable_mono_win64.exe"
) | Where-Object { $_ }

$resolvedGodot = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $resolvedGodot) {
    $onPath = Get-Command "godot" -ErrorAction SilentlyContinue
    if ($onPath) { $resolvedGodot = $onPath.Source }
}
if (-not $resolvedGodot) {
    Write-Error ("FAILED: no Godot binary found. Tried: " +
        (($candidates + "godot (PATH)") -join ", "))
    exit 1
}
$GodotPath = $resolvedGodot
if (-not (Test-Path -LiteralPath $projectFile)) {
    Write-Error "FAILED: project.godot not found at $projectFile"
    exit 1
}

if (-not $LogDirectory) {
    $LogDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "wog-asset-import"
}
if (-not (Test-Path -LiteralPath $LogDirectory)) {
    New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
}
$stdoutPath = Join-Path $LogDirectory "import-stdout.log"
$stderrPath = Join-Path $LogDirectory "import-stderr.log"

# Godot 4.7's import pipeline runs at project load, before the main
# scene composes. `--headless --editor --quit` opens the project in
# headless editor mode (which performs the full import scan), then
# exits. `--quit-after N` would let the scene compose and is the
# wrong shape for an import-only run — `--quit` is what stops the
# engine as soon as the import queue drains.
$arguments = @("--headless", "--editor", "--path", $gameDirectory, "--quit")

try {
    $process = Start-Process -FilePath $GodotPath `
        -ArgumentList $arguments `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -NoNewWindow `
        -PassThru

    # The importer can take a few seconds on a cold runner; ten minutes
    # is well past any reasonable ceiling and short enough to keep CI
    # responsive when something is genuinely stuck.
    if (-not $process.WaitForExit(600000)) {
        try { $process.Kill($true) } catch { }
        $failures.Add("importer did not finish within 10 minutes (hung)")
    }
    elseif ($process.ExitCode -ne 0) {
        $failures.Add("importer exited with code $($process.ExitCode)")
    }
}
catch {
    $failures.Add("importer could not be launched: $($_.Exception.Message)")
}

foreach ($relative in $promotedRelative) {
    $absolute = Join-Path $repoRoot $relative
    if (-not (Test-Path -LiteralPath $absolute)) {
        $failures.Add("$relative was not produced by the importer")
    }
}

if ($failures.Count -gt 0) {
    $detail = ($failures | ForEach-Object { "  - $_" }) -join "`n"
    Write-Error "FAILED: canonical asset import`n$detail`n  log: $stdoutPath"
    exit 1
}

Write-Output ("Canonical import produced $($promotedRelative.Count) promoted speed-glyph `.import` siblings. " +
    "log: $stdoutPath")

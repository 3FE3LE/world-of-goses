<#
.SYNOPSIS
    The canonical "does the real game still boot?" check.

.DESCRIPTION
    Architecture Hardening A9 shipped a regression in which the build was
    green, 1323 xUnit tests passed and every architecture guard held, while
    the actual game could not start: a recursive `_Ready` path overflowed the
    stack the moment Godot composed the production scene. Nothing in the
    verification pipeline touched Godot's lifecycle, so nothing noticed.

    This script is that missing edge. It launches the real production main
    scene, headless, with the visual-capture harness deliberately OFF, lets
    the scene tree run for a bounded number of frames, and fails on anything
    that would have caught A9:

      * abnormal process exit, including process-level kills such as a
        stack overflow, which .NET reports as 0xC00000FD and which never
        surfaces as a managed exception;
      * `ERROR:` / `SCRIPT ERROR:` lines, the two prefixes Godot reserves
        for real failures;
      * an unhandled managed exception in the .NET half;
      * the production scene never reaching its startup path at all — a boot
        that exits 0 having composed nothing is not a boot.

    There is deliberately ONE of these. `.github/workflows/ci.yml` and
    `tools/New-SessionSnapshot.ps1 -Mode Full` both call this file rather
    than each keeping their own slightly different idea of "boots".

.PARAMETER GodotPath
    Godot 4.7.1 .NET binary. When omitted, resolution falls back to the
    $GODOT environment variable (which chickensoft-games/setup-godot defines
    on CI) and then to `godot` on PATH, so the same invocation works on a
    developer machine and on a runner.

.PARAMETER Frames
    How many frames to let the scene tree run before quitting. The default is
    generous enough to cover `_Ready`, the first `_Process` passes, the
    deferred calls those queue, and the first autosave tick — A9's overflow
    happened during composition, but a regression that only bites on the
    second frame is just as fatal to a player.

.PARAMETER TimeoutSeconds
    Hard ceiling. A boot that hangs is a failed boot; without this the check
    would block CI instead of failing it.

.PARAMETER LogDirectory
    Where to leave the captured stdout/stderr. Defaults to a temp directory.
    The log is always written, and its path is always reported, so a CI
    failure can be diagnosed without re-running locally.

.EXAMPLE
    pwsh ./tools/Test-GodotBoot.ps1
#>
[CmdletBinding()]
param(
    [string]$GodotPath,

    [int]$Frames = 120,

    [int]$TimeoutSeconds = 120,

    [string]$LogDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$gameDirectory = Join-Path $repoRoot "game"
$projectFile = Join-Path $gameDirectory "project.godot"

# The production main scene. Asserted rather than passed on the command
# line: the point of the check is that the scene the *player* gets still
# boots, so the script must not be able to smoke-test some other scene and
# still report success.
$expectedMainScene = "res://scenes/CityPrototype.tscn"

# CityPrototype._Ready prints this before it composes anything else. Its
# absence means the engine started but the production scene never ran.
$startupMarker = "World of Goses prototype starting."

$failures = [System.Collections.Generic.List[string]]::new()

function Write-Result {
    param([string]$Summary, [bool]$Ok)
    Write-Output $Summary
    if (-not $Ok) { exit 1 }
    exit 0
}

# Resolution order: explicit argument, the $GODOT variable CI's Godot setup
# action defines, the repository's documented local install path, then PATH.
# A boot check that silently does nothing because it could not find the
# engine is worse than no boot check, so an unresolvable engine is a failure,
# never a skip.
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
    Write-Result ("FAILED: no Godot binary found. Tried: " +
        (($candidates + "godot (PATH)") -join ", ")) $false
}
$GodotPath = $resolvedGodot
if (-not (Test-Path -LiteralPath $projectFile)) {
    Write-Result "FAILED: project.godot not found at $projectFile" $false
}

$mainSceneLine = Select-String -LiteralPath $projectFile -Pattern '^run/main_scene="(.+)"$' |
    Select-Object -First 1
if (-not $mainSceneLine) {
    Write-Result "FAILED: project.godot declares no run/main_scene" $false
}
$declaredMainScene = $mainSceneLine.Matches[0].Groups[1].Value
if ($declaredMainScene -ne $expectedMainScene) {
    Write-Result ("FAILED: main scene is '$declaredMainScene', expected '$expectedMainScene'. " +
        "Update Test-GodotBoot.ps1 deliberately if the production entry point really moved.") $false
}

if (-not $LogDirectory) {
    $LogDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "wog-boot-smoke"
}
if (-not (Test-Path -LiteralPath $LogDirectory)) {
    New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
}
$stdoutPath = Join-Path $LogDirectory "boot-stdout.log"
$stderrPath = Join-Path $LogDirectory "boot-stderr.log"

# Normal boot means normal boot. The harness reads WOG_VISUAL_CAPTURE from
# the environment, so an operator who happened to leave it set from a capture
# run must not silently turn this into a fixture boot. Cleared for the child
# process only; the caller's environment is restored afterwards.
$previousCaptureFlag = $env:WOG_VISUAL_CAPTURE
$env:WOG_VISUAL_CAPTURE = $null

# No --wog-visual-capture, no --wog-visual-fixture, no scene override.
$arguments = @("--headless", "--path", $gameDirectory, "--quit-after", "$Frames")

try {
    $process = Start-Process -FilePath $GodotPath `
        -ArgumentList $arguments `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -NoNewWindow `
        -PassThru

    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        try { $process.Kill($true) } catch { }
        $failures.Add("boot did not finish within ${TimeoutSeconds}s (hung)")
        $exitCode = $null
    }
    else {
        $exitCode = $process.ExitCode
    }
}
finally {
    $env:WOG_VISUAL_CAPTURE = $previousCaptureFlag
}

$stdout = if (Test-Path -LiteralPath $stdoutPath) { Get-Content -LiteralPath $stdoutPath -Raw } else { "" }
$stderr = if (Test-Path -LiteralPath $stderrPath) { Get-Content -LiteralPath $stderrPath -Raw } else { "" }
if ($null -eq $stdout) { $stdout = "" }
if ($null -eq $stderr) { $stderr = "" }
$combined = "$stdout`n$stderr"

if ($null -ne $exitCode -and $exitCode -ne 0) {
    # A9 died this way. A managed stack overflow cannot be caught and never
    # reaches an exception handler; the CLR tears the process down and
    # Windows reports STATUS_STACK_OVERFLOW. Naming it explicitly is the
    # difference between "CI is red for some reason" and "you reintroduced
    # the A9 recursion".
    $named = switch ($exitCode) {
        -1073741571 { " (STATUS_STACK_OVERFLOW — the A9 failure mode)" }
        -1073741819 { " (STATUS_ACCESS_VIOLATION)" }
        -1073740791 { " (STATUS_STACK_BUFFER_OVERRUN)" }
        default { "" }
    }
    $failures.Add("process exited with $exitCode$named")
}

if ($combined -match "(?im)^\s*Stack overflow\.?\s*$" -or $combined -match "StackOverflowException") {
    $failures.Add("stack overflow reported in the boot log")
}

$engineErrors = @([regex]::Matches($combined, "(?m)^(ERROR|SCRIPT ERROR):.*"))
if ($engineErrors.Count -gt 0) {
    $first = $engineErrors[0].Value.Trim()
    $failures.Add("$($engineErrors.Count) engine/script error(s), first: $first")
}

$managedExceptions = @([regex]::Matches(
    $combined,
    "(?m)^.*(Unhandled exception|System\.[A-Za-z.]*Exception:).*"))
if ($managedExceptions.Count -gt 0) {
    $first = $managedExceptions[0].Value.Trim()
    $failures.Add("unhandled managed exception, first: $first")
}

if ($combined -notmatch [regex]::Escape($startupMarker)) {
    $failures.Add("production scene never reached its startup path (marker '$startupMarker' absent)")
}

if ($failures.Count -gt 0) {
    $detail = ($failures | ForEach-Object { "  - $_" }) -join "`n"
    Write-Result "FAILED: normal Godot boot`n$detail`n  log: $stdoutPath" $false
}

Write-Result "boots clean (main scene $expectedMainScene, $Frames frames, capture off)" $true

<#
.SYNOPSIS
    Records the state of the game at the start of a working session.

.DESCRIPTION
    Writes docs/session-state/STATE.txt with the facts a fresh session needs
    before it touches code: which commit it inherited, whether the working tree
    was left dirty, the active increment, the persisted save schema version and
    the build/test baseline. In -Mode Full it also captures a dated screenshot
    of the running macro view into docs/session-state/.

    Two modes exist because the two halves have very different costs.

    Fast (the SessionStart hook) reads git and the source tree only. It never
    launches dotnet or Godot, so it cannot delay a session start or steal the
    desktop focus. Every measured field it cannot verify is written as
    "not measured this session" rather than copied forward from the previous
    file: a state document that silently restates a stale test count is worse
    than one that admits it does not know.

    Full (before the session's first commit) measures everything and captures
    the screenshot. It is the mode whose output is meant to be committed.

    The script never throws. A failing probe is recorded as a failing probe and
    the remaining probes still run, because the reason to generate this file is
    usually that something is broken.

.PARAMETER Mode
    Fast reads git and source only. Full also runs build, tests, the headless
    boot, the context validators and the screenshot capture.

.PARAMETER GodotPath
    Godot 4.7.1 .NET binary, used by the headless boot and the capture.

.PARAMETER SkipCapture
    Run every Full probe but leave the screenshot alone. Use on a machine with
    no interactive desktop: the capture harness needs a real window.

.EXAMPLE
    pwsh ./tools/New-SessionSnapshot.ps1 -Mode Fast

.EXAMPLE
    pwsh ./tools/New-SessionSnapshot.ps1 -Mode Full
#>
[CmdletBinding()]
param(
    [ValidateSet("Fast", "Full")]
    [string]$Mode = "Fast",

    [string]$GodotPath = "C:\Tools\Godot\Godot_v4.7.1-stable_mono_win64.exe",

    [string]$OutputDirectory,

    [string]$Date,

    [switch]$SkipCapture
)

# Deliberately Continue, not Stop. This script reports on a repository that may
# be mid-breakage; an unreadable probe must degrade to one bad line in the
# report, never abort the report.
$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot "docs\session-state"
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

if (-not $Date) {
    $Date = [DateTime]::Now.ToString("yyyy-MM-dd")
}

$unmeasured = "not measured this session (-Mode Fast)"
$notes = New-Object System.Collections.Generic.List[string]

function Invoke-Probe {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][scriptblock]$Body,
        [string]$OnFailure = "probe failed"
    )
    try {
        return & $Body
    }
    catch {
        $notes.Add("$Name : $OnFailure - $($_.Exception.Message)")
        return "unavailable"
    }
}

function Invoke-Capture {
    param(
        [Parameter(Mandatory = $true)][string]$Command,
        [string[]]$CommandArgs = @(),
        [string]$WorkingDirectory = $repoRoot
    )
    Push-Location $WorkingDirectory
    try {
        # 2>&1 keeps MSBuild/xUnit diagnostics in the same stream we parse, so a
        # failure surfaces its own reason instead of an empty match.
        $output = & $Command @CommandArgs 2>&1 | Out-String
        return [PSCustomObject]@{ ExitCode = $LASTEXITCODE; Output = $output }
    }
    finally {
        Pop-Location
    }
}

# ---------------------------------------------------------------------------
# Repository
# ---------------------------------------------------------------------------

$branch = Invoke-Probe "git branch" { (git -C $repoRoot rev-parse --abbrev-ref HEAD).Trim() }
$head = Invoke-Probe "git head" { (git -C $repoRoot rev-parse --short HEAD).Trim() }
$headSubject = Invoke-Probe "git subject" { (git -C $repoRoot log -1 --format=%s).Trim() }
$headDate = Invoke-Probe "git date" { (git -C $repoRoot log -1 --format=%cI).Trim() }

$workingTree = Invoke-Probe "git status" {
    $dirty = @(git -C $repoRoot status --porcelain)
    if ($dirty.Count -eq 0) { "clean" } else { "$($dirty.Count) modified or untracked paths" }
}

# ---------------------------------------------------------------------------
# Game
# ---------------------------------------------------------------------------

$schemaVersion = Invoke-Probe "save schema" {
    $savePath = Join-Path $repoRoot "game\scripts\Domain\Persistence\WorldSave.cs"
    $match = Select-String -LiteralPath $savePath -Pattern "CurrentVersion\s*=\s*(\d+)" | Select-Object -First 1
    if (-not $match) { throw "CurrentVersion not found in WorldSave.cs" }
    "WorldSave.CurrentVersion = $($match.Matches[0].Groups[1].Value)"
}

$activeIncrement = Invoke-Probe "active increment" {
    $statusPath = Join-Path $repoRoot "docs\CURRENT_STATUS.md"
    $match = Select-String -LiteralPath $statusPath -Pattern "^\*\*Active increment:\*\*\s*(.+)$" | Select-Object -First 1
    if (-not $match) { throw "Active increment not found in CURRENT_STATUS.md" }
    $match.Matches[0].Groups[1].Value.Trim()
}

# ---------------------------------------------------------------------------
# Baseline
# ---------------------------------------------------------------------------

$buildResult = $unmeasured
$testResult = $unmeasured
$bootResult = $unmeasured
$contextResult = $unmeasured
$localeResult = $unmeasured
$captureLines = @("$unmeasured")

if ($Mode -eq "Full") {
    # The dotnet CLI localizes its summary, so "0 Advertencia(s)" and
    # "0 Warning(s)" both occur on developer machines here. Pin the CLI to
    # English for the duration of this process so the regexes below have one
    # shape to match instead of one per installed language pack.
    $env:DOTNET_CLI_UI_LANGUAGE = "en"

    $buildResult = Invoke-Probe "build" {
        $run = Invoke-Capture "dotnet" @("build") (Join-Path $repoRoot "game")
        $errors = [regex]::Match($run.Output, "(\d+)\s+Error\(s\)")
        $warnings = [regex]::Match($run.Output, "(\d+)\s+Warning\(s\)")
        if ($errors.Success -and $warnings.Success) {
            "$($errors.Groups[1].Value) errors, $($warnings.Groups[1].Value) warnings"
        }
        elseif ($run.ExitCode -eq 0) { "succeeded (summary not parsed)" }
        else { "FAILED (exit $($run.ExitCode))" }
    } "could not run dotnet build"

    $testResult = Invoke-Probe "tests" {
        $run = Invoke-Capture "dotnet" @("test") (Join-Path $repoRoot "tests\WorldofGoses.Tests")
        $summary = [regex]::Match($run.Output, "Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)")
        if ($summary.Success) {
            "$($summary.Groups[2].Value) passed, $($summary.Groups[1].Value) failed, " +
            "$($summary.Groups[3].Value) skipped, $($summary.Groups[4].Value) total"
        }
        elseif ($run.ExitCode -eq 0) { "passed (summary not parsed)" }
        else { "FAILED (exit $($run.ExitCode))" }
    } "could not run dotnet test"

    $bootResult = Invoke-Probe "headless boot" {
        if (-not (Test-Path -LiteralPath $GodotPath)) { throw "Godot not found at $GodotPath" }
        $run = Invoke-Capture $GodotPath @("--headless", "--path", "game", "--quit-after", "3")
        # A clean boot still prints informational lines, so match the two
        # prefixes Godot reserves for real failures rather than the word
        # "error" anywhere in the log.
        $failures = @([regex]::Matches($run.Output, "(?m)^(ERROR|SCRIPT ERROR):"))
        if ($run.ExitCode -ne 0) { "FAILED (exit $($run.ExitCode))" }
        elseif ($failures.Count -gt 0) { "boots, but reported $($failures.Count) engine or script errors" }
        else { "OK (no C# or scene errors)" }
    } "could not run the headless boot"

    $contextResult = Invoke-Probe "agent context" {
        $run = Invoke-Capture "pwsh" @("-NoProfile", "-File", (Join-Path $repoRoot "scripts\Validate-AgentContext.ps1"))
        $passed = [regex]::Match($run.Output, "Passed:\s*(\d+)")
        $failed = [regex]::Match($run.Output, "Failed:\s*(\d+)")
        if ($passed.Success -and $failed.Success) {
            "$($passed.Groups[1].Value) checks passed, $($failed.Groups[1].Value) failed"
        }
        elseif ($run.ExitCode -eq 0) { "passed (summary not parsed)" }
        else { "FAILED (exit $($run.ExitCode))" }
    } "could not run Validate-AgentContext.ps1"

    $localeResult = Invoke-Probe "localization" {
        $run = Invoke-Capture "pwsh" @("-NoProfile", "-File", (Join-Path $repoRoot "tools\Test-LocalizationCatalog.ps1"))
        $summary = [regex]::Match($run.Output, "(\d+)\s+template IDs,\s*(\d+)\s+runtime keys")
        if ($summary.Success) { "$($summary.Groups[1].Value) template IDs, $($summary.Groups[2].Value) runtime keys" }
        elseif ($run.ExitCode -eq 0) { "valid (summary not parsed)" }
        else { "FAILED (exit $($run.ExitCode))" }
    } "could not run Test-LocalizationCatalog.ps1"

    # -----------------------------------------------------------------------
    # Capture
    # -----------------------------------------------------------------------

    if ($SkipCapture) {
        $captureLines = @("skipped (-SkipCapture)")
    }
    else {
        $captureLines = Invoke-Probe "capture" {
            if (-not (Test-Path -LiteralPath $GodotPath)) { throw "Godot not found at $GodotPath" }
            $stagingDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "wog-session-$Date"
            & (Join-Path $repoRoot "tools\Capture-VisualMatrix.ps1") `
                -GodotPath $GodotPath `
                -OutputDirectory $stagingDirectory `
                -StateName "session" | Out-Null

            # The harness always captures both official resolutions, but only
            # 1280x720 is committed. 1920x1080 is the same frame at a different
            # scale, and this repository has no Git LFS: a second PNG per
            # session would double permanent history growth to prove nothing
            # the baseline does not already prove. The full pair stays in the
            # staging directory for the visual-regression review.
            $source = Join-Path $stagingDirectory "session-1280x720.png"
            if (-not (Test-Path -LiteralPath $source)) { throw "the harness produced no 1280x720 frame" }

            $destination = Join-Path $OutputDirectory "$Date-macro-1280x720.png"
            Copy-Item -LiteralPath $source -Destination $destination -Force
            $bytes = (Get-Item -LiteralPath $destination).Length

            @(
                "docs/session-state/$Date-macro-1280x720.png ($([math]::Round($bytes / 1KB)) KB)"
                "1920x1080 frame and manifest: $stagingDirectory (review artifact, not committed)"
                "live slot as fixture, WOG_VISUAL_CAPTURE=1, no persistence writes"
            )
        } "no screenshot was produced"

        if ($captureLines -eq "unavailable") {
            $captureLines = @(
                "FAILED - no screenshot this session"
                "the harness needs an interactive desktop and can report a 50x50 client (docs/VISUAL_REGRESSION.md)"
            )
        }
    }
}

# ---------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------

$report = New-Object System.Collections.Generic.List[string]
$report.Add("World of Goses - session state")
$report.Add("==============================")
$report.Add("")
$report.Add("Generated by tools/New-SessionSnapshot.ps1 -Mode $Mode")
$report.Add("Generated at $([DateTimeOffset]::Now.ToString('yyyy-MM-ddTHH:mm:sszzz'))")
$report.Add("")
$report.Add("This file is generated. Do not hand-edit it: the next session start")
$report.Add("overwrites it. Narrative history belongs in CHANGELOG.md, the design")
$report.Add("intent in docs/CURRENT_STATUS.md.")
$report.Add("")
$report.Add("Repository")
$report.Add("----------")
$report.Add("Branch          : $branch")
$report.Add("HEAD            : $head")
$report.Add("HEAD subject    : $headSubject")
$report.Add("HEAD committed  : $headDate")
$report.Add("Working tree    : $workingTree")
$report.Add("")
$report.Add("Game")
$report.Add("----")
$report.Add("Active increment: $activeIncrement")
$report.Add("Save schema     : $schemaVersion")
$report.Add("")
$report.Add("Baseline")
$report.Add("--------")
$report.Add("Build           : $buildResult")
$report.Add("Tests           : $testResult")
$report.Add("Headless boot   : $bootResult")
$report.Add("Agent context   : $contextResult")
$report.Add("Localization    : $localeResult")
$report.Add("")
$report.Add("Capture")
$report.Add("-------")
foreach ($line in @($captureLines)) {
    $report.Add("  $line")
}

if ($notes.Count -gt 0) {
    $report.Add("")
    $report.Add("Probe notes")
    $report.Add("-----------")
    foreach ($note in $notes) {
        $report.Add("  $note")
    }
}

$report.Add("")

$statePath = Join-Path $OutputDirectory "STATE.txt"
[System.IO.File]::WriteAllText($statePath, ($report -join "`r`n"), [System.Text.UTF8Encoding]::new($false))

Write-Host ($report -join [Environment]::NewLine)

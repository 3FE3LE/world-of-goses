#requires -Version 7.0
<#
.SYNOPSIS
  Recommend a verification plan for the current git diff.

.DESCRIPTION
  Reads `git diff --name-only` and emits a structured recommendation:
    - risk tier (LOW / MEDIUM / HIGH)
    - workflow mode (SURGICAL / FEATURE / RELEASE)
    - required commands (build, test filter, headless, visual fixture, …)
    - skipped commands (no localization, no agent validation, …)
    - review depth (none / PRESENTATION_REVIEW / DOMAIN_REVIEW / SYSTEM_REVIEW)

  The script is deterministic — no LLM is involved. It applies the
  path-to-rule mapping documented in
  docs/ai/WORKFLOW_MODES.md and docs/ai/RISK_MODEL.md.

  Use as a tie-breaker when the classification is ambiguous, not as a
  replacement for the agent's judgment. The agent may escalate when
  the script cannot.

.PARAMETER BaseRef
  The git ref to diff against. Defaults to HEAD.

.PARAMETER HeadRef
  The git ref to diff to. Defaults to the working tree (no second
  ref means `git diff --name-only`).

.PARAMETER Json
  Emit machine-readable JSON instead of human-readable text.

.PARAMETER RepoRoot
  Repository root. Defaults to the parent of the script directory.

.EXAMPLE
  pwsh ./tools/Get-VerificationPlan.ps1

.EXAMPLE
  pwsh ./tools/Get-VerificationPlan.ps1 -BaseRef origin/main -Json
#>
[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef = '',
    [switch]$Json,
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-GitPath {
    param([string]$Path, [string]$Root)
    if ([string]::IsNullOrEmpty($Path)) { return $Root }
    if (-not [System.IO.Path]::IsPathRooted($Path)) {
        return (Join-Path $Root $Path)
    }
    return $Path
}

function Get-DiffPaths {
    [CmdletBinding()]
    param(
        [string]$Root,
        [string]$Base,
        [string]$Head
    )

    Push-Location -LiteralPath $Root
    try {
        $gitArgs = @('diff', '--name-only', $Base)
        if (-not [string]::IsNullOrEmpty($Head)) {
            $gitArgs += $Head
        }
        $raw = & git @gitArgs 2>$null
        if ($LASTEXITCODE -ne 0) { return @() }
        return @($raw | Where-Object { $_ })
    } finally {
        Pop-Location
    }
}

function Test-AnyMatch {
    param(
        [string[]]$Paths,
        [string[]]$Patterns
    )
    foreach ($p in $Paths) {
        foreach ($pat in $Patterns) {
            if ($p -like $pat) { return $true }
        }
    }
    return $false
}

function Merge-Set {
    param([string[]]$A, [string[]]$B)
    $set = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($x in @($A) + @($B)) {
        if (-not [string]::IsNullOrWhiteSpace($x)) {
            [void]$set.Add($x)
        }
    }
    return @($set)
}

# -- 1. Collect the diff ---------------------------------------------------

$repo = Resolve-GitPath -Path $RepoRoot -Root (Get-Location).Path
$paths = Get-DiffPaths -Root $repo -Base $BaseRef -Head $HeadRef

if (-not $paths -or $paths.Count -eq 0) {
    $result = [pscustomobject]@{
        Mode       = 'NONE'
        Risk       = 'NONE'
        Changed    = @()
        Required   = @()
        Skipped    = @()
        Review     = 'none'
        Notes      = 'No changes detected between the given refs.'
    }
    if ($Json) { $result | ConvertTo-Json -Depth 5 }
    else { Write-Host 'No changes detected.' }
    return
}

# -- 2. Path-to-rule classification ----------------------------------------

$domainHits         = Test-AnyMatch -Paths $paths -Patterns @(
    'game/scripts/Domain/*',
    'game/scripts/Domain/**'
)
$saveSchemaHits     = Test-AnyMatch -Paths $paths -Patterns @(
    'game/scripts/Domain/**/WorldSave.cs',
    'game/scripts/Domain/*Save.cs',
    'game/scripts/Domain/**/*Save.cs'
)
$persistenceHits    = Test-AnyMatch -Paths $paths -Patterns @(
    'game/scripts/Domain/**/WorldPersistence.cs',
    'game/scripts/Domain/**/WorldPersistence/**',
    'game/scripts/Domain/**/WorldMigration*'
)
$architectureHits   = Test-AnyMatch -Paths $paths -Patterns @(
    'docs/engineering/architecture.md',
    'docs/engineering/state-authority.md'
)
$localeHits         = Test-AnyMatch -Paths $paths -Patterns @(
    '*.po',
    '*.pot',
    'game/locale/*',
    'game/locale/**'
)
$agentHits          = Test-AnyMatch -Paths $paths -Patterns @(
    '.agents/*',
    '.agents/**',
    '.claude/*',
    '.claude/**',
    '.codex/*',
    '.codex/**',
    'AGENTS.md',
    'CLAUDE.md',
    'docs/ai/*',
    'docs/ai/**',
    'scripts/*',
    'scripts/**',
    'tools/*',
    'tools/**',
    'Install-GodotDotNetSkills.ps1'
)
$uiHits             = Test-AnyMatch -Paths $paths -Patterns @(
    'game/scripts/Ui/*',
    'game/scripts/Ui/**',
    'game/scripts/*Panel.cs',
    'game/scripts/*View.cs',
    'game/scenes/*',
    'game/scenes/**',
    'game/scripts/visual/*',
    'game/scripts/visual/**'
)
$domainSpecificHits = Test-AnyMatch -Paths $paths -Patterns @(
    'game/scripts/Domain/Citizen/*',
    'game/scripts/Domain/Citizen/**',
    'game/scripts/Domain/City/*',
    'game/scripts/Domain/City/**',
    'game/scripts/Domain/Expedition/*',
    'game/scripts/Domain/Expedition/**'
)
$testHits           = Test-AnyMatch -Paths $paths -Patterns @(
    'tests/*',
    'tests/**'
)
$assetHits          = Test-AnyMatch -Paths $paths -Patterns @(
    'art/source/*',
    'art/source/**',
    'art/exports/*',
    'art/exports/**',
    'game/assets/*',
    'game/assets/**'
)
$docHits            = Test-AnyMatch -Paths $paths -Patterns @(
    'docs/*',
    'docs/**'
)
$csprojHits         = Test-AnyMatch -Paths $paths -Patterns @(
    'game/*.csproj',
    'game/*.sln',
    'game/project.godot'
)
$dependencyHits     = $csprojHits

# -- 3. Risk tier ---------------------------------------------------------

$reasons = New-Object System.Collections.Generic.List[string]

$risk = $null

if ($saveSchemaHits) {
    $risk = 'HIGH'; $reasons.Add('save schema / migration touched')
}
if ($persistenceHits -and $risk -ne 'HIGH') {
    $risk = 'HIGH'; $reasons.Add('persistence code touched')
}
if ($dependencyHits -and $risk -ne 'HIGH') {
    $risk = 'HIGH'; $reasons.Add('project / dependency files touched')
}
if ($architectureHits -and $risk -ne 'HIGH') {
    $risk = 'HIGH'; $reasons.Add('architecture / boundary doc touched')
}

# Domain classification:
# - 2+ subtrees touched → HIGH
# - root-level Domain file (e.g. WorldSave.cs at the layer root) → HIGH
# - 1 subtree touched → MEDIUM (FEATURE / DOMAIN_REVIEW)
# @() is load-bearing: Where-Object yields $null when nothing matches, and the
# `-eq 0` branch below — the root-level Domain case — could therefore never be
# reached, because reading .Count on $null throws first. A change touching only
# game/scripts/Domain/*.cs crashed the planner instead of being classified HIGH.
$domainSubtrees = @(@('Citizen', 'City', 'Expedition') | Where-Object {
    Test-AnyMatch -Paths $paths -Patterns @(
        "game/scripts/Domain/$_/*",
        "game/scripts/Domain/$_/**"
    )
})
if ($domainSubtrees.Count -ge 2) {
    $risk = 'HIGH'; $reasons.Add("multiple Domain subtrees touched: $($domainSubtrees -join ', ')")
} elseif ($domainSubtrees.Count -eq 1 -and $risk -ne 'HIGH') {
    $risk = 'MEDIUM'; $reasons.Add("single Domain subtree touched ($($domainSubtrees[0]))")
} elseif ($domainHits -and $domainSubtrees.Count -eq 0 -and $risk -ne 'HIGH') {
    # Root-level Domain file: anything under game/scripts/Domain/ that is
    # not inside one of the three named subtrees.
    $risk = 'HIGH'; $reasons.Add('root-level Domain file touched')
}

if (-not $risk) {
    if ($uiHits -or $assetHits) {
        $risk = 'MEDIUM'; $reasons.Add('UI / asset surface touched')
    } elseif ($testHits) {
        $risk = 'LOW'; $reasons.Add('test-only change')
    } elseif ($localeHits) {
        $risk = 'MEDIUM'; $reasons.Add('localization touched')
    } elseif ($agentHits) {
        $risk = 'MEDIUM'; $reasons.Add('agent / tooling layer touched')
    } elseif ($docHits) {
        $risk = 'LOW'; $reasons.Add('docs-only change')
    } else {
        $risk = 'LOW'; $reasons.Add('default to LOW')
    }
}

# -- 4. Workflow mode -----------------------------------------------------

switch ($risk) {
    'HIGH' { $mode = 'RELEASE' }
    'MEDIUM' {
        # UI / scene with multiple files → FEATURE; single-file UI → SURGICAL.
        if ($uiHits -and @($paths).Count -le 1) { $mode = 'SURGICAL' }
        else { $mode = 'FEATURE' }
    }
    'LOW' { $mode = 'SURGICAL' }
    default { $mode = 'SURGICAL' }
}

# Escalate ambiguous cases.
if ($saveSchemaHits -or $dependencyHits) {
    if ($mode -ne 'RELEASE') { $mode = 'RELEASE'; $reasons.Add('escalated: schema/dependency always RELEASE') }
}

# -- 5. Required vs skipped commands --------------------------------------

$required = New-Object System.Collections.Generic.List[string]
$skipped  = New-Object System.Collections.Generic.List[string]

# Build is always required when code changed.
$codeHit = ($paths | Where-Object { $_ -like '*.cs' -or $_ -like '*.tscn' -or $_ -like '*.tres' -or $_ -like '*.csproj' })
if ($codeHit) { $required.Add('dotnet build (cd game)') }
else { $skipped.Add('dotnet build') }

# Tests
switch ($mode) {
    'SURGICAL' {
        if ($testHits) {
            $required.Add('dotnet test --filter <affected test class>')
        } else {
            $skipped.Add('full dotnet test')
            $required.Add('dotnet test --filter <affected family, if any>')
        }
    }
    'FEATURE' {
        $required.Add('dotnet test --filter <affected test families>')
        $skipped.Add('full dotnet test (only on cross-domain / shared infra / persistence)')
    }
    'RELEASE' {
        $required.Add('dotnet test (full suite, cd tests/WorldofGoses.Tests)')
    }
}

# Headless boot (Full snapshot)
if ($mode -eq 'RELEASE') { $required.Add('New-SessionSnapshot.ps1 -Mode Full') }
elseif ($mode -eq 'FEATURE') {
    if ($paths | Where-Object { $_ -like 'game/scenes/*' -or $_ -like '*.tscn' }) {
        $required.Add('headless boot (WOG_VISUAL_CAPTURE / Godot --headless)')
    } else {
        $skipped.Add('Full snapshot (Fast only during iteration)')
    }
} else {
    $skipped.Add('Full snapshot (SURGICAL: not required)')
}

# Localization validation
if ($localeHits) { $required.Add('Test-LocalizationCatalog.ps1') }
else { $skipped.Add('Test-LocalizationCatalog.ps1') }

# Agent validation
if ($agentHits) { $required.Add('Validate-AgentContext.ps1 (after Sync)') }
else { $skipped.Add('Validate-AgentContext.ps1') }

# Visual fixtures
if ($uiHits -or $assetHits) {
    if ($mode -eq 'RELEASE') {
        $required.Add('Capture-VisualMatrix.ps1 (full matrix)')
    } elseif ($mode -eq 'FEATURE') {
        $required.Add('Capture-VisualMatrix.ps1 (relevant fixtures only)')
    } else {
        $required.Add('Capture-VisualMatrix.ps1 (one fixture, e.g. macro-hud-default)')
    }
} else {
    $skipped.Add('Capture-VisualMatrix.ps1')
}

# Mirror sync
if ($agentHits) { $required.Add('Sync-AgentContext.ps1 -Apply') }

# -- 6. Review depth ------------------------------------------------------

switch ($mode) {
    'SURGICAL' { $review = 'none' }
    'FEATURE' {
        if ($uiHits -and -not $domainHits -and -not $saveSchemaHits) {
            $review = 'PRESENTATION_REVIEW'
        } else {
            $review = 'DOMAIN_REVIEW'
        }
    }
    'RELEASE' { $review = 'SYSTEM_REVIEW' }
    default { $review = 'none' }
}

# -- 7. Output ------------------------------------------------------------

$result = [pscustomobject]@{
    Mode       = $mode
    Risk       = $risk
    Changed    = @($paths)
    Required   = @($required)
    Skipped    = @($skipped)
    Review     = $review
    Reasons    = @($reasons)
}

if ($Json) {
    $result | ConvertTo-Json -Depth 6
    return
}

Write-Host "Risk : $($result.Risk)"
Write-Host "Mode : $($result.Mode)"
Write-Host "Review: $($result.Review)"
if ($result.Reasons.Count -gt 0) {
    Write-Host 'Reasons:'
    foreach ($r in $result.Reasons) { Write-Host "  - $r" }
}
Write-Host ''
Write-Host 'Changed:'
foreach ($p in $result.Changed) { Write-Host "  $p" }
Write-Host ''
Write-Host 'Required:'
foreach ($c in $result.Required) { Write-Host "  + $c" }
Write-Host ''
Write-Host 'Skipped:'
foreach ($c in $result.Skipped) { Write-Host "  - $c" }
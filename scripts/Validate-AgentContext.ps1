#requires -Version 7.0
<#
.SYNOPSIS
    Validates the World of Goses agent-context architecture.

.DESCRIPTION
    Checks the structural and semantic invariants of the agent-context layer:

      - Required directories exist.
      - Canonical skills and agents are well-formed.
      - Adapter files exist for Claude Code and Codex.
      - Quality guardian is read-only.
      - Root documentation files reference the context map.
      - Context map routes reference real files.
      - In sync mode, mirrors match canonical sources by SHA-256.

    Exits non-zero when any check fails. Use this script as a pre-merge gate.

.EXAMPLE
    pwsh ./scripts/Validate-AgentContext.ps1

.EXAMPLE
    pwsh ./scripts/Validate-AgentContext.ps1 -SyncFirst

    Runs Sync-AgentContext.ps1 -Apply first, then validates. Useful in CI.

.EXAMPLE
    pwsh ./scripts/Validate-AgentContext.ps1 -RepoRoot D:/checkouts/wog
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $RepoRoot = (Resolve-Path -Path (Join-Path $PSScriptRoot '..')).Path,

    [switch] $SyncFirst
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($SyncFirst) {
    Write-Host "==> Running Sync-AgentContext.ps1 -Apply"
    & (Join-Path $PSScriptRoot 'Sync-AgentContext.ps1') -Apply
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Sync failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# -- Result accumulator -----------------------------------------------------

$script:Pass = 0
$script:Fail = 0
$script:Failures = New-Object System.Collections.Generic.List[string]

function Check {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][bool]$Result,
        [string]$Detail = ''
    )
    if ($Result) {
        $script:Pass++
        Write-Host "  ok    $Name" -ForegroundColor Green
    }
    else {
        $script:Fail++
        $msg = "FAIL  $Name" + ($(if ($Detail) { " -- $Detail" } else { '' }))
        $script:Failures.Add($msg)
        Write-Host "  $msg" -ForegroundColor Red
    }
}

# -- Path helpers -----------------------------------------------------------

$canonicalSkillsDir = Join-Path $RepoRoot '.agents/skills'
$canonicalAgentsDir = Join-Path $RepoRoot '.agents/agents'
$claudeAgentsDir    = Join-Path $RepoRoot '.claude/agents'
$codexSkillsDir     = Join-Path $RepoRoot '.codex/skills'
$claudeSkillsDir    = Join-Path $RepoRoot '.claude/skills'
$docsAiDir          = Join-Path $RepoRoot 'docs/ai'
$agentsMdPath       = Join-Path $RepoRoot 'AGENTS.md'
$claudeMdPath       = Join-Path $RepoRoot 'CLAUDE.md'
$syncScript         = Join-Path $RepoRoot 'scripts/Sync-AgentContext.ps1'
$validateScript     = $PSCommandPath

function Get-Sha256 {
    param([Parameter(Mandatory)][string]$Path)
    $h = Get-FileHash -LiteralPath $Path -Algorithm SHA256
    return $h.Hash.ToLowerInvariant()
}

function Get-TextSha256 {
    param([Parameter(Mandatory)][string]$Text)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = $sha.ComputeHash($bytes) }
    finally { $sha.Dispose() }
    return ([BitConverter]::ToString($hash)).Replace('-', '').ToLowerInvariant()
}

function Get-MarkdownHeadings {
    param([Parameter(Mandatory)][string]$Body)
    $result = New-Object System.Collections.Generic.List[string]
    $inFence = $false
    foreach ($line in ($Body -split "`r?`n")) {
        if ($line -match '^```') { $inFence = -not $inFence; continue }
        if ($inFence) { continue }
        if ($line -match '^#{1,6}\s+(.+?)\s*$') {
            $result.Add($Matches[1].Trim())
        }
    }
    return $result
}

function Get-Frontmatter {
    param([Parameter(Mandatory)][string]$Path)
    $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    if ($raw -match '(?s)\A---\r?\n(.*?)\r?\n---') {
        return $Matches[1]
    }
    return ''
}

function Has-AllHeadings {
    param(
        [Parameter(Mandatory)][System.Collections.Generic.List[string]]$Headings,
        [Parameter(Mandatory)][string[]]$Required
    )
    foreach ($r in $Required) {
        if (-not $Headings.Contains($r)) { return $false }
    }
    return $true
}

# -- 1. Directory shape -----------------------------------------------------

Write-Host ""
Write-Host "==> Directory shape" -ForegroundColor Cyan
Check 'canonical skills dir exists' (Test-Path -LiteralPath $canonicalSkillsDir)
Check 'canonical agents dir exists' (Test-Path -LiteralPath $canonicalAgentsDir)
Check 'docs/ai directory exists'   (Test-Path -LiteralPath $docsAiDir)
Check '.claude/agents directory exists' (Test-Path -LiteralPath $claudeAgentsDir)
Check '.codex/skills directory exists'  (Test-Path -LiteralPath $codexSkillsDir)
Check '.claude/skills directory exists' (Test-Path -LiteralPath $claudeSkillsDir)
Check 'Sync-AgentContext.ps1 exists'    (Test-Path -LiteralPath $syncScript)
Check 'Validate-AgentContext.ps1 exists' (Test-Path -LiteralPath $validateScript)

# -- 2. Canonical skills ----------------------------------------------------

Write-Host ""
Write-Host "==> Canonical skills" -ForegroundColor Cyan

$canonicalSkillIds = @()
$requiredSkillSections = @('Purpose', 'When to use', 'Core invariants', 'Definition of done')

# Only the 9 project domain skills must conform to the 12-section schema.
# Vendored gamedev skills (camera-systems, godot-*, router, etc.) use their
# own structure and are not graded here.
$projectDomainSkillIds = @(
    'core-game-vision',
    'citizens-rpg',
    'city-simulation',
    'expeditions-territory',
    'narrative-lore',
    'lineages-and-cultures',
    'technical-foundation',
    'presentation-experience',
    'vertical-slice-validation'
)

foreach ($skillDir in (Get-ChildItem -LiteralPath $canonicalSkillsDir -Directory -ErrorAction SilentlyContinue)) {
    $id = $skillDir.Name
    $canonicalSkillIds += $id
    $skillFile = Join-Path $skillDir.FullName 'SKILL.md'
    Check "skill '$id' has SKILL.md" (Test-Path -LiteralPath $skillFile)

    if (Test-Path -LiteralPath $skillFile) {
        $content = Get-Content -LiteralPath $skillFile -Raw -Encoding UTF8
        $frontmatter = Get-Frontmatter -Path $skillFile
        $hasName = ($frontmatter -match '(?m)^name:\s*[A-Za-z][A-Za-z0-9-]*\s*$')
        $hasDesc = ($frontmatter -match '(?ms)^description:\s*(>.+?(?=^\S)|\S.+)')
        Check "skill '$id' has name in frontmatter"  $hasName
        Check "skill '$id' has description"          $hasDesc

        if ($projectDomainSkillIds -contains $id) {
            $headings = Get-MarkdownHeadings -Body $content
            Check "domain skill '$id' has required sections" (Has-AllHeadings -Headings $headings -Required $requiredSkillSections)
        }
    }
}

# -- 3. Canonical agents ----------------------------------------------------

Write-Host ""
Write-Host "==> Canonical agents" -ForegroundColor Cyan

$canonicalAgentIds = @()
$expectedAgents = @(
    'gameplay-integrator',
    'citizens-rpg',
    'city-simulation',
    'expeditions-territory',
    'narrative-lore',
    'technical-foundation',
    'presentation-experience',
    'quality-guardian'
)

foreach ($expected in $expectedAgents) {
    $agentDir = Join-Path $canonicalAgentsDir $expected
    Check "canonical agent dir exists: $expected" (Test-Path -LiteralPath $agentDir)
}

foreach ($agentDir in (Get-ChildItem -LiteralPath $canonicalAgentsDir -Directory -ErrorAction SilentlyContinue)) {
    $id = $agentDir.Name
    $canonicalAgentIds += $id
    $agentFile = Join-Path $agentDir.FullName 'AGENT.md'
    Check "agent '$id' has AGENT.md" (Test-Path -LiteralPath $agentFile)

    if (Test-Path -LiteralPath $agentFile) {
        $content = Get-Content -LiteralPath $agentFile -Raw -Encoding UTF8
        $headings = Get-MarkdownHeadings -Body $content
        # Required section names. Allow either "Definition of done" or
        # "Definition of done for the review" since the latter is more
        # precise for a reviewer agent.
        $requiredAgentSections = @('Identity', 'When to use this agent', 'Working procedure')
        $hasDoD = $headings.Contains('Definition of done') -or $headings.Contains('Definition of done for the review')
        Check "agent '$id' has required sections" ((Has-AllHeadings -Headings $headings -Required $requiredAgentSections) -and $hasDoD)
    }
}

# -- 4. Claude Code adapters ------------------------------------------------

Write-Host ""
Write-Host "==> Claude Code adapters (.claude/agents/<id>.md)" -ForegroundColor Cyan

foreach ($id in $expectedAgents) {
    $adapter = Join-Path $claudeAgentsDir "$id.md"
    Check "Claude adapter exists: $id" (Test-Path -LiteralPath $adapter)

    if (Test-Path -LiteralPath $adapter) {
        $frontmatter = Get-Frontmatter -Path $adapter
        $hasName = ($frontmatter -match "(?m)^name:\s+$id\s*$")
        # Accept either a quoted scalar "..." or a block scalar '>' with at
        # least one indented continuation line.
        $hasDesc = ($frontmatter -match '(?ms)^description:\s*".+?"') `
                -or ($frontmatter -match "(?ms)^description:\s*>\r?\n\s+\S.+")
        Check "Claude adapter '$id' has name in frontmatter" $hasName
        Check "Claude adapter '$id' has description"        $hasDesc

        if ($id -eq 'quality-guardian') {
            $hasReadOnlyTool = ($frontmatter -match '(?m)^tools:\s*Read,\s*Grep,\s*Glob\s*$')
            Check "quality-guardian has tools: Read, Grep, Glob" $hasReadOnlyTool

            # A correct read-only definition lets the agent inherit
            # `disallowedTools: Bash, Edit, Write` or otherwise excludes
            # `Edit` and `Write` from its allowed pool. Either is fine; the
            # only thing we forbid is having `Edit` or `Write` listed in
            # the *allowed* `tools:` scalar.
            $allowedHasEditWrite = ($frontmatter -match '(?m)^tools:\s*[^`r`n]*\b(Edit|Write)\b')
            Check "quality-guardian has no Edit/Write in allowed tools" (-not $allowedHasEditWrite)
        }
    }
}

# -- 5. Codex adapters ------------------------------------------------------

Write-Host ""
Write-Host "==> Codex adapters (.codex/skills/agent-<id>/SKILL.md)" -ForegroundColor Cyan

foreach ($id in $expectedAgents) {
    $adapter = Join-Path $codexSkillsDir "agent-$id" 'SKILL.md'
    Check "Codex adapter exists: agent-$id" (Test-Path -LiteralPath $adapter)
    if (Test-Path -LiteralPath $adapter) {
        $frontmatter = Get-Frontmatter -Path $adapter
        $hasName  = ($frontmatter -match "(?m)^name:\s+agent-$id\s*$")
        $hasDesc  = ($frontmatter -match '(?ms)^description:\s*>')
        Check "Codex adapter 'agent-$id' has name"        $hasName
        Check "Codex adapter 'agent-$id' has description" $hasDesc
    }
}

# -- 6. Skill mirrors exist for every canonical skill -----------------------

Write-Host ""
Write-Host "==> Skill mirrors (.claude/skills/, .codex/skills/)" -ForegroundColor Cyan

foreach ($id in $canonicalSkillIds) {
    $claudeMirror = Join-Path $claudeSkillsDir $id 'SKILL.md'
    $codexMirror  = Join-Path $codexSkillsDir  $id 'SKILL.md'
    Check ".claude/skills mirror: $id" (Test-Path -LiteralPath $claudeMirror)
    Check ".codex/skills mirror:  $id" (Test-Path -LiteralPath $codexMirror)
}

# -- 7. Root entry files ----------------------------------------------------

Write-Host ""
Write-Host "==> Root entry files" -ForegroundColor Cyan

$contextMapPath = Join-Path $docsAiDir 'CONTEXT_MAP.md'

Check 'AGENTS.md exists' (Test-Path -LiteralPath $agentsMdPath)
Check 'CLAUDE.md exists' (Test-Path -LiteralPath $claudeMdPath)
Check 'CONTEXT_MAP.md exists' (Test-Path -LiteralPath $contextMapPath)

if (Test-Path -LiteralPath $agentsMdPath) {
    $content = Get-Content -LiteralPath $agentsMdPath -Raw -Encoding UTF8
    Check 'AGENTS.md references CONTEXT_MAP.md'        ($content -match 'docs/ai/CONTEXT_MAP\.md')
    Check 'AGENTS.md references AGENT_DISPATCH.md'     ($content -match 'docs/ai/AGENT_DISPATCH\.md')
    Check 'AGENTS.md references CROSS_DOMAIN_INVARIANTS' ($content -match 'CROSS_DOMAIN_INVARIANTS')
    # Brevity: under 400 lines after condensing.
    $lineCount = ($content -split "`r?`n").Count
    Check "AGENTS.md is brief (<= 350 lines)" ($lineCount -le 350) "actual=$lineCount"
}

if (Test-Path -LiteralPath $claudeMdPath) {
    $content = Get-Content -LiteralPath $claudeMdPath -Raw -Encoding UTF8
    Check 'CLAUDE.md references CONTEXT_MAP.md'        ($content -match 'CONTEXT_MAP')
    Check 'CLAUDE.md references AGENT_DISPATCH.md'     ($content -match 'AGENT_DISPATCH')
    Check 'CLAUDE.md references CROSS_DOMAIN_INVARIANTS' ($content -match 'CROSS_DOMAIN_INVARIANTS\.md')
}

# -- 8. CONTEXT_MAP.md routes resolve to real files -------------------------

Write-Host ""
Write-Host "==> CONTEXT_MAP.md route integrity" -ForegroundColor Cyan

if (Test-Path -LiteralPath $contextMapPath) {
    $content = Get-Content -LiteralPath $contextMapPath -Raw -Encoding UTF8
    # Match backtick-quoted relative paths starting with `docs/`, `game/`,
    # `tests/`, or `art/`.
    $paths = [regex]::Matches($content, '`(docs/[^`]+\.md|game/[^`]+\.(?:cs|tscn)|tests/[^`]+\.cs|art/[^`]+)`')
    $bad = 0
    foreach ($m in $paths) {
        $rel = $m.Groups[1].Value
        $abs = Join-Path $RepoRoot $rel
        if (-not (Test-Path -LiteralPath $abs)) {
            $script:Fail++
            $bad++
            $script:Failures.Add("CONTEXT_MAP.md references missing file: $rel")
            Write-Host "  missing reference: $rel" -ForegroundColor Red
        }
        else {
            $script:Pass++
        }
    }
}

# -- 9. Sync drift check ----------------------------------------------------

Write-Host ""
Write-Host "==> Mirror sync drift" -ForegroundColor Cyan

foreach ($id in $canonicalSkillIds) {
    $src = Join-Path $canonicalSkillsDir $id 'SKILL.md'
    if (-not (Test-Path -LiteralPath $src)) { continue }

    $srcHash = Get-Sha256 -Path $src
    foreach ($mirror in @(
        (Join-Path $claudeSkillsDir $id 'SKILL.md'),
        (Join-Path $codexSkillsDir  $id 'SKILL.md')
    )) {
        if (-not (Test-Path -LiteralPath $mirror)) {
            Check "mirror present: $($id)" $false "missing $mirror"
            continue
        }
        $mirrorHash = Get-Sha256 -Path $mirror
        Check "mirror matches canonical: $id -> $($mirror.Replace($RepoRoot, ''))" ($srcHash -eq $mirrorHash)
    }
}

# Agent mirrors carry a YAML frontmatter block that the canonical AGENT.md
# does not. We verify drift by checking that the canonical body content is
# present verbatim in the mirror file (after the frontmatter is stripped).
function Get-BodyOnly {
    param([Parameter(Mandatory)][string]$Path)
    $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    if ($raw -match '(?s)\A---\r?\n.*?\r?\n---\r?\n(.*)\z') {
        return $Matches[1]
    }
    return $raw
}

# Hash body content with a normalized trailing newline. Set-Content in
# PowerShell can append a final newline where the canonical file has none,
# so trimming the trailing newline makes the comparison stable.
function Get-NormalizedBodyHash {
    param([Parameter(Mandatory)][string]$Text)
    $trimmed = $Text -replace '\r?\n\z', ''
    return Get-TextSha256 -Text $trimmed
}

foreach ($id in $canonicalAgentIds) {
    $src = Join-Path $canonicalAgentsDir $id 'AGENT.md'
    if (-not (Test-Path -LiteralPath $src)) { continue }

    $canonicalHash = Get-NormalizedBodyHash -Text (Get-BodyOnly -Path $src)

    foreach ($mirror in @(
        (Join-Path $claudeAgentsDir "$id.md"),
        (Join-Path $codexSkillsDir "agent-$id" 'SKILL.md')
    )) {
        if (-not (Test-Path -LiteralPath $mirror)) {
            Check "agent mirror present: $id" $false "missing $mirror"
            continue
        }
        $mirrorHash = Get-NormalizedBodyHash -Text (Get-BodyOnly -Path $mirror)
        $relPath = $mirror.Replace($RepoRoot, '')
        Check "agent body matches canonical: $id -> $relPath" ($canonicalHash -eq $mirrorHash)
    }
}

# -- 10. No duplicate names -------------------------------------------------

Write-Host ""
Write-Host "==> No duplicate agent names" -ForegroundColor Cyan

$adapterNames = @()
if (Test-Path -LiteralPath $claudeAgentsDir) {
    foreach ($f in (Get-ChildItem -LiteralPath $claudeAgentsDir -Filter '*.md' -ErrorAction SilentlyContinue)) {
        $fm = Get-Frontmatter -Path $f.FullName
        if ($fm -match '(?m)^name:\s*([A-Za-z][A-Za-z0-9-]*)\s*$') {
            $adapterNames += $Matches[1]
        }
    }
}
$groups = @($adapterNames | Group-Object)
$dup = @($groups | Where-Object { $_.Count -gt 1 })
Check 'no duplicate agent names in .claude/agents/' ($dup.Count -eq 0) "duplicates=$($dup.Count)"

# -- 11. Existing files preserved ------------------------------------------

Write-Host ""
Write-Host "==> Existing files preserved" -ForegroundColor Cyan

# The pre-existing vendored skills must still be tracked.
foreach ($expected in @('godot-csharp', 'router', 'save-systems')) {
    Check "vendored skill '$expected' still present" (Test-Path -LiteralPath (Join-Path $canonicalSkillsDir $expected 'SKILL.md'))
}

# -- Summary ----------------------------------------------------------------

Write-Host ""
Write-Host "==> Summary" -ForegroundColor Cyan
Write-Host "  Passed: $script:Pass"
Write-Host "  Failed: $script:Fail"

if ($script:Fail -gt 0) {
    Write-Host ""
    Write-Host "Failures:" -ForegroundColor Red
    foreach ($f in $script:Failures) {
        Write-Host "  - $f" -ForegroundColor Red
    }
    exit 1
}

Write-Host "All checks passed." -ForegroundColor Green
exit 0
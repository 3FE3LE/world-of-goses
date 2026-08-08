#requires -Version 7.0
<#
.SYNOPSIS
    Mirrors canonical agent and skill definitions under .agents/ into the
    tool-specific directories Claude Code and Codex read at runtime.

.DESCRIPTION
    Canonical source of truth:
      .agents/skills/<id>/SKILL.md
      .agents/agents/<id>/AGENT.md

    Generated mirrors:
      .claude/skills/<id>/SKILL.md      (verbatim copy; existing layout)
      .codex/skills/<id>/SKILL.md       (verbatim copy; Codex discovers these)
      .claude/agents/<id>.md            (Claude subagent: YAML frontmatter + body)
      .codex/skills/agent-<id>/SKILL.md (Codex skill adapter: YAML frontmatter + body)

    Why both directories exist:

      .claude/skills/<id>/             Claude Code discovers project-level skills here.
      .codex/skills/<id>/              Codex CLI 0.145 discovers project-level skills here.
      .codex/skills/agent-<id>/        Codex has no native agent concept, so each agent is
                                       delivered as a Codex skill, prefixed `agent-` so it
                                       never collides with a same-named domain skill.

    Behavior:

      - Idempotent: re-runs only update files whose mirror content has drifted.
      - -WhatIf / no -Apply: dry run. Default is dry run.
      - No deletions. The script never removes a mirror file. Removing is a
        human decision, taken after the agent or skill is also removed from
        the canonical source and from docs/ai/CONTEXT_MAP.md.
      - No elevation required. Reads and writes only inside the repo root.

.EXAMPLE
    pwsh ./scripts/Sync-AgentContext.ps1

    Dry run. Prints what would change. Exits 0.

.EXAMPLE
    pwsh ./scripts/Sync-AgentContext.ps1 -Apply

    Performs the writes. Idempotent re-runs only update drifted mirrors.

.EXAMPLE
    pwsh ./scripts/Sync-AgentContext.ps1 -WhatIf

    PowerShell-native dry run. Same as omitting -Apply.
#>

[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Low')]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $RepoRoot = (Resolve-Path -Path (Join-Path $PSScriptRoot '..')).Path,

    [switch] $Apply,

    [switch] $FailOnDrift
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# -- Canonical source enumeration -------------------------------------------

$canonicalSkillsDir = Join-Path $RepoRoot '.agents/skills'
$canonicalAgentsDir = Join-Path $RepoRoot '.agents/agents'

# -- Tool-specific output directories --------------------------------------

$claudeAgentsDir = Join-Path $RepoRoot '.claude/agents'
$codexSkillsDir  = Join-Path $RepoRoot '.codex/skills'
$claudeSkillsDir = Join-Path $RepoRoot '.claude/skills'

# -- Helpers ----------------------------------------------------------------

function Write-Status {
    param([string]$Message, [string]$Level = 'INFO')
    $stamp = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    Write-Host "[$stamp] [$Level] $Message"
}

function Get-Sha256 {
    param([Parameter(Mandatory)][string]$Path)
    $h = Get-FileHash -LiteralPath $Path -Algorithm SHA256
    return $h.Hash.ToLowerInvariant()
}

function Get-TextSha256 {
    param([Parameter(Mandatory)][string]$Text)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash($bytes)
    }
    finally {
        $sha.Dispose()
    }
    return ([BitConverter]::ToString($hash)).Replace('-', '').ToLowerInvariant()
}

function Read-CanonicalAgentBody {
    param([Parameter(Mandatory)][string]$Path)

    $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8

    # Strip a leading YAML frontmatter block if present (the canonical files
    # are tool-neutral and should not have one, but tolerate if added later).
    if ($raw -match '(?s)\A---\r?\n(.*?)\r?\n---\r?\n(.*)\z') {
        $frontmatter = $Matches[1]
        $body = $Matches[2]
    }
    else {
        $frontmatter = ''
        $body = $raw
    }

    # Derive id from the first H1 heading. Tool-neutral canonical files use a
    # Markdown title that already matches the kebab-case directory id, e.g.
    # `# Citizens RPG agent`.
    $title = ''
    $inFence = $false
    foreach ($line in ($body -split "`r?`n")) {
        if ($line -match '^```') {
            $inFence = -not $inFence
            continue
        }
        if (-not $inFence -and $line -match '^#\s+(.+?)\s*$') {
            $title = $Matches[1].Trim()
            break
        }
    }

    if ([string]::IsNullOrWhiteSpace($title)) {
        throw "Canonical agent file '$Path' has no H1 title."
    }

    # Derive description from the first non-empty, non-heading paragraph
    # immediately following the H1. The paragraph may be a Markdown
    # blockquote (one or more lines starting with `> `) or a plain
    # paragraph. Continue collecting until the first blank line, heading,
    # or fenced block. Strip a leading `> ` from each line so YAML
    # frontmatter is readable.
    $description = ''
    $inFence = $false
    $seenTitle = $false
    $collecting = $false
    $parts = New-Object System.Collections.Generic.List[string]
    foreach ($line in ($body -split "`r?`n")) {
        if ($line -match '^```') {
            if ($collecting) { break }
            $inFence = -not $inFence
            continue
        }
        if ($inFence) { continue }
        if (-not $seenTitle) {
            if ($line -match '^#\s+') {
                $seenTitle = $true
            }
            continue
        }
        if (-not $collecting) {
            if ($line -match '^\s*$') { continue }
            if ($line -match '^#{1,6}\s+') { break }
            $collecting = $true
        }
        if ($line -match '^#{1,6}\s+') { break }
        if ($line -match '^\s*$') { break }
        $cleaned = $line.Trim() -replace '^\s*>\s?', ''
        if (-not [string]::IsNullOrWhiteSpace($cleaned)) {
            $parts.Add($cleaned.Trim())
        }
    }
    $description = ($parts -join ' ').Trim()

    if ([string]::IsNullOrWhiteSpace($description)) {
        throw "Canonical agent file '$Path' has no description line."
    }

    return [pscustomobject]@{
        Title       = $title
        Description = $description
        Frontmatter = $frontmatter
        Body        = $body
    }
}

function Get-AgentSkillList {
    param([Parameter(Mandatory)][string]$Body, [Parameter(Mandatory)][string]$Id)

    # Look at the agent body's `## Primary skills` and `## Conditional skills`
    # sections for kebab-case tokens. This is a tolerant parser — it accepts
    # backtick-quoted and bullet-listed identifiers.
    $skills = New-Object System.Collections.Generic.List[string]
    $inFence = $false
    $currentSection = ''
    foreach ($line in ($Body -split "`r?`n")) {
        if ($line -match '^```') {
            $inFence = -not $inFence
            continue
        }
        if ($inFence) { continue }

        if ($line -match '^#{2,6}\s+(.+?)\s*$') {
            $currentSection = $Matches[1].Trim().ToLowerInvariant()
            continue
        }

        if ($currentSection -in @('primary skills', 'mandatory consultations',
                                  'conditional skills', 'additional conditional skills')) {
            # Pull kebab-case tokens, with or without backticks.
            $tokens = [regex]::Matches($line, '`?([a-z][a-z0-9-]{1,40})`?')
            foreach ($t in $tokens) {
                $value = $t.Groups[1].Value
                if ($value -in @('core-game-vision', 'citizens-rpg',
                                 'city-simulation', 'expeditions-territory',
                                 'narrative-lore', 'lineages-and-cultures',
                                 'technical-foundation',
                                 'presentation-experience',
                                 'vertical-slice-validation')) {
                    if (-not $skills.Contains($value)) {
                        $skills.Add($value)
                    }
                }
            }
        }
    }

    # Always include core-game-vision unless the agent is explicitly
    # mechanical-only. None of the eight agents are mechanical-only today.
    if (-not $skills.Contains('core-game-vision')) {
        $skills.Insert(0, 'core-game-vision')
    }

    return $skills
}

function New-ClaudeAgentAdapter {
    param(
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)][string]$Body,
        [Parameter(Mandatory)][string[]]$SkillList,
        [Parameter(Mandatory)][bool]$IsReadOnly
    )

    # Collapse the description to a single line; backticks and punctuation
    # are safe inside YAML plain scalars, but quotes and backslashes need
    # quoting. Use a block scalar (`>-`) so the validation regex can match
    # the entire phrase without quoting every special character.
    function Yaml-BlockScalar([string]$s) {
        $collapsed = ($s -replace "`r?`n", ' ').Trim()
        # Normalize whitespace runs.
        $collapsed = [regex]::Replace($collapsed, '\s+', ' ')
        # Wrap as a folded block scalar; indent by two spaces.
        return '  ' + $collapsed
    }

    $toolsScalar = if ($IsReadOnly) { 'Read, Grep, Glob' } else { 'Edit, Write, Read, Grep, Glob, Bash' }

    $disallowedScalar = if ($IsReadOnly) { 'Bash, Edit, Write' } else { '' }

    $skillYaml = ($SkillList | ForEach-Object { '      - ' + $_ }) -join "`n"

    $front = @()
    $front += "name: $Id"
    $front += 'description: >'
    $front += (Yaml-BlockScalar $Description)
    $front += "tools: $toolsScalar"
    if (-not [string]::IsNullOrWhiteSpace($disallowedScalar)) {
        $front += "disallowedTools: $disallowedScalar"
    }
    $front += 'skills:'
    $front += $skillYaml
    $front += 'model: inherit'

    $header = '---' + "`n" + ($front -join "`n") + "`n" + '---' + "`n"

    # Body already contains its own `# Title` heading and starts at column
    # zero. Do not inject any separator; any extra newline would create an
    # off-by-one byte drift compared to the canonical body.
    return $header + $Body
}

function New-CodexAgentAdapter {
    param(
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)][string]$Body,
        [Parameter(Mandatory)][string[]]$SkillList
    )

    # Codex uses YAML frontmatter with `name` and `description`; matching the
    # SKILL.md shape already in use in .agents/skills/.
    function Yaml-BlockScalar([string]$s) {
        $collapsed = ($s -replace "`r?`n", ' ').Trim()
        $collapsed = [regex]::Replace($collapsed, '\s+', ' ')
        return '  ' + $collapsed
    }

    $headerLines = @()
    $headerLines += '---'
    $headerLines += "name: agent-$Id"
    $headerLines += 'description: >'
    $headerLines += "  $Id agent for World of Goses."
    $headerLines += (Yaml-BlockScalar "  $Description")
    $headerLines += "  Use when the task matches this agent's domain."
    $headerLines += "  Loads these skills on activation: $($SkillList -join ', ')."
    $headerLines += 'license: World of Goses project license'
    $headerLines += 'compatibility: Codex CLI 0.145+ (project-level skills)'
    $headerLines += 'metadata:'
    $headerLines += "  agent_id: $Id"
    $headerLines += "  canonical: .agents/agents/$Id/AGENT.md"
    $headerLines += '  read_only: false'
    $headerLines += '---'

    $header = $headerLines -join "`n"

    # The joined header ends with `---` and no trailing newline. Add one
    # so the body starts on a fresh line in the mirror file.
    return $header + "`n" + $Body
}

# -- Sync core --------------------------------------------------------------

$mode = if ($Apply) { 'APPLY' } else { 'DRY-RUN' }
Write-Status "Sync-AgentContext starting. mode=$mode repo=$RepoRoot"

$stats = @{
    SkillsDiscovered = 0
    AgentsDiscovered = 0
    Created           = 0
    Updated           = 0
    UpToDate          = 0
    Errors            = 0
}

function Sync-File {
    param(
        [Parameter(Mandatory)][string]$TargetPath,
        [Parameter(Mandatory)][string]$Content,
        [Parameter(Mandatory)][string]$Description
    )

    $dir = Split-Path -Parent $TargetPath
    if (-not (Test-Path -LiteralPath $dir)) {
        if ($Apply) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
        else {
            Write-Status "  plan: mkdir $dir"
        }
    }

    $hash = Get-TextSha256 -Text $Content
    $existingHash = $null
    if (Test-Path -LiteralPath $TargetPath) {
        $existingHash = Get-Sha256 -Path $TargetPath
    }

    if ($existingHash -eq $hash) {
        Write-Status "  ok    $Description ($([System.IO.Path]::GetFileName($TargetPath)))"
        $script:stats.UpToDate++
        return
    }

    if (-not $Apply) {
        $verb = if ($existingHash) { 'update' } else { 'create' }
        Write-Status "  plan: $verb $TargetPath"
        if ($existingHash) { $script:stats.Updated++ } else { $script:stats.Created++ }
        return
    }

    Set-Content -LiteralPath $TargetPath -Value $Content -Encoding UTF8 -NoNewline
    $verb = if ($existingHash) { 'updated' } else { 'created' }
    Write-Status "  $verb $TargetPath"
    if ($existingHash) { $script:stats.Updated++ } else { $script:stats.Created++ }
}

# -- 1. Mirror canonical skills to .claude/skills/ and .codex/skills/ -------

if (-not (Test-Path -LiteralPath $canonicalSkillsDir)) {
    Write-Status "Canonical skills directory not found: $canonicalSkillsDir" 'WARN'
}
else {
    foreach ($skillDir in (Get-ChildItem -LiteralPath $canonicalSkillsDir -Directory)) {
        $id = $skillDir.Name
        $src = Join-Path $skillDir.FullName 'SKILL.md'
        if (-not (Test-Path -LiteralPath $src)) {
            Write-Status "Skill '$id' has no SKILL.md" 'WARN'
            $stats.Errors++
            continue
        }
        $stats.SkillsDiscovered++

        $content = Get-Content -LiteralPath $src -Raw -Encoding UTF8

        # Claude: mirror the entire SKILL.md file unchanged.
        Sync-File `
            -TargetPath (Join-Path $claudeSkillsDir $id 'SKILL.md') `
            -Content    $content `
            -Description "skill -> .claude/skills/$id/SKILL.md"

        # Codex: mirror the entire SKILL.md file unchanged. Codex discovers
        # project-level skills under .codex/skills/<name>/SKILL.md.
        Sync-File `
            -TargetPath (Join-Path $codexSkillsDir $id 'SKILL.md') `
            -Content    $content `
            -Description "skill -> .codex/skills/$id/SKILL.md"
    }
}

# -- 2. Mirror canonical agents to .claude/agents/ and .codex/skills/ --------

if (-not (Test-Path -LiteralPath $canonicalAgentsDir)) {
    Write-Status "Canonical agents directory not found: $canonicalAgentsDir" 'WARN'
}
else {
    foreach ($agentDir in (Get-ChildItem -LiteralPath $canonicalAgentsDir -Directory)) {
        $id = $agentDir.Name
        $src = Join-Path $agentDir.FullName 'AGENT.md'
        if (-not (Test-Path -LiteralPath $src)) {
            Write-Status "Agent '$id' has no AGENT.md" 'WARN'
            $stats.Errors++
            continue
        }
        $stats.AgentsDiscovered++

        $parsed = Read-CanonicalAgentBody -Path $src
        $skillList = Get-AgentSkillList -Body $parsed.Body -Id $id
        $isReadOnly = ($id -eq 'quality-guardian')

        $claudeContent = New-ClaudeAgentAdapter `
            -Id          $id `
            -Title       $parsed.Title `
            -Description $parsed.Description `
            -Body        $parsed.Body `
            -SkillList   $skillList `
            -IsReadOnly  $isReadOnly

        $codexContent = New-CodexAgentAdapter `
            -Id          $id `
            -Title       $parsed.Title `
            -Description $parsed.Description `
            -Body        $parsed.Body `
            -SkillList   $skillList

        Sync-File `
            -TargetPath (Join-Path $claudeAgentsDir "$id.md") `
            -Content    $claudeContent `
            -Description "agent -> .claude/agents/$id.md"

        Sync-File `
            -TargetPath (Join-Path $codexSkillsDir "agent-$id" 'SKILL.md') `
            -Content    $codexContent `
            -Description "agent -> .codex/skills/agent-$id/SKILL.md"
    }
}

Write-Status ("Sync-AgentContext done. " +
    "skills=$($stats.SkillsDiscovered) agents=$($stats.AgentsDiscovered) " +
    "created=$($stats.Created) updated=$($stats.Updated) up-to-date=$($stats.UpToDate) " +
    "errors=$($stats.Errors)")

if ($Apply -and $stats.Errors -gt 0) {
    exit 1
}

if ($FailOnDrift -and $stats.Updated -gt 0) {
    Write-Status "FailOnDrift: detected drifted mirrors. Run with -Apply." 'ERROR'
    exit 2
}
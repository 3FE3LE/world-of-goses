<#
.SYNOPSIS
    Installs .githooks/commit-msg and points core.hooksPath at it.

.DESCRIPTION
    The repository owns .githooks/commit-msg, a hook that rejects any commit
    that credits an AI agent as author, committer or co-author. Per-clone
    activation is a git config line; this script is the one place that
    applies it, so the snapshot and the developer command stay in sync.

    The hook lives in the repository (not in .git/hooks) so it travels with
    the working copy and so a clone from a fresh state is guarded without
    extra setup. A clone that never runs this script is a clone that
    silently bypasses the rule that the prose in CLAUDE.md / AGENTS.md has
    already failed to enforce on its own.

    Idempotent. Safe to run on every session start.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$hooksDir = Join-Path $repoRoot ".githooks"
$hookPath = Join-Path $hooksDir "commit-msg"

if (-not (Test-Path -LiteralPath $hookPath)) {
    throw ".githooks/commit-msg is missing from the repository; refusing to install a non-existent guard."
}

# POSIX exec bit is required for sh. The file is created from this script
# path only when the install command is run on a non-POSIX host, so reset
# it here on every run. (chmod via .NET is not available on Windows PowerShell;
# the equivalent on Windows is "anyone" full control, which is the default.)
if ($IsLinux -or $IsMacOS) {
    chmod +x "$hookPath"
} else {
    & icacls $hookPath /reset /T | Out-Null
}

git -C $repoRoot config --local core.hooksPath ".githooks"
Write-Host "core.hooksPath = .githooks (commit-msg guard active)"

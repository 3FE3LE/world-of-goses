<#
.SYNOPSIS
    Phase 2 of the documentation migration: merge the hand-authored
    classification ledger into the inventory, and fail on the three ways a
    documentation tree silently rots.

.DESCRIPTION
    scripts/docs/inventory.ps1 records what exists and leaves the judgement
    fields null on purpose. This script fills them from
    scripts/docs/classification.json — a file a human wrote and a human can
    disagree with, line by line — and then checks three things:

      1. Every inventoried document is classified. A document nobody has
         classified is a document nobody has read.
      2. Every ledger entry names a document that still exists. Stale entries
         are how a ledger starts lying.
      3. Every live document under docs/ is reachable from docs/README.md.
         The four numbered orphans survived precisely because the index did
         not mention them.

    Read-only with respect to the documentation: it writes one report under
    .migration/ and nothing else. Idempotent.

.PARAMETER OutputDirectory
    Where the report is written. Defaults to .migration/ at the repo root.

.PARAMETER LedgerPath
    The hand-authored classification. Defaults to the file beside this script.

.EXAMPLE
    pwsh ./scripts/docs/inventory.ps1
    pwsh ./scripts/docs/classify.ps1
#>
[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [string]$LedgerPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoRoot '.migration' }
if (-not $LedgerPath) { $LedgerPath = Join-Path $PSScriptRoot 'classification.json' }

$inventoryPath = Join-Path $OutputDirectory 'document-inventory.json'
if (-not (Test-Path -LiteralPath $inventoryPath)) {
    throw "No inventory at $inventoryPath. Run scripts/docs/inventory.ps1 first."
}

$records = Get-Content -LiteralPath $inventoryPath -Raw | ConvertFrom-Json
$ledger = Get-Content -LiteralPath $LedgerPath -Raw | ConvertFrom-Json

$entries = $ledger.documents
$rules = @($ledger.rules)

# Longest prefix wins, so a specific rule can sit under a general one.
$rules = $rules | Sort-Object { $_.path_prefix.Length } -Descending

function Get-Classification {
    param([string]$Path)

    $exact = $entries.PSObject.Properties[$Path]
    if ($exact) { return @{ Source = 'document'; Value = $exact.Value } }
    foreach ($rule in $rules) {
        if ($Path.StartsWith($rule.path_prefix)) { return @{ Source = "rule:$($rule.path_prefix)"; Value = $rule } }
    }
    return $null
}

function Get-Field {
    param($Object, [string]$Name, $Default)
    $prop = $Object.PSObject.Properties[$Name]
    if ($prop -and $null -ne $prop.Value) { return $prop.Value }
    return $Default
}

# --- Merge ----------------------------------------------------------------

$unclassified = @()
$classified = @()

foreach ($r in $records) {
    $hit = Get-Classification -Path $r.path
    if (-not $hit) {
        $unclassified += $r.path
        $r.action = 'undecided'
        $classified += $r
        continue
    }

    $c = $hit.Value
    $r.proposed_type = Get-Field $c 'proposed_type' $null
    $r.authority = Get-Field $c 'authority' $null
    $r.domains = @(Get-Field $c 'domains' @())
    $r.duplicates_or_overlaps = @(Get-Field $c 'duplicates_or_overlaps' @())
    $r.related_code = @(Get-Field $c 'related_code' @())
    # The destination is the current path unless the ledger says otherwise:
    # after phase 3 most documents are exactly where they belong.
    $r.proposed_destination = Get-Field $c 'proposed_destination' $r.path
    $r.action = Get-Field $c 'action' 'undecided'
    $r | Add-Member -NotePropertyName classified_by -NotePropertyValue $hit.Source -Force
    $r | Add-Member -NotePropertyName classification_note -NotePropertyValue (Get-Field $c 'note' '') -Force
    $classified += $r
}

# --- Check 2: stale ledger entries ---------------------------------------

$knownPaths = [System.Collections.Generic.HashSet[string]]::new()
foreach ($r in $records) { [void]$knownPaths.Add($r.path) }

$stale = @()
foreach ($p in $entries.PSObject.Properties.Name) {
    if (-not $knownPaths.Contains($p)) { $stale += $p }
}

# --- Check 3: index completeness -----------------------------------------

# A document counts as indexed when docs/README.md names it, or names a
# directory that contains it. Directory-level rows are legitimate: the index
# points at session-state/ and licenses/ as units, not file by file.
$docsIndexPath = Join-Path $repoRoot 'docs/README.md'
$docsIndex = Get-Content -LiteralPath $docsIndexPath -Raw

$unindexed = @()
foreach ($r in $records) {
    if ($r.root_kind -ne 'documentation') { continue }
    if ($r.path -eq 'docs/README.md') { continue }

    $relative = $r.path.Substring('docs/'.Length)
    $candidates = @($relative)
    $segments = $relative.Split('/')
    for ($i = 1; $i -lt $segments.Count; $i++) {
        $candidates += (($segments[0..($i - 1)] -join '/') + '/')
    }
    if (-not ($candidates | Where-Object { $docsIndex.Contains($_) })) {
        $unindexed += $r.path
    }
}

# --- Report ---------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$classificationPath = Join-Path $OutputDirectory 'document-classification.json'
$classified | Sort-Object path | ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath $classificationPath -Encoding UTF8

[pscustomobject]@{
    Documents       = $classified.Count
    Classified      = $classified.Count - $unclassified.Count
    Unclassified    = $unclassified.Count
    StaleEntries    = $stale.Count
    UnindexedInDocs = $unindexed.Count
} | Format-List

"By type:"
$classified | Group-Object proposed_type | Sort-Object Count -Descending |
    ForEach-Object { "  {0,-12} {1}" -f ($_.Name ? $_.Name : '(none)'), $_.Count }

"`nBy action:"
$classified | Group-Object action | Sort-Object Count -Descending |
    ForEach-Object { "  {0,-12} {1}" -f $_.Name, $_.Count }

$pending = @($classified | Where-Object { $_.action -in @('split', 'merge', 'retire') })
if ($pending.Count -gt 0) {
    "`nProposed but not scheduled — these are decisions, not chores:"
    $pending | ForEach-Object { "  [{0}] {1}" -f $_.action, $_.path }
}

"`nWrote:"
"  $classificationPath"

# --- Verdict --------------------------------------------------------------

$failures = @()
if ($unclassified.Count -gt 0) {
    $failures += "Unclassified documents ($($unclassified.Count)). Add an entry or a rule to $($LedgerPath):"
    $unclassified | ForEach-Object { $failures += "    $_" }
}
if ($stale.Count -gt 0) {
    $failures += "Ledger entries for documents that no longer exist ($($stale.Count)):"
    $stale | ForEach-Object { $failures += "    $_" }
}
if ($unindexed.Count -gt 0) {
    $failures += "Documents missing from docs/README.md ($($unindexed.Count)). See its authority rule 7:"
    $unindexed | ForEach-Object { $failures += "    $_" }
}

if ($failures.Count -gt 0) {
    "`n" + ($failures -join "`n") | Write-Host
    exit 1
}

"`nAll documents classified, indexed, and accounted for."

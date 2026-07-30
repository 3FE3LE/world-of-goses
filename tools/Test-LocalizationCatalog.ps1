[CmdletBinding()]
param(
    [switch]$UpdateTemplate
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = Join-Path $repositoryRoot 'game'
$localeRoot = Join-Path $gameRoot 'locale'
$templatePath = Join-Path $localeRoot 'messages.pot'
$catalogPaths = @(
    Join-Path $localeRoot 'en.po'
    Join-Path $localeRoot 'es.po'
)

function Read-PoCatalog {
    param([Parameter(Mandatory)][string]$Path)

    # Ordinal (case-sensitive) comparer: Godot's TranslationServer matches
    # msgid keys case-sensitively at runtime (gettext semantics), so "wood"
    # and "Wood" are legitimately distinct keys. A plain PowerShell @{}
    # hashtable compares string keys case-INsensitively by default, which
    # would misreport same-word-different-case msgids as duplicates.
    $entries = [System.Collections.Generic.Dictionary[string, int]]::new([System.StringComparer]::Ordinal)
    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $Path -Encoding utf8) {
        $lineNumber++
        if ($line -notmatch '^msgid "(.*)"$') { continue }

        $id = $Matches[1]
        if ([string]::IsNullOrEmpty($id)) { continue }
        if ($entries.ContainsKey($id)) {
            throw "$Path contains duplicate msgid '$id' at line $lineNumber."
        }
        $entries[$id] = $lineNumber
    }
    return $entries
}

function Read-PoTranslations {
    param([Parameter(Mandatory)][string]$Path)

    # Same case-sensitive rationale as Read-PoCatalog above — a plain @{}
    # would silently collide "wood" and "Wood" translations (last one read
    # wins), masking that they are two distinct, independently-translated
    # runtime keys.
    $translations = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::Ordinal)
    $currentId = $null
    $collectingTranslation = $false
    $translation = [System.Text.StringBuilder]::new()
    foreach ($line in Get-Content -LiteralPath $Path -Encoding utf8) {
        if ($line -match '^msgid "(.*)"$') {
            if ($null -ne $currentId) {
                $translations[$currentId] = $translation.ToString()
            }
            $currentId = $Matches[1]
            $collectingTranslation = $false
            [void]$translation.Clear()
            continue
        }
        if ($null -ne $currentId -and $line -match '^msgstr "(.*)"$') {
            [void]$translation.Append($Matches[1])
            $collectingTranslation = $true
            continue
        }
        if ($collectingTranslation -and $line -match '^"(.*)"$') {
            [void]$translation.Append($Matches[1])
        }
    }
    if ($null -ne $currentId) {
        $translations[$currentId] = $translation.ToString()
    }
    return $translations
}

function Get-FormatPlaceholders {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Text)

    return @([regex]::Matches($Text, '\{\d+(?::[^}]+)?\}') |
        ForEach-Object Value |
        Sort-Object -Unique)
}

$catalogs = @{}
$translations = @{}
$allIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($catalogPath in $catalogPaths) {
    $catalog = Read-PoCatalog -Path $catalogPath
    $catalogs[$catalogPath] = $catalog
    $translations[$catalogPath] = Read-PoTranslations -Path $catalogPath
    foreach ($id in $catalog.Keys) { [void]$allIds.Add($id) }
}

$sourceKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$sourceFiles = Get-ChildItem -LiteralPath (Join-Path $gameRoot 'scripts') -Recurse -Filter '*.cs'
foreach ($sourceFile in $sourceFiles) {
    $content = Get-Content -Raw -LiteralPath $sourceFile.FullName -Encoding utf8
    foreach ($match in [regex]::Matches($content, 'UiText\.(?:Get|Format)\("([^"]+)"')) {
        [void]$sourceKeys.Add($match.Groups[1].Value)
    }
}

$errors = [System.Collections.Generic.List[string]]::new()
foreach ($catalogPath in $catalogPaths) {
    foreach ($key in $sourceKeys) {
        if (-not $catalogs[$catalogPath].ContainsKey($key)) {
            $errors.Add("$(Split-Path -Leaf $catalogPath) is missing runtime key '$key'.")
        }
    }
    foreach ($id in $catalogs[$catalogPath].Keys) {
        $translation = $translations[$catalogPath][$id]
        if ([string]::IsNullOrEmpty($translation)) {
            $errors.Add("$(Split-Path -Leaf $catalogPath) has an empty translation for '$id'.")
        }
        if ($translation -match '(?i)\bticks?\b') {
            $errors.Add("$(Split-Path -Leaf $catalogPath) exposes internal ticks in player-facing key '$id'.")
        }
    }
}

$enPath = $catalogPaths[0]
$esPath = $catalogPaths[1]
foreach ($id in $catalogs[$enPath].Keys) {
    if (-not $catalogs[$esPath].ContainsKey($id)) { continue }
    $enPlaceholders = Get-FormatPlaceholders -Text $translations[$enPath][$id]
    $esPlaceholders = Get-FormatPlaceholders -Text $translations[$esPath][$id]
    if (($enPlaceholders -join '|') -ne ($esPlaceholders -join '|')) {
        $errors.Add("Placeholder mismatch for '$id': EN [$($enPlaceholders -join ', ')] vs ES [$($esPlaceholders -join ', ')].")
    }
}

$templateLines = [System.Collections.Generic.List[string]]::new()
$templateLines.Add('# Generated by tools/Test-LocalizationCatalog.ps1 -UpdateTemplate.')
$templateLines.Add('# Edit en.po and es.po; do not translate strings in this template.')
$templateLines.Add('msgid ""')
$templateLines.Add('msgstr ""')
$templateLines.Add('"Content-Type: text/plain; charset=UTF-8\n"')
$templateLines.Add('')
foreach ($id in @($allIds) | Sort-Object) {
    $templateLines.Add("msgid `"$id`"")
    $templateLines.Add('msgstr ""')
    $templateLines.Add('')
}
$expectedTemplate = ($templateLines -join "`n").TrimEnd() + "`n"

if ($UpdateTemplate) {
    [System.IO.File]::WriteAllText($templatePath, $expectedTemplate, [System.Text.UTF8Encoding]::new($false))
}
else {
    $actualTemplate = (Get-Content -Raw -LiteralPath $templatePath -Encoding utf8) -replace "`r`n", "`n"
    if ($actualTemplate -ne $expectedTemplate) {
        $errors.Add('messages.pot is stale. Run tools/Test-LocalizationCatalog.ps1 -UpdateTemplate.')
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Localization catalogs valid: $($allIds.Count) template IDs, $($sourceKeys.Count) runtime keys."

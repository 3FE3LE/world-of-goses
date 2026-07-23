param(
    [string[]]$Lineage = @(),
    [ValidateSet('male', 'female')]
    [string[]]$Gender = @(),
    [string[]]$Set = @(),
    [string]$Output = 'dist/world-of-goses-lpc-lineages',
    [string]$Zip = 'dist/world-of-goses-lpc-lineages-godot4.zip',
    [switch]$NoZip
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $Root

$VenvPython = Join-Path $Root '.venv/Scripts/python.exe'
if (-not (Test-Path $VenvPython)) {
    $Launcher = Get-Command py -ErrorAction SilentlyContinue
    if ($Launcher) {
        & py -3 -m venv .venv
    } else {
        & python -m venv .venv
    }
}

& $VenvPython -m pip install --disable-pip-version-check -q -r source/requirements.txt

$Arguments = @('source/generate_lineage_sprites.py', '--output', $Output)
if ($NoZip) {
    $Arguments += '--no-zip'
} else {
    $Arguments += @('--zip', $Zip)
}
foreach ($Item in $Lineage) { $Arguments += @('--lineage', $Item) }
foreach ($Item in $Gender) { $Arguments += @('--gender', $Item) }
foreach ($Item in $Set) { $Arguments += @('--set', $Item) }

& $VenvPython @Arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

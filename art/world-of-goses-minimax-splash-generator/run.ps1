[CmdletBinding()]
param(
    [switch]$All,
    [string]$Only,
    [switch]$DryRun,
    [switch]$Force,
    [switch]$Yes,
    [string]$ProjectRoot
)

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$VenvRoot = Join-Path $ScriptRoot ".venv"
$PythonExe = Join-Path $VenvRoot "Scripts\python.exe"

function Resolve-SystemPython {
    if (Get-Command py -ErrorAction SilentlyContinue) {
        return @("py", "-3")
    }
    if (Get-Command python -ErrorAction SilentlyContinue) {
        return @("python")
    }
    throw "Python 3 was not found. Install Python 3.11+."
}

if (-not (Test-Path $PythonExe)) {
    Write-Host "Creating local Python environment..."
    $SystemPython = Resolve-SystemPython
    $Command = $SystemPython[0]
    $PrefixArgs = @()
    if ($SystemPython.Count -gt 1) {
        $PrefixArgs = $SystemPython[1..($SystemPython.Count - 1)]
    }

    & $Command @PrefixArgs -m venv $VenvRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Could not create the Python environment."
    }

    & $PythonExe -m pip install --upgrade pip
    & $PythonExe -m pip install -r (Join-Path $ScriptRoot "requirements.txt")
    if ($LASTEXITCODE -ne 0) {
        throw "Could not install Python dependencies."
    }
}

if (-not $DryRun -and [string]::IsNullOrWhiteSpace($env:MINIMAX_API_KEY)) {
    $SecureKey = Read-Host "Paste MINIMAX_API_KEY (hidden)" -AsSecureString
    $Pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureKey)
    try {
        $env:MINIMAX_API_KEY = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($Pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($Pointer)
    }
}

if (-not $All -and [string]::IsNullOrWhiteSpace($Only)) {
    $Only = "ardhen_male"
    Write-Host "Safe default: generating only ardhen_male."
    Write-Host "After reviewing it, run .\run.ps1 -All"
}

if ($All -and -not $Yes -and -not $DryRun) {
    Write-Warning "This will make 16 paid MiniMax requests."
    $Confirmation = Read-Host "Type GENERATE 16 to continue"
    if ($Confirmation -ne "GENERATE 16") {
        Write-Host "Cancelled."
        exit 0
    }
}

$Arguments = @(
    (Join-Path $ScriptRoot "generate_lineage_splashes.py"),
    "--prompts", (Join-Path $ScriptRoot "prompts.json")
)

if ($All) {
    $Arguments += "--all"
}
elseif (-not [string]::IsNullOrWhiteSpace($Only)) {
    $Arguments += @("--only", $Only)
}

if ($DryRun) {
    $Arguments += "--dry-run"
}
if ($Force) {
    $Arguments += "--force"
}
if (-not [string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $Arguments += @("--project-root", $ProjectRoot)
}

& $PythonExe @Arguments
exit $LASTEXITCODE

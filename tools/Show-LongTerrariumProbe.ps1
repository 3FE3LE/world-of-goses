param(
    [ValidateSet(8, 16, 21)]
    [int]$Rows = 8,
    [ValidateSet(3, 9)]
    [int]$Columns = 9,
    [string]$GodotPath = "C:\Tools\Godot\Godot_v4.7.1-stable_mono_win64.exe"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$gamePath = Join-Path $repoRoot "game"
if (($Rows -eq 8 -and $Columns -ne 9) -or ($Rows -ne 8 -and $Columns -ne 3)) {
    throw "Use 8x9 for the current probe, or 16x3 / 21x3 for historical probes."
}
$fixture = if ($Rows -eq 8 -and $Columns -eq 9) {
    "terrarium-8x9-window"
}
elseif ($Rows -eq 16) {
    "long-terrarium-16-rows"
}
else {
    "long-terrarium-20-rows"
}
$logPath = Join-Path ([System.IO.Path]::GetTempPath()) "wog-$fixture.log"
$previousCaptureMode = [System.Environment]::GetEnvironmentVariable(
    "WOG_VISUAL_CAPTURE",
    [System.EnvironmentVariableTarget]::Process)

try {
    [System.Environment]::SetEnvironmentVariable(
        "WOG_VISUAL_CAPTURE",
        "1",
        [System.EnvironmentVariableTarget]::Process)
    $process = Start-Process -FilePath $GodotPath -ArgumentList @(
        "--path", $gamePath,
        "--log-file", $logPath,
        "--resolution", "1280x720",
        "--",
        "--wog-visual-capture",
        "--wog-visual-fixture=$fixture"
    ) -PassThru
}
finally {
    [System.Environment]::SetEnvironmentVariable(
        "WOG_VISUAL_CAPTURE",
        $previousCaptureMode,
        [System.EnvironmentVariableTarget]::Process)
}

Write-Host "Opened the $Rows-by-$Columns terrarium probe. Live saves remain untouched."
Write-Host "Process: $($process.Id)"
Write-Host "Log: $logPath"

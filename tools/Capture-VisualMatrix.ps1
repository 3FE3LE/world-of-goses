[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GodotPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [string]$StateName = "macro-current",
    [string]$ScenePath = "res://scenes/CityPrototype.tscn",
    [string]$VisualFixture = "",
    [int]$StartupDelaySeconds = 2,
    [string[]]$NormalizedClicks = @()
)

$ErrorActionPreference = "Stop"
$projectPath = Resolve-Path (Join-Path $PSScriptRoot "..\game")
$resolvedGodot = Resolve-Path -LiteralPath $GodotPath
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $resolvedOutput | Out-Null

$resolutions = @(
    [PSCustomObject]@{ Width = 1024; Height = 576 },
    [PSCustomObject]@{ Width = 1280; Height = 720 },
    [PSCustomObject]@{ Width = 1600; Height = 900 }
)
$captures = @()

Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class VisualMatrixWindowCapture
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect { public int Left; public int Top; public int Right; public int Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    public struct Point { public int X; public int Y; }
    [DllImport("user32.dll")]
    public static extern bool GetClientRect(IntPtr handle, out Rect rect);
    [DllImport("user32.dll")]
    public static extern bool ClientToScreen(IntPtr handle, ref Point point);
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr handle, IntPtr insertAfter,
        int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")]
    public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
    public static readonly IntPtr TopMost = new IntPtr(-1);
    public const uint NoMove = 0x0002;
    public const uint NoSize = 0x0001;
    public const uint ShowWindow = 0x0040;
    public const uint LeftDown = 0x0002;
    public const uint LeftUp = 0x0004;
    public const uint RightDown = 0x0008;
    public const uint RightUp = 0x0010;
}
"@

foreach ($resolution in $resolutions) {
    $slug = "$($resolution.Width)x$($resolution.Height)"
    $framePath = Join-Path $resolvedOutput "$StateName-$slug.png"
    $logPath = Join-Path $resolvedOutput "$StateName-$slug-godot.log"
    $previousCaptureMode = [System.Environment]::GetEnvironmentVariable(
        "WOG_VISUAL_CAPTURE",
        [System.EnvironmentVariableTarget]::Process)
    [System.Environment]::SetEnvironmentVariable(
        "WOG_VISUAL_CAPTURE",
        "1",
        [System.EnvironmentVariableTarget]::Process)
    try {
        $arguments = @(
            "--path", $projectPath.Path,
            "--log-file", $logPath,
            $ScenePath,
            "--resolution", $slug,
            "--position", "0,0",
            "--", "--wog-visual-capture"
        )
        if (![string]::IsNullOrWhiteSpace($VisualFixture)) {
            $arguments += "--wog-visual-fixture=$VisualFixture"
        }
        $process = Start-Process -FilePath $resolvedGodot.Path -ArgumentList $arguments -PassThru
    }
    finally {
        [System.Environment]::SetEnvironmentVariable(
            "WOG_VISUAL_CAPTURE",
            $previousCaptureMode,
            [System.EnvironmentVariableTarget]::Process)
    }
    try {
        $deadline = [DateTime]::UtcNow.AddSeconds(10)
        do {
            Start-Sleep -Milliseconds 100
            $process.Refresh()
        } while ($process.MainWindowHandle -eq 0 -and !$process.HasExited -and [DateTime]::UtcNow -lt $deadline)
        if ($process.HasExited -or $process.MainWindowHandle -eq 0) {
            throw "Godot did not expose a window for $StateName at $slug."
        }

        Start-Sleep -Seconds $StartupDelaySeconds
        $rect = New-Object VisualMatrixWindowCapture+Rect
        $origin = New-Object VisualMatrixWindowCapture+Point
        if (![VisualMatrixWindowCapture]::GetClientRect($process.MainWindowHandle, [ref]$rect) `
            -or ![VisualMatrixWindowCapture]::ClientToScreen($process.MainWindowHandle, [ref]$origin)) {
            throw "Could not read Godot client bounds for $StateName at $slug."
        }
        $actualWidth = $rect.Right - $rect.Left
        $actualHeight = $rect.Bottom - $rect.Top
        if ($actualWidth -ne $resolution.Width -or $actualHeight -ne $resolution.Height) {
            throw "Godot client is ${actualWidth}x${actualHeight}, expected $slug."
        }

        [VisualMatrixWindowCapture]::SetWindowPos(
            $process.MainWindowHandle,
            [VisualMatrixWindowCapture]::TopMost,
            0, 0, 0, 0,
            [VisualMatrixWindowCapture]::NoMove -bor
                [VisualMatrixWindowCapture]::NoSize -bor
                [VisualMatrixWindowCapture]::ShowWindow) | Out-Null
        [VisualMatrixWindowCapture]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
        Start-Sleep -Milliseconds 250

        foreach ($click in $NormalizedClicks) {
            # Optional "R:" prefix simulates a right click (e.g. "R:0.5,0.6");
            # no prefix (or "L:") is a left click — the default.
            $isRightClick = $false
            $coords = $click
            if ($click -match '^[Rr]:(.+)$') {
                $isRightClick = $true
                $coords = $Matches[1]
            }
            elseif ($click -match '^[Ll]:(.+)$') {
                $coords = $Matches[1]
            }
            $parts = $coords.Split(',')
            if ($parts.Count -ne 2) { throw "Invalid normalized click '$click'. Use [L:|R:]X,Y." }
            $normalX = [double]::Parse($parts[0], [Globalization.CultureInfo]::InvariantCulture)
            $normalY = [double]::Parse($parts[1], [Globalization.CultureInfo]::InvariantCulture)
            if ($normalX -lt 0 -or $normalX -gt 1 -or $normalY -lt 0 -or $normalY -gt 1) {
                throw "Normalized click '$click' must stay within 0..1."
            }
            $screenX = $origin.X + [int]($actualWidth * $normalX)
            $screenY = $origin.Y + [int]($actualHeight * $normalY)
            [VisualMatrixWindowCapture]::SetCursorPos($screenX, $screenY) | Out-Null
            $downFlag = if ($isRightClick) { [VisualMatrixWindowCapture]::RightDown } else { [VisualMatrixWindowCapture]::LeftDown }
            $upFlag = if ($isRightClick) { [VisualMatrixWindowCapture]::RightUp } else { [VisualMatrixWindowCapture]::LeftUp }
            [VisualMatrixWindowCapture]::mouse_event($downFlag, 0, 0, 0, [UIntPtr]::Zero)
            [VisualMatrixWindowCapture]::mouse_event($upFlag, 0, 0, 0, [UIntPtr]::Zero)
            Start-Sleep -Milliseconds 350
        }

        # The screenshot is the primary artifact this harness exists to
        # produce; capture it before the frame-time sample so a perf
        # spike (measured below) never costs us the visual evidence.
        $bitmap = New-Object System.Drawing.Bitmap($actualWidth, $actualHeight)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.CopyFromScreen($origin.X, $origin.Y, 0, 0, $bitmap.Size)
            $bitmap.Save($framePath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $graphics.Dispose()
            $bitmap.Dispose()
        }

        # Frame-time sampling (S-1.7, reworked 2026-07-27). The prior
        # version measured PowerShell host Start-Sleep interval drift —
        # blind to any real stall inside the Godot process (see TO_DO.md
        # S-1.7's audit). CityWorldController.SampleFrameTimeForVisualCapture
        # instead prints the engine's own Performance.Monitor.TimeProcess
        # every frame (tagged, capped at 300 samples) whenever
        # WOG_VISUAL_CAPTURE is set. By now the process has been running
        # for several seconds (startup + optional clicks + screenshot), so
        # plenty of real frames are already in the log; a short extra wait
        # only guards the case where NormalizedClicks was empty and the
        # screenshot came back almost immediately.
        Start-Sleep -Milliseconds 500
        $frameTimeTag = "[WOG-FRAME-TIME]"
        $frameSamples = New-Object System.Collections.Generic.List[double]
        if (Test-Path -LiteralPath $logPath) {
            $tagLines = Select-String -LiteralPath $logPath -SimpleMatch $frameTimeTag
            $lastLines = $tagLines | Select-Object -Last 30
            foreach ($lineMatch in $lastLines) {
                $valueText = $lineMatch.Line.Substring(
                    $lineMatch.Line.IndexOf($frameTimeTag) + $frameTimeTag.Length).Trim()
                [double]$parsed = 0
                if ([double]::TryParse($valueText, [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
                    $frameSamples.Add($parsed)
                }
            }
        }
        $frameTimePath = Join-Path $resolvedOutput "$StateName-$slug-frame-time.json"
        $frameSamples | ConvertTo-Json -AsArray | Set-Content -LiteralPath $frameTimePath -Encoding utf8
        if ($frameSamples.Count -eq 0) {
            Write-Warning "No $frameTimeTag samples found in the log at $StateName ${slug} — frame budget not verified this run."
        }
        else {
            # Deliberately Write-Warning, not a terminating failure: the
            # screenshot above is the primary artifact this harness exists
            # to produce, and a perf regression must never cost it.
            $maxFrame = ($frameSamples | Measure-Object -Maximum).Maximum
            if ($maxFrame -gt 40.0) {
                Write-Warning "Frame budget exceeded at $StateName ${slug}: max $maxFrame ms (> 40 ms spike budget)."
            }
        }
    }
    finally {
        if (!$process.HasExited) { Stop-Process -Id $process.Id }
    }
    $frame = Get-Item -LiteralPath $framePath
    if ($frame.Length -le 0) {
        throw "No non-empty PNG was produced for $StateName at $slug."
    }
    $captures += [PSCustomObject]@{
        State = $StateName
        Width = $resolution.Width
        Height = $resolution.Height
        Scene = $ScenePath
        VisualFixture = $VisualFixture
        NormalizedClicks = $NormalizedClicks
        File = $frame.Name
        Bytes = $frame.Length
        CapturedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    }
}

$manifestPath = Join-Path $resolvedOutput "$StateName-manifest.json"
$captures | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $manifestPath -Encoding utf8
$captures

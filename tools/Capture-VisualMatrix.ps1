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
            $parts = $click.Split(',')
            if ($parts.Count -ne 2) { throw "Invalid normalized click '$click'. Use X,Y." }
            $normalX = [double]::Parse($parts[0], [Globalization.CultureInfo]::InvariantCulture)
            $normalY = [double]::Parse($parts[1], [Globalization.CultureInfo]::InvariantCulture)
            if ($normalX -lt 0 -or $normalX -gt 1 -or $normalY -lt 0 -or $normalY -gt 1) {
                throw "Normalized click '$click' must stay within 0..1."
            }
            $screenX = $origin.X + [int]($actualWidth * $normalX)
            $screenY = $origin.Y + [int]($actualHeight * $normalY)
            [VisualMatrixWindowCapture]::SetCursorPos($screenX, $screenY) | Out-Null
            [VisualMatrixWindowCapture]::mouse_event(
                [VisualMatrixWindowCapture]::LeftDown, 0, 0, 0, [UIntPtr]::Zero)
            [VisualMatrixWindowCapture]::mouse_event(
                [VisualMatrixWindowCapture]::LeftUp, 0, 0, 0, [UIntPtr]::Zero)
            Start-Sleep -Milliseconds 350
        }

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

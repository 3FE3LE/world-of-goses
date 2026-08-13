<#
.SYNOPSIS
    Produces the golden frames for the visual matrix.

.DESCRIPTION
    Two things happen here and they are deliberately kept apart (GitHub #2):

    GOLDEN CAPTURE is taken by the engine, from its own viewport, via
    `ViewportCaptureService`. It used to be `Graphics.CopyFromScreen` over the
    Godot window's screen rectangle, which is a copy of the desktop rather
    than of the game — and a run duly captured a Chrome/Google Meet window and
    filed it as a valid golden frame. No amount of raising and focusing the
    window fixes that class of bug, because `SetForegroundWindow` fails
    silently for a process that does not already own the foreground and
    another application's always-on-top window outranks a topmost one anyway.
    Reading the pixels out of the engine removes the desktop from the golden
    path entirely, so the artifact provably belongs to the launched process
    and fixture. There is no fallback: a capture that cannot be taken from the
    viewport fails.

    INPUT E2E still needs the real desktop, because the whole point of those
    fixtures is to exercise the actual pointer route rather than to synthesise
    an event inside the engine. That path therefore keeps the window
    manipulation — and now asserts, rather than hopes, that the Godot window
    is genuinely frontmost and genuinely under the cursor at the moment each
    click is sent. A click that would land on another window fails loudly
    instead of producing a screenshot of a city that never received it.

    Waits are on observable conditions rather than on hopeful sleeps wherever
    determinism required it: the harness polls until the client rect actually
    matches the requested resolution instead of sleeping and asserting once.
    The broader timer cleanup is GitHub #7 and is deliberately not done here.

.PARAMETER StartupSettleMilliseconds
    Upper bound on how long to wait for the window to reach its requested
    size. This is a deadline for a polled condition, not a fixed sleep.
#>
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
    [int]$StartupSettleMilliseconds = 15000,
    [string[]]$NormalizedClicks = @()
)

$ErrorActionPreference = "Stop"
$projectPath = Resolve-Path (Join-Path $PSScriptRoot "..\game")
$resolvedGodot = Resolve-Path -LiteralPath $GodotPath
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $resolvedOutput | Out-Null

$resolutions = @(
    [PSCustomObject]@{ Width = 1280; Height = 720 },
    [PSCustomObject]@{ Width = 1920; Height = 1080 }
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
    // Used to prove, rather than assume, that a synthesised click will reach
    // the game: WindowFromPoint resolves whatever window actually owns the
    // pixel under the cursor, and GetAncestor(..., GA_ROOT) lifts a child
    // control back to its top-level window so the comparison against Godot's
    // MainWindowHandle is apples to apples.
    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")]
    public static extern IntPtr GetAncestor(IntPtr handle, uint flags);
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr handle, IntPtr processId);
    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint attachTo, uint attachFrom, bool attach);
    [DllImport("user32.dll")]
    public static extern bool BringWindowToTop(IntPtr handle);
    // Named ShowWindowCommand because the SetWindowPos flag constant below
    // already claims the plain `ShowWindow` name.
    [DllImport("user32.dll", EntryPoint = "ShowWindow")]
    public static extern bool ShowWindowCommand(IntPtr handle, int command);
    public const int Restore = 9;
    public const uint GaRoot = 2;

    /// <summary>
    /// Takes the foreground for real. A bare SetForegroundWindow is a
    /// request, not a command: Windows refuses it when the calling process
    /// does not already own the foreground, and it fails by returning false
    /// rather than by raising. The input-E2E fixtures were built on that
    /// silent failure — the click still fired, into whatever window was
    /// actually in front. Attaching our input queue to the current
    /// foreground thread for the duration of the call is the supported way
    /// to lift that restriction for automation.
    /// </summary>
    public static bool ForceForeground(IntPtr handle)
    {
        IntPtr current = GetForegroundWindow();
        if (current == handle) return true;

        uint foregroundThread = GetWindowThreadProcessId(current, IntPtr.Zero);
        uint thisThread = GetCurrentThreadId();
        bool attached = foregroundThread != thisThread
            && AttachThreadInput(thisThread, foregroundThread, true);
        try
        {
            ShowWindowCommand(handle, Restore);
            BringWindowToTop(handle);
            SetForegroundWindow(handle);
        }
        finally
        {
            if (attached) AttachThreadInput(thisThread, foregroundThread, false);
        }
        return GetForegroundWindow() == handle;
    }
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
            # --windowed before --resolution, and both before the scene runs.
            # The project ships fullscreen borderless (display/window/size/mode
            # = 3), and a fullscreen window ignores --resolution outright: it
            # takes the desktop's size instead. That is how a 2560x1440 desktop
            # silently turned every golden frame into a 2560x1440 render that
            # no longer matched the matrix slug. The harness has to pin its own
            # window rather than inherit however the game happens to ship.
            "--windowed",
            "--resolution", $slug,
            "--position", "0,0",
            "--", "--wog-visual-capture",
            "--wog-visual-capture-out=$framePath",
            "--wog-visual-capture-size=$slug"
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

        # Poll until the client rect has settled, rather than sleeping a fixed
        # interval and asserting once. Godot reports a 50x50 bootstrap client
        # for the first frames of a cold start; the old fixed sleep meant a
        # slow machine failed the assertion outright and a fast one raced it.
        # Clicks derived from bootstrap geometry would land nowhere near their
        # target, so this has to be settled before any coordinate is computed.
        #
        # Settled means *proportional to* the requested resolution, not equal
        # to it. On a HiDPI desktop the window manager hands back physical
        # pixels — 2560x1442 for a 1280x720 request at 200% scale — and the
        # old equality check rejected every capture on such a machine, which
        # is how visual sign-off went dark for several sessions. Nothing
        # downstream needs the rect to be 1:1: clicks are normalized and
        # multiplied by the measured size, and the golden frame comes from the
        # engine's own viewport, which reports the size it truly rendered and
        # is asserted against the slug further down. What must still be
        # rejected is the bootstrap rect, and its square 1:1 aspect is what
        # separates it from a legitimately scaled window.
        $rect = New-Object VisualMatrixWindowCapture+Rect
        $origin = New-Object VisualMatrixWindowCapture+Point
        $actualWidth = 0
        $actualHeight = 0
        $requestedAspect = $resolution.Width / $resolution.Height
        $settleDeadline = [DateTime]::UtcNow.AddMilliseconds($StartupSettleMilliseconds)
        do {
            if (![VisualMatrixWindowCapture]::GetClientRect($process.MainWindowHandle, [ref]$rect) `
                -or ![VisualMatrixWindowCapture]::ClientToScreen($process.MainWindowHandle, [ref]$origin)) {
                throw "Could not read Godot client bounds for $StateName at $slug."
            }
            $actualWidth = $rect.Right - $rect.Left
            $actualHeight = $rect.Bottom - $rect.Top
            if ($actualWidth -ge $resolution.Width -and $actualHeight -gt 0) {
                $aspectDrift = [Math]::Abs(($actualWidth / $actualHeight) - $requestedAspect)
                if ($aspectDrift -le ($requestedAspect * 0.02)) { break }
            }
            Start-Sleep -Milliseconds 100
        } while (!$process.HasExited -and [DateTime]::UtcNow -lt $settleDeadline)

        $settledAspect = if ($actualHeight -gt 0) { $actualWidth / $actualHeight } else { 0 }
        if ($actualWidth -lt $resolution.Width `
            -or [Math]::Abs($settledAspect - $requestedAspect) -gt ($requestedAspect * 0.02)) {
            throw ("Godot client settled at ${actualWidth}x${actualHeight}, which is not " +
                "proportional to the requested $slug for $StateName. Clicks and geometry " +
                "cannot be derived from a bootstrap rect.")
        }

        # The scene still needs a moment to compose once the window is the
        # right size. Fixture composition has no observable signal to wait on
        # yet; replacing this one is GitHub #7.
        Start-Sleep -Seconds $StartupDelaySeconds

        [VisualMatrixWindowCapture]::SetWindowPos(
            $process.MainWindowHandle,
            [VisualMatrixWindowCapture]::TopMost,
            0, 0, 0, 0,
            [VisualMatrixWindowCapture]::NoMove -bor
                [VisualMatrixWindowCapture]::NoSize -bor
                [VisualMatrixWindowCapture]::ShowWindow) | Out-Null
        # Only the input-E2E path needs the foreground. The golden frame comes
        # from the engine's viewport and does not care what is on screen, so
        # a fixture with no clicks must not fail merely because the desktop
        # was busy.
        if ($NormalizedClicks.Count -gt 0) {
            $gotForeground = $false
            $foregroundDeadline = [DateTime]::UtcNow.AddSeconds(5)
            do {
                $gotForeground = [VisualMatrixWindowCapture]::ForceForeground($process.MainWindowHandle)
                if ($gotForeground) { break }
                Start-Sleep -Milliseconds 150
            } while ([DateTime]::UtcNow -lt $foregroundDeadline)

            if (!$gotForeground) {
                throw ("Input E2E aborted at $StateName ${slug}: could not bring the Godot window to " +
                    "the foreground, so synthesised clicks would land in another application. " +
                    "Run input fixtures on an interactive desktop that is not being driven by " +
                    "another foreground-stealing process.")
            }
            Start-Sleep -Milliseconds 250
        }

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

            # A click that misses the game must fail, not quietly produce a
            # screenshot of a city that never received it. Two independent
            # checks, because they catch different things: the foreground
            # check catches focus theft between clicks, and the hit test
            # catches an overlay sitting on top of the exact pixel we are
            # about to press even while Godot nominally has focus.
            $foreground = [VisualMatrixWindowCapture]::GetForegroundWindow()
            $foregroundRoot = [VisualMatrixWindowCapture]::GetAncestor(
                $foreground, [VisualMatrixWindowCapture]::GaRoot)
            if ($foregroundRoot -ne $process.MainWindowHandle) {
                throw ("Input E2E aborted at $StateName ${slug}: the foreground window is not the " +
                    "launched Godot process, so click '$click' would have been delivered elsewhere.")
            }

            $hitPoint = New-Object VisualMatrixWindowCapture+Point
            $hitPoint.X = $screenX
            $hitPoint.Y = $screenY
            $hitRoot = [VisualMatrixWindowCapture]::GetAncestor(
                [VisualMatrixWindowCapture]::WindowFromPoint($hitPoint),
                [VisualMatrixWindowCapture]::GaRoot)
            if ($hitRoot -ne $process.MainWindowHandle) {
                throw ("Input E2E aborted at $StateName ${slug}: another window owns the pixel at " +
                    "($screenX,$screenY), so click '$click' would have missed the game.")
            }

            $downFlag = if ($isRightClick) { [VisualMatrixWindowCapture]::RightDown } else { [VisualMatrixWindowCapture]::LeftDown }
            $upFlag = if ($isRightClick) { [VisualMatrixWindowCapture]::RightUp } else { [VisualMatrixWindowCapture]::LeftUp }
            [VisualMatrixWindowCapture]::mouse_event($downFlag, 0, 0, 0, [UIntPtr]::Zero)
            [VisualMatrixWindowCapture]::mouse_event($upFlag, 0, 0, 0, [UIntPtr]::Zero)
            Start-Sleep -Milliseconds 350
        }

        # The screenshot is the primary artifact this harness exists to
        # produce; capture it before the frame-time sample so a perf
        # spike (measured below) never costs us the visual evidence.
        #
        # Written by the engine from its own viewport (ViewportCaptureService),
        # never copied off the desktop. The handshake is three files: we drop
        # a .request, the engine answers with .done (carrying the dimensions
        # it actually rendered) or .failed. There is deliberately no
        # CopyFromScreen fallback — falling back to the desktop is how an
        # unrelated window became a golden frame in the first place.
        $requestPath = "$framePath.request"
        $donePath = "$framePath.done"
        $failedPath = "$framePath.failed"
        if (Test-Path -LiteralPath $framePath) { Remove-Item -LiteralPath $framePath -Force }
        Set-Content -LiteralPath $requestPath -Value "capture" -Encoding utf8

        $captureDeadline = [DateTime]::UtcNow.AddSeconds(30)
        while (!(Test-Path -LiteralPath $donePath) -and !(Test-Path -LiteralPath $failedPath)) {
            if ($process.HasExited) {
                throw "Godot exited before writing the golden frame for $StateName at $slug."
            }
            if ([DateTime]::UtcNow -ge $captureDeadline) {
                throw ("Timed out waiting for the engine to write the golden frame for " +
                    "$StateName at $slug. See $logPath.")
            }
            Start-Sleep -Milliseconds 100
        }
        if (Test-Path -LiteralPath $failedPath) {
            $reason = (Get-Content -LiteralPath $failedPath -Raw).Trim()
            throw "Engine-side capture failed for $StateName at ${slug}: $reason"
        }

        # The engine reports what it rendered. Asserting on that rather than
        # on the window rect means a mismatch between the window manager's
        # idea of the size and the framebuffer's cannot ship as a golden.
        $renderedSize = (Get-Content -LiteralPath $donePath -Raw).Trim()
        if ($renderedSize -ne $slug) {
            throw ("Engine rendered $renderedSize for $StateName but the matrix asked for $slug.")
        }
        Remove-Item -LiteralPath $requestPath, $donePath -Force -ErrorAction SilentlyContinue

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

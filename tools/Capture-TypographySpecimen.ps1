[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GodotPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"
$projectPath = Resolve-Path (Join-Path $PSScriptRoot "..\game")
$resolvedGodot = Resolve-Path -LiteralPath $GodotPath
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $resolvedOutput | Out-Null
Add-Type -AssemblyName System.Drawing

$resolutions = @(
    # The crop sits inside the cap band of the first row (GameTitle, Jacquard 24
    # at 48 px), which is the only row guaranteed to contain nothing but the
    # background colour and one glyph colour. Jacquard 24 puts its ascender at
    # 1020/1290 em, so at 48 px the baseline lands 38 px below the 48 px top
    # margin and the 28 px cap band runs from y=58 to y=86; the crop stays well
    # inside it. The 1920x1080 pair is the same band times the 1.5 canvas_items
    # scale, not an independently measured rectangle.
    [PSCustomObject]@{ Width = 1280; Height = 720; Crop = @(64, 62, 620, 18) },
    [PSCustomObject]@{ Width = 1920; Height = 1080; Crop = @(96, 93, 930, 27) }
)
$captures = @()

foreach ($resolution in $resolutions) {
    $slug = "$($resolution.Width)x$($resolution.Height)"
    $imagePath = Join-Path $resolvedOutput "typography-pixel-perfect-$slug.png"
    $logPath = Join-Path $resolvedOutput "typography-pixel-perfect-$slug-godot.log"
    if (Test-Path -LiteralPath $imagePath) {
        Remove-Item -LiteralPath $imagePath -Force
    }
    $arguments = @(
        "--path", $projectPath.Path,
        "--log-file", $logPath,
        "res://scenes/prototypes/TypographySpecimen.tscn",
        # Same argument order, and for the same reason, as Capture-VisualMatrix:
        # the project ships fullscreen borderless (display/window/size/mode = 3),
        # a fullscreen window ignores --resolution and takes the desktop's size,
        # and this fixture was silently rendering 2560x1440 on a 2560x1440 desktop.
        # --windowed has to come after the scene and before --resolution.
        "--windowed",
        "--resolution", $slug,
        "--position", "0,0",
        "--",
        "--wog-typography-output=$imagePath",
        # The scene shoots once the viewport reports this size, not on a fixed
        # frame, so a window still settling out of fullscreen cannot be filed as
        # a golden frame.
        "--wog-typography-size=$slug"
    )
    $process = Start-Process -FilePath $resolvedGodot.Path -ArgumentList $arguments -PassThru
    try {
        $deadline = [DateTime]::UtcNow.AddSeconds(15)
        while (!(Test-Path -LiteralPath $imagePath) `
            -and !$process.HasExited `
            -and [DateTime]::UtcNow -lt $deadline) {
            Start-Sleep -Milliseconds 100
            $process.Refresh()
        }
    }
    finally {
        if (!$process.HasExited) { Stop-Process -Id $process.Id }
    }
    if (!(Test-Path -LiteralPath $imagePath)) {
        throw "Typography capture did not create $imagePath."
    }

    $bitmap = [System.Drawing.Bitmap]::FromFile($imagePath)
    try {
        if ($bitmap.Width -ne $resolution.Width -or $bitmap.Height -ne $resolution.Height) {
            throw "Typography capture is $($bitmap.Width)x$($bitmap.Height), expected $slug."
        }
        $colors = New-Object System.Collections.Generic.HashSet[string]
        $crop = $resolution.Crop
        for ($y = $crop[1]; $y -lt $crop[1] + $crop[3]; $y++) {
            for ($x = $crop[0]; $x -lt $crop[0] + $crop[2]; $x++) {
                $color = $bitmap.GetPixel($x, $y)
                [void]$colors.Add("$($color.R),$($color.G),$($color.B),$($color.A)")
            }
        }
        if ($colors.Count -ne 2) {
            throw "Typography title crop at $slug contains $($colors.Count) colors; expected solid background and glyph only."
        }
    }
    finally {
        $bitmap.Dispose()
    }

    $file = Get-Item -LiteralPath $imagePath
    $captures += [PSCustomObject]@{
        State = "typography-pixel-perfect"
        Width = $resolution.Width
        Height = $resolution.Height
        File = $file.Name
        Bytes = $file.Length
        TitleCropColorCount = $colors.Count
    }
}

$manifestPath = Join-Path $resolvedOutput "typography-pixel-perfect-manifest.json"
$captures | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $manifestPath -Encoding utf8
$captures

<#
.SYNOPSIS
    Bakes a project-palette fill and border ramp into a hollow Kenney frame tile.

.DESCRIPTION
    The Kenney UI Pack - Pixel Adventure is a light/mid-tone pack: its darkest
    opaque tile centre is luminance 114, while this project's panel surfaces sit
    at 11-17. Its slate tiles also carry only 3-6 distinct tones, so darkening one
    with `modulate_color` until it matches the project compresses the tonal range
    to a few units out of 255 and the texture stops reading as texture at all.
    That is why panels could not simply point at a pack tile.

    The pack does, however, ship frame tiles whose centre is fully transparent
    (`tile_0008`, `tile_0009`, `tile_0019`, `tile_0032` in the Large set). This
    script takes one of those, floods the enclosed interior with a project fill
    colour, and remaps the frame's own tones onto a project border ramp. The
    result is a single 9-sliceable PNG that keeps the authored pixel frame and the
    project's palette at the same time.

    Two rules make the output trustworthy rather than approximate:

    - Only the *enclosed* interior is filled. The fill flood-starts at the centre
      and stops at the frame, so the transparent pixels outside a rounded corner
      stay transparent and the tile keeps its silhouette.
    - The tone remap is exact, not a tint. The frame's distinct opaque tones are
      ranked by luminance and mapped one-to-one onto -FrameRamp, so the ramp must
      supply exactly as many colours as the tile has tones. A mismatch is an
      error, not a silent nearest-fit.

    Writes a `.recipe.json` beside the PNG recording every input, so the asset is
    reproducible from the repository alone. This mirrors the existing generated
    lineage panels under `game/assets/ui/lineages/<lineage>/panel/`, which also
    ship a PNG next to the recipe that produced it.

.PARAMETER FrameTile
    Hollow-centre source tile. Its centre pixel must be transparent.

.PARAMETER OutputPng
    Destination PNG.

.PARAMETER FillRgba
    Interior fill as "r,g,b,a" in 0-255, e.g. "14,17,23,246".

.PARAMETER FrameRamp
    Border colours as "r,g,b", ordered darkest to lightest. Must contain exactly
    one entry per distinct opaque tone in the tile. Pass -ReportTones to discover
    how many that is.

.PARAMETER ReportTones
    Print the tile's distinct opaque tones and exit without writing anything.

.EXAMPLE
    pwsh ./tools/New-CompositeStylebox.ps1 -FrameTile '<tile_0008.png>' -ReportTones

.EXAMPLE
    pwsh ./tools/New-CompositeStylebox.ps1 `
        -FrameTile '<tile_0008.png>' -OutputPng 'game/assets/ui/.../panel_card.png' `
        -FillRgba '14,17,23,246' -FrameRamp '62,53,36','110,94,64','150,128,88'
#>
[CmdletBinding(DefaultParameterSetName = 'Compose')]
param(
    [Parameter(Mandatory)] [string] $FrameTile,
    [Parameter(Mandatory, ParameterSetName = 'Compose')] [string] $OutputPng,
    [Parameter(Mandatory, ParameterSetName = 'Compose')] [string] $FillRgba,
    [Parameter(Mandatory, ParameterSetName = 'Compose')] [string[]] $FrameRamp,
    [Parameter(ParameterSetName = 'Report')] [switch] $ReportTones
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# Anything at or below this alpha counts as a hole the fill may flow through.
# The pack's frames are hard-edged pixel art: pixels are 0 or 255, never partial.
$AlphaFloor = 200

function Get-Luminance([System.Drawing.Color] $c) {
    [int](0.2126 * $c.R + 0.7152 * $c.G + 0.0722 * $c.B)
}

function ConvertTo-Channels([string] $text, [int] $expected) {
    $parts = $text.Split(',')
    if ($parts.Count -ne $expected) {
        throw "Expected $expected comma-separated channels, got '$text'."
    }
    # Build a typed int[] explicitly. Returning a pipeline here would hand back
    # an Object[], which then silently changes how later arithmetic binds.
    $channels = [int[]]::new($expected)
    for ($i = 0; $i -lt $expected; $i++) {
        $v = [int]$parts[$i].Trim()
        if ($v -lt 0 -or $v -gt 255) { throw "Channel out of range 0-255 in '$text'." }
        $channels[$i] = $v
    }
    , $channels
}

if (-not (Test-Path -LiteralPath $FrameTile)) { throw "Frame tile not found: $FrameTile" }
$source = New-Object System.Drawing.Bitmap((Resolve-Path -LiteralPath $FrameTile).Path)

try {
    $width = $source.Width
    $height = $source.Height

    # Rank the frame's own tones by luminance. Pixel art of this kind has a
    # handful of flat tones, so this is a complete description of the frame.
    $toneCounts = @{}
    for ($y = 0; $y -lt $height; $y++) {
        for ($x = 0; $x -lt $width; $x++) {
            $c = $source.GetPixel($x, $y)
            if ($c.A -gt $AlphaFloor) {
                $key = "$($c.R),$($c.G),$($c.B)"
                $toneCounts[$key] = 1 + ($toneCounts[$key] ?? 0)
            }
        }
    }
    $tones = $toneCounts.Keys | ForEach-Object {
        $ch = ConvertTo-Channels $_ 3
        [pscustomobject]@{
            Key = $_
            Lum = [int](0.2126 * $ch[0] + 0.7152 * $ch[1] + 0.0722 * $ch[2])
        }
    } | Sort-Object Lum

    if ($ReportTones) {
        Write-Host "$FrameTile - ${width}x${height}, $($tones.Count) distinct opaque tones (dark to light):"
        foreach ($t in $tones) {
            Write-Host ("   {0,-14} lum {1,3}  {2} px" -f $t.Key, $t.Lum, $toneCounts[$t.Key])
        }
        $centre = $source.GetPixel([int]($width / 2), [int]($height / 2))
        Write-Host ("   centre alpha = {0} ({1})" -f $centre.A,
            $(if ($centre.A -le $AlphaFloor) { 'hollow, usable' } else { 'SOLID - not a frame tile' }))
        return
    }

    $centre = $source.GetPixel([int]($width / 2), [int]($height / 2))
    if ($centre.A -gt $AlphaFloor) {
        throw "$FrameTile has an opaque centre (alpha $($centre.A)); it is not a hollow frame tile."
    }
    if ($FrameRamp.Count -ne $tones.Count) {
        throw ("$FrameTile has $($tones.Count) distinct tones but -FrameRamp supplies " +
               "$($FrameRamp.Count). Pass -ReportTones to list them; the mapping is one-to-one by luminance.")
    }

    $fill = ConvertTo-Channels $FillRgba 4
    $fillColor = [System.Drawing.Color]::FromArgb($fill[3], $fill[0], $fill[1], $fill[2])

    $rampByTone = @{}
    for ($i = 0; $i -lt $tones.Count; $i++) {
        $ch = ConvertTo-Channels $FrameRamp[$i] 3
        $rampByTone[$tones[$i].Key] = [System.Drawing.Color]::FromArgb(255, $ch[0], $ch[1], $ch[2])
    }

    # Flood the enclosed interior only. Starting at the centre and refusing to
    # cross the frame is what keeps the transparent pixels outside a rounded
    # corner transparent -- a blanket "fill every transparent pixel" would square
    # the tile off and destroy its silhouette.
    # Cells are held as a single packed index (y * width + x) so the frontier is a
    # Stack[int]. Pushing coordinate pairs would mean pushing arrays, and the
    # conversions that involves are exactly what this script does not need.
    $interior = [bool[]]::new($width * $height)
    $visited = [bool[]]::new($width * $height)
    $stack = [System.Collections.Generic.Stack[int]]::new()
    $stack.Push(([int]($height / 2) * $width) + [int]($width / 2))

    while ($stack.Count -gt 0) {
        $index = $stack.Pop()
        if ($visited[$index]) { continue }
        $visited[$index] = $true

        $x = $index % $width
        $y = [int][math]::Floor($index / $width)
        if ($source.GetPixel($x, $y).A -gt $AlphaFloor) { continue }   # frame: stop here
        $interior[$index] = $true

        if ($x -gt 0) { $stack.Push($index - 1) }
        if ($x -lt ($width - 1)) { $stack.Push($index + 1) }
        if ($y -gt 0) { $stack.Push($index - $width) }
        if ($y -lt ($height - 1)) { $stack.Push($index + $width) }
    }

    $output = New-Object System.Drawing.Bitmap($width, $height)
    $filled = 0
    try {
        for ($y = 0; $y -lt $height; $y++) {
            for ($x = 0; $x -lt $width; $x++) {
                $c = $source.GetPixel($x, $y)
                if ($c.A -gt $AlphaFloor) {
                    $output.SetPixel($x, $y, $rampByTone["$($c.R),$($c.G),$($c.B)"])
                }
                elseif ($interior[($y * $width) + $x]) {
                    $output.SetPixel($x, $y, $fillColor)
                    $filled++
                }
                else {
                    $output.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
                }
            }
        }

        $directory = Split-Path -Parent $OutputPng
        if ($directory -and -not (Test-Path -LiteralPath $directory)) {
            New-Item -ItemType Directory -Path $directory -Force | Out-Null
        }
        $output.Save($OutputPng, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $output.Dispose() }

    $mapping = [System.Collections.Generic.List[object]]::new()
    for ($i = 0; $i -lt $tones.Count; $i++) {
        $mapping.Add([ordered]@{ source_tone = $tones[$i].Key; mapped_to = $FrameRamp[$i] })
    }
    $recipe = [ordered]@{
        version     = 1
        generator   = 'tools/New-CompositeStylebox.ps1'
        frame_tile  = ($FrameTile -replace '\\', '/')
        size        = [ordered]@{ width = $width; height = $height }
        fill_rgba   = @($fill)
        frame_ramp  = $mapping.ToArray()
        interior_px = $filled
        note        = 'Interior is flood-filled from the centre; pixels outside the frame silhouette stay transparent.'
    }
    $recipePath = [IO.Path]::ChangeExtension($OutputPng, '.recipe.json')
    $recipe | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $recipePath -Encoding utf8

    Write-Host ("{0}  <- {1}  fill=({2}) interior={3}px ramp={4} tones" -f `
        (Split-Path $OutputPng -Leaf), (Split-Path $FrameTile -Leaf), $FillRgba, $filled, $tones.Count)
    Write-Host ("   recipe: {0}" -f (Split-Path $recipePath -Leaf))
}
finally { $source.Dispose() }

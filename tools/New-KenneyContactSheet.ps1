<#
.SYNOPSIS
    Renders a labelled contact sheet from a Kenney tile folder.

.DESCRIPTION
    The Kenney pixel packs ship every sprite as `tile_NNNN.png` with no semantic
    filename and no XML atlas, so a tile can only be identified by looking at it.
    This script composes the individual tiles into one sheet, upscaled with
    nearest-neighbour so the pixels stay crisp, and draws each tile's index
    beneath it. That makes `tile_0002` addressable by eye and lets a promotion
    decision cite a real index instead of a guess.

    Read-only with respect to the pack: it never writes into `art/` or `game/`.
    Point -OutputPath wherever the inspection artifact belongs.

.PARAMETER SourceDirectory
    Folder holding the `tile_NNNN.png` files.

.PARAMETER OutputPath
    Destination PNG for the composed sheet.

.PARAMETER Columns
    Tiles per row in the source pack's own tilesheet layout. Kenney's
    `Tilesheet.txt` states 13 for the Large tiles and 23 for the Small tiles;
    keeping this value means the sheet's rows and columns match the pack's own
    grid, so `index = row * Columns + col` holds.

.PARAMETER Scale
    Integer upscale factor. Nearest-neighbour only.

.PARAMETER FirstIndex
    First tile index to render. Defaults to 0.

.PARAMETER LastIndex
    Last tile index to render. Defaults to every tile found.

.EXAMPLE
    pwsh ./tools/New-KenneyContactSheet.ps1 `
        -SourceDirectory 'art/exports/ui/kenney_ui-pack-pixel-adventure/Tiles/Large tiles/Thick outline' `
        -OutputPath "$env:TEMP/large-thick-panels.png" -Columns 13 -Scale 3 -LastIndex 38
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $SourceDirectory,
    [Parameter(Mandatory)] [string] $OutputPath,
    [int] $Columns = 13,
    [int] $Scale = 3,
    [int] $FirstIndex = 0,
    [int] $LastIndex = [int]::MaxValue
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

if (-not (Test-Path -LiteralPath $SourceDirectory)) {
    throw "Source directory not found: $SourceDirectory"
}
if ($Columns -lt 1) { throw 'Columns must be at least 1.' }
if ($Scale -lt 1) { throw 'Scale must be at least 1.' }

# Order by the numeric suffix, not lexically, so tile_0010 follows tile_0009.
$tiles =
    Get-ChildItem -LiteralPath $SourceDirectory -Filter 'tile_*.png' -File |
    ForEach-Object {
        if ($_.BaseName -match '^tile_(\d+)$') {
            [pscustomobject]@{ Index = [int]$Matches[1]; Path = $_.FullName }
        }
    } |
    Where-Object { $_.Index -ge $FirstIndex -and $_.Index -le $LastIndex } |
    Sort-Object Index

if (-not $tiles) { throw "No tile_NNNN.png files in range in $SourceDirectory" }

# Every tile in a Kenney pack shares one native size; measure the first.
$probe = [System.Drawing.Image]::FromFile($tiles[0].Path)
try {
    $tileWidth = $probe.Width
    $tileHeight = $probe.Height
}
finally { $probe.Dispose() }

$labelHeight = 14
$gutter = 4
$cellWidth = ($tileWidth * $Scale) + $gutter
$cellHeight = ($tileHeight * $Scale) + $labelHeight + $gutter

# Lay the sheet out on the pack's own grid so a cell's position maps back to
# (row, col) and therefore to index = row * Columns + col.
$firstRow = [math]::Floor($tiles[0].Index / $Columns)
$lastRow = [math]::Floor($tiles[-1].Index / $Columns)
$rowCount = ($lastRow - $firstRow) + 1

$sheetWidth = ($Columns * $cellWidth) + $gutter
$sheetHeight = ($rowCount * $cellHeight) + $gutter

$bitmap = New-Object System.Drawing.Bitmap($sheetWidth, $sheetHeight)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$font = New-Object System.Drawing.Font('Consolas', 8)
$labelBrush = [System.Drawing.Brushes]::White

try {
    # Magenta ground: it appears nowhere in these packs, so any leftover shows
    # up as an obvious gap rather than reading as part of a tile.
    $graphics.Clear([System.Drawing.Color]::FromArgb(255, 255, 0, 255))
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None

    foreach ($tile in $tiles) {
        $column = $tile.Index % $Columns
        $row = [math]::Floor($tile.Index / $Columns) - $firstRow

        $x = $gutter + ($column * $cellWidth)
        $y = $gutter + ($row * $cellHeight)

        $image = [System.Drawing.Image]::FromFile($tile.Path)
        try {
            $target = New-Object System.Drawing.Rectangle(
                $x, $y, ($tileWidth * $Scale), ($tileHeight * $Scale))
            $graphics.DrawImage($image, $target)
        }
        finally { $image.Dispose() }

        $label = '{0:0000}' -f $tile.Index
        $graphics.DrawString(
            $label, $font, $labelBrush, $x, ($y + ($tileHeight * $Scale)))
    }

    $directory = Split-Path -Parent $OutputPath
    if ($directory -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $font.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

Write-Host ("Contact sheet: {0} tiles {1}-{2}, {3}x{4} native, {5}x scale -> {6}" -f
    $tiles.Count, $tiles[0].Index, $tiles[-1].Index, $tileWidth, $tileHeight, $Scale, $OutputPath)

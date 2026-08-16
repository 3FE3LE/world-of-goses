<#
.SYNOPSIS
Exports Pixelorama sources into the sheets the game imports, by category.

.DESCRIPTION
One entry point for the art pipeline. Paths are derived from the convention in
`docs/presentation/art-pipeline.md` rather than passed in, so a subject cannot
be exported to the wrong place:

    art/source/<category>/<subject>.pxo
    art/exports/<category>/<subject>_sheet.png     + <subject>.tiles.json
    game/assets/<category>/<subject>_sheet.png

## Why there is a lockfile

Pixelorama tile indices are not stable. Its own manual says that in Auto mode
"tiles that are no longer used anywhere in the tilemap get erased from the
tileset" — so removing the last use of a tile from the canvas renumbers every
tile after it, with no deliberate act of deletion. A profile that names tiles by
id would silently start pointing at different art.

So this identifies a tile by the hash of its pixels, not by its position. The
lockfile records id to hash from the previous export; on the next one a tile
whose hash reappears under a different id is a **move**, and the biome profile's
role ids are rewritten to follow it. Renumbering stops being a hazard and
becomes an accounting entry.

## Extending to other categories

Only the reader varies. A tileset yields tiles of one size from
`tilesets/<index>/<id>`; a building will yield construction phases from
`image_data/frames/<n>/<layer>`; a character will yield animation frames the
same way. Everything downstream — grid composition, hashing, the lockfile diff,
promotion, the report — is shared and category-agnostic. Add a reader and a
post-export step in the two dispatch points marked CATEGORY DISPATCH, and the
rest applies unchanged. Unsupported categories fail by name rather than
pretending, because a silent wrong export is worse than a refusal.

.EXAMPLE
pwsh ./tools/Export-Art.ps1 -Category terrain -Subject eirune_ground

.EXAMPLE
pwsh ./tools/Export-Art.ps1 -All -Check
#>
[CmdletBinding()]
param(
    [string]$Category = "",

    [string]$Subject = "",

    [switch]$All,

    # Report what would change and write nothing. Suitable for CI.
    [switch]$Check,

    # Tiles per row in the composed sheet. The exporter owns the sheet geometry
    # and writes it into the profile, so this cannot drift out of sync with what
    # the game reads; the artist never edits TileSize, Separation or Columns.
    [int]$Columns = 10,

    [int]$TilesetIndex = 0
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO.Compression.FileSystem

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$SupportedCategories = @("terrain")
$AllCategories = @(
    "characters", "buildings", "terrain", "environments", "creatures",
    "items", "emblems", "effects", "audio", "ui")

# ---------------------------------------------------------------- helpers

function Get-TileHash([byte[]]$bytes) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return [System.BitConverter]::ToString($sha.ComputeHash($bytes)).Replace("-", "").ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Read-PxoManifest($archive, [string]$source) {
    $entry = $archive.Entries | Where-Object { $_.FullName -eq "data.json" }
    if ($null -eq $entry) { throw "$source has no data.json; is it a Pixelorama file?" }
    $reader = New-Object System.IO.StreamReader($entry.Open())
    try { return $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Close() }
}

function Read-Entry($archive, [string]$name, [string]$source) {
    $entry = $archive.Entries | Where-Object { $_.FullName -eq $name }
    if ($null -eq $entry) { throw "$source is missing '$name'." }
    $buffer = New-Object System.IO.MemoryStream
    $entry.Open().CopyTo($buffer)
    return $buffer.ToArray()
}

<#
CATEGORY DISPATCH (1 of 2) — reading a source.

Returns @{ Width; Height; Cells = @(byte[] ...) }, one raw RGBA cell per output
grid position, in the order they should be laid out. A tileset reader yields
tiles; a frame reader would yield frames. Nothing downstream cares which.
#>
function Read-TilesetCells($archive, $manifest, [string]$source, [int]$index) {
    $tilesets = @($manifest.tilesets)
    if ($tilesets.Count -le $index) {
        throw "$source holds $($tilesets.Count) tileset(s); index $index does not exist."
    }
    $tileset = $tilesets[$index]

    # Pixelorama serialises the size as the string "(32, 32)".
    if ($tileset.tile_size -notmatch '\(\s*(\d+)\s*,\s*(\d+)\s*\)') {
        throw "Unreadable tile_size '$($tileset.tile_size)' in $source."
    }
    $width = [int]$Matches[1]
    $height = [int]$Matches[2]
    $count = [int]$tileset.tile_amount
    $expected = $width * $height * 4

    $cells = @()
    for ($id = 0; $id -lt $count; $id++) {
        $bytes = Read-Entry $archive "tilesets/$index/$id" $source
        if ($bytes.Length -ne $expected) {
            throw "Tile $id in $source is $($bytes.Length) bytes; expected $expected (raw RGBA)."
        }
        $cells += , $bytes
    }
    return @{ Width = $width; Height = $height; Cells = $cells }
}

function Measure-Cell([byte[]]$pixels, [int]$width, [int]$height) {
    $opaque = 0
    for ($i = 3; $i -lt $pixels.Length; $i += 4) { if ($pixels[$i] -gt 0) { $opaque++ } }

    # Same metric as GroundAtlasTests.EveryFillTile_TilesWithItself: mean
    # per-channel disagreement between opposite edges. A fill repeats across a
    # whole floor, so its edges have to meet.
    $seam = 0.0
    for ($i = 0; $i -lt $width; $i++) {
        $left = (($i * $width) + 0) * 4
        $right = (($i * $width) + ($width - 1)) * 4
        $top = $i * 4
        $bottom = ((($height - 1) * $width) + $i) * 4
        for ($channel = 0; $channel -lt 3; $channel++) {
            $seam += [math]::Abs($pixels[$left + $channel] - $pixels[$right + $channel])
            $seam += [math]::Abs($pixels[$top + $channel] - $pixels[$bottom + $channel])
        }
    }
    $total = $width * $height
    return @{
        Opaque = $opaque
        Total  = $total
        Seam   = [math]::Round($seam / ($width * 2 * 3), 2)
    }
}

function Write-Sheet($cells, [int]$cellWidth, [int]$cellHeight, [int]$columns, [string]$path) {
    $rows = [math]::Ceiling($cells.Count / $columns)
    $sheet = New-Object System.Drawing.Bitmap(
        ($columns * $cellWidth), ($rows * $cellHeight),
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        for ($index = 0; $index -lt $cells.Count; $index++) {
            $pixels = $cells[$index]
            $originX = ($index % $columns) * $cellWidth
            $originY = [math]::Floor($index / $columns) * $cellHeight
            for ($y = 0; $y -lt $cellHeight; $y++) {
                for ($x = 0; $x -lt $cellWidth; $x++) {
                    $offset = (($y * $cellWidth) + $x) * 4
                    $sheet.SetPixel($originX + $x, $originY + $y,
                        [System.Drawing.Color]::FromArgb(
                            $pixels[$offset + 3], $pixels[$offset],
                            $pixels[$offset + 1], $pixels[$offset + 2]))
                }
            }
        }
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
        $sheet.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        return @{ Width = $sheet.Width; Height = $sheet.Height; Rows = $rows }
    }
    finally { $sheet.Dispose() }
}

<#
Classifies every cell against the previous export. "Moved" is the one that
matters: same pixels, different id, which is Pixelorama having renumbered.
#>
function Compare-Lock($previous, [string[]]$hashes) {
    $moves = @{}
    $lines = @()
    $previousById = @{}
    $previousByHash = @{}
    if ($null -ne $previous) {
        foreach ($property in $previous.tiles.PSObject.Properties) {
            $previousById[[int]$property.Name] = $property.Value
            if (-not $previousByHash.ContainsKey($property.Value)) {
                $previousByHash[$property.Value] = [int]$property.Name
            }
        }
    }

    for ($id = 0; $id -lt $hashes.Count; $id++) {
        $hash = $hashes[$id]
        if ($previousById.ContainsKey($id) -and $previousById[$id] -eq $hash) { continue }

        if ($previousByHash.ContainsKey($hash)) {
            $from = $previousByHash[$hash]
            if ($from -ne $id) {
                $moves[$from] = $id
                $lines += "  tile $from -> MOVED to id $id"
            }
        }
        elseif ($previousById.ContainsKey($id)) { $lines += "  tile $id -> REPAINTED" }
        elseif ($null -ne $previous) { $lines += "  tile $id -> NEW" }
    }

    foreach ($id in ($previousById.Keys | Sort-Object)) {
        if ($id -ge $hashes.Count -and -not $moves.ContainsValue($id)) {
            $lines += "  tile $id -> GONE"
        }
    }
    return @{ Moves = $moves; Lines = $lines }
}

<#
CATEGORY DISPATCH (2 of 2) — what to do with a profile after exporting.

terrain owns a GroundAtlasProfile per subject: the exporter writes the geometry
it just produced, and remaps role ids through any moves. Roles themselves stay
the artist's call — the measurement says which tiles *can* be a fill, not which
one *is* the material.
#>
function Update-GroundProfile([string]$path, $geometry, $moves, [bool]$dryRun) {
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Warning "No profile at $path; sheet exported but nothing declares its roles yet."
        return @()
    }

    $text = Get-Content -Raw -LiteralPath $path
    $changes = @()

    foreach ($pair in @(
            @{ Key = "TileSize"; Value = $geometry.CellWidth },
            @{ Key = "Separation"; Value = 0 },
            @{ Key = "Columns"; Value = $geometry.Columns })) {
        $pattern = "(?m)^$($pair.Key)\s*=\s*(-?\d+)\s*$"
        $match = [regex]::Match($text, $pattern)
        if ($match.Success -and $match.Groups[1].Value -ne "$($pair.Value)") {
            $changes += "  $($pair.Key): $($match.Groups[1].Value) -> $($pair.Value)"
            $text = [regex]::Replace($text, $pattern, "$($pair.Key) = $($pair.Value)")
        }
    }

    if ($moves.Count -gt 0) {
        $fillMatch = [regex]::Match($text, '(?m)^Fill\s*=\s*PackedInt32Array\(([^)]*)\)\s*$')
        if ($fillMatch.Success) {
            $ids = @($fillMatch.Groups[1].Value.Split(',') | ForEach-Object { [int]$_.Trim() })
            $remapped = @($ids | ForEach-Object { if ($moves.ContainsKey($_)) { $moves[$_] } else { $_ } })
            if (($ids -join ',') -ne ($remapped -join ',')) {
                $changes += "  Fill: ($($ids -join ', ')) -> ($($remapped -join ', '))"
                $text = [regex]::Replace($text, '(?m)^Fill\s*=\s*PackedInt32Array\([^)]*\)\s*$',
                    "Fill = PackedInt32Array($($remapped -join ', '))")
            }
        }
        $pathMatch = [regex]::Match($text, '(?m)^Path\s*=\s*(-?\d+)\s*$')
        if ($pathMatch.Success) {
            $id = [int]$pathMatch.Groups[1].Value
            if ($moves.ContainsKey($id)) {
                $changes += "  Path: $id -> $($moves[$id])"
                $text = [regex]::Replace($text, '(?m)^Path\s*=\s*-?\d+\s*$', "Path = $($moves[$id])")
            }
        }
    }

    if ($changes.Count -gt 0 -and -not $dryRun) {
        Set-Content -LiteralPath $path -Value $text -NoNewline -Encoding utf8
    }
    return $changes
}

# ---------------------------------------------------------------- one subject

function Export-Subject([string]$category, [string]$subject) {
    $source = Join-Path $repoRoot "art/source/$category/$subject.pxo"
    $sheetPath = Join-Path $repoRoot "art/exports/$category/${subject}_sheet.png"
    $lockPath = Join-Path $repoRoot "art/exports/$category/$subject.tiles.json"
    $promotePath = Join-Path $repoRoot "game/assets/$category/${subject}_sheet.png"
    $profilePath = Join-Path $repoRoot "game/assets/terrain/biomes/$subject.tres"

    Write-Output ""
    Write-Output "=== $category/$subject ==="

    $archive = [System.IO.Compression.ZipFile]::OpenRead($source)
    try {
        $manifest = Read-PxoManifest $archive $source
        $read = Read-TilesetCells $archive $manifest $source $TilesetIndex
    }
    finally { $archive.Dispose() }

    $hashes = @($read.Cells | ForEach-Object { Get-TileHash $_ })

    $previous = $null
    if (Test-Path -LiteralPath $lockPath) {
        $previous = Get-Content -Raw -LiteralPath $lockPath | ConvertFrom-Json
    }
    $diff = Compare-Lock $previous $hashes

    if ($null -eq $previous) { Write-Output "  first export - no lockfile to compare against" }
    elseif ($diff.Lines.Count -eq 0) { Write-Output "  no tile changed" }
    else { $diff.Lines | ForEach-Object { Write-Output $_ } }

    if ($Check) {
        Write-Output "  (check only - nothing written)"
    }
    else {
        $sheet = Write-Sheet $read.Cells $read.Width $read.Height $Columns $sheetPath
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $promotePath) | Out-Null
        Copy-Item -LiteralPath $sheetPath -Destination $promotePath -Force

        $tiles = [ordered]@{}
        for ($id = 0; $id -lt $hashes.Count; $id++) { $tiles["$id"] = $hashes[$id] }
        @{
            subject  = $subject
            category = $category
            tileSize = @($read.Width, $read.Height)
            columns  = $Columns
            tiles    = $tiles
        } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $lockPath -Encoding utf8

        Write-Output "  sheet: $($sheet.Width)x$($sheet.Height) - $($read.Cells.Count) cells of $($read.Width)x$($read.Height), $Columns columns, no gutter"
        Write-Output "  promoted to game/assets/$category/${subject}_sheet.png"
    }

    if ($category -eq "terrain") {
        $geometry = @{ CellWidth = $read.Width; Columns = $Columns }
        $profileChanges = Update-GroundProfile $profilePath $geometry $diff.Moves $Check.IsPresent
        if ($profileChanges.Count -gt 0) {
            Write-Output "  profile $subject.tres:"
            $profileChanges | ForEach-Object { Write-Output $_ }
        }
    }

    # Formatted here rather than emitted as objects: Format-Table breaks when a
    # caller pipes the stream through Select-Object, and this report exists to
    # be read in a terminal.
    $lines = @("", "  {0,4}  {1,11}  {2,7}  {3}" -f "id", "opaque", "seam", "usable as")
    for ($id = 0; $id -lt $read.Cells.Count; $id++) {
        $stats = Measure-Cell $read.Cells[$id] $read.Width $read.Height
        # Same 8.0 threshold as GroundAtlasTests.SeamTolerance.
        $usable = if ($stats.Opaque -eq 0) { "empty" }
                  elseif ($stats.Opaque -lt $stats.Total) { "prop (has transparency)" }
                  elseif ($stats.Seam -le 8.0) { "FILL or path" }
                  else { "edge/corner - not a fill" }
        $lines += "  {0,4}  {1,11}  {2,7}  {3}" -f
            $id, "$($stats.Opaque)/$($stats.Total)", $stats.Seam, $usable
    }
    return $lines
}

# ---------------------------------------------------------------- entry

if (-not $All -and [string]::IsNullOrWhiteSpace($Category)) {
    throw "Pass -Category <name>, optionally with -Subject <name>, or -All."
}

$categories = if ($All) { $SupportedCategories } else { @($Category) }

foreach ($name in $categories) {
    if ($SupportedCategories -notcontains $name) {
        if ($AllCategories -contains $name) {
            throw ("Category '$name' is a real category but has no reader yet: its sources are " +
                "canvas frames, not a uniform tileset, so composing them needs a second reader. " +
                "See the CATEGORY DISPATCH points in this script.")
        }
        throw ("'$name' is not a category. The pipeline defines: $($AllCategories -join ', '). " +
            "See docs/presentation/art-pipeline.md.")
    }

    $sourceDirectory = Join-Path $repoRoot "art/source/$name"
    if (-not (Test-Path -LiteralPath $sourceDirectory)) {
        Write-Warning "No sources under art/source/$name."
        continue
    }

    $subjects = if ([string]::IsNullOrWhiteSpace($Subject)) {
        @(Get-ChildItem -LiteralPath $sourceDirectory -Filter "*.pxo" |
            ForEach-Object { $_.BaseName } | Sort-Object)
    }
    else { @($Subject) }

    if ($subjects.Count -eq 0) { Write-Warning "No .pxo under art/source/$name."; continue }

    foreach ($each in $subjects) {
        Export-Subject $name $each | ForEach-Object { Write-Output $_ }
    }
}

Write-Output "Only cells marked FILL are safe in a profile's Fill or Path: a fill repeats across"
Write-Output "the whole floor, so its opposite edges have to match. GroundAtlasTests enforces the"
Write-Output "same threshold. Which of the candidates is the material and which the patch is an"
Write-Output "art decision, and stays yours."

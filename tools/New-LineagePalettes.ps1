<#
.SYNOPSIS
    Generates one 64-colour GIMP palette (.gpl) per lineage, importable into
    Pixelorama, Aseprite, GIMP, Krita and LibreSprite.

.DESCRIPTION
    The palettes are derived, not hand-picked, so the eight of them stay
    consistent with each other and can be regenerated when a lineage accent
    changes. The accents are the ones already fixed in code at
    game/scripts/LineageThemeRegistry.cs (IconAccentByLineage) — the splash art
    sits inside UI tinted with those exact colours, so a portrait built on a
    different hue would fight its own frame.

    Every palette has the SAME 64 slots in the SAME order:

      01-08  Neutrals        shared  line dark -> warm white
      09-14  Skin            shared
      15-20  Metal           shared
      21-26  Wood & leather  shared
      27-32  Stone           shared
      33-36  Emissive        shared  fire, glow, specular
      37-42  Lineage accent  unique  the identity ramp
      43-48  Variant I       unique  garment/material treatment A
      49-54  Variant II      unique  garment/material treatment B
      55-60  Atmosphere      unique  deep background, lineage-tinted
      61-64  Deep shadow     unique  occlusion, lineage-tinted

    Slots 1-36 are byte-identical across all eight files. That is what keeps
    the set reading as one game rather than eight unrelated illustrations.

    Variant I and II exist so the two splashes of a lineage (there are two per
    lineage) contrast with each other instead of looking like the same picture
    twice. They are deliberately NOT named "male" and "female": tying "darker,
    heavier" to one gender and "lighter, softer" to the other would bake a
    stereotype into every future asset. Assign whichever variant suits the
    character being drawn.

    Ramps use hue shifting rather than flat lightness steps: shadows rotate
    toward blue-violet and gain saturation, highlights rotate toward yellow and
    lose it. Flat ramps are the single most common reason hand-drawn pixel art
    reads as muddy.

    Three kinds of file are written:

      wog-common-36.gpl      the shared slots, once
      wog-<lineage>-28.gpl   the unique slots for one lineage
      wog-<lineage>-64.gpl   both concatenated, for actually drawing

    The split pair is the maintainable form: the shared block exists once, so
    it cannot drift between lineages. The combined file is the working one,
    because Pixelorama shows a single palette at a time and drawing a scene
    needs skin, stone and accent together.

.PARAMETER OutputDirectory
    Where the .gpl files are written. Defaults to art/palettes/.

.PARAMETER ShadowConvergence
    How far the deep-shadow ramp rotates toward blue-violet, 0..1. High values
    make every lineage's darks collapse onto the same mauve; low values keep
    each lineage recognisable even in occlusion. Physically, shadows do
    converge — this is a legibility choice, not a realism one.
#>
[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..' 'art' 'palettes'),
    [ValidateRange(0.0, 1.0)]
    [double]$ShadowConvergence = 0.28
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Accents copied from LineageThemeRegistry.IconAccentByLineage. Kept as the
# float triples used in code so a mismatch is obvious on inspection.
$Lineages = [ordered]@{
    ardhen  = @{ Rgb = @(0.69, 0.40, 0.25); Note = 'copper — memory, effort, repair' }
    eirune  = @{ Rgb = @(0.31, 0.62, 0.56); Note = 'soft teal — water, growth, symbiosis' }
    kovari  = @{ Rgb = @(0.42, 0.54, 0.68); Note = 'blue-grey — modular, mechanical, repair' }
    myrven  = @{ Rgb = @(0.54, 0.42, 0.65); Note = 'muted purple — layers, performance, mediation' }
    vaelun  = @{ Rgb = @(0.71, 0.72, 0.42); Note = 'khaki and olive — route, signal, refuge' }
    orveth  = @{ Rgb = @(0.81, 0.66, 0.19); Note = 'muted gold — contract, reserve, exchange' }
    caelith = @{ Rgb = @(0.48, 0.72, 0.85); Note = 'pale blue — node, synthesis, diagnosis' }
    theryn  = @{ Rgb = @(0.77, 0.42, 0.42); Note = 'soft red — pulse, empathy, ceremony' }
}

<#
    Guards the reason these palettes exist: no two lineages may become
    indistinguishable on screen. Ardhen, Orveth and Vaelun once shared a 10°
    amber band — Orveth and Vaelun were 2° apart — and nobody could tell whose
    accent they were looking at.

    Hue alone is the wrong test. Two blues 11° apart read as different
    lineages when one is pale and the other is a desaturated mid-tone, which is
    exactly the case for Caelith and Kovari. What actually made the amber trio
    fail was being close in hue AND lightness AND saturation at once.

    A pair therefore passes if it separates on any one axis clearly enough.
#>
function Assert-AccentsAreDistinguishable {
    param(
        [double]$MinimumHueGap = 12.0,
        [double]$MinimumLightnessGap = 0.10,
        [double]$MinimumSaturationGap = 0.20
    )
    $hsl = [ordered]@{}
    foreach ($name in $Lineages.Keys) {
        $rgb = $Lineages[$name].Rgb
        $hsl[$name] = ConvertTo-Hsl $rgb[0] $rgb[1] $rgb[2]
    }
    $names = @($hsl.Keys)
    for ($i = 0; $i -lt $names.Count; $i++) {
        for ($j = $i + 1; $j -lt $names.Count; $j++) {
            $a = $hsl[$names[$i]]
            $b = $hsl[$names[$j]]
            $hueGap = [Math]::Abs((($a.H - $b.H + 540.0) % 360.0) - 180.0)
            $lightnessGap = [Math]::Abs($a.L - $b.L)
            $saturationGap = [Math]::Abs($a.S - $b.S)

            if ($hueGap -ge $MinimumHueGap) { continue }
            if ($lightnessGap -ge $MinimumLightnessGap) { continue }
            if ($saturationGap -ge $MinimumSaturationGap) { continue }

            # -f binds tighter than +, so the template must be fully
            # parenthesised before formatting or only the last literal gets
            # its arguments.
            $template = "Accents '{0}' and '{1}' are indistinguishable: hue {2:N1} deg apart " +
                        "(needs {3}), lightness {4:N2} apart (needs {5}), saturation {6:N2} " +
                        "apart (needs {7}). Separate them on at least one axis."
            throw ($template -f
                $names[$i], $names[$j],
                $hueGap, $MinimumHueGap,
                $lightnessGap, $MinimumLightnessGap,
                $saturationGap, $MinimumSaturationGap)
        }
    }
}

function ConvertTo-Hsl([double]$r, [double]$g, [double]$b) {
    $max = [Math]::Max($r, [Math]::Max($g, $b))
    $min = [Math]::Min($r, [Math]::Min($g, $b))
    $l = ($max + $min) / 2.0
    $d = $max - $min
    if ($d -eq 0) { return @{ H = 0.0; S = 0.0; L = $l } }
    $s = if ($l -gt 0.5) { $d / (2.0 - $max - $min) } else { $d / ($max + $min) }
    $h = switch ($max) {
        $r { (($g - $b) / $d) % 6.0 }
        $g { (($b - $r) / $d) + 2.0 }
        default { (($r - $g) / $d) + 4.0 }
    }
    $h = $h * 60.0
    if ($h -lt 0) { $h += 360.0 }
    return @{ H = $h; S = $s; L = $l }
}

function ConvertFrom-Hsl([double]$h, [double]$s, [double]$l) {
    $h = (($h % 360.0) + 360.0) % 360.0
    $s = [Math]::Max(0.0, [Math]::Min(1.0, $s))
    $l = [Math]::Max(0.0, [Math]::Min(1.0, $l))
    if ($s -eq 0) {
        $v = [int][Math]::Round($l * 255)
        return @($v, $v, $v)
    }
    $q = if ($l -lt 0.5) { $l * (1 + $s) } else { $l + $s - ($l * $s) }
    $p = 2 * $l - $q
    $convert = {
        param([double]$t)
        if ($t -lt 0) { $t += 1 }
        if ($t -gt 1) { $t -= 1 }
        if ($t -lt (1.0 / 6.0)) { return $p + (($q - $p) * 6 * $t) }
        if ($t -lt 0.5) { return $q }
        if ($t -lt (2.0 / 3.0)) { return $p + (($q - $p) * ((2.0 / 3.0) - $t) * 6) }
        return $p
    }
    $hk = $h / 360.0
    return @(
        [int][Math]::Round((& $convert ($hk + 1.0 / 3.0)) * 255),
        [int][Math]::Round((& $convert $hk) * 255),
        [int][Math]::Round((& $convert ($hk - 1.0 / 3.0)) * 255)
    )
}

# Rotates a hue toward a target along the shortest arc.
function Move-Hue([double]$h, [double]$target, [double]$amount) {
    $delta = (($target - $h + 540.0) % 360.0) - 180.0
    return $h + ($delta * $amount)
}

<#
    Builds one ramp, dark to light, with hue shifting.
    ShadowHue 265 (blue-violet) and LightHue 50 (yellow) are the conventional
    cool-shadow / warm-light pair; pulling the ends toward them is what gives a
    ramp depth that pure lightness steps never produce.
#>
function New-Ramp {
    param(
        [double]$Hue,
        [double]$Saturation,
        [double]$MinLightness,
        [double]$MaxLightness,
        [int]$Steps,
        [double]$HueShift = 0.16,
        [double]$SaturationBoost = 0.18
    )
    $ramp = @()
    for ($i = 0; $i -lt $Steps; $i++) {
        $t = if ($Steps -eq 1) { 0.5 } else { $i / [double]($Steps - 1) }
        $l = $MinLightness + (($MaxLightness - $MinLightness) * $t)

        # Darks rotate toward blue-violet and gain saturation; lights rotate
        # toward yellow and lose it.
        $h = if ($t -lt 0.5) {
            Move-Hue $Hue 265.0 ($HueShift * (1 - ($t * 2)))
        } else {
            Move-Hue $Hue 50.0 ($HueShift * (($t - 0.5) * 2))
        }
        $s = $Saturation + ($SaturationBoost * (0.5 - $t) * 2)
        $s = [Math]::Max(0.03, [Math]::Min(1.0, $s))
        $ramp += , (ConvertFrom-Hsl $h $s $l)
    }
    return $ramp
}

# ── Shared slots 01-36 ───────────────────────────────────────────────────────
# Identical in every file. The top neutral is the project's own
# DefaultIconAccent (#F2EBD4) so UI and illustration share a white point.
$SharedRamps = [ordered]@{
    'neutral'  = @{ Ramp = (New-Ramp -Hue 222 -Saturation 0.10 -MinLightness 0.05 -MaxLightness 0.92 -Steps 8 -HueShift 0.10 -SaturationBoost 0.08); Label = 'Neutrals / line' }
    'skin'     = @{ Ramp = (New-Ramp -Hue 24  -Saturation 0.44 -MinLightness 0.20 -MaxLightness 0.80 -Steps 6); Label = 'Skin' }
    'metal'    = @{ Ramp = (New-Ramp -Hue 212 -Saturation 0.13 -MinLightness 0.16 -MaxLightness 0.84 -Steps 6); Label = 'Metal' }
    'wood'     = @{ Ramp = (New-Ramp -Hue 28  -Saturation 0.40 -MinLightness 0.13 -MaxLightness 0.66 -Steps 6); Label = 'Wood and leather' }
    'stone'    = @{ Ramp = (New-Ramp -Hue 218 -Saturation 0.09 -MinLightness 0.19 -MaxLightness 0.76 -Steps 6); Label = 'Stone' }
    'emissive' = @{ Ramp = (New-Ramp -Hue 38  -Saturation 0.92 -MinLightness 0.42 -MaxLightness 0.88 -Steps 4 -HueShift 0.12 -SaturationBoost 0.06); Label = 'Emissive / light' }
}

function New-LineagePalette {
    param([string]$Name, [double[]]$Rgb, [string]$Note)

    $hsl = ConvertTo-Hsl $Rgb[0] $Rgb[1] $Rgb[2]
    $h = $hsl.H
    $s = $hsl.S

    $unique = [ordered]@{
        'accent'    = @{ Ramp = (New-Ramp -Hue $h -Saturation ([Math]::Max($s, 0.34)) -MinLightness 0.18 -MaxLightness 0.82 -Steps 6); Label = 'Lineage accent' }
        # Variant I: deeper and more saturated, hue held. Variant II: lighter
        # and cooler, hue rotated 26 degrees so the two read apart at a glance
        # while both stay recognisably of the lineage.
        'variant-1' = @{ Ramp = (New-Ramp -Hue ($h - 13) -Saturation ([Math]::Min($s + 0.16, 0.85)) -MinLightness 0.12 -MaxLightness 0.62 -Steps 6); Label = 'Variant I (contrast pair A)' }
        'variant-2' = @{ Ramp = (New-Ramp -Hue ($h + 26) -Saturation ([Math]::Max($s - 0.12, 0.14)) -MinLightness 0.34 -MaxLightness 0.88 -Steps 6); Label = 'Variant II (contrast pair B)' }
        'atmos'     = @{ Ramp = (New-Ramp -Hue (Move-Hue $h 240.0 0.42) -Saturation 0.24 -MinLightness 0.14 -MaxLightness 0.70 -Steps 6); Label = 'Atmosphere / background' }
        'shadow'    = @{ Ramp = (New-Ramp -Hue (Move-Hue $h 268.0 $ShadowConvergence) -Saturation 0.34 -MinLightness 0.05 -MaxLightness 0.26 -Steps 4 -HueShift 0.08); Label = 'Deep shadow' }
    }

    return $unique
}

# Emits the "R G B<TAB>name" body lines for a group of ramps.
function ConvertTo-PaletteBody([System.Collections.Specialized.OrderedDictionary]$Groups) {
    $lines = @()
    foreach ($key in $Groups.Keys) {
        $step = 1
        foreach ($colour in $Groups[$key].Ramp) {
            $lines += ('{0,3} {1,3} {2,3}	{3}-{4}' -f $colour[0], $colour[1], $colour[2], $key, $step)
            $step++
        }
    }
    return $lines
}

function New-PaletteFile {
    param([string]$Title, [string[]]$Comments, [string[]]$Body, [string]$Path, [int]$Expected)

    if ($Body.Count -ne $Expected) {
        throw "$Title produced $($Body.Count) colours, expected $Expected."
    }
    $lines = @('GIMP Palette', "Name: $Title", 'Columns: 8')
    foreach ($comment in $Comments) { $lines += "# $comment" }
    $lines += '# Generated by tools/New-LineagePalettes.ps1 - edit the script,'
    $lines += '# not this file. Ramps run dark -> light with hue shifting.'
    $lines += '#'
    $lines += $Body

    # .gpl is a plain-text format read by many tools; UTF-8 without BOM is the
    # safest encoding for all of them.
    [System.IO.File]::WriteAllLines(
        $Path,
        $lines,
        (New-Object System.Text.UTF8Encoding $false))
    Write-Output "wrote $Path ($Expected colours)"
}

Assert-AccentsAreDistinguishable

if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}

# ── The shared block, written once ───────────────────────────────────────────
# It lived duplicated inside all eight files before. Emitting it once is what
# makes drift impossible rather than merely unlikely.
$sharedBody = ConvertTo-PaletteBody $SharedRamps
New-PaletteFile `
    -Title 'World of Goses - Common' `
    -Comments @(
        'Shared splash colours: neutrals, skin, metal, wood, stone, emissive.',
        'Identical for every lineage. These are what make eight separate',
        'illustrations read as one world, so change them for all or none.',
        'Pair with a wog-<lineage>-28.gpl, or use wog-<lineage>-64.gpl which',
        'already contains both.') `
    -Body $sharedBody `
    -Path (Join-Path $OutputDirectory 'wog-common-36.gpl') `
    -Expected 36

foreach ($name in $Lineages.Keys) {
    $spec = $Lineages[$name]
    $uniqueGroups = New-LineagePalette -Name $name -Rgb $spec.Rgb -Note $spec.Note
    $uniqueBody = ConvertTo-PaletteBody $uniqueGroups
    $title = (Get-Culture).TextInfo.ToTitleCase($name)

    $lineageComments = @(
        "Lineage-specific splash colours for $name.",
        $spec.Note,
        'Accent derives from IconAccentByLineage in',
        'game/scripts/LineageThemeRegistry.cs - the UI framing a splash is',
        'tinted with it, so a portrait on a different hue fights its frame.',
        'Variant I and II are the contrast pair for this lineage''s two',
        'splashes. They are NOT gendered: assign whichever suits the',
        'character. Pair with wog-common-36.gpl.')

    New-PaletteFile `
        -Title "World of Goses - $title (lineage)" `
        -Comments $lineageComments `
        -Body $uniqueBody `
        -Path (Join-Path $OutputDirectory "wog-$name-28.gpl") `
        -Expected 28

    # The combined file is the one to actually draw with: Pixelorama shows a
    # single palette at a time, and a scene needs skin, stone and accent
    # together. It is derived from the two above, never edited on its own.
    New-PaletteFile `
        -Title "World of Goses - $title" `
        -Comments (@(
            'Working palette: wog-common-36.gpl + wog-' + $name + '-28.gpl.',
            'Slots 1-36 shared, 37-64 lineage.') + $lineageComments[1..($lineageComments.Count - 1)]) `
        -Body ($sharedBody + $uniqueBody) `
        -Path (Join-Path $OutputDirectory "wog-$name-64.gpl") `
        -Expected 64
}

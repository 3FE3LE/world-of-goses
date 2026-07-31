# Splash palettes

Palettes for hand-drawing the splash illustrations. Format is GIMP Palette
(`.gpl`), read natively by Pixelorama, Aseprite, GIMP, Krita and LibreSprite.

**Generated, not hand-picked.** Edit `tools/New-LineagePalettes.ps1` and
re-run it; do not edit the `.gpl` files, they are overwritten.

```powershell
pwsh ./tools/New-LineagePalettes.ps1

# Keep more lineage character in the darks, or less:
pwsh ./tools/New-LineagePalettes.ps1 -ShadowConvergence 0.15
```

## The three kinds of file

| File | Colours | Purpose |
| --- | ---: | --- |
| `wog-common-36.gpl` | 36 | The shared block, stored once |
| `wog-<lineage>-28.gpl` | 28 | One lineage's own colours |
| `wog-<lineage>-64.gpl` | 64 | Both concatenated — draw with this |

The split pair is the maintainable form: the shared block exists in exactly one
place, so it cannot drift between lineages. The combined file is the working
one, because Pixelorama shows a single palette at a time and drawing a scene
needs skin, stone and accent together. It is derived, never edited on its own —
`wog-<lineage>-64.gpl` is byte-identical to `wog-common-36.gpl` followed by
`wog-<lineage>-28.gpl`.

## Importing into Pixelorama

`Window → Palettes → ⋯ → Import Palette`, then pick the `-64` file for the
lineage you are drawing. Import only the one you need — having all eight loaded
at once defeats the point of a constrained palette.

## Layout

Every palette has the same 64 slots in the same order, 8 per row:

| Slots | Group | Scope |
| ---: | --- | --- |
| 01-08 | Neutrals / line | shared |
| 09-14 | Skin | shared |
| 15-20 | Metal | shared |
| 21-26 | Wood and leather | shared |
| 27-32 | Stone | shared |
| 33-36 | Emissive (fire, glow, specular) | shared |
| 37-42 | Lineage accent | unique |
| 43-48 | Variant I | unique |
| 49-54 | Variant II | unique |
| 55-60 | Atmosphere / background | unique |
| 61-64 | Deep shadow | unique |

Slots 1-36 live in `wog-common-36.gpl`; slots 37-64 in each
`wog-<lineage>-28.gpl`. Shared skin, stone, metal and light are what make eight
separate illustrations read as one world, so change them for all lineages or
none.

## Where the colours come from

The accent ramps derive from `IconAccentByLineage` in
`game/scripts/LineageThemeRegistry.cs`. The UI framing a splash is tinted with
those exact colours, so a portrait built on a different hue fights its own
frame. If an accent changes in code, change it in the generator and re-run.

The top neutral is the project's `DefaultIconAccent` (`#F2EBD4`), so UI and
illustration share a white point.

## Variant I and Variant II

Each lineage has two splashes, and drawing both from one ramp makes them look
like the same picture twice. Variant I is deeper and more saturated; Variant II
is lighter and rotated 26°.

They are **not** "male" and "female". Tying "darker, heavier" to one gender and
"lighter, softer" to the other would bake a stereotype into every asset drawn
from these files. Assign whichever variant suits the character.

## Why the ramps are not flat

Each ramp shifts hue as it moves: shadows rotate toward blue-violet and gain
saturation, highlights rotate toward yellow and lose it. A ramp that only
changes lightness is the most common reason hand-drawn pixel art reads as
muddy.

The deep-shadow ramp rotates toward blue-violet by `-ShadowConvergence`
(default `0.28`). Physically, shadows do converge on one hue; visually, a high
value made ardhen, orveth, vaelun and theryn collapse onto the same mauve and
lose their identity in occlusion. The default keeps each lineage readable in
its darks.

## The accent-collision guard

The generator refuses to emit a set in which two lineages would be
indistinguishable, because that is a failure no amount of good ramp maths can
repair.

Hue alone is the wrong test. Caelith and Kovari sit 11° apart and still read as
different lineages, because one is pale and the other a desaturated mid-tone.
What made Ardhen, Orveth and Vaelun fail was being close in hue *and* lightness
*and* saturation at once — Orveth and Vaelun were 2° apart. A pair therefore
passes if it separates clearly on any one axis: 12° of hue, 0.10 of lightness,
or 0.20 of saturation.

Those three accents were re-spread in 2026-07 to copper (~20°), gold (~45°) and
khaki (~62°), each matching its own written description more closely than the
amber band they shared before. `LineageThemeRegistry.IconAccentByLineage` and
this generator carry the same values; changing one without the other is what
the guard exists to make loud.

## Authoring spec

- **Canvas:** 3:4 portrait. `540 × 720` fills the 720 px logical height of the
  base canvas exactly; `480 × 640` is also fine but must be displayed 1:1
  (640 of 720) rather than stretched — a 1.125× upscale destroys the pixel grid.
- **Colour:** 8 bits per channel, sRGB.
- **Export:** PNG-8 indexed. 64 colours fits with room to spare; expect
  roughly 60-120 KB per image.
- **Alpha:** 1-bit if the character is cut out; none at all if the splash is a
  full rectangular scene. Never soft or antialiased edges.

## Pipeline

Sources go in `art/source/characters/splash/`, exports in
`art/exports/characters/splash/`, and the imported copy Godot uses in
`game/assets/characters/splash/`. See `docs/ART_PIPELINE.md`.

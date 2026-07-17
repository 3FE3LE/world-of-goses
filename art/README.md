# art/

Pixel art pipeline for World of Goses. Follows the layout defined in
[`docs/ART_PIPELINE.md`](../docs/ART_PIPELINE.md).

```text
art/
├── source/        # Pixelorama sources (.pxo / .pxm). Editable.
│   ├── characters/
│   ├── buildings/
│   ├── terrain/
│   ├── effects/
│   └── ui/
├── references/    # Mood boards, inspiration, colour scripts. Not game art.
└── exports/       # PNGs exported from sources. Do not edit by hand.
    ├── characters/
    └── buildings/
```

## Placeholder pipeline

While the project has no real art yet, `art/export_placeholder_art.js`
generates minimal placeholder PNGs into `art/exports/`. The script:

- Emits an indexed-colour PNG (no external dependencies, Node.js only).
- Uses the same canvas sizes as the final art will use
  (64 × 96 worker, 192 × 192 mine), so the placeholders can be
  swapped without re-anchoring scenes.
- Lives at the repository root (not under `game/`); running it does
  not interact with the Godot project.

Re-run with:

```bash
node art/export_placeholder_art.js
```

Once real Pixelorama sources exist under `art/source/`, the
placeholder PNGs will be removed and the script will become obsolete.
from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "dist" / "appearance_variants"
LINEAGES = ("ardhen", "eirune", "kovari", "myrven", "vaelun", "orveth", "caelith", "theryn")
BODIES = ("male", "female")
ANIMATIONS = ("idle", "combat_idle", "walk", "run", "jump", "climb", "sit", "hurt", "slash", "thrust", "halfslash", "backslash", "shoot", "spellcast")
DIRECTIONS = ("down", "left", "up", "right")


def main() -> None:
    summary = json.loads((OUT / "appearance_manifest.json").read_text(encoding="utf-8"))
    expected = len(LINEAGES) * len(BODIES) * 13
    assert summary["count"] == expected, (summary["count"], expected)
    seen_hashes: dict[str, set[str]] = {}
    for lineage in LINEAGES:
        for body in BODIES:
            for entry in [e for e in summary["variants"] if e["lineage"] == lineage and e["body"] == body]:
                root = OUT / lineage / body / entry["id"]
                metadata_path = root / "assets" / "characters" / "lineages" / lineage / body / "metadata.json"
                metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
                assert metadata["appearance_variant"] == entry["id"]
                assert metadata["frame_size"] == [128, 128]
                assert metadata["baseline"] == [64, 126]
                assert metadata["lineage_palette"] == lineage
                textures = sorted((root / "assets" / "characters" / "lineages" / lineage / body / "textures").glob("*.png"))
                assert len(textures) == 56, (entry, len(textures))
                seen_hashes.setdefault(entry["id"], set()).add(hashlib.sha256(textures[0].read_bytes()).hexdigest())
                for path in textures:
                    with Image.open(path) as image:
                        assert image.mode == "RGBA"
                        assert image.height == 128
                        assert image.width % 128 == 0
                        assert set(image.getchannel("A").getdata()) <= {0, 255}
    print(f"Appearance variant matrix smoke test passed: {summary['count']} bundles across {len(LINEAGES)} lineages")


if __name__ == "__main__":
    main()

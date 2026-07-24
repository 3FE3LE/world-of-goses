from __future__ import annotations

import json
import re
import shutil
from pathlib import Path

PACKAGE_ROOT = Path(__file__).resolve().parents[1]
STAGING = PACKAGE_ROOT / "dist" / "appearance_variants"
CATALOG = PACKAGE_ROOT / "source" / "recipes" / "appearance_variants.json"
GAME = PACKAGE_ROOT.parent.parent / "game" / "assets" / "characters" / "lineages"

VARIANT_ID = re.compile(r"^[a-z][a-z0-9_]*$")


def rewrite_paths(target_root: Path, staging_root: Path) -> None:
    expected_prefix = "res://assets/characters/lineages/" + staging_root.relative_to(STAGING).as_posix() + "/"
    target_prefix = "res://assets/characters/lineages/" + target_root.relative_to(GAME.parent).as_posix() + "/"
    for path in [*(target_root.rglob("*.tres")), *(target_root.rglob("*.tscn"))]:
        text = path.read_text(encoding="utf-8")
        updated = text.replace(expected_prefix, target_prefix)
        if updated != text:
            path.write_text(updated, encoding="utf-8")


def main() -> None:
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    ready = [variant for variant in catalog["variants"] if variant["status"] == "ready" and variant["id"] != "standard"]
    if not ready:
        raise SystemExit("Catalog has no ready non-standard variants to promote.")
    for variant in ready:
        variant_id = variant["id"]
        if not VARIANT_ID.match(variant_id):
            raise SystemExit(f"Invalid variant id {variant_id!r}")
    summary: list[dict] = []
    for lineage in catalog["lineages"]:
        for body in catalog["bodies"]:
            for variant in ready:
                staging_path = STAGING / lineage / body / variant["id"]
                if not staging_path.exists():
                    print(f"Skipping missing {staging_path}")
                    continue
                staging_inner = staging_path / "assets" / "characters" / "lineages" / lineage / body
                target_path = GAME / lineage / "variants" / variant["id"] / body
                if target_path.exists():
                    shutil.rmtree(target_path)
                if not staging_inner.exists():
                    print(f"Skipping missing staging bundle {staging_inner}")
                    continue
                shutil.copytree(staging_inner, target_path)
                rewrite_paths(target_path, staging_inner)
                summary.append({"lineage": lineage, "body": body, "id": variant["id"], "output": str(target_path.relative_to(PACKAGE_ROOT.parent.parent))})
    (GAME / "appearance_manifest.json").write_text(
        json.dumps({"schema_version": 1, "ready_variant_count": len(ready), "promoted": summary}, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(f"Promoted {len(summary)} ready variant bundles into {GAME}")


if __name__ == "__main__":
    main()

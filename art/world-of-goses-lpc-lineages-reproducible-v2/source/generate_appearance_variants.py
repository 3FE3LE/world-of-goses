from __future__ import annotations

import copy
import hashlib
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

PACKAGE_ROOT = Path(__file__).resolve().parents[1]
SOURCE = PACKAGE_ROOT / "source"
CANONICAL = SOURCE / "recipes" / "lineages.json"
CATALOG = SOURCE / "recipes" / "appearance_variants.json"
GENERATOR = SOURCE / "generate_lineage_sprites.py"
OUTPUT_ROOT = PACKAGE_ROOT / "dist" / "appearance_variants"


def canonical_hash(value: object) -> str:
    payload = json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")
    return hashlib.sha256(payload).hexdigest()


def apply_variant(recipe: dict, lineage: str, overrides: dict) -> dict:
    recipe = copy.deepcopy(recipe)
    item = next(item for item in recipe["lineages"] if item["key"] == lineage)
    item["colors"].update(overrides.get("colors", {}))
    item["profiles"].update(overrides.get("profiles", {}))
    return recipe


def run_variant(lineage: str, body: str, variant: dict) -> dict:
    recipe = json.loads(CANONICAL.read_text(encoding="utf-8"))
    rendered_recipe = apply_variant(recipe, lineage, variant.get("overrides", {}))
    recipe_hash = canonical_hash({"lineage": lineage, "body": body, "variant": variant, "recipe": rendered_recipe})
    with tempfile.TemporaryDirectory(prefix="wog-appearance-") as temp_dir:
        recipe_path = Path(temp_dir) / "lineages.json"
        recipe_path.write_text(json.dumps(rendered_recipe, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        output = OUTPUT_ROOT / lineage / body / variant["id"]
        if output.exists():
            shutil.rmtree(output)
        command = [
            sys.executable,
            str(GENERATOR),
            "--recipes", str(recipe_path),
            "--output", str(output),
            "--lineage", lineage,
            "--gender", body,
            "--no-zip",
        ]
        subprocess.run(command, cwd=PACKAGE_ROOT, check=True)
    metadata = output / "assets" / "characters" / "lineages" / lineage / body / "metadata.json"
    metadata_value = json.loads(metadata.read_text(encoding="utf-8"))
    metadata_value.update({
        "appearance_variant": variant["id"],
        "appearance_display_name": variant["display_name"],
        "appearance_status": variant["status"],
        "appearance_recipe_hash": recipe_hash,
        "lineage_palette": lineage,
        "lineage_symbol": metadata_value["recipe_snapshot"]["profiles"]["symbol"],
    })
    metadata.write_text(json.dumps(metadata_value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    manifest = output / "docs" / "MANIFEST.json"
    manifest_value = json.loads(manifest.read_text(encoding="utf-8"))
    manifest_value["appearance_variant"] = variant["id"]
    manifest_value["appearance_recipe_hash"] = recipe_hash
    manifest.write_text(json.dumps(manifest_value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return {"lineage": lineage, "body": body, "id": variant["id"], "status": variant["status"], "output": str(output.relative_to(PACKAGE_ROOT)), "recipe_hash": recipe_hash}


def main() -> None:
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    if catalog.get("schema_version") != 1:
        raise SystemExit("Unsupported appearance catalog schema")
    lineages = catalog["lineages"]
    bodies = catalog["bodies"]
    variants = catalog["variants"]
    summary: list[dict] = []
    for lineage in lineages:
        for body in bodies:
            for variant in variants:
                allowed = variant.get("limited_to")
                if allowed is not None and lineage not in allowed:
                    continue
                summary.append(run_variant(lineage, body, variant))
    OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)
    (OUTPUT_ROOT / "appearance_manifest.json").write_text(
        json.dumps({"schema_version": 1, "count": len(summary), "variants": summary}, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(f"Generated {len(summary)} precomposed appearance bundles under {OUTPUT_ROOT}")


if __name__ == "__main__":
    main()

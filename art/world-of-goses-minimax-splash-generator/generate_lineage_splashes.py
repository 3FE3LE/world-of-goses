#!/usr/bin/env python3
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
import sys
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from io import BytesIO
from pathlib import Path
from typing import Any

import requests
from PIL import Image, ImageEnhance, ImageOps

API_URL = "https://api.minimax.io/v1/image_generation"
MAX_PROMPT_LENGTH = 1450
EXPECTED_KEYS = {
    "ardhen_male", "ardhen_female",
    "eirune_male", "eirune_female",
    "kovari_male", "kovari_female",
    "myrven_male", "myrven_female",
    "vaelun_male", "vaelun_female",
    "orveth_male", "orveth_female",
    "caelith_male", "caelith_female",
    "theryn_male", "theryn_female",
}

@dataclass(frozen=True)
class CharacterJob:
    key: str
    lineage: str
    gender: str
    seed: int
    prompt: str

class GeneratorError(RuntimeError):
    pass

def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate standardized lineage characters from a shared style reference.")
    parser.add_argument("--project-root", type=Path, help="Folder containing project.godot.")
    parser.add_argument("--prompts", type=Path, default=Path(__file__).with_name("prompts.json"))
    parser.add_argument("--style-reference", type=Path, default=None, help="Optional override for the shared style reference image.")
    parser.add_argument("--only", help="Generate one character, e.g. ardhen_male.")
    parser.add_argument("--all", action="store_true", help="Generate all 16 characters.")
    parser.add_argument("--dry-run", action="store_true", help="Validate without calling MiniMax.")
    parser.add_argument("--force", action="store_true", help="Overwrite existing outputs.")
    parser.add_argument("--legacy-reference", action="store_true", help="Use legacy subject_reference=data-url payload.")
    parser.add_argument("--keep-prepared-reference", action="store_true")
    return parser.parse_args()

def find_project_root(explicit: Path | None) -> Path:
    if explicit:
        root = explicit.resolve()
        if not (root / "project.godot").is_file():
            raise GeneratorError(f"No project.godot found in {root}")
        return root
    starts = [Path.cwd().resolve(), Path(__file__).resolve().parent]
    checked: set[Path] = set()
    for start in starts:
        for candidate in (start, *start.parents):
            if candidate in checked:
                continue
            checked.add(candidate)
            if (candidate / "project.godot").is_file():
                return candidate
    for start in starts:
        for ancestor in [start, *list(start.parents)[:4]]:
            for child in ("game", "src", "app"):
                candidate = ancestor / child
                if (candidate / "project.godot").is_file():
                    return candidate.resolve()
    raise GeneratorError("Could not locate project.godot. Run inside the repository or pass --project-root.")

def load_config(path: Path) -> dict[str, Any]:
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as exc:
        raise GeneratorError(f"Prompts file not found: {path}") from exc
    except json.JSONDecodeError as exc:
        raise GeneratorError(f"Invalid JSON in {path}: {exc}") from exc

    keys = set(data.get("characters", {}))
    missing = EXPECTED_KEYS - keys
    extra = keys - EXPECTED_KEYS
    if missing or extra:
        raise GeneratorError(f"prompts.json mismatch. Missing={sorted(missing)}, extra={sorted(extra)}")

    settings = data.get("settings", {})
    width = int(settings.get("width", 0))
    height = int(settings.get("height", 0))
    if width < 512 or height < 512 or width % 8 or height % 8:
        raise GeneratorError("MiniMax width/height must be at least 512 and divisible by 8.")
    if "style_reference" not in settings:
        raise GeneratorError("settings.style_reference is missing.")
    return data

def assemble_prompt(
    config: dict[str, Any],
    entry: dict[str, Any],
    use_male_reference: bool,
    using_base_fallback: bool,
) -> str:
    base = config["base_prompt"]

    if use_male_reference:
        base += (
            " For this female, use only the generated male of the same lineage "
            "as visual reference. Keep lineage palette, materials and motifs, "
            "but follow the female skin, eyes, hair, face and tattoos above exactly."
        )
    elif using_base_fallback:
        base += (
            " The lineage male is unavailable; use only the shared base style reference."
        )

    prompt = (
        f"{base}\n\n"
        f"CHARACTER DIRECTION: {entry['prompt']}\n\n"
        f"AVOID: {config['negative_prompt']}"
    )

    if len(prompt) >= MAX_PROMPT_LENGTH:
        raise GeneratorError(
            f"Prompt has {len(prompt)} characters; safe maximum is "
            f"{MAX_PROMPT_LENGTH - 1}."
        )

    return prompt

def lineage_order(keys: list[str]) -> list[str]:
    lineages = ["ardhen", "eirune", "kovari", "myrven", "vaelun", "orveth", "caelith", "theryn"]
    ordered: list[str] = []
    for lineage in lineages:
        for gender in ("male", "female"):
            key = f"{lineage}_{gender}"
            if key in keys:
                ordered.append(key)
    return ordered

def make_jobs(config: dict[str, Any], only: str | None, generate_all: bool) -> list[CharacterJob]:
    if only and generate_all:
        raise GeneratorError("Choose either --only or --all.")
    selected = list(config["characters"])
    if only:
        selected = [only]
    elif not generate_all:
        selected = ["ardhen_male"]
    else:
        selected = lineage_order(selected)

    jobs: list[CharacterJob] = []
    for key in selected:
        if key not in config["characters"]:
            raise GeneratorError(f"Unknown character '{key}'.")
        entry = config["characters"][key]
        jobs.append(CharacterJob(
            key=key,
            lineage=entry["lineage"],
            gender=entry["gender"],
            seed=int(entry["seed"]),
            prompt="",  # filled later when actual reference mode is known
        ))
    return jobs

def resolve_style_reference(config: dict[str, Any], prompts_path: Path, override: Path | None) -> Path:
    path = override.resolve() if override is not None else (prompts_path.parent / config["settings"]["style_reference"]).resolve()
    if not path.is_file():
        raise GeneratorError(f"Style reference not found: {path}")
    return path

def prepare_style_reference(style_ref_path: Path, settings: dict[str, Any], output_path: Path) -> Path:
    canvas_size = int(settings["reference_canvas_size"])
    max_width = int(settings["reference_max_width"])
    max_height = int(settings["reference_max_height"])
    with Image.open(style_ref_path) as image:
        image = image.convert("RGBA")
        image.thumbnail((max_width, max_height), Image.Resampling.NEAREST)
        canvas = Image.new("RGBA", (canvas_size, canvas_size), (210, 214, 220, 255))
        x = (canvas_size - image.width) // 2
        y = (canvas_size - image.height) // 2
        canvas.paste(image, (x, y), image)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        canvas.convert("RGB").save(output_path, "PNG", optimize=True)
    return output_path

def to_data_url(path: Path) -> str:
    encoded = base64.b64encode(path.read_bytes()).decode("ascii")
    return f"data:image/png;base64,{encoded}"

def describe_response(response: requests.Response, body: Any) -> str:
    if isinstance(body, dict):
        base_resp = body.get("base_resp")
        if isinstance(base_resp, dict) and base_resp.get("status_code", 0) != 0:
            return f"{base_resp.get('status_code')}: {base_resp.get('status_msg')}"
        return json.dumps(body, ensure_ascii=False)[:1200]
    return response.text[:1200]

def call_minimax(api_key: str, settings: dict[str, Any], prompt: str, seed: int, reference_data_url: str, legacy_reference: bool) -> tuple[dict[str, Any], bool]:
    payload: dict[str, Any] = {
        "model": settings["model"],
        "prompt": prompt,
        "width": int(settings["width"]),
        "height": int(settings["height"]),
        "response_format": settings["response_format"],
        "seed": seed,
        "n": int(settings["n"]),
        "prompt_optimizer": bool(settings["prompt_optimizer"]),
    }
    if legacy_reference:
        payload["subject_reference"] = reference_data_url
    else:
        payload["subject_reference"] = [{"type": "character", "image_file": reference_data_url}]
    response = requests.post(
        API_URL,
        headers={"Authorization": f"Bearer {api_key}", "Content-Type": "application/json"},
        json=payload,
        timeout=int(settings["timeout_seconds"]),
    )
    try:
        body: Any = response.json()
    except ValueError:
        body = response.text
    if not response.ok:
        raise GeneratorError(f"MiniMax HTTP {response.status_code}: {describe_response(response, body)}")
    if not isinstance(body, dict):
        raise GeneratorError(f"Unexpected MiniMax response: {body!r}")
    base_resp = body.get("base_resp", {})
    if isinstance(base_resp, dict) and base_resp.get("status_code", 0) != 0:
        raise GeneratorError(f"MiniMax API error: {describe_response(response, body)}")
    return body, legacy_reference

def decode_images(body: dict[str, Any]) -> list[bytes]:
    data = body.get("data") or {}
    encoded_images = data.get("image_base64") or []
    if encoded_images:
        return [base64.b64decode(item) for item in encoded_images]
    urls = data.get("image_urls") or []
    images: list[bytes] = []
    for url in urls:
        response = requests.get(url, timeout=180)
        response.raise_for_status()
        images.append(response.content)
    return images

def save_final_pixel_image(image_bytes: bytes, postprocess: dict[str, Any], destination: Path) -> None:
    native_width = int(postprocess["native_width"])
    native_height = int(postprocess["native_height"])
    output_width = int(postprocess["output_width"])
    output_height = int(postprocess["output_height"])
    palette_colors = int(postprocess["palette_colors"])
    do_autocontrast = bool(postprocess.get("autocontrast", False))
    contrast = float(postprocess.get("contrast", 1.0))
    sharpness = float(postprocess.get("sharpness", 1.0))
    if output_width % native_width or output_height % native_height:
        raise GeneratorError("Final dimensions must be integer multiples of native dimensions.")
    with Image.open(BytesIO(image_bytes)) as source:
        source = source.convert("RGB")
        native = source.resize((native_width, native_height), Image.Resampling.BOX)
        if do_autocontrast:
            native = ImageOps.autocontrast(native, cutoff=1)
        if contrast != 1.0:
            native = ImageEnhance.Contrast(native).enhance(contrast)
        if sharpness != 1.0:
            native = ImageEnhance.Sharpness(native).enhance(sharpness)
        native = native.quantize(colors=palette_colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGB")
        final = native.resize((output_width, output_height), Image.Resampling.NEAREST)
        destination.parent.mkdir(parents=True, exist_ok=True)
        final.save(destination, "PNG", optimize=True)

def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as file:
        for block in iter(lambda: file.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()

def load_manifest(path: Path) -> dict[str, Any]:
    if path.is_file():
        try:
            return json.loads(path.read_text(encoding="utf-8"))
        except (json.JSONDecodeError, OSError):
            pass
    return {"schema_version": 11, "generations": {}}

def save_manifest(path: Path, manifest: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

def main() -> int:
    args = parse_args()
    try:
        project_root = find_project_root(args.project_root)
        prompts_path = args.prompts.resolve()
        config = load_config(prompts_path)
        settings = config["settings"]
        postprocess = settings.get("pixel_postprocess", {})
        jobs = make_jobs(config, args.only, args.all)
        style_ref = resolve_style_reference(config, prompts_path, args.style_reference)

        output_root = project_root / "art" / "generated" / "standardized_lineage_characters"
        temp_dir = project_root / "art" / ".minimax-temp"
        prepared_style_ref = temp_dir / "style_reference_prepared.png"
        manifest_path = output_root / "manifest.json"

        print(f"Godot project: {project_root}")
        print(f"Shared style reference: {style_ref}")
        print(f"MiniMax render: {settings['width']}x{settings['height']}")
        if postprocess.get("enabled"):
            print(f"Pixel pipeline: {postprocess['native_width']}x{postprocess['native_height']} native -> {postprocess['output_width']}x{postprocess['output_height']} nearest | {postprocess['palette_colors']} colors")
        print(f"Jobs: {', '.join(job.key for job in jobs)}")
        print(f"Output: {output_root}")

        prepare_style_reference(style_ref, settings, prepared_style_ref)

        if args.dry_run:
            for job in jobs:
                ref_desc = "male only if exists, else base" if job.gender == "female" else "base style only"
                entry = config["characters"][job.key]
                prompt = assemble_prompt(config, entry, use_male_reference=(job.gender == "female"), using_base_fallback=False)
                print(f"[OK] {job.key}: refs={ref_desc} | prompt={len(prompt)} chars | seed={job.seed}")
            print("Dry run complete. No API calls were made.")
            if temp_dir.exists() and not args.keep_prepared_reference:
                for p in temp_dir.glob("*"):
                    p.unlink()
                try:
                    temp_dir.rmdir()
                except OSError:
                    pass
            return 0

        api_key = os.environ.get("MINIMAX_API_KEY", "").strip()
        if not api_key:
            raise GeneratorError("MINIMAX_API_KEY is not set.")

        manifest = load_manifest(manifest_path)
        failures: list[str] = []

        for index, job in enumerate(jobs, start=1):
            destination = output_root / job.lineage / f"{job.gender}.png"
            if destination.exists() and not args.force:
                print(f"[{index}/{len(jobs)}] SKIP {job.key}: output exists. Use --force to overwrite.")
                continue

            entry = config["characters"][job.key]
            male_output = output_root / job.lineage / "male.png"
            use_male_reference = False
            using_base_fallback = False

            if job.gender == "female":
                if male_output.exists():
                    reference_path = male_output
                    use_male_reference = True
                else:
                    reference_path = prepared_style_ref
                    using_base_fallback = True
            else:
                reference_path = prepared_style_ref

            prompt = assemble_prompt(config, entry, use_male_reference=use_male_reference, using_base_fallback=using_base_fallback)

            try:
                print(f"[{index}/{len(jobs)}] Calling MiniMax for {job.key}...")
                ref_note = "male only" if use_male_reference else ("base fallback" if using_base_fallback else "base style")
                print(f"  Reference mode: {ref_note}")
                try:
                    body, used_legacy = call_minimax(
                        api_key=api_key,
                        settings=settings,
                        prompt=prompt,
                        seed=job.seed,
                        reference_data_url=to_data_url(reference_path),
                        legacy_reference=args.legacy_reference,
                    )
                except GeneratorError as first_error:
                    error_text = str(first_error).lower()

                    if (
                        args.legacy_reference
                        or "prompt length" in error_text
                        or "safe maximum" in error_text
                    ):
                        raise

                    print(f"  Documented payload rejected: {first_error}")
                    print("  Retrying with legacy local-reference payload...")
                    body, used_legacy = call_minimax(
                        api_key=api_key,
                        settings=settings,
                        prompt=prompt,
                        seed=job.seed,
                        reference_data_url=to_data_url(reference_path),
                        legacy_reference=True,
                    )

                images = decode_images(body)
                if not images:
                    raise GeneratorError(f"No image returned for {job.key}")

                if postprocess.get("enabled", False):
                    save_final_pixel_image(images[0], postprocess, destination)
                else:
                    destination.parent.mkdir(parents=True, exist_ok=True)
                    with Image.open(BytesIO(images[0])) as image:
                        image.convert("RGB").save(destination, "PNG", optimize=True)

                manifest["generations"][job.key] = {
                    "generated_at": datetime.now(timezone.utc).isoformat(),
                    "model": settings["model"],
                    "seed": job.seed,
                    "prompt": prompt,
                    "prompt_length": len(prompt),
                    "style_reference": str(style_ref.name),
                    "style_reference_sha256": sha256(style_ref),
                    "paired_male_reference": str(male_output.relative_to(project_root)).replace("\\", "/") if use_male_reference else None,
                    "reference_mode": "male_only" if use_male_reference else ("base_fallback" if using_base_fallback else "base_style"),
                    "output": str(destination.relative_to(project_root)).replace("\\", "/"),
                    "output_sha256": sha256(destination),
                    "pixel_postprocess": postprocess,
                    "request_id": body.get("id"),
                    "legacy_reference_payload": used_legacy,
                }
                save_manifest(manifest_path, manifest)
                print(f"  Saved final image: {destination}")

            except Exception as exc:
                failures.append(f"{job.key}: {exc}")
                print(f"  ERROR: {exc}", file=sys.stderr)

            if index < len(jobs):
                time.sleep(1)

        if temp_dir.exists() and not args.keep_prepared_reference:
            for p in temp_dir.glob("*"):
                p.unlink()
            try:
                temp_dir.rmdir()
            except OSError:
                pass

        if failures:
            print("\nFailures:", file=sys.stderr)
            for failure in failures:
                print(f"  - {failure}", file=sys.stderr)
            return 2

        print("\nGeneration complete.")
        return 0

    except GeneratorError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        print("\nCancelled.")
        return 130

if __name__ == "__main__":
    raise SystemExit(main())

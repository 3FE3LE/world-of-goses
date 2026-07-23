from __future__ import annotations

import argparse
import csv
import hashlib
import json
import math
import re
import shutil
import sys
import textwrap
import zipfile
from dataclasses import dataclass, replace
from pathlib import Path
from typing import Any, Iterable

from PIL import Image, ImageDraw, ImageFont

PACKAGE_ROOT = Path(__file__).resolve().parents[1]
SOURCE_DIR = PACKAGE_ROOT / 'source'
BASE_DIR = SOURCE_DIR / 'lpc_bases'
RECIPE_DIR = SOURCE_DIR / 'recipes'
VENDOR_DIR = SOURCE_DIR / 'vendor'
DEFAULT_LINEAGE_RECIPE = RECIPE_DIR / 'lineages.json'
DEFAULT_BUILD_RECIPE = RECIPE_DIR / 'build.json'
DEFAULT_OUT_ROOT = PACKAGE_ROOT / 'dist' / 'world-of-goses-lpc-lineages'
DEFAULT_ZIP_PATH = PACKAGE_ROOT / 'dist' / 'world-of-goses-lpc-lineages-godot4.zip'

OUT_ROOT = DEFAULT_OUT_ROOT
ZIP_PATH: Path | None = DEFAULT_ZIP_PATH
FRAME = 64
GODOT_FRAME = 128
DIRECTIONS: dict[str, int] = {'down': 0, 'left': 1, 'up': 2, 'right': 3}
ANIMATION_ORDER = [
    'idle', 'combat_idle', 'walk', 'run', 'jump', 'climb', 'sit', 'hurt',
    'slash', 'thrust', 'halfslash', 'backslash', 'shoot', 'spellcast',
]
ANIMATION_CONFIGS: dict[str, dict[str, Any]] = {}
ANIMATION_COUNTS = {
    'idle': 2, 'combat_idle': 2, 'walk': 9, 'run': 8, 'jump': 5,
    'climb': 6, 'sit': 3, 'hurt': 6, 'slash': 6, 'thrust': 8,
    'halfslash': 6, 'backslash': 13, 'shoot': 13, 'spellcast': 7,
}
ANIMATION_SPEEDS = {
    'idle': 3.0, 'combat_idle': 4.0, 'walk': 9.0, 'run': 12.0, 'jump': 9.0,
    'climb': 6.0, 'sit': 3.0, 'hurt': 9.0, 'slash': 11.0, 'thrust': 12.0,
    'halfslash': 11.0, 'backslash': 14.0, 'shoot': 14.0, 'spellcast': 10.0,
}
ANIMATION_LOOPS = {
    'idle': True, 'combat_idle': True, 'walk': True, 'run': True,
    'jump': False, 'climb': True, 'sit': True, 'hurt': False,
    'slash': False, 'thrust': False, 'halfslash': False, 'backslash': False,
    'shoot': False, 'spellcast': False,
}
BASELINE = [64, 126]
SCENE_OFFSET = [0, -62]
PACKAGE_NAME = 'world-of-goses-lpc-lineages'
PACKAGE_VERSION = '2.0.0'
SELECTED_GENDERS = ('male', 'female')
WEAPON_RECIPES: dict[str, Any] = {}

# LPC adult base source palette from the official body sheets.
LPC_BODY_COLORS = [
    (39, 25, 32, 255),
    (153, 66, 60, 255),
    (204, 134, 101, 255),
    (228, 164, 124, 255),
    (249, 213, 186, 255),
    (250, 236, 231, 255),
]
LPC_RANK = {c: i for i, c in enumerate(LPC_BODY_COLORS)}
HEX_COLOR = re.compile(r'^#[0-9A-Fa-f]{6}$')


def rgb(value: str) -> tuple[int, int, int]:
    value = value.lstrip('#')
    return tuple(int(value[i:i+2], 16) for i in (0, 2, 4))


def blend(a: tuple[int, int, int], b: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    return tuple(round(x * (1 - t) + y * t) for x, y in zip(a, b))


def ramp(value: str) -> list[tuple[int, int, int, int]]:
    base = rgb(value)
    values = [
        blend(base, (18, 15, 20), 0.78),
        blend(base, (18, 15, 20), 0.52),
        blend(base, (18, 15, 20), 0.27),
        base,
        blend(base, (255, 247, 235), 0.26),
        blend(base, (255, 250, 242), 0.52),
    ]
    return [(*v, 255) for v in values]


@dataclass(frozen=True)
class LineageStyle:
    key: str
    name: str
    principle: str
    visual: str
    primary: str
    secondary: str
    accent: str
    skin: str
    hair: str
    symbol: str
    accessory_profile: str
    back_profile: str
    female_hair_back_profile: str
    weapon_profile: str
    weapon_blade_outline: str
    weapon_blade: str
    weapon_shine: str
    weapon_grip: str
    weapon_length: int
    variants: dict[str, Any]


LINEAGES: list[LineageStyle] = []


def load_json(path: Path) -> dict[str, Any]:
    try:
        return json.loads(path.read_text(encoding='utf-8'))
    except FileNotFoundError as exc:
        raise SystemExit(f'Recipe file not found: {path}') from exc
    except json.JSONDecodeError as exc:
        raise SystemExit(f'Invalid JSON in {path}: {exc}') from exc


def require_color(value: str, field: str) -> str:
    if not HEX_COLOR.fullmatch(value):
        raise SystemExit(f'{field} must use #RRGGBB, received {value!r}')
    return value.upper()


def apply_overrides(recipe: dict[str, Any], overrides: list[str]) -> None:
    by_key = {item['key']: item for item in recipe.get('lineages', [])}
    for raw in overrides:
        if '=' not in raw:
            raise SystemExit(f'Invalid --set value {raw!r}; expected lineage.path=value')
        path, value = raw.split('=', 1)
        parts = path.split('.')
        if len(parts) < 3:
            raise SystemExit(f'Invalid --set path {path!r}; example: ardhen.colors.primary')
        lineage = by_key.get(parts[0])
        if lineage is None:
            raise SystemExit(f'Unknown lineage in --set: {parts[0]}')
        target: Any = lineage
        for part in parts[1:-1]:
            if not isinstance(target, dict) or part not in target:
                raise SystemExit(f'Unknown recipe field: {path}')
            target = target[part]
        field = parts[-1]
        if not isinstance(target, dict):
            raise SystemExit(f'Unknown recipe field: {path}')
        target[field] = value

def load_lineages(recipe: dict[str, Any], selected: set[str] | None = None) -> list[LineageStyle]:
    weapons = recipe.get('weapons', {})
    result: list[LineageStyle] = []
    seen: set[str] = set()
    for item in recipe.get('lineages', []):
        key = item['key']
        if selected and key not in selected:
            continue
        if key in seen:
            raise SystemExit(f'Duplicate lineage key: {key}')
        seen.add(key)
        colors = item['colors']
        profiles = item['profiles']
        weapon_profile = profiles.get('weapon', 'sword')
        weapon = weapons.get(weapon_profile)
        if not isinstance(weapon, dict):
            raise SystemExit(f'Unknown weapon profile {weapon_profile!r} for {key}')
        result.append(LineageStyle(
            key=key,
            name=item['name'],
            principle=item['principle'],
            visual=item['visual'],
            primary=require_color(colors['primary'], f'{key}.colors.primary'),
            secondary=require_color(colors['secondary'], f'{key}.colors.secondary'),
            accent=require_color(colors['accent'], f'{key}.colors.accent'),
            skin=require_color(colors['skin'], f'{key}.colors.skin'),
            hair=require_color(colors['hair'], f'{key}.colors.hair'),
            symbol=profiles.get('symbol', key),
            accessory_profile=profiles.get('accessories', key),
            back_profile=profiles.get('back', 'none'),
            female_hair_back_profile=profiles.get('female_hair_back', 'long_locks'),
            weapon_profile=weapon_profile,
            weapon_blade_outline=require_color(weapon['blade_outline'], f'weapons.{weapon_profile}.blade_outline'),
            weapon_blade=require_color(weapon['blade'], f'weapons.{weapon_profile}.blade'),
            weapon_shine=require_color(weapon['shine'], f'weapons.{weapon_profile}.shine'),
            weapon_grip=require_color(weapon['grip'], f'weapons.{weapon_profile}.grip'),
            weapon_length=int(weapon.get('length', 38)),
            variants=item.get('variants', {}),
        ))
    if not result:
        raise SystemExit('No lineages selected')
    return result


def style_for_gender(style: LineageStyle, gender: str, weapons: dict[str, Any]) -> LineageStyle:
    variant = style.variants.get(gender, {})
    colors = variant.get('colors', {})
    profiles = variant.get('profiles', {})
    weapon_profile = profiles.get('weapon', style.weapon_profile)
    weapon = weapons.get(weapon_profile)
    if not isinstance(weapon, dict):
        raise SystemExit(f'Unknown weapon profile {weapon_profile!r} for {style.key}.{gender}')
    return replace(
        style,
        primary=require_color(colors.get('primary', style.primary), f'{style.key}.variants.{gender}.colors.primary'),
        secondary=require_color(colors.get('secondary', style.secondary), f'{style.key}.variants.{gender}.colors.secondary'),
        accent=require_color(colors.get('accent', style.accent), f'{style.key}.variants.{gender}.colors.accent'),
        skin=require_color(colors.get('skin', style.skin), f'{style.key}.variants.{gender}.colors.skin'),
        hair=require_color(colors.get('hair', style.hair), f'{style.key}.variants.{gender}.colors.hair'),
        symbol=profiles.get('symbol', style.symbol),
        accessory_profile=profiles.get('accessories', style.accessory_profile),
        back_profile=profiles.get('back', style.back_profile),
        female_hair_back_profile=profiles.get('female_hair_back', style.female_hair_back_profile),
        weapon_profile=weapon_profile,
        weapon_blade_outline=require_color(weapon['blade_outline'], f'weapons.{weapon_profile}.blade_outline'),
        weapon_blade=require_color(weapon['blade'], f'weapons.{weapon_profile}.blade'),
        weapon_shine=require_color(weapon['shine'], f'weapons.{weapon_profile}.shine'),
        weapon_grip=require_color(weapon['grip'], f'weapons.{weapon_profile}.grip'),
        weapon_length=int(weapon.get('length', style.weapon_length)),
    )


def configure_build(config: dict[str, Any]) -> None:
    global FRAME, GODOT_FRAME, DIRECTIONS, ANIMATION_ORDER
    global ANIMATION_CONFIGS, ANIMATION_COUNTS, ANIMATION_SPEEDS, ANIMATION_LOOPS
    global BASELINE, SCENE_OFFSET, PACKAGE_NAME, PACKAGE_VERSION

    FRAME = int(config['source_frame_size'])
    GODOT_FRAME = int(config['output_frame_size'])
    if FRAME != 64 or GODOT_FRAME != 128:
        raise SystemExit('This renderer currently supports a 64x64 LPC source and 128x128 output canvas.')
    DIRECTIONS = {str(key): int(value) for key, value in config['directions'].items()}
    animations = config['animations']
    ANIMATION_ORDER = [item['name'] for item in animations]
    ANIMATION_CONFIGS = {item['name']: item for item in animations}
    ANIMATION_COUNTS = {item['name']: int(item['frames']) for item in animations}
    ANIMATION_SPEEDS = {item['name']: float(item['fps']) for item in animations}
    ANIMATION_LOOPS = {item['name']: bool(item['loop']) for item in animations}
    BASELINE = [int(v) for v in config.get('baseline', [64, 126])]
    SCENE_OFFSET = [int(v) for v in config.get('scene_offset', [0, -62])]
    PACKAGE_NAME = config.get('package', PACKAGE_NAME)
    PACKAGE_VERSION = config.get('package_version', PACKAGE_VERSION)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description='Generate reproducible World of Goses LPC lineage sprites.')
    parser.add_argument('--recipes', type=Path, default=DEFAULT_LINEAGE_RECIPE, help='Lineage recipe JSON.')
    parser.add_argument('--build-config', type=Path, default=DEFAULT_BUILD_RECIPE, help='Build and animation JSON.')
    parser.add_argument('--output', type=Path, default=DEFAULT_OUT_ROOT, help='Generated package directory.')
    parser.add_argument('--zip', dest='zip_path', type=Path, default=DEFAULT_ZIP_PATH, help='Generated ZIP path.')
    parser.add_argument('--no-zip', action='store_true', help='Do not create a ZIP archive.')
    parser.add_argument('--lineage', action='append', default=[], help='Generate only this lineage. Repeat for several.')
    parser.add_argument('--gender', action='append', choices=('male', 'female'), default=[], help='Generate only this body variant.')
    parser.add_argument('--set', dest='overrides', action='append', default=[], help='Temporary override, e.g. ardhen.colors.primary=#706050')
    return parser.parse_args()

def load_sources(genders: tuple[str, ...]) -> dict[str, dict[str, Image.Image]]:
    sources: dict[str, dict[str, Image.Image]] = {}
    for gender in genders:
        sources[gender] = {}
        for animation in ANIMATION_ORDER:
            config = ANIMATION_CONFIGS[animation]
            mode = config.get('mode')
            if mode not in ('sheet', 'sheet_mirror'):
                continue
            source_name = str(config['source']).format(gender=gender)
            source_path = BASE_DIR / source_name
            if source_path.exists():
                sheet = Image.open(source_path).convert('RGBA')
            else:
                fallback = config.get('fallbacks', {}).get(gender)
                if not fallback:
                    raise SystemExit(f'Missing LPC source sheet: {source_path}')
                fallback_path = BASE_DIR / fallback['source']
                if not fallback_path.exists():
                    raise SystemExit(f'Missing fallback LPC source sheet: {fallback_path}')
                fallback_sheet = Image.open(fallback_path).convert('RGBA')
                columns = [int(value) for value in fallback['columns']]
                sheet = Image.new('RGBA', (len(columns) * FRAME, len(DIRECTIONS) * FRAME), (0, 0, 0, 0))
                for row in DIRECTIONS.values():
                    for dst_col, src_col in enumerate(columns):
                        frame = crop_frame(fallback_sheet, row, src_col)
                        sheet.alpha_composite(frame, (dst_col * FRAME, row * FRAME))
            if mode == 'sheet_mirror' and sheet.height == FRAME:
                sheet = expand_single_row_sheet(sheet)
            expected_width = ANIMATION_COUNTS[animation] * FRAME
            expected_height = len(DIRECTIONS) * FRAME
            if sheet.width < expected_width or sheet.height < expected_height:
                raise SystemExit(
                    f'{source_path.name} is {sheet.size}, expected at least '
                    f'{expected_width}x{expected_height} for {animation}'
                )
            sources[gender][animation] = sheet
    return sources


def expand_single_row_sheet(sheet: Image.Image) -> Image.Image:
    """Promote a 1-row LPC sheet (climb, hurt) into the canonical 4-row layout.

    Row 0 (down) keeps the original. Row 2 (up) is a verbatim copy because
    the LPC base sheet provides no upward-facing variant. Rows 1 (left) and
    3 (right) are horizontal mirrors of the down row.
    """
    if sheet.height != FRAME:
        return sheet
    down_row = sheet.crop((0, 0, sheet.width, FRAME))
    left_row = down_row.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    expanded = Image.new('RGBA', (sheet.width, len(DIRECTIONS) * FRAME), (0, 0, 0, 0))
    expanded.paste(down_row, (0, 0))   # down
    expanded.paste(left_row, (0, FRAME))  # left
    expanded.paste(down_row.copy(), (0, 2 * FRAME))  # up
    expanded.paste(left_row.copy(), (0, 3 * FRAME))  # right
    return expanded

def crop_frame(sheet: Image.Image, row: int, col: int) -> Image.Image:
    return sheet.crop((col * FRAME, row * FRAME, (col + 1) * FRAME, (row + 1) * FRAME)).copy()


def shade_for(original: tuple[int, int, int, int], target: list[tuple[int, int, int, int]]) -> tuple[int, int, int, int]:
    rank = LPC_RANK.get(original)
    if rank is not None:
        return target[rank]
    # Fallback for any future palette variation, ordered by luminance.
    lum = (original[0] * 299 + original[1] * 587 + original[2] * 114) / 1000
    idx = min(5, max(0, round(lum / 255 * 5)))
    return target[idx]


def is_hand_zone(direction: str, x: int, y: int) -> bool:
    if not (36 <= y <= 49):
        return False
    if direction in ('down', 'up'):
        return x <= 24 or x >= 40
    if direction == 'left':
        return x <= 27 or x >= 39
    return x >= 37 or x <= 25


def recolor_body(frame: Image.Image, style: LineageStyle, direction: str) -> Image.Image:
    primary = ramp(style.primary)
    secondary = ramp(style.secondary)
    accent = ramp(style.accent)
    skin = ramp(style.skin)
    boot = ramp(blend(rgb(style.primary), (25, 22, 25), 0.62).__class__ and '#302C2D')
    # The expression above intentionally resolves to a stable boot palette without
    # introducing a fourth configurable color token.
    px = frame.load()
    for y in range(FRAME):
        for x in range(FRAME):
            c = px[x, y]
            if c[3] == 0:
                continue
            if is_hand_zone(direction, x, y):
                target = skin
            elif y >= 56:
                target = boot
            elif y >= 44:
                target = secondary
            elif y >= 40 and direction in ('left', 'right'):
                target = secondary
            else:
                target = primary
            px[x, y] = shade_for(c, target)
    return frame


def poly(draw: ImageDraw.ImageDraw, points: Iterable[tuple[int, int]], fill):
    draw.polygon(list(points), fill=fill)


def rect(draw: ImageDraw.ImageDraw, xy, fill):
    draw.rectangle(xy, fill=fill)


def draw_hair_back(draw: ImageDraw.ImageDraw, style: LineageStyle, gender: str, direction: str, sy: int) -> None:
    hair = ramp(style.hair)
    outline, dark, mid, base, light, hi = hair
    if gender == 'female':
        if style.female_hair_back_profile == 'bun':
            # Neat bun, repeated circular units.
            if direction == 'left':
                rect(draw, (37, 17 + sy, 43, 24 + sy), outline)
                rect(draw, (38, 18 + sy, 42, 23 + sy), base)
            elif direction == 'right':
                rect(draw, (20, 17 + sy, 26, 24 + sy), outline)
                rect(draw, (21, 18 + sy, 25, 23 + sy), base)
            else:
                rect(draw, (28, 10 + sy, 35, 15 + sy), outline)
                rect(draw, (29, 11 + sy, 34, 14 + sy), base)
        elif style.female_hair_back_profile == 'braid':
            # Heavy braid / travel braid.
            x = 39 if direction != 'right' else 21
            for i in range(4):
                yy = 24 + sy + i * 4
                rect(draw, (x, yy, x + 3, yy + 3), outline)
                rect(draw, (x + 1, yy, x + 2, yy + 2), base if i % 2 == 0 else light)
        elif style.female_hair_back_profile == 'mechanical_ponytail':
            # High ponytail, held by a mechanical clasp.
            if direction == 'left':
                poly(draw, [(36, 16 + sy), (45, 17 + sy), (42, 21 + sy), (46, 26 + sy), (38, 24 + sy)], outline)
                poly(draw, [(37, 17 + sy), (43, 18 + sy), (40, 21 + sy), (44, 24 + sy), (38, 23 + sy)], base)
            elif direction == 'right':
                poly(draw, [(27, 16 + sy), (18, 17 + sy), (21, 21 + sy), (17, 26 + sy), (25, 24 + sy)], outline)
                poly(draw, [(26, 17 + sy), (20, 18 + sy), (23, 21 + sy), (19, 24 + sy), (25, 23 + sy)], base)
            else:
                rect(draw, (37, 15 + sy, 43, 22 + sy), outline)
                rect(draw, (38, 16 + sy, 42, 21 + sy), base)
        else:
            # Long locks. Side views keep the rear silhouette readable.
            if direction == 'left':
                poly(draw, [(31, 16 + sy), (40, 17 + sy), (42, 35 + sy), (38, 42 + sy), (33, 36 + sy)], outline)
                poly(draw, [(33, 18 + sy), (38, 18 + sy), (40, 34 + sy), (37, 39 + sy), (35, 34 + sy)], base)
            elif direction == 'right':
                poly(draw, [(32, 16 + sy), (23, 17 + sy), (21, 35 + sy), (25, 42 + sy), (30, 36 + sy)], outline)
                poly(draw, [(30, 18 + sy), (25, 18 + sy), (23, 34 + sy), (26, 39 + sy), (28, 34 + sy)], base)
            else:
                rect(draw, (22, 20 + sy, 26, 40 + sy), outline)
                rect(draw, (38, 20 + sy, 42, 40 + sy), outline)
                rect(draw, (23, 22 + sy, 25, 38 + sy), base)
                rect(draw, (39, 22 + sy, 41, 38 + sy), base)
                rect(draw, (24, 24 + sy, 24, 30 + sy), light)


def draw_head(draw: ImageDraw.ImageDraw, style: LineageStyle, direction: str, sy: int) -> None:
    skin = ramp(style.skin)
    outline, dark, shadow, base, light, hi = skin
    if direction == 'down':
        poly(draw, [(26, 15 + sy), (37, 15 + sy), (40, 18 + sy), (41, 27 + sy), (37, 33 + sy), (27, 33 + sy), (23, 28 + sy), (23, 20 + sy)], outline)
        poly(draw, [(27, 17 + sy), (36, 17 + sy), (38, 19 + sy), (39, 27 + sy), (36, 31 + sy), (28, 31 + sy), (25, 27 + sy), (25, 20 + sy)], base)
        rect(draw, (25, 22 + sy, 26, 28 + sy), light)
        rect(draw, (37, 20 + sy, 38, 28 + sy), shadow)
        rect(draw, (22, 22 + sy, 24, 27 + sy), outline)
        rect(draw, (23, 23 + sy, 24, 26 + sy), base)
        rect(draw, (40, 22 + sy, 42, 27 + sy), outline)
        rect(draw, (40, 23 + sy, 41, 26 + sy), shadow)
        rect(draw, (28, 24 + sy, 29, 25 + sy), (45, 32, 39, 255))
        rect(draw, (35, 24 + sy, 36, 25 + sy), (45, 32, 39, 255))
        rect(draw, (31, 28 + sy, 33, 28 + sy), dark)
    elif direction == 'up':
        poly(draw, [(26, 15 + sy), (37, 15 + sy), (40, 19 + sy), (40, 29 + sy), (36, 33 + sy), (28, 33 + sy), (24, 29 + sy), (24, 19 + sy)], outline)
        poly(draw, [(27, 17 + sy), (36, 17 + sy), (38, 20 + sy), (38, 28 + sy), (35, 31 + sy), (29, 31 + sy), (26, 28 + sy), (26, 20 + sy)], shadow)
        rect(draw, (27, 18 + sy, 29, 27 + sy), base)
    elif direction == 'left':
        poly(draw, [(27, 15 + sy), (36, 15 + sy), (39, 19 + sy), (39, 28 + sy), (35, 33 + sy), (27, 32 + sy), (23, 28 + sy), (23, 20 + sy)], outline)
        poly(draw, [(28, 17 + sy), (35, 17 + sy), (37, 20 + sy), (37, 28 + sy), (34, 31 + sy), (28, 30 + sy), (25, 27 + sy), (25, 20 + sy)], base)
        rect(draw, (22, 23 + sy, 25, 26 + sy), base)
        rect(draw, (26, 23 + sy, 27, 24 + sy), (45, 32, 39, 255))
        rect(draw, (24, 28 + sy, 27, 28 + sy), dark)
        rect(draw, (34, 19 + sy, 36, 29 + sy), shadow)
    else:
        poly(draw, [(28, 15 + sy), (37, 15 + sy), (41, 20 + sy), (41, 28 + sy), (37, 32 + sy), (29, 33 + sy), (25, 28 + sy), (25, 19 + sy)], outline)
        poly(draw, [(29, 17 + sy), (36, 17 + sy), (39, 20 + sy), (39, 27 + sy), (36, 30 + sy), (29, 31 + sy), (27, 28 + sy), (27, 20 + sy)], base)
        rect(draw, (39, 23 + sy, 42, 26 + sy), shadow)
        rect(draw, (36, 23 + sy, 37, 24 + sy), (45, 32, 39, 255))
        rect(draw, (36, 28 + sy, 39, 28 + sy), dark)
        rect(draw, (28, 19 + sy, 30, 29 + sy), light)


def draw_hair_front(draw: ImageDraw.ImageDraw, style: LineageStyle, gender: str, direction: str, sy: int) -> None:
    hair = ramp(style.hair)
    outline, dark, shadow, base, light, hi = hair
    if direction == 'up':
        poly(draw, [(25, 14 + sy), (38, 14 + sy), (41, 18 + sy), (40, 30 + sy), (36, 33 + sy), (28, 33 + sy), (23, 29 + sy), (23, 19 + sy)], outline)
        poly(draw, [(27, 15 + sy), (37, 15 + sy), (39, 19 + sy), (38, 29 + sy), (35, 31 + sy), (29, 31 + sy), (25, 28 + sy), (25, 19 + sy)], base)
        rect(draw, (27, 16 + sy, 30, 27 + sy), light)
        return
    if direction == 'down':
        if gender == 'male':
            poly(draw, [(25, 14 + sy), (38, 14 + sy), (41, 18 + sy), (39, 22 + sy), (35, 20 + sy), (32, 23 + sy), (29, 20 + sy), (24, 23 + sy), (22, 19 + sy)], outline)
            poly(draw, [(26, 15 + sy), (37, 15 + sy), (39, 18 + sy), (37, 20 + sy), (34, 18 + sy), (32, 21 + sy), (29, 18 + sy), (25, 21 + sy), (24, 18 + sy)], base)
        else:
            poly(draw, [(24, 14 + sy), (39, 14 + sy), (42, 18 + sy), (40, 24 + sy), (36, 20 + sy), (33, 24 + sy), (29, 20 + sy), (24, 25 + sy), (21, 19 + sy)], outline)
            poly(draw, [(26, 15 + sy), (38, 15 + sy), (40, 18 + sy), (38, 21 + sy), (35, 18 + sy), (33, 22 + sy), (29, 18 + sy), (25, 22 + sy), (23, 18 + sy)], base)
        rect(draw, (27, 15 + sy, 31, 16 + sy), light)
    elif direction == 'left':
        poly(draw, [(26, 14 + sy), (37, 14 + sy), (40, 18 + sy), (38, 23 + sy), (34, 20 + sy), (30, 23 + sy), (24, 20 + sy), (23, 17 + sy)], outline)
        poly(draw, [(27, 15 + sy), (36, 15 + sy), (38, 18 + sy), (36, 21 + sy), (33, 18 + sy), (29, 21 + sy), (25, 19 + sy)], base)
        rect(draw, (28, 15 + sy, 31, 16 + sy), light)
    else:
        poly(draw, [(27, 14 + sy), (38, 14 + sy), (41, 17 + sy), (40, 20 + sy), (34, 23 + sy), (30, 20 + sy), (26, 23 + sy), (23, 18 + sy)], outline)
        poly(draw, [(28, 15 + sy), (37, 15 + sy), (39, 18 + sy), (35, 21 + sy), (31, 18 + sy), (27, 21 + sy), (25, 18 + sy)], base)
        rect(draw, (29, 15 + sy, 32, 16 + sy), light)


def draw_lineage_accessories(draw: ImageDraw.ImageDraw, style: LineageStyle, direction: str, sy: int, body_top: int) -> None:
    a = ramp(style.accent)
    p = ramp(style.primary)
    s = ramp(style.secondary)
    outline = a[0]
    y = body_top + 6
    # Most chest signs are intentionally tiny. Their silhouettes survive palette swaps.
    profile = style.accessory_profile
    if profile == 'ardhen':
        rect(draw, (24, body_top + 2, 27, body_top + 5), a[0])
        rect(draw, (25, body_top + 2, 27, body_top + 4), a[3])
        rect(draw, (37, body_top + 2, 40, body_top + 5), a[0])
        rect(draw, (37, body_top + 2, 39, body_top + 4), a[3])
        if direction in ('down', 'up'):
            rect(draw, (29, y, 30, y + 5), a[4])
            rect(draw, (34, y, 35, y + 5), a[4])
            rect(draw, (30, y + 2, 34, y + 3), a[3])
    elif profile == 'eirune':
        if direction in ('down', 'up'):
            draw.line((28, body_top + 2, 35, body_top + 12), fill=s[4], width=2)
            rect(draw, (34, body_top + 7, 37, body_top + 9), a[3])
            rect(draw, (35, body_top + 6, 36, body_top + 10), a[4])
        # Seed/cell at shoulder.
        rect(draw, (23, body_top + 1, 25, body_top + 3), a[3])
        rect(draw, (24, body_top, 24, body_top + 4), a[4])
    elif profile == 'kovari':
        draw.line((27, body_top + 1, 36, body_top + 13), fill=s[1], width=2)
        for x, yy in ((26, body_top + 3), (37, body_top + 3), (29, body_top + 10), (35, body_top + 10)):
            rect(draw, (x, yy, x + 1, yy + 1), a[4])
        # Goggle band.
        rect(draw, (25, 21 + sy, 39, 22 + sy), outline)
        if direction == 'down':
            rect(draw, (27, 21 + sy, 30, 24 + sy), outline)
            rect(draw, (34, 21 + sy, 37, 24 + sy), outline)
            rect(draw, (28, 22 + sy, 29, 23 + sy), a[5])
            rect(draw, (35, 22 + sy, 36, 23 + sy), a[5])
    elif profile == 'myrven':
        # Double collar and half-mask.
        rect(draw, (27, body_top + 1, 31, body_top + 4), s[4])
        rect(draw, (33, body_top + 1, 37, body_top + 4), a[4])
        if direction == 'down':
            rect(draw, (32, 23 + sy, 38, 27 + sy), p[1])
            rect(draw, (33, 24 + sy, 37, 26 + sy), s[4])
        elif direction == 'left':
            rect(draw, (24, 23 + sy, 29, 27 + sy), p[1])
        elif direction == 'right':
            rect(draw, (35, 23 + sy, 40, 27 + sy), p[1])
        draw.line((31, body_top + 4, 31, body_top + 13), fill=s[4], width=1)
        draw.line((33, body_top + 4, 33, body_top + 13), fill=a[4], width=1)
    elif profile == 'vaelun':
        # Travel scarf and compass diamond.
        rect(draw, (26, body_top, 38, body_top + 2), a[3])
        if direction == 'left':
            poly(draw, [(37, body_top + 1), (45, body_top + 4), (39, body_top + 7)], fill=a[3])
        elif direction == 'right':
            poly(draw, [(27, body_top + 1), (19, body_top + 4), (25, body_top + 7)], fill=a[3])
        elif direction == 'up':
            poly(draw, [(37, body_top + 1), (42, body_top + 6), (36, body_top + 5)], fill=a[3])
        else:
            poly(draw, [(36, body_top + 2), (41, body_top + 9), (35, body_top + 7)], fill=a[3])
        if direction == 'down':
            poly(draw, [(32, y), (35, y + 3), (32, y + 6), (29, y + 3)], fill=s[0])
            poly(draw, [(32, y + 1), (34, y + 3), (32, y + 5), (30, y + 3)], fill=a[5])
            rect(draw, (32, y + 2, 32, y + 4), s[2])
    elif profile == 'orveth':
        # Symmetrical lapels, seal and twin pouches.
        draw.line((27, body_top + 2, 31, body_top + 10), fill=a[4], width=2)
        draw.line((37, body_top + 2, 33, body_top + 10), fill=a[4], width=2)
        rect(draw, (24, body_top + 12, 28, body_top + 16), s[0])
        rect(draw, (36, body_top + 12, 40, body_top + 16), s[0])
        rect(draw, (25, body_top + 13, 27, body_top + 15), s[3])
        rect(draw, (37, body_top + 13, 39, body_top + 15), s[3])
        rect(draw, (30, body_top + 11, 34, body_top + 15), a[0])
        rect(draw, (31, body_top + 12, 33, body_top + 14), a[4])
    elif profile == 'caelith':
        # Node triangle and circlet.
        rect(draw, (25, 19 + sy, 39, 20 + sy), a[1])
        for xx in (26, 32, 38):
            rect(draw, (xx, 18 + sy, xx + 1, 20 + sy), a[5])
        if direction == 'down':
            draw.line((28, y + 1, 32, y + 6), fill=a[4], width=1)
            draw.line((36, y + 1, 32, y + 6), fill=a[4], width=1)
            draw.line((28, y + 1, 36, y + 1), fill=a[3], width=1)
            for xx, yy in ((28, y), (36, y), (32, y + 6)):
                rect(draw, (xx, yy, xx + 1, yy + 1), a[5])
    elif profile == 'theryn':
        # Necklace and pulse line.
        if direction == 'down':
            for xx, yy in ((28, body_top + 3), (30, body_top + 5), (32, body_top + 6), (34, body_top + 5), (36, body_top + 3)):
                rect(draw, (xx, yy, xx + 1, yy + 1), a[4])
            draw.line((26, body_top + 10, 29, body_top + 10, 31, body_top + 8, 33, body_top + 13, 35, body_top + 10, 38, body_top + 10), fill=s[5], width=1)
        else:
            rect(draw, (29, body_top + 4, 35, body_top + 5), a[4])


def compose_character_frame(base: Image.Image, style: LineageStyle, gender: str, direction: str) -> Image.Image:
    bbox = base.getbbox() or (17, 32, 47, 62)
    sy = max(-1, min(2, bbox[1] - 32))
    body_top = bbox[1]
    back = Image.new('RGBA', (FRAME, FRAME), (0, 0, 0, 0))
    draw_back = ImageDraw.Draw(back)
    draw_hair_back(draw_back, style, gender, direction, sy)
    if style.back_profile == 'mantle':
        # Layered mantle behind the torso.
        s = ramp(style.secondary)
        if direction == 'down':
            poly(draw_back, [(23, body_top + 1), (41, body_top + 1), (39, 51), (32, 55), (25, 51)], fill=s[1])
            poly(draw_back, [(25, body_top + 2), (39, body_top + 2), (37, 49), (32, 52), (27, 49)], fill=s[3])
    if style.back_profile == 'vine':
        a = ramp(style.accent)
        # Living vine silhouette behind one shoulder.
        draw_back.line((22, body_top + 1, 19, body_top - 4, 21, body_top - 8), fill=a[2], width=2)
        rect(draw_back, (17, body_top - 6, 20, body_top - 4), a[3])
    recolored = recolor_body(base.copy(), style, direction)
    back.alpha_composite(recolored)
    front = Image.new('RGBA', (FRAME, FRAME), (0, 0, 0, 0))
    draw = ImageDraw.Draw(front)
    draw_head(draw, style, direction, sy)
    draw_hair_front(draw, style, gender, direction, sy)
    draw_lineage_accessories(draw, style, direction, sy, body_top)
    back.alpha_composite(front)
    return back


def sword_geometry(direction: str, phase: int, length: int) -> tuple[tuple[int, int], tuple[int, int], tuple[int, int]]:
    # Kept as a no-op helper so external tooling that imported it still
    # resolves. The procedural sword arc was retired when the combat
    # animations moved to the official LPC slash/thrust/halfslash/backslash
    # sheets, which already carry the weapon drawn in.
    del direction, phase, length
    return (0, 0), (0, 0), (0, 0)


def draw_sword(layer: Image.Image, style: LineageStyle, direction: str, phase: int, behind: bool) -> None:
    # Deprecated: combat animations are now sourced directly from the
    # Universal LPC Spritesheet Character Generator. The weapon is baked
    # into each sheet, so no procedural overlay is required.
    del layer, style, direction, phase, behind


def normalize_128(frame64: Image.Image) -> Image.Image:
    out = Image.new('RGBA', (GODOT_FRAME, GODOT_FRAME), (0, 0, 0, 0))
    x = (GODOT_FRAME - FRAME) // 2
    y = GODOT_FRAME - FRAME
    out.alpha_composite(frame64, (x, y))
    return out

def make_slash_frames(idle_frames: list[Image.Image], walk_frames: list[Image.Image], style: LineageStyle, direction: str) -> list[Image.Image]:
    # Deprecated: kept only so older recipes that still reference the
    # `generated_slash` mode don't crash before validating the recipe.
    # Combat animations now use the official LPC sheets via `mode = sheet`.
    del idle_frames, walk_frames, style, direction
    return []


def save_strip(frames: list[Image.Image], path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    strip = Image.new('RGBA', (len(frames) * GODOT_FRAME, GODOT_FRAME), (0, 0, 0, 0))
    for i, frame in enumerate(frames):
        if frame.size == (64, 64):
            frame = normalize_128(frame)
        strip.alpha_composite(frame, (i * GODOT_FRAME, 0))
    strip.save(path, optimize=True)


def make_sprite_frames_tres(style: LineageStyle, gender: str, char_dir: Path) -> str:
    animations = []
    ext = []
    sub = []
    ext_index = 1
    frame_total = 0
    for anim in ANIMATION_ORDER:
        for direction in DIRECTIONS:
            key = f'{anim}_{direction}'
            tex_path = f'res://assets/characters/lineages/{style.key}/{gender}/textures/{key}_128.png'
            ext_id = f'{ext_index}_{key}'
            ext.append(f'[ext_resource type="Texture2D" path="{tex_path}" id="{ext_id}"]')
            frame_refs = []
            for i in range(ANIMATION_COUNTS[anim]):
                sub_id = f'Atlas_{key}_{i}'
                sub.append(textwrap.dedent(f'''\
                [sub_resource type="AtlasTexture" id="{sub_id}"]
                atlas = ExtResource("{ext_id}")
                region = Rect2({i * GODOT_FRAME}, 0, {GODOT_FRAME}, {GODOT_FRAME})
                ''').rstrip())
                frame_refs.append(textwrap.dedent(f'''\
                {{
                "duration": 1.0,
                "texture": SubResource("{sub_id}")
                }}''').rstrip())
                frame_total += 1
            animations.append(textwrap.dedent(f'''\
            {{
            "frames": [{', '.join(frame_refs)}],
            "loop": {'true' if ANIMATION_LOOPS[anim] else 'false'},
            "name": &"{key}",
            "speed": {ANIMATION_SPEEDS[anim]}
            }}''').rstrip())
            ext_index += 1
    load_steps = 1 + len(ext) + frame_total
    return '\n\n'.join([
        f'[gd_resource type="SpriteFrames" load_steps={load_steps} format=3]',
        '\n'.join(ext),
        '\n\n'.join(sub),
        '[resource]\nanimations = [' + ',\n'.join(animations) + ']\n',
    ])

def make_scene(style: LineageStyle, gender: str) -> str:
    frames_path = f'res://assets/characters/lineages/{style.key}/{gender}/{style.key}_{gender}_sprite_frames.tres'
    node_name = f'{style.name}{gender.title()}'
    default_animation = 'idle_down' if 'idle' in ANIMATION_ORDER else f'{ANIMATION_ORDER[0]}_down'
    return textwrap.dedent(f'''\
    [gd_scene load_steps=3 format=3]

    [ext_resource type="SpriteFrames" path="{frames_path}" id="1_frames"]
    [ext_resource type="Script" path="res://scripts/visual/LineageSpritePlayer.cs" id="2_script"]

    [node name="{node_name}" type="AnimatedSprite2D"]
    texture_filter = 1
    sprite_frames = ExtResource("1_frames")
    animation = &"{default_animation}"
    autoplay = "{default_animation}"
    centered = true
    offset = Vector2({SCENE_OFFSET[0]}, {SCENE_OFFSET[1]})
    script = ExtResource("2_script")
    ''')

def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def selected_credits() -> list[dict[str, str]]:
    path = VENDOR_DIR / 'LPC_SELECTED_CREDITS.csv'
    with path.open(newline='', encoding='utf-8') as f:
        return list(csv.DictReader(f))


def write_selected_credits(path: Path) -> None:
    rows = selected_credits()
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('w', newline='', encoding='utf-8') as f:
        writer = csv.DictWriter(f, fieldnames=['filename', 'notes', 'authors', 'licenses', 'urls'])
        writer.writeheader()
        writer.writerows(rows)

def build_contact_sheet(previews: dict[tuple[str, str], Image.Image], output: Path) -> None:
    cell_w, cell_h = 320, 280
    columns = min(4, max(1, len(LINEAGES)))
    rows = math.ceil(len(LINEAGES) / columns)
    canvas = Image.new('RGBA', (cell_w * columns, cell_h * rows), (20, 22, 28, 255))
    draw = ImageDraw.Draw(canvas)
    try:
        font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 20)
        small = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 13)
        tiny = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 12)
    except OSError:
        font = ImageFont.load_default()
        small = font
        tiny = font

    for idx, style in enumerate(LINEAGES):
        col = idx % columns
        row = idx // columns
        x0, y0 = col * cell_w, row * cell_h
        draw.rectangle((x0, y0, x0 + cell_w - 1, y0 + cell_h - 1), outline=(63, 69, 82, 255), width=1)
        draw.text((x0 + 14, y0 + 12), style.name, font=font, fill=(238, 235, 225, 255))
        wrapped = textwrap.wrap(style.principle, width=34)
        for line_i, line in enumerate(wrapped[:2]):
            draw.text((x0 + 14, y0 + 42 + line_i * 17), line, font=small, fill=(168, 179, 190, 255))

        gender_count = len(SELECTED_GENDERS)
        for gender_i, gender in enumerate(SELECTED_GENDERS):
            frame = previews[(style.key, gender)]
            bbox = frame.getbbox() or (32, 64, 96, 128)
            bbox = (max(0, bbox[0] - 3), max(0, bbox[1] - 3), min(128, bbox[2] + 3), min(128, bbox[3] + 2))
            crop = frame.crop(bbox)
            scale = 3
            crop = crop.resize((crop.width * scale, crop.height * scale), Image.Resampling.NEAREST)
            slot_x = x0 + 102 if gender_count == 1 else x0 + 30 + gender_i * 150
            slot_y = y0 + 82
            canvas.alpha_composite(crop, (slot_x + (115 - crop.width) // 2, slot_y))

        animation_label = ' · '.join(ANIMATION_ORDER)
        draw.text((x0 + 14, y0 + 257), f"{' · '.join(SELECTED_GENDERS)} · {animation_label} · 128", font=tiny, fill=(198, 167, 103, 255))

    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output, optimize=True)

def write_docs(manifest: dict) -> None:
    docs = OUT_ROOT / 'docs'
    docs.mkdir(parents=True, exist_ok=True)
    animation_lines = []
    for animation in ANIMATION_ORDER:
        names = ', '.join(f'`{animation}_{direction}`' for direction in DIRECTIONS)
        animation_lines.append(f'- {names}')
    non_looping = [name for name in ANIMATION_ORDER if not ANIMATION_LOOPS[name]]
    non_looping_text = ', '.join(f'`{name}_*`' for name in non_looping) or 'Ninguna'
    animation_markdown = '\n'.join(animation_lines)
    (OUT_ROOT / 'IMPORT_GODOT4.md').write_text(textwrap.dedent(f'''\
    # Importación en Godot 4

    ## Instalación

    1. Copia `assets/`, `scripts/` y `docs/` junto a `project.godot`.
    2. Abre Godot y espera la importación de PNG.
    3. Instancia una escena desde `res://assets/characters/lineages/<linaje>/<male|female>/`.

    ## Animaciones generadas

    {animation_markdown}

    Animaciones sin loop: {non_looping_text}.

    Todas las celdas miden `{GODOT_FRAME} × {GODOT_FRAME}`, usan transparencia real y baseline `{tuple(BASELINE)}`.
    Las escenas fuerzan `Texture Filter = Nearest` y `offset = Vector2({SCENE_OFFSET[0]}, {SCENE_OFFSET[1]})`.

    ## Uso desde C#

    ```csharp
    sprite.PlayIdle(Vector2.Down);
    sprite.PlayCombatIdle(Vector2.Down);
    sprite.PlayWalk(velocity);
    sprite.PlayRun(velocity);
    sprite.PlayJump(Vector2.Down);
    sprite.PlayClimb(Vector2.Down);
    sprite.PlaySit(Vector2.Down);
    sprite.PlayHurt(Vector2.Down);
    sprite.PlaySlash(Vector2.Right);
    sprite.PlayThrust(Vector2.Right);
    sprite.PlayHalfslash(Vector2.Right);
    sprite.PlayBackslash(Vector2.Right);
    sprite.PlayShoot(Vector2.Right);
    sprite.PlaySpellcast(Vector2.Down);
    sprite.PlayDirectional("idle", velocity); // cualquier nombre declarado en build.json
    ```
    '''), encoding='utf-8')

    matrix_lines = ['# Matriz visual de linajes', '', 'Los atuendos codifican cultura y gramática visual, no clases ni profesiones.', '', '| Linaje | Núcleo | Traducción visual | Perfiles editables |', '|---|---|---|---|']
    for style in LINEAGES:
        profiles = f'accessories={style.accessory_profile}; back={style.back_profile}; female_hair_back={style.female_hair_back_profile}; weapon={style.weapon_profile}'
        matrix_lines.append(f'| {style.name} | {style.principle} | {style.visual}; símbolo `{style.symbol}` | `{profiles}` |')
    (docs / 'LINEAGE_DESIGN_MATRIX.md').write_text('\n'.join(matrix_lines) + '\n', encoding='utf-8')

    (docs / 'LICENSING_AND_ATTRIBUTION.md').write_text(textwrap.dedent('''\
    # Licencias y atribución

    Los cuerpos y ciclos de movimiento proceden de bases oficiales del **Universal LPC Spritesheet Character Generator**. Las prendas, símbolos, accesorios y paletas son adaptaciones originales para World of Goses.

    Conserva los archivos dentro de `docs/licenses/`. El generador web permite exportar créditos y configuraciones JSON, pero este paquete utiliza su propio archivo determinista `source/recipes/lineages.json` para las capas originales de World of Goses.

    ## Transformaciones

    - Recoloración por paleta y región corporal.
    - Cabezas, cabello, prendas, accesorios y símbolos dibujados proceduralmente.
    - Normalización a celdas `128 × 128` sin antialiasing.
    - `slash` de seis frames compuesto sobre poses LPC.
    - Idle femenino derivado de columnas neutrales del walk oficial incluido.
    '''), encoding='utf-8')

    (docs / 'MANIFEST.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')

def write_csharp() -> None:
    path = OUT_ROOT / 'scripts' / 'visual' / 'LineageSpritePlayer.cs'
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(textwrap.dedent('''\
    using Godot;

    namespace WorldOfGoses.Visuals;

    public partial class LineageSpritePlayer : AnimatedSprite2D
    {
        private Vector2 _facing = Vector2.Down;

        public void PlayDirectional(string animationName, Vector2 direction)
        {
            _facing = NormalizeDirection(direction, _facing);
            Play($"{animationName}_{ToSuffix(_facing)}");
        }

        public void PlayIdle(Vector2 direction) => PlayDirectional("idle", direction);
        public void PlayCombatIdle(Vector2 direction) => PlayDirectional("combat_idle", direction);
        public void PlayWalk(Vector2 direction) => PlayDirectional("walk", direction);
        public void PlayRun(Vector2 direction) => PlayDirectional("run", direction);
        public void PlayJump(Vector2 direction) => PlayDirectional("jump", direction);
        public void PlayClimb(Vector2 direction) => PlayDirectional("climb", direction);
        public void PlaySit(Vector2 direction) => PlayDirectional("sit", direction);
        public void PlayHurt(Vector2 direction) => PlayDirectional("hurt", direction);
        public void PlaySlash(Vector2 direction) => PlayDirectional("slash", direction);
        public void PlayThrust(Vector2 direction) => PlayDirectional("thrust", direction);
        public void PlayHalfslash(Vector2 direction) => PlayDirectional("halfslash", direction);
        public void PlayBackslash(Vector2 direction) => PlayDirectional("backslash", direction);
        public void PlayShoot(Vector2 direction) => PlayDirectional("shoot", direction);
        public void PlaySpellcast(Vector2 direction) => PlayDirectional("spellcast", direction);
        public void ResumeIdle() => Play($"idle_{ToSuffix(_facing)}");

        private static Vector2 NormalizeDirection(Vector2 direction, Vector2 fallback)
        {
            if (direction.IsZeroApprox())
            {
                return fallback;
            }

            return Mathf.Abs(direction.X) > Mathf.Abs(direction.Y)
                ? new Vector2(Mathf.Sign(direction.X), 0)
                : new Vector2(0, Mathf.Sign(direction.Y));
        }

        private static string ToSuffix(Vector2 direction)
        {
            if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
            {
                return direction.X < 0 ? "left" : "right";
            }

            return direction.Y < 0 ? "up" : "down";
        }
    }
    '''), encoding='utf-8')

def validate_generated_pngs() -> None:
    errors: list[str] = []
    for path in OUT_ROOT.glob('assets/characters/lineages/*/*/textures/*_128.png'):
        with Image.open(path) as image:
            if image.height != GODOT_FRAME or image.width % GODOT_FRAME != 0:
                errors.append(f'{path}: invalid strip size {image.size}')
            if image.mode != 'RGBA':
                errors.append(f'{path}: expected RGBA, got {image.mode}')
            alpha_values = set(image.getchannel('A').tobytes())
            if not alpha_values.issubset({0, 255}):
                errors.append(f'{path}: alpha is not binary')
    if errors:
        raise SystemExit('Generated asset validation failed:\n' + '\n'.join(errors))


def copy_reproducible_sources() -> None:
    destination = OUT_ROOT / 'source'
    shutil.copytree(
        SOURCE_DIR,
        destination,
        dirs_exist_ok=True,
        ignore=shutil.ignore_patterns('__pycache__', '*.pyc'),
    )
    for name in ('build.ps1', 'build.sh', 'README_GENERATOR.md'):
        source = PACKAGE_ROOT / name
        if source.exists():
            shutil.copy2(source, OUT_ROOT / name)


def generate() -> None:
    resolved_output = OUT_ROOT.resolve()
    if resolved_output == PACKAGE_ROOT.resolve():
        raise SystemExit('Refusing to overwrite the source package. Use dist/ or another output directory.')
    if OUT_ROOT.exists():
        shutil.rmtree(OUT_ROOT)
    OUT_ROOT.mkdir(parents=True)
    sources = load_sources(SELECTED_GENDERS)
    previews: dict[tuple[str, str], Image.Image] = {}
    manifest = {
        'package': PACKAGE_NAME,
        'version': PACKAGE_VERSION,
        'frame_size': [GODOT_FRAME, GODOT_FRAME],
        'baseline': BASELINE,
        'directions': list(DIRECTIONS),
        'animations': {
            name: {
                'frames': ANIMATION_COUNTS[name],
                'fps': ANIMATION_SPEEDS[name],
                'loop': ANIMATION_LOOPS[name],
                'mode': ANIMATION_CONFIGS[name]['mode'],
            }
            for name in ANIMATION_ORDER
        },
        'recipe': 'res://source/recipes/lineages.json',
        'build_recipe': 'res://source/recipes/build.json',
        'characters': [],
    }

    for style in LINEAGES:
        for gender in SELECTED_GENDERS:
            render_style = style_for_gender(style, gender, WEAPON_RECIPES)
            char_dir = OUT_ROOT / 'assets' / 'characters' / 'lineages' / style.key / gender
            textures = char_dir / 'textures'
            char_animations: dict[str, list[Image.Image]] = {}
            for direction, row in DIRECTIONS.items():
                raw_composed: dict[str, list[Image.Image]] = {}
                for animation in ANIMATION_ORDER:
                    config = ANIMATION_CONFIGS[animation]
                    mode = config['mode']
                    if mode in ('sheet', 'sheet_mirror'):
                        # `sheet_mirror` sheets were already expanded to 4
                        # rows inside load_sources, so the crop_frame call
                        # here is identical for both modes.
                        base_frames = [crop_frame(sources[gender][animation], row, column) for column in range(ANIMATION_COUNTS[animation])]
                        raw_composed[animation] = [compose_character_frame(frame, render_style, gender, direction) for frame in base_frames]
                        final_frames = [normalize_128(frame) for frame in raw_composed[animation]]
                    elif mode == 'generated_slash':
                        raise SystemExit(
                            "Animation mode 'generated_slash' was retired in package version 3.0.0. "
                            "Use the official LPC combat sheets (slash, thrust, halfslash, backslash, "
                            "shoot, spellcast) with mode 'sheet' instead."
                        )
                    else:
                        raise SystemExit(f'Unsupported animation mode: {mode}')
                    key = f'{animation}_{direction}'
                    char_animations[key] = final_frames
                    save_strip(final_frames, textures / f'{key}_128.png')

            tres = make_sprite_frames_tres(render_style, gender, char_dir)
            (char_dir / f'{style.key}_{gender}_sprite_frames.tres').write_text(tres, encoding='utf-8')
            (char_dir / f'{style.key}_{gender}.tscn').write_text(make_scene(render_style, gender), encoding='utf-8')
            metadata = {
                'lineage': render_style.name,
                'lineage_key': style.key,
                'body_variant': gender,
                'principle': render_style.principle,
                'visual_translation': render_style.visual,
                'frame_size': [GODOT_FRAME, GODOT_FRAME],
                'baseline': BASELINE,
                'animations': manifest['animations'],
                'directions': list(DIRECTIONS),
                'recipe_snapshot': {
                    'colors': {'primary': render_style.primary, 'secondary': render_style.secondary, 'accent': render_style.accent, 'skin': render_style.skin, 'hair': render_style.hair},
                    'profiles': {'symbol': render_style.symbol, 'accessories': render_style.accessory_profile, 'back': render_style.back_profile, 'female_hair_back': render_style.female_hair_back_profile, 'weapon': render_style.weapon_profile},
                },
                'source': 'Bundled LPC body snapshots plus deterministic World of Goses procedural overlays',
                'generator_recipe': 'res://source/recipes/lineages.json',
                'credits': 'res://docs/licenses/LPC_SELECTED_CREDITS.csv',
            }
            (char_dir / 'metadata.json').write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding='utf-8')
            preview_key = 'idle_down' if 'idle_down' in char_animations else next(iter(char_animations))
            previews[(style.key, gender)] = char_animations[preview_key][0]
            manifest['characters'].append({
                'lineage': style.key,
                'body_variant': gender,
                'scene': f'res://assets/characters/lineages/{style.key}/{gender}/{style.key}_{gender}.tscn',
                'sprite_frames': f'res://assets/characters/lineages/{style.key}/{gender}/{style.key}_{gender}_sprite_frames.tres',
            })

    write_csharp()
    write_docs(manifest)
    license_dir = OUT_ROOT / 'docs' / 'licenses'
    license_dir.mkdir(parents=True, exist_ok=True)
    shutil.copy2(VENDOR_DIR / 'LPC_FULL_CREDITS.csv', license_dir / 'LPC_FULL_CREDITS.csv')
    shutil.copy2(VENDOR_DIR / 'GENERATOR_GPL-3.0.txt', license_dir / 'GENERATOR_GPL-3.0.txt')
    write_selected_credits(license_dir / 'LPC_SELECTED_CREDITS.csv')
    copy_reproducible_sources()

    preview_dir = OUT_ROOT / 'previews'
    build_contact_sheet(previews, preview_dir / 'ALL_LINEAGES_CONTACT_SHEET.png')
    validate_generated_pngs()

    hashes = {}
    for path in sorted(OUT_ROOT.rglob('*')):
        if path.is_file() and path.name != 'SHA256SUMS.json':
            hashes[str(path.relative_to(OUT_ROOT)).replace('\\', '/')] = sha256(path)
    (OUT_ROOT / 'SHA256SUMS.json').write_text(json.dumps(hashes, indent=2), encoding='utf-8')

    if ZIP_PATH is not None:
        ZIP_PATH.parent.mkdir(parents=True, exist_ok=True)
        if ZIP_PATH.exists():
            ZIP_PATH.unlink()
        with zipfile.ZipFile(ZIP_PATH, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
            for path in sorted(OUT_ROOT.rglob('*')):
                if path.is_file():
                    zf.write(path, path.relative_to(OUT_ROOT.parent))

    print(f'Generated {OUT_ROOT}')
    if ZIP_PATH is not None:
        print(f'ZIP {ZIP_PATH} ({ZIP_PATH.stat().st_size:,} bytes)')


def main() -> None:
    global LINEAGES, OUT_ROOT, ZIP_PATH, SELECTED_GENDERS, WEAPON_RECIPES
    args = parse_args()
    configure_build(load_json(args.build_config.resolve()))
    recipe = load_json(args.recipes.resolve())
    apply_overrides(recipe, args.overrides)
    WEAPON_RECIPES = recipe.get('weapons', {})
    LINEAGES = load_lineages(recipe, set(args.lineage) or None)
    SELECTED_GENDERS = tuple(dict.fromkeys(args.gender)) if args.gender else ('male', 'female')
    OUT_ROOT = args.output.resolve()
    ZIP_PATH = None if args.no_zip else args.zip_path.resolve()
    generate()

if __name__ == '__main__':
    main()

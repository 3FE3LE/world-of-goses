# Modular Character Generator Feasibility

**Date:** 2026-07-23
**Status:** technical report for review
**Scope:** sprites, animations, clothing, armor, weapons, equipment, accessories, recipes, and Godot 4 export

> This document explicitly distinguishes **Verified facts**, **Inferences**, and **Recommendations**. It does not propose implementing the complete refactor yet.

## 1. Executive summary

**Verified facts**

- The current generator is not a modular equipment compositor. It starts from LPC body sheets, applies recoloring and hardcoded procedural overlays, and flattens the result into final PNGs.
- The source uses `64×64` cells. Each frame is placed without scaling on a `128×128` canvas at `(32,64)`. The body is not enlarged to 128 pixels; it occupies approximately the original LPC area in the lower half of the canvas.
- The integrated runtime contains 14 animations × 4 directions for 8 lineages × 2 bodies. Each scene contains one `AnimatedSprite2D` with `centered=true`, Nearest filtering, and offset `(0,-62)`.
- The 128-pixel canvas leaves room for weapons and accessories, but presentation code also assumes that size in AtlasTexture regions, slots, hitboxes, visual speed, and anchors.
- Current combat weapons are baked into the LPC sheets. The old procedural sword is retired, and weapon recipes do not actually replace those pixels.
- The archived copy under `art/.../assets` is outdated and contains only idle/walk/slash. The authoritative runtime is `game/assets/characters/lineages/`.

**Inferences**

- The generator can evolve into a modular layer system without being replaced, but geometry, the asset catalog, recipes, validation, and composition must first be separated.
- A global layer order is enough for passive pieces. Weapons, shields, long capes, and long hair require back/front partitions and rules by animation and direction; some pieces will need per-frame offsets or pivots.
- Shrinking the canvas before modularization would increase risk for little value. Saving transparent space does not justify the migration cost or clipping/jitter risk.

**Primary recommendation**

Adopt a **hybrid, backward-compatible architecture**:

1. Initially preserve the current `128×128` canvas, baseline, offset, animation names, and Godot export.
2. Precompose final PNGs on demand from normalized recipes and cache them by hash.
3. Keep runtime composition only for a small subset of frequently changing layers or immediate feedback, if the POC proves its cost and synchronization acceptable.
4. Do not generate every possible combination in advance.

## 2. Scope and exclusions

This report covers:

- LPC bodies and compatible body types;
- front/back hair;
- torso, legs, footwear, gloves, and armor;
- helmets, hats, masks, and pauldrons;
- capes, backpacks, and rear accessories;
- weapons, shields, and tools;
- cultural accessories and professional variants;
- wounds, bandages, and visible equipment wear;
- recipes, validation, caching, and PNG/Godot export.

Terrain, tilesets, buildings, UI, iconography, and environmental effects are excluded.

## 3. Current state

### 3.1 Inputs and recipes

**Fact.** The package is located at:

```text
art/world-of-goses-lpc-lineages-reproducible-v2/
```

Its primary inputs are:

```text
source/generate_lineage_sprites.py
source/recipes/build.json
source/recipes/lineages.json
source/recipes/build.schema.json
source/recipes/lineages.schema.json
source/lpc_bases/
source/reference/06_LINEAGES.md
```

`lineages.json` describes eight lineages, palettes, and cultural profiles. It allows `variants.male` and `variants.female` overrides, but it does not define a generic external-clothing catalog.

Currently recognized profiles are:

- `accessories`: one procedural implementation per lineage;
- `back`: `none`, `vine`, or `mantle`;
- `female_hair_back`: `bun`, `braid`, `mechanical_ponytail`, or `long_locks`;
- `weapon`: references an entry in `weapons`, currently only `sword`.

**Fact.** The schemas do not describe every field consumed by the generator, and the code does not execute JSON Schema validation. Effective validation is partial and procedural.

**Recommendation.** Before expanding the catalog, make recipes and schemas real, versioned contracts validated in CI.

### 3.2 Animations

`build.json` declares 14 animations:

| Animation | Frames | FPS | Loop | Mode |
|---|---:|---:|:---:|---|
| idle | 2 | 3 | yes | sheet |
| combat_idle | 2 | 4 | yes | sheet |
| walk | 9 | 9 | yes | sheet |
| run | 8 | 12 | yes | sheet |
| jump | 5 | 9 | no | sheet |
| climb | 6 | 6 | yes | sheet_mirror |
| sit | 3 | 3 | yes | sheet |
| hurt | 6 | 9 | no | sheet_mirror |
| slash | 6 | 11 | no | sheet |
| thrust | 8 | 12 | no | sheet |
| halfslash | 6 | 11 | no | sheet |
| backslash | 13 | 14 | no | sheet |
| shoot | 13 | 14 | no | sheet |
| spellcast | 7 | 10 | no | sheet |

Directions are `down`, `left`, `up`, and `right`. `climb` and `hurt` use one row: down/up receive the original and left/right receive a mirrored version. Female idle uses walk columns 0 and 4 as a fallback.

**Fact.** Runtime `SpriteFrames` contain every animation, although the audited `LineageSpritePlayer` adapter directly exposed only idle, walk, and slash. This is an adapter limitation, not missing art.

### 3.3 Per-frame composition

The effective order is:

```text
back hair
back accessory (mantle/vine)
recolored LPC body
head
front hair
cultural accessories
```

The result is flattened into one RGBA image.

**Fact.** Cultural overlays use drawing primitives with literal coordinates in `64×64` space. There are no clothing files by slot, pivots, masks, or declarative depth rules.

**Fact.** `recolor_body()` processes every nontransparent pixel. On combat sheets this includes baked weapons. Procedural sword functions are disabled/no-op.

**Consequence.** Changing `weapon` does not currently change the visible weapon. Modular weapons require separate body pose, hand, weapon back/front, and optionally trail layers.

### 3.4 Outputs

For each lineage/body, the build generates:

```text
56 PNG strips (14 animations × 4 directions)
1 SpriteFrames .tres
1 AnimatedSprite2D .tscn
1 metadata.json
```

Output also includes a manifest, documentation, licenses, reproducible sources, contact sheet, hashes, and an optional ZIP.

**Fact.** The output directory is deleted and recreated for every build. `SHA256SUMS.json` is a post-build inventory, not an incremental cache.

### 3.5 Godot dependency

Runtime integration uses:

- `CharacterVisualRegistry`: resolves exactly 8 lineages × 2 bodies under `res://assets/characters/lineages/`;
- `LineageSpritePlayer`: selects directional animations;
- `CitizenSpriteCarrier` and `CitizenSpriteBank`: instantiate, share, and move the visual representation;
- hosts such as `VisibleWorkerSlots`, `HeroProfileView`, and `MacroCitizenActivity`.

Character scenes contain no collision nodes, shadows, Y-sort, or physics. Visual order depends on the host tree. This lowers physical migration cost but does not remove positioning and clipping dependencies.

## 4. Canvas analysis

### 4.1 Is 64 actually scaled to 128?

**Fact. No.** `normalize_128()` creates a 128×128 canvas and places the 64×64 source at:

```text
x = (128 - 64) / 2 = 32
y = 128 - 64       = 64
```

Approximate representation:

```text
128×128 canvas
┌──────────────────────────────────────┐ y=0
│                                      │
│          transparent space           │
│                                      │
├──────────┌──────────────────┐────────┤ y=64
│          │ 64×64 LPC frame  │         │
│          │ body/weapon      │         │
│          │                  │         │
│          └──────────────────┘         │ y=127
└──────────────────────────────────────┘
           x=32              x=95
```

There is no antialiasing or fractional scaling. The sprite preserves LPC resolution.

### 4.2 Baseline and offset

`build.json` declares:

```text
baseline: [64,126]
scene_offset: [0,-62]
```

**Fact.** The baseline is written to metadata/documentation; it does not control raster placement. The scene uses `centered=true` and offset `-62`. Together, the carrier origin behaves approximately as a foot point rather than the canvas visual center.

**Inference.** Bottom-aligned content, baseline 126, and offset -62 intentionally correspond, but are expressed as separate constants. They should become one validated contract to prevent jitter.

### 4.3 Required transparent space

Top and side margins can contain:

- oversized swords, spears, bows, or hammers;
- swings, trails, and diagonal attacks;
- capes, long hair, and backpacks;
- future carry, victory, teleport, or other poses.

Not every animation needs the full margin, but a shared canvas simplifies pivots, animation switching, and AtlasTexture generation.

### 4.4 Where 128 is hardcoded

| Area | Dependency |
|---|---|
| Generator | 64→128 guard, normalization, strips, and PNG validation |
| `build.json` | `output_frame_size`, baseline, and scene offset |
| `.tres` | 128×128 AtlasTexture regions and X steps in multiples of 128 |
| `.tscn` | `centered`, offset, and filtering |
| Carrier | visual speed of 128 px/s associated with the current cycle |
| Slots | centers, height, entry/exit, and clipping based on 128 |
| Macro | manual 128×128 hitbox and anchors |
| Profile | 128-pixel-high visual host |
| Registry | size-independent while paths/names remain stable |
| Collision/shadow/Y-sort | currently absent from character scenes |

### 4.5 What a change would break

An uncoordinated canvas change can cause:

- incorrect AtlasTexture regions;
- clipped weapons/capes;
- vertical jumps when animations change;
- feet outside the anchor;
- overlapping labels;
- miscentered visual hitboxes;
- inconsistent movement speed/duration;
- blurred images if Nearest filtering is lost.

Registry paths remain valid if names and folders are preserved. There is no character physics to recalculate.

### 4.6 Compact, standard, and extended profiles

| Profile | Feasibility | Assessment |
|---|---|---|
| compact 64×96 | technically possible after refactor | high side/top clipping risk; little weapon margin |
| standard 96×96 | technically possible after refactor | viable for passive poses, but needs new baseline/offset and animation audit |
| extended 128×128 | current | safe, compatible, and extensible |

**Recommendation.** Keep `extended 128×128` as the public contract during early phases. Make `source_frame_size`, `output_frame_size`, baseline, and placement internally configurable, but do not promote alternate profiles until automatic bounds and no-clipping tests exist.

### 4.7 Different canvas by animation

Godot can store frames with different AtlasTexture sizes, but bounds and center changes complicate:

- stable pivots;
- cross-layer synchronization;
- hitboxes and clipping;
- runtime composition;
- visual validation;
- caching and modding.

**Recommendation.** Do not use animation-specific sizes in the first modular architecture. If future optimization requires it, retain a uniform logical 128×128 canvas and allow physical trimming/packing that preserves origin and pivot metadata.

## 5. Layer-system feasibility

| Goal | Classification | Reason |
|---|---|---|
| Skin color | feasible now | recoloring exists, though baked equipment must be separated |
| Short hair | moderate refactor | procedural implementation exists; needs catalog and formal recipe |
| Long hair | large refactor | needs back/front and pose rules |
| Torso/clothing | moderate refactor | LPC has layers, but generator does not consume them separately |
| Trousers | moderate refactor | mostly passive; body variants matter |
| Boots | moderate refactor | passive, but must preserve baseline and pose-specific feet |
| Gloves | large refactor | hand/weapon and attack occlusion |
| Helmet/hat | moderate refactor | mostly passive, with hair hiding |
| Mask | moderate refactor | passive, compatible with face/helmet |
| Pauldrons | large refactor | arms and attack occlusion |
| Short cape | moderate refactor | simple back/front by direction |
| Long cape | large refactor | pose, direction, and attack deformation |
| Backpack | moderate refactor | back layer, possibly hidden from the front |
| Weapon | large refactor | body pose, hand, back/front, and weapon-specific animation |
| Shield | large refactor | offhand, occlusion, and additional poses |
| Tools | large refactor | require compatible use animations, not merely an overlay |
| Cultural accessory | feasible now/moderate | procedural implementation exists; lineage must be decoupled from profession |
| Professional variant | moderate refactor | outfit recipe, not lineage bonus or predetermined career |
| Wounds/bandages | moderate refactor | passive overlays for simple cases |
| Worn equipment | moderate/large refactor | palette/mask or damage-specific sprites |
| Variable canvas by animation | initially discouraged | complexity without sufficient value |
| Thousands of pregenerated combinations | discouraged | combinatorial explosion and oversized repository |

## 6. Proposed architecture

### 6.1 Principles

1. The body pose is authoritative for each animation/frame.
2. Every piece declares compatibility, coverage, and licensing.
3. Baseline and pivot are stable integer contracts.
4. Layers split into back/front only when necessary.
5. Exceptional rules are expressed by animation/direction, not hardcoded lineage conditionals.
6. Lineage, profession, and equipment remain independent dimensions.
7. The generator remains compatible with current recipes.

### 6.2 Recommended slots

Proposed default order:

```text
back_fx
weapon_back
offhand_back
backpack
cape_back
hair_back
body
legs
feet
torso
belt
arms
gloves
shoulders_back
head
face
hair_front
helmet
mask
shoulders_front
lineage_accessory_back
offhand_front
weapon_front
lineage_accessory_front
cape_front
front_fx
```

This must not become one inflexible list. Use:

- a default global order;
- direction overrides;
- animation overrides;
- pieces split into back/front sublayers;
- per-frame rules only as a last resort.

For example, a shield may be in front during `idle_down` and behind during `idle_up`; a sword may cross back/front during slash; a long cape may require animation-specific source art.

### 6.3 Pivots, offsets, and occlusion

Every piece may declare:

- `pivot`: logical attachment point;
- `offset`: integer correction by animation/direction/frame;
- `hide_slots`: hair hidden by a helmet;
- `requires`: torso required by pauldrons;
- `conflicts`: cape incompatible with backpack;
- `occlusion_mask`: optional, only where back/front splitting is insufficient.

**Recommendation.** Prefer correctly aligned assets and back/front splits. Use masks and per-frame offsets exceptionally because they multiply authoring cost.

## 7. Proposed equipment format

```json
{
  "schema_version": 1,
  "id": "helmet.iron_guard.01",
  "display_name": "Iron Guard Helmet",
  "slot": "helmet",
  "compatible_bodies": ["male", "female"],
  "source_license": {
    "license": "CC-BY-SA-3.0",
    "author": "Example Author",
    "source": "https://example.invalid/item"
  },
  "palette": {
    "metal_dark": "#343A40",
    "metal": "#69737D",
    "highlight": "#B8C3CB"
  },
  "layers": {
    "back": null,
    "front": "helmet_front"
  },
  "animations": {
    "idle": {
      "source": "idle.png",
      "frames": 2,
      "directions": ["down", "left", "up", "right"]
    },
    "walk": {
      "source": "walk.png",
      "frames": 9,
      "directions": ["down", "left", "up", "right"]
    },
    "slash": {
      "fallback": "walk_pose_map",
      "requires_body_animation": "slash"
    }
  },
  "anchors": {
    "default": [32, 62]
  },
  "offsets": {
    "idle.down": [0, 0]
  },
  "layer_rules": {
    "default": ["helmet_front"],
    "idle.up": ["helmet_front"]
  },
  "hide_slots": ["hair_front"],
  "requires": [],
  "conflicts": ["helmet.heavy_hood"],
  "tags": ["armor", "guard"]
}
```

Required fields:

- version, ID, slot, and body compatibility;
- license/credits;
- animations, sources, frames, and directions;
- explicit fallback;
- layers and pivot.

Optional fields:

- recolorable palette;
- offsets;
- dependencies/incompatibilities;
- hiding;
- depth rules;
- required body animation.

A fallback must never invent an incompatible pose. A helmet can reuse walk art during a similar animation; a hammer cannot reuse a sword slash when the hand and attack arc do not match.

## 8. Weapons and animations

### 8.1 Classification

**Passive:** helmet, mask, necklace, insignia, bracelet, small backpack, and short hair. These can follow the head/body where common alignment exists.

**Dependent:** sword, spear, hammer, bow, shield, long cape, wide skirt, very long hair, and large tools. These need dedicated sprites or pose rules.

### 8.2 Hand–weapon–pose synchronization

A robust representation separates:

```text
weapon_back
body pose
hand/body overlay
weapon_front
attack_trail
```

The weapon recipe must declare its required body animation:

| Weapon | Body animation |
|---|---|
| sword | slash/halfslash/backslash depending on attack |
| spear | thrust |
| hammer | dedicated impact or compatible heavy attack |
| bow | shoot |
| shield | idle/walk plus defensive/offhand poses |
| tool | specific carry/use when hands or torso change |

**Recommendation.** Do not simply draw weapons above every frame. For the POC, use weapons whose sheets already match slash; postpone hammer/shield until their poses are defined.

## 9. LPC versus original assets

| Component | LPC | Recommended for World of Goses |
|---|---|---|
| base body | yes | original variants only where LPC lacks the body type |
| base animations | yes | new poses when gameplay requires them |
| common hair | yes | original culture-specific variants |
| common clothing | yes, after compatibility indexing | original professions and silhouettes |
| armor/helmets | partial | identity- and profession-specific designs |
| weapons/shields | depending on layout | original world-specific tools and weapons |
| lineage accessories | not as final identity | original World of Goses assets |
| wounds/bandages/wear | variable coverage | preferably original |

LPC assets can differ across legacy/current sets, bodies, layouts, frame counts, and oversized assets. Before adding a broad catalog, index:

- stable ID;
- layout/cell size;
- available animations;
- compatible bodies;
- frames/directions;
- license and credits;
- back/front layers;
- checksums.

Do not download or integrate thousands of files before building this index and its validators.

## 10. Composition strategies

### 10.1 Precomposition

**Advantages:** few draw calls, simple and deterministic rendering, current-runtime compatibility, straightforward visual validation.

**Disadvantages:** regeneration after equipment changes, many textures if everything is baked, large imports/repository.

### 10.2 Runtime composition

**Advantages:** instant changes, flexible dyes/modding, no need to bake every combination.

**Disadvantages:** multiple `AnimatedSprite2D` nodes, animation/frame synchronization, more draw calls, complex back/front ordering, more memory/nodes for hundreds of citizens, difficult debugging.

### 10.3 Recommended hybrid

- Keep modular sources and recipes.
- Precompose only appearances actually in use.
- Cache by normalized hash.
- Regenerate when recipe, asset, or generator version changes.
- Use runtime overlays only for small or temporary layers where value justifies draw calls.
- Prefer precomposed sprites in mass views.
- Allow more detail/expedition layers only after measurement.
- Generate portraits from the same recipe through a separate export profile.

This avoids 15,000 speculative combinations while retaining reasonable performance for a persistent city.

## 11. Recipe cache

Normalized recipe:

```json
{
  "lineage": "ardhen",
  "body": "male",
  "skin": "skin_04",
  "hair_back": "hair_braid_02",
  "torso": "miner_vest_01",
  "legs": "work_trousers_01",
  "helmet": "iron_guard_01",
  "weapon": "hammer_02",
  "back": "rope_pack_01"
}
```

Suggested key:

```text
sha256(
  generator_version
  + canonical_recipe_json
  + build_profile
  + source_asset_hashes
)
```

Result:

```text
ardhen_male_a914c8...
```

The cache should store:

- final PNGs;
- `.tres`/`.tscn` when applicable;
- normalized recipe;
- generator version;
- source hashes;
- aggregated credits;
- informational timestamp, excluded from the hash.

`SHA256SUMS.json` can remain as package auditing, but does not replace an incremental index.

## 12. Incremental roadmap

### Phase 0 — Audit and report

- **Files:** this document.
- **Risk:** low.
- **Result:** decisions and bounded POC.
- **Tests:** technical review against code/assets.
- **Compatibility:** complete.

### Phase 1 — Contracts, schemas, and documentation debt

- **Files:** recipes/schemas, validators, generated documentation.
- **Risk:** low–medium.
- **Result:** validated current configuration; archive/runtime clearly distinguished.
- **Tests:** valid/invalid fixtures, 14 animations, licenses.
- **Compatibility:** load v1 recipes unchanged.

### Phase 2 — Parameterized canvas and primitives

- **Files:** generator, geometry/placement module, build profile.
- **Risk:** medium.
- **Result:** visually identical 128 output without scattered guards.
- **Tests:** pixel diff against baseline, bounds, baseline, alpha, Nearest.
- **Compatibility:** byte/pixel-compatible current output where possible.

### Phase 3 — Slots and first passive pieces

- **Files:** equipment schemas/catalog, compositor, validators.
- **Risk:** medium.
- **Result:** two interchangeable helmets and two torsos.
- **Tests:** combinations, hair hiding, bodies, four directions.
- **Compatibility:** old recipes translate to a legacy outfit.

### Phase 4 — Weapons and dynamic depth

- **Files:** back/front compositor, weapon recipes, pose mapping.
- **Risk:** high.
- **Result:** two slash-compatible weapons without incorrect occlusion.
- **Tests:** frame-by-frame, hand/pivot, left/right, no jitter.
- **Compatibility:** current baked weapons remain available as legacy.

### Phase 5 — Animations and expanded catalog

- **Files:** indexed LPC sources, original assets, build config/licenses.
- **Risk:** high.
- **Result:** controlled addition of carry/hit/injured/victory/retreat where valid poses exist.
- **Tests:** frame count, layout, bodies, licenses, clipping.
- **Compatibility:** current 14 animations remain stable.

### Phase 6 — Cache and Godot integration

- **Files:** generator cache, manifest, registry/visual loading only if the POC requires it.
- **Risk:** medium–high.
- **Result:** generate/reuse only appearances in use.
- **Tests:** hit/miss/invalidation, Godot loading, memory, draw calls, mass views.
- **Compatibility:** current `CharacterVisualRegistry` and scenes continue to work.

### Phase 7 — Expanded catalog and modding

- **Files:** indexes, content packages, validation tooling.
- **Risk:** medium.
- **Result:** gradual addition of LPC/original assets without importing the entire ecosystem.
- **Tests:** package isolation, credits, conflicts, versions.
- **Compatibility:** versioned, migratable recipes.

## 13. Recommended minimum POC

Goal: prove modularity without changing gameplay or promoting assets into `game/assets`.

### Scope

```text
1 body: existing Ardhen male
2 helmets
2 torsos
2 slash-compatible weapons
animations: idle, walk, slash
directions: down, left, up, right
output: PNG strips + SpriteFrames + test Godot scene
canvas: 128×128
baseline: [64,126]
offset: [0,-62]
```

### Success criteria

- The unequipped body matches the approved legacy output pixel for pixel.
- Changing helmet/torso does not change baseline or cause jitter.
- A recipe can hide hair under a helmet.
- Torso and helmet work in all three animations and four directions.
- Weapons preserve hand alignment and depth during slash.
- No frame touches the canvas edge without a documented exception.
- The generator emits a normalized recipe and stable hash.
- A second execution reuses the cache.
- Output opens in Godot with Nearest filtering and plays every animation.

### Outside the POC

- shields;
- hammers and bows when they require new poses;
- alternate canvases;
- thousands of LPC pieces;
- mass runtime layering;
- gameplay changes.

Generate the POC under `dist/` or a temporary directory and review it before promotion.

## 14. Identified risks and debt

1. **Outdated archive.** `art/.../assets` has only three animations and a player with a different namespace/API. Do not copy it over `game/`.
2. **Partial runtime API.** Resources contain 14 animations, but the audited adapter did not expose all of them.
3. **Incomplete schemas.** They can accept configurations interpreted unexpectedly by the generator.
4. **Baked weapons.** Recoloring and modular separation are unsafe until body/equipment are distinguished.
5. **Silent fallbacks.** Some unknown profiles fall back to defaults instead of failing.
6. **Licensing.** A modular catalog must aggregate credits from every layer, not only the body.
7. **Combinatorial explosion.** Without on-demand caching, repository growth is unnecessary.

## 15. Final assessment

### Is evolution toward thousands of combinations viable?

Yes, if combinations are recipes generated and cached on demand. Baking all of them in advance is neither viable nor recommended.

### Which layer architecture should be used?

Declarative slots, a default global order, back/front splits for complex pieces, animation/direction overrides, and integer pivots. Use per-frame masks/offsets only where clean separation cannot solve the asset.

### Precomposition, runtime, or hybrid?

Hybrid, dominated by cached precomposition for persistent citizens, with selective runtime layers only where they deliver demonstrated value.

### What must change for clothing, helmets, and weapons?

Schemas and validation; asset catalog; slot compositor; parameterized geometry; body compatibility; back/front depth; pivots; weapon pose mapping; caching/invalidation; aggregated licensing.

### What should happen to 128×128?

Keep it as the initial public contract. Make it internally configurable only after visual equivalence and bounds tests exist. Do not use animation-specific canvases in the first version.

### Which parts of the game can break?

Atlas regions, offsets, slots, clipping, anchors, hitboxes, labels, and visual speed. Registry paths remain valid if names are preserved. There is currently no character physics, shadow, or Y-sort to migrate.

### What is the smallest POC?

One Ardhen male, two helmets, two torsos, and two weapons, limited to idle/walk/slash in four directions, exported outside `game/assets` using the current canvas/baseline and pixel-perfect visual validation.

## 16. References

- `art/world-of-goses-lpc-lineages-reproducible-v2/README_GENERATOR.md`
- `art/world-of-goses-lpc-lineages-reproducible-v2/IMPORT_GODOT4.md`
- `art/world-of-goses-lpc-lineages-reproducible-v2/source/generate_lineage_sprites.py`
- `art/world-of-goses-lpc-lineages-reproducible-v2/source/recipes/build.json`
- `art/world-of-goses-lpc-lineages-reproducible-v2/source/recipes/lineages.json`
- `art/world-of-goses-lpc-lineages-reproducible-v2/source/reference/06_LINEAGES.md`
- `art/world-of-goses-lpc-lineages-reproducible-v2/docs/LICENSING_AND_ATTRIBUTION.md`
- `game/scripts/visual/LineageSpritePlayer.cs`
- `game/scripts/visual/CharacterVisualRegistry.cs`
- `game/scripts/CitizenSpriteCarrier.cs`
- `game/scripts/CitizenSpriteBank.cs`
- `docs/ART_PIPELINE.md`
- `docs/ASSET_INVENTORY.md`
- [Universal LPC Spritesheet Character Generator](https://liberatedpixelcup.github.io/Universal-LPC-Spritesheet-Character-Generator/)

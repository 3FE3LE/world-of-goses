#!/usr/bin/env node
// OBSOLETE — kept for historical reference only.
//
// This script used to generate the first batch of placeholder pixel
// art for the World of Goses prototype. The PNGs it produced
// (`worker_placeholder.png` and `mine_placeholder.png`) have been
// removed; they were replaced by hand-authored placeholders for the
// Home, Quarry, and Farm building PNGs that now live in
// `art/exports/buildings/`.
//
// Running this script will overwrite nothing important (those files
// no longer exist), but it will recreate the obsolete PNGs at
// incompatible canvas sizes. Do not run it. Delete this file once
// the team is confident no agent or CI will resurrect it.
//
// If you genuinely need to regenerate placeholder PNGs, write a new
// script that emits them into the current canonical paths
// (`art/exports/buildings/home_idle.png`, `quarry_idle.png`,
// `farm_idle.png`) and update `game/scripts/BuildingArt.cs` to match.
// art/source/buildings/ and re-exporting at the same dimensions.

'use strict';

const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

const ROOT = path.resolve(__dirname, '..');

function crc32Table() {
  const table = new Uint32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) {
      c = (c & 1) ? (0xedb88320 ^ (c >>> 1)) : (c >>> 1);
    }
    table[n] = c >>> 0;
  }
  return table;
}
const CRC_TABLE = crc32Table();

function crc32(buf) {
  let c = 0xffffffff;
  for (let i = 0; i < buf.length; i++) {
    c = CRC_TABLE[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
  }
  return (c ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const length = Buffer.alloc(4);
  length.writeUInt32BE(data.length, 0);
  const typeBuf = Buffer.from(type, 'ascii');
  const crcInput = Buffer.concat([typeBuf, data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(crcInput), 0);
  return Buffer.concat([length, typeBuf, data, crc]);
}

// Writes an indexed-color PNG. `pixels` is a Uint8Array of palette
// indices (one byte per pixel, no per-row filter byte — caller passes
// raw index data and the function adds the filter byte per row).
function writeIndexedPng(filePath, width, height, palette, indices) {
  // Palette: array of [r, g, b] triples.
  const plte = Buffer.alloc(palette.length * 3);
  for (let i = 0; i < palette.length; i++) {
    plte[i * 3 + 0] = palette[i][0];
    plte[i * 3 + 1] = palette[i][1];
    plte[i * 3 + 2] = palette[i][2];
  }

  // Add a per-row filter byte (0 = None) before deflate.
  const stride = width;
  const filtered = Buffer.alloc(height * (stride + 1));
  for (let y = 0; y < height; y++) {
    filtered[y * (stride + 1)] = 0;
    for (let x = 0; x < stride; x++) {
      filtered[y * (stride + 1) + 1 + x] = indices[y * stride + x];
    }
  }
  const compressed = zlib.deflateSync(filtered, { level: 9 });

  const sig = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);

  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8;   // bit depth
  ihdr[9] = 3;   // color type: indexed
  ihdr[10] = 0;  // compression
  ihdr[11] = 0;  // filter
  ihdr[12] = 0;  // interlace

  const out = Buffer.concat([
    sig,
    chunk('IHDR', ihdr),
    chunk('PLTE', plte),
    chunk('IDAT', compressed),
    chunk('IEND', Buffer.alloc(0)),
  ]);
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, out);
  console.log(`wrote ${filePath} (${width}x${height}, ${out.length} bytes)`);
}

// Palette indices used by the placeholder art. Real Pixelorama sources
// can define their own palettes; these constants only document what
// each index means for the generated placeholders.
const PAL = {
  transparent: 0,
  shadow:      1,
  pitDark:     2,
  pitMid:      3,
  earthDark:   4,
  earthMid:    5,
  earthLight:  6,
  stoneDark:   7,
  stoneMid:    8,
  stoneLight:  9,
  woodDark:   10,
  woodMid:    11,
  woodLight:  12,
  skin:       13,
  hair:       14,
  clothDark:  15,
  clothMid:   16,
  clothLight: 17,
  highlight:  18,
};

const WORKER_PALETTE = [
  [0,   0,   0],    // 0  transparent
  [10,  10,  10],   // 1  shadow
  [35,  25,  18],   // 2  pit dark
  [50,  40,  30],   // 3  pit mid
  [80,  55,  35],   // 4  earth dark
  [120, 85,  50],   // 5  earth mid
  [160, 120, 70],   // 6  earth light
  [70,  70,  80],   // 7  stone dark
  [110, 110, 120],  // 8  stone mid
  [170, 170, 180],  // 9  stone light
  [70,  45,  25],   // 10 wood dark
  [120, 80,  40],   // 11 wood mid
  [170, 120, 65],   // 12 wood light
  [220, 200, 170],  // 13 skin
  [90,  60,  40],   // 14 hair
  [55,  80,  55],   // 15 cloth dark
  [85,  120, 80],   // 16 cloth mid
  [120, 160, 105],  // 17 cloth light
  [240, 240, 235],  // 18 highlight
];

function buildWorkerPlaceholder() {
  const w = 64, h = 96;
  const idx = new Uint8Array(w * h);

  function set(x, y, p) {
    if (x < 0 || x >= w || y < 0 || y >= h) return;
    idx[y * w + x] = p;
  }
  function rect(x0, y0, x1, y1, p) {
    for (let y = y0; y < y1; y++) {
      for (let x = x0; x < x1; x++) {
        set(x, y, p);
      }
    }
  }

  // Background: transparent. The slot is a placeholder that will be
  // composited over the slot's Control backdrop.

  // Head (rounded-ish silhouette)
  const headCx = 32, headCy = 18, headR = 12;
  for (let y = 0; y < 32; y++) {
    for (let x = 0; x < w; x++) {
      const dx = x - headCx, dy = y - headCy;
      const d2 = dx * dx + dy * dy;
      if (d2 <= headR * headR) {
        set(x, y, PAL.skin);
      } else if (d2 <= (headR + 1) * (headR + 1)) {
        set(x, y, PAL.hair); // hair rim
      }
    }
  }
  // Hair cap
  rect(headCx - headR, headCy - headR, headCx + headR, headCy - 4, PAL.hair);

  // Eyes
  set(headCx - 4, headCy + 1, PAL.shadow);
  set(headCx + 4, headCy + 1, PAL.shadow);

  // Body (torso) — wide trapezoid using cloth
  for (let y = 32; y < 70; y++) {
    const inset = Math.floor((y - 32) * 0.18);
    rect(16 + inset, y, 48 - inset, y + 1, PAL.clothMid);
    // darker trim on sides
    set(16 + inset, y, PAL.clothDark);
    set(47 - inset, y, PAL.clothDark);
  }

  // Belt highlight
  rect(16, 60, 48, 62, PAL.clothLight);

  // Arms holding a pickaxe handle (diagonal wood)
  rect(20, 36, 24, 60, PAL.clothDark);
  rect(40, 36, 44, 60, PAL.clothDark);

  // Pickaxe handle (vertical wood on right side)
  rect(46, 40, 50, 70, PAL.woodMid);
  rect(46, 40, 50, 44, PAL.woodLight);
  // Pickaxe head (stone, horizontal)
  rect(44, 38, 56, 44, PAL.stoneMid);
  rect(44, 38, 56, 40, PAL.stoneLight);
  rect(44, 42, 56, 44, PAL.stoneDark);

  // Legs
  rect(20, 70, 28, 90, PAL.clothDark);
  rect(36, 70, 44, 90, PAL.clothDark);

  // Boots
  rect(18, 90, 30, 94, PAL.woodDark);
  rect(34, 90, 46, 94, PAL.woodDark);

  // Subtle shadow under feet
  rect(14, 94, 50, 96, PAL.shadow);

  return idx;
}

function buildMinePlaceholder() {
  const w = 192, h = 192;
  const idx = new Uint8Array(w * h);

  function set(x, y, p) {
    if (x < 0 || x >= w || y < 0 || y >= h) return;
    idx[y * w + x] = p;
  }
  function rect(x0, y0, x1, y1, p) {
    for (let y = y0; y < y1; y++) {
      for (let x = x0; x < x1; x++) set(x, y, p);
    }
  }

  // Ground baseline (earth strip at the bottom)
  rect(0, h - 32, w, h, PAL.earthMid);
  rect(0, h - 32, w, h - 24, PAL.earthLight);
  rect(0, h - 12, w, h - 8, PAL.earthDark);

  // Pit opening in the centre (quarry)
  const pitCx = 96, pitCy = 130, pitRx = 60, pitRy = 36;
  for (let y = pitCy - pitRy; y <= pitCy + pitRy; y++) {
    for (let x = pitCx - pitRx; x <= pitCx + pitRx; x++) {
      const dx = (x - pitCx) / pitRx;
      const dy = (y - pitCy) / pitRy;
      const d2 = dx * dx + dy * dy;
      if (d2 <= 1.0) {
        // Inside the pit, gradient from mid to dark toward bottom
        set(x, y, d2 > 0.55 ? PAL.pitMid : PAL.pitDark);
      }
    }
  }

  // Wooden support frame around the pit
  // Top beam
  rect(20, 90, 172, 100, PAL.woodMid);
  rect(20, 90, 172, 94, PAL.woodLight);
  rect(20, 98, 172, 100, PAL.woodDark);
  // Left post
  rect(20, 100, 30, 170, PAL.woodMid);
  rect(20, 100, 24, 170, PAL.woodLight);
  rect(26, 100, 30, 170, PAL.woodDark);
  // Right post
  rect(162, 100, 172, 170, PAL.woodMid);
  rect(162, 100, 166, 170, PAL.woodLight);
  rect(168, 100, 172, 170, PAL.woodDark);

  // Stone pile on the right of the pit
  for (let i = 0; i < 12; i++) {
    const sx = 110 + (i % 4) * 14;
    const sy = 156 + Math.floor(i / 4) * 10;
    rect(sx, sy, sx + 12, sy + 10, PAL.stoneMid);
    rect(sx, sy, sx + 12, sy + 3, PAL.stoneLight);
    rect(sx, sy + 8, sx + 12, sy + 10, PAL.stoneDark);
  }

  // Stone pile on the left
  for (let i = 0; i < 8; i++) {
    const sx = 28 + (i % 3) * 14;
    const sy = 158 + Math.floor(i / 3) * 10;
    rect(sx, sy, sx + 12, sy + 10, PAL.stoneMid);
    rect(sx, sy, sx + 12, sy + 3, PAL.stoneLight);
    rect(sx, sy + 8, sx + 12, sy + 10, PAL.stoneDark);
  }

  // Roof tiles across the top beam
  for (let i = 0; i < 16; i++) {
    rect(20 + i * 10, 84, 30 + i * 10, 90, PAL.woodDark);
    rect(20 + i * 10, 80, 30 + i * 10, 84, PAL.woodLight);
  }

  // Small lantern hanging from the top beam (right side)
  rect(120, 100, 132, 104, PAL.woodDark);
  rect(122, 104, 130, 116, PAL.stoneMid);
  rect(122, 104, 130, 108, PAL.stoneLight);
  rect(122, 114, 130, 116, PAL.stoneDark);
  set(125, 110, PAL.highlight);

  // A pickaxe leaning against the left post
  rect(34, 100, 36, 140, PAL.woodMid);
  rect(30, 96, 40, 102, PAL.stoneMid);
  rect(30, 96, 40, 98, PAL.stoneLight);

  return idx;
}

const workerPng = buildWorkerPlaceholder();
writeIndexedPng(
  path.join(ROOT, 'art/exports/characters/worker_placeholder.png'),
  64, 96, WORKER_PALETTE, workerPng);

const minePng = buildMinePlaceholder();
writeIndexedPng(
  path.join(ROOT, 'art/exports/buildings/mine_placeholder.png'),
  192, 192, WORKER_PALETTE, minePng);
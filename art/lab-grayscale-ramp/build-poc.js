// Laboratorio: prueba de concepto del LUT de rampa de paleta.
//
// No es parte del pipeline. Lee las hojas LPC ya compuestas (que se van a
// descartar) y comprueba dos cosas que no se pueden comprobar dibujando:
//
//   E1  Round-trip: ¿bastan 6 niveles de gris por zona para reconstruir el
//       original? Mide el error por píxel.
//   E2  Distinción: aplicando rampas construidas SOLO desde los 5 colores
//       declarados de cada linaje sobre UNA hoja en grises, ¿se siguen
//       distinguiendo los linajes?
//   E2c E2 con `skin` y `hair` bloqueados a la misma rampa en los tres
//       linajes, para aislar lo que consigue la prenda.
//
// La zona de cada píxel NO se adivina del color: se deriva del acuerdo entre
// tres linajes que comparten geometría y difieren en paleta. Es la versión
// medible del argumento "la zona viene de la identidad de la capa".
//
// Uso:  node art/lab-grayscale-ramp/build-poc.js

'use strict';

const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

const REPO = path.resolve(__dirname, '..', '..');
const OUT = __dirname;
const SHADES = 6;

// Zonas. 0 es compartida entre linajes (contorno, blanco de ojo): no la toca
// ninguna rampa de linaje.
const ZONES = ['neutral', 'skin', 'hair', 'primary', 'secondary', 'accent'];
const LINEAGE_ZONES = ['skin', 'hair', 'primary', 'secondary', 'accent'];

// ---------------------------------------------------------------- PNG

function crc32Table() {
  const t = new Int32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    t[n] = c;
  }
  return t;
}
const CRC_TABLE = crc32Table();

function crc32(buf) {
  let c = -1;
  for (let i = 0; i < buf.length; i++) c = CRC_TABLE[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
  return (c ^ -1) >>> 0;
}

function chunk(type, data) {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length, 0);
  const body = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(body), 0);
  return Buffer.concat([len, body, crc]);
}

function paeth(a, b, c) {
  const p = a + b - c;
  const pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
  return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
}

/** Decodifica PNG de 8 bits, color type 6 (RGBA). Devuelve {width,height,data}. */
function readPng(file) {
  const buf = fs.readFileSync(file);
  if (buf.readUInt32BE(0) !== 0x89504e47) throw new Error(`${file}: no es PNG`);
  let pos = 8, width = 0, height = 0, colorType = -1, depth = 0;
  const idat = [];
  while (pos < buf.length) {
    const len = buf.readUInt32BE(pos);
    const type = buf.toString('ascii', pos + 4, pos + 8);
    const data = buf.subarray(pos + 8, pos + 8 + len);
    if (type === 'IHDR') {
      width = data.readUInt32BE(0);
      height = data.readUInt32BE(4);
      depth = data[8];
      colorType = data[9];
    } else if (type === 'IDAT') idat.push(data);
    else if (type === 'IEND') break;
    pos += 12 + len;
  }
  if (depth !== 8 || colorType !== 6) {
    throw new Error(`${file}: se esperaba 8-bit RGBA, hay depth=${depth} type=${colorType}`);
  }
  const raw = zlib.inflateSync(Buffer.concat(idat));
  const bpp = 4, stride = width * bpp;
  const out = Buffer.alloc(stride * height);
  let rp = 0;
  for (let y = 0; y < height; y++) {
    const filter = raw[rp++];
    const row = raw.subarray(rp, rp + stride); rp += stride;
    const cur = out.subarray(y * stride, (y + 1) * stride);
    const prev = y > 0 ? out.subarray((y - 1) * stride, y * stride) : null;
    for (let x = 0; x < stride; x++) {
      const a = x >= bpp ? cur[x - bpp] : 0;
      const b = prev ? prev[x] : 0;
      const c = prev && x >= bpp ? prev[x - bpp] : 0;
      let v = row[x];
      if (filter === 1) v += a;
      else if (filter === 2) v += b;
      else if (filter === 3) v += (a + b) >> 1;
      else if (filter === 4) v += paeth(a, b, c);
      cur[x] = v & 0xff;
    }
  }
  return { width, height, data: out };
}

function writePng(file, width, height, data) {
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8; ihdr[9] = 6;
  const stride = width * 4;
  const filtered = Buffer.alloc((stride + 1) * height);
  for (let y = 0; y < height; y++) {
    filtered[y * (stride + 1)] = 0;
    data.copy(filtered, y * (stride + 1) + 1, y * stride, (y + 1) * stride);
  }
  fs.writeFileSync(file, Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', ihdr),
    chunk('IDAT', zlib.deflateSync(filtered, { level: 9 })),
    chunk('IEND', Buffer.alloc(0)),
  ]));
}

// ---------------------------------------------------------------- color

const hex = (h) => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];

function rgbToHsv(r, g, b) {
  r /= 255; g /= 255; b /= 255;
  const mx = Math.max(r, g, b), mn = Math.min(r, g, b), d = mx - mn;
  let h = 0;
  if (d > 1e-9) {
    if (mx === r) h = ((g - b) / d) % 6;
    else if (mx === g) h = (b - r) / d + 2;
    else h = (r - g) / d + 4;
    h *= 60; if (h < 0) h += 360;
  }
  return [h, mx < 1e-9 ? 0 : d / mx, mx];
}

function hsvToRgb(h, s, v) {
  h = ((h % 360) + 360) % 360;
  const c = v * s, x = c * (1 - Math.abs(((h / 60) % 2) - 1)), m = v - c;
  let r, g, b;
  if (h < 60) [r, g, b] = [c, x, 0];
  else if (h < 120) [r, g, b] = [x, c, 0];
  else if (h < 180) [r, g, b] = [0, c, x];
  else if (h < 240) [r, g, b] = [0, x, c];
  else if (h < 300) [r, g, b] = [x, 0, c];
  else [r, g, b] = [c, 0, x];
  return [Math.round((r + m) * 255), Math.round((g + m) * 255), Math.round((b + m) * 255)];
}

const luma = (r, g, b) => 0.299 * r + 0.587 * g + 0.114 * b;

/** Distancia de matiz/croma, invariante a luminosidad. Dos grises casan entre sí. */
function chromaDistance(a, bb) {
  const [h1, s1] = rgbToHsv(a[0], a[1], a[2]);
  const [h2, s2] = rgbToHsv(bb[0], bb[1], bb[2]);
  const ds = Math.abs(s1 - s2);
  if (s1 < 0.12 && s2 < 0.12) return ds;          // ambos casi neutros
  if (s1 < 0.12 || s2 < 0.12) return 1.0 + ds;    // uno neutro y otro no: mal casan
  let dh = Math.abs(h1 - h2); if (dh > 180) dh = 360 - dh;
  return (dh / 180) * 1.0 + ds * 0.45;
}

/** Rampa de 6 pasos desde un color base, con desplazamiento de matiz. */
function buildRamp(baseHex) {
  const [h, s, v] = rgbToHsv(...hex(baseHex));
  const out = [];
  for (let i = 0; i < SHADES; i++) {
    const t = i / (SHADES - 1);                       // 0 = sombra, 1 = luz
    const vv = v * (0.34 + t * (1.36 - 0.34));
    const ss = s * (1.18 - t * 0.44);
    const hh = h + (t - 0.5) * 16;                    // sombras frías, luces cálidas
    out.push(hsvToRgb(hh, Math.min(1, ss), Math.min(1, vv)));
  }
  return out;
}

// ---------------------------------------------------------------- datos

const recipe = JSON.parse(fs.readFileSync(
  path.join(REPO, 'art/world-of-goses-lpc-lineages-reproducible-v2/source/recipes/lineages.json'), 'utf8'));
const COLORS = {};
for (const l of recipe.lineages) COLORS[l.key] = l.colors;

const LINEAGES = ['caelith', 'eirune', 'kovari'];
const sheetPath = (lin, body) =>
  path.join(REPO, `game/assets/characters/lineages/${lin}/${body}/textures/idle_down_128.png`);

// ---------------------------------------------------------------- núcleo

function encode(body) {
  const imgs = LINEAGES.map((l) => readPng(sheetPath(l, body)));
  const { width, height } = imgs[0];
  for (const i of imgs) {
    if (i.width !== width || i.height !== height) throw new Error('hojas de tamaño distinto');
  }
  const n = width * height;
  const zone = new Uint8Array(n);
  const shade = new Uint8Array(n);
  const alpha = new Uint8Array(n);
  // Muestras por (zona, sombra) y linaje, para la rampa empírica de E1.
  const samples = {};
  for (const l of LINEAGES) {
    samples[l] = ZONES.map(() => Array.from({ length: SHADES }, () => [0, 0, 0, 0]));
  }

  const px = (img, i) => [img.data[i * 4], img.data[i * 4 + 1], img.data[i * 4 + 2]];
  const zoneOfPixel = [];

  for (let i = 0; i < n; i++) {
    const a = imgs[0].data[i * 4 + 3];
    alpha[i] = a;
    if (a === 0) { zoneOfPixel.push(-1); continue; }
    const cols = imgs.map((im) => px(im, i));
    const same = cols.every((c) => c[0] === cols[0][0] && c[1] === cols[0][1] && c[2] === cols[0][2]);
    if (same) { zone[i] = 0; zoneOfPixel.push(0); continue; }
    // Acuerdo entre los tres linajes: la zona que minimiza la distancia
    // simultánea en las tres paletas declaradas.
    let best = 1, bestScore = Infinity;
    for (let z = 1; z < ZONES.length; z++) {
      let s = 0;
      for (let k = 0; k < LINEAGES.length; k++) {
        s += chromaDistance(cols[k], hex(COLORS[LINEAGES[k]][ZONES[z]]));
      }
      if (s < bestScore) { bestScore = s; best = z; }
    }
    zone[i] = best;
    zoneOfPixel.push(best);
  }

  // Índice de sombra: rango de luminancia promedio dentro de cada zona.
  const lumByZone = ZONES.map(() => []);
  const avgLuma = new Float32Array(n);
  for (let i = 0; i < n; i++) {
    if (alpha[i] === 0) continue;
    let s = 0;
    for (const im of imgs) s += luma(...px(im, i));
    avgLuma[i] = s / imgs.length;
    lumByZone[zone[i]].push(avgLuma[i]);
  }
  const bounds = lumByZone.map((arr) => {
    if (!arr.length) return [0, 1];
    return [Math.min(...arr), Math.max(...arr)];
  });
  for (let i = 0; i < n; i++) {
    if (alpha[i] === 0) continue;
    const [lo, hi] = bounds[zone[i]];
    const t = hi - lo < 1e-6 ? 0 : (avgLuma[i] - lo) / (hi - lo);
    shade[i] = Math.min(SHADES - 1, Math.round(t * (SHADES - 1)));
    for (let k = 0; k < LINEAGES.length; k++) {
      const acc = samples[LINEAGES[k]][zone[i]][shade[i]];
      const c = px(imgs[k], i);
      acc[0] += c[0]; acc[1] += c[1]; acc[2] += c[2]; acc[3]++;
    }
  }

  // Rampa empírica (E1): media real observada por (zona, sombra).
  const empirical = {};
  for (const l of LINEAGES) {
    empirical[l] = samples[l].map((zs) => zs.map((a) =>
      a[3] ? [Math.round(a[0] / a[3]), Math.round(a[1] / a[3]), Math.round(a[2] / a[3])] : null));
  }
  return { width, height, imgs, zone, shade, alpha, empirical };
}

/** Rampas declaradas (E2): sólo desde los 5 colores de la receta. */
function declaredRamps(lineage, lockNeutral) {
  const ramp = ZONES.map(() => null);
  for (const z of LINEAGE_ZONES) {
    const src = lockNeutral && (z === 'skin' || z === 'hair')
      ? COLORS[LINEAGES[0]][z]   // bloqueado: la misma rampa en los tres
      : COLORS[lineage][z];
    ramp[ZONES.indexOf(z)] = buildRamp(src);
  }
  return ramp;
}

function render(enc, ramp, keepNeutralFrom) {
  const { width, height, zone, shade, alpha } = enc;
  const out = Buffer.alloc(width * height * 4);
  for (let i = 0; i < width * height; i++) {
    if (alpha[i] === 0) continue;
    let c;
    if (zone[i] === 0 || !ramp[zone[i]]) {
      const s = keepNeutralFrom.data;
      c = [s[i * 4], s[i * 4 + 1], s[i * 4 + 2]];
    } else {
      c = ramp[zone[i]][shade[i]];
    }
    out[i * 4] = c[0]; out[i * 4 + 1] = c[1]; out[i * 4 + 2] = c[2]; out[i * 4 + 3] = alpha[i];
  }
  return out;
}

// --------------------------------------------------------- composición

function blit(dst, dw, src, sw, sh, ox, oy) {
  for (let y = 0; y < sh; y++) {
    for (let x = 0; x < sw; x++) {
      const s = (y * sw + x) * 4, d = ((oy + y) * dw + ox + x) * 4;
      dst[d] = src[s]; dst[d + 1] = src[s + 1]; dst[d + 2] = src[s + 2]; dst[d + 3] = src[s + 3];
    }
  }
}

const CELL = 128;

/** Recorta la caja del sujeto en el frame 0 y la amplía por vecino más cercano. */
function cropZoom(src, w, h, box, z) {
  const [x0, y0, bw, bh] = box;
  const out = Buffer.alloc(bw * z * bh * z * 4);
  for (let y = 0; y < bh * z; y++) {
    for (let x = 0; x < bw * z; x++) {
      const s = (((y0 + Math.floor(y / z)) * w) + x0 + Math.floor(x / z)) * 4;
      const d = (y * bw * z + x) * 4;
      out[d] = src[s]; out[d + 1] = src[s + 1]; out[d + 2] = src[s + 2]; out[d + 3] = src[s + 3];
    }
  }
  return out;
}

function boundingBox(alpha, w, h, limitW) {
  let x0 = limitW, y0 = h, x1 = -1, y1 = -1;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < limitW; x++) {
      if (!alpha[y * w + x]) continue;
      if (x < x0) x0 = x; if (x > x1) x1 = x;
      if (y < y0) y0 = y; if (y > y1) y1 = y;
    }
  }
  const pad = 2;
  x0 = Math.max(0, x0 - pad); y0 = Math.max(0, y0 - pad);
  x1 = Math.min(limitW - 1, x1 + pad); y1 = Math.min(h - 1, y1 + pad);
  return [x0, y0, x1 - x0 + 1, y1 - y0 + 1];
}

/** Contacto ampliado: columnas = linajes, filas = experimentos. */
function writeZoomSheet(name, rows, enc, zoom) {
  const { width, height, alpha } = enc;
  const box = boundingBox(alpha, width, height, CELL);
  const [, , bw, bh] = box;
  const cw = bw * zoom, ch = bh * zoom, gap = 6;
  const sheetW = LINEAGES.length * cw + (LINEAGES.length + 1) * gap;
  const sheetH = rows.length * ch + (rows.length + 1) * gap;
  const sheet = Buffer.alloc(sheetW * sheetH * 4);
  for (let i = 0; i < sheetW * sheetH; i++) {
    sheet[i * 4] = 24; sheet[i * 4 + 1] = 24; sheet[i * 4 + 2] = 28; sheet[i * 4 + 3] = 255;
  }
  rows.forEach((r, ri) => {
    r.imgs.forEach((im, ci) => {
      const z = cropZoom(im, width, height, box, zoom);
      const ox = gap + ci * (cw + gap), oy = gap + ri * (ch + gap);
      for (let y = 0; y < ch; y++) {
        for (let x = 0; x < cw; x++) {
          const s = (y * cw + x) * 4, d = ((oy + y) * sheetW + ox + x) * 4;
          const a = z[s + 3];
          if (!a) continue;
          sheet[d] = z[s]; sheet[d + 1] = z[s + 1]; sheet[d + 2] = z[s + 2]; sheet[d + 3] = 255;
        }
      }
    });
  });
  writePng(path.join(OUT, name), sheetW, sheetH, sheet);
}

function run(body) {
  const enc = encode(body);
  const { width, height, imgs } = enc;
  const frames = width / CELL;

  const rows = [];
  rows.push({ label: 'A original', imgs: imgs.map((im) => im.data) });
  rows.push({
    label: 'B round-trip (rampa empírica)',
    imgs: LINEAGES.map((l, k) => render(enc, enc.empirical[l], imgs[k])),
  });
  rows.push({
    label: 'C rampa declarada',
    imgs: LINEAGES.map((l, k) => render(enc, declaredRamps(l, false), imgs[k])),
  });
  rows.push({
    label: 'D rampa declarada, skin+hair bloqueados',
    imgs: LINEAGES.map((l, k) => render(enc, declaredRamps(l, true), imgs[k])),
  });

  const sheetW = width, sheetH = height * rows.length * LINEAGES.length;
  // Layout: por fila, los 3 linajes apilados verticalmente.
  const sheet = Buffer.alloc(sheetW * sheetH * 4);
  let y = 0;
  for (const r of rows) {
    for (const im of r.imgs) { blit(sheet, sheetW, im, width, height, 0, y); y += height; }
  }
  writePng(path.join(OUT, `poc_${body}.png`), sheetW, sheetH, sheet);
  writeZoomSheet(`zoom_${body}.png`, rows, enc, 5);

  // Mapas de depuración: zona y sombra, en paleta de trabajo legible.
  const WORK_ZONE = [[90, 90, 90], [230, 120, 110], [150, 90, 200], [70, 140, 230], [80, 200, 150], [235, 205, 90]];
  const dbg = Buffer.alloc(width * height * 2 * 4);
  for (let i = 0; i < width * height; i++) {
    if (enc.alpha[i] === 0) continue;
    const zc = WORK_ZONE[enc.zone[i]];
    const px = Math.round((enc.shade[i] / (SHADES - 1)) * 255);
    const x = i % width, yy = Math.floor(i / width);
    const dz = (yy * width * 2 + x) * 4, ds = (yy * width * 2 + width + x) * 4;
    dbg[dz] = zc[0]; dbg[dz + 1] = zc[1]; dbg[dz + 2] = zc[2]; dbg[dz + 3] = 255;
    dbg[ds] = px; dbg[ds + 1] = px; dbg[ds + 2] = px; dbg[ds + 3] = 255;
  }
  writePng(path.join(OUT, `debug_${body}.png`), width * 2, height, dbg);

  // La hoja codificada de verdad: R = sombra, G = zona. Es lo que consume el shader.
  const encoded = Buffer.alloc(width * height * 4);
  for (let i = 0; i < width * height; i++) {
    if (enc.alpha[i] === 0) continue;
    encoded[i * 4] = Math.round((enc.shade[i] / (SHADES - 1)) * 255);
    encoded[i * 4 + 1] = Math.round((enc.zone[i] / (ZONES.length - 1)) * 255);
    encoded[i * 4 + 2] = 0;
    encoded[i * 4 + 3] = enc.alpha[i];
  }
  writePng(path.join(OUT, `encoded_${body}.png`), width, height, encoded);

  // Métricas.
  const err = {};
  for (let k = 0; k < LINEAGES.length; k++) {
    const rt = rows[1].imgs[k], orig = imgs[k].data;
    let sum = 0, max = 0, cnt = 0;
    for (let i = 0; i < width * height; i++) {
      if (enc.alpha[i] === 0) continue;
      const d = Math.max(Math.abs(rt[i * 4] - orig[i * 4]),
        Math.abs(rt[i * 4 + 1] - orig[i * 4 + 1]),
        Math.abs(rt[i * 4 + 2] - orig[i * 4 + 2]));
      sum += d; if (d > max) max = d; cnt++;
    }
    err[LINEAGES[k]] = { mean: +(sum / cnt).toFixed(2), max, pixels: cnt };
  }

  // Separación entre linajes en las zonas de prenda, fila D (control).
  const sep = [];
  for (let a = 0; a < LINEAGES.length; a++) {
    for (let b = a + 1; b < LINEAGES.length; b++) {
      let sum = 0, cnt = 0;
      const A = rows[3].imgs[a], B = rows[3].imgs[b];
      for (let i = 0; i < width * height; i++) {
        if (enc.alpha[i] === 0 || enc.zone[i] < 3) continue; // sólo prenda
        sum += Math.abs(A[i * 4] - B[i * 4]) + Math.abs(A[i * 4 + 1] - B[i * 4 + 1])
          + Math.abs(A[i * 4 + 2] - B[i * 4 + 2]);
        cnt++;
      }
      sep.push({ pair: `${LINEAGES[a]}/${LINEAGES[b]}`, meanDelta: +(sum / (cnt * 3)).toFixed(1), pixels: cnt });
    }
  }

  const zoneCount = ZONES.map((_, z) => {
    let c = 0;
    for (let i = 0; i < width * height; i++) if (enc.alpha[i] && enc.zone[i] === z) c++;
    return { zone: ZONES[z], pixels: c };
  });

  return { body, frames, size: `${width}x${height}`, roundTripError: err, garmentSeparation: sep, zoneCount };
}

// ---------------------------------------------------------------- rampas

function writeRampTextures() {
  for (const lin of LINEAGES) {
    const w = SHADES, h = 8;
    const data = Buffer.alloc(w * h * 4);
    for (let z = 0; z < ZONES.length; z++) {
      const base = ZONES[z] === 'neutral' ? null : COLORS[lin][ZONES[z]];
      const ramp = base ? buildRamp(base) : null;
      for (let s = 0; s < SHADES; s++) {
        const c = ramp ? ramp[s] : [Math.round((s / (SHADES - 1)) * 255), 0, 0];
        const i = (z * w + s) * 4;
        data[i] = c[0]; data[i + 1] = c[1]; data[i + 2] = c[2]; data[i + 3] = 255;
      }
    }
    writePng(path.join(OUT, `ramp_${lin}.png`), w, h, data);
  }
}

const report = { shades: SHADES, zones: ZONES, lineages: LINEAGES, bodies: [] };
for (const body of ['male', 'female']) report.bodies.push(run(body));
writeRampTextures();
fs.writeFileSync(path.join(OUT, 'report.json'), JSON.stringify(report, null, 2));
console.log(JSON.stringify(report, null, 2));

// M3: turn the hand-curated RadioStationsList.md into the LittleFS payload.
//
//   node build-data.mjs [--force]
//
// Reads  Tools/StationMining/RadioStationsList.md  (+ Assets/)
// Writes Esp32InternetRadio/data/stations.csv      (+ data/logos/)
//
// M4: the logos are converted here rather than copied. `Assets/` holds the
// PNGs a human works with; `data/logos/` gets `.565` files, which are nothing
// but 92x92 little-endian RGB565 pixels — 16,928 bytes, no header, ready to be
// read a row at a time and pushed straight at the panel.
//
// That is a change to D5, and it was forced by the board rather than chosen.
// PNGdec needs a 45,604-byte contiguous allocation for zlib's sliding window,
// and the ESP32 only has one at boot: with an HTTPS stream live the largest
// free block measures 16,372–19,444, and even with the stream stopped it is
// 38,900. Decoding on the device therefore worked at boot and nowhere else.
// Doing it here costs flash and gives up "drop a PNG in data/logos/" — the
// PNG still drops into Assets/, it just has to go through this script.
//
// The markdown table is the source of truth a human edits; data/ is generated
// and should never be hand-edited. Frequency ranges ("100-101") expand to one
// CSV row per dial position, because the firmware indexes 4 banks x 19 slots
// and Architecture.md §5 wants one row per filled slot.
//
// Refuses to write if anything would produce a slot the firmware cannot play:
// a duplicate (button, frequency), a URL over kMaxUrlLength, a logo that is
// missing or not 92x92, or a comma anywhere in a field (the firmware parser is
// hand-rolled and splits on commas — there is no quoting). --force downgrades
// those to warnings and drops the offending rows.

import { readFileSync, writeFileSync, mkdirSync, readdirSync, rmSync, existsSync } from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const REPO = path.resolve(HERE, '../..');
const TABLE = path.join(HERE, 'RadioStationsList.md');
const ASSETS = path.join(HERE, 'Assets');
const OUT = path.join(REPO, 'Esp32InternetRadio/data');
const OUT_LOGOS = path.join(OUT, 'logos');

const MAX_URL = 192;          // AudioEngine::kMaxUrlLength — playUrl() returns false past it
const LOGO_PX = 92;           // no runtime scaler; anything else is a data error
const LOGO_BYTES = LOGO_PX * LOGO_PX * 2;   // Display.cpp checks the file is exactly this
const BUTTONS = ['L', 'M', 'K', 'U'];
const FREQ_MIN = 87, FREQ_MAX = 105;

// §6: the station name is drawn at x=21 in the 5×7 font ported from
// RadioApp.Hardware/Helpers/Font5x7.cs — 6 px advance, clipped at the right
// edge of a 160 px screen. That font has no glyphs above U+007F, so a name
// carrying any is a data error rather than a rendering compromise, and it is
// worth failing on here: the CSV parser is byte-transparent, so the fault would
// otherwise stay invisible until M4 draws it.
const NAME_X = 21, SCREEN_W = 160, GLYPH_ADVANCE = 6;
const NAME_FITS = Math.floor((SCREEN_W - NAME_X) / GLYPH_ADVANCE);  // 23

const force = process.argv.includes('--force');
const problems = [];
const advisories = [];
const warn = (row, message) => problems.push(`${TABLE.replace(REPO + path.sep, '')}:${row.line}  ${row.button} ${row.freq}  ${message}`);
const advise = (row, message) => advisories.push(`${row.button} ${row.freq}`.padEnd(10) + message);

function pngSize(file) {
  const b = readFileSync(file);
  if (b.slice(0, 8).toString('hex') !== '89504e470d0a1a0a') return null;
  return { w: b.readUInt32BE(16), h: b.readUInt32BE(20), bytes: b.length };
}

// --- PNG -> RGB565 ----------------------------------------------------------
// Deliberately not a PNG library. This script has no dependencies and installs
// nothing, which is worth keeping for a tool that runs once a season; and the
// input is not "PNG" in general but the 60 files in Assets/, every one of them
// 92x92, 8-bit, colour type 6, non-interlaced. So this handles exactly that and
// throws on anything else, rather than silently mis-rendering it.
//
// node:zlib does the only genuinely hard part. What is left is un-filtering:
// each row is prefixed with one filter byte, and PNG filters are defined
// against the reconstructed bytes to the left and above, not the raw ones.
function pngToRgb565(file) {
  const b = readFileSync(file);
  const width = b.readUInt32BE(16), height = b.readUInt32BE(20);
  const depth = b[24], colorType = b[25], interlace = b[28];

  if (depth !== 8 || colorType !== 6 || interlace !== 0) {
    throw new Error(`${path.basename(file)}: need 8-bit RGBA non-interlaced, got `
                    + `depth=${depth} colour=${colorType} interlace=${interlace}`);
  }

  // IDAT is allowed to be split across any number of chunks.
  const parts = [];
  for (let at = 8; at + 8 <= b.length;) {
    const length = b.readUInt32BE(at);
    const type = b.toString('ascii', at + 4, at + 8);
    if (type === 'IDAT') parts.push(b.subarray(at + 8, at + 8 + length));
    if (type === 'IEND') break;
    at += length + 12;   // length + type + data + CRC
  }

  const raw = zlib.inflateSync(Buffer.concat(parts));
  const bpp = 4, stride = width * bpp;

  if (raw.length !== (stride + 1) * height) {
    throw new Error(`${path.basename(file)}: inflated to ${raw.length} bytes, expected ${(stride + 1) * height}`);
  }

  const out = Buffer.alloc(width * height * 2);
  const line = Buffer.alloc(stride), prev = Buffer.alloc(stride);

  for (let y = 0; y < height; y++) {
    const filter = raw[y * (stride + 1)];
    raw.copy(line, 0, y * (stride + 1) + 1, (y + 1) * (stride + 1));

    for (let x = 0; x < stride; x++) {
      const a = x >= bpp ? line[x - bpp] : 0;   // left
      const bb = prev[x];                       // above
      const c = x >= bpp ? prev[x - bpp] : 0;   // above-left
      let value = line[x];

      if (filter === 1) value += a;
      else if (filter === 2) value += bb;
      else if (filter === 3) value += (a + bb) >> 1;
      else if (filter === 4) {
        const p = a + bb - c;
        const pa = Math.abs(p - a), pb = Math.abs(p - bb), pc = Math.abs(p - c);
        value += (pa <= pb && pa <= pc) ? a : (pb <= pc ? bb : c);
      } else if (filter !== 0) {
        throw new Error(`${path.basename(file)}: row ${y} has filter ${filter}`);
      }

      line[x] = value & 0xff;
    }

    for (let x = 0; x < width; x++) {
      // §6 draws on black and the firmware has no alpha channel to give the
      // panel, so transparency is composited here, once, instead of every
      // station change.
      const alpha = line[x * bpp + 3];
      const r = Math.round(line[x * bpp] * alpha / 255);
      const g = Math.round(line[x * bpp + 1] * alpha / 255);
      const bl = Math.round(line[x * bpp + 2] * alpha / 255);

      // Little-endian, because the ESP32 is and Display.cpp reads the bytes
      // straight into a uint16_t row with no swapping.
      out.writeUInt16LE(((r & 0xf8) << 8) | ((g & 0xfc) << 3) | (bl >> 3), (y * width + x) * 2);
    }

    line.copy(prev);
  }

  return out;
}

// The name that reaches the CSV, and therefore LittleFS: the .png in the table
// is the source asset, the .565 beside it is what the radio opens.
const rgb565Name = (logo) => logo.replace(/\.png$/i, '.565');

// --- parse the markdown table -----------------------------------------------
const rows = [];
readFileSync(TABLE, 'utf8').split(/\r?\n/).forEach((line, i) => {
  if (!line.includes('|')) return;
  const c = line.split('|').map((s) => s.trim());
  if (c[0] === 'Button' || /^-+$/.test(c[0]) || c.length < 7) return;
  rows.push({ line: i + 1, button: c[0], freq: c[1], country: c[2], name: c[3], site: c[4], url: c[5], logo: c[6] });
});

// --- validate and expand ----------------------------------------------------
const slots = new Map();      // "L87" -> row
const logosUsed = new Set();

for (const row of rows) {
  if (!BUTTONS.includes(row.button)) { warn(row, `unknown button "${row.button}"`); continue; }

  const m = row.freq.match(/^(\d+)(?:-(\d+))?$/);
  if (!m) { warn(row, `unparseable frequency "${row.freq}"`); continue; }
  const lo = Number(m[1]), hi = m[2] ? Number(m[2]) : lo;
  if (lo < FREQ_MIN || hi > FREQ_MAX || hi < lo) { warn(row, `frequency out of ${FREQ_MIN}-${FREQ_MAX}`); continue; }

  if (!/^https?:\/\//.test(row.url)) { warn(row, `no usable stream URL ("${row.url}") — ${row.name}`); continue; }
  if (row.url.length > MAX_URL) { warn(row, `URL is ${row.url.length} chars, over the ${MAX_URL} cap — ${row.name}`); continue; }
  if (/\.m3u8?($|\?)/.test(row.url)) { warn(row, `HLS/playlist URL — ESP32-audioI2S cannot follow it — ${row.name}`); continue; }

  // No quoting in the firmware parser, so a comma anywhere is unrepresentable.
  // Drop the row rather than only warning: emitted, it would shift every field
  // after it and the firmware would reject the whole line anyway.
  const commaIn = [['name', row.name], ['url', row.url], ['logo', row.logo]]
    .find(([, value]) => value.includes(','));
  if (commaIn) {
    warn(row, `comma in ${commaIn[0]} — the CSV parser cannot represent it`);
    continue;
  }

  // The 5×7 font is ASCII-only (§6). Name the offending characters: "non-ASCII"
  // alone sends you hunting through a line that looks fine in every editor.
  const nonAscii = [...row.name].filter((c) => c.codePointAt(0) > 0x7f);
  if (nonAscii.length) {
    const shown = [...new Set(nonAscii)].join('');
    warn(row, `non-ASCII in name (${shown}) — the 5x7 font cannot draw it — ${row.name}`);
    continue;
  }

  // Clipping is the defined behaviour, not a fault (§6), so this is advice
  // rather than an error — but a name whose distinguishing half falls off the
  // right edge is worth seeing before M4 rather than during it.
  if (row.name.length > NAME_FITS) {
    advise(row, `name clips at ${NAME_FITS} chars: "${row.name.slice(0, NAME_FITS)}|${row.name.slice(NAME_FITS)}"`);
  }

  if (!row.logo || row.logo === '-') { warn(row, `no logo — ${row.name}`); continue; }
  const logoPath = path.join(ASSETS, row.logo);
  if (!existsSync(logoPath)) { warn(row, `logo "${row.logo}" is not in Assets/ — ${row.name}`); continue; }
  const size = pngSize(logoPath);
  if (!size) { warn(row, `logo "${row.logo}" is not a PNG`); continue; }
  if (size.w !== LOGO_PX || size.h !== LOGO_PX) { warn(row, `logo "${row.logo}" is ${size.w}x${size.h}, must be ${LOGO_PX}x${LOGO_PX}`); continue; }

  let clash = false;
  for (let f = lo; f <= hi; f++) {
    const key = row.button + f;
    if (slots.has(key)) { warn(row, `duplicate slot — "${row.name}" collides with "${slots.get(key).name}"`); clash = true; }
  }
  if (clash) continue;

  for (let f = lo; f <= hi; f++) slots.set(row.button + f, row);
  logosUsed.add(row.logo);
}

if (problems.length) {
  console.error(`\n${problems.length} problem row(s):`);
  problems.forEach((p) => console.error('  ' + p));
  if (!force) {
    console.error('\nNothing written. Fix the table, or re-run with --force to skip these rows.');
    process.exit(1);
  }
  console.error('\n--force: the rows above are skipped; their slots stay empty (audio stops, no error).\n');
}

// --- write ------------------------------------------------------------------
rmSync(OUT_LOGOS, { recursive: true, force: true });
mkdirSync(OUT_LOGOS, { recursive: true });

const lines = ['button,frequency,name,url,logo'];
for (const button of BUTTONS) {
  for (let f = FREQ_MIN; f <= FREQ_MAX; f++) {
    const row = slots.get(button + f);
    if (row) lines.push(`${button},${f},${row.name},${row.url},${rgb565Name(row.logo)}`);
  }
}
writeFileSync(path.join(OUT, 'stations.csv'), lines.join('\n') + '\n');

let logoBytes = 0;
for (const logo of logosUsed) {
  const pixels = pngToRgb565(path.join(ASSETS, logo));

  // Every logo is the same size by construction, and Display.cpp rejects a
  // file that is not exactly this — the only check a headerless format can
  // offer, so it is worth being sure of on this side too.
  if (pixels.length !== LOGO_BYTES) {
    console.error(`${logo}: converted to ${pixels.length} bytes, expected ${LOGO_BYTES}`);
    process.exit(1);
  }

  writeFileSync(path.join(OUT_LOGOS, rgb565Name(logo)), pixels);
  logoBytes += pixels.length;
}

// --- report -----------------------------------------------------------------
const csvBytes = readFileSync(path.join(OUT, 'stations.csv')).length;
console.log(`stations.csv   ${slots.size}/76 slots filled, ${lines.length - 1} rows, ${csvBytes} bytes`);
const pngBytes = [...logosUsed].reduce((sum, l) => sum + pngSize(path.join(ASSETS, l)).bytes, 0);
console.log(`logos/         ${logosUsed.size} .565 files, ${(logoBytes / 1024).toFixed(1)} KB  (${LOGO_BYTES} bytes each, from ${(pngBytes / 1024).toFixed(1)} KB of PNG)`);

// LittleFS rounds every file up to a 4 KB block, and with 60-odd files that
// overhead is not a rounding error — M3 measured 748 KB on the board against
// 595 KB of content. Report what the partition will actually hold.
const BLOCK = 4096;
const blocks = (n) => Math.ceil(n / BLOCK) * BLOCK;
const onFlash = logosUsed.size * blocks(LOGO_BYTES) + blocks(csvBytes);
console.log(`LittleFS total ${((logoBytes + csvBytes) / 1024).toFixed(1)} KB of content, ~${(onFlash / 1024).toFixed(1)} KB in 4 KB blocks  → the D6 partition input`);

const urls = [...new Set([...slots.values()].map((r) => r.url))];
const lens = urls.map((u) => u.length).sort((a, b) => a - b);
console.log(`URL length     median ${lens[lens.length >> 1]}, max ${lens[lens.length - 1]} (cap ${MAX_URL})`);

for (const button of BUTTONS) {
  const empty = [];
  for (let f = FREQ_MIN; f <= FREQ_MAX; f++) if (!slots.has(button + f)) empty.push(f);
  console.log(`bank ${button}         ${19 - empty.length}/19 filled${empty.length ? `, empty: ${empty.join(', ')}` : ''}`);
}

// Printed after the summary, not before it: these do not stop a build, and
// burying the numbers above under 17 lines of advice is how advice gets ignored.
if (advisories.length) {
  console.log(`\n${advisories.length} name(s) will clip on the 160 px screen (§6 — this is defined behaviour, not an error):`);
  advisories.forEach((a) => console.log('  ' + a));
}

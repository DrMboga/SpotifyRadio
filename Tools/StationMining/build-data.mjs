// M3: turn the hand-curated RadioStationsList.md into the LittleFS payload.
//
//   node build-data.mjs [--force]
//
// Reads  Tools/StationMining/RadioStationsList.md  (+ Assets/)
// Writes Esp32InternetRadio/data/stations.csv      (+ data/logos/)
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

import { readFileSync, writeFileSync, mkdirSync, copyFileSync, readdirSync, rmSync, existsSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const REPO = path.resolve(HERE, '../..');
const TABLE = path.join(HERE, 'RadioStationsList.md');
const ASSETS = path.join(HERE, 'Assets');
const OUT = path.join(REPO, 'Esp32InternetRadio/data');
const OUT_LOGOS = path.join(OUT, 'logos');

const MAX_URL = 192;          // AudioEngine::kMaxUrlLength — playUrl() returns false past it
const LOGO_PX = 92;           // no runtime scaler; anything else is a data error
const BUTTONS = ['L', 'M', 'K', 'U'];
const FREQ_MIN = 87, FREQ_MAX = 105;

const force = process.argv.includes('--force');
const problems = [];
const warn = (row, message) => problems.push(`${TABLE.replace(REPO + path.sep, '')}:${row.line}  ${row.button} ${row.freq}  ${message}`);

function pngSize(file) {
  const b = readFileSync(file);
  if (b.slice(0, 8).toString('hex') !== '89504e470d0a1a0a') return null;
  return { w: b.readUInt32BE(16), h: b.readUInt32BE(20), bytes: b.length };
}

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
  for (const [field, value] of [['name', row.name], ['url', row.url], ['logo', row.logo]]) {
    if (value.includes(',')) { warn(row, `comma in ${field} — the CSV parser cannot represent it`); }
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
    if (row) lines.push(`${button},${f},${row.name},${row.url},${row.logo}`);
  }
}
writeFileSync(path.join(OUT, 'stations.csv'), lines.join('\n') + '\n');

let logoBytes = 0;
for (const logo of logosUsed) {
  copyFileSync(path.join(ASSETS, logo), path.join(OUT_LOGOS, logo));
  logoBytes += pngSize(path.join(ASSETS, logo)).bytes;
}

// --- report -----------------------------------------------------------------
const csvBytes = readFileSync(path.join(OUT, 'stations.csv')).length;
console.log(`stations.csv   ${slots.size}/76 slots filled, ${lines.length - 1} rows, ${csvBytes} bytes`);
console.log(`logos/         ${logosUsed.size} files, ${(logoBytes / 1024).toFixed(1)} KB  (largest ${(Math.max(...[...logosUsed].map((l) => pngSize(path.join(ASSETS, l)).bytes)) / 1024).toFixed(1)} KB)`);
console.log(`LittleFS total ${((logoBytes + csvBytes) / 1024).toFixed(1)} KB  → the D6 partition input`);

const urls = [...new Set([...slots.values()].map((r) => r.url))];
const lens = urls.map((u) => u.length).sort((a, b) => a - b);
console.log(`URL length     median ${lens[lens.length >> 1]}, max ${lens[lens.length - 1]} (cap ${MAX_URL})`);

for (const button of BUTTONS) {
  const empty = [];
  for (let f = FREQ_MIN; f <= FREQ_MAX; f++) if (!slots.has(button + f)) empty.push(f);
  console.log(`bank ${button}         ${19 - empty.length}/19 filled${empty.length ? `, empty: ${empty.join(', ')}` : ''}`);
}

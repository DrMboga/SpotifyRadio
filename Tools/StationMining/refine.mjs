// Cleans up the URLs a probe run accepted, before they reach stations.csv.
//
//   node refine.mjs probed.json refined.json
//
// Three things the raw MyTuner URLs get wrong for this radio:
//
// 1. LENGTH. AudioEngine::playUrl() caps a URL at kMaxUrlLength = 192 bytes and
//    returns false past that — the station would simply never play, silently.
//    26 of the scraped URLs are over, the longest 1707 characters.
// 2. SESSION JUNK. Many carry aggregator tags, consent blobs, listener ids and
//    timestamped tokens. Some are decoration; some expire, which turns into a
//    station that works today and is dead next month.
// 3. AACP WHERE MP3 EXISTS. §5.2 prefers the MP3 sibling: it is the lighter
//    decoder on a board with no RAM to spare.
//
// Rather than guess which query parameters matter, this generates candidate
// variants and *probes* them, keeping the cleanest one that actually plays. A
// stripped URL that 403s means the token was load-bearing, and the original
// stays.

import { execFile } from 'node:child_process';
import { readFileSync, writeFileSync } from 'node:fs';
import { promisify } from 'node:util';

const execFileAsync = promisify(execFile);
const MAX_URL = 192; // must match AudioEngine::kMaxUrlLength
const TIMEOUT = Number(process.env.PROBE_TIMEOUT || 8);
const CONCURRENCY = Number(process.env.PROBE_CONCURRENCY || 10);

const TRACKING = /aggregator=|aw_0_|_art=|listenerid=|metaid=|[?&]cb=|__cb=|amsparams=|ar-distributor=|playerid=|pbid=|upd-|mode=preroll/i;

function variantsFor(url) {
  const out = [];
  const add = (u) => { if (u && !out.includes(u)) out.push(u); };

  add(url);

  const noQuery = url.split('?')[0];
  add(noQuery);

  // §5.2: the MP3 sibling of an AAC+ endpoint.
  if (/aacp/i.test(url)) {
    add(noQuery.replace(/aacp/i, 'mp3'));
    add(url.replace(/aacp/i, 'mp3'));
  }

  return out;
}

async function probeUrl(url) {
  const args = [
    '--silent', '--show-error', '--location', '--max-redirs', '10', '--cookie', '',
    '--max-time', String(TIMEOUT),
    '--header', 'Icy-MetaData: 1',
    '--header', 'User-Agent: ESP32InternetRadio/1.0',
    '--output', process.platform === 'win32' ? 'NUL' : '/dev/null',
    '--dump-header', '-', url,
  ];

  let headers = '';
  try {
    headers = (await execFileAsync('curl', args, { maxBuffer: 1 << 20 })).stdout;
  } catch (error) {
    headers = error.stdout || '';
    if (!headers) return null;
  }

  const status = [...headers.matchAll(/^(?:HTTP\/[\d.]+|ICY)\s+(\d{3})/gim)].pop()?.[1];
  if (status !== '200' && status !== '206') return null;

  const ct = ([...headers.matchAll(/^content-type:\s*(.*)$/gim)].pop()?.[1] || '').toLowerCase().trim();
  if (/mpegurl|scpls/.test(ct)) return null;
  const codec = /mpeg|mp3/.test(ct) ? 'mp3' : /aac/.test(ct) ? 'aac' : null;
  if (!codec) return null;

  const bitrate = [...headers.matchAll(/^icy-br:\s*(.*)$/gim)].pop()?.[1]?.trim() || '';
  return { url, codec, bitrate };
}

async function refineOne(row) {
  const needsWork = row.url.length >= MAX_URL || TRACKING.test(row.url) || /aacp/i.test(row.url);
  if (!needsWork) return { ...row, refined: false };

  for (const candidate of variantsFor(row.url)) {
    // Skip variants that could never be accepted by the firmware anyway.
    if (candidate.length >= MAX_URL) continue;

    const result = await probeUrl(candidate);
    if (!result) continue;

    // §5.2: never trade an MP3 endpoint for an AAC one while cleaning up.
    if (row.codec === 'mp3' && result.codec !== 'mp3') continue;

    return {
      ...row,
      url: result.url,
      codec: result.codec,
      bitrate: result.bitrate || row.bitrate,
      refined: result.url !== row.url,
      originalUrl: result.url !== row.url ? row.url : undefined,
    };
  }

  // Nothing shorter worked. If the original is over the cap it cannot be used.
  return {
    ...row,
    refined: false,
    tooLong: row.url.length >= MAX_URL,
  };
}

const rows = JSON.parse(readFileSync(process.argv[2], 'utf8')).filter((r) => r.alive);
const out = new Array(rows.length);
let next = 0;

async function worker() {
  while (next < rows.length) {
    const i = next++;
    out[i] = await refineOne(rows[i]);
    process.stderr.write(`\r  refined ${i + 1}/${rows.length}   `);
  }
}
await Promise.all(Array.from({ length: CONCURRENCY }, worker));
process.stderr.write('\n');

const changed = out.filter((r) => r.refined);
const dropped = out.filter((r) => r.tooLong);
const usable = out.filter((r) => !r.tooLong);

console.error(`${changed.length} URLs shortened or switched to MP3`);
console.error(`${dropped.length} still over ${MAX_URL} chars - unusable by the firmware`);
for (const r of dropped) console.error(`    ${r.name} (${r.url.length})`);

writeFileSync(process.argv[3], JSON.stringify(usable, null, 2));
console.error(`${usable.length} written`);

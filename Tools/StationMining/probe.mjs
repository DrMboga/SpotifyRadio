// MD-3: probe candidate stream URLs and report what is actually playable.
//
// The database's URLs are ~5 months old and 45 % of them reveal no codec in the
// path (Architecture.md §5.1), so the only trustworthy source of truth is what
// the server says when you connect. This asks every candidate and records it.
//
// Shells out to curl rather than using fetch(): plenty of Shoutcast servers
// still answer "ICY 200 OK" instead of "HTTP/1.1 200 OK", which undici rejects
// outright and curl handles without blinking.
//
//   node probe.mjs candidates.json > probed.json
//
// Input:  [{ name, country, genres, likes, dislikes, rating, url }, ...]
// Output: the same objects plus { alive, status, finalUrl, codec, bitrate,
//                                 icyName, icyGenre, note }

import { execFile } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { promisify } from 'node:util';

const execFileAsync = promisify(execFile);

// Overridable: a slow redirect chain (iHeart hands out a token then bounces
// again) can need longer than the default.
const TIMEOUT_SECONDS = Number(process.env.PROBE_TIMEOUT || 8);
const CONCURRENCY = Number(process.env.PROBE_CONCURRENCY || 6);

// Codec comes from Content-Type, which is the one field servers get right.
// §5.2 prefers MP3; D15 says AAC has to work too, so both are "ok" — anything
// else (HLS playlists, Ogg, an HTML error page) is not something the ESP32
// decoder can take.
function codecFromContentType(contentType) {
  if (!contentType) return 'unknown';
  const t = contentType.toLowerCase();
  // Playlists must be tested BEFORE mpeg: an HLS manifest is served as
  // `application/vnd.apple.mpegurl`, which contains "mpeg" and would otherwise
  // be reported as a playable MP3 stream. ESP32-audioI2S cannot follow HLS.
  if (t.includes('mpegurl') || t.includes('x-scpls') || t.includes('pls')) return 'playlist';
  if (t.includes('mpeg') || t.includes('mp3')) return 'mp3';
  if (t.includes('aac')) return 'aac';
  if (t.includes('ogg') || t.includes('opus')) return 'ogg';
  if (t.includes('html') || t.includes('json') || t.includes('xml')) return 'not-audio';
  return contentType;
}

function headerValue(headerText, name) {
  // Last occurrence wins: with -L the header block accumulates across every
  // hop, and the value that matters is the one the final server sent.
  const re = new RegExp(`^${name}:\\s*(.*)$`, 'gim');
  let match;
  let last = null;
  while ((match = re.exec(headerText)) !== null) last = match[1].trim();
  return last;
}

// A live stream never ends, so curl always eventually hits --max-time and exits
// 28. That is not a failure: by then the response headers are long since in
// hand, and they are the whole answer. So this judges the headers and ignores
// the exit code — which is also why there is no --range here. Asking for a byte
// range makes no difference to a server that streams regardless, and made an
// earlier version of this script report every working station as a timeout.
async function probeOne(candidate) {
  const result = { ...candidate, alive: false, status: '', codec: '', bitrate: '', icyName: '', icyGenre: '', note: '' };

  const args = [
    '--silent', '--show-error',
    '--location',                    // follow the CDN redirect chain
    '--max-redirs', '10',
    // Enables curl's cookie engine without reading a file, so a cookie set on
    // one hop comes back on the next. The big US broadcasters hand one out
    // with their redirect and stall without it.
    '--cookie', '',
    '--max-time', String(TIMEOUT_SECONDS),
    '--header', 'Icy-MetaData: 1',   // ask for the ICY headers
    '--header', 'User-Agent: ESP32InternetRadio/1.0',
    '--output', process.platform === 'win32' ? 'NUL' : '/dev/null',
    '--dump-header', '-',
    candidate.url,
  ];

  let headers = '';
  try {
    headers = (await execFileAsync('curl', args, { maxBuffer: 1 << 20 })).stdout;
  } catch (error) {
    // Exit 28 (timeout) is the normal outcome for a healthy stream. Whatever
    // headers arrived before the clock ran out are on the error object.
    headers = error.stdout || '';
    if (!headers) {
      result.note = 'connection failed';
      return result;
    }
  }

  // Last status line wins — with -L the block accumulates over every hop. Both
  // "HTTP/1.1 200 OK" and Shoutcast's "ICY 200 OK" are matched.
  const statusLines = [...headers.matchAll(/^(?:HTTP\/[\d.]+|ICY)\s+(\d{3})/gim)];
  const code = statusLines.length ? statusLines[statusLines.length - 1][1] : '';

  result.status = code;
  result.codec = codecFromContentType(headerValue(headers, 'content-type'));
  result.bitrate = headerValue(headers, 'icy-br') || '';
  result.icyName = headerValue(headers, 'icy-name') || '';
  result.icyGenre = headerValue(headers, 'icy-genre') || '';

  if (code !== '200' && code !== '206') {
    if (!code) {
      result.note = 'no response';
    } else {
      // 403 and 451 usually mean geo-blocking rather than a dead stream — the
      // radio lives in Germany, so those are unusable here however good the
      // station is.
      result.note = (code === '403' || code === '451') ? 'blocked (likely geo-restricted)' : `http ${code}`;
    }
  } else if (result.codec === 'playlist') {
    result.note = 'playlist, not a stream - resolve to the inner URL';
  } else if (result.codec === 'not-audio' || result.codec === 'unknown') {
    result.note = 'no audio content-type';
  } else if (result.codec === 'ogg') {
    result.note = 'ogg/opus - ESP32-audioI2S cannot decode this';
  } else {
    result.alive = true;
  }

  return result;
}

async function probeAll(candidates) {
  const results = new Array(candidates.length);
  let next = 0;

  async function worker() {
    while (next < candidates.length) {
      const index = next++;
      results[index] = await probeOne(candidates[index]);
      process.stderr.write(`\r  probed ${index + 1}/${candidates.length}   `);
    }
  }

  await Promise.all(Array.from({ length: Math.min(CONCURRENCY, candidates.length) }, worker));
  process.stderr.write('\n');
  return results;
}

const inputPath = process.argv[2];
if (!inputPath) {
  console.error('usage: node probe.mjs candidates.json > probed.json');
  process.exit(1);
}

const candidates = JSON.parse(readFileSync(inputPath, 'utf8'));
process.stderr.write(`probing ${candidates.length} candidates...\n`);
const probed = await probeAll(candidates);

const alive = probed.filter((r) => r.alive).length;
process.stderr.write(`${alive} alive, ${probed.length - alive} not\n`);

console.log(JSON.stringify(probed, null, 2));

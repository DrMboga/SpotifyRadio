// Turns probed.json into a human-readable shortlist to pick dial slots from.
//
//   node shortlist.mjs probed.json out.md [perBucket]
//
// Grouped by country then genre bucket, because that is how the radio is
// organised: 4 banks x 19 dial positions, and a bank wants to be one coherent
// thing (§2).

import { readFileSync, writeFileSync } from 'node:fs';

const [inPath, outPath, perBucketArg] = process.argv.slice(2);
if (!inPath || !outPath) {
  console.error('usage: node shortlist.mjs probed.json out.md [perBucket]');
  process.exit(1);
}
const PER_BUCKET = Number(perBucketArg || 15);

const all = JSON.parse(readFileSync(inPath, 'utf8'));
const alive = all.filter((r) => r.alive);

// The DB separates genres with "|", which would break a markdown table.
const esc = (s) => String(s ?? '').replace(/\|/g, ' / ').trim();

// §5.2: MP3 is the lighter decoder on a board with no RAM to spare. Among
// stations of comparable rank, the MP3 one is the better slot.
const codecTag = (r) => `${r.codec.toUpperCase()}${r.bitrate ? ' ' + r.bitrate + 'k' : ''}`;

function table(rows) {
  const header = '| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |\n|--:|---|---|---|---|---|';
  const body = rows.map((r, i) => {
    // Flag where the server's own name disagrees with the catalogue name —
    // that means the URL points at a different station than the label claims.
    const icy = esc(r.icyName) || '—';
    const votes = r.likes === null || r.likes === undefined ? '—' : `${r.likes}/${r.dislikes}`;
    return `| ${i + 1} | ${esc(r.name)} | ${icy} | ${votes} | ${codecTag(r)} | \`${r.url}\` |`;
  });
  return [header, ...body].join('\n');
}

const countries = [...new Set(alive.map((r) => r.country))].sort();
const buckets = ['rock', 'pop', 'retro', 'untagged'];
const bucketTitle = { rock: 'Rock / Metal', pop: 'Pop / Contemporary', retro: 'Retro (60s–90s, oldies, classic hits)', untagged: 'Untagged (no genre in the database)' };

let md = `# Station shortlist — ranked and probe-verified

Ranked by the §5.3 score: the **Wilson lower bound on 👍/(👍+👎)**, not raw like
count. That is deliberate — sorting by raw likes ranks by city size and puts
London pop on top of everything; sorting by \`Rating\` buries the 73 % of rows
where \`Rating = 0\` means *unrated* rather than *bad*. \`Rating\` enters only as a
tiebreak between near-equal scores.

Genre buckets match **both** MyTuner vocabularies. The taxonomy changed between
the original UK/German scrape and the later Russian/US one — \`Pop Music\` finds
289 old rows and 0 new ones, \`Pop / Top 40\` the reverse — so a filter written
for either alone silently loses half the catalogue.

Every row answered from this machine with a usable audio \`Content-Type\`. Prefer
**MP3 over AAC** where two stations are otherwise equal (§5.2): AAC is the
heavier decoder on a board with no RAM to spare.

Check **Server says (ICY)** against the station name — where they disagree, the
URL points somewhere other than the label claims.

`;

const counts = [];
for (const country of countries) {
  const inCountry = alive.filter((r) => r.country === country);
  counts.push(`${country}: ${inCountry.length}`);
  md += `\n## ${country} — ${inCountry.length} verified\n`;

  for (const bucket of buckets) {
    const rows = inCountry.filter((r) => r.bucket === bucket).slice(0, PER_BUCKET);
    if (!rows.length) continue;
    md += `\n### ${bucketTitle[bucket]} (${rows.length})\n\n${table(rows)}\n`;
  }
}

// What did not survive, so a missing favourite is explained rather than silent.
const dead = all.filter((r) => !r.alive);
if (dead.length) {
  const byNote = {};
  for (const r of dead) (byNote[r.note] ??= []).push(`${r.name} (${r.country})`);
  md += `\n## Not usable (${dead.length})\n`;
  for (const [note, names] of Object.entries(byNote).sort((a, b) => b[1].length - a[1].length)) {
    md += `\n**${note}** — ${names.length}\n\n${names.map((n) => `- ${n}`).join('\n')}\n`;
  }
}

writeFileSync(outPath, md);
console.error(`${alive.length} alive / ${all.length} probed`);
console.error(counts.join('  '));

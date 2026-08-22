// MD-1: rank the v1 scraper database into candidate shortlists per country.
//
//   node --experimental-sqlite rank.mjs <db-path> <out.json> [limitPerBucket]
//
// Two things here are not obvious and both are load-bearing.
//
// 1. RANKING (Architecture.md §5.3). `Rating` is 0 on 73 % of rows and 0 means
//    *unrated*, not bad — Radio X London has 355 likes and a Rating of 0.
//    Sorting by Rating buries good stations; sorting by raw Likes ranks by
//    audience size, which puts London pop on top because London is large. So
//    the key is the Wilson lower bound on likes/(likes+dislikes): a confidence-
//    discounted ratio, where 40 likes and 0 dislikes beats 600 and 70, but
//    2 likes and 0 dislikes beats neither.
//
// 2. GENRE VOCABULARY DRIFT. MyTuner changed its taxonomy between the original
//    UK/German scrape and the later Russian/US one, and the two share almost no
//    terms: `Pop Music` matches 289 old rows and 0 new ones, `Alternative Rock`
//    21 old and 0 new. Filtering on either vocabulary alone silently drops half
//    the catalogue, so every bucket below has to match both.

import { DatabaseSync } from 'node:sqlite';
import { writeFileSync } from 'node:fs';

const [dbPath, outPath, limitArg] = process.argv.slice(2);
if (!dbPath || !outPath) {
  console.error('usage: node --experimental-sqlite rank.mjs <db> <out.json> [limitPerBucket]');
  process.exit(1);
}
const LIMIT = Number(limitArg || 40);

// Old vocabulary on the left of each pair, new on the right.
const BUCKETS = {
  rock: /\b(rock|metal|punk|grunge)\b|alternative|\bindie\b/i,
  pop: /\bpop\b|top 40|hot ac|adult contemporary|euro hits|\bcharts?\b|\bdance\b|\bedm\b/i,
  retro: /\b(60s|70s|80s|90s|oldies|classic hits)\b/i,
};

// Formats the radio is not for. Checked first, so a station tagged
// "News | Rock" still counts as rock but a pure talk station never does.
const EXCLUDE = /\b(news|talk|sports?|religious|christian|gospel|classical|culture|education|community|local government)\b/i;

function wilsonLowerBound(likes, dislikes) {
  const n = likes + dislikes;
  if (n === 0) return 0;
  const z = 1.96; // 95 % confidence
  const p = likes / n;
  const denominator = 1 + (z * z) / n;
  const centre = p + (z * z) / (2 * n);
  const margin = z * Math.sqrt((p * (1 - p) + (z * z) / (4 * n)) / n);
  return (centre - margin) / denominator;
}

function bucketsFor(genres) {
  const g = genres || '';
  const matched = [];
  for (const [name, pattern] of Object.entries(BUCKETS)) {
    if (pattern.test(g)) matched.push(name);
  }
  // A station with no genre tag at all is still worth considering if it ranks
  // well — the tag is missing, not negative.
  if (matched.length === 0 && !g.trim()) matched.push('untagged');
  return matched;
}

const db = new DatabaseSync(dbPath, { readOnly: true });

const rows = db.prepare(`
  select Name, Country, Genres, Rating, Likes, Dislikes, StationStreamUrl url
  from RadioStationInfos
  where StationProcessed = 1
    and StationStreamUrl is not null and StationStreamUrl <> ''
`).all();

console.error(`${rows.length} processed rows with a stream URL`);

const scored = rows.map((r) => {
  const genres = r.Genres || '';
  const score = wilsonLowerBound(r.Likes, r.Dislikes)
    // Rating enters only as a mild tiebreak, and only when it is not the
    // "unrated" sentinel. At most 0.02 it can reorder near-ties and nothing else.
    + (r.Rating > 0 ? (r.Rating / 100) * 0.02 : 0);

  return {
    name: r.Name,
    country: r.Country,
    genres,
    rating: r.Rating,
    likes: r.Likes,
    dislikes: r.Dislikes,
    url: r.url,
    source: 'db',
    score,
    buckets: EXCLUDE.test(genres) && !/\b(rock|metal|pop)\b/i.test(genres) ? [] : bucketsFor(genres),
  };
});

const out = [];
const countries = [...new Set(scored.map((r) => r.country))].sort();

for (const country of countries) {
  for (const bucket of [...Object.keys(BUCKETS), 'untagged']) {
    const picks = scored
      .filter((r) => r.country === country && r.buckets.includes(bucket))
      .sort((a, b) => b.score - a.score || b.likes - a.likes)
      .slice(0, LIMIT);

    if (picks.length) {
      console.error(`  ${country.padEnd(16)} ${bucket.padEnd(9)} ${picks.length}`);
      out.push(...picks.map((p) => ({ ...p, bucket })));
    }
  }
}

// De-dupe: a station tagged "80s | Rock" lands in two buckets. Keep the first
// (buckets are ordered rock, pop, retro) so the probe only visits it once.
const seen = new Set();
const unique = out.filter((r) => (seen.has(r.url) ? false : (seen.add(r.url), true)));

writeFileSync(outPath, JSON.stringify(unique, null, 2));
console.error(`\n${unique.length} unique candidates written to ${outPath}`);

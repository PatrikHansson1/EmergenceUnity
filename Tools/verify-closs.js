// EMERGENCE — C-LOSS WITNESS: independent node verification (Fas 7, D-182 follow-up).
//
// Purpose: the INDEPENDENT RECOMPUTATION half of the C-loss witness (TD085-TD086-REVIEW demanded
// content assertions on pop/maxGen/avgAge/crafts/knows — and that node evidence lands as a FILE in
// Reports/, not as commit-message prose). This script:
//   1. re-runs the engine (StreamingAssets twin) for <seed> to <yPre> and <yPost>;
//   2. recomputes per-village {pop,maxGen,avgAge,crafts,knows} with ITS OWN aggregation code
//      (local villageRaw reimplementation + local union/round — no call to E.villageScope);
//   3. diffs recomputation vs E.villageScope vs the exported fixture JSON files, field for field;
//   4. asserts the LOSS: <village> knows[] shrinks strictly between the two years while pop holds;
//   5. quotes the engine's OWN loss narration (knowledgeLost events) around the loss year.
// Pure READ (D-078 r4): consumes no S.rand beyond the sim's own deterministic run.
// usage: node verify-closs.js <seed> <yPre> <yPost> <villageName>
// writes: ../Reports/fas7-closs-node.txt
'use strict';
const FS = require('fs'), PATH = require('path');
const E = require(PATH.join(__dirname, '..', 'Assets', 'StreamingAssets', 'Emergence', 'emergence-engine.js'));
const seed = parseInt(process.argv[2], 10), yPre = parseInt(process.argv[3], 10), yPost = parseInt(process.argv[4], 10);
const villageName = process.argv[5];
if (!seed || !yPre || !yPost || !villageName) { console.error('usage: node verify-closs.js <seed> <yPre> <yPost> <villageName>'); process.exit(1); }

const VILLAGE_RADIUS = 18; // mirrored constant — the membership law (engine line: villageRaw)
function myScope(S) {
  // INDEPENDENT aggregation: same membership LAW, own code — never calls E.villageScope.
  return S.villages.map(v => {
    const mem = S.agents.filter(a => {
      if (a.dead) return false;
      const px = a.home ? a.home.x : a.x, py = a.home ? a.home.y : a.y;
      let best = null, bd = 1e9;
      for (const q of S.villages) { const d = Math.hypot(px - q.x, py - q.y); if (d < bd) { bd = d; best = q; } }
      return bd <= VILLAGE_RADIUS && best === v;
    });
    const ku = new Set(); for (const a of mem) for (const k of a.knows) ku.add(k);
    const knows = E.TECHS.filter(t => ku.has(t.id)).map(t => t.id);
    return { name: '' + v.name, pop: mem.length, maxGen: mem.reduce((m, a) => Math.max(m, a.gen), 0),
             avgAge: mem.length ? Math.round(mem.reduce((s, a) => s + a.age, 0) / mem.length) : 0,
             crafts: knows.length, knows };
  });
}
function runTo(years) { const S = E.createWorld(seed); S.silent = true; while (S.tick < years * E.YEAR && !S.ended) E.tickWorld(S); return S; }
function fx(years) {
  const p = PATH.join(__dirname, '..', 'Assets', 'Emergence', 'WorldStates', `world-${seed}-y${years}-e15.json`);
  return JSON.parse(FS.readFileSync(p, 'utf8'));
}
const out = []; let fails = 0;
function check(ok, msg) { out.push((ok ? 'OK   ' : 'FAIL ') + msg); if (!ok) fails++; }
function eq(a, b) { return JSON.stringify(a) === JSON.stringify(b); }
function fields(v) { return { name: v.name, pop: v.pop, maxGen: v.maxGen, avgAge: v.avgAge, crafts: v.crafts, knows: v.knows }; }

out.push('C-LOSS WITNESS — independent node verification');
out.push(`generated ${new Date().toISOString()}  engine VERSION=${E.VERSION}  seed=${seed}  yPre=${yPre}  yPost=${yPost}  village=${villageName}`);
out.push('');
for (const [tag, yr] of [['PRE', yPre], ['POST', yPost]]) {
  const S = runTo(yr), mine = myScope(S), theirs = E.villageScope(S), file = fx(yr).villages;
  check(mine.length === theirs.length && theirs.length === file.length, `${tag} y${yr}: village count mine=${mine.length} scope=${theirs.length} file=${file.length}`);
  for (let i = 0; i < mine.length; i++) {
    const named = theirs[i] ? Object.assign({ name: '' + S.villages[i].name }, theirs[i]) : null;
    check(eq(fields(mine[i]), fields({ name: '' + S.villages[i].name, pop: theirs[i].pop, maxGen: theirs[i].maxGen, avgAge: theirs[i].avgAge, crafts: theirs[i].crafts, knows: theirs[i].knows })),
      `${tag} y${yr} [${mine[i].name}] independent recompute == E.villageScope  {pop:${mine[i].pop},maxGen:${mine[i].maxGen},avgAge:${mine[i].avgAge},crafts:${mine[i].crafts}}`);
    check(eq(fields(mine[i]), fields(file[i])),
      `${tag} y${yr} [${mine[i].name}] independent recompute == exported fixture file (driver-parity chain)`);
    check(mine[i].crafts === mine[i].knows.length, `${tag} y${yr} [${mine[i].name}] contract: crafts==knows.length (${mine[i].crafts})`);
  }
  out.push('  ' + tag + ' y' + yr + ': ' + mine.map(v => `${v.name} pop=${v.pop} maxGen=${v.maxGen} avgAge=${v.avgAge} crafts=${v.crafts}`).join(' | '));
}
const Spre = runTo(yPre), Spost = runTo(yPost);
const pre = myScope(Spre).find(v => v.name === villageName), post = myScope(Spost).find(v => v.name === villageName);
check(!!pre && !!post, `village '${villageName}' present both years`);
const lost = pre.knows.filter(k => !post.knows.includes(k));
const gained = post.knows.filter(k => !pre.knows.includes(k));
check(lost.length > 0 && gained.length === 0, `THE LOSS: ${villageName} knows shrinks STRICTLY: ${pre.crafts} -> ${post.crafts}, lost=[${lost.join(', ')}], gained=[${gained.join(', ')}]`);
check(post.pop > 0 && pre.pop === post.pop, `pop holds (${pre.pop} -> ${post.pop}) — knowledge died, not the village`);
const others = myScope(Spost).filter(v => v.name !== villageName && v.pop > 0);
check(others.some(v => v.crafts !== post.crafts), `DISTINCTION: at least one other living village differs in crafts at y${yPost} (${others.map(v => v.name + '=' + v.crafts).join(', ')} vs ${villageName}=${post.crafts})`);
out.push('');
out.push('## THE ENGINE\'S OWN WITNESS (knowledgeLost events, y' + (yPre - 1) + '–y' + (yPost + 1) + ')');
const strip = s => s.replace(/<[^>]+>/g, '');
const evs = Spost.events.filter(e => e.type === 'knowledgeLost' && e.year >= yPre - 1 && e.year <= yPost + 1);
for (const e of evs) out.push('  y' + e.year + ': ' + strip(e.txt));
if (evs.length === 0) out.push('  (none in window — the shrink is a village-level census change; world-level narration may sit elsewhere)');
out.push('');
out.push('verdict: ' + (fails === 0 ? 'GREEN' : 'FAIL(' + fails + ')'));
FS.writeFileSync(PATH.join(__dirname, '..', 'Reports', 'fas7-closs-node.txt'), out.join('\n') + '\n');
console.log(out.join('\n'));
process.exit(fails === 0 ? 0 : 1);

// POST 3 — SPARA/LADDA SKARPT. Kön är ren data sedan D-266 (typ-tagg + argument).
// Nu bevisas det: serialisera vid år 1500, ladda i en NY process-instans, kör till
// 3000, och kräv identisk invariantvektor mot en obruten körning.
'use strict';
const fs = require('fs');
const SRC = fs.readFileSync('./spike-v9.js', 'utf8');
function fresh() { const m = { exports: {} }; new Function('module','exports','require','process',SRC)(m,m.exports,require,process); return m.exports; }

const M = fresh();

function inv(W) {
  const ys = W.procedures.map(p => p.year);
  return JSON.stringify({ n: W.procedures.length, last: Math.max(...ys), pop: W.pop,
    mats: W.mats.length, dims: W.visibleDims, heat: W.heatCap, epoch: W.epoch,
    demand: [...W.demand].sort(), best: W.best,
    tail: W.procedures.slice(-10).map(p => p.name + '@' + p.year) });
}
function serialize(W) {
  return JSON.stringify({ seed: W.seed, now: W.now, year: W.year, pop: W.pop,
    visibleDims: W.visibleDims, instruments: W.instruments, heatCap: W.heatCap, epoch: W.epoch,
    methods: W.methods, demand: [...W.demand], memo: [...W.memo], best: W.best,
    roleBest: W.roleBest, seenCombos: [...W.seenCombos], bestDecay: W.bestDecay,
    roleOpen: W.roleOpen, roleQuality: W.roleQuality, roleThresh: W.roleThresh,
    mats: W.mats, procedures: W.procedures.map(p => ({ ...p, inputs: p.inputs.map(i => i.name) })),
    procKey: [...W.procKey], seedTriples: [...W.seedTriples], stats: W.stats,
    queue: W.q.h.map(e => ({ tick: e.tick, cls: e.cls, id: e.id, seq: e.seq, type: e.type, arg: e.arg })),
    qseq: W.q.seq });
}
function deserialize(M, blob) {
  const d = JSON.parse(blob);
  const W = M.createWorld(d.seed, {});
  Object.assign(W, { now: d.now, year: d.year, pop: d.pop, visibleDims: d.visibleDims,
    instruments: d.instruments, heatCap: d.heatCap, epoch: d.epoch, methods: d.methods,
    best: d.best, roleBest: d.roleBest, bestDecay: d.bestDecay, roleOpen: d.roleOpen,
    roleQuality: d.roleQuality, roleThresh: d.roleThresh, mats: d.mats, stats: d.stats });
  W.demand = new Set(d.demand); W.memo = new Map(d.memo); W.seenCombos = new Set(d.seenCombos);
  W.procKey = new Set(d.procKey); W.seedTriples = new Set(d.seedTriples);
  const byName = {}; for (const m of d.mats) byName[m.name] = m;
  W.procedures = d.procedures.map(p => ({ ...p, inputs: p.inputs.map(n => byName[n] || { name: n, d: {} }) }));
  W.q.h = d.queue.slice(); W.q.seq = d.qseq;
  return W;
}

// A: obruten körning till 3000
const A = M.runTo ? null : null;
const full = M.run(4242, 3000);

// B: kör till 1500, spara, ladda i FÄRSK modul, fortsätt till 3000
const half = M.run(4242, 1500);
const blob = serialize(half);
const M2 = fresh();
const loaded = deserialize(M2, blob);
// fortsätt: samma loop som run(), men från laddat tillstånd
while (loaded.year < 3000) { const e = loaded.q.pop(); if (!e) break; loaded.now = e.tick; loaded.stats.events++; M2.HANDLER ? M2.HANDLER[e.type](loaded, e.arg) : null; }

console.log('POST 3 — SPARA/LADDA (tal 5)\n');
console.log('  sparat vid år', half.year, '·', (blob.length/1024).toFixed(0), 'kB ·', half.q.size, 'köposter');
console.log('  köposter som bär en closure:', half.q.h.filter(e => typeof e.fn === 'function').length, '(var 3 av 3 i v3)');
const ok = M2.HANDLER ? inv(full) === inv(loaded) : null;
if (ok === null) { console.log('\n  HANDLER exporteras inte — kan inte köra vidare efter laddning.'); console.log('  FYND: dispatch-tabellen måste exporteras för att en laddad värld ska kunna fortsätta.'); }
else { console.log('\n  invariantvektor obruten == laddad:', ok ? '✓ IDENTISK' : '✗ SKILJER'); if (!ok) { console.log('  obruten:', inv(full).slice(0,180)); console.log('  laddad :', inv(loaded).slice(0,180)); } }

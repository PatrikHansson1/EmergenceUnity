// ★ T3 — ABLATIONEN. Masterplan v2 §4: en mekanism är trovärdig bara om nollningen
// ändrar den invariant den ska styra OCH INTE de andra. Och den är samtidigt
// läckagedetektorn: försvinner utfallet HELT när en regel stängs av, VAR den regeln utfallet.
'use strict';
const fs = require('fs');
const SRC = fs.readFileSync('./spike-v5.js', 'utf8');

function variant(name, patch) {
  const code = patch(SRC);
  const mod = { exports: {} };
  new Function('module', 'exports', 'require', 'process', code)(mod, mod.exports, require, process);
  return { name, run: mod.exports.run };
}

const V = [
  variant('FULL  (allt på)', s => s),
  variant('utan EFTERFRÅDAN (klausul d)', s => s.replace(
    "  for (const a of c.aff) if (W.demand.has(a) && sc >= (W.best[a] || 0) - 2) return 'd';", "")),
  variant('utan ROLLPLATS (klausul c)', s => s.replace(
    "    if (s2 > (W.roleBest[part.role] || 0)) return 'c';   // strikt bättre än förra bäraren av rollen", "")),
  variant('utan INSTRUMENT (dimensioner låsta)', s => s.replace(
    "  W.visibleDims++; W.instruments++;", "  W.instruments++;")),
  variant('utan TEMPERATURSTEGEN', s => s.replace(
    "  if (proc.tier !== undefined && proc.tier > W.heatCap) { if (W.methods.written) W.memo.add(tk); return null; }", "")),
  variant('utan MEMOISERING (metod 3)', s => s.replace(
    "  if (W.methods.written && W.memo.has(tk)) { W.stats.memoHits++; return null; }", "")),
];

const SEEDS = [4242, 777, 1234];
console.log('T3 ABLATION — 3 seeds × 3000 år\n');
console.log('  variant                              procedurer   sista år   lucka   0-30   (2000,3000]');
console.log('  ' + '-'.repeat(94));
const base = {};
for (const v of V) {
  const rows = SEEDS.map(s => {
    const W = v.run(s, 3000);
    const ys = W.procedures.map(p => p.year).sort((a, b) => a - b);
    let g = 0; for (let i = 1; i < ys.length; i++) if (ys[i] - ys[i - 1] > g) g = ys[i] - ys[i - 1];
    return { n: W.procedures.length, last: ys[ys.length - 1] || 0, gap: g,
             e: W.procedures.filter(p => p.year <= 30).length,
             l: W.procedures.filter(p => p.year > 2000).length };
  });
  const avg = k => Math.round(rows.reduce((a, r) => a + r[k], 0) / rows.length);
  const r = { n: avg('n'), last: avg('last'), gap: avg('gap'), e: avg('e'), l: avg('l') };
  if (v.name.startsWith('FULL')) Object.assign(base, r);
  const d = k => { const p = Math.round((r[k] - base[k]) / Math.max(1, base[k]) * 100); return v.name.startsWith('FULL') ? '' : ` (${p >= 0 ? '+' : ''}${p}%)`; };
  console.log(`  ${v.name.padEnd(36)} ${String(r.n).padStart(7)}${d('n').padEnd(8)} ${String(r.last).padStart(7)} ${String(r.gap).padStart(7)} ${String(r.e).padStart(6)} ${String(r.l).padStart(12)}`);
}
console.log('\n  LÄSNING: en mekanism som INTE ändrar något är dekoration och ska strykas.');
console.log('  En mekanism vars nollning får utfallet att FÖRSVINNA HELT var själv utfallet.');
console.log('  Det man vill se är att varje ben bär NÅGOT och att inget ben bär ALLT.');

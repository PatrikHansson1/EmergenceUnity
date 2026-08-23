// POST 1 — KALIBRERA HUVUDRATTEN. M3: halva repertoaren ska anlända år 400–900.
// Mätvärdet är MEDIANÅRET, inte totalen — en ratt som ger fler procedurer men alla
// tidigt har inte flyttat bågen, den har bara gjort flodvågen större.
'use strict';
const fs = require('fs');
const SRC = fs.readFileSync('./spike-v9.js', 'utf8');
const M = (() => { const m = { exports: {} }; new Function('module','exports','require','process',SRC)(m,m.exports,require,process); return m.exports; })();
const SEEDS = [4242, 777, 1234];

function measure(opts) {
  const rows = SEEDS.map(s => {
    const W = M.run(s, 3000, opts);
    const ys = W.procedures.map(p => p.year).sort((a, b) => a - b);
    let g = 0; for (let i = 1; i < ys.length; i++) if (ys[i] - ys[i - 1] > g) g = ys[i] - ys[i - 1];
    const half = ys[Math.floor(ys.length / 2)] || 0;
    return { n: ys.length, half, gap: g, early: ys.filter(y => y <= 30).length,
             last: ys.filter(y => y > 2000).length, ms: W.ms };
  });
  const a = k => Math.round(rows.reduce((x, r) => x + r[k], 0) / rows.length);
  return { n: a('n'), half: a('half'), gap: a('gap'), early: a('early'), last: a('last'), ms: a('ms') };
}

console.log('POST 1 — KALIBRERING AV bestDecay   3 seeds × 3000 år');
console.log('M3-mål: MEDIANÅRET (halva repertoaren) i intervallet 400–900\n');
console.log('  decay   proc.   MEDIANÅR   lucka   år0-30   (2000,3000]   ms/körning   M3');
console.log('  ' + '-'.repeat(84));
const cand = [];
for (let d = 100; d >= 80; d -= 2) {
  const dec = d / 100;
  const r = measure({ bestDecay: dec });
  const m3 = r.half >= 400 && r.half <= 900;
  const m10 = r.early >= 6;
  if (m3 && m10) cand.push({ dec, ...r });
  console.log(`  ${dec.toFixed(2)}   ${String(r.n).padStart(5)}   ${String(r.half).padStart(8)}   ${String(r.gap).padStart(5)}   ${String(r.early).padStart(6)}   ${String(r.last).padStart(11)}   ${String(r.ms).padStart(10)}   ${m3 ? '✓' : ' '}${m10 ? '' : ' (M10 röd)'}`);
}
console.log('\n  KANDIDATER (M3 och M10 gröna):', cand.length ? cand.map(c => c.dec.toFixed(2)).join(' · ') : 'INGEN');
if (cand.length) {
  // välj den som ger minst lucka bland kandidaterna — jämnast båge
  const best = cand.slice().sort((a, b) => a.gap - b.gap || a.n - b.n)[0];
  console.log(`  REKOMMENDERAT: bestDecay = ${best.dec.toFixed(2)}  (medianår ${best.half}, ${best.n} procedurer, lucka ${best.gap} år)`);
}

// POST 2 — bidrar öppnaren c1 något när ratten fungerar?
console.log('\n\nPOST 2 — BIDRAR c1 (ÖPPNAREN) NÄR bestDecay < 1?');
console.log('  decay   med c1   utan c1   skillnad');
console.log('  ' + '-'.repeat(44));
for (const dec of [1.00, 0.94, 0.90, 0.86]) {
  const withC1 = measure({ bestDecay: dec });
  const noC1 = measure({ bestDecay: dec, roleOpen: false });
  const d = Math.round((withC1.n - noC1.n) / Math.max(1, noC1.n) * 100);
  console.log(`  ${dec.toFixed(2)}   ${String(withC1.n).padStart(6)}   ${String(noC1.n).padStart(7)}   ${(d >= 0 ? '+' : '') + d}%`);
}

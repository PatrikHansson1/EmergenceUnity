// NÄSTA post 1 — ÄR ROLLPLATS OCH MEMOISERING BROMSAR ELLER ÖPPNARE?
// Binär av/på räcker inte: en mekanism kan öppna svagt och bromsa starkt.
// Därför STYRKESVEP — samma mekanism vid flera nivåer, samma seeds.
'use strict';
const fs = require('fs');
const SRC = fs.readFileSync('./spike-v7.js', 'utf8');
const SEEDS = [4242, 777, 1234];

function build(patch) {
  const mod = { exports: {} };
  new Function('module', 'exports', 'require', 'process', patch(SRC))(mod, mod.exports, require, process);
  return mod.exports.run;
}
function measure(run) {
  const rows = SEEDS.map(s => {
    const W = run(s, 3000);
    const ys = W.procedures.map(p => p.year).sort((a, b) => a - b);
    let g = 0; for (let i = 1; i < ys.length; i++) if (ys[i] - ys[i - 1] > g) g = ys[i] - ys[i - 1];
    return { n: W.procedures.length, gap: g, last: ys[ys.length - 1] || 0,
             l: W.procedures.filter(p => p.year > 2000).length };
  });
  const a = k => Math.round(rows.reduce((x, r) => x + r[k], 0) / rows.length);
  return { n: a('n'), gap: a('gap'), last: a('last'), l: a('l') };
}

console.log('STYRKESVEP — broms eller öppnare?   3 seeds × 3000 år\n');

// --- ROLLPLATS: tröskeln för hur mycket bättre ett material måste vara för att fylla en roll ---
console.log('## ROLLPLATS (klausul c) — tröskelns styrka');
console.log('   tröskel   procedurer   lucka   sista år   (2000,3000]');
for (const t of ['AV', 0, 2, 5, 10, 20]) {
  const run = t === 'AV'
    ? build(s => s.replace("    if (s2 > (W.roleBest[part.role] || 0)) return 'c';   // strikt bättre än förra bäraren av rollen", ""))
    : build(s => s.replace("if (s2 > (W.roleBest[part.role] || 0)) return 'c';", `if (s2 > (W.roleBest[part.role] || 0) + ${t}) return 'c';`));
  const r = measure(run);
  console.log(`   ${String(t).padStart(7)}   ${String(r.n).padStart(10)}   ${String(r.gap).padStart(5)}   ${String(r.last).padStart(8)}   ${String(r.l).padStart(11)}`);
}

// --- MEMOISERING: när metoden anländer (tidigt = starkare) ---
console.log('\n## MEMOISERING (metod 3) — när den anländer');
console.log('   anländer   procedurer   lucka   sista år   (2000,3000]');
for (const y of ['ALDRIG', 50, 200, 400, 1000, 2000]) {
  const run = y === 'ALDRIG'
    ? build(s => s.replace("  if (W.methods.written && W.memo.has(tk)) { W.stats.memoHits++; return null; }", ""))
    : build(s => s.replace("W.q.push(400 * YEAR, CLASS.THRESHOLD, 2, EV.METHOD, 1);", `W.q.push(${y} * YEAR, CLASS.THRESHOLD, 2, EV.METHOD, 1);`));
  const r = measure(run);
  console.log(`   ${String(y).padStart(8)}   ${String(r.n).padStart(10)}   ${String(r.gap).padStart(5)}   ${String(r.last).padStart(8)}   ${String(r.l).padStart(11)}`);
}
console.log('\n  LÄSNING: monotont fallande med styrkan = BROMS. Stigande = ÖPPNARE.');
console.log('  Icke-monotont = mekanismen gör två saker och ska delas i två.');

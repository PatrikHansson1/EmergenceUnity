// M3 ÄR FEL MÅTT FÖR EN GENERATIV MOTOR — och det syns i talen: medianåret ligger
// på ~1800 oavsett bestDecay (1,00 → 0,80), för ankomsten är en konstant process
// och medianen av en likformig ankomst ÄR mittpunkten, per konstruktion.
// "Halva repertoaren" var skrivet för ett ÄNDLIGT träd på 53 noder. En generativ
// repertoar har ingen halva.
//
// RÄTT MÅTT: FÖRMÅGEFRONTEN. När når världen första gången varje förmåga?
// Det är det som motsvarar "nådde de järnåldern", och det går att kalibrera mot
// verklig historia — till skillnad från en median över ett obegränsat rum.
'use strict';
const fs = require('fs');
const SRC = fs.readFileSync('./spike-v9.js', 'utf8');
const M = (() => { const m = { exports: {} }; new Function('module','exports','require','process',SRC)(m,m.exports,require,process); return m.exports; })();
const SEEDS = [4242, 777, 1234, 8919, 56433];
const AFF = ['cut','hold','bind','burn','build','carry','grind','pierce','contain','spring','see','conduct'];

function frontier(opts) {
  const first = {};
  for (const s of SEEDS) {
    const W = M.run(s, 3000, opts);
    for (const p of W.procedures) for (const a of p.aff)
      if (first[a] === undefined || p.year < first[a]) { if (!first[a] || p.year < first[a]) first[a] = p.year; }
  }
  return first;
}
function frontierPerSeed(opts) {
  return SEEDS.map(s => {
    const W = M.run(s, 3000, opts);
    const f = {};
    for (const p of W.procedures) for (const a of p.aff) if (f[a] === undefined || p.year < f[a]) f[a] = p.year;
    return f;
  });
}

console.log('FÖRMÅGEFRONTEN — vilket år når världen varje förmåga första gången?');
console.log('5 seeds × 3000 år, bestDecay 0.94\n');
const per = frontierPerSeed({ bestDecay: 0.94 });
console.log('  förmåga     median    spann över seeds        nås i');
console.log('  ' + '-'.repeat(62));
for (const a of AFF) {
  const ys = per.map(f => f[a]).filter(v => v !== undefined).sort((x, y) => x - y);
  if (!ys.length) { console.log(`  ${a.padEnd(10)}      —      NÅS ALDRIG i någon värld`); continue; }
  const med = ys[Math.floor(ys.length / 2)];
  console.log(`  ${a.padEnd(10)} ${String(med).padStart(6)}    ${String(ys[0]).padStart(4)}–${String(ys[ys.length-1]).padEnd(5)}          ${ys.length}/5 världar`);
}

console.log('\n\n★ KONTINGENSMÅTTET — skiljer sig ORDNINGEN mellan världar?');
console.log('  (flaggskeppsfrågan i miniatyr: nödvändig eller kontingent ordning?)\n');
const orders = per.map(f => AFF.filter(a => f[a] !== undefined).sort((x, y) => f[x] - f[y]));
for (let i = 0; i < orders.length; i++) console.log(`  seed ${String(SEEDS[i]).padStart(5)}: ${orders[i].join(' → ')}`);
// Kendalls tau mellan seed 0 och de andra
function tau(A, B) {
  const common = A.filter(x => B.includes(x));
  let c = 0, d = 0;
  for (let i = 0; i < common.length; i++) for (let j = i + 1; j < common.length; j++) {
    const a1 = A.indexOf(common[i]) - A.indexOf(common[j]);
    const b1 = B.indexOf(common[i]) - B.indexOf(common[j]);
    if (a1 * b1 > 0) c++; else if (a1 * b1 < 0) d++;
  }
  return c + d ? ((c - d) / (c + d)) : 1;
}
const taus = orders.slice(1).map(o => tau(orders[0], o));
console.log('\n  Kendalls tau mot seed 4242:', taus.map(t => t.toFixed(2)).join(' · '));
console.log('  medel:', (taus.reduce((a, b) => a + b, 0) / taus.length).toFixed(2));
console.log('\n  τ nära 1,00 = ordningen är NÖDVÄNDIG (beroendestyrd).');
console.log('  τ nära 0,00 = ordningen är KONTINGENT (slumpen avgör).');
console.log('  Det verkliga svaret ligger däremellan, och GRÄNSEN är uppsatsen.');

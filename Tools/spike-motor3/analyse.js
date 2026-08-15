// SPIKENS SEX TAL, mätta — och skepsisen mot det egna resultatet.
// Att rummet inte STÄNGER är hälften av provet. Andra hälften är att det inte
// SVÄMMAR ÖVER: tusen procedurer utan mening är No Man's Sky i annan förklädnad.
'use strict';
const { run, FORM, DIM } = require('./spike.js');

const YEARS = parseInt(process.argv[2] || '3000', 10);
const SEEDS = (process.argv[3] || '4242,777,1234,8919,56433').split(',').map(Number);
const bar = (n, max, w = 34) => '█'.repeat(Math.max(0, Math.round(n / Math.max(1, max) * w)));

console.log(`SPIKENS TAL — ${SEEDS.length} seeds × ${YEARS} år\n`);
const worlds = SEEDS.map(s => run(s, YEARS));

// ---- FORM: växer rummet, eller svämmar det över? ----
console.log('## UPPTÄCKTSKURVANS FORM (procedurer per sekel, seed ' + SEEDS[0] + ')');
const W0 = worlds[0];
const cent = {};
for (const p of W0.procedures) { const c = Math.floor(p.year / 100) * 100; cent[c] = (cent[c] || 0) + 1; }
const ks = Object.keys(cent).map(Number).sort((a, b) => a - b);
const mx = Math.max(...Object.values(cent));
for (const k of ks) if (k % 200 === 0) console.log(`  år ${String(k).padStart(4)}  ${bar(cent[k], mx)} ${cent[k]}`);

// ---- SKÄLEN: skiftar de över tid? (a=ny bäst · b=nytt predikat · c=roll · d=efterfrågan) ----
console.log('\n## VARFÖR EN PROCEDUR BEHÖLLS — skiftar skälet över tid?');
for (const band of [[0, 500], [500, 1500], [1500, 3000]]) {
  const inBand = W0.procedures.filter(p => p.year >= band[0] && p.year < band[1]);
  const c = {}; for (const p of inBand) c[p.why] = (c[p.why] || 0) + 1;
  const tot = inBand.length || 1;
  console.log(`  år ${String(band[0]).padStart(4)}–${String(band[1]).padStart(4)}  n=${String(tot).padStart(4)}   ` +
    ['a', 'b', 'c', 'd'].map(x => `${x}:${String(Math.round((c[x] || 0) / tot * 100)).padStart(3)}%`).join('  '));
}

// ---- MENINGSFULLHET: bär de olika affordanser, eller är de brus? ----
console.log('\n## MENING — bär procedurerna olika förmågor eller är de brus?');
const affCount = {}, formCount = {};
for (const p of W0.procedures) { for (const a of p.aff) affCount[a] = (affCount[a] || 0) + 1; formCount[FORM[p.form]] = (formCount[FORM[p.form]] || 0) + 1; }
const affKeys = Object.keys(affCount).sort((a, b) => affCount[b] - affCount[a]);
console.log('  distinkta affordanser i bruk: ' + affKeys.length + ' av 12   ·   distinkta former: ' + Object.keys(formCount).length + ' av ' + FORM.length);
console.log('  ' + affKeys.map(a => `${a}:${affCount[a]}`).join(' · '));
const withComp = W0.procedures.filter(p => p.comp).length;
console.log(`  sammansatta (roll-graf): ${withComp} (${Math.round(withComp / W0.procedures.length * 100)} %) — kombinatoriken som inte mättar`);

// ---- ADAPTIVITETEN: den enda egenskap en tickmotor omöjligt kan ha ----
console.log('\n## ★ ADAPTIVITETEN — kostar ett tyst sekel mindre än ett dramatiskt?');
{
  const W = run(SEEDS[0], YEARS);
  const perC = {}; for (const p of W.procedures) { const c = Math.floor(p.year / 100); perC[c] = (perC[c] || 0) + 1; }
  const vals = Object.values(perC);
  const quiet = Math.min(...vals), loud = Math.max(...vals);
  // händelser är proportionella mot arbete; räkna dem per sekel
  console.log(`  tystaste seklet: ${quiet} procedurer · bullrigaste: ${loud}   kvot ${(loud / Math.max(1, quiet)).toFixed(1)}×`);
  console.log(`  händelser totalt: ${W.stats.events} på ${YEARS} år = ${(W.stats.events / YEARS).toFixed(2)}/år`);
  console.log('  (en TICKMOTOR skulle kosta 144 tickar × pop varje år oavsett — kvoten är där 1,0)');
}

// ---- SKALNINGEN: kostnaden mot folkmängd ----
console.log('\n## SKALNING — kostnad mot folkmängd (mål: exponent ≤ 1,0)');
{
  const pts = [];
  for (const cap of [1, 2, 4, 8]) {
    const W = run(SEEDS[0], 600, { popScale: cap });
    // popScale är inte inkopplad i spiken; vi mäter i stället kostnad mot ANTAL PROCEDURER,
    // som är den storhet som faktiskt växer och som söket skalar mot
    pts.push([W.procedures.length, W.ms]);
  }
  console.log('  (spiken har ingen agentbefolkning att skala — mäts i fas 1 mot riktig pop)');
}

// ---- SEX TALEN ----
console.log('\n' + '='.repeat(78));
console.log('## DE SEX TALEN');
let pass = 0, tot = 0;
const judge = (n, name, ok, got, want) => { tot++; if (ok) pass++; console.log(`  ${ok ? '✓' : '✗'} ${n}. ${name.padEnd(42)} ${String(got).padStart(14)}   (${want})`); };

const msPerYear = worlds.map(w => w.ms / YEARS);
judge(1, 'kostnad per simulerat år (ms)', Math.max(...msPerYear) < 50,
  msPerYear.map(m => m.toFixed(2)).join('/'), 'mål < 50 ms');

const novel = worlds.map(w => w.procedures.filter(p => !w.seedTriples.has(p.tk)).length);
judge(2, 'procedurer utanför sådden', Math.min(...novel) >= 20, Math.min(...novel) + '–' + Math.max(...novel), '≥ 20');

const gaps = worlds.map(w => { const ys = w.procedures.map(p => p.year).sort((a, b) => a - b); let g = 0; for (let i = 1; i < ys.length; i++) if (ys[i] - ys[i - 1] > g) g = ys[i] - ys[i - 1]; return g; });
const ratios = worlds.map(w => { const a = w.procedures.filter(p => p.year <= 1000).length, b = w.procedures.filter(p => p.year > 2000).length; return b / Math.max(1, a); });
judge(3, 'ingen lucka > 300 år', Math.max(...gaps) <= 300, Math.max(...gaps) + ' år', '≤ 300');
judge(3, 'sista tredjedelen mot första', Math.min(...ratios) >= 0.25, (Math.min(...ratios) * 100).toFixed(0) + '%', '≥ 25 %');

const early = worlds.map(w => w.procedures.filter(p => p.year <= 30).length);
judge(4, 'M10 överlever: år 0–30', Math.min(...early) >= 6, Math.min(...early) + '–' + Math.max(...early), '≥ 6');

console.log(`\n  ${pass}/${tot} tal gröna i denna körning. Tal 5 (spara/ladda) och 6 (läckage) körs separat.`);
console.log('\n  SKEPSIS MOT DET EGNA RESULTATET, utskriven:');
console.log('  · spiken har ingen agentbefolkning, ingen terräng, ingen kultur — kostnadstalet');
console.log('    är därför INTE jämförbart med motorns 0,855 s/år. Det som är jämförbart är FORMEN.');
console.log('  · "utanför sådden" är ett generöst mått så länge sådd-mängden är liten. Det tal');
console.log('    som faktiskt bär beviset är LUCKAN och SISTA TREDJEDELEN — att rummet inte stänger.');
console.log('  · och det motsatta felet mäts ovan: svämmar rummet över utan mening?');

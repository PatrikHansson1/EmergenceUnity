// EMERGENCE — RUN_CALIBRATE (D-259, port P0). Mätstickan varje inkrement bedöms mot.
//
// VARFÖR DEN FINNS: en 3000-årsmotor som inte går att kontrollera är en 3000-årsmotor som har fel.
// Den här proben ställer VÅR värld bredvid de tal arkeologin och kliodynamiken faktiskt har mätt,
// och skriver ut båda. Den bevisar ingenting; den gör avvikelsen SYNLIG, vilket är det enda en
// mätsticka ska göra.
//
// KONSTITUTIONEN STÅR ÖVER DEN HÄR FILEN, och det är värt att skriva rakt in i verktyget:
//   "Litteraturen vinner aldrig över världen."
// Kalibreringstalen är en spegel, inte ett facit. När vår värld avviker OCH ändå är bättre är
// avvikelsen ett fynd, inte ett fel. Ingen rad här får någonsin användas för att tvinga fram ett
// utfall — bara för att upptäcka att en REGEL producerar något orimligt.
//
// LÄXAN SOM BYGGDE IN SIG I FILEN (D-258): forskningens mål för uppgång-och-fall är CV >= 0,25.
// Vår monotona värld mäter CV 0,55-0,70 — den KLARAR målet och svänger ändå inte en enda gång.
// Ett mått som godkänner en rak ramp som en svängning mäter fel sak. Därför räknar den här proben
// alltid FORMEN (verkliga nedgångar, cykellängd) bredvid spridningen, och rapporterar bägge.
//
// Kör:  node Tools/probe-calibrate.js [år] [seeds...]
// t.ex. node Tools/probe-calibrate.js 300 4242 777 1234 8919 56433
'use strict';
const fs = require('fs'), path = require('path');

const ENGINE = process.env.EMERGENCE_ENGINE ||
  path.join(__dirname, '..', 'Assets', 'StreamingAssets', 'Emergence', 'emergence-engine.js');
const src = fs.readFileSync(ENGINE, 'utf8');
const mod = { exports: {} };
new Function('module', 'exports', 'window', src)(mod, mod.exports, undefined);
const E = mod.exports;

const YEARS = parseInt(process.argv[2] || '300', 10);
const SEEDS = process.argv.slice(3).map(Number);
if (!SEEDS.length) SEEDS.push(4242, 777, 1234);

// ---------------------------------------------------------------------------
// MÅLEN. Varje rad: vad verkligheten mätte, vem som mätte det, och hur säkert det är.
// `soft` = riktning snarare än tröskel. `unbuilt` = mekaniken finns inte i motorn än,
// raden står här för att den ska finnas den dag den byggs (och för att tomrummet ska synas).
// ---------------------------------------------------------------------------
const TARGETS = [
  { id:'halfTree',   label:'halva teknikträdet uppfunnet vid år',  want:'>= 400',
    src:'härlett ur Wand & Hoyer 2024 (~2500 år för en full komplexitetstraversering)' },
  { id:'lastTech',   label:'sista nya tekniken, år',               want:'> 0,6 x körningen',
    src:'innehållsbågen ska inte ta slut före världen (D-255)' },
  { id:'popCV',      label:'befolkningens variationskoefficient',  want:'0,25–0,35',
    src:'Kondor m.fl., Sci Rep 2023 (21 534 C14-dateringar, mellanholocen Europa)' },
  { id:'busts',      label:'verkliga nedgångar > 25 %',            want:'>= 1 per 500 år',
    src:'samma — OCH det är den här raden, inte CV, som avgör om världen svänger' },
  { id:'cycle',      label:'befolkningskurvans halvcykel, år',     want:'~500',
    src:'Kondor m.fl. 2023, ACF:s första minimum. Klimat ensamt ger ~200 = fel svar' },
  { id:'settLife',   label:'boplatsers medellivslängd, år',        want:'600–1100',
    src:'Lawrence m.fl., Urban Studies 2023 (497 platser, Bördiga halvmånen)', unbuilt:true },
  { id:'fallen',     label:'byar som fallit per 300 år',           want:'>= 1',
    src:'motorlane-orderns eget acceptanskriterium', unbuilt:true },
  { id:'gini',       label:'förmögenhets-Gini',                    want:'~0,25 (arbetsbegränsat)',
    src:'GINI-projektet, PNAS 2025 (47 000 hus, 10 000 år)', soft:true },
  { id:'heaps',      label:'nyheters Heaps-exponent',              want:'~0,59',
    src:'Taalbi, ICC 2026 (3 086 innovationer) — kräver generativt träd', unbuilt:true },
];

function pct(a, p) { const s = a.slice().sort((x, y) => x - y); return s[Math.min(s.length - 1, Math.floor(s.length * p))]; }

/** Formen, inte spridningen: hur många gånger vänder kurvan ner mer än `drop` från en topp? */
function bustsOf(series, drop) {
  let peak = 0, n = 0, marks = [];
  for (let i = 0; i < series.length; i++) {
    const v = series[i];
    if (v > peak) peak = v;
    if (peak > 8 && v < peak * (1 - drop)) { n++; marks.push(i); peak = v; }
  }
  return { n, marks };
}

/** Autokorrelationens första minimum — cykellängden, om det finns en cykel. */
function acfFirstMin(series) {
  const n = series.length;
  if (n < 40) return null;
  const m = series.reduce((a, b) => a + b, 0) / n;
  const d = series.map(v => v - m);
  const den = d.reduce((a, b) => a + b * b, 0);
  if (den === 0) return null;
  let prev = 1, prevLag = 0;
  for (let lag = 1; lag < Math.floor(n / 2); lag++) {
    let s = 0; for (let i = 0; i + lag < n; i++) s += d[i] * d[i + lag];
    const r = s / den;
    if (r > prev && prevLag > 0) return prevLag;   // vände upp -> föregående lag var minimum
    prev = r; prevLag = lag;
  }
  return null;
}

function run(seed) {
  const S = E.createWorld(seed); S.silent = true;
  const pop = [], vill = [];
  for (let y = 0; y < YEARS && !S.ended; y++) {
    for (let t = 0; t < E.YEAR && !S.ended; t++) E.tickWorld(S);
    let n = 0; for (const a of S.agents) if (!a.dead) n++;
    pop.push(n); vill.push(S.villages.length);
  }
  const born = [];
  for (const k in S.knowledge) born.push(S.knowledge[k].yearBorn);
  born.sort((a, b) => a - b);
  const m = pop.reduce((a, b) => a + b, 0) / pop.length;
  const sd = Math.sqrt(pop.reduce((a, b) => a + (b - m) * (b - m), 0) / pop.length);
  let gini = null;
  try {
    const w = S.agents.filter(a => !a.dead).map(a => E.wealthOf ? E.wealthOf(a) : 0).sort((x, y) => x - y);
    if (w.length > 1) {
      const tot = w.reduce((a, b) => a + b, 0);
      if (tot > 0) { let c = 0; for (let i = 0; i < w.length; i++) c += (i + 1) * w[i];
        gini = (2 * c) / (w.length * tot) - (w.length + 1) / w.length; }
    }
  } catch (e) { /* wealthOf kan saknas i äldre motorer — tystnad är rätt svar, inte en krasch */ }
  const b = bustsOf(pop, 0.25);
  return {
    seed, years: pop.length, ended: S.ended, pop, vill,
    halfTree: born.length ? born[Math.floor(born.length / 2)] : null,
    lastTech: born.length ? born[born.length - 1] : null,
    techs: born.length,
    popCV: sd / m, busts: b.n, bustYears: b.marks, cycle: acfFirstMin(pop),
    maxVill: Math.max(...vill), endVill: vill[vill.length - 1], gini,
  };
}

// ---------------------------------------------------------------------------
console.log(`EMERGENCE — RUN_CALIBRATE   motor ${E.VERSION}   ${SEEDS.length} seeds x ${YEARS} år`);
console.log(`karta ${E.W}x${E.H}, ${E.YEAR} tickar/år, ${E.TECHS.length} tekniker\n`);

const rs = SEEDS.map(s => { const t = Date.now(); const r = run(s); r.ms = Date.now() - t; return r; });

console.log('per seed:');
console.log('  seed     år   tekn  halva_trädet  sista_tech   pop_slut  CV     nedgångar  cykel  byar  s/körning');
for (const r of rs) {
  console.log(`  ${String(r.seed).padStart(6)} ${String(r.years).padStart(6)} ${String(r.techs).padStart(6)}` +
    `${String(r.halfTree).padStart(14)}${String(r.lastTech).padStart(12)}` +
    `${String(r.pop[r.pop.length-1]).padStart(11)}  ${r.popCV.toFixed(2).padStart(5)}` +
    `${String(r.busts).padStart(11)}${String(r.cycle === null ? '—' : r.cycle).padStart(7)}` +
    `${String(r.endVill).padStart(6)}${(r.ms/1000).toFixed(0).padStart(11)}`);
}

const med = k => pct(rs.map(r => r[k]).filter(v => v !== null && v !== undefined), 0.5);
const got = {
  halfTree: med('halfTree'), lastTech: med('lastTech'), popCV: med('popCV'),
  busts: med('busts'), cycle: rs.map(r => r.cycle).filter(v => v !== null).length ? med('cycle') : null,
  settLife: null, fallen: null, heaps: null,
  gini: rs.map(r => r.gini).filter(v => v !== null).length ? med('gini') : null,
};

function verdict(t, v) {
  if (t.unbuilt && (v === null || v === undefined)) return ['— ', 'mekaniken finns inte i motorn än'];
  if (v === null || v === undefined) return ['— ', 'omätbar'];
  switch (t.id) {
    case 'halfTree': return v >= 400 ? ['OK', ''] : ['XX', `halva trädet redan år ${v} — ${Math.round(400/Math.max(v,1))}x för tidigt`];
    case 'lastTech': return v > YEARS * 0.6 ? ['OK', ''] : ['XX', `bågen tar slut vid år ${v} av ${YEARS}`];
    case 'popCV':    return (v >= 0.25 && v <= 0.40) ? ['OK', ''] : ['? ', `${v.toFixed(2)} — läs NEDGÅNGARNA innan du tror på den här raden`];
    case 'busts':    { const want = Math.max(1, Math.round(YEARS/500)); return v >= want ? ['OK',''] : ['XX', `${v} nedgångar på ${YEARS} år, ville ha >= ${want}`]; }
    case 'cycle':    return (v >= 300 && v <= 800) ? ['OK', ''] : ['? ', `${v} år`];
    case 'gini':     return (v >= 0.20 && v <= 0.35) ? ['OK', ''] : ['? ', v.toFixed(2)];
    default:         return ['? ', String(v)];
  }
}

console.log('\nMOT VERKLIGHETEN:');
console.log('      mått                                   vår värld       målet             källa');
console.log('  ' + '-'.repeat(112));
let red = 0;
for (const t of TARGETS) {
  const v = got[t.id];
  const [mark, note] = verdict(t, v);
  if (mark === 'XX') red++;
  const shown = v === null || v === undefined ? '—' : (typeof v === 'number' && v % 1 ? v.toFixed(2) : String(v));
  console.log(`  ${mark}  ${t.label.padEnd(38)} ${shown.padStart(10)}   ${t.want.padEnd(16)}  ${t.src}`);
  if (note) console.log(`      ^ ${note}`);
}

console.log('\n' + '='.repeat(96));
console.log(red === 0
  ? 'INGEN RÖD RAD. Läs ändå formen: en hög CV utan nedgångar är en ramp, inte en cykel.'
  : `${red} RÖDA RADER. Det är förväntat före P2 — den här filen finns för att göra avståndet mätbart.`);
console.log('Konstitutionen står över varje rad ovan: litteraturen vinner aldrig över världen.');
console.log('Avviker vår värld OCH är bättre, är avvikelsen ett fynd — inte ett fel.');

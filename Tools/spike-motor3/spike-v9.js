// ============================================================================
// EMERGENCE — MOTOR 3, SPIKEN. Masterplan v2, fas 0.
//
// Detta är den kortaste konstruktion som kan visa att idén är FEL. Den rör ingen
// kanon, ingen befintlig kod, ingen om-baselining. Faller ett av de sex talen
// avskrivs Motor 3 och vi kör den inkrementella planen — och det rapporteras som
// ett genombrott, inte ett misslyckande.
//
// TRE RÖRELSER, EN MASKIN:
//   A  HÄNDELSEKÄRNAN   — tiden stegas inte, den hoppas. Kostnaden följer vad som
//                         HÄNDER, inte hur många tickar som passerar.
//   B  EGENSKAPSALGEBRAN — teknologier författas inte, de HÄRLEDS ur material,
//                         FORM och sammansättning. FORM-axeln är granskningens
//                         fynd: en skalär materialalgebra stänger vid 14 procedurer.
//   C  SÖKNINGEN         — metoder ÄR sökalgoritmer. Den nedskrivna misslyckade
//                         prövningen är memoisering. Instrument öppnar DIMENSIONER,
//                         och det är det enda som håller B öppet.
//
// LAGAR SOM GÄLLER VARJE RAD HÄR:
//   · deterministisk: nycklad mulberry32, aldrig en klocka, aldrig iterationsordning
//   · sluten matematik: + - * / sqrt floor round min max abs imul. INGEN sin/cos/log/pow.
//     (hasardtakter via invers-CDF-tabell i heltal — därför behövs ingen log)
//   · PROPERTIES / PROCESSES / CONSEQUENCES: inget utfallsord i en villkorssats
//   · varje entitet bär `res` (upplösning) redan nu, så aggregatet blir en påbyggnad
//     och inte en andra omskrivning
// ============================================================================
'use strict';

// ---------------------------------------------------------------------------
// 0 · NYCKLAD RNG. Strömmen beror på VEM som drar, inte på hur många som drog före.
//     Det är förutsättningen för att två upplösningar någonsin ska kunna jämföras.
// ---------------------------------------------------------------------------
function mix(a, b, c, d, e) {
  let h = (a ^ 0x9e3779b9) >>> 0;
  h = Math.imul(h ^ b, 0x85ebca6b) >>> 0;
  h = Math.imul(h ^ c, 0xc2b2ae35) >>> 0;
  h = Math.imul(h ^ d, 0x27d4eb2f) >>> 0;
  h = Math.imul(h ^ e, 0x165667b1) >>> 0;
  return (h ^ (h >>> 15)) >>> 0;
}
/** Ett drag. domän/nyckel/tick/syfte identifierar det unikt — ingen delad räknare. */
function draw(W, domain, key, purpose) {
  let a = mix(W.seed, domain, key, W.now, purpose);
  a = (a + 0x6D2B79F5) | 0;
  let t = Math.imul(a ^ (a >>> 15), 1 | a);
  t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
  return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
}
const drawI = (W, d, k, p, n) => Math.floor(draw(W, d, k, p) * n) % n;

// Invers-CDF för geometrisk väntetid, i heltal. Ersätter Math.log helt.
// TAB[i] = antal år att vänta när u ligger i hink i, för takt r = (i+1)/64 per år.
const HZ = [];
for (let r = 1; r <= 64; r++) {
  const row = new Array(64);
  let cum = 0, k = 0;
  const p = r / 64;
  for (let b = 0; b < 64; b++) {
    const target = (b + 0.5) / 64;
    while (cum < target && k < 400) { cum += (1 - cum) * p; k++; }
    row[b] = k < 1 ? 1 : k;
  }
  HZ.push(row);
}
const waitYears = (W, rate64, dom, key, pur) =>
  HZ[Math.max(0, Math.min(63, rate64 - 1))][drawI(W, dom, key, pur, 64)];

// ---------------------------------------------------------------------------
// 1 · HÄNDELSEKÖN. Binär heap på packad heltalsnyckel. Lat ogiltigförklaring
//     via epok — man tar aldrig bort ur heapen, man förkastar vid pop.
// ---------------------------------------------------------------------------
const CLASS = { CALENDAR: 0, THRESHOLD: 1, ARRIVAL: 2, HAZARD: 3, MEET: 4, DEMAND: 5 };

// ★ HÄNDELSETYPER, INTE CLOSURES (spikens tal 5 gick rött: 3 av 3 köposter bar en
// funktionsreferens, så en sparad värld kunde återställa DATA men inte FRAMTIDEN).
// En händelse bär nu en TYP-tagg och ett heltalsargument. Tabellen byggs vid laddning.
// Closures får aldrig korsa en sparfil.
const EV = { YEAR: 0, METHOD: 1, INSTRUMENT: 2, DEMAND: 3, PRESSURE: 4 };
class Queue {
  constructor() { this.h = []; this.seq = 0; }
  push(tick, cls, id, type, arg) {
    const e = { tick, cls, id, seq: this.seq++, type, arg };
    this.h.push(e);
    let i = this.h.length - 1;
    while (i > 0) { const p = (i - 1) >> 1; if (this.cmp(this.h[i], this.h[p]) < 0) { this.sw(i, p); i = p; } else break; }
  }
  cmp(a, b) { return a.tick - b.tick || a.cls - b.cls || a.id - b.id || a.seq - b.seq; }
  sw(i, j) { const t = this.h[i]; this.h[i] = this.h[j]; this.h[j] = t; }
  pop() {
    if (!this.h.length) return null;
    const top = this.h[0], last = this.h.pop();
    if (this.h.length) {
      this.h[0] = last;
      let i = 0;
      for (;;) {
        const l = 2 * i + 1, r = l + 1; let m = i;
        if (l < this.h.length && this.cmp(this.h[l], this.h[m]) < 0) m = l;
        if (r < this.h.length && this.cmp(this.h[r], this.h[m]) < 0) m = r;
        if (m === i) break; this.sw(i, m); i = m;
      }
    }
    return top;
  }
  get size() { return this.h.length; }
}

// ---------------------------------------------------------------------------
// 2 · EGENSKAPSALGEBRAN
//     Nio dimensioner, heltal 0..7. Sex synliga från start; tre öppnas av
//     INSTRUMENT — det är rörelse C inne i B, och det är det som håller rummet öppet.
// ---------------------------------------------------------------------------
const DIM = ['hard', 'plastic', 'cohesive', 'thresh', 'dense', 'rough', 'conduct', 'clear', 'elastic'];
const DIM_VISIBLE_AT_START = 6;             // conduct/clear/elastic kräver instrument

// FORM — granskningens fynd. Skärpa är en egenskap hos en FORM, inte hos ett material.
const FORM = ['raw', 'lump', 'sheet', 'rod', 'blade', 'vessel', 'cord', 'frame', 'wheel', 'lever', 'mesh', 'head'];
const F = {}; FORM.forEach((n, i) => F[n] = i);

// AFFORDANSER — BERÄKNADE ur (material, form, sammansättning). Aldrig författade.
const AFFORD = ['cut', 'hold', 'bind', 'burn', 'build', 'carry', 'grind', 'pierce', 'contain', 'spring', 'see', 'conduct'];
function affordances(m, form, comp) {
  const a = [];
  const d = m.d;
  if ((form === F.blade || form === F.head) && d.hard >= 4) a.push('cut');
  if (form === F.rod || form === F.lever) a.push('hold');
  if (form === F.cord || form === F.mesh) a.push('bind');
  if (d.thresh >= 1 && d.thresh <= 3) a.push('burn');
  if ((form === F.sheet || form === F.frame || form === F.lump) && d.hard >= 3) a.push('build');
  if (form === F.vessel || form === F.mesh) a.push('carry');
  if ((form === F.wheel || form === F.lump) && d.hard >= 5 && d.rough >= 4) a.push('grind');
  if ((form === F.rod || form === F.head) && d.hard >= 5) a.push('pierce');
  if (form === F.vessel && d.plastic <= 3) a.push('contain');
  if (d.elastic >= 5 && (form === F.rod || form === F.cord)) a.push('spring');
  if (d.clear >= 5 && form === F.sheet) a.push('see');
  if (d.conduct >= 5 && (form === F.rod || form === F.sheet)) a.push('conduct');
  // sammansättning: en roll-graf kan bära affordanser dess delar inte har ensamma
  // STRIKT: en sammansättning bär bara en förmåga om DELARNA faktiskt bär den.
  // Rollnamn ensamt är outcome leakage i miniatyr — namnet får inte skapa egenskapen.
  if (comp && comp.length >= 2) {
    const by = r => comp.find(c => c.role === r);
    const head = by('head'), haft = by('haft'), axle = by('axle'), wheel = by('wheel'),
          frame = by('frame'), cover = by('cover');
    if (head && haft && head.d && head.d.hard >= 5 && haft.d && haft.d.cohesive >= 3) { a.push('cut'); a.push('pierce'); }
    if (axle && wheel && axle.d && axle.d.hard >= 4 && wheel.d && wheel.d.dense >= 4) a.push('carry');
    if (frame && cover && frame.d && frame.d.hard >= 4 && cover.d && cover.d.cohesive >= 3) a.push('build');
  }
  return a;
}

// ★ TEMPERATURSTEGEN (forskningssvepet §4.1: "ugnen är metallurgins moder").
// Processer har NIVÅER. En varmare process kan bara köras om världen kan NÅ den
// temperaturen — och världens maxvärme härleds ur det bästa kärl den kan bygga.
// Ett nytt material som tål mer höjer taket och låser upp processer inget material
// tidigare uppfyllde. Det är arbetet klausul (b) saknade, och det är HÄRLETT.
const PROCESS = [
  { id: 'heat1',   tier: 2, need: d => d.thresh >= 1,        gives: d => ({ hard: +2, plastic: -2 }) },
  { id: 'heat2',   tier: 4, need: d => d.thresh >= 3,        gives: d => ({ hard: +3, plastic: -2, dense: +1 }) },
  { id: 'heat3',   tier: 6, need: d => d.thresh >= 5,        gives: d => ({ hard: +4, dense: +2, conduct: +1 }) },
  { id: 'strike',  need: d => d.hard >= 3,                   gives: d => ({ rough: +1 }) },
  { id: 'grindp',  need: d => d.hard >= 2,                   gives: d => ({ rough: +2, dense: +1 }) },
  { id: 'twist',   need: d => d.cohesive >= 3,               gives: d => ({ cohesive: +2, elastic: +1 }) },
  { id: 'weave',   need: d => d.cohesive >= 4,               gives: d => ({ cohesive: +1 }) },
  { id: 'mix',     need: d => d.plastic >= 3,                gives: d => ({ hard: +1, cohesive: +1 }) },
  { id: 'carve',   need: d => d.plastic >= 2 || d.hard <= 4, gives: d => ({ rough: -1 }) },
  { id: 'assemble',need: () => true,                         gives: () => ({}) },
];

const clampQ = v => v < 0 ? 0 : v > 7 ? 7 : v;

/** Normaliserad poang: dimensionsmedel (0..7) + formagor + sammansattning. */
function scoreOf(out, aff, comp, visibleDims) {
  // DIMENSIONSOBEROENDE. Rangen ar vad saken KAN GORA — antal formagor, hur djup
  // sammansattningen ar, och hur val den bara den formaga den ar bast pa. Att oppna
  // en dimension far darfor aldrig sanka en gammal poang; den kan bara skapa NYA
  // formagor. Ett instrument blir en mojlighet i stallet for en straffavgift.
  let peak = 0;
  for (const k of ['hard', 'cohesive', 'dense', 'elastic', 'clear', 'conduct'])
    if (out.d[k] > peak) peak = out.d[k];
  return aff.length * 10 + (comp ? comp.length * 4 : 0) + peak;
}

/** Världens maxvärme: härledd ur bästa kärl den kan bygga, aldrig satt.
 *  Ett kärl av något som själv tål hetta bär en hetare eld. */
function maxHeat(W) {
  // Bara ett KARL bar en het eld. Malmen finns fran ar noll; formagan att smalta
  // den gor det inte. Taket stiger nar nagon bygger ett battre karl — aldrig av sig sjalvt.
  let best = 2;                                   // oppen eld
  for (const m of W.mats) {
    if (!m.vessel) continue;                      // <-- radikalt: bara karl raknas
    const v = 2 + m.d.thresh + (m.d.dense >= 5 ? 1 : 0);
    if (v > best) best = v;
  }
  return best;
}

/** Utgångsmaterialen. Sex synliga dimensioner; tre väntar på instrument. */
function seedMaterials() {
  const mk = (name, o) => ({ name, d: Object.assign({ hard: 0, plastic: 0, cohesive: 0, thresh: 0, dense: 0, rough: 0, conduct: 0, clear: 0, elastic: 0 }, o) });
  return [
    mk('stone',  { hard: 6, dense: 6, rough: 4 }),
    mk('wood',   { hard: 3, cohesive: 3, thresh: 2, elastic: 4 }),
    mk('clay',   { plastic: 6, cohesive: 3, thresh: 3, dense: 4 }),
    mk('fibre',  { cohesive: 6, elastic: 3, thresh: 2 }),
    mk('bone',   { hard: 5, dense: 4 }),
    mk('hide',   { cohesive: 4, plastic: 4, elastic: 4 }),
    mk('sand',   { dense: 3, rough: 6, thresh: 4, clear: 5 }),
    mk('copper', { hard: 3, plastic: 4, thresh: 5, dense: 6, conduct: 6 }),
    mk('tin',    { hard: 2, plastic: 5, thresh: 4, dense: 5, conduct: 5 }),
    mk('iron',   { hard: 6, thresh: 6, dense: 7, conduct: 5 }),
    mk('reed',   { cohesive: 4, thresh: 2, elastic: 5 }),
    mk('resin',  { plastic: 5, cohesive: 5, thresh: 2 }),
  ];
}

// ---------------------------------------------------------------------------
// 3 · VÄRLDEN. Marken genererar EFTERFRÅGAN — den ena av två behåll-regler som
//     inte mättar. Utan den stänger algebran (granskningen mätte 14 procedurer).
// ---------------------------------------------------------------------------
const DEMANDS = ['carry', 'contain', 'cut', 'build', 'burn', 'bind', 'grind', 'pierce', 'see', 'spring', 'conduct', 'hold'];

// ANDRA BENET: efterfragan anlander inte bara ur marken. Trangsel foder konflikt,
// konflikt foder behov av skydd och rackvidd; tathet foder sjukdom, sjukdom foder
// behov av forvaring, rening och avstand. Ingen av dem ar ett UTFALL — bagge ar
// egenskaper hos varlden som SKAPAR behov, och behov ar det som haller rummet oppet.
const PRESSURE = {
  conflict: ['pierce', 'build', 'cut', 'spring'],
  disease:  ['contain', 'carry', 'burn', 'see'],
};

function createWorld(seed, opts = {}) {
  const W = {
    seed: seed >>> 0, now: 0, year: 0,
    q: new Queue(),
    mats: seedMaterials(),
    visibleDims: DIM_VISIBLE_AT_START,
    procedures: [],                 // härledda
    procKey: new Set(),             // stabila id
    best: {},                       // affordans -> bästa poäng hittills
    openRoles: new Set(['haft', 'head', 'binder', 'axle', 'wheel', 'frame', 'cover']),
    roleBest: {},
    seenCombos: new Set(),
    roleOpen: opts.roleOpen !== false,        // c1 oppnaren
    roleQuality: opts.roleQuality !== false,  // c2 bromsen
    roleThresh: opts.roleThresh === undefined ? 0 : opts.roleThresh,
    demand: new Set(['carry', 'cut']),
    memo: new Map(),                // ★ metod 3: nedskrivna misslyckanden -> epok
    epoch: 0,                       // hojs nar varlden andras
    bestDecay: opts.bestDecay === undefined ? 0.85 : opts.bestDecay,
    methods: { accident: true, craft: false, written: false },
    instruments: 0,
    pop: 4, res: 'soul',            // `res` finns redan nu — aggregatet blir en påbyggnad
    log: [],                        // (year, kind, what)
    stats: { attempts: 0, memoHits: 0, events: 0, byYear: {} },
    seedTriples: new Set(),
    heatCap: 2,
    pressureOn: opts.pressureOn !== false,
    opts,
  };
  return W;
}

const YEAR = 1;  // spiken räknar i år; händelsekärnan gör tickupplösning onödig

// ---------------------------------------------------------------------------
// 4 · SÖKNINGEN. Ett försök = välj ingångar, form, process. Metoderna ÄR
//     algoritmen: olyckan samplar likformigt, hantverket söker lokalt kring det
//     som fungerar, den nedskrivna prövningen memoiserar bort återbesök.
// ---------------------------------------------------------------------------
function tripleKey(inputs, proc, form) {
  return inputs.map(i => i.name).sort().join('+') + '|' + proc + '|' + FORM[form];
}

function attempt(W, key) {
  W.stats.attempts++;
  const pool = W.mats;
  let ia, ib, procIdx, form;

  if (W.methods.craft && W.procedures.length && drawI(W, 7, key, 1, 100) < 55) {
    // LOKAL SÖKNING: variera en känd procedur i ett steg
    const base = W.procedures[drawI(W, 7, key, 2, W.procedures.length)];
    ia = base.inputs[0];
    ib = pool[drawI(W, 7, key, 3, pool.length)];
    procIdx = drawI(W, 7, key, 4, 100) < 60 ? base.procIdx : drawI(W, 7, key, 5, PROCESS.length);
    form = drawI(W, 7, key, 6, 100) < 60 ? base.form : drawI(W, 7, key, 7, FORM.length);
  } else {
    // LIKFORMIG SAMPLING: olyckan man lade märke till
    ia = pool[drawI(W, 7, key, 8, pool.length)];
    ib = pool[drawI(W, 7, key, 9, pool.length)];
    procIdx = drawI(W, 7, key, 10, PROCESS.length);
    form = drawI(W, 7, key, 11, FORM.length);
  }

  const inputs = ia === ib ? [ia] : [ia, ib];
  const proc = PROCESS[procIdx];
  const tk = tripleKey(inputs, proc.id, form);

  // ★ MEMOISERING — sökande utan återläggning
  if (W.methods.written && W.memo.get(tk) === W.epoch) { W.stats.memoHits++; return null; }

  // processpredikat: en av ingångarna måste tåla processen
  if (proc.tier !== undefined && proc.tier > W.heatCap) { if (W.methods.written) W.memo.set(tk, W.epoch); return null; }
  const ok = inputs.some(m => proc.need(m.d));
  if (!ok) { if (W.methods.written) W.memo.set(tk, W.epoch); return null; }

  // härled utgångens dimensioner — max över ingångarna, plus processens verkan,
  // och ENDAST i de dimensioner som är SYNLIGA (instrument öppnar fler)
  const d = {};
  for (let i = 0; i < DIM.length; i++) {
    const dim = DIM[i];
    if (i >= W.visibleDims) { d[dim] = 0; continue; }
    let v = 0; for (const m of inputs) if (m.d[dim] > v) v = m.d[dim];
    d[dim] = v;
  }
  const delta = proc.gives(d);
  for (const k in delta) if (DIM.indexOf(k) < W.visibleDims) d[k] = clampQ(d[k] + delta[k]);

  // sammansättning: 'assemble' binder två delar i roller
  let comp = null;
  if (proc.id === 'assemble' && inputs.length === 2) {
    const roles = ['haft', 'head', 'binder', 'axle', 'wheel', 'frame', 'cover'];
    comp = [
      { part: inputs[0].name, d: inputs[0].d, role: roles[drawI(W, 7, key, 12, roles.length)] },
      { part: inputs[1].name, d: inputs[1].d, role: roles[drawI(W, 7, key, 13, roles.length)] },
    ];
  }

  const out = { name: null, d };
  const aff = affordances(out, form, comp);
  if (!aff.length) { if (W.methods.written) W.memo.set(tk, W.epoch); return null; }

  return { inputs, procIdx, proc: proc.id, form, comp, out, aff, tk };
}

/** BEHÅLL-REGELN, fyra klausuler. (b) och (c) saknades i v1 och det var därför rummet stängde. */
function keep(W, c) {
  // NORMALISERAD poang: medelvarde over SYNLIGA dimensioner, inte rasumma.
  // Med rasumma sankte lasta dimensioner poangen -> ribban sjonk -> instrumenten blev
  // inverterade (+64 % nar de stangdes av). Normaliseringen gor ribban jamforbar
  // over tid, sa en ny dimension ar en MOJLIGHET och inte en straffavgift.
  const sc = scoreOf(c.out, c.aff, c.comp, W.visibleDims);
  // a) höjer bäst-hittills på någon affordans
  for (const a of c.aff) if (!(a in W.best) || sc > W.best[a]) return 'a';
  // c) fyller en öppen rollplats i en sammansättning
  // c1 — OPPNAREN: en rollKOMBINATION världen aldrig sett förut.
  if (W.roleOpen && c.comp) {
    const combo = c.comp.map(x => x.role).sort().join('+');
    if (!W.seenCombos.has(combo)) return 'c1';
  }
  // c2 — BROMSEN: strikt bättre bärare av en KÄND roll.
  if (W.roleQuality && c.comp) for (const part of c.comp) {
    let s2 = 0; for (const k in part.d) s2 += part.d[k];
    if (s2 > (W.roleBest[part.role] || 0) + W.roleThresh) return 'c2';
  }
  // d) slår befintligt på en affordans världen JUST NU efterfrågar
  for (const a of c.aff) if (W.demand.has(a) && sc >= (W.best[a] || 0) - 2) return 'd';
  return null;
}

function adopt(W, c, why) {
  const nm = c.proc + '-' + FORM[c.form] + '-' + c.inputs.map(i => i.name).join('/');
  c.out.name = nm;
  const p = { name: nm, inputs: c.inputs, proc: c.proc, procIdx: c.procIdx, form: c.form,
              comp: c.comp, aff: c.aff, year: W.year, why, tk: c.tk, res: 'named' };
  W.procedures.push(p); W.procKey.add(c.tk);
  c.out.vessel = (c.form === F.vessel || c.form === F.frame);
  W.mats.push(c.out);                                  // utgången blir en ny ingång
  const nh = maxHeat(W);
  if (nh > W.heatCap) { W.heatCap = nh; W.epoch++; decayBest(W); W.log.push([W.year, 'heat', 'varmegransen stiger till ' + nh + ' via ' + nm]); }
  const s = scoreOf(c.out, c.aff, c.comp, W.visibleDims);
  for (const a of c.aff) if (!(a in W.best) || s > W.best[a]) W.best[a] = s;
  // Rollbromsen: en roll stängs inte för alltid (då dör klausul c efter sju procedurer),
  // men den kräver ett STRIKT bättre material nästa gång. Bromsen är en tröskel, inte en dörr.
  if (c.comp) W.seenCombos.add(c.comp.map(x => x.role).sort().join('+'));
  if (c.comp) for (const part of c.comp) {
    const prev = W.roleBest[part.role] || 0;
    let s2 = 0; for (const k in part.d) s2 += part.d[k];
    if (s2 > prev) W.roleBest[part.role] = s2;
  }
  W.stats.byYear[W.year] = (W.stats.byYear[W.year] || 0) + 1;
  W.log.push([W.year, 'procedure', nm + '  [' + why + ']  ' + c.aff.join(',')]);
  return p;
}

// ---------------------------------------------------------------------------
// 5 · HÄNDELSERNA
// ---------------------------------------------------------------------------
function schedule(W) {
  // KALENDER: ett år. Billig, en enda händelse, oavsett folkmängd.
  W.q.push(W.now + YEAR, CLASS.CALENDAR, 0, EV.YEAR, 0);
}

function yearTick(W) {
  W.year++;
  // befolkning: långsam logistisk drift mot ett tak marken sätter (properties, inte utfall)
  const cap = 60 + W.procedures.length * 3;
  if (W.pop < cap) W.pop += Math.max(1, Math.floor((cap - W.pop) / 40));
  else if (W.pop > cap) W.pop -= Math.max(1, Math.floor((W.pop - cap) / 40));

  // SÖKNING: antal försök ~ sökare, sublinjärt
  const searchers = Math.max(1, Math.floor(Math.sqrt(W.pop) * 2));
  for (let i = 0; i < searchers; i++) {
    const key = mix(W.year, i, W.pop, 0, 0);
    const c = attempt(W, key);
    if (!c) continue;
    if (W.procKey.has(c.tk)) continue;
    const why = keep(W, c);
    if (why) adopt(W, c, why);
    else if (W.methods.written) W.memo.set(c.tk, W.epoch);
  }
  schedule(W);
}

/** METODER som anländer — var och en ändrar sökalgoritmen, ingen ändrar en takt. */
function scheduleMethods(W) {
  W.q.push(30 * YEAR, CLASS.THRESHOLD, 1, EV.METHOD, 0);
  W.q.push(400 * YEAR, CLASS.THRESHOLD, 2, EV.METHOD, 1);
}
function onMethod(W, which) {
  if (which === 0) { W.methods.craft = true; W.log.push([W.year, 'method', 'hantverket — lokal sokning']); }
  else { W.methods.written = true; W.log.push([W.year, 'method', 'den nedskrivna misslyckade provningen — memoisering']); }
}

/** INSTRUMENT öppnar DIMENSIONER. Det enda som håller egenskapsrummet öppet. */
function scheduleInstruments(W) {
  [700, 1400, 2100].forEach((y, i) => W.q.push(y * YEAR, CLASS.THRESHOLD, 10 + i, EV.INSTRUMENT, i));
}
function decayBest(W) {
  // Nar varlden andras ger ribban efter. Ett hogvattenmarke satt fore ett
  // instrument, en ny efterfragan eller ett hogre varmetak beskriver en annan
  // varld an den som nu galler — och att lata det sta kvar fryser rummet.
  if (W.bestDecay >= 1) return;
  for (const a in W.best) W.best[a] = Math.floor(W.best[a] * W.bestDecay);
}
function onInstrument(W) {
  W.visibleDims++; W.instruments++; W.epoch++; decayBest(W);
  W.log.push([W.year, 'instrument', 'ny dimension synlig: ' + DIM[W.visibleDims - 1]]);
}

/** MARKEN genererar EFTERFRÅGAN — hasardstyrd, via invers-CDF, ingen log. */
function scheduleDemand(W, n) {
  const wait = waitYears(W, 6, 3, n, 1);
  W.q.push(W.now + wait * YEAR, CLASS.DEMAND, n, EV.DEMAND, n);
}
function onDemand(W, n) {
  const d = DEMANDS[drawI(W, 3, n, 2, DEMANDS.length)];
  if (!W.demand.has(d)) { W.demand.add(d); W.epoch++; decayBest(W); W.log.push([W.year, 'demand', 'marken kraver: ' + d]); }
  scheduleDemand(W, n + 1);
}

/** ANDRA BENET: tryck ur trangsel. Konflikt och sjukdom aterkommer och SLAPPER
 *  sedan taget — till skillnad fran marken, vars efterfragan ar monoton. Det ar
 *  darfor de kan bara rummet nar marken mattat. */
function schedulePressure(W, n) {
  if (!W.pressureOn) return;
  const wait = waitYears(W, 4, 5, n, 1);
  W.q.push(W.now + wait * YEAR, CLASS.HAZARD, n, EV.PRESSURE, n);
}
function onPressure(W, n) {
  const kinds = ['conflict', 'disease'];
  const k = kinds[drawI(W, 5, n, 2, 2)];
  const set = PRESSURE[k];
  const a = set[drawI(W, 5, n, 3, set.length)];
  const on = drawI(W, 5, n, 4, 100) < 55;
  if (on) { W.demand.add(a); W.epoch++; decayBest(W); W.log.push([W.year, 'pressure', k + ' kraver: ' + a]); }
  else if (W.demand.has(a) && W.demand.size > 2) { W.demand.delete(a); W.log.push([W.year, 'pressure', k + ' slapper: ' + a]); }
  schedulePressure(W, n + 1);
}

const HANDLER = { [EV.YEAR]: yearTick, [EV.METHOD]: onMethod, [EV.INSTRUMENT]: onInstrument, [EV.DEMAND]: onDemand, [EV.PRESSURE]: onPressure };

// ---------------------------------------------------------------------------
// 6 · KÖR
// ---------------------------------------------------------------------------
function run(seed, years, opts = {}) {
  const W = createWorld(seed, opts);
  // sådd: de befintliga teknologiernas trippel-nycklar registreras som "skrivna av oss"
  for (const m of W.mats) for (const p of PROCESS) for (let f = 0; f < 4; f++)
    W.seedTriples.add(tripleKey([m], p.id, f));
  schedule(W); scheduleMethods(W); scheduleInstruments(W); scheduleDemand(W, 0); schedulePressure(W, 0);

  const t0 = process.hrtime.bigint();
  while (W.year < years) {
    const e = W.q.pop();
    if (!e) break;
    W.now = e.tick;
    W.stats.events++;
    HANDLER[e.type](W, e.arg);
  }
  W.ms = Number(process.hrtime.bigint() - t0) / 1e6;
  return W;
}

module.exports = { run, createWorld, DIM, FORM, PROCESS, AFFORD, tripleKey, Queue, draw, waitYears, HANDLER, CLASS, EV };

// ---------------------------------------------------------------------------
if (require.main === module) {
  const YEARS = parseInt(process.argv[2] || '3000', 10);
  const SEEDS = (process.argv[3] || '4242,777,1234').split(',').map(Number);
  console.log(`EMERGENCE — MOTOR 3 SPIKEN   ${SEEDS.length} seeds × ${YEARS} år\n`);
  for (const s of SEEDS) {
    const W = run(s, YEARS);
    const yrs = Object.keys(W.stats.byYear).map(Number).sort((a, b) => a - b);
    const early = W.procedures.filter(p => p.year <= 30).length;
    const first1000 = W.procedures.filter(p => p.year <= 1000).length;
    const last1000 = W.procedures.filter(p => p.year > 2000).length;
    let gap = 0; for (let i = 1; i < yrs.length; i++) if (yrs[i] - yrs[i - 1] > gap) gap = yrs[i] - yrs[i - 1];
    const novel = W.procedures.filter(p => !W.seedTriples.has(p.tk)).length;
    console.log(`seed ${s}: ${W.procedures.length} procedurer · ${novel} ej i sådden · år 0–30: ${early} · [0,1000]: ${first1000} · (2000,3000]: ${last1000} · största lucka: ${gap} år · ${W.stats.events} händelser · ${W.ms.toFixed(0)} ms · ${(W.ms / YEARS).toFixed(4)} ms/år`);
  }
}

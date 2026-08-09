// EMERGENCE — tick-precise world export (store-capture rig, D-183 follow-up)
// Same DRIVER-PARITY serializer as export-world-e15.js (mirrors Fas3SimDriver.ExportJs FIELD FOR
// FIELD) but stops at an exact TICK, not a year boundary — Signature Moments live mid-year.
// usage: node export-world-tick.js <seed> <tick>   (writes ../Assets/Emergence/WorldStates/world-<seed>-t<tick>-e15.json)
// Presentation contract (D-078 rule 4): a READ of S; the body may never write back.
'use strict';
const FS = require('fs'), PATH = require('path');
const E = require(PATH.join(__dirname, '..', 'Assets', 'StreamingAssets', 'Emergence', 'emergence-engine.js'));
const seed = parseInt(process.argv[2], 10), stop = parseInt(process.argv[3], 10);
if (!seed || !stop) { console.error('usage: node export-world-tick.js <seed> <tick>'); process.exit(1); }
const S = E.createWorld(seed); S.silent = true;
while (S.tick < stop && !S.ended) E.tickWorld(S);
const out = {
  engineVersion: E.VERSION, seed, years: Math.floor(S.tick / E.YEAR), tick: S.tick, ended: !!S.ended, season: '' + S.season,
  era: E.worldEra(S),
  eraName: '' + E.eraName(E.worldEra(S)),
  W: E.W, H: E.H,
  // store-capture rig runs WorldDresser.Build on this file -> it NEEDS real tiles (unlike the
  // runtime-Apply e15 fixtures). Same flat serialization as export-world-state.js.
  tileTypes: S.tiles.map(row => row.map(t => ({ grass: 'g', water: 'w', forest: 'f', stone: 's', berry: 'b', sand: 'a', clay: 'c', iron: 'i' }[t.t] || 'g')).join('')).join(''),
  tileN: [].concat(...S.tiles.map(row => row.map(t => t.n | 0))),
  agents: S.agents.filter(a => !a.dead).map(a => ({ id: a.id, name: '' + a.name, x: a.x, y: a.y, age: a.age, gen: a.gen, task: '' + a.task, verb: '' + E.verbOf(a.task), say: '' + (a.say || ''), sayAct: '' + (a.sayAct || ''), home: !!a.home, wealth: E.wealthOf(a) })),
  dead: S.agents.filter(a => a.dead).length,
  huts: S.huts.map(h => ({ x: h.x, y: h.y, owner: '' + (h.owner || ''), free: !!h.free })),
  fires: S.fires.map(f => ({ x: f.x, y: f.y, fuel: f.fuel })),
  fields: S.fields.map(f => ({ x: f.x, y: f.y, stage: f.stage, owner: '' + (f.owner || '') })),
  villages: (() => { const sc = E.villageScope(S); return S.villages.map((v, i) => ({ x: v.x, y: v.y, name: '' + v.name, leader: '' + (v.leaderName || ''), gift: '' + (v.giftName || ''), pop: sc[i].pop, maxGen: sc[i].maxGen, avgAge: sc[i].avgAge, crafts: sc[i].crafts, knows: sc[i].knows })); })(),
  animals: S.animals.map(an => ({ id: an.id, type: '' + an.type, x: an.x, y: an.y })),
  pathUse: S.pathUse || [],
  dna: JSON.stringify(E.computeDNA(S))
};
const outDir = PATH.join(__dirname, '..', 'Assets', 'Emergence', 'WorldStates');
const f = `world-${seed}-t${stop}-e15.json`;
FS.writeFileSync(PATH.join(outDir, f), JSON.stringify(out));
console.log(f, 'tick=' + out.tick, 'y=' + out.years, 'season=' + out.season, 'agents=' + out.agents.length,
  'fires=' + out.fires.length, 'huts=' + out.huts.length, 'villages=' + out.villages.length, 'era=' + out.eraName);

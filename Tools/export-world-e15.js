// EMERGENCE — E1.5 body-consumption fixture export (engine 2.4.1 -> JSON for the Unity body lane)
// DRIVER PARITY: this serializer mirrors Fas3SimDriver.ExportJs FIELD FOR FIELD (the exact export
// the body consumes live), so a fixture made here is a REAL 2.4.1 export through the same schema:
// era/eraName (R2 ink. 1), agents[].verb + wealth (E1.5), villages[].leader + gift (E1.5).
// The engine source is the StreamingAssets twin — the SAME file Jint runs in editor and player (D-093).
// usage: node export-world-e15.js <seed> <years>   (writes ../Assets/Emergence/WorldStates/world-<seed>-y<years>-e15.json)
// Presentation contract (D-078 rule 4): a READ of S; the body may never write back.
'use strict';
const FS = require('fs'), PATH = require('path');
const E = require(PATH.join(__dirname, '..', 'Assets', 'StreamingAssets', 'Emergence', 'emergence-engine.js'));
const seed = parseInt(process.argv[2], 10), years = parseInt(process.argv[3], 10);
if (!seed || !years) { console.error('usage: node export-world-e15.js <seed> <years>'); process.exit(1); }
const S = E.createWorld(seed); S.silent = true;
const stop = years * E.YEAR;
while (S.tick < stop && !S.ended) E.tickWorld(S);
const out = {
  engineVersion: E.VERSION, seed, years: Math.floor(S.tick / E.YEAR), tick: S.tick, ended: !!S.ended, season: '' + S.season,
  era: E.worldEra(S),
  eraName: '' + E.eraName(E.worldEra(S)),
  W: E.W, H: E.H,
  tileTypes: '', tileN: [],
  agents: S.agents.filter(a => !a.dead).map(a => ({ id: a.id, name: '' + a.name, x: a.x, y: a.y, age: a.age, gen: a.gen, task: '' + a.task, verb: '' + E.verbOf(a.task), say: '' + (a.say || ''), sayAct: '' + (a.sayAct || ''), home: !!a.home, wealth: E.wealthOf(a) })),
  dead: S.agents.filter(a => a.dead).length,
  huts: S.huts.map(h => ({ x: h.x, y: h.y, owner: '' + (h.owner || ''), free: !!h.free })),
  fires: S.fires.map(f => ({ x: f.x, y: f.y, fuel: f.fuel })),
  fields: S.fields.map(f => ({ x: f.x, y: f.y, stage: f.stage, owner: '' + (f.owner || '') })),
  // VILLAGE-SCOPE (TD-085 driver parity): merge E.villageScope(S) field for field with the driver.
  villages: (() => { const sc = E.villageScope(S); return S.villages.map((v, i) => ({ x: v.x, y: v.y, name: '' + v.name, leader: '' + (v.leaderName || ''), gift: '' + (v.giftName || ''), pop: sc[i].pop, maxGen: sc[i].maxGen, avgAge: sc[i].avgAge, crafts: sc[i].crafts, knows: sc[i].knows })); })(),
  animals: S.animals.map(an => ({ id: an.id, type: '' + an.type, x: an.x, y: an.y })),
  pathUse: S.pathUse || [],
  dna: JSON.stringify(E.computeDNA(S))
};
const outDir = PATH.join(__dirname, '..', 'Assets', 'Emergence', 'WorldStates');
const f = `world-${seed}-y${years}-e15.json`;
FS.writeFileSync(PATH.join(outDir, f), JSON.stringify(out));
const lead = out.villages.filter(v => v.leader).map(v => v.name + ':' + v.leader);
const gift = out.villages.filter(v => v.gift).map(v => v.name + ':' + v.gift);
const topW = out.agents.slice().sort((a, b) => b.wealth - a.wealth).slice(0, 3).map(a => a.name + '=' + a.wealth.toFixed(1));
console.log(f, 'agents=' + out.agents.length, 'villages=' + out.villages.length, 'fires=' + out.fires.length,
  'era=' + out.era, out.eraName, '| leaders: ' + (lead.join(', ') || 'NONE'), '| gifts: ' + (gift.join(', ') || 'NONE'),
  '| topWealth: ' + topW.join(', '), '| sayActs: ' + [...new Set(out.agents.map(a => a.sayAct).filter(s => s))].join(','));

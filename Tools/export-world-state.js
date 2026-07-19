// EMERGENCE P1 — world-state export (engine 2.0.0 -> JSON for the Unity dressing layer)
// usage: node export-world-state.js <seed> <years> [foundersJsonFile]
// Presentation contract (D-078 rule 4): this is a READ of S — the dressing layer
// consumes it and may never write back.
'use strict';
const fs=require('fs');
const E=require('/root/e1/emergence-engine-2.0.js');
const seed=parseInt(process.argv[2],10), years=parseInt(process.argv[3],10);
const founders=process.argv[4]?JSON.parse(fs.readFileSync(process.argv[4],'utf8')):undefined;
const S=E.createWorld(seed,founders);S.silent=true;
const ticks=years*E.YEAR;
while(S.tick<ticks&&!S.ended)E.tickWorld(S);
const out={
  engineVersion:E.VERSION, seed, years, tick:S.tick, ended:S.ended, season:S.season,
  W:E.W, H:E.H,
  // JsonUtility-friendly flat tiles: one char per tile, row-major (y*W+x)
  // g=grass w=water f=forest s=stone b=berry a=sand c=clay i=iron
  tileTypes:S.tiles.map(row=>row.map(t=>({grass:'g',water:'w',forest:'f',stone:'s',berry:'b',sand:'a',clay:'c',iron:'i'}[t.t]||'g')).join('')).join(''),
  tileN:[].concat(...S.tiles.map(row=>row.map(t=>t.n|0))),
  agents:S.agents.filter(a=>!a.dead).map(a=>({id:a.id,name:a.name,x:a.x,y:a.y,age:a.age,gen:a.gen,task:a.task,say:a.say,sayAct:a.sayAct,traits:a.traits,home:!!a.home,knows:[...a.knows]})),
  dead:S.agents.filter(a=>a.dead).length,
  huts:S.huts.map(h=>({x:h.x,y:h.y,owner:h.owner,free:!!h.free})),
  fires:S.fires.map(f=>({x:f.x,y:f.y,fuel:f.fuel})),
  fields:S.fields.map(f=>({x:f.x,y:f.y,stage:f.stage,owner:f.owner})),
  villages:S.villages.map(v=>({x:v.x,y:v.y,name:v.name})),
  animals:S.animals.map(an=>({id:an.id,type:an.type,x:an.x,y:an.y})),
  pathUse:null, // engine has no path tracking yet — roads derive from hut/village adjacency in P1a; engine-side pathUse counter is an E2-candidate minor (RNG-neutral)
  dna:E.computeDNA(S),
};
const f=`world-${seed}-y${years}${founders?'-founders':''}.json`;
fs.writeFileSync(f,JSON.stringify(out));
console.log(f, 'agents='+out.agents.length,'huts='+out.huts.length,'villages='+out.villages.length,'fires='+out.fires.length,'fields='+out.fields.length,'season='+out.season,(fs.statSync(f).size/1024).toFixed(0)+'KB');

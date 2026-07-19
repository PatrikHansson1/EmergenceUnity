// EMERGENCE P1 — dusk-tick world export (engine 2.0.1 -> JSON for the Unity dressing layer)
// The year-boundary export lands at hour 0 (no burning fires). Fires are lit on cold
// evenings, so the "one warm point at blue hour" identity (TD-017) needs a WINTER DUSK
// snapshot. This scans a run for the best dusk tick (hour 18-21) with >=1 fire.
// usage: node export-world-dusk.js <seed> <years>  (writes ../Assets/Emergence/WorldStates/)
// Presentation contract (D-078 rule 4): a READ of S; the dresser may never write back.
'use strict';
const fs=require('path'), FS=require('fs'), PATH=require('path');
const E=require('../Assets/Emergence/Engine/emergence-engine.js');
const seed=parseInt(process.argv[2]||'4242',10), years=parseInt(process.argv[3]||'120',10);
function serialize(S){
  return {engineVersion:E.VERSION, seed, years, tick:S.tick, hour:S.hour, ended:S.ended, season:S.season, W:E.W, H:E.H,
    tileTypes:S.tiles.map(row=>row.map(t=>({grass:'g',water:'w',forest:'f',stone:'s',berry:'b',sand:'a',clay:'c',iron:'i'}[t.t]||'g')).join('')).join(''),
    tileN:[].concat(...S.tiles.map(row=>row.map(t=>t.n|0))),
    agents:S.agents.filter(a=>!a.dead).map(a=>({id:a.id,name:a.name,x:a.x,y:a.y,age:a.age,gen:a.gen,task:a.task,say:a.say,sayAct:a.sayAct,traits:a.traits,home:!!a.home,knows:[...a.knows]})),
    dead:S.agents.filter(a=>a.dead).length,
    huts:S.huts.map(h=>({x:h.x,y:h.y,owner:h.owner,free:!!h.free})),
    fires:S.fires.map(f=>({x:f.x,y:f.y,fuel:f.fuel})),
    fields:S.fields.map(f=>({x:f.x,y:f.y,stage:f.stage,owner:f.owner})),
    villages:S.villages.map(v=>({x:v.x,y:v.y,name:v.name})),
    animals:S.animals.map(an=>({id:an.id,type:an.type,x:an.x,y:an.y})),
    pathUse:null, dna:E.computeDNA(S)};
}
const S=E.createWorld(seed); S.silent=true;
const stop=years*E.YEAR;
let best=null, maxFires=0;
while(S.tick<stop && !S.ended){
  E.tickWorld(S);
  if(S.fires.length>maxFires) maxFires=S.fires.length;
  const dusk=S.hour>=18 && S.hour<=21;
  if(dusk && S.fires.length>0){
    const souls=S.agents.filter(a=>!a.dead).length;
    const score=S.fires.length*100 + souls + S.villages.length*10 + (S.season==='winter'?50:S.season==='autumn'?25:0);
    if(!best||score>best.score) best={score,snap:serialize(S),souls,fires:S.fires.length,season:S.season,hour:S.hour,vil:S.villages.length};
  }
}
if(!best){ console.log('no dusk fire found for',seed,years,'maxFiresSeen='+maxFires); process.exit(1); }
const outDir=PATH.join(__dirname,'..','Assets','Emergence','WorldStates');
const f=`world-${seed}-y${years}-dusk.json`;
FS.writeFileSync(PATH.join(outDir,f), JSON.stringify(best.snap));
console.log('WROTE',f,'season='+best.season,'hour='+best.hour,'souls='+best.souls,'villages='+best.vil,'fires='+best.fires,'tick='+best.snap.tick);

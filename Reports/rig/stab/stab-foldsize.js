// Fold-storleksprobe: skriver varje aggregate-event + K/T.de-läge vid fold/de-fold. node stab-foldsize.js <engine> <seed> <år>
globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;
const path=require('path'); const _m=require(path.resolve(process.argv[2])); const E=globalThis.Emergence||_m;
const YEAR=144, seed=Number(process.argv[3]||97013), years=Number(process.argv[4]||120);
const S=E.createWorld(seed); S.silent=true; let yr=0, seen=0;
while(yr<years&&!S.ended){ const t=(yr+1)*YEAR; while(S.tick<t&&!S.ended)E.tickWorld(S); yr++;
  const evs=(S.events||[]).slice(seen); seen=(S.events||[]).length;
  for(const e of evs) if(e.type==='aggregate'){ const alive=S.agents.filter(a=>!a.dead).length; const K=E.carryingCapacity?E.carryingCapacity(S):null;
    const aggs=(S.aggregates||[]).map(g=>g.village+':'+(g.cohorts.reduce((a,b)=>a+b,0)).toFixed(1)+'/b'+g.bearers.length).join(' ');
    console.log(`y${yr} alive=${alive} K=${K===null?'?':K.toFixed(1)} aggs=[${aggs}] :: ${e.txt||e.text||JSON.stringify(e).slice(0,160)}`); } }
console.log('done', yr, 'alive', S.agents.filter(a=>!a.dead).length);

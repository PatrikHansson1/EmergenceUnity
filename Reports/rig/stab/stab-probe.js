// STADS-STABILITETS-PROBE (D-616): kontrollmätning på HEAD. Bruk: node stab-probe.js <engine.js> <seed> <år>
globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;
globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;
const path=require('path'); const _m=require(path.resolve(process.argv[2]||'./engine-pace.js')); const E=globalThis.Emergence||_m;
const YEAR=144, seed=Number(process.argv[3]||97013), years=Number(process.argv[4]||300);
const S=E.createWorld(seed); S.silent=true;
let clockYear=null, qCraftMax=1, popMax=0; const tenures={}; const open={}; const cycles=[]; let yr=0;
while(yr<years && !S.ended){
  const target=(yr+1)*YEAR; while(S.tick<target&&!S.ended)E.tickWorld(S); yr++;
  const alive=S.agents.filter(a=>!a.dead).length; let aggPop=0; for(const g of (S.aggregates||[])) aggPop+=g.cohorts[0]+g.cohorts[1]+g.cohorts[2]+g.cohorts[3];
  popMax=Math.max(popMax, alive+aggPop);
  if(S._qCraft>qCraftMax)qCraftMax=S._qCraft;
  if(clockYear===null && S.knowledge && S.knowledge.clock && S.knowledge.clock.status==='alive') clockYear=yr;
  const now=new Set((S.aggregates||[]).map(g=>g.village));
  for(const v of now) if(!(v in open)) open[v]=yr;
  for(const v of Object.keys(open)) if(!now.has(v)){ const t=yr-open[v]; (tenures[v]=tenures[v]||[]).push(t); cycles.push({village:v,from:open[v],to:yr,tenure:t}); delete open[v]; }
}
for(const v of Object.keys(open)){ const t=yr-open[v]; (tenures[v]=tenures[v]||[]).push(t); cycles.push({village:v,from:open[v],to:null,tenure:t,ongoing:true}); }
const allT=cycles.map(c=>c.tenure).sort((a,b)=>a-b); const med=allT.length?allT[Math.floor(allT.length/2)]:null;
const perCity={}; for(const c of cycles) perCity[c.village]=(perCity[c.village]||0)+1;
console.log(JSON.stringify({seed,years,ended:S.ended,endYear:yr,clockYear,qCraftMax:+qCraftMax.toFixed(3),popMax,folds:cycles.length,medianTenure:med,maxCyclesPerCity:Math.max(0,...Object.values(perCity)),perCity,cycles}));

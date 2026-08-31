// stab-probe2: som stab-probe men med löpande utskrift var 25:e år (stderr) och tidsbudget. node stab-probe2.js <engine> <seed> <år> [budgetSek]
globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;
const path=require('path'); const _m=require(path.resolve(process.argv[2])); const E=globalThis.Emergence||_m;
const YEAR=144, seed=Number(process.argv[3]||97013), years=Number(process.argv[4]||300), budget=Number(process.argv[5]||500)*1000;
const t0=Date.now(); const F=process.env.FOUNDERS?[{name:'Ask the First',traits:{curiosity:0.9,social:0.4,diligence:0.6,conformity:0.3}},{name:'Embla the First'},{traits:{social:0.85}},null]:undefined; const S=E.createWorld(seed,F); S.silent=true;
let clockYear=null,qCraftMax=1,popMax=0; const open={}; const cycles=[]; let yr=0; let aggEv=0;
while(yr<years&&!S.ended&&Date.now()-t0<budget){
  const target=(yr+1)*YEAR; while(S.tick<target&&!S.ended)E.tickWorld(S); yr++;
  const alive=S.agents.filter(a=>!a.dead).length; let aggPop=0; for(const g of (S.aggregates||[]))aggPop+=g.cohorts[0]+g.cohorts[1]+g.cohorts[2]+g.cohorts[3];
  popMax=Math.max(popMax,alive+aggPop); if(S._qCraft>qCraftMax)qCraftMax=S._qCraft;
  if(clockYear===null&&S.knowledge&&S.knowledge.clock&&S.knowledge.clock.status==='alive')clockYear=yr;
  const now=new Set((S.aggregates||[]).map(g=>g.village));
  for(const v of now)if(!(v in open))open[v]=yr;
  for(const v of Object.keys(open))if(!now.has(v)){cycles.push({village:v,from:open[v],to:yr,tenure:yr-open[v]});delete open[v];}
  if(yr%25===0)process.stderr.write(`y${yr} alive=${alive} agg=${(S.aggregates||[]).length} aggPop=${aggPop.toFixed(0)} folds=${cycles.length} ${((Date.now()-t0)/1000).toFixed(0)}s\n`);
}
for(const v of Object.keys(open))cycles.push({village:v,from:open[v],to:null,tenure:yr-open[v],ongoing:true});
const allT=cycles.map(c=>c.tenure).sort((a,b)=>a-b); const med=allT.length?allT[Math.floor(allT.length/2)]:null;
const perCity={}; for(const c of cycles)perCity[c.village]=(perCity[c.village]||0)+1;
console.log(JSON.stringify({seed,years,endYear:yr,ended:S.ended,budgetHit:yr<years&&!S.ended,secs:Math.round((Date.now()-t0)/1000),clockYear,qCraftMax:+qCraftMax.toFixed(3),popMax:Math.round(popMax),folds:cycles.length,medianTenure:med,maxCyclesPerCity:Math.max(0,...Object.values(perCity)),perCity,cycles}));

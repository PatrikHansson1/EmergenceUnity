// bake-deep.js <seed> <years> — G1-djupmätning (D-627): full fysik, loggar EN rad per simår
// (år, ms, levande, aggPop, kumulativ tid) till bake-<seed>.progress.csv + slutrad DONE.
// Överlever avbrott: csv:n appendas löpande. Kör via RUN_NODEBAKE.trigger: "bake-deep.js 97013 3000".
globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;
globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;
const fs=require('fs'),path=require('path');
const _m=require(path.join(__dirname,'..','engine-pace.js')); const E=globalThis.Emergence||_m;
const YEAR=144, seed=Number(process.argv[2]||97013), years=Number(process.argv[3]||3000);
const csv=path.join(__dirname,'bake-'+seed+'.progress.csv');
fs.appendFileSync(csv,'# start '+new Date().toISOString()+' seed='+seed+' years='+years+' node='+process.version+'\n');
const S=E.createWorld(seed); S.silent=true; const t0=Date.now(); let yr=0;
while(yr<years&&!S.ended){
  const ty=Date.now(); const target=(yr+1)*YEAR;
  while(S.tick<target&&!S.ended)E.tickWorld(S); yr++;
  const alive=S.agents.filter(a=>!a.dead).length; let ap=0; for(const g of (S.aggregates||[]))ap+=g.cohorts[0]+g.cohorts[1]+g.cohorts[2]+g.cohorts[3];
  fs.appendFileSync(csv,yr+','+(Date.now()-ty)+','+alive+','+Math.round(ap)+','+Math.round((Date.now()-t0)/1000)+'\n');
}
const clock=S.knowledge&&S.knowledge.clock&&S.knowledge.clock.status==='alive';
const press=S.knowledge&&S.knowledge.printpress&&S.knowledge.printpress.status==='alive';
fs.appendFileSync(csv,'# DONE '+new Date().toISOString()+' endYear='+yr+' ended='+S.ended+' totalSecs='+Math.round((Date.now()-t0)/1000)+' clock='+!!clock+' printpress='+!!press+' events='+(S.events||[]).length+'\n');
console.log('DONE',seed,yr,Math.round((Date.now()-t0)/1000)+'s');

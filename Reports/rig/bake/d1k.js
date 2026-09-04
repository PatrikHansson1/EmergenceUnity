// d1k.js — D1 varv 1 k-underlag (D-701): 8 kanonfrön × 1200 år på varv-1-tvillingen (engine-d1v1).
// Windows-native via RUN_NODEBAKE (cwd Reports/rig/bake -> kör: "../d1/d1k.js x ."). Skriver
// per-frö hyposammanfattning + trials-fördelning till Reports/rig/d1/d1k-results.txt.
// RIGGVARV: inga M4-frön, ingen mätning mot M-D1-måltal. G6-spärren respekteras.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const ENG=path.join(__dirname,'..','d1','engine-d1v1.js');
const src=fs.readFileSync(ENG,'utf8');
const out=path.join(__dirname,'..','d1','d1k-results.txt');
fs.appendFileSync(out,'# start '+new Date().toISOString()+' engine=engine-d1v1 sha='+crypto.createHash('sha256').update(src,'utf8').digest('hex').slice(0,8)+' node='+process.version+'\n');
const founders=[{name:'Ask the First',traits:{curiosity:0.9,social:0.4,diligence:0.6,conformity:0.3}},{name:'Embla the First'},{traits:{social:0.85}},null];
const SEEDS=[[97013,null],[4242,null],[20260718,null],[31415,null],[2323,null],[1618,null],[777,null],[97013,founders]];
for(const [seed,f] of SEEDS){
  const tag=f?seed+'-founders':String(seed);
  const m={exports:{}};new Function('module','exports','require','process','globalThis',src)(m,m.exports,require,process,globalThis);
  const E=m.exports;const YEAR=E.YEAR||144;const t0=Date.now();
  const S=f?E.createWorld(seed,f):E.createWorld(seed);S.silent=true;
  while(Math.floor(S.tick/YEAR)<1200&&!S.ended)E.tickWorld(S);
  const hy=Object.values(S.hypos||{});
  const dist={};for(const h of hy)dist[h.trials]=(dist[h.trials]||0)+1;
  let immortal=0;for(const h of hy){if(h.status!=='alive')continue;let live=false;for(const a of S.agents){if(a.id===h.holder&&!a.dead){live=true;break;}}if(!live)immortal++;}
  fs.appendFileSync(out,JSON.stringify({tag,endYear:Math.floor(S.tick/YEAR),ended:S.ended,secs:Math.round((Date.now()-t0)/1000),
    hypos:hy.length,alive:hy.filter(h=>h.status==='alive').length,dead:hy.filter(h=>h.status==='dead').length,held:hy.filter(h=>h.status==='held').length,
    maxTrials:Math.max(0,...hy.map(h=>h.trials)),mutations:hy.reduce((s,h)=>s+h.mutations,0),trialsDist:dist,immortalAfterSweep:immortal,
    needs:hy.reduce((o,h)=>{o[h.need||'?']=(o[h.need||'?']||0)+1;return o;},{}),
    pop:S.agents.filter(a=>!a.dead).length,failedExps:S.stats.failedExperiments})+'\n');
  console.log('DONE',tag);
}
fs.appendFileSync(out,'# DONE '+new Date().toISOString()+'\n');

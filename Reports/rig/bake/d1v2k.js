// d1v2k.js — D1 VARV 2-mätning mot V2-delmålen (D-716): 8 riggfrön (kanon) × 1200 år på v22-tvillingen.
// Windows-native via RUN_NODEBAKE. Skriver per-frö rad till Reports/rig/d1/d1v2k-results.txt:
// ways (födda/döda/levande, per behov, namn), hypos-sammanfattning, TECH-id-mängden år 1200
// (för V2-c icke-kannibalisering mot d1k). INGA M4-frön.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const ENG=path.join(__dirname,'..','d1','engine-v22.js');
const src=fs.readFileSync(ENG,'utf8');
const out=path.join(__dirname,'..','d1','d1v2k-results.txt');
fs.appendFileSync(out,'# start '+new Date().toISOString()+' engine=engine-v22 sha='+crypto.createHash('sha256').update(src,'utf8').digest('hex').slice(0,8)+' node='+process.version+'\n');
const founders=[{name:'Ask the First',traits:{curiosity:0.9,social:0.4,diligence:0.6,conformity:0.3}},{name:'Embla the First'},{traits:{social:0.85}},null];
const SEEDS=[[97013,null],[4242,null],[20260718,null],[31415,null],[2323,null],[1618,null],[777,null],[97013,founders]];
for(const [seed,f] of SEEDS){
  const tag=f?seed+'-founders':String(seed);
  const m={exports:{}};new Function('module','exports','require','process','globalThis',src)(m,m.exports,require,process,globalThis);
  const E=m.exports;const YEAR=E.YEAR||144;const t0=Date.now();
  const S=f?E.createWorld(seed,f):E.createWorld(seed);S.silent=true;
  while(Math.floor(S.tick/YEAR)<1200&&!S.ended)E.tickWorld(S);
  const hy=Object.values(S.hypos||{}), wy=Object.values(S.ways||{});
  const dist={};for(const h of hy)dist[h.trials]=(dist[h.trials]||0)+1;
  const techs=Object.keys(S.knowledge||{}).filter(k=>S.knowledge[k]).sort();
  fs.appendFileSync(out,JSON.stringify({tag,endYear:Math.floor(S.tick/YEAR),ended:S.ended,secs:Math.round((Date.now()-t0)/1000),
    hypos:hy.length,maxTrials:Math.max(0,...hy.map(h=>h.trials)),trialsDist:dist,
    ways:wy.length,waysAlive:wy.filter(w=>w.status==='alive').length,waysDead:wy.filter(w=>w.status==='dead').length,
    wayDetail:wy.map(w=>({n:w.name,need:w.need,tech:w.tech,born:w.yearBorn,st:w.status,bearers:w.bearers.length})),
    wayEvs:(S.events||[]).filter(e=>e.type==='wayBorn'||e.type==='wayLost').length,
    techsAt1200:techs.length,techList:techs.join(','),
    pop:S.agents.filter(a=>!a.dead).length,failedExps:S.stats.failedExperiments})+'\n');
  console.log('DONE',tag,'ways',wy.length);
}
fs.appendFileSync(out,'# DONE '+new Date().toISOString()+'\n');

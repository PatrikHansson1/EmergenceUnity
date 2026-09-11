// m4o.js — KONFIRMATORISKA OMVARVET (prereg LÅST D1-OMVARV-PREREG-LOCKED-2026-09, sha
// 699cfd40...; D-750): SAMMA 20 frön x 3000 år som m4.js men på engine-v23 (k_sick=9).
// Fält = m4.js:s fulla uppsättning (för m4-dom.py) PLUS trialsDist/tailByNeed/wayHypos
// (m4t-instrumenteringen, D-741-läxan). Resumbart per frö. Ingen dom i skriptet;
// dom först vid # DONE 20/20 med M-O1-O6b. Hälsokoll ENDAST via radantal (F9).
const fs=require('fs'),path=require('path'),crypto=require('crypto');
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const ENG=path.join(__dirname,'..','d1','engine-v23.js');
const src=fs.readFileSync(ENG,'utf8');
const sha=crypto.createHash('sha256').update(src,'utf8').digest('hex').slice(0,8);
if(sha!=='2554ab41'){console.error('SHA-ASSERT FALLERADE: '+sha);process.exit(1);}
const out=path.join(__dirname,'..','d1','m4o-results.txt');
const founders=[{name:'Ask the First',traits:{curiosity:0.9,social:0.4,diligence:0.6,conformity:0.3}},{name:'Embla the First'},{traits:{social:0.85}},null];
const FRESH=[1287500642,1270723023,1253945404,1237167785,1220390166,1203612547,1186834928,1438499213,1421721594,547441174,564218793,513885936];
const CANON=[[97013,null],[4242,null],[20260718,null],[31415,null],[2323,null],[1618,null],[777,null],[97013,founders]];
const JOBS=FRESH.map(s=>({tag:String(s),seed:s,f:null,cls:'fresh'})).concat(CANON.map(([s,f])=>({tag:f?s+'-founders':String(s),seed:s,f,cls:'canon'})));
const done=new Set();
if(fs.existsSync(out))for(const line of fs.readFileSync(out,'utf8').split('\n')){try{const r=JSON.parse(line);if(r&&r.tag&&r.secs!==undefined)done.add(r.tag);}catch(e){}}
fs.appendFileSync(out,'# start '+new Date().toISOString()+' engine=engine-v23 sha='+sha+' node='+process.version+' skip=['+[...done].join(',')+']\n');
function evType(S,ref){if(typeof ref!=='string'||!ref.startsWith('ev:'))return ref;const e=S.events[Number(ref.slice(3))];return e?e.type+'@y'+e.year:'MISSING';}
for(const job of JOBS){
  if(done.has(job.tag)){console.log('SKIP',job.tag);continue;}
  const m={exports:{}};new Function('module','exports','require','process','globalThis',src)(m,m.exports,require,process,globalThis);
  const E=m.exports;const YEAR=E.YEAR||144;const t0=Date.now();
  const S=job.f?E.createWorld(job.seed,job.f):E.createWorld(job.seed);S.silent=true;
  let at1200=null;
  while(Math.floor(S.tick/YEAR)<3000&&!S.ended){
    E.tickWorld(S);
    const y=Math.floor(S.tick/YEAR);
    if(y>=1200&&at1200===null)at1200={year:y,pop:S.agents.filter(a=>!a.dead).length,ended:S.ended};
  }
  const hy=Object.values(S.hypos||{}), wy=Object.values(S.ways||{});
  const disc=S.events.filter(e=>e.type==='tech'||e.type==='wayBorn').map(e=>({t:e.type,id:e.tech||e.way,year:e.year,tick:e.tick,agent:e.agent!==undefined}));
  const wayEvs=S.events.filter(e=>e.type==='wayBorn'||e.type==='wayLost').map(e=>{
    const o={t:e.type,way:e.way,year:e.year,tick:e.tick,agent:e.agent!==undefined,causes:(e.causes||[]).map(c=>evType(S,c))};
    if(e.type==='wayBorn'&&e.causes)for(const c of e.causes){if(typeof c==='string'&&c.startsWith('ev:')){const ce=S.events[Number(c.slice(3))];if(ce&&ce.type==='corrected')o.correctedCauses=(ce.causes||[]).map(x=>evType(S,x));}}
    return o;});
  const techs=Object.keys(S.knowledge||{}).filter(k=>S.knowledge[k]).sort();
  const dist={};for(const h of hy)dist[h.trials]=(dist[h.trials]||0)+1;
  const tail={};for(const h of hy){const n=h.need||'?';(tail[n]=tail[n]||{});for(let K=6;K<=14;K++)if(h.trials>=K)tail[n][K]=(tail[n][K]||0)+1;}
  const wayHy=hy.filter(h=>h.wayId).map(h=>({tech:h.tech,need:h.need,trials:h.trials,mut:h.mutations}));
  fs.appendFileSync(out,JSON.stringify({tag:job.tag,cls:job.cls,endYear:Math.floor(S.tick/YEAR),ended:S.ended,secs:Math.round((Date.now()-t0)/1000),
    at1200,hypos:hy.length,ways:wy.length,waysAlive:wy.filter(w=>w.status==='alive').length,
    wayDetail:wy.map(w=>({n:w.name,need:w.need,tech:w.tech,recipe:w.recipe,born:w.yearBorn,st:w.status,bearers:w.bearers.length})),
    wayEvs,discCount:disc.length,disc,techsAtEnd:techs.length,
    pop:S.agents.filter(a=>!a.dead).length,failedExps:S.stats.failedExperiments,
    trialsDist:dist,tailByNeed:tail,wayHypos:wayHy})+'\n');
  console.log('DONE',job.tag,'ways',wy.length,'endYear',Math.floor(S.tick/YEAR));
}
const done2=new Set();
for(const line of fs.readFileSync(out,'utf8').split('\n')){try{const r=JSON.parse(line);if(r&&r.tag&&r.secs!==undefined)done2.add(r.tag);}catch(e){}}
fs.appendFileSync(out,(done2.size>=20?'# DONE 20/20 ':'# PARTIAL '+done2.size+'/20 ')+new Date().toISOString()+'\n');

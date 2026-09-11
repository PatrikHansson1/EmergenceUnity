// m4t.js — DIAGNOSTISKT OMLOPP (D-744, Patrik-beslutat 6 sep): samma 20 frön x 3000 år som m4.js,
// deterministisk återuppspelning, READ-ONLY — enda syftet är trials-svansdatan som m4.js missade
// (D-741 instrumenteringslucka). INGEN motorändring, INGEN dom i skriptet. Resumbart per frö.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const ENG=path.join(__dirname,'..','d1','engine-v22.js');
const src=fs.readFileSync(ENG,'utf8');
const sha=crypto.createHash('sha256').update(src,'utf8').digest('hex').slice(0,8);
if(sha!=='952f21f6'){console.error('SHA-ASSERT FALLERADE: '+sha);process.exit(1);}
const out=path.join(__dirname,'..','d1','m4t-results.txt');
const founders=[{name:'Ask the First',traits:{curiosity:0.9,social:0.4,diligence:0.6,conformity:0.3}},{name:'Embla the First'},{traits:{social:0.85}},null];
const FRESH=[1287500642,1270723023,1253945404,1237167785,1220390166,1203612547,1186834928,1438499213,1421721594,547441174,564218793,513885936];
const CANON=[[97013,null],[4242,null],[20260718,null],[31415,null],[2323,null],[1618,null],[777,null],[97013,founders]];
const JOBS=FRESH.map(s=>({tag:String(s),seed:s,f:null})).concat(CANON.map(([s,f])=>({tag:f?s+'-founders':String(s),seed:s,f})));
const done=new Set();
if(fs.existsSync(out))for(const line of fs.readFileSync(out,'utf8').split('\n')){try{const r=JSON.parse(line);if(r&&r.tag&&r.secs!==undefined)done.add(r.tag);}catch(e){}}
fs.appendFileSync(out,'# start '+new Date().toISOString()+' engine=engine-v22 sha='+sha+' node='+process.version+' skip=['+[...done].join(',')+']\n');
for(const job of JOBS){
  if(done.has(job.tag)){console.log('SKIP',job.tag);continue;}
  const m={exports:{}};new Function('module','exports','require','process','globalThis',src)(m,m.exports,require,process,globalThis);
  const E=m.exports;const YEAR=E.YEAR||144;const t0=Date.now();
  const S=job.f?E.createWorld(job.seed,job.f):E.createWorld(job.seed);S.silent=true;
  while(Math.floor(S.tick/YEAR)<3000&&!S.ended)E.tickWorld(S);
  const hy=Object.values(S.hypos||{});
  const dist={};for(const h of hy)dist[h.trials]=(dist[h.trials]||0)+1;
  // svans per behov: antal hypoteser med trials >= K, per need (kanal-kontrafaktiken)
  const tail={};for(const h of hy){const n=h.need||'?';(tail[n]=tail[n]||{});for(let K=6;K<=14;K++)if(h.trials>=K)tail[n][K]=(tail[n][K]||0)+1;}
  // way-födda hypotesers slutliga trials (hur långt de fortsatte efter 8)
  const wayHy=hy.filter(h=>h.wayId).map(h=>({tech:h.tech,need:h.need,trials:h.trials,mut:h.mutations}));
  fs.appendFileSync(out,JSON.stringify({tag:job.tag,endYear:Math.floor(S.tick/YEAR),ended:S.ended,
    secs:Math.round((Date.now()-t0)/1000),hypos:hy.length,maxTrials:Math.max(0,...hy.map(h=>h.trials)),
    trialsDist:dist,tailByNeed:tail,wayHypos:wayHy,ways:Object.keys(S.ways||{}).length,
    pop:S.agents.filter(a=>!a.dead).length})+'\n');
  console.log('DONE',job.tag,'maxTrials',Math.max(0,...hy.map(h=>h.trials)));
}
const done2=new Set();
for(const line of fs.readFileSync(out,'utf8').split('\n')){try{const r=JSON.parse(line);if(r&&r.tag&&r.secs!==undefined)done2.add(r.tag);}catch(e){}}
fs.appendFileSync(out,(done2.size>=20?'# DONE 20/20 ':'# PARTIAL '+done2.size+'/20 ')+new Date().toISOString()+'\n');

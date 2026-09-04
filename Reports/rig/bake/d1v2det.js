// d1v2det.js — V2-d determinismomkörning (D-716): ENBART frö 1618 x 1200 år på v22-tvillingen,
// KÖRS TVÅ GÅNGER i färska modulinstanser i samma jobb. Jämför payloads (exkl. secs) och
// skriver IDENTICAL-verdikt. Facit jämförs sedan även mot d1v2k-raden för 1618. INGA M4-frön.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const ENG=path.join(__dirname,'..','d1','engine-v22.js');
const src=fs.readFileSync(ENG,'utf8');
const sha=crypto.createHash('sha256').update(src,'utf8').digest('hex').slice(0,8);
if(sha!=='952f21f6'){console.error('SHA-ASSERT FALLERADE: '+sha);process.exit(1);}
const out=path.join(__dirname,'..','d1','d1v2det-results.txt');
fs.appendFileSync(out,'# start '+new Date().toISOString()+' engine=engine-v22 sha='+sha+' node='+process.version+'\n');
function run(seed){
  const m={exports:{}};new Function('module','exports','require','process','globalThis',src)(m,m.exports,require,process,globalThis);
  const E=m.exports;const YEAR=E.YEAR||144;const t0=Date.now();
  const S=E.createWorld(seed);S.silent=true;
  while(Math.floor(S.tick/YEAR)<1200&&!S.ended)E.tickWorld(S);
  const hy=Object.values(S.hypos||{}), wy=Object.values(S.ways||{});
  const dist={};for(const h of hy)dist[h.trials]=(dist[h.trials]||0)+1;
  const techs=Object.keys(S.knowledge||{}).filter(k=>S.knowledge[k]).sort();
  return {payload:{tag:'1618',endYear:Math.floor(S.tick/YEAR),ended:S.ended,
    hypos:hy.length,maxTrials:Math.max(0,...hy.map(h=>h.trials)),trialsDist:dist,
    ways:wy.length,waysAlive:wy.filter(w=>w.status==='alive').length,waysDead:wy.filter(w=>w.status==='dead').length,
    wayDetail:wy.map(w=>({n:w.name,need:w.need,tech:w.tech,born:w.yearBorn,st:w.status,bearers:w.bearers.length})),
    wayEvs:(S.events||[]).filter(e=>e.type==='wayBorn'||e.type==='wayLost').length,
    techsAt1200:techs.length,techList:techs.join(','),
    pop:S.agents.filter(a=>!a.dead).length,failedExps:S.stats.failedExperiments},secs:Math.round((Date.now()-t0)/1000)};
}
const r1=run(1618);fs.appendFileSync(out,JSON.stringify(Object.assign({run:1,secs:r1.secs},r1.payload))+'\n');console.log('DONE run1');
const r2=run(1618);fs.appendFileSync(out,JSON.stringify(Object.assign({run:2,secs:r2.secs},r2.payload))+'\n');console.log('DONE run2');
const a=JSON.stringify(r1.payload),b=JSON.stringify(r2.payload);
fs.appendFileSync(out,'# IDENTICAL '+(a===b)+'\n# DONE '+new Date().toISOString()+'\n');
console.log('IDENTICAL',a===b);

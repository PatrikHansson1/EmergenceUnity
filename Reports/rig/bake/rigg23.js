// rigg23.js — RIGGVARVET för D1-omvarvet (prereg rev 2 f21456be §4; D-748). SKRIVET i förväg
// (väckning 156); KÖRS först vid låset, EFTER mkomvarv.py skapat engine-v23.js och EFTER att
// m4t avslutat (EN nodprocess åt gången). 8 kanonfrön x 1200 år, BÅDA motorerna (v22 + v23),
// per frö: way-räkning, way-födselår, tillståndsfingeravtryck (identitetsjämförelse).
// Förutsägelserna (§4): way-antal v23 <= v22 per frö; frön UTAN sick-way <=1200 i v22 ska ha
// IDENTISKT fingeravtryck (divergens kaskaderar annars från första undertryckta födseln).
// Resumbart per (motor,frö). Ingen dom i skriptet — jämförelsen görs vid avläsning.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const ENGINES=[['v22',path.join(__dirname,'..','d1','engine-v22.js'),'952f21f6'],
               ['v23',path.join(__dirname,'..','d1','engine-v23.js'),null]];
const out=path.join(__dirname,'..','d1','rigg23-results.txt');
const founders=[{name:'Ask the First',traits:{curiosity:0.9,social:0.4,diligence:0.6,conformity:0.3}},{name:'Embla the First'},{traits:{social:0.85}},null];
const CANON=[[97013,null],[4242,null],[20260718,null],[31415,null],[2323,null],[1618,null],[777,null],[97013,founders]];
const srcs={};
for(const [name,p,want] of ENGINES){
  const s=fs.readFileSync(p,'utf8');
  const sha=crypto.createHash('sha256').update(s,'utf8').digest('hex').slice(0,8);
  if(want&&sha!==want){console.error('SHA-ASSERT '+name+' FALLERADE: '+sha);process.exit(1);}
  if(name==='v23'&&s.indexOf('v23 D1-omvarv K1')<0){console.error('engine-v23.js saknar K1-markören — kör mkomvarv.py först');process.exit(1);}
  srcs[name]={src:s,sha};
}
const done=new Set();
if(fs.existsSync(out))for(const line of fs.readFileSync(out,'utf8').split('\n')){try{const r=JSON.parse(line);if(r&&r.key&&r.secs!==undefined)done.add(r.key);}catch(e){}}
fs.appendFileSync(out,'# start '+new Date().toISOString()+' v22='+srcs.v22.sha+' v23='+srcs.v23.sha+' node='+process.version+' skip='+done.size+'\n');
function fpr(S){
  const techs=Object.keys(S.techs||{}).filter(t=>S.techs[t]&&S.techs[t].known).sort();
  const hy=Object.values(S.hypos||{}).map(h=>[h.id,h.tech,h.trials,h.mutations,h.status]).sort((a,b)=>a[0]<b[0]?-1:1);
  const ag=S.agents.filter(a=>!a.dead).length;
  return crypto.createHash('sha256').update(JSON.stringify({tick:S.tick,ag,techs,hy,stats:S.stats,ways:Object.keys(S.ways||{}).sort()}),'utf8').digest('hex').slice(0,16);
}
for(const [ename] of ENGINES){
  for(const [seed,f] of CANON){
    const tag=f?seed+'-founders':String(seed);const key=ename+':'+tag;
    if(done.has(key)){console.log('SKIP',key);continue;}
    const m={exports:{}};new Function('module','exports','require','process','globalThis',srcs[ename].src)(m,m.exports,require,process,globalThis);
    const E=m.exports;const YEAR=E.YEAR||144;const t0=Date.now();
    const S=f?E.createWorld(seed,f):E.createWorld(seed);S.silent=true;
    while(Math.floor(S.tick/YEAR)<1200&&!S.ended)E.tickWorld(S);
    const ways=Object.values(S.ways||{}).map(w=>({need:w.need,tech:w.tech,born:w.yearBorn,st:w.status}));
    fs.appendFileSync(out,JSON.stringify({key,engine:ename,tag,endYear:Math.floor(S.tick/YEAR),ended:S.ended,
      secs:Math.round((Date.now()-t0)/1000),ways:ways.length,wayDetail:ways,
      pop:S.agents.filter(a=>!a.dead).length,fpr:fpr(S)})+'\n');
    console.log('DONE',key,'ways',ways.length);
  }
}
const done2=new Set();
for(const line of fs.readFileSync(out,'utf8').split('\n')){try{const r=JSON.parse(line);if(r&&r.key&&r.secs!==undefined)done2.add(r.key);}catch(e){}}
fs.appendFileSync(out,(done2.size>=16?'# DONE 16/16 ':'# PARTIAL '+done2.size+'/16 ')+new Date().toISOString()+'\n');

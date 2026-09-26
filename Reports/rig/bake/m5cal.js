// m5cal.js — M5 REACH-KALIBRERING (prereg M5-PREREG-UTKAST-2026-09-12.md §3, D-771).
// MÄTNING på LÅSTA v24 (e2285e55) — motorn rörs ALDRIG (D-489). Ingen dom, inget lås i skriptet.
// Syfte: incidens av kandidat-tekniker + era + PER-VÄRLD-TID på 20 FÄRSKA frön × 3000 år,
// för att välja M5-predikat via DISKRIMINERINGSTEST (förkasta icke-diskriminerande; välj ALDRIG mot 1/100).
// Resumbart per frö. Frön: FNV1a32('M5cal/'+i) (separat prefix från de 100 M5-fröna). Ingen xp.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const ENG=path.join(__dirname,'..','m5','engine-v24.js');
const src=fs.readFileSync(ENG,'utf8');
const sha=crypto.createHash('sha256').update(src,'utf8').digest('hex').slice(0,8);
if(sha!=='e2285e55'){console.error('SHA-ASSERT FALLERADE (v24 e2285e55 väntat): '+sha);process.exit(1);}
const out=path.join(__dirname,'..','m5','m5cal-results.txt');
// ---- frödragning: FNV1a32('M5cal/'+i), hoppa kanonfrön + dubbletter, tills 20 ----
function FNV1a32(str){let h=2166136261>>>0;const b=Buffer.from(str,'utf8');for(let i=0;i<b.length;i++){h^=b[i];h=Math.imul(h,16777619)>>>0;}return h>>>0;}
const CANON=new Set([97013,4242,20260718,31415,2323,1618,777]);
const SEEDS=[];for(let i=1;SEEDS.length<20&&i<100000;i++){const s=FNV1a32('M5cal/'+i);if(CANON.has(s)||SEEDS.includes(s))continue;SEEDS.push(s);}
// ---- kandidat-tekniker (affordans-triaden) + ankare ----
const TARGET=['numbers','calendar','coinage','clock','printpress','computing','law','school','science','writing'];
const done=new Set();
if(fs.existsSync(out))for(const line of fs.readFileSync(out,'utf8').split('\n')){try{const r=JSON.parse(line);if(r&&r.tag&&r.secs!==undefined)done.add(r.tag);}catch(e){}}
fs.appendFileSync(out,'# start '+new Date().toISOString()+' engine=engine-v24 sha='+sha+' node='+process.version+' N='+SEEDS.length+' skip=['+[...done].join(',')+']\n');
for(const seed of SEEDS){
  const tag=String(seed);
  if(done.has(tag)){console.log('SKIP',tag);continue;}
  const m={exports:{}};new Function('module','exports','require','process','globalThis',src)(m,m.exports,require,process,globalThis);
  const E=m.exports;const YEAR=E.YEAR||144;const t0=Date.now();
  const S=E.createWorld(seed);S.silent=true; // ingen founders, ingen xp — baslinjemotor
  while(Math.floor(S.tick/YEAR)<3000&&!S.ended)E.tickWorld(S);
  const dna=E.computeDNA(S);
  const k=S.knowledge||{};
  const eraN=E.worldEra(S);const eraNm=E.eraName(eraN);
  const reach={};for(const id of TARGET)reach[id]=(k[id]?(k[id].yearBorn!=null?k[id].yearBorn:true):null);
  fs.appendFileSync(out,JSON.stringify({tag,seed,endYear:Math.floor(S.tick/YEAR),ended:S.ended,endedYear:S.endedYear||null,
    secs:Math.round((Date.now()-t0)/1000),pop:S.agents.filter(a=>!a.dead).length,maxPop:S.maxPop,
    era:eraN,eraName:eraNm,knowN:(dna&&dna.knowledgeCount)||Object.keys(k).length,
    reach})+'\n');
  console.log('DONE',tag,'era',eraN,eraNm,'endYear',Math.floor(S.tick/YEAR),'clock',reach.clock,'coinage',reach.coinage,'computing',reach.computing);
}
const done2=new Set();
for(const line of fs.readFileSync(out,'utf8').split('\n')){try{const r=JSON.parse(line);if(r&&r.tag&&r.secs!==undefined)done2.add(r.tag);}catch(e){}}
fs.appendFileSync(out,(done2.size>=20?'# DONE 20/20 ':'# PARTIAL '+done2.size+'/20 ')+new Date().toISOString()+'\n');

// m5.js <shard> <nshards> [years=3000] — M5 KONFIRMATORISK MÄTNING (prereg LÅST M5-PREREG-LOCKED-2026-09-13.md,
// sha 25ce9491; D-776). 100 färska frön FNV1a32('M5/'+i) exkl. 8 kanon × 3000 år på LÅSTA v24 (e2285e55).
// Motorn rörs ALDRIG (D-489). M5-predikat = 'computing' föds (appliceras i ANALYSEN; scriptet records reach{}
// för ALLA kandidater så mätningen är predikat-oberoende). Sharded parallellt; egna filer per shard; resumbart;
// per-500-år progress = liveness. Ingen dom i scriptet; dom (§7-fallträd) görs när 100/100 kompletta.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const shard=Number(process.argv[2]||0), nsh=Number(process.argv[3]||1), YEARS=Number(process.argv[4]||3000);
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const ENG=path.join(__dirname,'..','m5','engine-v24.js');
const src=fs.readFileSync(ENG,'utf8');
const sha=crypto.createHash('sha256').update(src,'utf8').digest('hex').slice(0,8);
if(sha!=='e2285e55'){console.error('SHA-ASSERT FALLERADE (v24 e2285e55): '+sha);process.exit(1);}
const M5=path.join(__dirname,'..','m5');
const out=path.join(M5,`m5-results-${shard}.txt`);
const prog=path.join(M5,`m5-progress-${shard}.txt`);
const errf=path.join(M5,`m5-error-${shard}.txt`);
function FNV1a32(s){let h=2166136261>>>0;const b=Buffer.from(s,'utf8');for(let i=0;i<b.length;i++){h^=b[i];h=Math.imul(h,16777619)>>>0;}return h>>>0;}
const CANON=new Set([97013,4242,20260718,31415,2323,1618,777]);
// 100 färska M5-frön (kollision mot kanon + dubbletter ⇒ i+1000, upprepat)
const SEEDS=[]; const seen=new Set();
for(let i=1;SEEDS.length<100;i++){let j=i; let s=FNV1a32('M5/'+j); let guard=0;
  while((CANON.has(s)||seen.has(s))&&guard<50){j+=1000;s=FNV1a32('M5/'+j);guard++;}
  seen.add(s); SEEDS.push(s);}
const TARGET=['numbers','calendar','coinage','clock','printpress','computing','law','school','science','writing'];
const done=new Set();
if(fs.existsSync(out))for(const line of fs.readFileSync(out,'utf8').split('\n')){try{const r=JSON.parse(line);if(r&&r.tag&&r.secs!==undefined)done.add(r.tag);}catch(e){}}
fs.appendFileSync(out,`# start shard ${shard}/${nsh} ${new Date().toISOString()} engine=v24 sha=${sha} node=${process.version} N=100 years=${YEARS} predicate=computing skip=${done.size}\n`);
let ndone=0;
for(let idx=0;idx<SEEDS.length;idx++){
  if(idx%nsh!==shard)continue;
  const seed=SEEDS[idx], tag=String(seed);
  if(done.has(tag)){continue;}
  try{
    const m={exports:{}};new Function('module','exports','require','process','globalThis',src)(m,m.exports,require,process,globalThis);
    const E=m.exports;const YEAR=E.YEAR||144;const t0=Date.now();
    const S=E.createWorld(seed);S.silent=true;
    let lastP=0;
    while(Math.floor(S.tick/YEAR)<YEARS&&!S.ended){E.tickWorld(S);const y=Math.floor(S.tick/YEAR);
      if(y>=lastP+500){lastP=y;try{fs.writeFileSync(prog,`shard ${shard} idx${idx} tag ${tag} y${y} pop${S.agents.filter(a=>!a.dead).length} era${E.worldEra(S)} ${Math.round((Date.now()-t0)/1000)}s ${new Date().toISOString()}\n`);}catch(e){}}}
    const dna=E.computeDNA(S);const k=S.knowledge||{};const eraN=E.worldEra(S);
    const reach={};for(const id of TARGET)reach[id]=(k[id]?(k[id].yearBorn!=null?k[id].yearBorn:true):null);
    // krönike-predikat för computing: agent + causes (för maskinell verifiering av vad/vem/varför)
    let compEv=null;
    if(k.computing){const e=(S.events||[]).find(ev=>ev.type==='tech'&&ev.tech==='computing');
      if(e)compEv={agent:e.agent!==undefined,year:e.year,causes:(e.causes||[]).length};}
    fs.appendFileSync(out,JSON.stringify({tag,seed,endYear:Math.floor(S.tick/YEAR),ended:S.ended,endedYear:S.endedYear||null,
      secs:Math.round((Date.now()-t0)/1000),pop:S.agents.filter(a=>!a.dead).length,maxPop:S.maxPop,
      era:eraN,eraName:E.eraName(eraN),knowN:(dna&&dna.knowledgeCount)||Object.keys(k).length,
      computingBuilt:(reach.computing!=null),computingYear:reach.computing,compEv,reach})+'\n');
    ndone++;
    console.log('DONE',tag,'era',eraN,'computing',reach.computing);
  }catch(e){try{fs.appendFileSync(errf,tag+': '+(e.stack||e.message)+'\n');}catch(_){}console.error('ERR',tag,e.message);}
}
fs.appendFileSync(out,`# shard ${shard}/${nsh} DONE ${new Date().toISOString()} (+${ndone} denna körning)\n`);

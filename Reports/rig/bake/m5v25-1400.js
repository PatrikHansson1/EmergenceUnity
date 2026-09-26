// m5v25-1400.js <shard> <nshards> [years=1400] — M-A5 JÄMFÖRELSEMÄTNING: samma 100 M5-frön på v25b men vid 1400 år
// (samma horisont + population som M5W4:s frontlinje-H*=0,372 mättes på, D-822). Räknas mot M-A5:s 0,30-band.
// Förhandsbindning (D-869/Patrik 26 sep): frontlinje-H* på fork-dugliga >=0,30 => M-A5 GRON; annars akta rod => §5-dom.
// Ursprunglig header: M5 OMMÄTNING PÅ v25b (BREDD-A-PREREG-LOCKED-2026-09-24 §2 huvudmätning; M5-PREREG-LOCKED
// sha 25ce9491 OFÖRÄNDRAT: samma 100 frön, 3000 år, predikat computing ⇒ dömer M-M5b OCH M-A1..A6). Motor = LIVE v25b (5dd13837).
// Tillägg mot m5.js: reach{} utökad med frontlinjen (electricity..ai), vote för atompower/computing (varför grenen valdes), svältdöd.
// m5.js <shard> <nshards> [years=3000] — M5 KONFIRMATORISK MÄTNING (prereg LÅST M5-PREREG-LOCKED-2026-09-13.md,
// sha 25ce9491; D-776). 100 färska frön FNV1a32('M5/'+i) exkl. 8 kanon × 3000 år på LÅSTA v24 (e2285e55).
// Motorn rörs ALDRIG (D-489). M5-predikat = 'computing' föds (appliceras i ANALYSEN; scriptet records reach{}
// för ALLA kandidater så mätningen är predikat-oberoende). Sharded parallellt; egna filer per shard; resumbart;
// per-500-år progress = liveness. Ingen dom i scriptet; dom (§7-fallträd) görs när 100/100 kompletta.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const shard=Number(process.argv[2]||0), nsh=Number(process.argv[3]||1), YEARS=Number(process.argv[4]||1400);
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const ENG=path.join(__dirname,'..','..','..','Assets','StreamingAssets','Emergence','emergence-engine.js'); // LIVE v25b (BREDD A)
const src=fs.readFileSync(ENG,'utf8');
const sha=crypto.createHash('sha256').update(src,'utf8').digest('hex').slice(0,8);
if(sha!=='5dd13837'){console.error('SHA-ASSERT FALLERADE (v25b 5dd13837): '+sha);process.exit(1);}
const M5=path.join(__dirname,'..','m5v25-1400'); if(!fs.existsSync(M5))fs.mkdirSync(M5);
// DUBBELSTARTSKYDD (D-856): bryggan kan leverera RUN_LOCALNODE.trigger två gånger — en andra instans av samma skärva avslutar tyst.
const lock=path.join(M5,`m5v25-1400-lock-${shard}.txt`);
if(fs.existsSync(lock)&&Date.now()-fs.statSync(lock).mtimeMs<10*60*1000){console.error('DUBBELSTART: 1400-skärva '+shard+' kör redan (lås < 10 min) — avslutar');process.exit(0);}
fs.writeFileSync(lock,new Date().toISOString()); setInterval(()=>{try{fs.writeFileSync(lock,new Date().toISOString());}catch(e){}},60*1000).unref();
const out=path.join(M5,`m5v25-1400-results-${shard}.txt`);
const prog=path.join(M5,`m5v25-1400-progress-${shard}.txt`);
const errf=path.join(M5,`m5v25-1400-error-${shard}.txt`);
function FNV1a32(s){let h=2166136261>>>0;const b=Buffer.from(s,'utf8');for(let i=0;i<b.length;i++){h^=b[i];h=Math.imul(h,16777619)>>>0;}return h>>>0;}
const CANON=new Set([97013,4242,20260718,31415,2323,1618,777]);
// 100 färska M5-frön (kollision mot kanon + dubbletter ⇒ i+1000, upprepat)
const SEEDS=[]; const seen=new Set();
for(let i=1;SEEDS.length<100;i++){let j=i; let s=FNV1a32('M5/'+j); let guard=0;
  while((CANON.has(s)||seen.has(s))&&guard<50){j+=1000;s=FNV1a32('M5/'+j);guard++;}
  seen.add(s); SEEDS.push(s);}
const TARGET=['numbers','calendar','coinage','clock','printpress','computing','law','school','science','writing','electricity','antibiotics','flight','atompower','rocketry','spaceflight','ai'];
const done=new Set();
if(fs.existsSync(out))for(const line of fs.readFileSync(out,'utf8').split('\n')){try{const r=JSON.parse(line);if(r&&r.tag&&r.secs!==undefined)done.add(r.tag);}catch(e){}}
fs.appendFileSync(out,`# start shard ${shard}/${nsh} ${new Date().toISOString()} engine=v25b sha=${sha} node=${process.version} N=100 years=${YEARS} predicate=computing skip=${done.size}\n`);
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
      computingBuilt:(reach.computing!=null),computingYear:reach.computing,compEv,reach,
      vote:{atompower:(k.atompower&&k.atompower.vote)||null,computing:(k.computing&&k.computing.vote)||null},
      starvDeaths:(S.stats&&S.stats.deaths&&S.stats.deaths.starvation)||0})+'\n');
    ndone++;
    console.log('DONE',tag,'era',eraN,'computing',reach.computing);
  }catch(e){try{fs.appendFileSync(errf,tag+': '+(e.stack||e.message)+'\n');}catch(_){}console.error('ERR',tag,e.message);}
}
fs.appendFileSync(out,`# shard ${shard}/${nsh} DONE ${new Date().toISOString()} (+${ndone} denna körning)\n`);

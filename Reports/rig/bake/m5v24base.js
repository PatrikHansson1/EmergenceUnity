// m5v24base.js <shard> <nshards> <seedsfile> [years=3000] [engine] [sha8] [outdir]
// M-A4-BASLINJE: kör v24 (kanon e2285e55, backup emergence-engine.js.pre-v25) på EXAKT de frön där v25b forkade,
// 3000 år, och spelar in svältdöd/utdöd/reach — så att BREDD-A-PREREG-LOCKED §3 M-A4 ("svältdöd median och utdöda ej över
// v24-baslinjen på samma frön") kan dömas. Ej-fork-världar är byte-identiska v24≡v25b (mekaniken är inert utan samtidigt
// valbara exklusiva tekniker) och behöver ingen ombakning. Motorn rörs ALDRIG; detta läser bara backupen.
// Samma inspelningsformat som m5v25.js. Låsfil + dubbelstartskydd (D-856/857). Resumbart.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const shard=Number(process.argv[2]||0), nsh=Number(process.argv[3]||1);
const SEEDF=process.argv[4]; const YEARS=Number(process.argv[5]||3000);
const ENG=process.argv[6]||path.join(__dirname,'..','..','..','Assets','StreamingAssets','Emergence','emergence-engine.js.pre-v25');
const WANT=process.argv[7]||'e2285e55';
const OUTD=process.argv[8]||path.join(__dirname,'..','m5v24base');
const pre=[path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),path.join(__dirname,'prelude-hypot.js')].find(p=>fs.existsSync(p));
eval(fs.readFileSync(pre,'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const src=fs.readFileSync(ENG,'utf8');
const sha=crypto.createHash('sha256').update(src,'utf8').digest('hex').slice(0,8);
if(sha!==WANT){console.error('SHA-ASSERT FALLERADE (väntade '+WANT+'): '+sha);process.exit(1);}
if(!fs.existsSync(OUTD))fs.mkdirSync(OUTD,{recursive:true});
const lock=path.join(OUTD,`m5v24base-lock-${shard}.txt`);
if(fs.existsSync(lock)&&Date.now()-fs.statSync(lock).mtimeMs<10*60*1000){console.error('DUBBELSTART: skärva '+shard+' kör redan — avslutar');process.exit(0);}
fs.writeFileSync(lock,new Date().toISOString()); setInterval(()=>{try{fs.writeFileSync(lock,new Date().toISOString());}catch(e){}},60*1000).unref();
const out=path.join(OUTD,`m5v24base-results-${shard}.txt`), prog=path.join(OUTD,`m5v24base-progress-${shard}.txt`), errf=path.join(OUTD,`m5v24base-error-${shard}.txt`);
const SEEDS=fs.readFileSync(SEEDF,'utf8').split(/\s+/).filter(Boolean).map(Number);
const TARGET=['numbers','calendar','coinage','clock','printpress','computing','law','school','science','writing','electricity','antibiotics','flight','atompower','rocketry','spaceflight','ai'];
const done=new Set();
if(fs.existsSync(out))for(const line of fs.readFileSync(out,'utf8').split('\n')){try{const r=JSON.parse(line);if(r&&r.tag&&r.secs!==undefined)done.add(r.tag);}catch(e){}}
fs.appendFileSync(out,`# start shard ${shard}/${nsh} ${new Date().toISOString()} engine=v24-baslinje sha=${sha} node=${process.version} N=${SEEDS.length} years=${YEARS} skip=${done.size}\n`);
let ndone=0;
for(let idx=0;idx<SEEDS.length;idx++){
  if(idx%nsh!==shard)continue;
  const seed=SEEDS[idx], tag=String(seed);
  if(done.has(tag))continue;
  try{
    const m={exports:{}};new Function('module','exports','require','process','globalThis',src)(m,m.exports,require,process,globalThis);
    const E=m.exports;const YEAR=E.YEAR||144;const t0=Date.now();
    const S=E.createWorld(seed);S.silent=true;let lastP=0;
    while(Math.floor(S.tick/YEAR)<YEARS&&!S.ended){E.tickWorld(S);const y=Math.floor(S.tick/YEAR);
      if(y>=lastP+500){lastP=y;try{fs.writeFileSync(prog,`shard ${shard} idx${idx} tag ${tag} y${y} pop${S.agents.filter(a=>!a.dead).length} era${E.worldEra(S)} ${Math.round((Date.now()-t0)/1000)}s ${new Date().toISOString()}\n`);}catch(e){}}}
    const dna=E.computeDNA(S);const k=S.knowledge||{};const eraN=E.worldEra(S);
    const reach={};for(const id of TARGET)reach[id]=(k[id]?(k[id].yearBorn!=null?k[id].yearBorn:true):null);
    fs.appendFileSync(out,JSON.stringify({tag,seed,endYear:Math.floor(S.tick/YEAR),ended:S.ended,endedYear:S.endedYear||null,
      secs:Math.round((Date.now()-t0)/1000),pop:S.agents.filter(a=>!a.dead).length,maxPop:S.maxPop,
      era:eraN,eraName:E.eraName(eraN),knowN:(dna&&dna.knowledgeCount)||Object.keys(k).length,reach,
      starvDeaths:(S.stats&&S.stats.deaths&&S.stats.deaths.starvation)||0})+'\n');
    ndone++;console.log('DONE',tag,'era',eraN,'starv',(S.stats&&S.stats.deaths&&S.stats.deaths.starvation)||0);
  }catch(e){try{fs.appendFileSync(errf,tag+': '+(e.stack||e.message)+'\n');}catch(_){}console.error('ERR',tag,e.message);}
}
fs.appendFileSync(out,`# shard ${shard}/${nsh} DONE ${new Date().toISOString()} (+${ndone} denna körning)\n`);

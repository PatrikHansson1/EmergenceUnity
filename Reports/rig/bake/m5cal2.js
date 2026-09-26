// m5cal2.js <shard> <nshards> [years=3000] — SHARDAD reach-kalibrering (M5-PREREG §3, D-771/774).
// Kör de M5cal-frön där (index % nshards == shard). Parallellisera genom att starta N processer
// (shard 0..N-1) — var skriver sin EGEN resultatfil (concurrency-säkert). MÄTNING på LÅSTA v24
// (e2285e55) — motorn rörs ALDRIG (D-489). Resumbart per shard. Per-500-år progressrad = liveness.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const shard=Number(process.argv[2]||0), nsh=Number(process.argv[3]||1), YEARS=Number(process.argv[4]||3000);
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const ENG=path.join(__dirname,'..','m5','engine-v24.js');
const src=fs.readFileSync(ENG,'utf8');
const sha=crypto.createHash('sha256').update(src,'utf8').digest('hex').slice(0,8);
if(sha!=='e2285e55'){console.error('SHA-ASSERT FALLERADE (v24 e2285e55): '+sha);process.exit(1);}
const M5=path.join(__dirname,'..','m5');
const out=path.join(M5,`m5cal-results-${shard}.txt`);
const prog=path.join(M5,`m5cal-progress-${shard}.txt`);
const errf=path.join(M5,`m5cal-error-${shard}.txt`);
function FNV1a32(s){let h=2166136261>>>0;const b=Buffer.from(s,'utf8');for(let i=0;i<b.length;i++){h^=b[i];h=Math.imul(h,16777619)>>>0;}return h>>>0;}
const CANON=new Set([97013,4242,20260718,31415,2323,1618,777]);
const SEEDS=[];for(let i=1;SEEDS.length<20&&i<100000;i++){const s=FNV1a32('M5cal/'+i);if(CANON.has(s)||SEEDS.includes(s))continue;SEEDS.push(s);}
const TARGET=['numbers','calendar','coinage','clock','printpress','computing','law','school','science','writing'];
// done-set: läs BÅDE gamla samlingsfilen (sekventiella körningen) OCH denna shards fil
const done=new Set();
for(const f of [path.join(M5,'m5cal-results.txt'),out]){if(fs.existsSync(f))for(const line of fs.readFileSync(f,'utf8').split('\n')){try{const r=JSON.parse(line);if(r&&r.tag&&r.secs!==undefined)done.add(r.tag);}catch(e){}}}
fs.appendFileSync(out,`# start shard ${shard}/${nsh} ${new Date().toISOString()} engine=v24 sha=${sha} node=${process.version} years=${YEARS} skip=[${[...done].join(',')}]\n`);
for(let idx=0;idx<SEEDS.length;idx++){
  if(idx%nsh!==shard)continue;
  const seed=SEEDS[idx], tag=String(seed);
  if(done.has(tag)){console.log('SKIP',tag);continue;}
  try{
    const m={exports:{}};new Function('module','exports','require','process','globalThis',src)(m,m.exports,require,process,globalThis);
    const E=m.exports;const YEAR=E.YEAR||144;const t0=Date.now();
    const S=E.createWorld(seed);S.silent=true;
    let lastP=0;
    while(Math.floor(S.tick/YEAR)<YEARS&&!S.ended){E.tickWorld(S);const y=Math.floor(S.tick/YEAR);
      if(y>=lastP+500){lastP=y;try{fs.writeFileSync(prog,`shard ${shard} tag ${tag} y${y} pop${S.agents.filter(a=>!a.dead).length} era${E.worldEra(S)} ${Math.round((Date.now()-t0)/1000)}s ${new Date().toISOString()}\n`);}catch(e){}}}
    const dna=E.computeDNA(S);const k=S.knowledge||{};const eraN=E.worldEra(S);
    const reach={};for(const id of TARGET)reach[id]=(k[id]?(k[id].yearBorn!=null?k[id].yearBorn:true):null);
    fs.appendFileSync(out,JSON.stringify({tag,seed,endYear:Math.floor(S.tick/YEAR),ended:S.ended,endedYear:S.endedYear||null,
      secs:Math.round((Date.now()-t0)/1000),pop:S.agents.filter(a=>!a.dead).length,maxPop:S.maxPop,
      era:eraN,eraName:E.eraName(eraN),knowN:(dna&&dna.knowledgeCount)||Object.keys(k).length,reach})+'\n');
    console.log('DONE',tag,'era',eraN,'clock',reach.clock,'coinage',reach.coinage,'computing',reach.computing);
  }catch(e){try{fs.appendFileSync(errf,tag+': '+(e.stack||e.message)+'\n');}catch(_){}console.error('ERR',tag,e.message);}
}
fs.appendFileSync(out,`# shard ${shard}/${nsh} DONE ${new Date().toISOString()}\n`);

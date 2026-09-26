// aitime.js <shard> <nshards> [years=3000] — RIKTAD AI-TIDSMÄTNING (deskriptiv uppföljning på M5/D-778).
// SAMMA 100 frön som M5 (FNV1a32('M5/'+i) exkl. kanon) ⇒ varje värld IDENTISK med M5 (golden-master D-623),
// resultat korsläsbara mot m5-results. LÅSTA v24 (SHA-assert e2285e55). MOTORN RÖRS ALDRIG (D-489).
// Registrerar reach{} för hela era 7–10-kedjan + ai/spaceflight yearBorn + agent/causes (krönike-predikat).
// Deskriptiv (INGET måltal, ingen prereg-lås) — svarar "hur länge till ai-åldern + vad byggdes på vägen".
// Sharded parallellt; egna filer per shard; resumbart; per-500-år progress = liveness.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const shard=Number(process.argv[2]||0), nsh=Number(process.argv[3]||1), YEARS=Number(process.argv[4]||3000);
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const ENG=path.join(__dirname,'..','m5','engine-v24.js');
const src=fs.readFileSync(ENG,'utf8');
const sha=crypto.createHash('sha256').update(src,'utf8').digest('hex').slice(0,8);
if(sha!=='e2285e55'){console.error('SHA-ASSERT FALLERADE (v24 e2285e55): '+sha);process.exit(1);}
const M5=path.join(__dirname,'..','m5');
const out=path.join(M5,`ai-results-${shard}.txt`);
const prog=path.join(M5,`ai-progress-${shard}.txt`);
const errf=path.join(M5,`ai-error-${shard}.txt`);
function FNV1a32(s){let h=2166136261>>>0;const b=Buffer.from(s,'utf8');for(let i=0;i<b.length;i++){h^=b[i];h=Math.imul(h,16777619)>>>0;}return h>>>0;}
const CANON=new Set([97013,4242,20260718,31415,2323,1618,777]);
// 100 frön IDENTISKA med m5.js (samma loop, samma kanon-exkludering)
const SEEDS=[]; const seen=new Set();
for(let i=1;SEEDS.length<100;i++){let j=i; let s=FNV1a32('M5/'+j); let guard=0;
  while((CANON.has(s)||seen.has(s))&&guard<50){j+=1000;s=FNV1a32('M5/'+j);guard++;}
  seen.add(s); SEEDS.push(s);}
// FOKUS-LÄGE (argv[5]='focus'): kör BARA de 44 världar som byggde computing i M5 — endast de kan nå ai
// (computing är hårt förvillkor för ai, motor-rad 268). Halverar körtid; övriga 56 har ai=null per konstruktion.
const FOCUS=new Set([17518355,675890416,727136832,745738832,762516451,777469689,779294070,794247308,796071689,796218784,812996403,829774022,846404546,846551641,860444225,861357784,863329260,863476355,879959784,910777082,913809212,930586831,947364450,947511545,964142069,997550212,1064954878,1081585402,2975484873,2992262492,3025817730,3042595349,3094208134,3109705825,3162304348,3194873848,3211651467,3228429086,3246192443,3278909038,3295539562,3329241895,3330080538,3362797133]);
const MODE=process.argv[5]||'';
const RUNSEEDS=(MODE==='focus')?SEEDS.filter(s=>FOCUS.has(s)):SEEDS;
// hela sena kedjan (era 6→10) + M5-porten computing; ai/spaceflight = era 10-målen
const TARGET=['numbers','writing','printpress','school','university','philosophy','scholarship','optics',
  'science','steam','electricity','combustion','flight','antibiotics','atompower','computing','rocketry','spaceflight','ai'];
function evFor(S,tid){const e=(S.events||[]).find(ev=>ev.type==='tech'&&ev.tech===tid);
  return e?{agent:e.agent!==undefined,year:e.year,causes:(e.causes||[]).length}:null;}
const done=new Set();
if(fs.existsSync(out))for(const line of fs.readFileSync(out,'utf8').split('\n')){try{const r=JSON.parse(line);if(r&&r.tag&&r.secs!==undefined)done.add(r.tag);}catch(e){}}
fs.appendFileSync(out,`# start shard ${shard}/${nsh} ${new Date().toISOString()} engine=v24 sha=${sha} node=${process.version} N=${RUNSEEDS.length} mode=${MODE||'all'} years=${YEARS} kind=aitime skip=${done.size}\n`);
let ndone=0;
for(let idx=0;idx<RUNSEEDS.length;idx++){
  if(idx%nsh!==shard)continue;
  const seed=RUNSEEDS[idx], tag=String(seed);
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
    const aiYear=(typeof reach.ai==='number')?reach.ai:null;
    const sfYear=(typeof reach.spaceflight==='number')?reach.spaceflight:null;
    const compYear=(typeof reach.computing==='number')?reach.computing:null;
    // vilken era-10-tech kom först, och när nåddes era 10
    let era10First=null,era10Year=null;
    for(const [id,y] of [['ai',aiYear],['spaceflight',sfYear]]) if(typeof y==='number'&&(era10Year==null||y<era10Year)){era10Year=y;era10First=id;}
    fs.appendFileSync(out,JSON.stringify({tag,seed,endYear:Math.floor(S.tick/YEAR),ended:S.ended,endedYear:S.endedYear||null,
      secs:Math.round((Date.now()-t0)/1000),pop:S.agents.filter(a=>!a.dead).length,maxPop:S.maxPop,
      era:eraN,eraName:E.eraName(eraN),knowN:(dna&&dna.knowledgeCount)||Object.keys(k).length,
      computingBuilt:(compYear!=null),computingYear:compYear,
      aiBuilt:(aiYear!=null),aiYear,aiEv:evFor(S,'ai'),
      spaceflightBuilt:(sfYear!=null),spaceflightYear:sfYear,spaceflightEv:evFor(S,'spaceflight'),
      era10First,era10Year,gapCompToAi:(aiYear!=null&&compYear!=null)?aiYear-compYear:null,
      reach})+'\n');
    ndone++;
    console.log('DONE',tag,'era',eraN,'comp',compYear,'ai',aiYear,'sf',sfYear);
  }catch(e){try{fs.appendFileSync(errf,tag+': '+(e.stack||e.message)+'\n');}catch(_){}console.error('ERR',tag,e.message);}
}
fs.appendFileSync(out,`# shard ${shard}/${nsh} DONE ${new Date().toISOString()} (+${ndone} denna körning)\n`);

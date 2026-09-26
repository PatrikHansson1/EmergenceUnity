// breadth-run.js — BREDD-SESSIONSKIT (lek + titta). Sandlada, motorn ORORD.
// Tva rattar UTAN motoringrepp bortom EN inert CAPSCALE-rad:
//   CAPSCALE = band-multiplikator (storre varldar)   [via globalThis]
//   AGGT     = stads-troskel (lagre => fler, mindre stader = "spritt")  [via S._aggT, motorn stodjer test-override]
// Anvandning:
//   node breadth-run.js compare <seedidx=0> <years=500>      // EN varld, 4 forinstallningar sida-vid-sida
//   node breadth-run.js single <capscale> <aggt> <years> <n> <startidx>
//   node breadth-run.js pick <years> <idx ...>                 // utvalda fron, lagger till i breadth-pick.txt
//   node breadth-run.js rarity <a|b> <varde> <years> <idx ...>  // steg-2-sandlada (krav ENGINE=engine-window.js)
const fs=require('fs'),path=require('path');
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));  // pa din dator: rig/golden-cloud
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const SRC=fs.readFileSync(path.join(__dirname,process.env.ENGINE||'engine-sandbox.js'),'utf8'); // ENGINE=engine-branch.js for gren-testet
function FNV1a32(s){let h=2166136261>>>0;const b=Buffer.from(s,'utf8');for(let i=0;i<b.length;i++){h^=b[i];h=Math.imul(h,16777619)>>>0;}return h>>>0;}
const CANON=new Set([97013,4242,20260718,31415,2323,1618,777]);
const SEEDS=[];{const seen=new Set();for(let i=1;SEEDS.length<100;i++){let j=i,s=FNV1a32('M5/'+j),g=0;while((CANON.has(s)||seen.has(s))&&g<50){j+=1000;s=FNV1a32('M5/'+j);g++;}seen.add(s);SEEDS.push(s);}}
function run(seed,capscale,aggt,years){
  globalThis.__CAPSCALE=capscale;
  const m={exports:{}};new Function('module','exports','require','process','globalThis',SRC)(m,m.exports,require,process,globalThis);
  const E=m.exports;const YEAR=E.YEAR||144;const t0=Date.now();
  const S=E.createWorld(seed);S.silent=true;
  if(aggt&&aggt!==70)S._aggT={agg:aggt,de:Math.max(4,Math.round(aggt*0.7))};
  let _lp=0;
  while(Math.floor(S.tick/YEAR)<years&&!S.ended){E.tickWorld(S);const _y=Math.floor(S.tick/YEAR);
    if(_y>=_lp+(_lp<200?50:200)){_lp=_y;console.log(`   ...tag ${seed} y${_y} pop${S.agents.filter(a=>!a.dead).length} era${E.worldEra(S)} ${Math.round((Date.now()-t0)/1000)}s`);}}
  const k=S.knowledge||{};const alive=S.agents.filter(a=>!a.dead).length;
  const cohort=(S.aggregates||[]).reduce((t,g)=>t+g.cohorts[0]+g.cohorts[1]+g.cohorts[2]+g.cohorts[3],0);
  const yb=id=>k[id]&&k[id].yearBorn!=null?k[id].yearBorn:null;
  const FR=['electricity','antibiotics','flight','atompower','computing','rocketry','spaceflight','ai'];
  const frontier={}; for(const t of FR) frontier[t]=yb(t);
  return {tag:String(seed),capscale,aggt:aggt||70,endYear:Math.floor(S.tick/YEAR),ended:S.ended,
    maxPopInd:S.maxPop, aliveInd:alive, cohortMass:Math.round(cohort), total:Math.round(alive+cohort),
    cities:(S.aggregates||[]).length, villages:(S.villages||[]).length,
    era:E.worldEra(S), knowN:Object.keys(k).length,
    computingYr:k.computing&&k.computing.yearBorn||null, aiYr:k.ai&&k.ai.yearBorn||null,
    // BARARE: hur manga levande individer kan varje sen nyckelteknik (varierar det mellan varldar? => mojlig fork-nyckel)
    bearers:Object.fromEntries(['numbers','printpress','science','university','steam','electricity','combustion','optics','antibiotics','flight','atompower','computing'].map(t=>[t,S.agents.filter(a=>!a.dead&&a.knows.has(t)).length])),
    votes:S._vote||{}, // N2b: rostningen vid grenvalet (ar, omfang, nycklar)
    frontierBy:Object.fromEntries(FR.filter(t=>k[t]).map(t=>[t,k[t].inventedBy])), // VEM (individ eller gille) — N2-avlasning
    frontier, years:Object.fromEntries(Object.values(k).map(x=>[x.id,x.yearBorn])), // ALLA teknikers fodelsear — for att se vilka som VARIERAR mellan varldar
    starvDeaths:(S.stats&&S.stats.deaths&&S.stats.deaths.starvation)||0,
    // C-sparet: KULTUR-SIGNATUR (levande seder/tro + ways) — laser bara S, andrar inget
    culture:(()=>{const cs=Object.values(S.customs||{}).filter(c=>c.status==='alive');
      return {customs:cs.map(c=>c.kind+':'+c.name).sort(), norms:cs.filter(c=>c.norm).map(c=>c.name).sort(), religions:cs.filter(c=>c.religion).map(c=>c.name).sort(),
        lenses:cs.filter(c=>c.lens).map(c=>c.lens).sort(), ways:Object.values(S.ways||{}).filter(w=>w.status==='alive').map(w=>w.need+':'+w.recipe).sort(),
        // B-sparet: MATERIAL-SIGNATUR — vilka material varje teknik faktiskt gjordes av (substitution via MATDIM/suits syns har)
        madeFrom:Object.values(k).filter(x=>x.madeFrom).map(x=>x.id+'='+x.madeFrom).sort()};})(),
    secs:Math.round((Date.now()-t0)/1000)};
}
function row(r){return [String(r.tag).padEnd(11),`cap${r.capscale}`.padEnd(6),`agg${r.aggt}`.padEnd(7),
  `tot${r.total}`.padEnd(8),`ind${r.aliveInd}`.padEnd(7),`stad${r.cities}`.padEnd(7),`era${r.era}`.padEnd(6),
  `kn${r.knowN}`.padEnd(6),`svalt${r.starvDeaths}`.padEnd(9),`comp${r.computingYr||'-'}`.padEnd(10),`ai${r.aiYr||'-'}`.padEnd(8),`sf${r.spaceflightYr||'-'}`.padEnd(8),`${r.secs}s`].join(' ');}
const mode=process.argv[2]||'compare';
if(mode==='compare'){
  const idx=Number(process.argv[3]||0), years=Number(process.argv[4]||500), seed=SEEDS[idx];
  const presets=[['baslinje',1,70],['STORRE',4,70],['SPRITT',1,30],['STORRE+SPRITT',4,30]];
  console.log(`\nBREDD-JAMFORELSE — fro ${seed} (idx ${idx}), ${years} ar. Motorn ORORD (sandlada).`);
  console.log('  total=individer+stadskohort · stad=aggregat · svalt=svaltdodsfall (kollaps-varning)\n');
  const outs=[];
  for(const [namn,cs,ag] of presets){const r=run(seed,cs,ag,years);outs.push({namn,r});
    console.log(namn.padEnd(15),row(r));}
  fs.writeFileSync(path.join(__dirname,`breadth-compare-idx${idx}.txt`),outs.map(o=>o.namn+' '+JSON.stringify(o.r)).join('\n')+'\n');
  console.log(`\nSkrivet: breadth-compare-idx${idx}.txt · Titta: vaxer 'tot'? Fler 'stad' vid SPRITT? Skjuter 'svalt' i hojden vid STORRE (=kollapsrisk)?`);
}else if(mode==='pick'){
  // pick <years> <idx,idx,...> — kor UTVALDA fron (t.ex. kanda snabba era-9-varldar) vid cap1/agg70
  // PowerShell delar "68,94,11" till separata argument — ta ALLA fran argv[4] och splitta pa komma
  const years=Number(process.argv[3]||1200); const idxs=process.argv.slice(4).join(',').split(',').map(s=>Number(s.trim())).filter(n=>!isNaN(n));
  console.log(`\nBREDD-PICK — ${years} ar, fron idx [${idxs.join(',')}]. Motorn ORORD. (lagger till i breadth-pick.txt)\n`);
  for(const i of idxs){ if(i<0||i>=SEEDS.length)continue; const r=run(SEEDS[i],1,70,years);console.log(row(r));
    fs.appendFileSync(path.join(__dirname,`breadth-pick.txt`),JSON.stringify(r)+'\n'); }
  console.log(`\nTillagt i: breadth-pick.txt`);
}else if(mode==='rarity'){
  // rarity <a|b> <varde> <years> <idx ...> — steg-2-sandlada (computing-sallsynthet) med engine-window.js
  //   a = stokastisk grind: __RARITY.computing=varde (t.ex. 0.25 = fjardedels chans per forsok)
  //   b = mognadstroskel:  __MOG.computing=varde (t.ex. 400 = forkunskaper maste ha funnits 400 ar)
  const mech=String(process.argv[3]||'a'), val=Number(process.argv[4]||0.25), years=Number(process.argv[5]||1200);
  const idxs=process.argv.slice(6).join(',').split(',').map(s=>Number(s.trim())).filter(n=>!isNaN(n));
  if(mech==='a')globalThis.__RARITY={computing:val}; else globalThis.__MOG={computing:val};
  console.log(`\nBREDD-RARITY — mek ${mech} varde ${val}, ${years} ar, fron idx [${idxs.join(',')}]. Motorn ORORD. (lagger till i breadth-rarity.txt)\n`);
  for(const i of idxs){ if(i<0||i>=SEEDS.length)continue; const r=run(SEEDS[i],1,70,years); r.mech=mech; r.val=val; console.log(row(r));
    fs.appendFileSync(path.join(__dirname,`breadth-rarity.txt`),JSON.stringify(r)+'\n'); }
  console.log(`\nTillagt i: breadth-rarity.txt`);
}else{
  const cs=Number(process.argv[3]||1),ag=Number(process.argv[4]||70),years=Number(process.argv[5]||500),n=Number(process.argv[6]||4),st=Number(process.argv[7]||0);
  console.log(`\nBREDD-SINGEL — cap${cs} agg${ag}, ${years} ar, ${n} fron fran idx ${st}. Motorn ORORD.\n`);
  const outs=[];
  for(let i=st;i<st+n&&i<SEEDS.length;i++){const r=run(SEEDS[i],cs,ag,years);outs.push(r);console.log(row(r));}
  fs.writeFileSync(path.join(__dirname,`breadth-single-cap${cs}-agg${ag}.txt`),outs.map(o=>JSON.stringify(o)).join('\n')+'\n');
}

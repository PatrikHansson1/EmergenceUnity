// g0probe.js — G0-OMMÄTNINGEN (G1/D-430-återskapning, FAS B): observatör vid gainKnowledge-ENTRÉN.
// Motorfilen är ORÖRD: proben string-patchar källan i minnet (ankrad, räknad) före laddning.
// Räknar per värld: tech -> set av alt-signaturer (sorterade nycklar '+'-joinade); anrop UTAN
// altUsed (t.ex. taught) räknas INTE (D-430). Entrén = FÖRE a.knows-vakten (kastade anrop räknas).
// 8 kanonfrön × 120 år. JSON-rad per frö. INGEN dom här — måltalet ≥6/8 appliceras vid avläsning (D-430, flyttas aldrig).
const fs=require('fs'),path=require('path'),crypto=require('crypto');
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const src0=fs.readFileSync(path.join(__dirname,'engine-v22.js'),'utf8');
const sha=crypto.createHash('sha256').update(src0,'utf8').digest('hex').slice(0,8);
if(sha!=='952f21f6'){console.error('SHA-ASSERT FALLERADE: '+sha);process.exit(1);}
const ANCHOR="function gainKnowledge(S,a,id,via,altUsed){";
if(src0.split(ANCHOR).length!==2){console.error('ANKARE: träffar != 1');process.exit(1);}
const src=src0.replace(ANCHOR,ANCHOR+"if(globalThis.__G0REC)globalThis.__G0REC(id,via,altUsed);");
const out=path.join(__dirname,'g0probe-results.txt');
fs.appendFileSync(out,'# start '+new Date().toISOString()+' engine=engine-v22 sha='+sha+' node='+process.version+'\n');
const founders=[{name:'Ask the First',traits:{curiosity:0.9,social:0.4,diligence:0.6,conformity:0.3}},{name:'Embla the First'},{traits:{social:0.85}},null];
const SEEDS=[[97013,null],[4242,null],[20260718,null],[31415,null],[2323,null],[1618,null],[777,null],[97013,founders]];
const ONLY=process.argv[2]?Number(process.argv[2]):null;
for(const [seed,f] of SEEDS){
  if(ONLY!==null&&seed!==ONLY)continue;
  const tag=f?seed+'-founders':String(seed);
  const rec={}; let calls=0, skipped=0;
  globalThis.__G0REC=(id,via,alt)=>{calls++;if(!alt){skipped++;return;}
    const sig=Object.keys(alt).sort().join('+')||'plain';
    (rec[id]=rec[id]||new Set()).add(sig);};
  const m={exports:{}};new Function('module','exports','require','process','globalThis',src)(m,m.exports,require,process,globalThis);
  const E=m.exports;const YEAR=E.YEAR||144;const t0=Date.now();
  const S=f?E.createWorld(seed,f):E.createWorld(seed);S.silent=true;
  while(Math.floor(S.tick/YEAR)<120&&!S.ended)E.tickWorld(S);
  delete globalThis.__G0REC;
  const per={};for(const t in rec)per[t]=[...rec[t]].sort();
  const multi=Object.keys(per).filter(t=>per[t].length>=2).sort();
  fs.appendFileSync(out,JSON.stringify({tag,endYear:Math.floor(S.tick/YEAR),ended:S.ended,
    secs:Math.round((Date.now()-t0)/1000),calls,skippedNoAlt:skipped,
    techsProbed:Object.keys(per).length,multiWayTechs:multi,multiCount:multi.length,perTech:per})+'\n');
  console.log('DONE',tag,'multi',multi.length,multi.join(','));
}
fs.appendFileSync(out,'# DONE '+new Date().toISOString()+'\n');

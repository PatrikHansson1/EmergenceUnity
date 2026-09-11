// g0probe.js <label> <years> — G0-OBSERVATÖREN (D-430-beskrivningen återskapad, D-762-prereg): hook vid gainKnowledge-ENTRÉN
// (inkl. kastade anrop), räknar (tech, altUsed-materialmängd)-par per värld; 'taught' utan altUsed räknas ej.
const fs=require('fs'),path=require('path'),vm=require('vm');
const [,,label,yearsS]=process.argv; const YEAR=144, years=Number(yearsS), seed=Number(label.replace('-founders',''));
const founders=label.endsWith('-founders')?[{name:'Ask the First',traits:{curiosity:0.9,social:0.4,diligence:0.6,conformity:0.3}},{name:'Embla the First'},{traits:{social:0.85}},null]:null;
const ctx=vm.createContext({console,ArrayBuffer,DataView,Set,Map,JSON,Math,Number,Object,Array,String,Date,Uint8Array,Float64Array,Int32Array,Uint32Array,Error,TypeError,RangeError,parseInt,parseFloat,isNaN,isFinite,Infinity,NaN,undefined});
ctx.globalThis=ctx;
vm.runInContext(fs.readFileSync(path.join(__dirname,'prelude-hypot.js'),'utf8'),ctx);
vm.runInContext("globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;",ctx);
const pairs=new Map(); let calls=0, discarded=0, taughtNoAlt=0;
ctx.__G0=function(id,via,altUsed,already){ calls++; if(already)discarded++; if(!altUsed){ if(via==='taught')taughtNoAlt++; return; }
  const key=Object.keys(altUsed).sort().join('+'); if(!pairs.has(id))pairs.set(id,new Map()); const m=pairs.get(id); m.set(key,(m.get(key)||0)+1); };
vm.runInContext(fs.readFileSync(path.join(__dirname,'engine-g0.js'),'utf8'),ctx,{filename:'engine-g0.js'});
ctx.__F=founders;
vm.runInContext(`(function(){const S=Emergence.createWorld(${seed},__F);S.silent=true;while(S.tick<${years*YEAR}&&!S.ended)Emergence.tickWorld(S);globalThis.__END={tick:S.tick,ended:S.ended,pop:S.agents.filter(a=>!a.dead).length};})()`,ctx);
const multi=[]; for(const [id,m] of pairs) if(m.size>=2) multi.push({tech:id,ways:[...m.entries()]});
const out={label,years,calls,discarded,taughtNoAlt,techsWithAlt:pairs.size,multiWayTechs:multi,pass:multi.length>=1,end:ctx.__END};
fs.writeFileSync(`g0-${label}-${years}.json`,JSON.stringify(out,null,1));
console.log(JSON.stringify({label,calls,discarded,techsWithAlt:pairs.size,multi:multi.map(x=>x.tech+':'+x.ways.map(w=>w[0]).join('|')),pass:out.pass}));

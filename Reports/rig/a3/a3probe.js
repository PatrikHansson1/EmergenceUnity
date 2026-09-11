// a3probe.js <engine> <seed> <years> — A3 TRYCKPRESSPROBEN (ren läsmätning, D-760-prereg). Per år: per by literate?, holds-antal,
// knowledgeLost/rediscovered-händelser per by; världsnivå: writing/printpress känt (någon levande själ), globala extinktioner.
const fs=require('fs'),path=require('path'),vm=require('vm');
const [,,eng,seedS,yearsS]=process.argv; const YEAR=144, years=Number(yearsS), seed=Number(String(seedS).replace('-founders','')); const label=String(seedS);
const founders=label.endsWith('-founders')?[{name:'Ask the First',traits:{curiosity:0.9,social:0.4,diligence:0.6,conformity:0.3}},{name:'Embla the First'},{traits:{social:0.85}},null]:null;
const ctx=vm.createContext({console,ArrayBuffer,DataView,Set,Map,JSON,Math,Number,Object,Array,String,Date,Uint8Array,Float64Array,Int32Array,Uint32Array,Error,TypeError,RangeError,parseInt,parseFloat,isNaN,isFinite,Infinity,NaN,undefined});
ctx.globalThis=ctx;
vm.runInContext(fs.readFileSync(path.join(__dirname,'prelude-hypot.js'),'utf8'),ctx);
vm.runInContext("globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;",ctx);
vm.runInContext(fs.readFileSync(path.resolve(eng),'utf8'),ctx,{filename:eng});
ctx.__F=founders; const S=vm.runInContext(`(function(){const S=Emergence.createWorld(${seed},__F);S.silent=true;return S;})()`,ctx);
const E=ctx.Emergence;
const rows=[]; let evSeen=0;
const yearly=[]; // per år: {y, writing, printpress, villages:[{name,literate,holds,pop}], lost:[{v,tech}], redisc:[...], extinct:[tech]}
while(S.tick<years*YEAR&&!S.ended){
  E.tickWorld(S);
  if(S.tick%YEAR===0){
    const y=S.tick/YEAR;
    const alive=S.agents.filter(a=>!a.dead);
    const known=new Set(); for(const a of alive)for(const k of a.knows)known.add(k);
    const evs=S.events.slice(evSeen); evSeen=S.events.length;
    const lost=[],redisc=[],extinct=[];
    for(const e of evs){ if(e.type==='knowledgeLost'){ if(e.village)lost.push({v:e.village,tech:e.tech}); else extinct.push(e.tech);} else if(e.type==='rediscovered')redisc.push({v:e.village,tech:e.tech}); }
    yearly.push({y,writing:known.has('writing'),printpress:known.has('printpress'),pop:alive.length,
      villages:S.villages.map(v=>({name:v.name,literate:!!v.literate,scribes:v._scribes||0,holds:(v.holds||[]).length,everHeld:v.everHeld?v.everHeld.size:0})),
      lost,redisc,extinct});
  }
}
fs.writeFileSync(`a3-${label}-${years}.json`,JSON.stringify({seed:label,years,engine:path.basename(eng),ended:S.ended,yearly}));
console.log('DONE',label,'years',yearly.length,'ended',S.ended);

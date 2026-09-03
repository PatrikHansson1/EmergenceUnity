// defold-probe.js <seed> <engine> — D-663: visar att de-fold inte returnerar själar (agents.length/alive/aggPop före→efter varje aggregate-event, max 3 events, 130 år). Kräver prelude-hypot.js i samma mapp.
const fs=require('fs'),path=require('path'),vm=require('vm');
const ctx=vm.createContext({console,ArrayBuffer,DataView,Set,Map,JSON,Math,Number,Object,Array,String,Date,Uint8Array,Float64Array,Int32Array,Uint32Array,Error,TypeError,RangeError,parseInt,parseFloat,isNaN,isFinite,Infinity,NaN,undefined});ctx.globalThis=ctx;
vm.runInContext(fs.readFileSync(path.join(__dirname,'prelude-hypot.js'),'utf8'),ctx);
vm.runInContext("globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;",ctx);
vm.runInContext(fs.readFileSync(process.argv[3],'utf8'),ctx); const E=vm.runInContext('Emergence',ctx);
const S=E.createWorld(Number(process.argv[2])); let seen=0; const YEAR=E.YEAR;
while(S.tick<130*YEAR&&!S.ended&&seen<3){const n0=S.agents.length,al0=S.agents.filter(a=>!a.dead).length,ev0=S.events.length; let ap0=0;for(const g of S.aggregates)ap0+=g.cohorts[0]+g.cohorts[1]+g.cohorts[2]+g.cohorts[3];
  E.tickWorld(S);
  const newEv=S.events.slice(ev0).filter(e=>e.type==='aggregate');
  if(newEv.length){const al1=S.agents.filter(a=>!a.dead).length;let ap1=0;for(const g of S.aggregates)ap1+=g.cohorts[0]+g.cohorts[1]+g.cohorts[2]+g.cohorts[3];
    console.log('year',Math.floor(S.tick/YEAR),newEv[0].txt.replace(/<[^>]+>/g,'').slice(0,90),'| agents.length',n0,'->',S.agents.length,'| alive',al0,'->',al1,'| aggPop',ap0.toFixed(1),'->',ap1.toFixed(1)); seen++;}}
// Resultat 2026-09-01 (2323, v18 d02bb1ee): år112 fold alive 82->63 aggPop 0->19 · år113 de-fold "souls step out (19 souls)" agents 82->82 alive 63->63 aggPop 19->0 · år119 fold igen.

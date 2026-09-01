// mb-probe.js <engine> <seed> <years> [budgetSec] — M-b consistency + per-year size + time per sim-year
const fs=require('fs'),path=require('path'),vm=require('vm');
const [,,eng,seedS,yearsS,budS]=process.argv; const seed=Number(seedS),years=Number(yearsS),budget=(Number(budS)||500)*1000;
const ctx=vm.createContext({console,ArrayBuffer,DataView,Set,Map,JSON,Math,Number,Object,Array,String,Date,Uint8Array,Float64Array,Int32Array,Uint32Array,Error,TypeError,RangeError,parseInt,parseFloat,isNaN,isFinite,Infinity,NaN,undefined});
ctx.globalThis=ctx;
vm.runInContext(fs.readFileSync(path.join(__dirname,'prelude-hypot.js'),'utf8'),ctx);
vm.runInContext("globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;",ctx);
vm.runInContext(fs.readFileSync(path.resolve(eng),'utf8'),ctx,{filename:eng});
const E=vm.runInContext('Emergence',ctx);
const S=E.createWorld(seed); const YEAR=E.YEAR; const t0=Date.now(); let y=0; const sizes=[];
while(y<years && Date.now()-t0<budget){ for(let i=0;i<YEAR;i++)E.tickWorld(S); y++; if(y%30===0||y===years) sizes.push([y,S.ledger?JSON.stringify(S.ledger).length:0]); }
const secs=(Date.now()-t0)/1000;
const L=S.ledger||{}; const sum=o=>{let t=0;(function w(x){if(typeof x==='number')t+=x;else if(x&&typeof x==='object')for(const k in x)w(x[k]);})(o);return t;};
const deaths=Object.values(S.stats.deaths).reduce((t,x)=>t+x,0);
const out={engine:path.basename(eng),seed,years:y,secs:+secs.toFixed(1),secPerYear:+(secs/y).toFixed(3),
 deadBook:L.deadBook?L.deadBook.length:null,sumDeaths:deaths,births:L.births?Object.keys(L.births).length:null,statsBirths:S.stats.births,
 extract:L.extract?sum(L.extract):null,tileDec:ctx.__TDEC||null,depleted:L.depleted?sum(L.depleted):null,
 teachActs:L.teach?sum(Object.values(L.teach.byPair)):null,teachByYear:L.teach?Object.values(L.teach.byYear).reduce((t,yv)=>t+Object.values(yv).reduce((u,o)=>u+(o.n||0),0),0):null,
 tradeN:L.trade?Object.values(L.trade).reduce((t,yv)=>t+Object.values(yv).reduce((u,o)=>u+(o.n||0),0),0):null,statsTrades:S.stats.trades||0,
 cohortDeathYears:L.cohortDeaths?Object.keys(L.cohortDeaths).length:null,aggregates:S.aggregates?S.aggregates.length:0,
 snapshots:L.snapshot?L.snapshot.length:null,lastSnap:L.snapshot&&L.snapshot.length?L.snapshot[L.snapshot.length-1]:null,
 ledgerBytes:L?JSON.stringify(L).length:0,sizes};
console.log(JSON.stringify(out));

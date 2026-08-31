// golden-cloud.js <engine.js> <label> <ticks> — kör harnessens runGolden under aktiveringsflaggorna (som JintHost.EpochActive) och skriver sha256 + bytes.
// D-623: V8-tvilling av GoldenMasterRunner. Läser harness.js + prelude-hypot.js från samma mapp (kopiera från Assets/Emergence/Engine/harness/).
// label = "97013" | "4242" | "20260718" | "97013-founders". OUT=<fil> skriver kanon-texten.
const fs=require('fs'),path=require('path'),crypto=require('crypto'),vm=require('vm');
const [,,eng,label,ticksS]=process.argv; const ticks=Number(ticksS);
const seed=Number(label.replace('-founders','')); const founders=label.endsWith('-founders')?[{name:'Ask the First',traits:{curiosity:0.9,social:0.4,diligence:0.6,conformity:0.3}},{name:'Embla the First'},{traits:{social:0.85}},null]:null;
const ctx=vm.createContext({console,ArrayBuffer,DataView,Set,Map,JSON,Math,Number,Object,Array,String,Date,Uint8Array,Float64Array,Int32Array,Uint32Array,Error,TypeError,RangeError,parseInt,parseFloat,isNaN,isFinite,Infinity,NaN,undefined});
ctx.globalThis=ctx;
vm.runInContext(fs.readFileSync(path.join(__dirname,'prelude-hypot.js'),'utf8'),ctx);
vm.runInContext("globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;",ctx);
vm.runInContext(fs.readFileSync(path.resolve(eng),'utf8'),ctx,{filename:eng});
vm.runInContext(fs.readFileSync(path.join(__dirname,'harness.js'),'utf8'),ctx);
ctx.__F=founders; const t0=Date.now();
const canon=vm.runInContext(`EmergenceGolden.runGolden(${seed},${ticks},__F)`,ctx);
const sha=crypto.createHash('sha256').update(canon,'utf8').digest('hex');
console.log(JSON.stringify({engine:path.basename(eng),label,ticks,sha,bytes:Buffer.byteLength(canon,'utf8'),secs:Math.round((Date.now()-t0)/1000)}));
if(process.env.OUT) fs.writeFileSync(process.env.OUT,canon);

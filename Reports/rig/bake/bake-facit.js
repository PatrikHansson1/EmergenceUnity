'use strict';
// bake-facit.js — re-record the deep-time facits (4242 + 2323, t=172800 = 1200y) on the LIVE engine
// (v21 9da69373, B2.3). Windows-native via RUN_NODEBAKE (D-698). Intentional divergence from the
// v20 facits: cities now breathe souls (B2.3). Writes NEW files facit-seed-<s>-t172800.v21.txt in
// deeptime-facit/ — promotion to .canon.txt (with .f39e0684.bak backup) happens in a separate,
// eyeballed step. Loads the V8-hypot prelude first (parity with all probe contexts).
const fs=require('fs'),path=require('path'),crypto=require('crypto');
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const ENGINE=path.join(__dirname,'..','..','..','Assets','StreamingAssets','Emergence','emergence-engine.js');
const src=fs.readFileSync(ENGINE,'utf8');
const engSha=crypto.createHash('sha256').update(src,'utf8').digest('hex');
const m={exports:{}};new Function('module','exports','require','process','globalThis',src)(m,m.exports,require,process,globalThis);
globalThis.Emergence=m.exports;
(new Function(fs.readFileSync(path.join(__dirname,'..','..','..','Assets','Emergence','Engine','harness','harness.js'),'utf8')))();
const outDir=path.join(__dirname,'..','deeptime-facit');
const log=path.join(__dirname,'bake-facit.progress.txt');
fs.appendFileSync(log,'# start '+new Date().toISOString()+' engineSha='+engSha+' node='+process.version+'\n');
if(engSha.slice(0,8)!=='9da69373'){fs.appendFileSync(log,'# ABORT wrong engine sha\n');process.exit(1);}
for(const seed of [4242,2323]){
  const t0=Date.now();
  const txt=globalThis.EmergenceGolden.runGolden(seed,172800,null);
  const sha=crypto.createHash('sha256').update(txt,'utf8').digest('hex');
  const f=path.join(outDir,'facit-seed-'+seed+'-t172800.v21.txt');
  fs.writeFileSync(f,txt);
  fs.appendFileSync(log,'seed='+seed+' len='+txt.length+' sha='+sha+' secs='+Math.round((Date.now()-t0)/1000)+'\n');
  console.log('DONE',seed,sha.slice(0,12),Math.round((Date.now()-t0)/1000)+'s');
}
fs.appendFileSync(log,'# DONE '+new Date().toISOString()+'\n');

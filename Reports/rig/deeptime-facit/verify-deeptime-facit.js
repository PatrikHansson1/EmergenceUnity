'use strict';
// DJUPTIDS-REGRESSIONSNÄTET (D-587): kör efter varje motorändring — jämför HEAD-motorn mot facit.
// Bruk:  node verify-deeptime-facit.js <motorfil>   (facit-filer i samma katalog)
const fs=require('fs'),path=require('path');
const ENGINE=process.argv[2]||'../../Assets/StreamingAssets/Emergence/emergence-engine.js';
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const m={exports:{}};new Function('module','exports','require','process','globalThis',fs.readFileSync(ENGINE,'utf8'))(m,m.exports,require,process,globalThis);
globalThis.Emergence=m.exports;
(new Function(fs.readFileSync(path.join(__dirname,'../../Assets/Emergence/Engine/harness/harness.js'),'utf8')))();
let all=true;
for(const seed of [4242,2323]){
  const facit=fs.readFileSync(path.join(__dirname,'facit-seed-'+seed+'-t172800.canon.txt'),'utf8');
  const now=globalThis.EmergenceGolden.runGolden(seed,172800,null);
  const ok=now===facit; all=all&&ok;
  console.log('djuptid',seed,'×1200 år:',ok?'IDENTISK ✓':'AVVIKER — medveten ombaselinering eller regression?');
}
process.exit(all?0:1);

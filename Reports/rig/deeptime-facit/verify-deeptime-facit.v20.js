'use strict';
// DJUPTIDS-REGRESSIONSNÄTET (D-587, facit omspelat på v20 f39e0684 = v19-dynamik, D-658): kör efter varje motorändring.
// Bruk:  node verify-deeptime-facit.js <motorfil>   (facit-filer i samma katalog; engelska aggregat-rader sedan v18 — ingen mappning längre)
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
  let i=0; if(!ok){while(i<now.length&&i<facit.length&&now[i]===facit[i])i++;}
  console.log(JSON.stringify({seed,ok,len:now.length,facitLen:facit.length,firstDiff:ok?null:i}));
}
console.log(all?'DEEPTIME GREEN':'DEEPTIME RED'); process.exit(all?0:1);

'use strict';
// DJUPTIDS-REGRESSIONSNÄTET (D-587). Facit omspelat på v24 e2285e55 (text-rev; D1 v22/v23 + text F1/F3/F6/F7), dubbelkört byte-identiskt (D-764).
// Båda facit = facit-seed-<s>-t172800.canon.txt på v24 (4242 f363ae0b, 2323 776429bc); v21-facit kvar som .9da69373.bak (befordrade 11 sep, D-765).
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

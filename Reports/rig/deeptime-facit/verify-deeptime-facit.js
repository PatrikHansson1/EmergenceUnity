'use strict';
// DJUPTIDS-REGRESSIONSNÄTET (D-587): kör efter varje motorändring — jämför HEAD-motorn mot facit.
// Bruk:  node verify-deeptime-facit.js <motorfil>   (facit-filer i samma katalog)
// D-614/D-615: facit spelades in på cf5cba65 (svenska aggregat-rader). v18 (d02bb1ee) bytte ENBART de två
// strängarna (bevisat: Reports/rig/p1/textonly-proof.js). Tills facit spelas om på v18 mappas HEAD-canon
// omvänt (eng→sv) före jämförelsen — exakt inversen av D-614-bytet. Ta bort mappningen när facit är omspelat.
const fs=require('fs'),path=require('path');
const ENGINE=process.argv[2]||'../../Assets/StreamingAssets/Emergence/emergence-engine.js';
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
const m={exports:{}};new Function('module','exports','require','process','globalThis',fs.readFileSync(ENGINE,'utf8'))(m,m.exports,require,process,globalThis);
globalThis.Emergence=m.exports;
(new Function(fs.readFileSync(path.join(__dirname,'../../Assets/Emergence/Engine/harness/harness.js'),'utf8')))();
function d614reverse(s){return s.split('is in sight again: souls step out of the crowd (').join('åter i sikte: själarna träder ur mängden (')
 .split(' souls).').join(' själar).').split('has grown past the single gaze — ').join('har växt bortom den enskilda blicken — ')
 .split(' souls now live as a people; ').join(' själar lever nu som folkmängd; ').split(' names carry the chronicle.').join(' namn bär krönikan.');}
let all=true;
for(const seed of [4242,2323]){
  const facit=fs.readFileSync(path.join(__dirname,'facit-seed-'+seed+'-t172800.canon.txt'),'utf8');
  const now=globalThis.EmergenceGolden.runGolden(seed,172800,null);
  const ok=now===facit||d614reverse(now)===facit; all=all&&ok;
  console.log('djuptid',seed,'×1200 år:',ok?('IDENTISK ✓'+(now===facit?'':' (via D-614 text-only-mappning)')):'AVVIKER — medveten ombaselinering eller regression?');
}
process.exit(all?0:1);

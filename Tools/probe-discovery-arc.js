// RÄTTAD MÄTNING. Den förra räknade fel: `if(!first[k])` är sant även när året är 0,
// så allt som uppfanns första året räknades om varje år. Denna läser motorns EGET fält
// `yearBorn`, som motorn stämplar vid uppfinningen — en nivå närmare sanningen än min bokföring.
const fs=require('fs');
const src=fs.readFileSync('/mnt/user-data/uploads/EmergenceUnity/Assets/StreamingAssets/Emergence/emergence-engine.js','utf8');
const mod={exports:{}}; new Function('module','exports','window',src)(mod,mod.exports,undefined);
const E=mod.exports;
const YEARS=parseInt(process.argv[2]||'160',10);
console.log('VERSION',E.VERSION,'TECHS',E.TECHS.length,'— mäter',YEARS,'år\n');
for(const seed of [4242,777,1234,8919,56433]){
  const S=E.createWorld(seed); S.silent=true;
  for(let y=0;y<YEARS&&!S.ended;y++) for(let t=0;t<E.YEAR&&!S.ended;t++) E.tickWorld(S);
  const born=[]; let lost=0,redis=0;
  for(const k in S.knowledge){const e=S.knowledge[k];born.push([e.yearBorn,k,e.status]);lost+=e.losses||0;redis+=e.rediscoveries||0;}
  born.sort((a,b)=>a[0]-b[0]);
  const dec={}; for(const b of born){const d=Math.floor((b[0]-1)/10)*10;dec[d]=(dec[d]||0)+1;}
  const alive=S.agents.filter(a=>!a.dead).length;
  console.log(`===== seed ${seed}: ${born.length} tekniker uppfunna av ${E.TECHS.length} · FÖRSTA år ${born[0][0]} · SISTA år ${born[born.length-1][0]} · pop ${alive} · byar ${S.villages.length} · gen ${S.maxGeneration} · förluster ${lost} · återupptäckter ${redis}`);
  const ds=Object.keys(dec).map(Number).sort((a,b)=>a-b);
  for(const d of ds) console.log(`   år ${String(d).padStart(3)}–${String(d+9).padStart(3)}  ${'█'.repeat(dec[d])} ${dec[d]}`);
  const half=born[Math.floor(born.length/2)][0], q90=born[Math.floor(born.length*0.9)][0];
  console.log(`   halva trädet uppfunnet vid år ${half} · 90 % vid år ${q90} · DÄREFTER ${YEARS-born[born.length-1][0]} ÅR UTAN EN ENDA NY UPPFINNING\n`);
}

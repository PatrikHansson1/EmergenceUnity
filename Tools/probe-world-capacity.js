// Vad rymmer världen? Mätt på den orörda kartan, före någon simulering.
const fs=require('fs');
const src=fs.readFileSync('/mnt/user-data/uploads/EmergenceUnity/Assets/StreamingAssets/Emergence/emergence-engine.js','utf8');
const mod={exports:{}}; new Function('module','exports','window',src)(mod,mod.exports,undefined);
const E=mod.exports;
console.log('W x H =',E.W,'x',E.H,'=',E.W*E.H,'rutor');
for(const seed of [4242,777,1234,8919,56433]){
  const S=E.createWorld(seed);
  const cnt={};
  for(let y=0;y<E.H;y++)for(let x=0;x<E.W;x++){const t=S.tiles[y][x].t;cnt[t]=(cnt[t]||0)+1;}
  // motorns egen bärkraftsformel, replikerad ur källan (rad 769):
  const berry=cnt.berry||0;
  const capNoFields=Math.floor(10+berry*0.28);
  const capMaxFields=Math.floor(10+berry*0.28+24*1.8+8);
  // hur många byar ryms med motorns egen minsta byavstånd?
  const land=(E.W*E.H)-(cnt.water||0);
  console.log(`seed ${seed}: ${JSON.stringify(cnt)}`);
  console.log(`   land ${land} rutor · bärkraft utan fält ${capNoFields} · med 24 fält + fiske ${capMaxFields} (TAKET)`);
}
// byavståndet och VILLAGE_RADIUS ur källan
console.log('\nur källan: VILLAGE_RADIUS=18 (en agent längre bort än 18 tillhör ingen by)');
console.log('byar bildas där 3+ hyddor klustrar, minst 9 rutor isär (ENGINE-2.0-EXPLORATION §1)');
console.log('teoretiskt antal byplatser vid 9 rutors separation på 100x70:', Math.floor(100/9)*Math.floor(70/9));

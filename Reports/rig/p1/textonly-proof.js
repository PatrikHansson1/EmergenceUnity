// Bevis: NY motor ≡ GAMMAL motor upp till exakt de två strängbytena (D-614)
const fs=require('fs'), crypto=require('crypto');
function run(enginePath, seed, ticks){
  // isolerad kontext per motor
  const vm=require('vm'); const ctx={console}; vm.createContext(ctx);
  vm.runInContext("globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;", ctx);
  vm.runInContext(fs.readFileSync(enginePath,'utf8'), ctx);
  vm.runInContext(fs.readFileSync('harness.js','utf8'), ctx);
  return vm.runInContext(`EmergenceGolden.runGolden(${seed},${ticks})`, ctx);
}
const seed=Number(process.argv[2]||97013), years=Number(process.argv[3]||150), ticks=years*144;
const a=run('engine-old.js',seed,ticks), b=run('engine-new.js',seed,ticks);
const back=b.split('is in sight again: souls step out of the crowd (').join('åter i sikte: själarna träder ur mängden (')
            .split(' souls).').join(' själar).')
            .split('has grown past the single gaze — ').join('har växt bortom den enskilda blicken — ')
            .split(' souls now live as a people; ').join(' själar lever nu som folkmängd; ')
            .split(' names carry the chronicle.').join(' namn bär krönikan.');
const folds=(b.match(/has grown past the single gaze/g)||[]).length, defolds=(b.match(/is in sight again/g)||[]).length;
console.log(JSON.stringify({seed,years,oldLen:a.length,newLen:b.length,folds,defolds,identicalRaw:a===b,identicalAfterReverse:a===back,
  oldSha:crypto.createHash('sha256').update(a).digest('hex').slice(0,16),newSha:crypto.createHash('sha256').update(b).digest('hex').slice(0,16)}));

// M5-KLASS BEKRAFTELSE av forken (D-798 rek): engine-window4.js, 1400 ar, de 44 M5-fron som byggde computing.
// Resumbar: hoppar over fron vars tag redan finns i breadth-pick-m5w4.txt. Anvandning: node breadth-run-m5w4.js <idx ...>
process.env.ENGINE='engine-window4.js';
const fs=require('fs'),path=require('path');
const OUT=path.join(__dirname,'breadth-pick-m5w4.txt');
const done=new Set(fs.existsSync(OUT)?fs.readFileSync(OUT,'utf8').split('\n').filter(Boolean).map(l=>JSON.parse(l).tag):[]);
let src=fs.readFileSync(path.join(__dirname,'breadth-run.js'),'utf8').replace(/breadth-pick\.txt/g,'breadth-pick-m5w4.txt');
// filtrera bort redan korda fron innan pick-laget startar
src=src.replace("for(const i of idxs){ if(i<0||i>=SEEDS.length)continue;","for(const i of idxs){ if(i<0||i>=SEEDS.length)continue; if(globalThis.__DONE.has(String(SEEDS[i]))){console.log('   (hoppar over '+SEEDS[i]+', redan kord)');continue;}");
globalThis.__DONE=done;
process.argv.splice(2,0,'pick','1400');
require('module').prototype._compile.call(module,src,path.join(__dirname,'breadth-run.js'));

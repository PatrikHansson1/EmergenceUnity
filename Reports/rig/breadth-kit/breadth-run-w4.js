// wrapper: engine-window4.js (N2b: varlden rostar via barare), skriver till breadth-pick-w4.txt
process.env.ENGINE='engine-window4.js';
const fs=require('fs'),path=require('path');
let src=fs.readFileSync(path.join(__dirname,'breadth-run.js'),'utf8').replace(/breadth-pick\.txt/g,'breadth-pick-w4.txt');
require('module').prototype._compile.call(module,src,path.join(__dirname,'breadth-run.js'));

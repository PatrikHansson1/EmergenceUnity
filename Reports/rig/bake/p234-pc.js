// EMERGENCE — P2/P4/P3 probe on the PC (node, via RUN_LOCALNODE vakt): canon engine + presentation v0.9 from StreamingAssets.
// usage: node p234-pc.js <seed> <years>   → Reports/p234-<seed>.txt ; asserts ENGINE-SHA + PRESENTATION-SHA before running.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const SA=path.resolve(__dirname,'..','..','..','Assets','StreamingAssets','Emergence');
const sha=f=>crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const eng=path.join(SA,'emergence-engine.js'),pres=path.join(SA,'emergence-presentation.js');
const out=[];const log=(...a)=>{const s=a.join(' ');out.push(s);console.log(s);};
log('ENGINE-SHA', sha(eng), fs.readFileSync(path.join(SA,'ENGINE-SHA.txt'),'utf8').trim()===sha(eng)?'OK':'MISMATCH');
log('PRESENTATION-SHA', sha(pres), fs.readFileSync(path.join(SA,'PRESENTATION-SHA.txt'),'utf8').trim()===sha(pres)?'OK':'MISMATCH');
const pre=[path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),path.join(__dirname,'prelude-hypot.js')].find(p=>fs.existsSync(p));
if(pre)eval(fs.readFileSync(pre,'utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
function load(f){const SRC=fs.readFileSync(f,'utf8');const m={exports:{}};new Function('module','exports','require','process','globalThis',SRC)(m,m.exports,require,process,globalThis);return m.exports;}
const E=load(eng); require(pres); const P=globalThis.EmergencePresentation;
const seed=Number(process.argv[2]||97013), years=Number(process.argv[3]||100);
const S=E.createWorld(seed);S.silent=true;
let prev=null;const stops=[],preds=[],verdict={};const t0=Date.now();
for(let y=1;y<=years;y++){for(let i=0;i<E.YEAR;i++)E.tickWorld(S);
  if(y%10===0){const r=P.selfStop(S,prev,E);if(r.stop)stops.push({year:y,reasons:r.reasons.map(x=>x.kind+': '+x.text)});prev=r.snapshot;
    for(const p of preds){if(!verdict[p.id]){const v=P.judge(S,p,E);if(v!=='open')verdict[p.id]={v,year:y};}}}
  if(y===20||y===60){preds.push(...P.offer(S,E,{horizon:60}));}}
const h=s=>crypto.createHash('sha256').update(s).digest('hex').slice(0,16);
log('presentation',P.VERSION,'seed',seed,'years',years,'secs',(Date.now()-t0)/1000|0,'pop',S.agents.filter(a=>!a.dead).length,'events',S.events.length);
log('P1 digest(100)',h(P.reportDigest(S,100)));
const band=P.eraBand(S,E);
log('P2 eras',JSON.stringify(band.eras));log('P2 density',band.decades.map(d=>d.count+'/'+d.era).join(' '));
log('P2 bookmarks',band.bookmarks.length);for(const b of band.bookmarks)log('   ',b.year,b.kind,'—',b.text.slice(0,110));
log('P4 stops',stops.length);for(const s of stops)log('   y'+s.year,s.reasons.join(' | ').slice(0,200));
const judged={hit:0,miss:0,open:0};for(const p of preds)judged[verdict[p.id]?verdict[p.id].v:'open']++;
log('P3 offered',preds.length,'journal',JSON.stringify(judged));for(const p of preds)log('   [made y'+p.madeYear+']',p.text,'->',verdict[p.id]?verdict[p.id].v+' @y'+verdict[p.id].year:'open');
log('P2 band sha',h(JSON.stringify(band)));
fs.writeFileSync(path.resolve(__dirname,'..','..','p234-'+seed+'.txt'),out.join('\n')+'\n');

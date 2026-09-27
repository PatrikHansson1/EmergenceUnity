// fixture-gen: run canon v25b for N years on a seed, dump a presentation fixture (S.events + knowledge + era timeline)
const fs=require('fs'),crypto=require('crypto');
eval(fs.readFileSync(__dirname+'/prelude-hypot.js','utf8'));
for(const k of ['__G29','__SOIL','__LADDER','__FOREST','__CLIMATE','__PACE','__PROD'])globalThis[k]=true;
function load(f){const SRC=fs.readFileSync(f,'utf8');const m={exports:{}};new Function('module','exports','require','process','globalThis',SRC)(m,m.exports,require,process,globalThis);return m.exports;}
const seed=Number(process.argv[2]||97013), years=Number(process.argv[3]||60), out=process.argv[4]||`fixture-${seed}-y${years}.json`;
const E=load(__dirname+'/engine-v25b-kandidat.js');
const S=E.createWorld(seed);S.silent=true;
const t0=Date.now(); const eraLog=[]; let lastEra=-1;
for(let y=0;y<years;y++){for(let i=0;i<E.YEAR;i++)E.tickWorld(S);
  const era=E.worldEra(S); if(era!==lastEra){eraLog.push({year:y+1,era,name:E.eraName(era)});lastEra=era;}
  if((y+1)%20===0)process.stderr.write(`y${y+1} ${(Date.now()-t0)/1000|0}s pop=${S.agents.filter(a=>!a.dead).length} ev=${S.events.length} era=${era}\n`);}
const fx={seed,years,engineSha:crypto.createHash('sha256').update(fs.readFileSync(__dirname+'/engine-v25b-kandidat.js')).digest('hex'),
  tick:S.tick,YEAR:E.YEAR,eraLog,events:S.events,knowledge:S.knowledge,
  villages:(S.villages||[]).map(v=>({name:v.name,id:v.id})),pop:S.agents.filter(a=>!a.dead).length,
  agents:S.agents.filter(a=>!a.dead).map(a=>({id:a.id,name:a.name,born:a.born,vil:a.vil,knows:[...(a.knows||[])]}))};
fs.writeFileSync(out,JSON.stringify(fx));
console.log('wrote',out,'events',S.events.length,'eras',JSON.stringify(eraLog),'secs',(Date.now()-t0)/1000|0);

// b23f.js <years> — 97013-founders on engine-b23 (B2.3 M3 last seed), native node on Windows (RUN_NODEBAKE).
// Loads the V8-hypot prelude first (parity with golden-cloud context), then the b23 engine.
const fs=require('fs'),path=require('path');
eval(fs.readFileSync(path.join(__dirname,'..','golden-cloud','prelude-hypot.js'),'utf8'));
globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;
const _m=require(path.join(__dirname,'..','b23','engine-b23.js')); const E=globalThis.Emergence||_m;
const years=Number(process.argv[2]||1200); const YEAR=E.YEAR;
const founders=[{name:'Ask the First',traits:{curiosity:0.9,social:0.4,diligence:0.6,conformity:0.3}},{name:'Embla the First'},{traits:{social:0.85}},null];
const S=E.createWorld(97013,founders); const out=path.join(__dirname,'..','b23','b23-97013-founders.csv');
fs.writeFileSync(out,'year,alive,aggPop,aggregates,villages,bearersAliveMin\n');
let y=0, defolds=[], m7ok=true, cityYears=0, bearOK=0, aliveMinAfterFold=1e9, sawFold=false; const t0=Date.now();
while(y<years&&!S.ended){
  for(let i=0;i<YEAR&&!S.ended;i++){
    const pre=(S.aggregates||[]).map(g=>({v:g.village,sum:g.cohorts.reduce((t,x)=>t+Math.round(x),0)}));
    const al0=S.agents.filter(a=>!a.dead).length;
    E.tickWorld(S);
    if((S.aggregates||[]).length<pre.length){
      const now=new Set((S.aggregates||[]).map(g=>g.village));
      for(const p of pre)if(!now.has(p.v)){
        const al1=S.agents.filter(a=>!a.dead).length;
        const d={year:Math.floor(S.tick/YEAR),v:p.v,expected:p.sum,delta:al1-al0};
        defolds.push(d); if(Math.abs(d.delta-d.expected)>2)m7ok=false;
      }
    }
  }
  y++;
  const alive=S.agents.filter(a=>!a.dead).length; let ap=0,minB=99;
  for(const g of (S.aggregates||[])){ap+=g.cohorts[0]+g.cohorts[1]+g.cohorts[2]+g.cohorts[3];
    const ba=S.agents.filter(a=>!a.dead&&g.bearers.indexOf(a.id)>=0).length; if(ba<minB)minB=ba;}
  if((S.aggregates||[]).length){cityYears++; if(minB>=12)bearOK++; sawFold=true;}
  if(sawFold&&alive<aliveMinAfterFold)aliveMinAfterFold=alive;
  fs.appendFileSync(out,y+','+alive+','+ap.toFixed(1)+','+(S.aggregates||[]).length+','+S.villages.length+','+((S.aggregates||[]).length?minB:'')+'\n');
}
fs.appendFileSync(out,'# END year='+y+' ended='+S.ended+' secs='+Math.round((Date.now()-t0)/1000)+' aggregates='+JSON.stringify((S.aggregates||[]).map(g=>({v:g.village,c:g.cohorts.map(x=>+x.toFixed(1)),bearers:g.bearers.length})))+'\n');
fs.appendFileSync(out,'# M2 aliveMinAfterFold='+(sawFold?aliveMinAfterFold:'n/a')+' cityYears='+cityYears+' bearersAlive>=12 in '+bearOK+' ('+(cityYears?Math.round(100*bearOK/cityYears):0)+'%)\n');
fs.appendFileSync(out,'# M7 defolds='+defolds.length+' ok='+m7ok+' detail='+JSON.stringify(defolds.slice(0,10))+'\n');
console.log('DONE 97013-founders',y,'ended',S.ended);

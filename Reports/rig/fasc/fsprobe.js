// fsprobe.js <engine> <label> — FAS C nivå P: typerna bland raderna P1 väljer (steg 50 = dom, steg 25 = känslighet), 150 år.
globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;
globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;
const fs=require('fs'),path=require('path');
const _m=require(path.resolve(process.argv[2])); const E=globalThis.Emergence||_m;
require('./emergence-presentation.js'); const P=globalThis.EmergencePresentation;
if(process.env.WOVR){const o=JSON.parse(process.env.WOVR);for(const k in o)P.WEIGHT[k]=o[k];}
const YEAR=144, label=String(process.argv[3]), seed=Number(label.replace('-founders','')), years=150;
const founders=label.endsWith('-founders')?[{name:'Ask the First',traits:{curiosity:0.9,social:0.4,diligence:0.6,conformity:0.3}},{name:'Embla the First'},{traits:{social:0.85}},null]:null;
const S=E.createWorld(seed,founders); S.silent=true; while(S.tick<years*YEAR&&!S.ended)E.tickWorld(S);
function lines(step){const out=[];for(let y=0;y<years;y+=step){const r=P.writeIntervalReport(S,y,y+step-1);for(const l of r.lines)out.push({y0:y,year:l.year,type:l.type,weight:l.weight});}return out;}
const res={engine:path.basename(process.argv[2]),label,years,ended:S.ended,step50:lines(50),step25:lines(25),eventTypesTotal:Object.entries(S.events.reduce((m,e)=>(m[e.type]=(m[e.type]||0)+1,m),{})).sort((a,b)=>b[1]-a[1])};
const tag=path.basename(process.argv[2]).replace('.js','')+(process.env.WTAG?'-'+process.env.WTAG:'');
fs.writeFileSync(`fs-${tag}-${label}.json`,JSON.stringify(res));
console.log('DONE',tag,label,'lines50',res.step50.length,'types50',[...new Set(res.step50.map(l=>l.type))].join(','));

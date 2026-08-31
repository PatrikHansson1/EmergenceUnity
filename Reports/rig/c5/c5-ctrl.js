// C5-KONTROLL (§11c baslinje på v19): våldsfamiljen + icke-vålds-kanaler + grudge-öden, <seed> × 120 år.
globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;
globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;
const path=require('path'); const _m=require(path.resolve('./engine-pace.js')); const E=globalThis.Emergence||_m;
const YEAR=144, seed=Number(process.argv[2]||97013), years=Number(process.argv[3]||120);
const F=process.env.FOUNDERS?[{name:'Ask the First',traits:{curiosity:0.9,social:0.4,diligence:0.6,conformity:0.3}},{name:'Embla the First'},{traits:{social:0.85}},null]:undefined;
const S=E.createWorld(seed,F); S.silent=true;
while(S.tick<years*YEAR&&!S.ended)E.tickWorld(S);
const n={}; for(const e of (S.events||[])) n[e.type]=(n[e.type]||0)+1;
let grudgesOpen=0, grudgeHolders=0;
for(const a of S.agents){ if(a.dead||!a.grudges)continue; const g=Object.keys(a.grudges).length; if(g>0){grudgeHolders++; grudgesOpen+=g;} }
const feud=n.feud||0, viol=(n.violence||0), steal=n.steal||0, raid=n.raid||0;
const peaceLaw=(S.events||[]).filter(e=>e.type==='violence'&&/Peace of Kin/.test(e.txt||'')).length;
const out={seed,years,ended:S.ended,alive:S.agents.filter(a=>!a.dead).length,
  violenceFamily:{violence:viol,steal,raid,feud,mourn:n.mourn||0,total:viol+steal+raid+feud},
  nonViolent:{sharing:n.sharing||0,giftway:n.giftway||0,tribute:n.tribute||0,trade:n.trade||0,peaceLawBorn:peaceLaw,tabooBroken:n.tabooBroken||0},
  grudges:{openAtEnd:grudgesOpen,holders:grudgeHolders},
  killings:S.stats&&S.stats.killings||0};
console.log(JSON.stringify(out));

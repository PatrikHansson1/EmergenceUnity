#!/usr/bin/env python3
"""mkledger.py <engine-in> <engine-out>
Emissionskontraktet S.ledger (D-649/D-650, prereg LEDGER-PREREG-2026-09-01). Applies anchored, additive
patches to a v19 (8907f6f6) engine and writes the rig twin. Every anchor must occur EXACTLY once.
Block 1: init + helpers + K1 deadBook + K2 births (D-651).  Block 2: K11 snapshot, K3 extract/depleted,
K5 teach, K6 trade, K13 cohortDeaths.  Rules R-a..R-h: no S.rand(), no ev(), never read in tick logic.
"""
import sys
src=open(sys.argv[1],encoding='utf-8').read()
n0=len(src)
INIT="S.ledger={births:{},deadBook:[],extract:{},depleted:{},meals:{},teach:{byPair:{},byTeacher:{},byYear:{}},trade:{},rel:{},travel:{},stripped:{},smelt:{},wood:{},huts:{},fires:{},tribute:{},hardship:{},cohortDeaths:{},snapshot:[]};"
def rep(anchor,new,label):
    global src
    c=src.count(anchor)
    assert c==1,f"{label}: anchor count {c}: {anchor[:80]!r}"
    src=src.replace(anchor,new)
    print(f"ok {label}")

# ---- init in createWorld (after the start event) ----
A="  ev(S,'start',`🌍 Four humans wake in an untouched world: <b>${S.agents.map(a=>a.name).join('</b>, <b>')}</b>. They know nothing — but they can observe everything. What will they create?`,{});\n  return S;\n"
rep(A,A.replace("  return S;\n",
"  // ===== EMISSIONSKONTRAKTET (D-649/D-650): S.ledger — a top-level field OUTSIDE the golden payload\n"
"  // (harness endState lists stats/agents/villages/… but never S itself). Write-only from mechanics, read by\n"
"  // export/presentation/bake. No S.rand(), no ev(), never read in tickWorld logic. Keys = ids/indices, never names.\n"
"  "+INIT+"\n  return S;\n"),"init")

# ---- lazy init in tickWorld (pathUse pattern, older checkpoints) ----
A="function tickWorld(S){\n  if(S.ended)return;\n"
rep(A,A+"  if(!S.ledger)"+INIT+" // ledger init for older checkpoints (pathUse pattern)\n","tick-init")

# ---- helpers + K11 snapshot function, placed before killAgent ----
A="function killAgent(S,a,causeKey,causeTxt,extraCauses){\n"
HELP=("// ledger helpers (D-650): village index (villages are push-only, no id), chronicle year (same formula as ev()),\n"
"// counters, and per-(year,village) buckets. All pure; none touch S.rand or events.\n"
"function _lvix(S,a){const v=villageOf(S,a);return v?S.villages.indexOf(v):-1;}\n"
"function _lvixn(S,name){for(let i=0;i<S.villages.length;i++)if(S.villages[i].name===name)return i;return -1;}\n"
"function _lyear(S){return Math.floor(S.tick/YEAR)+1;}\n"
"function _linc(o,k){o[k]=(o[k]||0)+1;return o;}\n"
"function _lyv(o,S,vix){const y=_lyear(S);const t=o[y]||(o[y]={});return t[vix]||(t[vix]={});}\n"
"function _lr2(x){return Math.round(x*100)/100;}\n"
"// K11 year snapshot: state at the close of chronicle-year S.tick/YEAR (called at S.tick%YEAR===0 after the yearly ticks).\n"
"function ledgerYearSnapshot(S){\n"
"  const alive=S.agents.filter(a=>!a.dead).length; const vs=[];\n"
"  for(let i=0;i<S.villages.length;i++){const v=S.villages[i];const ag=villageAggregate(S,v);\n"
"    let cm=0;if(S.aggregates)for(const g of S.aggregates)if(g.village===v.name)cm+=g.cohorts[0]+g.cohorts[1]+g.cohorts[2]+g.cohorts[3];\n"
"    vs.push({v:i,pop:ag.pop,c:[ag.cohorts.c0_13,ag.cohorts.c14_39,ag.cohorts.c40_64,ag.cohorts.c65p],k:ag.knows.length,scribes:ag.scribes,wealth:ag.wealth,agg:_lr2(cm),\n"
"      tr:[_lr2(ag.traits.curiosity.mean),_lr2(ag.traits.social.mean),_lr2(ag.traits.diligence.mean)]});}\n"
"  S.ledger.snapshot.push({year:S.tick/YEAR,alive,births:S.stats.births,deaths:Object.values(S.stats.deaths).reduce((t,x)=>t+x,0),trades:S.stats.trades||0,\n"
"    villages:S.villages.length,fields:S.fields.length,huts:S.huts.length,fires:S.fires.length,knowledge:Object.keys(S.knowledge).length,customs:S.customs?Object.keys(S.customs).length:0,v:vs});\n"
"}\n")
rep(A,HELP+A,"helpers+K11fn")

# ---- K1 deadBook ----
A="  S.stats.deaths[causeKey]=(S.stats.deaths[causeKey]||0)+1;\n"
rep(A,A+"  if(S.ledger){const _b=S.ledger.births[a.id];S.ledger.deadBook.push({id:a.id,name:a.name,born:a.born,died:_lyear(S),age:a.age,gen:a.gen,parentIds:_b?_b.parents:null,village:_lvix(S,a),epithet:a.epithet||null,knowsN:a.knows.size,customsN:a.customs.size,cause:causeKey,wealth:wealth(a)});} // K1 the book of the dead\n","K1")

# ---- K2 births ----
A="    S.agents.push(child);S.stats.births++;\n"
rep(A,A+"    if(S.ledger)S.ledger.births[child.id]={parents:[a.id,b.id],year:_lyear(S),village:_lvix(S,a)}; // K2 the book of births\n","K2")

# ---- K11 hook in tickWorld ----
A="  if(S.tick%YEAR===0)sicknessTick(S); // B4 (D-507): feberns år — tyst under tröskeln // ENGINE 2.1 (D-086): per-community knowledge census + local loss/rediscovery (yearly; pure readout)\n"
rep(A,A+"  if(S.tick%YEAR===0&&S.ledger)ledgerYearSnapshot(S); // K11 year snapshot (ledger; pure readout, after the yearly ticks)\n","K11-hook")

# ---- K3 extract / depleted: berry ----
A="doSeek(S,a,'berry',()=>{S.tiles[a.ty][a.tx0].n--;if(S.tiles[a.ty][a.tx0].n<=0)regrowLater(S,a.tx0,a.ty,'berry');"
rep(A,"doSeek(S,a,'berry',()=>{S.tiles[a.ty][a.tx0].n--;if(S.ledger)_linc(_lyv(S.ledger.extract,S,_lvix(S,a)),'berry');if(S.tiles[a.ty][a.tx0].n<=0){regrowLater(S,a.tx0,a.ty,'berry');if(S.ledger)_linc(_lyv(S.ledger.depleted,S,_lvix(S,a)),'berry');}","K3-berry")

# ---- K3: needed material ----
A=("      S.tiles[a.ty][a.tx0].n--;\n"
   "      if(S.tiles[a.ty][a.tx0].n<=0&&MATSOURCE[need]!=='grass'){const typ=S.tiles[a.ty][a.tx0].t;")
rep(A,("      S.tiles[a.ty][a.tx0].n--;\n"
   "      if(S.ledger)_linc(_lyv(S.ledger.extract,S,_lvix(S,a)),need); // K3 extraction\n"
   "      if(S.tiles[a.ty][a.tx0].n<=0&&MATSOURCE[need]!=='grass'){const typ=S.tiles[a.ty][a.tx0].t;if(S.ledger)_linc(_lyv(S.ledger.depleted,S,_lvix(S,a)),typ);"),"K3-need")

# ---- K3: forage ----
A=("        S.tiles[a.ty][a.tx0].n--;\n"
   "        if(S.tiles[a.ty][a.tx0].n<=0&&MATSOURCE[m]!=='grass'){const typ=S.tiles[a.ty][a.tx0].t;")
rep(A,("        S.tiles[a.ty][a.tx0].n--;\n"
   "        if(S.ledger)_linc(_lyv(S.ledger.extract,S,_lvix(S,a)),m); // K3 extraction (forage)\n"
   "        if(S.tiles[a.ty][a.tx0].n<=0&&MATSOURCE[m]!=='grass'){const typ=S.tiles[a.ty][a.tx0].t;if(S.ledger)_linc(_lyv(S.ledger.depleted,S,_lvix(S,a)),typ);"),"K3-forage")

# ---- K5 teaching acts (only when the pupil actually learned) ----
A="      gainKnowledge(S,b,k,'taught');taught=true;\n"
rep(A,A+"      if(S.ledger&&b.knows.has(k)){_linc(S.ledger.teach.byPair,a.id+'>'+b.id);const _t=S.ledger.teach.byTeacher,_r=_t[a.id]||(_t[a.id]={n:0,techs:{}});_r.n++;_linc(_r.techs,k);const _y=_lyv(S.ledger.teach.byYear,S,_lvix(S,a));_y.n=(_y.n||0)+1;if(!sameVillage)_y.cross=(_y.cross||0)+1;} // K5 acts of teaching\n","K5")

# ---- K6 trade ----
A="  a.task='trading';S.stats.trades=(S.stats.trades||0)+1;\n"
rep(A,A+"  if(S.ledger){const _o=_lyv(S.ledger.trade,S,_lvix(S,a));_o.n=(_o.n||0)+1;if(cross)_o.cross=(_o.cross||0)+1;if(offerFood)_o.food=(_o.food||0)+1;const _m=_o.mat||(_o.mat={});_m[need]=(_m[need]||0)+tq;} // K6 trade\n","K6")

# ---- K13 cohort deaths: aggregateTick (natural + Malthus) ----
A="    const births=_AGGFERT*c[1]*damp, a01=c[0]/14,a12=c[1]/26,a23=c[2]/25;\n"
rep(A,A+"    const _ld=_AGGDR[0]*c[0]+_AGGDR[1]*c[1]+_AGGDR[2]*c[2]+_AGGDR[3]*c[3]; // K13 (ledger): natural cohort deaths this year, read before the update\n","K13-pre")
A="    if(pop+bearersAlive>K){ const f=Math.max(0,(K-bearersAlive))/pop; for(let ci2=0;ci2<4;ci2++)c[ci2]*=f; pop=c[0]+c[1]+c[2]+c[3]; } // v3: Malthus hårdkap\n"
rep(A,"    const _lp0=pop;\n"+A+"    if(S.ledger){const _o=_lyv(S.ledger.cohortDeaths,S,_lvixn(S,g.village));_o.nat=_lr2((_o.nat||0)+_ld);if(_lp0>pop)_o.malthus=_lr2((_o.malthus||0)+(_lp0-pop));} // K13 cohort deaths\n","K13-agg")

# ---- K13: sicknessTick cohort reduction ----
A="    if(g){ const wc=[2,1,1,3];\n      for(let ci=0;ci<4;ci++)g.cohorts[ci]=Math.max(0,g.cohorts[ci]*(1-Math.min(0.9,q0*aq*wc[ci]*hm)));\n    }\n"
rep(A,"    if(g){ const wc=[2,1,1,3]; const _lc0=g.cohorts[0]+g.cohorts[1]+g.cohorts[2]+g.cohorts[3];\n      for(let ci=0;ci<4;ci++)g.cohorts[ci]=Math.max(0,g.cohorts[ci]*(1-Math.min(0.9,q0*aq*wc[ci]*hm)));\n      if(S.ledger){const _o=_lyv(S.ledger.cohortDeaths,S,S.villages.indexOf(v));_o.sick=_lr2((_o.sick||0)+(_lc0-(g.cohorts[0]+g.cohorts[1]+g.cohorts[2]+g.cohorts[3])));} // K13 cohort deaths (fever)\n    }\n","K13-sick")

# ================= BLOCK 3 (prio 3): K4 meals, K7 village relations, K8 travel, K9 stripped, K10 making, K12 hardship =================
# ---- K4 meals: one counter per intake kind, per (year, village) ----
A="a.hunger=clamp(a.hunger+75,0,140);a.task='feasting on the hunt';"
rep(A,A+"if(S.ledger)_linc(_lyv(S.ledger.meals,S,_lvix(S,a)),'hunt');","K4-hunt")
A="a.task='harvesting';S.stats.harvests=(S.stats.harvests||0)+1;"
rep(A,A+"if(S.ledger)_linc(_lyv(S.ledger.meals,S,_lvix(S,a)),'harvest');","K4-harvest")
A="a.task='fishing';"
rep(A,A+"if(S.ledger)_linc(_lyv(S.ledger.meals,S,_lvix(S,a)),'fish');","K4-fish")
A="a.task='eating';"
rep(A,A+"if(S.ledger)_linc(_lyv(S.ledger.meals,S,_lvix(S,a)),'berry');","K4-berry")
A="a.task='baking bread';"
rep(A,A+"if(S.ledger)_linc(_lyv(S.ledger.meals,S,_lvix(S,a)),'bread');","K4-bread")
A="giver.hunger-=15;taker.hunger=clamp(taker.hunger+25,0,140);"
rep(A,A+"if(S.ledger)_linc(_lyv(S.ledger.meals,S,_lvix(S,taker)),'shared');","K4-shared")
A="outcome='took food';"
rep(A,A+"if(S.ledger)_linc(_lyv(S.ledger.meals,S,_lvix(S,a)),'stolen');","K4-stolen")

# ---- K8 travel: journeys between villages (from nearest village hv to destination v) ----
A="a.visit={x:v.x,y:v.y,name:v.name,t:15,k0:a.knows.size,cs:[...a.customs].sort().join()};"
rep(A,A+"if(S.ledger)_linc(_lyv(S.ledger.travel,S,S.villages.indexOf(hv)),S.villages.indexOf(v)); // K8 travel [year][from][to]","K8")

# ---- K7 village relations: the friction warTick computes per (A,B), READ only; raids marked after the fact ----
A="      const hostility=stress*0.7+(surplus>6?0.3:0)+(grud>1?0.35:0);\n"
rep(A,A+"      if(S.ledger)_lyv(S.ledger.rel,S,S.villages.indexOf(A))[S.villages.indexOf(B)]={stress:_lr2(stress),surplus:_lr2(surplus),might,grud,host:_lr2(hostility)}; // K7 village relations (readout of the friction)\n","K7-friction")
A="      S.stats.wars=(S.stats.wars||0)+1;\n"
rep(A,A+"      if(S.ledger){const _o=_lyv(S.ledger.rel,S,S.villages.indexOf(A))[S.villages.indexOf(B)];if(_o){_o.raid=1;_o.deadA=deadA;_o.deadB=deadB;_o.loot=loot;}} // K7 the raid itself\n","K7-raid")

# ---- K9 stripped knowledge (the land takes a craft out of living hands) ----
A="if(techScarceRes(k)!==null && (TECH[k].era||0)>=2 && !sustain.has(k)) a.knows.delete(k);"
rep(A,"if(techScarceRes(k)!==null && (TECH[k].era||0)>=2 && !sustain.has(k)){a.knows.delete(k);if(S.ledger)_linc(_lyv(S.ledger.stripped,S,S.villages.indexOf(v)),k);} // K9 stripped knowledge","K9")

# ---- K10 making: huts, fires, wood burned/built, the plow, materials consumed by invention; tribute ----
A="const h={x:a.x,y:a.y,owner:a.name};S.huts.push(h);a.home=h;S.bgDirty=true;"
rep(A,A+"if(S.ledger){_linc(_lyv(S.ledger.huts,S,_lvix(S,a)),'built');const _w=_lyv(S.ledger.wood,S,_lvix(S,a));_w.hut=(_w.hut||0)+8;} // K10 huts/wood","K10-hut")
A="a.inv.wood-=2;S.fires.push({x:a.x,y:a.y,fuel:600});a.task='lighting a fire';"
rep(A,A+"if(S.ledger){_linc(_lyv(S.ledger.fires,S,_lvix(S,a)),'lit');const _w=_lyv(S.ledger.wood,S,_lvix(S,a));_w.fire=(_w.fire||0)+2;}","K10-fire1")
A="a.inv.wood-=2;S.fires.push({x:a.x,y:a.y,fuel:600});a.task='tending the flame';"
rep(A,A+"if(S.ledger){_linc(_lyv(S.ledger.fires,S,_lvix(S,a)),'tended');const _w=_lyv(S.ledger.wood,S,_lvix(S,a));_w.fire=(_w.fire||0)+2;}","K10-fire2")
A="a.inv.iron-=2;a.plow=1;a.task='forging a plow';"
rep(A,A+"if(S.ledger){const _o=_lyv(S.ledger.smelt,S,_lvix(S,a));const _r=_o.plow||(_o.plow={});_r.iron=(_r.iron||0)+2;} // K10 the plow","K10-plow")
A="          Object.entries(alt).forEach(([m,q])=>a.inv[m]-=q);\n          gainKnowledge(S,a,t.id,'invented',alt);\n"
rep(A,A+"          if(S.ledger){const _o=_lyv(S.ledger.smelt,S,_lvix(S,a));const _r=_o[t.id]||(_o[t.id]={});for(const _m in alt)_r[_m]=(_r[_m]||0)+alt[_m];} // K10 materials consumed by invention\n","K10-invent")
A="if(bk&&bq>2){a.inv[bk]--;L.inv[bk]=(L.inv[bk]||0)+1;given++;}"
rep(A,"if(bk&&bq>2){a.inv[bk]--;L.inv[bk]=(L.inv[bk]||0)+1;given++;if(S.ledger){const _o=_lyv(S.ledger.tribute,S,S.villages.indexOf(v));_o.n=(_o.n||0)+1;_o.leader=L.id;_linc(_o.mat||(_o.mat={}),bk);}} // K10 tribute","K10-tribute")

# ---- K12 hardship: agent-ticks below the desperation thresholds (hunger<25 | warmth<25, read from wouldBreakTaboo); counters only ----
A="  a.energy=clamp(a.energy,0,100);a.warmth=clamp(a.warmth,0,100);a.social=clamp(a.social,0,100);\n"
rep(A,A+"  if(S.ledger&&(a.hunger<25||a.warmth<25)){const _o=_lyv(S.ledger.hardship,S,_lvix(S,a));if(a.hunger<25)_o.hungry=(_o.hungry||0)+1;if(a.warmth<25)_o.cold=(_o.cold||0)+1;} // K12 hardship (aggregated per year/village)\n","K12")

open(sys.argv[2],'w',encoding='utf-8').write(src)
print(f"wrote {sys.argv[2]} ({n0} -> {len(src)} bytes)")

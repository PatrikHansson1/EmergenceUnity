#!/usr/bin/env python3
"""mkd1v2.py <engine-in(xp-twin)> <engine-out(v22-twin)>
D1 VARV 2 (prereg LOCKED 2026-09-04, sha 6e6fbedd..., D-715/D-716): hypotheses + WAYS.
Way birth at the 8th FAILED trial (k=8, D-708). Crystallization semantics + the
NON-CANNIBALIZATION LAW (D-707): a way never ends the hypothesis nor blocks TECH attempts;
its effect acts ONLY while the tech is absent from the practitioner's community (groupKnows).
Locked effect caps (half product effect): cold coldDrain x0.78 | hunger +8 at berry-meal |
sick village death-risk x0.75 with a living bearer. Name pools locked 3x6, FNV pick.
New S.rand draws ONLY inside the expT<=0 branch; yearly sweeps draw nothing.
Applied AFTER mkxp.py.
"""
import sys
src=open(sys.argv[1],encoding='utf-8').read()
n0=len(src)
def rep(a,b,l,count=1):
    global src
    c=src.count(a); assert c==count,f"{l}: count {c} (expected {count})"
    src=src.replace(a,b); print('ok',l)

# A1: state init (hypos from varv 1 + ways)
rep("events:[],knowledge:{},customs:{},nextCustomId:1,",
    "events:[],knowledge:{},customs:{},nextCustomId:1,hypos:{},nextHypoId:1,ways:{},nextWayId:1,","init")

# A2: failure branch -- hypo birth/correction + WAY BIRTH at trials==8
rep("      S.stats.failedExperiments++;\n",
"""      S.stats.failedExperiments++;
          // D1 varv 2 (prereg LOCKED 2026-09-04): a failed experiment against an OPEN need survives
          // as a hypothesis, corrects on repeat -- and at the 8th failure the PRACTICE crystallizes
          // into a way (the invention never came; the way never blocks it, D-707).
          if(needBoost(S,t.id)){
            let hy=null;for(const hid in S.hypos){const h=S.hypos[hid];if(h.status==='alive'&&h.holder===a.id&&h.tech===t.id){hy=h;break;}}
            if(hy){hy.trials++;hy.trust=Math.max(0,hy.trust-0.15);
              if(t.alts&&t.alts.length>1){hy.conj=Object.keys(t.alts[Math.floor(S.rand()*t.alts.length)]).join('+')||'plain';hy.mutations++;}
              else hy.mutations++;
              if(hy.trials===8&&!hy.wayId){
                const ce=ev(S,'corrected',`<b>${a.name}</b> failed again — and changed the way of trying. The guess survives its maker's stubbornness.`,{agent:a.id,x:a.x,y:a.y,causes:hy.needEv!==undefined?['ev:'+hy.needEv]:[]});
                const wid='W'+(S.nextWayId++);
                const mats=(hy.conj||'plain');
                const wname=a.name+"'s "+_wayPool(hy.need)[_fnvh(mats)%6];
                S.ways[wid]={id:wid,name:wname,need:hy.need,tech:hy.tech,recipe:mats,inventedBy:a.name,yearBorn:Math.floor(S.tick/YEAR),bearers:[a.id],status:'alive',hypo:hy.id};
                hy.wayId=wid;
                const we=ev(S,'wayBorn',`✨ The invention never came — but the practice held. <b>${a.name}</b> now keeps <b>${wname}</b>, and others may learn it.`,{way:wid,agent:a.id,x:a.x,y:a.y,causes:['ev:'+ce.id].concat(hy.needEv!==undefined?['ev:'+hy.needEv]:[])});
                S.ways[wid].evId=we.id;
              }
            }else{
              const hid='H'+(S.nextHypoId++);
              let _ne;for(let _i=S.events.length-1;_i>=0&&_i>S.events.length-400;_i--){const _p=S.events[_i];if(_p&&(_p.type==='death'||_p.type==='sickness')){_ne=_p.id;break;}}
              S.hypos[hid]={id:hid,tech:t.id,need:hypoNeedOf(S,t.id),conj:alt?Object.keys(alt).join('+'):'plain',holder:a.id,village:(S.villages.find(v=>dist(v,a)<12)||{}).name||'',trust:0.5,trials:1,mutations:0,born:Math.floor(S.tick/YEAR),status:'alive',needEv:_ne};
            }
          }
""","failure-branch")

# A3: success branch -- held marking (the tree stays the spine; the way is NOT removed: D-707)
rep("          gainKnowledge(S,a,t.id,'invented',alt);\n",
"""          gainKnowledge(S,a,t.id,'invented',alt);
          if(S.hypos)for(const hid in S.hypos){const h=S.hypos[hid];if(h.status==='alive'&&h.holder===a.id&&h.tech===t.id){h.status='held';h.trust=Math.min(1,h.trust+0.3);h.held=Math.floor(S.tick/YEAR);}}
""","success-branch")

# A4: helpers before knowledgeRetentionTick
rep("function knowledgeRetentionTick(S){",
"""// D1 varv 2 helpers (prereg LOCKED 2026-09-04). Name pools are the LOCKED 3x6 lists.
function _wayPool(need){
  if(need==='hunger')return ['gleaning way','lean-year way','gathering way','ember-bread way','field-edge way','sparing way'];
  if(need==='cold')return ['ember way','windbreak way','huddling way','ash-bank way','peat way','shelter way'];
  return ['bitter-leaf way','boiled-water way','sickbed way','clean-hearth way','elder-root way','quiet-house way'];}
function hypoNeedOf(S,id){const _y=Math.floor(S.tick/YEAR);
  if(S._needSick!==undefined&&_y-S._needSick<=15&&NEED_TECHS.sick.indexOf(id)>=0)return 'sick';
  if(S._needHunger!==undefined&&_y-S._needHunger<=15&&NEED_TECHS.hunger.indexOf(id)>=0)return 'hunger';
  if(S._needCold!==undefined&&_y-S._needCold<=15&&NEED_TECHS.cold.indexOf(id)>=0)return 'cold';
  return '';}
// hypotheses die with their holder (illiterate law is the default: single-holder objects)
function hypoSweep(S){if(!S.hypos)return;
  for(const hid in S.hypos){const h=S.hypos[hid];if(h.status!=='alive')continue;
    let live=false;for(const a of S.agents){if(a.id===h.holder&&!a.dead){live=true;break;}}
    if(!live){h.status='dead';h.died=Math.floor(S.tick/YEAR);}}}
// ways carry the knowledge life cycle: die with the last bearer; spread deterministically
// (<=1 new bearer/way/year, nearest non-bearer within 8 of a bearer -- NO rand in yearly ticks).
function waySweep(S){if(!S.ways)return;
  for(const wid in S.ways){const w=S.ways[wid];if(w.status!=='alive')continue;
    w.bearers=w.bearers.filter(id=>{for(const a of S.agents){if(a.id===id)return !a.dead;}return false;});
    if(!w.bearers.length){w.status='dead';w.died=Math.floor(S.tick/YEAR);
      ev(S,'wayLost',`🕯️ With its last keeper gone, <b>${w.name}</b> is no longer practiced anywhere.`,{way:wid,causes:w.evId!==undefined?['ev:'+w.evId]:[]});continue;}
    let best=null,bd=1e9;
    for(const bid of w.bearers){let b=null;for(const a of S.agents){if(a.id===bid){b=a;break;}}if(!b)continue;
      for(const o of S.agents){if(o.dead||o.age<14||w.bearers.indexOf(o.id)>=0)continue;const d=dist(o,b);if(d<8&&d<bd){bd=d;best=o;}}}
    if(best)w.bearers.push(best.id);}}
// effect guard (NON-CANNIBALIZATION, D-707): the way's boost acts ONLY while its tech is absent
// from the practitioner's community.
function _wayBoost(S,a,need){if(!S.ways)return false;
  for(const wid in S.ways){const w=S.ways[wid];
    if(w.status==='alive'&&w.need===need&&w.bearers.indexOf(a.id)>=0&&!groupKnows(S,a,w.tech))return true;}
  return false;}
function _villageWayGuard(S,vname,need){if(!S.ways)return false;
  for(const wid in S.ways){const w=S.ways[wid];if(w.status!=='alive'||w.need!==need)continue;
    for(const bid of w.bearers){for(const a of S.agents){if(a.id===bid&&!a.dead){const v=villageOf(S,a);if(v&&v.name===vname&&!groupKnows(S,a,w.tech))return true;}}}}
  return false;}
function knowledgeRetentionTick(S){""","helpers")

# A5: yearly hooks (mkxp already appended its poke line after the retention line; anchor still unique)
rep("  if(S.tick%YEAR===0)knowledgeRetentionTick(S);\n",
"  if(S.tick%YEAR===0)knowledgeRetentionTick(S);\n  if(S.tick%YEAR===0){hypoSweep(S);waySweep(S);}\n","yearly-hook")

# E1: cold effect (locked cap x0.78, half of clothes' x0.55-effect)
rep("  if(globalThis.__PROD&&a.clothes)coldDrain*=0.55; // §46: sydda kläder biter mot vintern\n",
"""  if(globalThis.__PROD&&a.clothes)coldDrain*=0.55; // §46: sydda kläder biter mot vintern
  if(_wayBoost(S,a,'cold'))coldDrain*=0.78; // D1 varv 2: a kept cold-way blunts the winter -- only while the tech is missing (D-707)
""","cold-effect")

# E2: hunger effect (+8 at the berry meal, the ONE named action; never storage -- granary guard)
rep("a.hunger=clamp(a.hunger+45+(worldKnows(S,'mill')?20:0),0,140);a.task='eating';",
"a.hunger=clamp(a.hunger+45+(worldKnows(S,'mill')?20:0)+(_wayBoost(S,a,'hunger')?8:0),0,140);a.task='eating';","hunger-effect")

# E3: sick effect (village death-risk x0.75 with a living bearer; both hm usages)
rep("    const hm=healer?0.5:1;\n",
"    const hm=(healer?0.5:1)*(_villageWayGuard(S,v.name,'sick')?0.75:1); // D1 varv 2: a kept sick-way (only while medicine is absent, D-707)\n","sick-effect")

open(sys.argv[2],'w',encoding='utf-8').write(src)
print(f"wrote {sys.argv[2]} ({n0} -> {len(src)} chars)")

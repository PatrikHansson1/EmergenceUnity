#!/usr/bin/env python3
"""mkd1.py <engine-in(v21)> <engine-out(d1-varv1-twin)>
D1 VARV 1 (prereg UTKAST rev 2, EJ LAST -- riggvarv, ingen matning mot M-D1-maltal, D-701):
hypothesis objects + correction, NO ways. Additive schema. New S.rand draws ONLY inside the
experiment-resolution branch (expT<=0); the yearly sweep draws nothing. 5 anchors, each exactly once.
"""
import sys
src=open(sys.argv[1],encoding='utf-8').read()
n0=len(src)
def rep(anchor,new,label):
    global src
    c=src.count(anchor)
    assert c==1,f"{label}: anchor count {c}: {anchor[:80]!r}"
    src=src.replace(anchor,new)
    print(f"ok {label}")

# A1: state init
A="events:[],knowledge:{},customs:{},nextCustomId:1,"
rep(A,A+"hypos:{},nextHypoId:1,","init")

# A2: hypothesis birth/correction in the failure branch (only under an open need window)
A="      S.stats.failedExperiments++;\n"
B="""      S.stats.failedExperiments++;
          // D1 varv 1 (D-701): a failed experiment against an OPEN need survives as a hypothesis --
          // or corrects the one already held. Draws S.rand only here (the branch already draws).
          if(needBoost(S,t.id)){
            let hy=null;for(const hid in S.hypos){const h=S.hypos[hid];if(h.status==='alive'&&h.holder===a.id&&h.tech===t.id){hy=h;break;}}
            if(hy){hy.trials++;hy.trust=Math.max(0,hy.trust-0.15);
              if(t.alts&&t.alts.length>1){hy.conj=Object.keys(t.alts[Math.floor(S.rand()*t.alts.length)]).join('+')||'plain';hy.mutations++;}
              else hy.mutations++;
            }else{
              const hid='H'+(S.nextHypoId++);
              S.hypos[hid]={id:hid,tech:t.id,need:hypoNeedOf(S,t.id),conj:alt?Object.keys(alt).join('+'):'plain',holder:a.id,village:(S.villages.find(v=>dist(v,a)<12)||{}).name||'',trust:0.5,trials:1,mutations:0,born:Math.floor(S.tick/YEAR),status:'alive'};
            }
          }
"""
rep(A,B,"failure-branch")

# A3: held-marking in the success branch
A="          gainKnowledge(S,a,t.id,'invented',alt);\n"
B="""          gainKnowledge(S,a,t.id,'invented',alt);
          if(S.hypos)for(const hid in S.hypos){const h=S.hypos[hid];if(h.status==='alive'&&h.holder===a.id&&h.tech===t.id){h.status='held';h.trust=Math.min(1,h.trust+0.3);h.held=Math.floor(S.tick/YEAR);}}
"""
rep(A,B,"success-branch")

# A4: helper functions (hoisted) before knowledgeRetentionTick
A="function knowledgeRetentionTick(S){"
B="""// D1 varv 1 (D-701): which open need window covers this tech (mirror of needBoost's windows).
function hypoNeedOf(S,id){const _y=Math.floor(S.tick/YEAR);
  if(S._needSick!==undefined&&_y-S._needSick<=15&&NEED_TECHS.sick.indexOf(id)>=0)return 'sick';
  if(S._needHunger!==undefined&&_y-S._needHunger<=15&&NEED_TECHS.hunger.indexOf(id)>=0)return 'hunger';
  if(S._needCold!==undefined&&_y-S._needCold<=15&&NEED_TECHS.cold.indexOf(id)>=0)return 'cold';
  return '';}
// D1 varv 1: hypotheses die with their holder -- yearly sweep, draws nothing.
function hypoSweep(S){if(!S.hypos)return;
  for(const hid in S.hypos){const h=S.hypos[hid];if(h.status!=='alive')continue;
    let live=false;for(const a of S.agents){if(a.id===h.holder&&!a.dead){live=true;break;}}
    if(!live){h.status='dead';h.died=Math.floor(S.tick/YEAR);}}}
function knowledgeRetentionTick(S){"""
rep(A,B,"helpers")

# A5: yearly hook
A="  if(S.tick%YEAR===0)knowledgeRetentionTick(S);\n"
rep(A,"  if(S.tick%YEAR===0)knowledgeRetentionTick(S);\n  if(S.tick%YEAR===0)hypoSweep(S);\n","yearly-hook")

open(sys.argv[2],'w',encoding='utf-8').write(src)
print(f"wrote {sys.argv[2]} ({n0} -> {len(src)} chars)")

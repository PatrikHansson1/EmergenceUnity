#!/usr/bin/env python3
"""mkb23.py <engine-in(v20)> <engine-out(v21-twin)>
B2.3 (prereg B23-BARARPAFYLLNING locked D-682, sha 76bd497e...): applies to a v20 (f39e0684) engine:
 (1) DEFEKTFIX (D-663): re-individualised souls get born/_fromCity and are PUSHED into S.agents.
 (2) BEARER REPLENISHMENT: a city always keeps its 15 faces — when bearers die, up to 2 souls/year
     step out of the cohorts (FNV 'B23/' keys, mass conserved, traits from traitsM, knows subset).
 (3) MATDIM added to the module export list (D-669, bildgrammatikens E1).
Every anchor must occur EXACTLY once. No new ev(); makeAgent's own RNG use is the only stream effect
(same law as re-individualisation, prereg §5a).
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

# ---- (1) defektfix: push + born + _fromCity inside the re-individualisation k-loop ----
A="""            for(let j=0;j<take;j++)a.knows.add(ks[(h+j*2654435761)%ks.length]);}
        }}"""
B="""            for(let j=0;j<take;j++)a.knows.add(ks[(h+j*2654435761)%ks.length]);}
          a.born=S.tick/YEAR-a.age;a._fromCity=S.villages.indexOf(v);S.agents.push(a); // B2.3 defektfix (D-663): the soul actually enters the world
        }}"""
rep(A,B,"defektfix")

# ---- (2) bearer replenishment: before the de-fold test ----
A="    if(pop+bearersAlive<T.de){\n"
B="""    // B2.3 (D-681/D-682): a city always keeps its 15 faces — when a bearer dies, a soul steps out
    // of the crowd (<=2/year, FNV 'B23/' keys, mass conserved; adults first). Placed after Malthus,
    // before the de-fold test (prereg (g)).
    {
      g.bearers=g.bearers.filter(id=>{for(const a2 of S.agents){if(a2.id===id)return !a2.dead;}return false;});
      let _need=15-g.bearers.length,_made=0;
      if(_need>0){
        const v2=S.villages.find(vv=>vv.name===g.village); const yr2=Math.floor(S.tick/YEAR);
        const B23B=[[1,13],[14,39],[40,64],[65,80]], cis=[1,2,0,3];
        while(_need>0&&_made<2){
          let ci=-1; for(const c2 of cis){if(c[c2]>=1){ci=c2;break;}} if(ci<0)break;
          const h=_fnvh('B23/'+String(S.seed)+'/'+g.village+'/'+yr2+'/'+_made);
          const age=B23B[ci][0]+h%(B23B[ci][1]-B23B[ci][0]+1);
          const a=makeAgent(S,(v2?v2.x:50)+((h>>>8)%7)-3,(v2?v2.y:35)+((h>>>16)%7)-3,null,false);
          a.age=age;a.born=S.tick/YEAR-age;a._fromCity=S.villages.indexOf(v2);
          for(const t in g.traitsM){const mu=g.traitsM[t].mean,sd=g.traitsM[t].sd;a.traits[t]=clamp(mu+((h>>>24)/255-0.5)*2*sd,0.05,0.95);}
          if(age>=14){const ks=g.knowsUnion; const take=Math.min(ks.length,2+(h%4));
            for(let j=0;j<take;j++)a.knows.add(ks[(h+j*2654435761)%ks.length]);}
          c[ci]=Math.max(0,c[ci]-1);S.agents.push(a);g.bearers.push(a.id);_need--;_made++;
        }
        pop=c[0]+c[1]+c[2]+c[3];
      }
    }
    if(pop+bearersAlive<T.de){
"""
rep(A,B,"replenish")

# ---- (3) MATDIM export ----
A="VERSION:'2.6.0',villageAggregate}"
rep(A,"VERSION:'2.6.0',villageAggregate,MATDIM}","matdim-export")

open(sys.argv[2],'w',encoding='utf-8').write(src)
print(f"wrote {sys.argv[2]} ({n0} -> {len(src)} chars)")

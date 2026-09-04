#!/usr/bin/env python3
"""mkxp.py <engine-in(v21)> <engine-out(xp-twin)>
XP-YTAN (PUB-0 Alt 1, D-710; forregistrerad i D1-preregen LOCKED 2026-09-04):
createWorld(seed,founders,xp) dar xp={tune:{TUNE-nyckel:varde}, poke:{year:N,...}}.
HELT INAKTIV nar xp saknas: ingen lasning i tick-vagen utan S._xp, ingen TUNE-mutation.
BEGRANSNING (dokumenterad): xp.tune muterar modulens TUNE-objekt => EN xp-varld per process
(M5-tvillingar korrs en per process). poke ar en NO-OP-barare: verben satts i M5-preregen.
3 ankare, exakt en gang var. Appliceras FORE mkd1v2.
"""
import sys
src=open(sys.argv[1],encoding='utf-8').read()
n0=len(src)
def rep(a,b,l):
    global src
    c=src.count(a); assert c==1,f"{l}: count {c}"
    src=src.replace(a,b); print('ok',l)

# X1: signature
rep("function createWorld(seed,founders){",
    "function createWorld(seed,founders,xp){","signature")

# X2: state fields
rep("nextId:1,maxGeneration:1,usedNames:0,ended:false,",
    "nextId:1,maxGeneration:1,usedNames:0,ended:false,_xp:xp||null,_xpPoked:false,","state")

# X3: tune apply + poke hook. Tune: applied once at world creation (module TUNE mutated -- one
# xp-world per process, see header). Poke: yearly no-op carrier next to the retention hook.
rep("  if(S.tick%YEAR===0)knowledgeRetentionTick(S);\n",
"""  if(S.tick%YEAR===0)knowledgeRetentionTick(S);
  // XP poke-hook (D-710): NO-OP carrier -- verbs are defined in the M5 prereg, not here.
  if(S._xp&&S._xp.poke&&!S._xpPoked&&Math.floor(S.tick/YEAR)>=S._xp.poke.year){S._xpPoked=true;}
""","poke-hook")

# X4: tune application inside createWorld, right after S literal closes. Anchor: the start-event line.
A="  ev(S,'start',`🌍 Four humans wake in an untouched world:"
c=src.count(A); assert c==1,f"tune-apply anchor count {c}"
src=src.replace(A,"""  // XP tune-overstyrning (D-710): mutates module TUNE once -- one xp-world per process.
  if(xp&&xp.tune){for(const _tk in xp.tune)TUNE[_tk]=xp.tune[_tk];}
"""+A)
print('ok tune-apply')

open(sys.argv[2],'w',encoding='utf-8').write(src)
print(f"wrote {sys.argv[2]} ({n0} -> {len(src)} chars)")

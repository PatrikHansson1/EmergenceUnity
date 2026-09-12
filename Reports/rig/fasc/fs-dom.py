import json,glob,collections
SEEDS=['97013','4242','20260718','31415','2323','1618','777','97013-founders']
def load(tag): return {s:json.load(open(f'fs-{tag}-{s}.json')) for s in SEEDS}
def dom(tag,step='step50',verbose=True):
    D=load(tag); top1=collections.Counter(); over50=collections.Counter(); flora=set(); rows=[]
    for s in SEEDS:
        c=collections.Counter(l['type'] for l in D[s][step]); n=sum(c.values())
        mx=max(c.values()); tops=[t for t,v in c.items() if v==mx]
        for t in tops: top1[t]+=1
        for t,v in c.items():
            if v/n>0.5: over50[t]+=1
        srt=sorted(c.items(),key=lambda kv:(-kv[1],kv[0])); third=srt[min(2,len(srt)-1)][1]
        for t,v in c.items():
            if v>=third: flora.add(t)
        rows.append((s,n,len(c),','.join(tops),mx,dict(srt)))
    fs1=all(v<=6 for v in top1.values()); fs2=all(v<=2 for v in over50.values()); fs3=len(flora)>=5
    if verbose:
        print(f'== {tag} {step}')
        for s,n,k,tops,mx,c in rows: print(f'  {s:>15} rader {n:2d} typer {k:2d} top1={tops} ({mx})  {c}')
        print('  top1-räkning:',dict(top1.most_common()))
        print('  >50%-räkning:',dict(over50) or '{}')
        print('  top-3-flora:',len(flora),sorted(flora))
        print(f'  FS1 {"GRÖN" if fs1 else "RÖD"} · FS2 {"GRÖN" if fs2 else "RÖD"} · FS3 {"GRÖN" if fs3 else "RÖD"}')
    return fs1,fs2,fs3,top1,flora
r24=dom('engine-v24text'); r23=dom('engine-v23'); r22=dom('engine-v22')
print('== känslighet steg 25 (v24)'); dom('engine-v24text','step25')
# FS4 identitet
D24=load('engine-v24text');D23=load('engine-v23');D22=load('engine-v22')
same=lambda A,B: all([l['type'] for l in A[s]['step50']]==[l['type'] for l in B[s]['step50']] for s in SEEDS)
print('FS4: v23==v24 typsekvens per rad:',same(D23,D24),'| v22==v23:',same(D22,D23))
for s in SEEDS:
    a=[l['type'] for l in D22[s]['step50']]; b=[l['type'] for l in D23[s]['step50']]
    if a!=b: print('  v22→v23 diff',s,[(x,y) for x,y in zip(a,b) if x!=y])

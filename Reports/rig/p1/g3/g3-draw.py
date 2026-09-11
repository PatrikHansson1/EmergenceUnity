# G3(a) — deterministisk dragning av tio prov (FNV-1a 32 ur frö 20260911), skriven FÖRE materialet läses.
seeds=['97013','4242','20260718','31415','2323','1618','777','97013-founders']
def fnv(s):
    h=2166136261
    for c in s.encode(): h=((h^c)*16777619)&0xffffffff
    return h
trials=[]
for i in range(10):
    kind='same' if i%2==0 else 'diff'   # 5 same, 5 diff, växelvis
    r=fnv(f'20260911:{i}')
    a=seeds[r%8]
    if kind=='same':
        w1=(r>>3)%3; w2=(w1+1+((r>>6)%2))%3   # två olika 50-årsfönster (0:0–49,1:50–99,2:100–149)
        trials.append(dict(id=i+1,kind=kind,A=(a,w1),B=(a,w2)))
    else:
        b=seeds[((r>>3)%7+seeds.index(a)+1)%8]; w=(r>>6)%3
        trials.append(dict(id=i+1,kind=kind,A=(a,w),B=(b,w)))
import json; print(json.dumps(trials,indent=0))
json.dump(trials,open('g3-trials.json','w'))

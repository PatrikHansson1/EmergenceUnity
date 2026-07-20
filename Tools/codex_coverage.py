import os, re, json, glob
ASSETS="/sessions/rcw-016mfkpsxwdczj1wx1ean4sh/mnt/Dev/EmergenceUnity/Assets"
EMSRC=os.path.join(ASSETS,"Emergence")
# 1) gather all referenced names/prefixes from Emergence code
refs=set()
for root,_,files in os.walk(EMSRC):
    for f in files:
        if f.endswith((".cs",)):
            try: txt=open(os.path.join(root,f),encoding="utf-8",errors="ignore").read()
            except: continue
            for m in re.findall(r'"([A-Za-z0-9_ ]{3,})"', txt):
                refs.add(m)
def is_used(name):
    if name in refs: return True
    # prefix match (FindPrefabs uses prefixes like "Prefab_TreeLarge","Prefab_Bush")
    for r in refs:
        if len(r)>=4 and (name.startswith(r) or r.startswith(name)): return True
    return False
# 2) enumerate all prefabs outside Emergence, categorize
rows=[]
for p in glob.glob(ASSETS+"/**/*.prefab", recursive=True):
    if "/Emergence/" in p: continue
    name=os.path.basename(p)[:-7]
    rel=p[len(ASSETS)+1:]
    pack=rel.split("/")[0]
    # category = folder leaf
    cat=os.path.dirname(rel).split("/")[-1]
    rows.append((pack,cat,name,is_used(name)))
# 3) also our own GLB + Quaternius
for p in glob.glob(EMSRC+"/Models/**/*.glb", recursive=True):
    rows.append(("Emergence-GLB", os.path.dirname(p).split("/")[-1], os.path.basename(p)[:-4], True))
rows.sort()
# 4) summary
from collections import defaultdict
per_pack=defaultdict(lambda:[0,0])
for pack,cat,name,used in rows:
    per_pack[pack][0]+=1
    if used: per_pack[pack][1]+=1
total=len(rows); used=sum(1 for r in rows if r[3])
print(f"TOTAL objects indexed: {total}   used-in-code: {used}   ORPHAN (no code ref): {total-used}  ({100*(total-used)//total}% unused)")
print("\nper pack (total / used / orphan):")
for pack,(t,u) in sorted(per_pack.items(), key=lambda kv:-kv[1][0]):
    print(f"  {pack:42s} {t:4d} / {u:3d} / {t-u}")
# 5) write master index CSV to canon
out="/sessions/rcw-016mfkpsxwdczj1wx1ean4sh/mnt/Emergence/45-UNITY/ASSET-MASTER-INDEX.csv"
with open(out,"w",encoding="utf-8") as f:
    f.write("pack,category,object,used_in_code\n")
    for pack,cat,name,u in rows:
        f.write(f'"{pack}","{cat}","{name}",{"yes" if u else "no"}\n')
print("\nmaster index written:", out.split("/mnt/")[-1], f"({total} rows)")

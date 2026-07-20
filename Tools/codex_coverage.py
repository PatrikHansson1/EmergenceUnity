#!/usr/bin/env python3
# EMERGENCE — codex coverage & integrity tool (TD-033, hardened D-083).
# Now checks the CODEX (not just code refs): reports ORPHANS (asset with no codex entry),
# DANGLING (codex entry whose prefab was deleted — the EP's "delete function"), and coverage.
# Path-robust: derives the repo root from this file's location (no stale session id).
import os, re, json, glob, sys
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)                       # Tools/.. = repo root
ASSETS = os.path.join(REPO, "Assets")
EMSRC  = os.path.join(ASSETS, "Emergence")
CODEX  = os.path.join(EMSRC, "Codex", "object-codex.json")

# ---- 1) every prefab/GLB we own (the master set) ----
asset_names = set()
rows = []
for p in glob.glob(ASSETS + "/**/*.prefab", recursive=True):
    if os.sep + "Emergence" + os.sep in p: continue
    name = os.path.basename(p)[:-7]
    rel = os.path.relpath(p, ASSETS)
    pack = rel.split(os.sep)[0]
    cat = os.path.dirname(rel).split(os.sep)[-1]
    asset_names.add(name); rows.append((pack, cat, name))
for p in glob.glob(EMSRC + "/Models/**/*.glb", recursive=True):
    name = os.path.basename(p)          # keep extension for .glb (codex refers to "mill.glb")
    asset_names.add(name); asset_names.add(name[:-4])
    rows.append(("Emergence-GLB", os.path.dirname(p).split(os.sep)[-1], name))

# ---- 2) the codex ----
codex_prefabs = {}
if os.path.exists(CODEX):
    codex = json.load(open(CODEX, encoding="utf-8"))
    for o in codex.get("objects", []):
        codex_prefabs[o["id"]] = o.get("prefab", "")
else:
    print("WARN: no object-codex.json at", CODEX)

def asset_exists(pref):
    if not pref: return False
    if pref in asset_names: return True
    base = pref[:-7] if pref.endswith(".prefab") else (pref[:-4] if pref.endswith(".glb") else pref)
    return base in asset_names or pref in asset_names

# ---- 3) integrity: DANGLING (delete guard) + coverage ----
dangling = [(oid, pref) for oid, pref in codex_prefabs.items() if not asset_exists(pref)]
codex_asset_basenames = set()
for pref in codex_prefabs.values():
    codex_asset_basenames.add(pref); codex_asset_basenames.add(pref[:-7] if pref.endswith(".prefab") else pref[:-4] if pref.endswith(".glb") else pref)
orphans = [(pk, ct, nm) for (pk, ct, nm) in rows if nm not in codex_asset_basenames and (nm + ".glb") not in codex_asset_basenames]

total = len(rows); indexed = total - len(orphans)
print(f"ASSETS owned: {total}   in codex: {indexed}   ORPHAN (no codex entry): {len(orphans)}  ({100*len(orphans)//max(total,1)}% un-indexed)")
print(f"CODEX entries: {len(codex_prefabs)}   DANGLING (entry -> deleted/missing prefab): {len(dangling)}")
if dangling:
    print("  !! DANGLING (fix before dressing — these will silently skip):")
    for oid, pref in dangling: print(f"     {oid} -> {pref}")
else:
    print("  OK: every codex entry resolves to an owned asset (no broken references after deletions).")
from collections import defaultdict
per = defaultdict(int)
for pk, ct, nm in orphans: per[pk] += 1
print("\norphans per pack (top):")
for pk, n in sorted(per.items(), key=lambda kv:-kv[1])[:12]:
    print(f"  {pk:42s} {n}")
# write master index with the codex flag
# write to the repo's own Reports/ (robust on any machine); copy to canon 45-UNITY/ if wanted
os.makedirs(os.path.join(REPO, "Reports"), exist_ok=True)
out = os.path.normpath(os.path.join(REPO, "Reports", "asset-master-index.csv"))
try:
    with open(out, "w", encoding="utf-8") as f:
        f.write("pack,category,object,in_codex\n")
        for pk, ct, nm in sorted(rows):
            inx = "no" if (nm not in codex_asset_basenames and (nm+".glb") not in codex_asset_basenames) else "yes"
            f.write(f'"{pk}","{ct}","{nm}",{inx}\n')
    print("\nmaster index written:", out)
except Exception as e:
    print("index write skipped:", e)

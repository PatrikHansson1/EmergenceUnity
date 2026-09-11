#!/usr/bin/env python3
# m4o-dom.py — MEKANISK OMVARVS-avläsning per LÅST prereg D1-OMVARV-PREREG-LOCKED-2026-09
# (sha 699cfd40...; ärver 6e6fbedd:s G1/G2-definitioner via m4-dom.py-logiken). Läser
# m4o-results.txt (v23, k_sick=9, samma 20 frön) och beräknar M-O1/M-O2/M-O3/M-O6/M-O6b;
# M-O4/M-O5 är liveprotokoll/kostnadssteg utanför denna fil. VÄGRAR ofullständig fil.
# Kör: python m4o-dom.py [resultatfil]
import sys, json, io

path = sys.argv[1] if len(sys.argv)>1 else "m4o-results.txt"
rows, done = {}, False
for line in io.open(path, encoding="utf-8"):
    line=line.strip()
    if line.startswith("# DONE 20/20"): done=True
    if not line or line.startswith("#"): continue
    try: r=json.loads(line)
    except Exception: continue
    if r.get("tag") and r.get("secs") is not None: rows[r["tag"]]=r
assert done, "VAGRAR: '# DONE 20/20' saknas — ingen partiell avläsning"
assert len(rows)==20, f"VAGRAR: {len(rows)} kompletta världar, kräver 20"

CANON={"97013","4242","20260718","31415","2323","1618","777","97013-founders"}

def last_discovery(r):
    seen=set(); disc=[]
    for e in r["disc"]:
        if e["t"]=="tech":
            if e["id"] in seen: continue   # G1: endast FÖRSTA gången per id
            seen.add(e["id"])
        disc.append(e)
    if not disc: return None
    return max(disc, key=lambda e:(e["year"], e["tick"]))

def kronike_ok(r, way_id):
    for e in r["wayEvs"]:
        if e["t"]=="wayBorn" and e["way"]==way_id:
            types=[c.split("@")[0] for c in e.get("causes",[]) if isinstance(c,str)]
            types+=[c.split("@")[0] for c in e.get("correctedCauses",[]) if isinstance(c,str)]
            return bool(e.get("agent")) and ("corrected" in types) and (("death" in types) or ("sickness" in types))
    return False

tab=[]; o2_worlds=[]; wayfree=0; c_fail=[]; f_seen=False
for tag,r in sorted(rows.items(), key=lambda kv:(kv[1]["cls"],kv[0])):
    ld=last_discovery(r)
    if r["ways"]==0: wayfree+=1
    if any(e["t"]=="wayLost" for e in r["wayEvs"]) or any(w["st"]=="dead" for w in r["wayDetail"]): f_seen=True
    ld_is_way = ld is not None and ld["t"]=="wayBorn"
    pred = kronike_ok(r, ld["id"]) if ld_is_way else None
    if ld_is_way and pred: o2_worlds.append(tag)
    if tag in CANON:
        a12=r.get("at1200")
        if not a12 or a12.get("ended") or a12.get("pop",0)<=0: c_fail.append(tag)
    tab.append((tag, r["cls"], ld["t"] if ld else "-", str(ld["id"]) if ld else "-",
                ld["year"] if ld else "-", r["ways"], "JA" if pred else ("NEJ" if pred is not None else "-"),
                r["endYear"], r["pop"]))

print("tag | klass | sistaUpptäckt(typ,id,år) | ways | krönikepred | endYear | pop")
for t in tab: print(f"{t[0]} | {t[1]} | {t[2]},{t[3]},y{t[4]} | {t[5]} | {t[6]} | y{t[7]} | {t[8]}")
print()
wayworlds = 20-wayfree
o1 = (wayworlds<=8) and (wayfree>=12)
print(f"M-O1 (manusvakt: way-världar ≤8 OCH way-fria ≥12): way-världar {wayworlds}/20, way-fria {wayfree}/20 -> {'GRÖN' if o1 else 'RÖD'}")
print(f"M-O2 (M-D1a-skydd: ≥1/20 way-sist + G2-predikat): {len(o2_worlds)}/20 -> {'GRÖN' if o2_worlds else 'RÖD'} {o2_worlds}")
print(f"M-O3 (alla 8 kanon lever år 1200): {'GRÖN' if not c_fail else 'RÖD '+str(c_fail)}")
print(f"M-O6 (soft, way-livscykeldöd): {'observerad' if f_seen else 'ej observerad'} (blockar ej)")
print(f"M-O6b (soft, way-världar ≥2): {wayworlds}/20 -> {'uppfylld' if wayworlds>=2 else 'EJ uppfylld — signal, blockar ej'}")
verdict = 'HELGRÖN (M-O1+M-O2+M-O3)' if (o1 and o2_worlds and not c_fail) else 'RÖD'
print(f"HÅRDA SVITEN: {verdict}")
print("OBS: M-O4 (determinism/liveprotokoll/rebaseline/commit-ordning) och M-O5 (V8-kostnad) är egna dagtidssteg.")

#!/usr/bin/env python3
# m4-dom.py — MEKANISK M4-avläsning per prereg 6e6fbedd (§4 G1/G2, §5, §6). Läser m4-results.txt
# och beräknar fakta; domen exekveras i D-loggen av läsaren mot §6-fallträdet. VÄGRAR ofullständig
# fil (kräver "# DONE 20/20" + 20 unika kompletta rader). Kör: python m4-dom.py [resultatfil]
import sys, json, io

path = sys.argv[1] if len(sys.argv)>1 else "m4-results.txt"
rows, done = {}, False
for line in io.open(path, encoding="utf-8"):
    line=line.strip()
    if line.startswith("# DONE 20/20"): done=True
    if not line or line.startswith("#"): continue
    try: r=json.loads(line)
    except Exception: continue
    if r.get("tag") and r.get("secs") is not None: rows[r["tag"]]=r
assert done, "VAGRAR: '# DONE 20/20' saknas — ingen partiell avläsning (§5/G11)"
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
    return max(disc, key=lambda e:(e["year"], e["tick"]))  # sista = högsta år, tie högre tick

def kronike_ok(r, way_id):
    # G2: wayBorn-ev för way:en bär agentref + kedja som når (i) behovssignal (death/sickness-ev)
    # och (ii) senaste korrigeringen (corrected-ev). causes är upplösta till "typ@yN" av m4.js.
    for e in r["wayEvs"]:
        if e["t"]=="wayBorn" and e["way"]==way_id:
            types=[c.split("@")[0] for c in e.get("causes",[]) if isinstance(c,str)]
            types+=[c.split("@")[0] for c in e.get("correctedCauses",[]) if isinstance(c,str)]
            return bool(e.get("agent")) and ("corrected" in types) and                    (("death" in types) or ("sickness" in types))
    return False

tab=[]; a_worlds=[]; b_wayfree=0; c_fail=[]; f_seen=False
for tag,r in sorted(rows.items(), key=lambda kv:(kv[1]["cls"],kv[0])):
    ld=last_discovery(r)
    ways=r["ways"]
    if ways==0: b_wayfree+=1
    if any(e["t"]=="wayLost" for e in r["wayEvs"]) or any(w["st"]=="dead" for w in r["wayDetail"]): f_seen=True
    ld_is_way = ld is not None and ld["t"]=="wayBorn"
    pred = kronike_ok(r, ld["id"]) if ld_is_way else None
    if ld_is_way and pred: a_worlds.append(tag)
    if tag in CANON:
        a12=r.get("at1200")
        if not a12 or a12.get("ended") or a12.get("pop",0)<=0: c_fail.append(tag)
    tab.append((tag, r["cls"], ld["t"] if ld else "-", str(ld["id"]) if ld else "-",
                ld["year"] if ld else "-", ways, "JA" if pred else ("NEJ" if pred is not None else "-"),
                r["endYear"], r["pop"]))

print("tag | klass | sistaUpptäckt(typ,id,år) | ways | krönikepred | endYear | pop")
for t in tab: print(f"{t[0]} | {t[1]} | {t[2]},{t[3]},y{t[4]} | {t[5]} | {t[6]} | y{t[7]} | {t[8]}")
print()
print(f"M-D1a (way sist + G2-predikat, kräver ≥1/20): {len(a_worlds)}/20 -> {'GRÖN' if a_worlds else 'RÖD'} {a_worlds}")
print(f"M-D1b (0 ways, kräver ≥12/20): {b_wayfree}/20 -> {'GRÖN' if b_wayfree>=12 else 'RÖD'}")
manus = 20-b_wayfree
print(f"  manusvakt: way-världar = {manus}/20 ({'LARM >8' if manus>8 else 'ok'})")
print(f"M-D1c (alla 8 kanon lever år 1200): {'GRÖN' if not c_fail else 'RÖD '+str(c_fail)}")
print(f"M-D1f (soft, way/hypotes dör med siste bäraren): {'observerad' if f_seen else 'ej observerad'} (blockar ej)")
print("OBS: M-D1d/e redan gröna (D-720/724/725/726/735). Domen mot §6-fallträdet exekveras i D-loggen.")

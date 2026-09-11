#!/usr/bin/env python3
# kalib.py — mekanisk implementation av den FRUSNA §2-kalibreringsformeln
# (D1-OMVARV-PREREG-UTKAST-2026-09.md rev 2, sha f21456be..., frusen 2026-09-06; D-746).
# Läser m4t-results FÖRST efter # DONE 20/20. Ingen tolkningsfrihet: formeln ordagrant.
#   VAKTEN:  W(K) = |{w : maxsick(w) >= K}|, K i {9..12}; enbart ocensurerade svansar.
#   VALET:   k_sick = minsta K i {9..12} med W(K) <= 7 som OCKSÅ klarar SKYDDET.
#   SKYDDET: A = {1618, 20260718, 97013, 97013-founders, 1186834928, 1203612547, 1237167785};
#            reach(w) = max(maxsick(w), 9 om cens(w) annars 0); krav: minst en w i A
#            med reach(w) >= k_sick.
#   cens(w) = w har wayHypo med need=sick och trials=8.
#   UTGÅNG SAKNAS: inget giltigt K => RÖD (exit 2). Saknat # DONE/20 rader => vägran (exit 3).
import json, sys
A = {"1618","20260718","97013","97013-founders","1186834928","1203612547","1237167785"}
path = sys.argv[1] if len(sys.argv) > 1 else "m4t-results.txt"
lines = open(path, encoding="utf-8").read().splitlines()
if not any(l.strip().startswith("# DONE") for l in lines):
    print("VÄGRAR: ingen '# DONE'-markör i", path); sys.exit(3)
rows = [json.loads(l) for l in lines if l.strip() and not l.strip().startswith("#")]
if len(rows) != 20:
    print(f"VÄGRAR: {len(rows)} datarader, kräver exakt 20"); sys.exit(3)
tags = [r["tag"] for r in rows]
if len(set(tags)) != 20:
    print("VÄGRAR: dubblerade tags"); sys.exit(3)
def maxsick(r):
    ks = [int(k) for k, v in (r.get("tailByNeed", {}).get("sick", {}) or {}).items() if v >= 1]
    return max(ks) if ks else 0
def cens(r):
    return any(h.get("need") == "sick" and h.get("trials") == 8 for h in r.get("wayHypos", []))
def reach(r):
    return max(maxsick(r), 9 if cens(r) else 0)
print("värld               maxsick cens reach  iA")
for r in sorted(rows, key=lambda r: -reach(r)):
    print(f"{r['tag']:<19} {maxsick(r):>7} {str(cens(r)):>4} {reach(r):>5}  {'A' if r['tag'] in A else ''}")
missingA = A - set(tags)
if missingA:
    print("VÄGRAR: A-världar saknas i datat:", sorted(missingA)); sys.exit(3)
chosen = None
for K in (9, 10, 11, 12):
    W = sum(1 for r in rows if maxsick(r) >= K)
    guard = W <= 7
    prot = sum(1 for r in rows if r["tag"] in A and reach(r) >= K)
    ok = guard and prot >= 1
    print(f"K={K}: W(K)={W} vakt({'<=7 OK' if guard else '>7 FALL'}) skydd(A-världar med reach>=K: {prot}) => {'GILTIGT' if ok else 'ogiltigt'}")
    if ok and chosen is None:
        chosen = K
if chosen is None:
    print("RESULTAT: inget giltigt K i {9..12} — RÖD till hemkomstbordet (ingen handkalibrering)")
    sys.exit(2)
print(f"RESULTAT: k_sick = {chosen}")

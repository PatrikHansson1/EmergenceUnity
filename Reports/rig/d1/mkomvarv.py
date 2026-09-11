#!/usr/bin/env python3
# mkomvarv.py — generator v22 -> v23 (D1-omvarvet, K1-patchen; prereg D1-OMVARV rev 2,
# sha f21456be..., D-746/D-747). SKRIVEN i förväg (väckning 155); KÖRS först vid låset,
# med k_sick från kalib.py. Rivbarhet = kör inte generatorn; live-motorn RÖRS ALDRIG här.
# Användning: python3 mkomvarv.py <k_sick>   (k_sick i {9,10,11,12})
# Läser live v22 (sha-assert), skriver Reports/rig/d1/engine-v23.js + sha256 på stdout.
import hashlib, sys, os
V22_SHA = "952f21f670cda2e83d3f4871dd2535d65caacb8b3f0f7b305aa161032bc273c9"
SRC = os.path.join(os.path.dirname(__file__), "..", "..", "..",
                   "Assets", "StreamingAssets", "Emergence", "emergence-engine.js")
DST = os.path.join(os.path.dirname(__file__), "engine-v23.js")
if len(sys.argv) != 2 or sys.argv[1] not in ("9", "10", "11", "12"):
    print("Användning: mkomvarv.py <k_sick i {9..12}>"); sys.exit(3)
K = sys.argv[1]
src = open(SRC, encoding="utf-8").read()
got = hashlib.sha256(src.encode("utf-8")).hexdigest()
if got != V22_SHA:
    print("VÄGRAR: käll-sha", got[:16], "!= v22", V22_SHA[:16]); sys.exit(3)
# ANKARE 1 (räknat: exakt 1): way-födselvillkoret i failed-experiment-grenen.
A1 = "if(hy.trials===8&&!hy.wayId){"
if src.count(A1) != 1:
    print(f"VÄGRAR: ankare A1 träffar {src.count(A1)} ggr, kräver exakt 1"); sys.exit(3)
patched = src.replace(A1,
    "if(hy.trials===((hy.need==='sick')?" + K + ":8)&&!hy.wayId){"
    + "// v23 D1-omvarv K1: k_sick=" + K + " (prereg D1-OMVARV-PREREG rev2 f21456be)\n                ", 1)
# Sanitet: patchen ändrade exakt ett ställe och inget annat.
if patched.count("v23 D1-omvarv K1") != 1 or A1 in patched:
    print("VÄGRAR: patchresultatet inkonsistent"); sys.exit(3)
open(DST, "w", encoding="utf-8", newline="\n").write(patched)
print("skrev", DST)
print("v23 sha256 =", hashlib.sha256(patched.encode("utf-8")).hexdigest())

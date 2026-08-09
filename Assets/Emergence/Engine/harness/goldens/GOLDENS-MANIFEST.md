# GOLDENS — ENGINE 2.4.1 baseline (E1.5b hardening: review conditions V1/V2/V6/V8 closed; 2026-08-09)

2.4.1 = 2.4.0 + the E1.5b wave (TD-082, D-176, closing E15-WAVE-REVIEW-2026-07-25 §4):
**V1 (I2)** leader rule REBUILT — `leaderScore` reads UNCLAMPED accumulated standing
(knowledge+ways+age+wealth+ambition) and recognition demands an ABSOLUTE lead
(`TUNE.leaderLead`, eased by sqrt(leaderVillage/adults) as the group grows — scalar stress);
leader frequency now rises weakly with village size instead of falling.
**V2 (I6)** the revenge trigger reads the grudge BOOKKEEPING (`a.grudges`, in conjunction with
hot `rel`), never `rel` alone — every `feud` event resolves an `ev:` reference BY CONSTRUCTION.
**V6 (I1)** theft chains to its drive: every winter onset is now a `season` event
(`S.lastWinterEv`), starvation deaths are bookkept per village (`S.lastStarve`), and a
desperation `steal` cites the freshest of them; a rare one-per-life `hoard` milestone event
(`TUNE.hoardMark`) lets greed's `raid` cite the pile it preys on (or the leadership event whose
tribute fed it). **V8 (I8)** feud lines in `writeHistory` obey the same room law as quirks.
No RNG draws added anywhere; primary/secondary mulberry32 stream discipline unchanged.
VERSION honestly bumped to '2.4.1'.
**Histories DIVERGE from 2.4.0 BY DESIGN** (V1/V2 change sim behavior; V6 adds events that
shift event ids) — same seed still ⇒ bit-identical rerun, proven by double fresh-process node
runs at t720 AND t8640 on all four suite runs (8/8 IDENTICAL). Engine SHA-256:
`91e5535db3dfaae4a1ce3fafcb70e088cd48425489abb7e65f6e8b1f0c3ec7b0`
(live twin: Assets/StreamingAssets/Emergence/emergence-engine.js — the Engine/ copy is the
write-locked 2.0.1 relic per D-093 and is NOT the source; EngineSourcePath prefers the twin).

Regeneration provenance (all 2026-08-09): FÖRE-gate — node reproduced ALL EIGHT checked-in
2.4.0 canons byte-perfect (t720 + t8640) before any change; post-change compile CLEAN
(10:20:49, errors 0, warnings 16 = baseline); Jint gate RED 4/4 (10:23, expected) with
divergent files byte-identical to the node-generated new t720 canons (cmp OK ×4); t720 canons
below = those Jint-divergent/node-identical files; t8640 canons node-generated (node = jint
parity law, byte-proven at t720 above and confirmed GREEN by the full-depth Jint gate).

| Run | Ticks | Canon SHA-256 |
|---|---|---|
| 97013 | 720 | `04f3b29a1a6ac31f0e62abb781bb6d1aa06ebfd793239b89388397ce387fb9f5` |
| 4242 | 720 | `33b4bbf16a244e11199a7e248e1998117c0d067d8bc4c2a7cd5e0dd5e0821453` |
| 20260718 | 720 | `a0c59e227dba2cc2e951d1c53084f9a075682b51816eb2a64a475ea2de86ebcf` |
| 4242 + test founders | 720 | `a8107e62aee0a765fc5f2ff28db92de1e4b9aeed498091a38f31ee87f924a28a` |
| 97013 | 8640 | `92298942cd2f22a4544ea73fb7ff11c5c9a3490910248431fbfb36690fa9067e` |
| 4242 | 8640 | `a3ce741116d3600e655f67a7d91ddf97ece577fc84497ea09707b1a49be86a3f` |
| 20260718 | 8640 | `330656b59b22c2bcf94ac798358cd5be091f1380cfffbd9e002d3e261b26a894` |
| 4242 + test founders | 8640 | `58509b6f0fd4116bbfe737157a210accc0e66af33fa510ae1c4ff9754af92031` |

Test founders string unchanged (see GoldenMasterRunner.TestFounders).

---
Previous baseline (ENGINE 2.4.0, E1.5, superseded 2026-08-09): engine SHA `7fc4ae8e…`,
t720 canons `a90f3f5d…`/`14f4c9e0…`/`cc5bb8cf…`/`37f1cb27…`, t8640 `dbfe5398…`/`aaf6f577…`/
`e3f87046…`/`7f3ed82f…` (full values in git history of this file). Earlier 2.0.1→2.3.2
provenance likewise in git history.

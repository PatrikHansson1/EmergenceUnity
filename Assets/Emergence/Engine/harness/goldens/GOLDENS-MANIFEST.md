# GOLDENS — ENGINE 2.6.0 + F1.0c + F1.2a-d + F1.4 ★ TÖRSTEN BASELINEAD ★ (alla 8 kanoner om-baselinerade 2026-08-21, D-462)

**F1.4 (D-451): TÖRSTEN OCH PLATSEN.** Törst-behov (init 80) · avtappning säsong × krukbärande
× torrår · torråret vart ~7:e år per FNV(frö@år), deterministiskt · opportunistiskt drickande
nära vatten · uppsökande vattenvandring vid törst<20 (svälten går alltid först) · **uttorkning
FÖRSVAGAR (energi −1,5/tick vid 0), dödar aldrig direkt** — dödskanalen var probens tillägg,
aldrig D-393:s krav. Inga brunnsmekaniker (W3 hedersamt fälld — gratisvatten dämpar olikhet).
**Slutprov 32 frön:** W1 0,33 (baslinje 0,66 — byarna dras mot vattnet) · 1 utdöd/32 (31337
LEVER nu) · 0 törstdödar. **Baselineringen 2026-08-21:** kanonerna genererades i MOLNET med
harnessen (byte-exakthet FÖRBEVISAD: gamla kanonernas SHA reproducerade exakt på båda
horisonter), installerades, och Unity/Jint körde GRÖNT direkt — t720 alla 4 (93–122 s/frö),
t8640 alla 4 (2 125–3 275 s/frö, ~3 h 10 min; F1.4-världarna är ~65 % större i händelsevolym).
Varje jintSha == molnprediktion. Engine SHA: 2a77bfcf23717502… (föregående: 8a473f0dff7d62fe…,
backup-ref backup/pre-f14-2026-08-21 → bda59f84ebf4…; gamla kanoner i goldens/.pre-f14/).

---

# GOLDENS — ENGINE 2.6.0 + F1.0c + F1.2a-d ★ F1.2-FAMILJEN KOMPLETT ★ (alla 8 kanoner om-baselinerade 2026-08-21, D-427)

**F1.2d (D-377/D-384): SKARP GEOLOGI — MARKEN SKILJER SIG ÅT.** Per (frö, material) via
FNV-1a får varje värld 0× / 0,4× / 1,6× av varje malm (30/40/30), lera med golv 6.
Världsgen ändras, så **alla 8 kanoner divergerar FRÅN FÖRSTA TICKET med avsikt** — redan
dag ett berättar en annan själ den första sagan. **Ödesomfördelningen i rådata:** lilla
97013 fick rik mark och skriver 2,4× mer historia (428 679 → 1 031 270 byte); förra veckans
rikaste 4242 blev fattigare (1 259 548 → 762 806). Disk-kedjans acceptans: **parvis 5,3 mot
måltalet ≥ 5, 0 döda av 8.** Ordningen hölls i bägge baseliningarna: RÖD → granskad diff →
ny kanon → GRÖN, bekräftelsernas jint-hashar byte-identiska. Motor-SHA: `8a473f0d…`.
**Familjens facit:** a (`20c4d8a`, inga kanoner) · b (`9894b8c`, EN kanon) · c (`cf5b8c5`,
alla 8) · d (denna commit, alla 8, från första ticket). Fyra steg, åtta baseliningar,
noll oplanerade röda.

---

# GOLDENS — ENGINE 2.6.0 + F1.0c + F1.2a-c (ALLA 8 kanoner om-baselinerade 2026-08-21, D-421)

**F1.2c (D-374/D-390/D-409): sammansättningen** — två material blir ett nytt i verkstaden,
grindat av `hasLeisure` (D-086:s kulturlag), eld + värme ≥ max(thresh)+2, djup ≤ 1, tabu på
bägge delarna och 60-ticks karens (`_compCd`). Mixregeln: LIMITING-dimensioner medlas,
övriga tar max. **ALLA 8 kanonfiler om-baselinerade** — t720-kvartetten divergerar redan
från de första åren (det nya själsfältet `_compCd` serialiseras från första beröringen),
t8640-kvartetten bär den fulla verkan: **rika 4242 skriver 27 % mer historia
(994 684 → 1 259 548 byte), lilla 97013 nästan ingen (+116 byte)** — sammansättningen
arbetar där överskottet finns, precis som leisure-grinden föreskriver. Ordningen hölls i
BÅDA baseliningarna: RÖD med avsikt → granskad diff → ny kanon → GRÖN, bekräftelsernas
jint-hashar byte-identiska med röda passens. Motor-SHA: `bd3f1874…`. Föregående steg:
F1.2b (`9894b8c`, endast 20260718-t8640) · F1.2a (`20c4d8a`, inga kanoner rörda).

---

# GOLDENS — ENGINE 2.6.0 + F1.0c + F1.2a + F1.2b (20260718-t8640 om-baselinerad 2026-08-20, D-420)

**F1.2b (D-383/D-404): recepten läser egenskaper** — `pickAlt` ersatt: exakt recept först,
därefter substitution under unionsregeln (bägge materialens särskiljande dimensioner, z≥0,8
min 2, band 0,6σ), tabu prövat mot det faktiskt konsumerade materialet (G-review I2).
**Kirurgin, mätt:** endast `seed-20260718-t8640` om-baselinerad — världen divergerar vid sitt
FÖRSTA bronzetools-event år 32 (374→353 event, 708 264→597 313 byte). De andra tre t8640-kanonerna
och hela t720-kvartetten är BYTE-IDENTISKA: exakta receptet vinner när det kan uppfyllas, och
substitution utan geologisk brist är sällsynt (samma fynd som D-403/D-414). Ordningen hölls:
RÖD med avsikt → granskad diff → ny kanon → GRÖN, bekräftelsens jint-hash byte-identisk med
röda passets (`557335bd…`). Motor-SHA: `76d62fee…`. F1.2a (D-383, MATDIM-tabellen) lämnade
alla kanoner orörda — gröna båda horisonterna, commit `20c4d8a`.

---

# GOLDENS — ENGINE 2.6.0 + F1.0c (t8640-kanon om-baselinerad 2026-08-16, D-360/D-407)

**F1.0c (D-360): brons och stål fanns som TEKNIK men aldrig som TING** — teknikobjektet
har inget utdatafält och MATSOURCE saknade dem, så bronzetools/clock/printpress/steam var
onåbara i varje värld (själar vandrade "searching for undefined", 42 663 själ-tick på ett frö).
F1.0c lägger SMELT (bronze=copper*2+tin, steel=iron*2+coal*2) som en arbetshandling.
Inga rand-drag i ingreppet; strömmen skiljer sig först vid första metallarbetet.

**Om-baseliningen, exakt:**
- t720-kvartetten: **GRÖN MOT OFÖRÄNDRAD KANON** — vid år 5 kan ingen brons/stål, så
  patchen är byte-identisk före sin egen mekanism. Kanon INTE bytt.
- t8640: 97013 **GRÖN mot oförändrad kanon** (den lilla tvillingbyvärlden når aldrig
  metallarbetet på 60 år); **4242 / 20260718 / 4242-founders om-baselinerade**
  (RÖD 15:13 → ny kanon → GRÖN 16:12, jint-hashar byte-identiska mellan passen).
  Nya kanonfiler är 2–3× större än de gamla — F1.0c:s verkan synlig i rådata:
  världarna når längre och skriver mer historia.
- Engine-SHA: c14afaa806110ec82f3c4763e5d01a1130b1f33a5471565674734061a02eebcd
  (båda ENGINE-SHA.txt + ExpectedEngineSha uppdaterade i samma commit).

Föregående manifest (2.4.1-baslinjen) nedan, orört:

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

## RE-BASELINED 2026-08-14 — ENGINE 2.5.0 (M1+M2 of D-226 / D-228)
Three insights (`weightsBalance`, `soundsRing`, `herbsHeal`) were defined in `OBS`, required by techs,
and observed nowhere. `tryObserve` hooks were added at the moments the insight texts themselves name,
and `printpress.pre` was repointed off a tech id that does not exist. `tryObserve` consumes `S.rand`,
so the sim stream moves BY CONSTRUCTION — this is a deliberate, approved re-baseline, not a regression.

| seed | t=720 canonical SHA |
|---|---|
| 97013 | `ca250f8908b43da8ab2a26d496ab84db33138844d78bf3d3a9f70ea05b3ec27c` |
| 4242 | `d84809227af570c4fe1dd878bf01aa96854a56853ca1197d2d8aa578727f6c27` |
| 20260718 | `e718ec6c2346d59180f0d989d25f55b064dcf4c42a35ee3d848bbbb97375518e` |
| 4242-founders | `f80404f8827b47e316a2e57e5ec5b53f4a25c678c36754d6929c7275fde9bfed` |

Engine SHA: `87a6576c119dd045e6c88ae46efa37030d90e008d31ff18cec6109c7d0a05868` (was `22e2ac053cd8…`).

**DEBT, stated so it is not discovered later:** the `t8640` goldens in this folder are from 2.4.1 and are
now STALE. The t720 quartet is the per-step cadence (D-131); the deep horizon must be re-baselined with
`Reports/GOLDEN_DEPTH.txt` before the next full-depth gate.

## RE-BASELINED 2026-08-14 (second) — ENGINE 2.5.1 (M4 of D-226 / D-229)
Clay and sand are DEPOSITS, not ore bodies. They were mined to zero and never returned, and writing —
which costs clay, and which the canon calls the arc's ENDING — died with them mid-run. They now regrow
like forest at 4x the interval. Ore (iron/copper/tin/coal/gold) stays finite; its exhaustion is the
geography story, not a bug. `regrowLater` consumes `S.rand`, so the stream moves by construction.

| seed | t=720 canonical SHA |
|---|---|
| 97013 | `e97b191fd8399d16c4477401a643fa1dd207bbc1cd7f687d3309b61bca58e2bc` |
| 4242 | `649ef324d71b1ebe84fe0ea1bd1a3524d2bcad7008ce11764d4c6223b7918dbe` |
| 20260718 | `f6d6f94ad1b1451e0041e203fc31fdf4be091800b49d3a3f953ffe55980e6680` |
| 4242-founders | `0e1ecd5658cc8301cb6f5891ee9db9edddde12126827e8594fc5e3afdd4fe2ac` |

Engine SHA: `b83e53803ea5b0c6723adb95068fa88ee68eb4407da43223607c229491dcbef7` (was `87a6576c119d…`).
The `t8640` debt above still stands.

## RE-BASELINED 2026-08-14 (third) — ENGINE 2.6.0 (M3 of D-226 / D-234)
The curiosity expedition could bring home only six materials and the ORES were not among them, so
`copperGreen` was unobservable and the whole bronze->steel->clock->steam branch was unreachable in
every world. Ores added; and the reach a people can fetch from now grows with wheel/sailing/road,
plus a partner village's ground when they can reach it (the tin trade).

| seed | t=720 canonical SHA |
|---|---|
| 97013 | `4c361d7eda016172aa915f525ff629409be9f5b16295761a8852cba82a5b66cf` |
| 4242 | `10daf429db7435d5ff6ee351b7c16c0078e6535e7c2d0c3a878e76465c95131f` |
| 20260718 | `429540dd73ad6785f0539b788b6ac73b087ce3792e3b430f0a021a8f9dbbda0e` |
| 4242-founders | `d2307e0fb9150774a282547deca0275ddf692d0768dad87c4e0632c3b5a75646` |

Engine SHA: `2fc1647cc683ee856304931926e338d59919ed35ba04bbc529459f8d87132933` (was `b83e53803ea5…`).

## DEEP HORIZON RE-BASELINED 2026-08-14 — t=8640 (60 years), ENGINE 2.6.0 — THE DEBT IS PAID
The `t8640` goldens had been 2.4.1 since this morning and were declared stale in writing three times
today rather than quietly carried. They are now regenerated against 2.6.0 and confirmed by a second
full run: RED 4/4 -> new canon installed -> GREEN 4/4. Two passes, ~58 minutes each.

| seed | t=8640 canonical SHA | secs |
|---|---|---|
| 97013 | `27ef6ac4caf0ce0d7fcc499c71595dfd88d9365d3e79c506513d80f9efad36a8` | 539 |
| 4242 | `5c789cb8e11b009e4b03187b068037d249bdb45984c4dbb88bc9219a9f268eb2` | 1076 |
| 20260718 | `f526a4385df9ba6065f0e81886aec4043d12ef4ea676b34cbd00146d060064f1` | 972 |
| 4242-founders | `7ac885c8e8f80c28de105087ed88e0221907405d387f7c61557faad42c3ba603` | 890 |

Engine SHA: `2fc1647cc683ee856304931926e338d59919ed35ba04bbc529459f8d87132933`.
Both horizons now hash the SAME engine. `Reports/GOLDEN_DEPTH.txt` has been stood down so the
per-step cadence is the fast t=720 quartet again (D-131).


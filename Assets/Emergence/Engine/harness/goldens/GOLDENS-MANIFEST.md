# GOLDENS — ENGINE 2.4.0 baseline (E1.5 "dramatik-minimum": dedicated conflict traits, causal violence chains, leaders, gift-ways; 2026-08-09)

2.4.0 = 2.3.2 + the E1.5 wave (TD-080, MOTOR-LANE-ORDER-E15-DRAMATIK, D-166 B1, EP-sanctioned):
dedicated inherited traits `aggression`/`impulse`/`vindictiveness` drawn from a SECONDARY
mulberry32 stream (`S.rand2`, birth-time only — primary-stream draw order at agent creation is
byte-identical to 2.3.2, so canon souls keep their canon identities); per-rung violence events
`steal`/`raid`/`feud` + `mourn` with `causes[]` chains (R2 grammar) and grudge bookkeeping;
recognized leaders + upward tribute (`leader`/`tribute` events, `village.leader`); named
gift-ways (`giftway` event, `village.giftName`); gate-tuned rates (TUNE.theftRate/.raidRate/
.feudRate); `wealthOf` exported; VERSION honestly bumped to '2.4.0'.
**Histories DIVERGE from 2.3.2 BY DESIGN** (the wave adds RNG consumption in the tick path —
same seed still ⇒ bit-identical rerun, proven by double-boot node runs at t8640 on all four
seeds). Engine SHA-256:
`7fc4ae8e8e62920bdb5f685253dc88f6fe5287b214d8113ede26956733e955ab`
(live twin: Assets/StreamingAssets/Emergence/emergence-engine.js — the Engine/ copy is the
write-locked 2.0.1 relic per D-093 and is NOT the source; EngineSourcePath prefers the twin).

Regeneration provenance (all 2026-08-09): FÖRE-gate GREEN on 2.3.2 canons 03:06; node
reproduced ALL EIGHT checked-in 2.3.2 canons byte-perfect before re-baseline (t720 + t8640);
post-change Jint gate RED 4/4 (04:2x) with divergent files byte-identical to the node-generated
new t720 canons (cmp OK ×4); t720 canons below = those Jint-divergent/node-identical files;
t8640 canons node-generated (same node = jint parity law, confirmed GREEN by the full-depth
Jint gate afterwards).

| Run | Ticks | Canon SHA-256 |
|---|---|---|
| 97013 | 720 | `a90f3f5df3fce89e8f0e20e789d4eb3ceec900c1b00c43f8136dddc0fc5e2cdc` |
| 4242 | 720 | `14f4c9e0d7c5d4ebffb27a6f763c49034a59b9b180efae04cd6576a4d353c9a8` |
| 20260718 | 720 | `cc5bb8cf784ab6e1fd3ae0bec768316646c0c76c7bac6aa4ad786ce75290c7d2` |
| 4242 + test founders | 720 | `37f1cb272f23c96ef4ccd2f9fe9ba2c19c326d726253566c363888a9dc2b184e` |
| 97013 | 8640 | `dbfe5398c23d9e6f44379e4fb836d88ac767fda5f08e634b3f38e570943cf765` |
| 4242 | 8640 | `aaf6f57780411b29dc8232d506be83ddf1f17dc667764c8633dc47929a48d84c` |
| 20260718 | 8640 | `e3f87046bbbae5a78e1b033589cc9ae241e75b98564cfbfc69b4143cd1de4232` |
| 4242 + test founders | 8640 | `7f3ed82f6f095fa8b9cec0b45f8b87bb054d8866e58e7b79f3a06ce6e0fb15c9` |

Test founders string unchanged (see GoldenMasterRunner.TestFounders; founder inputs may now
additionally carry aggression/impulse/vindictiveness — additive, unused by the test string).

---
Previous baseline (ENGINE 2.3.2, R2 INK1, superseded 2026-08-09): engine SHA `e59b028c…`,
t720 canons `bb73f9a1…`/`f98c6c0a…`/`cbaac800…`/`7263987a…`, t8640 `1124d2b3…`/`d433a780…`/
`f5e694b4…`/`ff8ca35d…` (full values in git history of this file). Earlier 2.0.1→2.3.1
provenance likewise in git history.

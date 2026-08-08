# GOLDENS — ENGINE 2.3.2 baseline (R2 INK1: export verbs + era canon + pathUse + causes[], 2026-08-09)

2.3.2 = 2.3.1 + the R2 wave increment 1 (TD-076, MOTOR-LANE-ORDER-R2-FAS4): read-side
additive only — event `id` + `causes[]` (birth→parents, death→cause, tech→prerequisites,
village→founder; rediscovery→the loss it undoes), `S.pathUse` footfall tally (moveToward),
`verbOf`/`worldEra`/`eraName`/`ERAS` pure functions, `knowledge[*].evId/evLost` substrate.
**History-identical to 2.3.1**: same seed ⇒ byte-identical event stream/agents/DNA modulo
exactly the new fields (proven node-side at t8640 for 97013/4242/20260718/8919; see
Dropbox 20-DESIGN/R2-INK1-GOLDEN-DIFF.md). Engine SHA-256:
`e59b028cd88b06e082d0a2ed92d50f3c9f917b5429c477378f08546ca60b94c1`
(live twin: Assets/StreamingAssets/Emergence/emergence-engine.js — the Engine/ copy is the
write-locked 2.0.1 relic per D-093 and is NOT the source; EngineSourcePath prefers the twin).

Regeneration provenance: t720 canons written by the in-editor Jint gate (divergent files of
the RED run 2026-08-09 00:02, byte-verified == node harness output); t8640 canons generated
node-side and CONFIRMED GREEN by the in-editor Jint gate 2026-08-09 00:09–01:18 (secs
796/1700/958/665). node = jint parity held byte-for-byte on all eight canons.

| Run | Ticks | Canon SHA-256 |
|---|---|---|
| 97013 | 720 | `bb73f9a1dbf226902e5299e07d2d1bee28a6c733893356770a0826f9e74096e1` |
| 4242 | 720 | `f98c6c0a9367368bd0dc233051a390dce7b0f7bb85516f93bf22a6f5917db24f` |
| 20260718 | 720 | `cbaac800bbe9f57e5f3ad9af28e180810a5bbd1444dd95ad97cbb262bd4ed0d1` |
| 4242 + test founders | 720 | `7263987ac20ac6e5df680b2f0b549ad188ff0ff4fc6abdf224752760c692ede9` |
| 97013 | 8640 | `1124d2b3787712f63bbc1c95a1775f219f3f1ef1ca496d2de9cc69b720c111c0` |
| 4242 | 8640 | `d433a780d8ea76bf904bf4f335e2cdf27afa8bf634b675eac1025072baa804de` |
| 20260718 | 8640 | `f5e694b474b07887711fbb8b1bcbc8ce43ce026b3d387fdac415cc4107c105a7` |
| 4242 + test founders | 8640 | `ff8ca35da91b700716cd27c95d483ce5faba0191869a1d89dc03d04073ae4f51` |

Test founders string unchanged (see GoldenMasterRunner.TestFounders).

---
Previous baseline (ENGINE 2.0.1→2.3.1 era, superseded 2026-08-09): engine SHA
`4f237acffa0cc76e6de29df8abb43db6519705d07b7b8b0439fae45f2dcad18e` (2.3.1), t720 canon SHAs
00919f18… / d9710842… / 34fa5a18… / 3b8b61b2…; the original 2.0.1 manifest text (water fix,
farm sweep) lives in git history of this file.

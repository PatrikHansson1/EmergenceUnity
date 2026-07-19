# GOLDENS — engine 1.2.1 baseline (regenerated 2026-07-19, cloud sandbox)

D-078 prerequisite discharged: browser+Node goldens regenerated against engine
**1.2.1** (D-068 winter fix). Jint (built from source, jint@v3.1.6 +
esprima-dotnet@v3.0.5, .NET 8, hypot prelude) reproduces all three 60-year
seeds bit for bit. Engine SHA-256: `fc6bda1010032c9041021ad0de391eab92f4db71426cee2b94f11340c57ec18e`
(recorded in ../../ENGINE-SHA.txt; 1.2-era goldens SHA `508cfd52…` are retired).

| Seed | Ticks | Canon SHA-256 (browser ≡ node ≡ jint) |
|---|---|---|
| 97013 | 8640 | `ad6ead87f55068955d230fe173703c27b6b745ff1eff72704548e67567b67947` |
| 4242 | 8640 | `59a7bf6892b405caeac798d25c659047151b810e90908d1c2f4eb20fcfb28b73` |
| 20260718 | 8640 | `2c264ad8104ff6682d27d3c9f3b40af26e3d32a6d0e275a9e14037981a7b8a8d` |
| 4242 | 23040 | `3559bdb6da41337e2cb592998b24df16f1f223c27d3d49333a658ea06172e24d` |

Player smoke reference: seed 97013, 1440 ticks → canon SHA-256
`dba53207c7f7e004a322933561de6fcd2fa1fbebdd4c3eb75442036a0b7ef65f`.

**1.2.1 baseline note:** seed 97013 now goes EXTINCT at tick 615 (first winter
reshapes that world — under winterless 1.2 it survived 60y). Per
JINT-GOLDEN-MASTER-PLAN §3 the early-exit is itself part of the compared
output. 4242/60y matches D-068's verification facit exactly (18 alive, 16
techs, 3 harsh winters). Season census per year: 40/44/32/28 (QA law).

Files: `seed-<seed>-t<ticks>.canon.txt` (authoritative, IEEE-754
bit-serialization) + `.readable.json` (diff triage only). The harness that
produced them: `../harness.js` (+ `../prelude-hypot.js` for .NET hosts,
fuzz-verified 2,000,007 samples, 0 mismatches vs native V8).

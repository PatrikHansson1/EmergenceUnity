# GOLDENS — ENGINE 2.0.1 baseline (E1 + THE WATER FIX, 2026-07-19)

2.0.1 = 2.0.0 + the water fix (findNearest could never return water: n>0 guard vs
water's n=0 — fishing was unreachable AND unpracticable since 1.x; found via the
EP's field report, confirmed 0/20 worlds x 120y). Engine SHA-256:
`a06b302b149f1a48efba73c651973e7fd6526576c1a1dcf8077eae33e5b52273`.
2.0.0 codes retired hours after minting (D-081; pre-EA re-baseline per D-078).

| Run | Ticks | Canon SHA-256 (node = browser = jint verified) |
|---|---|---|
| 97013 | 8640 | `8999660d22a1eab2…` |
| 4242 | 8640 | `907c4aa567b4e7ce…` |
| 20260718 | 8640 | `3e3226f17bb7401a…` |
| 4242 + test founders | 8640 | `9ff4e9882c8ec7ac…` |

Player smoke: 97013 x 1440 -> `f8178984d5d959fde267892d2948c15dbfc05956bfc1687a106475e0d8b660a0`.
Farm sweep 20x120y: extinct 0/20 (2.0.0: 3/20; 1.2.1: 8/20), pop 604 (341), fishing alive 20/20 with 629 catches — the water economy was a missing food pillar; survival jump flagged for the E2 balance pass.
Test founders string unchanged (see GoldenMasterRunner.TestFounders).

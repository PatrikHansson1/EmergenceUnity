# Reports/rig/ledger — emissionskontraktet S.ledger (D-649/D-650)
Prereg: Dropbox 20-DESIGN/LEDGER-PREREG-2026-09-01.md (LÅST, sha e9e88508…).
Block 1 (K1 dödsbok + K2 födelsebok + init + helpers): ledger-k1k2.patch mot v19 8907f6f6 (Reports/rig/engine-pace.js).
M-a: golden-cloud 720+8640 alla 8 SHA byte-identiska (2026-09-01 ~10:15Z). M-b: 97013×120 år deadBook 25 = Σdeaths 25, births 161 = stats.births 161.
Nästa block: K11 årssnapshot, K3 uttag, K5 läroakter, K6 handel, K13 kohortdöd — M-a efter varje.

## Block 2 (K11 snapshot, K3 extract/depleted, K5 teach, K6 trade, K13 cohortDeaths) — 2026-09-01 ~10:45Z
Generator: `python3 mkledger.py ../engine-pace.js engine-ledger.js` (alla 14 ankare måste träffa exakt en gång; skriver hela kontraktet block 1+2).
Probe: `node mb-probe.js <engine> <seed> <år> [budgetSek]` (kräver harness/prelude-hypot.js i samma mapp som i golden-cloud). Oberoende tile-räknare: sed `S.tiles[a.ty][a.tx0].n--` → `(globalThis.__TDEC=(globalThis.__TDEC||0)+1,…)` i en probe-kopia (3 träffar).
M-a: golden-cloud 720+8640 alla 8 SHA byte-identiska (tvillingens sha256 börjar 4cc9358c). M-b 97013×120: extract 3459 = tileDec 3459; deadBook 25 = Σdeaths 25; births 161 = 161; trade 301 = stats.trades 301; teach byPair 1896 = byYear 1896; 120 snapshots. K13 ej exercerad (inga aggregat på 97013×120).

## Block 3 (K4 meals, K7 rel, K8 travel, K9 stripped, K10 huts/fires/wood/smelt/tribute, K12 hardship) — 2026-09-01 ~11:00Z (D-653)
mkledger.py = 33 ankare (sha256 473b6c03…) ⇒ motor sha256 f39e0684… (= v20, LIVE 11:08Z). M-a 8/8 grön i moln. M-f lastfritt: 130,7 s → 135,3 s (+3,5 %). M-b hela kontraktet 97013×120 konsistent (meals.stolen räknar bara steal-food; stats.steals även steal-goods).
LIVE-PROTOKOLL: backup Reports/backup/emergence-engine-8907f6f6-pre-ledger.js · kvartett uppdaterad · Fas3SimDriver ExportJs `ledger:S.ledger||{}` · RUN_COMPILE CLEAN 13:08 lokal · RUN_GOLDEN 720 startad 13:09 lokal → sedan 8640 → RUN_LANG → trigger-commit.
`emergence-engine-v20-candidate.js` här är byte-identisk med den installerade motorn (verifieringskopia).

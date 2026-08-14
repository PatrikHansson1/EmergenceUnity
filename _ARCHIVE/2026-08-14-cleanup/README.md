# General cleanup — 2026-08-14 (D-225)

Patrik: "bra om vi rensar och arkiverar de filer som är fel, behövs kanske en rensning rent generellt."

Archived here (moved, never deleted — the VM mount allows rename but not unlink, D-127):

- `object-codex.json.bak` — a stale snapshot of the codex from before the 53-tech rebuild. A `.bak`
  beside a live data file is a second source of truth waiting to be read by mistake.
- 39 `_prev` / `_p2` / `_pre_gate` / `STALE_MARKER` files from `Reports/`. Every probe that reruns
  used to leave its predecessor behind; 224 files had become 185 files of evidence and 39 of
  sediment. The reports directory is where we go to answer "what is true right now", so anything in
  it that is NOT current is a hazard, not a history — the history is in git.

Not archived, deliberately: every current `*_DONE.txt` and report, because they are the live
evidence a probe writes and reads.

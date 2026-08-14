# The 2.0.1 engine relic — archived 2026-08-14 (D-225)

`Assets/Emergence/Engine/emergence-engine.js` was a **write-locked 2.0.1 relic**. Every engine wave
since 2.3 lives only in the StreamingAssets twin, and `EmergenceJintHost.EngineSourcePath` redirects
to StreamingAssets whenever it exists — so this file was **never executed**. `BuildScript.cs` said so
in a comment, and D-172 disarmed the sync that would have overwritten the real engine with it.

**It was archived anyway, because a warning in a file nobody opens does not protect a reader who
greps the folder everyone opens.**

It cost a real mistake to prove that. On 2026-08-14 the Codex was audited against this file and the
audit concluded that *14 of 29 codex entries were gated on techs the engine would never invent*.
That conclusion was false. The relic declares **17 techs**; the engine the game actually runs
declares **53**. The audit was rigorous, reproducible and wrong, because it read the decoy.

| | relic (this file) | the living engine |
|---|---|---|
| path | `Assets/Emergence/Engine/` | `Assets/StreamingAssets/Emergence/` |
| VERSION | 2.0.1 | 2.4.1 |
| techs | 17 | 53 |
| sha256 | `a06b302b…` | `22e2ac05…` |

`ENGINE-SHA.txt` in the Engine folder records the **StreamingAssets** hash, which is correct and is
why the golden master's SHA assert has always passed — it was asserting the right file all along.

Kept in the Engine folder because they ARE live: `harness/harness.js`, `harness/prelude-hypot.js`,
`harness/goldens/`, `ENGINE-SHA.txt`.

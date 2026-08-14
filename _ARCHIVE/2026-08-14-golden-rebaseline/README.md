# Golden re-baseline triage files — 2026-08-14, engine 2.4.1 -> 2.5.0

These are the canonical outputs the 2.5.0 engine produced on the run that went RED against the 2.4.1
goldens. They ARE the new goldens (copied into `Assets/Emergence/Engine/harness/goldens/`), kept here as
the triage record of the moment the line moved: the RED run, the four SHAs, and the confirming GREEN.

The change that moved them: three insights that were defined, required and never observed
(`weightsBalance`, `soundsRing`, `herbsHeal`) got `tryObserve` hooks, and `printpress.pre` stopped
pointing at a tech id that does not exist. `tryObserve` consumes `S.rand`, so the stream moves by
construction. Approved by the EP as part of D-226.

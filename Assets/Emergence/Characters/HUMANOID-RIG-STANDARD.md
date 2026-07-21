# Humanoid Rig Standard — the intake folder (D-107 A1/A3)

**This folder is the rig-standard intake for all people (and later, humanoid-able characters).**

Any model imported here is automatically forced to **Unity Humanoid (Mecanim)** by
`HumanoidRigStandard.cs` (`OnPreprocessModel` → `animationType = Human`, avatar created from the model).

## The rule (A1)
One rig for everything that walks, so a single animation set retargets to every character we buy.
Animation is state-driven by the reconciler and deterministic (hash-picked variant, never sim-RNG).

## The purchase gate (A3)
A character package is **not bought** unless it can map to Humanoid. Drop the candidate here, let the
postprocessor try Humanoid, then run `Emergence/Fas0/Validate Humanoid Rig Standard` — if the avatar
is not `isHuman/isValid`, it fails the gate (before any render-test beside the buildings).

## Scope note
The current Quaternius placeholder villagers live under `Assets/Emergence/Models/characters` and are
**not** governed by this folder — Fas 2 decides whether they are retargeted or replaced by the Väg-1
purchase that lands here.

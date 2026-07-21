# Fas 0 — Fundament & standarder (D-107)

*Emergence · Studio Director · byggfas. Denna mapp gjuter de tvärsgående besluten (D-107 Del A) i kod-fundament innan Fas 1. Ingen fil här öppnar ett designbeslut — den sekvenserar bygget.*

## Vad som ligger här

**Runtime/** (Assembly-CSharp)
- `PresentationEventBus.cs` — den **tomma event-bussen** (A4/A7). Deterministisk, läser-aldrig-skriver. Kanaler: AssetSpawned/Removed/Upgraded, Milestone, AgentActivity, samt **reserverade** Chronicle (Fas 4) och Audio (Fas 6) — så reconcilern aldrig behöver rivas upp senare.
- `ReconcilerSkeleton.cs` — reconciler-**skelett**. Fas 1 gör det levande (läs state → materialisera/avveckla per Codex `when`). Fas 0: äger bussen + kan sända dummy-events för grinden.
- `PerfSampler.cs` — runtime FPS/frametids-sampler (A6). Läser bara.

**Editor/** (Assembly-CSharp-Editor)
- `AssetIntakePass.cs` — **Stage 1-2-passet över hela ägda biblioteket**. Magenta-scan + missing-mats + impostor/billboard-LOD + LOD-för-tunga-meshar + **bounds/scale-tabell**. Jobbar på prefab-asset-nivå (ingen scen-laddning, ingen capture → immun mot capture-flaken D-101f). Skriver `Reports/intake-report.txt` + `.csv`. **Grinden GRÖN = magenta=0 över hela biblioteket.**
- `AssetIntakeRunner.cs` — headless-runner för ovan. Trigger: `Reports/RUN_INTAKE_ALL.trigger` → `INTAKE_ALL_DONE.txt`.
- `HumanoidRigStandard.cs` — **A1/A3-grinden.** `AssetPostprocessor` som tvingar Humanoid på allt som importeras i `Assets/Emergence/Characters/` (Fas 2-köpet landar där); validator rapporterar rigg-status. Placeholder-villagers under `Models/characters` lämnas orörda.
- `PerfHarness.cs` — **A6-budgeten** (provisorisk) + statisk scen-census (renderare, unika material, trianglar, LODGroups, agenter). Skriver `Reports/perf-report.txt`. Fas 1 kalibrerar siffrorna på en riktig by.
- `Fas0Grind.cs` — **kör hela Fas 0-grinden i ett svep** (alla fyra + event-buss-självtest). Meny: `Emergence/Fas0/RUN FAS 0 GRIND (all)`. Headless: `Reports/RUN_FAS0.trigger` → `FAS0_DONE.txt`.

## Det ENDA som krävs av dig (Patrik)

1. **Ett Unity-fönsterklick** för att kompilera de nya scripten (ny .cs kräver fokus även med Auto Refresh på). Kolla att Console är felfri.
2. **Ögonkolla att Auto Refresh är PÅ** — Edit ▸ Preferences ▸ General (eller Project Settings ▸ Editor) ▸ *Enabled Outside Playmode* + *Directory Monitoring*. (Går ej att verifiera från disk — det är en maskin-lokal EditorPref.)
3. Kör grinden: meny `Emergence/Fas0/RUN FAS 0 GRIND (all)` **eller** låt mig droppa `Reports/RUN_FAS0.trigger` via bryggan. Läs `Reports/FAS0_DONE.txt`.

Grinden är GRÖN när `intake-report.txt` visar magenta=0 över hela biblioteket. Då öppnar Fas 1 (live reconciler + Codex fill-pass).

## Vad detta MEDVETET inte gör
- Rör inte engine/determinism (guldmastern orörd; baslinjen är GRÖN per `Reports/golden-report.txt` 2026-07-20).
- Tvingar inte om de befintliga placeholder-karaktärerna till Humanoid (Fas 2-beslut).
- Instansierar/renderar inget (intake-passet läser prefab-data → snabbt och flak-fritt).

# BREDD-SESSIONSKIT — lek med större/märkligare världar (utan att röra motorn)
Syfte: känna efter två bredd-rattar *innan* något beslut. Ren utforskning.

## Säkerhet (läs först)
- `engine-sandbox.js` är en **slängkopia** av v24 med EN enda extra rad (band-ratten), byte-bevisat **inert vid skala 1**.
- Din **låsta motor (e2285e55) rörs ALDRIG**. Att köra det här kitet **ändrar ingenting och öppnar inget lås** — det bara simulerar och skriver ut siffror.
- Motorlås D, kanon och måltal är orörda. Först *om* en inställning känns rätt blir det ett medvetet lås D-beslut att bygga in den på riktigt — kitet självt bygger inget.

## Så kör du (PowerShell)
```
cd C:\Dev\EmergenceUnity\Reports\rig\breadth-kit
node breadth-run.js compare 0 500
```
eller `.\run-breadth.ps1`. (Kräver Node — samma som M5-bakningarna.)

## De två rattarna (ingen motoränding — sätts vid körning)
- **CAPSCALE** = band-multiplikator. Högre → högre bärkraftstak → *större* världar. (Väg a.)
- **AGGT** = stads-tröskel. Lägre → städer bildas vid lägre folkmängd → *fler, mindre* städer = *spritt*. (Väg b — motorn stödjer denna override inbyggt.)

## Lägen
- `compare <fröidx> <år>` — EN värld under 4 förinställningar sida-vid-sida: baslinje / STÖRRE / SPRITT / STÖRRE+SPRITT.
- `single <capscale> <aggt> <år> <antal> <startidx>` — egna inställningar över flera frön.

## Vad du tittar efter (kolumnerna)
- **tot** = individer + stadskohort (världens verkliga folkmängd). Växer den med STÖRRE?
- **stad** = antal städer. Blir de fler med SPRITT?
- **svalt** = svältdödsfall. ⚠️ Skjuter den i höjden med STÖRRE? Då är det **kollapsrisken** (kanon: forcerad koncentration §32 gav 10/32 utdöda världar, D-531) — och då är SPRITT (b) troligen räddningen: sprider folket så en dålig klimatepok tar en stad, inte hela världen.
- **era / kn / comp** = hur långt/rikt världen når (om du kör längre horisont).

## Förslag på lek (~en kvart)
1. `compare 0 500`, sen `compare 5 500`, `compare 12 500` — olika frön. Ser du att STÖRRE ökar `tot` men driver upp `svalt`, medan SPRITT håller svälten nere?
2. Prova `single 4 30 500 6 0` — sex världar med STÖRRE+SPRITT. Känns de mer levande?
3. Vill du se om större världar når *längre* (era 10): höj horisonten (t.ex. `compare 0 1500`) — tar längre tid.

## Efter leken
Säg vilken känsla som vann (större? spritt? båda? ingen?), så hjälper jag skriva en låst prereg + bygga in ratten på riktigt när du väljer att öppna lås D. Facit och denna beskrivning: DECISIONS.md ## D-783.

## Tillägg 2026-09-17 — gren-sandlådorna (A2) och lås-fönstrets kandidat
- `engine-branch2.js` (fa052832): A2 = `excludes`-mekanik + atompower⟂computing. FORK BEKRÄFTAD på frö 947511545 (idx 68): atompower 918 → rocketry 1090, computing spärrad.
- `engine-branch3.js` (b0a75619): som branch2 + **spaceflight om-föräldrad** till `rocketry+electricity` (var ouppnåelig under forken). Två destinationer: rymd (atompower→rocketry→spaceflight) vs tänkande maskin (computing→ai).
- `engine-window.js` (270643b0): **hela lås-D-fönstrets kandidat** = branch3 + krönikefix (gille-event får guild+causes) + steg-2-rattar `globalThis.__RARITY[id]` (chansmultiplikator per teknik, b2a) och `globalThis.__MOG[id]` (mognadsår på förkunskaper, b2b). Rattarna är **inerta utan flagga** (byte-identiskt utfall verifierat y40).
- Nya lägen i `breadth-run.js`: `pick <år> <idx ...>` (lägger till i breadth-pick.txt) · `rarity <a|b> <värde> <år> <idx ...>` (kräver ENGINE=engine-window.js; lägger till i breadth-rarity.txt).
- Startare: `.\window-run.ps1 pick 1200 94 11 14 68` · `.\window-run.ps1 rarity a 0.25 1200 68 94 11 14` · `.\window-run.ps1 rarity b 400 1200 68 94 11 14`.
- Mät: `python3 20-DESIGN/divergence-metric.py breadth-kit/breadth-pick.txt --frontier` (i molnet — python saknas på PATH).
Kända snabba era-9-frön (M5): idx 68, 94, 11, 14, 44, 38. Kanon e2285e55 fortfarande ORÖRD av allt ovan.

# window-run.ps1 — HELA las-D-fonstrets kandidat i EN sandlada: engine-window.js
#   = A2 (atompower<->computing) + spaceflight om-foraldrad (rocketry+electricity) + kronik-fix (gille: vem/varfor)
#   + steg-2-rattar (inerta utan flagga): __RARITY.computing (b2a) / __MOG.computing (b2b). Kanon e2285e55 ORORD.
# Exempel:
#   .\window-run.ps1 pick 1200 94 11 14 68              # A2-fork pa kanda era-9-fron (som branch3)
#   .\window-run.ps1 rarity a 0.25 1200 68 94 11 14     # b2a: computing-forsok far 25 % chans
#   .\window-run.ps1 rarity b 400 1200 68 94 11 14      # b2b: computing kraver 400 ar mogna forkunskaper
Set-Location $PSScriptRoot
$env:ENGINE = "engine-window.js"
if ($args.Count -eq 0) { node breadth-run.js pick 1200 68 94 11 14 } else { node breadth-run.js @args }
$env:ENGINE = $null

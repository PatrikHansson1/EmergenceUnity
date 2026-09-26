# branch3-run.ps1 — A2 (atompower <-> computing) + spaceflight om-foraldrad till rocketry+electricity (engine-branch3.js).
# Tva destinationer: atompower->rocketry->spaceflight  vs  computing->ai. Se frontier-kolumnerna / breadth-pick.txt.
# Hemkomst-kommando (3 kanda snabba era-9-fron, ~30 min/varld):  .\branch3-run.ps1 pick 1200 94 11 14 68
Set-Location $PSScriptRoot
$env:ENGINE = "engine-branch3.js"
if ($args.Count -eq 0) { node breadth-run.js single 1 70 1500 15 0 } else { node breadth-run.js @args }
$env:ENGINE = $null

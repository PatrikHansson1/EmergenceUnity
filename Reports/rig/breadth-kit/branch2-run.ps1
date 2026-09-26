# branch2-run.ps1 — kor A2 (senspels-gren: atompower <-> computing) med engine-branch2.js.
# Delar frontlinjen i rymd/kraft-vag vs tankande-maskin-vag. Ser i frontier-kolumnerna.
Set-Location $PSScriptRoot
$env:ENGINE = "engine-branch2.js"
if ($args.Count -eq 0) { node breadth-run.js single 1 70 1500 15 0 } else { node breadth-run.js @args }
$env:ENGINE = $null

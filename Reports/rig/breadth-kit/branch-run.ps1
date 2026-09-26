# branch-run.ps1 — kor A (senspels-gren, ai<->spaceflight demo) med engine-branch.js.
# Exempel: .\branch-run.ps1 single 1 70 1500 15 0   (15 varldar, se ai/sf-split i sista kolumnerna)
Set-Location $PSScriptRoot
$env:ENGINE = "engine-branch.js"
if ($args.Count -eq 0) { node breadth-run.js single 1 70 1500 15 0 } else { node breadth-run.js @args }
$env:ENGINE = $null

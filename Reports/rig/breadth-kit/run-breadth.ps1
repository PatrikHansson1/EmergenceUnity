# run-breadth.ps1 — starta bredd-leken. Kor fran rig\breadth-kit.
# Exempel:  .\run-breadth.ps1              (jamforelse, fro-idx 0, 500 ar)
#           .\run-breadth.ps1 compare 3 500
#           .\run-breadth.ps1 single 4 30 500 4 0
Set-Location $PSScriptRoot
if ($args.Count -eq 0) { node breadth-run.js compare 0 500 }
else { node breadth-run.js @args }

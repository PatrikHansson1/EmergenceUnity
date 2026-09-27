# steg0-och-vakt.ps1 — Emergence (D-874 F13 / D-875 "Kvar pa Patrik" 1-2), korsstartad av Patrik via Claude 2026-09-26.
# 1) Steg 0-stadning: FLYTTAR (raderar inte) dubbelpaketen ut ur Unity-projektet till ett arkiv utanfor projektet,
#    sa Unity slutar importera 2,8 GB .unitypackage-arkiv. Patrik raderar arkivmappen nar han vill.
# 2) Startar om bak-vakten local-node-runner.ps1 i ett eget fonster (lamna det oppet).
$ErrorActionPreference = "Continue"
$arch = "C:\Dev\_Emergence_steg0_arkiv_2026-09-26"
$log  = "C:\Dev\EmergenceUnity\Reports\STEG0_DONE.txt"
New-Item -ItemType Directory -Force -Path $arch | Out-Null
"STEG0 start $((Get-Date).ToUniversalTime().ToString('o'))" | Set-Content $log
$moves = @(
  @{ src = "C:\Dev\EmergenceUnity\Assets\FANTASTIC - Village Pack";        dst = "$arch\FANTASTIC - Village Pack" },
  @{ src = "C:\Dev\EmergenceUnity\Assets\FANTASTIC - Village Pack.meta";   dst = "$arch\FANTASTIC - Village Pack.meta" },
  @{ src = "C:\Dev\EmergenceUnity\Assets\FANTASTIC - City Pack";           dst = "$arch\FANTASTIC - City Pack" },
  @{ src = "C:\Dev\EmergenceUnity\Assets\FANTASTIC - City Pack.meta";      dst = "$arch\FANTASTIC - City Pack.meta" },
  @{ src = "C:\Dev\EmergenceUnity\Assets\FANTASTIC - Nature Pack\Standard_FANTASTIC_Nature_Pack_U6.unitypackage";      dst = "$arch\Standard_FANTASTIC_Nature_Pack_U6.unitypackage" },
  @{ src = "C:\Dev\EmergenceUnity\Assets\FANTASTIC - Nature Pack\Standard_FANTASTIC_Nature_Pack_U6.unitypackage.meta"; dst = "$arch\Standard_FANTASTIC_Nature_Pack_U6.unitypackage.meta" },
  @{ src = "C:\Dev\EmergenceUnity\Assets\FlatKit\[Render Pipeline] Built-In.unitypackage";      dst = "$arch\FlatKit [Render Pipeline] Built-In.unitypackage" },
  @{ src = "C:\Dev\EmergenceUnity\Assets\FlatKit\[Render Pipeline] Built-In.unitypackage.meta"; dst = "$arch\FlatKit [Render Pipeline] Built-In.unitypackage.meta" }
)
foreach ($m in $moves) {
  if (Test-Path -LiteralPath $m.src) {
    try { Move-Item -LiteralPath $m.src -Destination $m.dst -Force; "MOVED  $($m.src) -> $($m.dst)" | Add-Content $log }
    catch { "ERROR  $($m.src): $($_.Exception.Message)" | Add-Content $log }
  } else { "ABSENT $($m.src)" | Add-Content $log }
}
$size = (Get-ChildItem -LiteralPath $arch -Recurse -File | Measure-Object -Property Length -Sum).Sum
"ARKIV $arch  bytes=$size" | Add-Content $log
# 2) bak-vakten
Start-Process powershell -ArgumentList '-NoExit','-ExecutionPolicy','Bypass','-File','C:\Dev\EmergenceUnity\Reports\rig\bake\local-node-runner.ps1'
"VAKT startad $((Get-Date).ToUniversalTime().ToString('o'))" | Add-Content $log
"DONE" | Add-Content $log

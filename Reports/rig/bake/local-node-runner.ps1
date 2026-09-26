# local-node-runner.ps1 — Emergence lokal bak-vakt (D-776).
# Syfte: Unity-editorns automatiska node-start gick sonder med Windows-uppdateringen 8 sep.
# Denna vakt startas EN gang av Patrik och kor tills fonstret stangs. Den pollar en trigger-fil
# som Claude skriver via filbryggan och startar node-bakningar sjalv — sa Patrik slipper godkanna
# varje korning. Sakerhet: kor ENDAST 'node <vitlistade args>' (script + siffror/bindestreck/punkt)
# eller raden 'STOP' (dodar node). Inget annat kommando kan koras. Confined till rig/bake.
$ErrorActionPreference = "SilentlyContinue"
$bake  = "C:\Dev\EmergenceUnity\Reports\rig\bake"
$trig  = Join-Path $bake "RUN_LOCALNODE.trigger"
$alive = Join-Path $bake "local-runner-alive.txt"
$log   = Join-Path $bake "local-runner.log"
Write-Host "==================================================================="
Write-Host " Emergence lokal bak-vakt IGANG."
Write-Host " Pollar RUN_LOCALNODE.trigger var 3:e sekund i rig\bake."
Write-Host " LAMNA DETTA FONSTER OPPET (minimera garna). Ctrl+C stoppar."
Write-Host "==================================================================="
Add-Content $log "$((Get-Date).ToUniversalTime().ToString('o')) RUNNER START"
while ($true) {
  try {
    Set-Content -Path $alive -Value ((Get-Date).ToUniversalTime().ToString('o'))
    if (Test-Path $trig) {
      $lines = Get-Content $trig
      Remove-Item $trig -Force
      foreach ($line in $lines) {
        $a = ($line -replace '\s+$','').Trim()
        if ($a -eq "") { continue }
        if ($a -eq "STOP") {
          Get-Process node -ErrorAction SilentlyContinue | Stop-Process -Force
          Add-Content $log "$((Get-Date).ToUniversalTime().ToString('o')) STOP (dodade node)"
          Write-Host "[STOP] dodade node-processer"
          continue
        }
        if ($a.Length -le 200 -and $a -match '^[A-Za-z0-9_\-\. ]+$') {
          Start-Process node -ArgumentList $a -WorkingDirectory $bake -WindowStyle Hidden
          Add-Content $log "$((Get-Date).ToUniversalTime().ToString('o')) START node $a"
          Write-Host "[START] node $a"
        } else {
          Add-Content $log "$((Get-Date).ToUniversalTime().ToString('o')) REFUSED $a"
          Write-Host "[REFUSED] $a"
        }
      }
    }
  } catch {
    Add-Content $log "$((Get-Date).ToUniversalTime().ToString('o')) ERR $($_.Exception.Message)"
  }
  Start-Sleep -Seconds 3
}

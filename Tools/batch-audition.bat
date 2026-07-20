@echo off
rem EMERGENCE — headless audition/capture (TD-037, the accelerator).
rem Spawned detached from Emergence/Build/Queue Batchmode Audition, or run directly when the
rem editor is closed. Runs Unity in batchmode WITH graphics (no -nographics — the camera must
rem render) and executes AuditionRunner.BatchAll, which dresses + captures every key shot to the
rem Dropbox evidence dir (auto-syncs). Zero computer-use per capture cycle.
setlocal
set PROJECT=C:\Dev\EmergenceUnity
set UNITY="C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe"
set LOG=%PROJECT%\Builds\batch-audition.log
if not exist "%PROJECT%\Builds" mkdir "%PROJECT%\Builds"
echo %date% %time% audition queue start > "%LOG%"

:waitlock
if exist "%PROJECT%\Temp\UnityLockfile" (
  echo %date% %time% waiting for editor to release the project lock... >> "%LOG%"
  ping -n 6 127.0.0.1 > nul
  goto waitlock
)

echo %date% %time% lock free - starting batchmode audition (with graphics) >> "%LOG%"
%UNITY% -batchmode -projectPath "%PROJECT%" -executeMethod Emergence.Editor.AuditionRunner.BatchAll -logFile "%PROJECT%\Builds\audition.log"
set RC=%ERRORLEVEL%
echo %date% %time% batchmode audition exit code %RC% >> "%LOG%"
echo audition=%RC% > "%PROJECT%\Builds\audition-result.txt"
endlocal

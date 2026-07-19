@echo off
rem EMERGENCE — queued batchmode build + player golden smoke (BUILD-AUTOMATION §2).
rem Spawned detached from the editor menu (Emergence/Build/Queue Batchmode Build);
rem waits for the editor to release the project lock, then builds headless and
rem runs the player smoke. All results land in Builds\.
setlocal
set PROJECT=C:\Dev\EmergenceUnity
set UNITY="C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe"
set LOG=%PROJECT%\Builds\batch-queue.log
if not exist "%PROJECT%\Builds" mkdir "%PROJECT%\Builds"
echo %date% %time% queue start > "%LOG%"

:waitlock
if exist "%PROJECT%\Temp\UnityLockfile" (
  echo %date% %time% waiting for editor to quit... >> "%LOG%"
  timeout /t 5 /nobreak > nul
  goto waitlock
)

echo %date% %time% lock free - starting batchmode build >> "%LOG%"
%UNITY% -batchmode -nographics -quit -projectPath "%PROJECT%" -executeMethod Emergence.Editor.BuildScript.BuildWindows -logFile "%PROJECT%\Builds\build.log"
set BUILDRC=%ERRORLEVEL%
echo %date% %time% batchmode exit code %BUILDRC% >> "%LOG%"
if not "%BUILDRC%"=="0" goto done

echo %date% %time% running player golden smoke >> "%LOG%"
if exist "%PROJECT%\Builds\EmergenceUnity\smoke-result.txt" del "%PROJECT%\Builds\EmergenceUnity\smoke-result.txt"
"%PROJECT%\Builds\EmergenceUnity\EmergenceUnity.exe" -batchmode -nographics -goldenSmoke
set SMOKERC=%ERRORLEVEL%
echo %date% %time% player smoke exit code %SMOKERC% >> "%LOG%"

:done
echo %date% %time% queue done build=%BUILDRC% smoke=%SMOKERC% >> "%LOG%"
echo build=%BUILDRC% smoke=%SMOKERC% > "%PROJECT%\Builds\queue-result.txt"
endlocal

rem --- post-build: push to GitHub (git installed on Windows 2026-07-19) ---
cd /d "%PROJECT%"
git push origin main >> "%LOG%" 2>&1
echo %date% %time% git push exit %ERRORLEVEL% >> "%LOG%"

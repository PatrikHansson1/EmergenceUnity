@echo off
rem Emergence steg 0-stadning + omstart av bak-vakten (D-875). Dubbelklicka. Loggar till Reports\STEG0_DONE.txt.
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Dev\EmergenceUnity\Reports\rig\bake\steg0-och-vakt.ps1"

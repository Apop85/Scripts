@echo off
rem STEFFLIX-App fuer Android bauen (Doppelklick). Ergebnis: STEFFLIX.apk im Release-Ordner.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
pause

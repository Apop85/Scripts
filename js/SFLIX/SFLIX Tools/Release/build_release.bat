@echo off
REM ===================================================================
REM  STEFFLIX - Release bauen und bereitstellen (Doppelklick genuegt)
REM
REM  Liegt in <Release-Ordner>\SFLIX Tools\Release und arbeitet auf dem Release-Ordner
REM  zwei Ebenen darueber. Startet release.ps1. Ablauf mit Rueckfragen:
REM   1. Updater    bei geaendertem Quellcode: Version hochzaehlen (aenderbar),
REM                 updateStefflix.exe in "SFLIX Tools\SflixTools" bauen und in den
REM                 Release-Ordner kopieren
REM   2. ffmpeg     auf Wunsch ffmpeg.json auf die neueste stabile Version setzen
REM   3. STEFFLIX   auf Wunsch version.js erhoehen und Changelog in news.txt ergaenzen
REM   4. Pruefen    check_release.py, create_news.py, create_checksums.py
REM   5. Uebersicht welche Dateien gepusht werden muessen
REM
REM  Optionen (werden an release.ps1 weitergegeben):
REM   build_release.bat -Auto           alle Vorschlaege ohne Rueckfrage uebernehmen
REM   build_release.bat -MarkReleased   aktuellen Updater-Quellcode als bereitgestellt merken
REM
REM  Benoetigt: Python mit requests (pip install requests), .NET Framework 4.x
REM ===================================================================
setlocal
cd /d "%~dp0"
title STEFFLIX - Release bauen
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0release.ps1" %*
set "CODE=%ERRORLEVEL%"
echo.
pause
exit /b %CODE%

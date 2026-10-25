@echo off
REM ===================================================================
REM  STEFFLIX - Release vorbereiten (Doppelklick genuegt)
REM
REM  1. Pruefen       check_release.py    (Version, Dateien, start.html, updateStefflix.exe, ffmpeg.json)
REM  2. Changelog     create_news.py      (news.txt -> news.json)
REM  3. Pruefsummen   create_checksums.py (checksums.json)
REM  4. Aufraeumen    __pycache__
REM
REM  Programme werden hier nicht mehr gebaut:
REM   - updateStefflix.exe (STEFFLIX Updater ab 3.0) wird in "SFLIX Tools\SflixTools" mit
REM     build.ps1 gebaut und mit tools\publish.ps1 -Repo <dieser Ordner> hierher kopiert
REM     (zusammen mit ffmpeg.json).
REM   - updater.exe, updateData.exe und setup.exe sind eingefroren. Sie werden nur noch fuer
REM     den Umstieg aelterer Installationen bereitgestellt und nicht mehr veraendert.
REM
REM  Danach alle geaenderten Dateien zusammen pushen, vor allem updateStefflix.exe
REM  und checksums.json: passen sie nicht zusammen, verweigern die Updater
REM  die Installation.
REM
REM  Benoetigtes Paket: pip install requests (wird von sflix_update.py importiert)
REM ===================================================================
setlocal
cd /d "%~dp0"
title STEFFLIX - Release vorbereiten

echo.
echo [1/4] Pruefe Release...
python check_release.py
if errorlevel 1 goto :fehler

echo.
echo [2/4] Erzeuge news.json aus news.txt...
python create_news.py
if errorlevel 1 goto :fehler

echo.
echo [3/4] Erzeuge checksums.json...
python create_checksums.py
if errorlevel 1 goto :fehler

echo.
echo [4/4] Raeume auf...
call :aufraeumen

echo.
echo ===================================================================
echo  FERTIG - Release bereit zum Pushen.
echo ===================================================================
echo.
pause
exit /b 0


:aufraeumen
if exist build rmdir /S /Q build
if exist dist rmdir /S /Q dist
if exist __pycache__ rmdir /S /Q __pycache__
del /Q *.spec 2>nul
exit /b 0


:fehler
call :aufraeumen
echo.
echo ===================================================================
echo  ABBRUCH - das Release ist NICHT fertig. Bitte Meldung oben pruefen.
echo ===================================================================
echo.
pause
exit /b 1

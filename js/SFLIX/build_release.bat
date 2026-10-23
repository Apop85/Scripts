@echo off
REM ===================================================================
REM  STEFFLIX - Release bauen (Doppelklick genuegt)
REM
REM  1. Pruefen       check_release.py   (Version, Dateien, start.html)
REM  2. Changelog     create_news.py     (news.txt -> news.json)
REM  3. Programme     PyInstaller        (updateData, updateStefflix, updater, setup)
REM  4. Pruefsummen   create_checksums.py (checksums.json, nach den Programmen!)
REM  5. Aufraeumen    build-Ordner, __pycache__
REM
REM  Danach alle geaenderten Dateien zusammen pushen, vor allem die .exe-Dateien
REM  und checksums.json: passen sie nicht zusammen, verweigern die Updater
REM  die Installation.
REM
REM  Optional: "build_release.bat ohne-exe" fuehrt alles ausser Schritt 3 aus.
REM
REM  Benoetigte Pakete: pip install pyinstaller requests pillow
REM ===================================================================
setlocal
cd /d "%~dp0"
title STEFFLIX - Release bauen

set "SKIP_EXE="
if /i "%~1"=="ohne-exe" set "SKIP_EXE=1"

REM updateData braucht von Pillow nur JPG/PNG/GIF/WebP/BMP. AVIF, Schriften, Farbprofile,
REM numpy und tkinter werden ausgeschlossen (spart ca. 15 MB). AVIF-Vorschaubilder
REM werden deshalb in Originalgroesse verwendet.
set "PILLOW_EXCLUDES=--exclude-module numpy --exclude-module tkinter --exclude-module PIL._avif --exclude-module PIL.AvifImagePlugin --exclude-module PIL._imagingft --exclude-module PIL.ImageFont --exclude-module PIL._imagingcms --exclude-module PIL.ImageCms --exclude-module PIL.ImageTk --exclude-module PIL.ImageQt --exclude-module PIL.ImageShow"

echo.
echo [1/5] Pruefe Release...
python check_release.py
if errorlevel 1 goto :fehler

echo.
echo [2/5] Erzeuge news.json aus news.txt...
python create_news.py
if errorlevel 1 goto :fehler

echo.
if defined SKIP_EXE (
    echo [3/5] Programme werden nicht gebaut ^(ohne-exe^).
    goto :pruefsummen
)
echo [3/5] Baue Programme ^(dauert einige Minuten^)...
python -c "import PyInstaller, requests, PIL" 2>nul
if errorlevel 1 (
    echo PyInstaller, requests oder Pillow fehlt: pip install pyinstaller requests pillow
    goto :fehler
)
call :baue getfilepath.py updateData.exe "%PILLOW_EXCLUDES%"
if errorlevel 1 goto :fehler
call :baue updateStefflix.py updateStefflix.exe ""
if errorlevel 1 goto :fehler
call :baue updater.py updater.exe ""
if errorlevel 1 goto :fehler
call :baue setup.py setup.exe ""
if errorlevel 1 goto :fehler

:pruefsummen
echo.
echo [4/5] Erzeuge checksums.json...
python create_checksums.py
if errorlevel 1 goto :fehler

echo.
echo [5/5] Raeume auf...
call :aufraeumen

echo.
echo ===================================================================
echo  FERTIG - Release bereit zum Pushen.
if defined SKIP_EXE echo  Achtung: Programme wurden NICHT neu gebaut ^(ohne-exe^).
echo ===================================================================
echo.
pause
exit /b 0


REM -------------------------------------------------------------------
REM  :baue Quelldatei Zieldatei "zusaetzliche Optionen"
REM  Die bestehende .exe wird erst ersetzt, wenn der Build erfolgreich war.
REM -------------------------------------------------------------------
:baue
echo    %~2 ...
pyinstaller --noconfirm --log-level WARN --paths C:\Windows\System32\downlevel --console --onefile --distpath build\dist --workpath build\work --specpath build %~3 "%~1"
if errorlevel 1 (
    echo    Build von %~2 fehlgeschlagen.
    exit /b 1
)
copy /Y "build\dist\%~n1.exe" "%~2" >nul
if errorlevel 1 (
    echo    %~2 konnte nicht ersetzt werden ^(laeuft das Programm noch?^).
    exit /b 1
)
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

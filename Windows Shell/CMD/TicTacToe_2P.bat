@echo off
TITLE _____-----TIC-----TAC-----TOE-----_____
mode con lines=30 cols=60
chcp 1252
COLOR 1F
set p1=0
set p2=0
set "SPACE=  "
set "SPACE1= "
cls
echo _-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-
echo                 TIC       TAC        TOE
echo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo.
echo.
echo.
echo.
echo Spieler 1 Bitte gib deinen Namen ein:
set /p s1=
echo.
echo Spieler 2 Bitte gib deinen Namen ein:
set /p s2=
echo.
goto PRESET

:PRESET
cls
echo _-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-
echo                 TIC       TAC        TOE
echo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
set "a1=."
set "a2=."
set "a3=."
set "a4=."
set "a5=."
set "a6=."
set "a7=."
set "a8=."
set "a9=."
set axa=X
set aoa=O
set rd=0
set a1x=%a1% & set "a1xr=%a1% "
set a2x=%a2% & set "a2xr=%a2% "
set a3x=%a3% & set "a3xr=%a3% "
set a4x=%a4% & set "a4xr=%a4% "
set a5x=%a5% & set "a5xr=%a5% "
set a6x=%a6% & set "a6xr=%a6% "
set a7x=%a7% & set "a7xr=%a7% "
set a8x=%a8% & set "a8xr=%a8% "
set a9x=%a9% & set "a9xr=%a9% "
set a1o=%a1% & set "a1om=%a1% "
set a2o=%a2% & set "a2om=%a2% "
set a3o=%a3% & set "a3om=%a3% "
set a4o=%a4% & set "a4om=%a4% "
set a5o=%a5% & set "a5om=%a5% "
set a6o=%a6% & set "a6om=%a6% "
set a7o=%a7% & set "a7om=%a7% "
set a8o=%a8% & set "a8om=%a8% "
set a9o=%a9% & set "a9om=%a9% "

echo.
echo.
echo.
echo.
echo Wer soll beginnen? X oder O?
set /p ausw=

if %ausw% == o goto SPIELER2
if %ausw% == x goto SPIELER1
goto PRESET

:SPIELER1
cls
echo _-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-
echo                 TIC       TAC        TOE
echo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo		%s1%		%p1%:%p2%		%s2% 
echo.
echo                       %s1% wählt
echo. 
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo              I%a1x% %a1o% %a1xr%I%a2x% %a2o% %a2x%I%a3x% %a3o% %a3xr%I
echo    X   X     I%a1om% %a1x% %a1o%I%a2om% %a2x% %a2o%I%a3om% %a3x% %a3o%I
echo     X X      I%a1x% %a1o% %a1xr%I%a2x% %a2o% %a2x%I%a3x% %a3o% %a3xr%I	
echo      X       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~	1 I 2 I 3
echo     X X      I%a4x% %a4o% %a4xr%I%a5x% %a5o% %a5xr%I%a6x% %a6o% %a6xr%I	---------
echo    X   X     I%a4om% %a4x% %a4o%I%a5om% %a5x% %a5o%I%a6om% %a6x% %a6o%I	4 I 5 I 6
echo              I%a4x% %a4o% %a4xr%I%a5x% %a5o% %a5xr%I%a6x% %a6o% %a6xr%I	---------
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~	7 I 8 I 9
echo              I%a7x% %a7o% %a7xr%I%a8x% %a8o% %a8xr%I%a9x% %a9o% %a9xr%I
echo              I%a7om% %a7x% %a7o%I%a8om% %a8x% %a8o%I%a9om% %a9x% %a9o%I
echo              I%a7x% %a7o% %a7xr%I%a8x% %a8o% %a8xr%I%a9x% %a9o% %a9xr%I
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo.
echo                     Runde: %rd%
echo. 
echo                     Wähle dein Feld
set /p choice=                         

if %choice% == d goto DEBUG
if %choice% == 1 if %aoa% == %a1% goto FAIL1
if %choice% == 1 if %axa% == %a1% goto FAIL2
if %choice% == 2 if %aoa% == %a2% goto FAIL1
if %choice% == 2 if %axa% == %a2% goto FAIL2
if %choice% == 3 if %aoa% == %a3% goto FAIL1
if %choice% == 3 if %axa% == %a3% goto FAIL2
if %choice% == 4 if %aoa% == %a4% goto FAIL1
if %choice% == 4 if %axa% == %a4% goto FAIL2
if %choice% == 5 if %aoa% == %a5% goto FAIL1
if %choice% == 5 if %axa% == %a5% goto FAIL2
if %choice% == 6 if %aoa% == %a6% goto FAIL1
if %choice% == 6 if %axa% == %a6% goto FAIL2
if %choice% == 7 if %aoa% == %a7% goto FAIL1
if %choice% == 7 if %axa% == %a7% goto FAIL2
if %choice% == 8 if %aoa% == %a8% goto FAIL1
if %choice% == 8 if %axa% == %a8% goto FAIL2
if %choice% == 9 if %aoa% == %a9% goto FAIL1
if %choice% == 9 if %axa% == %a9% goto FAIL2
if %choice% == 1 set a1=%axa% & goto PRUEX 
if %choice% == 2 set a2=%axa% & goto PRUEX
if %choice% == 3 set a3=%axa% & goto PRUEX
if %choice% == 4 set a4=%axa% & goto PRUEX
if %choice% == 5 set a5=%axa% & goto PRUEX
if %choice% == 6 set a6=%axa% & goto PRUEX
if %choice% == 7 set a7=%axa% & goto PRUEX
if %choice% == 8 set a8=%axa% & goto PRUEX
if %choice% == 9 set a9=%axa% & goto PRUEX
if %choice% == x goto START
goto SPIELER1

:PRUEX
if %a1%==X set a1o=%SPACE1% & set a1x=%axa% & set "a1xr=%axa% " & set "a1om=%SPACE%"
if %a2%==X set a2o=%SPACE1% & set a2x=%axa% & set "a2xr=%axa% " & set "a2om=%SPACE%"
if %a3%==X set a3o=%SPACE1% & set a3x=%axa% & set "a3xr=%axa% " & set "a3om=%SPACE%"
if %a4%==X set a4o=%SPACE1% & set a4x=%axa% & set "a4xr=%axa% " & set "a4om=%SPACE%"
if %a5%==X set a5o=%SPACE1% & set a5x=%axa% & set "a5xr=%axa% " & set "a5om=%SPACE%"
if %a6%==X set a6o=%SPACE1% & set a6x=%axa% & set "a6xr=%axa% " & set "a6om=%SPACE%"
if %a7%==X set a7o=%SPACE1% & set a7x=%axa% & set "a7xr=%axa% " & set "a7om=%SPACE%"
if %a8%==X set a8o=%SPACE1% & set a8x=%axa% & set "a8xr=%axa% " & set "a8om=%SPACE%"
if %a9%==X set a9o=%SPACE1% & set a9x=%axa% & set "a9xr=%axa% " & set "a9om=%SPACE%"

if %a1%==O set a1x=%SPACE1% & set a1o=%aoa% & set "a1xr=%aoa% " & set "a1xr=%SPACE%" & set "a1om=%aoa% "
if %a2%==O set a2x=%SPACE1% & set a2o=%aoa% & set "a2xr=%aoa% " & set "a2xr=%SPACE%" & set "a2om=%aoa% "
if %a3%==O set a3x=%SPACE1% & set a3o=%aoa% & set "a3xr=%aoa% " & set "a3xr=%SPACE%" & set "a3om=%aoa% "
if %a4%==O set a4x=%SPACE1% & set a4o=%aoa% & set "a4xr=%aoa% " & set "a4xr=%SPACE%" & set "a4om=%aoa% "
if %a5%==O set a5x=%SPACE1% & set a5o=%aoa% & set "a5xr=%aoa% " & set "a5xr=%SPACE%" & set "a5om=%aoa% "
if %a6%==O set a6x=%SPACE1% & set a6o=%aoa% & set "a6xr=%aoa% " & set "a6xr=%SPACE%" & set "a6om=%aoa% "
if %a7%==O set a7x=%SPACE1% & set a7o=%aoa% & set "a7xr=%aoa% " & set "a7xr=%SPACE%" & set "a7om=%aoa% "
if %a8%==O set a8x=%SPACE1% & set a8o=%aoa% & set "a8xr=%aoa% " & set "a8xr=%SPACE%" & set "a8om=%aoa% "
if %a9%==O set a9x=%SPACE1% & set a9o=%aoa% & set "a9xr=%aoa% " & set "a9xr=%SPACE%" & set "a9om=%aoa% "


set /a rd=%rd%+1
if %a1% == X if %a2% == %a1% if %a2% == %a3% goto XWIN
if %a4% == X if %a5% == %a4% if %a5% == %a6% goto XWIN
if %a7% == X if %a8% == %a7% if %a8% == %a9% goto XWIN
if %a1% == X if %a4% == %a1% if %a4% == %a7% goto XWIN
if %a2% == X if %a5% == %a2% if %a5% == %a8% goto XWIN
if %a3% == X if %a6% == %a3% if %a6% == %a9% goto XWIN
if %a1% == X if %a5% == %a1% if %a5% == %a9% goto XWIN
if %a7% == X if %a5% == %a7% if %a5% == %a3% goto XWIN
if %rd% == 9 goto PAR
goto SPIELER2

:SPIELER2
cls
echo _-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-
echo                 TIC       TAC        TOE
echo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo		%s1%		%p1%:%p2%		%s2% 
echo.
echo                       %s2% wählt
echo. 
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo              I%a1x% %a1o% %a1xr%I%a2x% %a2o% %a2x%I%a3x% %a3o% %a3xr%I
echo     XxX      I%a1om% %a1x% %a1o%I%a2om% %a2x% %a2o%I%a3om% %a3x% %a3o%I
echo    X   X     I%a1x% %a1o% %a1xr%I%a2x% %a2o% %a2x%I%a3x% %a3o% %a3xr%I
echo    X   X     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~	1 I 2 I 3
echo    X   X     I%a4x% %a4o% %a4xr%I%a5x% %a5o% %a5xr%I%a6x% %a6o% %a6xr%I	---------
echo     XXX      I%a4om% %a4x% %a4o%I%a5om% %a5x% %a5o%I%a6om% %a6x% %a6o%I	3 I 4 I 5
echo              I%a4x% %a4o% %a4xr%I%a5x% %a5o% %a5xr%I%a6x% %a6o% %a6xr%I	---------
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~	7 I 8 I 9
echo              I%a7x% %a7o% %a7xr%I%a8x% %a8o% %a8xr%I%a9x% %a9o% %a9xr%I
echo              I%a7om% %a7x% %a7o%I%a8om% %a8x% %a8o%I%a9om% %a9x% %a9o%I
echo              I%a7x% %a7o% %a7xr%I%a8x% %a8o% %a8xr%I%a9x% %a9o% %a9xr%I
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo.
echo                     Runde: %rd%
echo. 
echo                     Wähle dein Feld
set /p choice=                             

if %choice% == d goto DEBUG
if %choice% == 1 if %aoa% == %a1% goto FAIL3
if %choice% == 1 if %axa% == %a1% goto FAIL4
if %choice% == 2 if %aoa% == %a2% goto FAIL3
if %choice% == 2 if %axa% == %a2% goto FAIL4
if %choice% == 3 if %aoa% == %a3% goto FAIL3
if %choice% == 3 if %axa% == %a3% goto FAIL4
if %choice% == 4 if %aoa% == %a4% goto FAIL3
if %choice% == 4 if %axa% == %a4% goto FAIL4
if %choice% == 5 if %aoa% == %a5% goto FAIL3
if %choice% == 5 if %axa% == %a5% goto FAIL4
if %choice% == 6 if %aoa% == %a6% goto FAIL3
if %choice% == 6 if %axa% == %a6% goto FAIL4
if %choice% == 7 if %aoa% == %a7% goto FAIL3
if %choice% == 7 if %axa% == %a7% goto FAIL4
if %choice% == 8 if %aoa% == %a8% goto FAIL3
if %choice% == 8 if %axa% == %a8% goto FAIL4
if %choice% == 9 if %aoa% == %a9% goto FAIL3
if %choice% == 9 if %axa% == %a9% goto FAIL4
if %choice% == 1 set a1=%aoa% & goto PRUEO
if %choice% == 2 set a2=%aoa% & goto PRUEO
if %choice% == 3 set a3=%aoa% & goto PRUEO
if %choice% == 4 set a4=%aoa% & goto PRUEO
if %choice% == 5 set a5=%aoa% & goto PRUEO
if %choice% == 6 set a6=%aoa% & goto PRUEO
if %choice% == 7 set a7=%aoa% & goto PRUEO
if %choice% == 8 set a8=%aoa% & goto PRUEO
if %choice% == 9 set a9=%aoa% & goto PRUEO
if %choice% == x goto START
goto SPIELER2

:PRUEO
if %a1%==X set a1o=%SPACE1% & set a1x=%axa% & set "a1xr=%axa% " & set "a1om=%SPACE%"
if %a2%==X set a2o=%SPACE1% & set a2x=%axa% & set "a2xr=%axa% " & set "a2om=%SPACE%"
if %a3%==X set a3o=%SPACE1% & set a3x=%axa% & set "a3xr=%axa% " & set "a3om=%SPACE%"
if %a4%==X set a4o=%SPACE1% & set a4x=%axa% & set "a4xr=%axa% " & set "a4om=%SPACE%"
if %a5%==X set a5o=%SPACE1% & set a5x=%axa% & set "a5xr=%axa% " & set "a5om=%SPACE%"
if %a6%==X set a6o=%SPACE1% & set a6x=%axa% & set "a6xr=%axa% " & set "a6om=%SPACE%"
if %a7%==X set a7o=%SPACE1% & set a7x=%axa% & set "a7xr=%axa% " & set "a7om=%SPACE%"
if %a8%==X set a8o=%SPACE1% & set a8x=%axa% & set "a8xr=%axa% " & set "a8om=%SPACE%"
if %a9%==X set a9o=%SPACE1% & set a9x=%axa% & set "a9xr=%axa% " & set "a9om=%SPACE%"

if %a1%==O set a1x=%SPACE1% & set a1o=%aoa% & set "a1xr=%aoa% " & set "a1xr=%SPACE%" & set "a1om=%aoa% "
if %a2%==O set a2x=%SPACE1% & set a2o=%aoa% & set "a2xr=%aoa% " & set "a2xr=%SPACE%" & set "a2om=%aoa% "
if %a3%==O set a3x=%SPACE1% & set a3o=%aoa% & set "a3xr=%aoa% " & set "a3xr=%SPACE%" & set "a3om=%aoa% "
if %a4%==O set a4x=%SPACE1% & set a4o=%aoa% & set "a4xr=%aoa% " & set "a4xr=%SPACE%" & set "a4om=%aoa% "
if %a5%==O set a5x=%SPACE1% & set a5o=%aoa% & set "a5xr=%aoa% " & set "a5xr=%SPACE%" & set "a5om=%aoa% "
if %a6%==O set a6x=%SPACE1% & set a6o=%aoa% & set "a6xr=%aoa% " & set "a6xr=%SPACE%" & set "a6om=%aoa% "
if %a7%==O set a7x=%SPACE1% & set a7o=%aoa% & set "a7xr=%aoa% " & set "a7xr=%SPACE%" & set "a7om=%aoa% "
if %a8%==O set a8x=%SPACE1% & set a8o=%aoa% & set "a8xr=%aoa% " & set "a8xr=%SPACE%" & set "a8om=%aoa% "
if %a9%==O set a9x=%SPACE1% & set a9o=%aoa% & set "a9xr=%aoa% " & set "a9xr=%SPACE%" & set "a9om=%aoa% "

set /a rd=%rd%+1

if %a1% == O if %a2% == %a1% if %a2% == %a3% goto OWIN
if %a4% == O if %a5% == %a4% if %a5% == %a6% goto OWIN
if %a7% == O if %a8% == %a7% if %a8% == %a9% goto OWIN
if %a1% == O if %a4% == %a1% if %a4% == %a7% goto OWIN
if %a2% == O if %a5% == %a2% if %a5% == %a8% goto OWIN
if %a3% == O if %a6% == %a3% if %a6% == %a9% goto OWIN
if %a1% == O if %a5% == %a1% if %a5% == %a9% goto OWIN
if %a7% == O if %a5% == %a7% if %a5% == %a3% goto OWIN
if %rd% == 9 goto PAR
goto SPIELER1

:XWIN
cls
if %a1%==X set a1o=%axa% & set a1x=%axa% & set "a1xr=%axa% " & set "a1om=%axa% "
if %a2%==X set a2o=%axa% & set a2x=%axa% & set "a2xr=%axa% " & set "a2om=%axa% "
if %a3%==X set a3o=%axa% & set a3x=%axa% & set "a3xr=%axa% " & set "a3om=%axa% "
if %a4%==X set a4o=%axa% & set a4x=%axa% & set "a4xr=%axa% " & set "a4om=%axa% "
if %a5%==X set a5o=%axa% & set a5x=%axa% & set "a5xr=%axa% " & set "a5om=%axa% "
if %a6%==X set a6o=%axa% & set a6x=%axa% & set "a6xr=%axa% " & set "a6om=%axa% "
if %a7%==X set a7o=%axa% & set a7x=%axa% & set "a7xr=%axa% " & set "a7om=%axa% "
if %a8%==X set a8o=%axa% & set a8x=%axa% & set "a8xr=%axa% " & set "a8om=%axa% "
if %a9%==X set a9o=%axa% & set a9x=%axa% & set "a9xr=%axa% " & set "a9om=%axa% "

if %a1%==O set a1x=. & set a1o=. & set "a1xr=. " & set "a1xr=." & set "a1om=. "
if %a2%==O set a2x=. & set a2o=. & set "a2xr=. " & set "a2xr=." & set "a2om=. "
if %a3%==O set a3x=. & set a3o=. & set "a3xr=. " & set "a3xr=." & set "a3om=. "
if %a4%==O set a4x=. & set a4o=. & set "a4xr=. " & set "a4xr=." & set "a4om=. "
if %a5%==O set a5x=. & set a5o=. & set "a5xr=. " & set "a5xr=." & set "a5om=. "
if %a6%==O set a6x=. & set a6o=. & set "a6xr=. " & set "a6xr=." & set "a6om=. "
if %a7%==O set a7x=. & set a7o=. & set "a7xr=. " & set "a7xr=." & set "a7om=. "
if %a8%==O set a8x=. & set a8o=. & set "a8xr=. " & set "a8xr=." & set "a8om=. "
if %a9%==O set a9x=. & set a9o=. & set "a9xr=. " & set "a9xr=." & set "a9om=. "
set /a p1=%p1%+1
echo _-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-
echo                 TIC       TAC        TOE
echo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo		%s1%		%p1%:%p2%		%s2% 
echo.
echo                       %s1% Gewinnt!
echo. 
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo              I%a1x% %a1o% %a1xr%I%a2x% %a2o% %a2x%I%a3x% %a3o% %a3xr%I
echo    X   X     I%a1om% %a1x% %a1o%I%a2om% %a2x% %a2o%I%a3om% %a3x% %a3o%I
echo     X X      I%a1x% %a1o% %a1xr%I%a2x% %a2o% %a2x%I%a3x% %a3o% %a3xr%I	
echo      X       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~	1 I 2 I 3
echo     X X      I%a4x% %a4o% %a4x%I%a5x% %a5o% %a5xr%I%a6x% %a6o% %a6x%I	---------
echo    X   X     I%a4om% %a4x% %a4o%I%a5om% %a5x% %a5o%I%a6om% %a6x% %a6o%I	4 I 5 I 6
echo              I%a4x% %a4o% %a4x%I%a5x% %a5o% %a5xr%I%a6x% %a6o% %a6x%I	---------
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~	7 I 8 I 9
echo              I%a7x% %a7o% %a7x%I%a8x% %a8o% %a8xr%I%a9x% %a9o% %a9x%I
echo              I%a7om% %a7x% %a7o%I%a8om% %a8x% %a8o%I%a9om% %a9x% %a9o%I
echo              I%a7x% %a7o% %a7x%I%a8x% %a8o% %a8xr%I%a9x% %a9o% %a9x%I
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo. 
echo                        %s1% Gewinnt!
echo.
echo           Wähle n fuer Neustart oder x für Exit
set /p choice=                         

if %choice% == n goto PRESET
if %choice% == x exit
if %choice% == d goto DEBUG
goto XWIN

:OWIN
cls
if %a1%==O set a1o=%aoa% & set a1x=%aoa% & set "a1xr=%aoa% " & set "a1om=%aoa% "
if %a2%==O set a2o=%aoa% & set a2x=%aoa% & set "a2xr=%aoa% " & set "a2om=%aoa% "
if %a3%==O set a3o=%aoa% & set a3x=%aoa% & set "a3xr=%aoa% " & set "a3om=%aoa% "
if %a4%==O set a4o=%aoa% & set a4x=%aoa% & set "a4xr=%aoa% " & set "a4om=%aoa% "
if %a5%==O set a5o=%aoa% & set a5x=%aoa% & set "a5xr=%aoa% " & set "a5om=%aoa% "
if %a6%==O set a6o=%aoa% & set a6x=%aoa% & set "a6xr=%aoa% " & set "a6om=%aoa% "
if %a7%==O set a7o=%aoa% & set a7x=%aoa% & set "a7xr=%aoa% " & set "a7om=%aoa% "
if %a8%==O set a8o=%aoa% & set a8x=%aoa% & set "a8xr=%aoa% " & set "a8om=%aoa% "
if %a9%==O set a9o=%aoa% & set a9x=%aoa% & set "a9xr=%aoa% " & set "a9om=%aoa% "

if %a1%==X set a1x=. & set a1o=. & set "a1xr=. " & set "a1xr=." & set "a1om=. "
if %a2%==X set a2x=. & set a2o=. & set "a2xr=. " & set "a2xr=." & set "a2om=. "
if %a3%==X set a3x=. & set a3o=. & set "a3xr=. " & set "a3xr=." & set "a3om=. "
if %a4%==X set a4x=. & set a4o=. & set "a4xr=. " & set "a4xr=." & set "a4om=. "
if %a5%==X set a5x=. & set a5o=. & set "a5xr=. " & set "a5xr=." & set "a5om=. "
if %a6%==X set a6x=. & set a6o=. & set "a6xr=. " & set "a6xr=." & set "a6om=. "
if %a7%==X set a7x=. & set a7o=. & set "a7xr=. " & set "a7xr=." & set "a7om=. "
if %a8%==X set a8x=. & set a8o=. & set "a8xr=. " & set "a8xr=." & set "a8om=. "
if %a9%==X set a9x=. & set a9o=. & set "a9xr=. " & set "a9xr=." & set "a9om=. "
set /a p2=%p2%+1
echo _-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-
echo                 TIC       TAC        TOE
echo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo		%s1%		%p1%:%p2%		%s2% 
echo.
echo                       %s2% Gewinnt
echo. 
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo              I%a1x% %a1o% %a1xr%I%a2x% %a2o% %a2x%I%a3x% %a3o% %a3xr%I
echo     XxX      I%a1om% %a1x% %a1o%I%a2om% %a2x% %a2o%I%a3om% %a3x% %a3o%I
echo    X   X     I%a1x% %a1o% %a1xr%I%a2x% %a2o% %a2x%I%a3x% %a3o% %a3xr%I
echo    X   X     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~	1 I 2 I 3
echo    X   X     I%a4x% %a4o% %a4x%I%a5x% %a5o% %a5x%I%a6x% %a6o% %a6x%I	---------
echo     XXX      I%a4om% %a4x% %a4o%I%a5om% %a5x% %a5o%I%a6om% %a6x% %a6o%I	3 I 4 I 5
echo              I%a4x% %a4o% %a4x%I%a5x% %a5o% %a5x%I%a6x% %a6o% %a6x%I	---------
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~	7 I 8 I 9
echo              I%a7x% %a7o% %a7x%I%a8x% %a8o% %a8x%I%a9x% %a9o% %a9x%I
echo              I%a7om% %a7x% %a7o%I%a8om% %a8x% %a8o%I%a9om% %a9x% %a9o%I
echo              I%a7x% %a7o% %a7x%I%a8x% %a8o% %a8x%I%a9x% %a9o% %a9x%I
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo. 
echo                        %s2% Gewinnt!
echo.
echo           Wähle n fuer Neustart oder x fuer Exit
set /p choice=                         

if %choice% == n goto PRESET
if %choice% == x exit
if %choice% == d goto DEBUG
goto OWIN


:FAIL1
cls
echo _-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-
echo                 TIC       TAC        TOE
echo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo		%s1%		%p1%:%p2%		%s2% 
echo.
echo                       %s1% wählt
echo. 
echo.
echo             Dieses Feld gehört deinem Gegner!
echo                  Wähle ein anderes Feld
ping -n 4 localhost >NUL
goto SPIELER1

:FAIL2
cls
echo _-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-
echo                 TIC       TAC        TOE
echo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo		%s1%		%p1%:%p2%		%s2% 
echo.
echo                       %s1% wählt
echo. 
echo.
echo              Dieses Feld gehört dir bereits!
echo                  Wähle ein anderes Feld
ping -n 4 localhost >NUL
goto SPIELER1

:FAIL4
cls
echo _-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-
echo                 TIC       TAC        TOE
echo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo		%s1%		%p1%:%p2%		%s2% 
echo.
echo                       %s2% wählt
echo. 
echo.
echo             Dieses Feld gehört deinem Gegner!
echo                  Wähle ein anderes Feld
ping -n 4 localhost >NUL
goto SPIELER2

:FAIL3
cls
echo _-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-
echo                 TIC       TAC        TOE
echo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo		%s1%		%p1%:%p2%		%s2% 
echo.
echo                       %s2% wählt
echo. 
echo.
echo              Dieses Feld gehört dir bereits!
echo                  Wähle ein anderes Feld
ping -n 4 localhost >NUL
goto SPIELER2

:PAR
cls
echo _-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-
echo                 TIC       TAC        TOE
echo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo. 
echo.
echo                     Unentschieden!
echo. 
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo              I%a1x% %a1o% %a1xr%I%a2x% %a2o% %a2x%I%a3x% %a3o% %a3xr%I
echo    XXxXX     I%a1om% %a1x% %a1o%I%a2om% %a2x% %a2o%I%a3om% %a3x% %a3o%I
echo    XX XX     I%a1x% %a1o% %a1xr%I%a2x% %a2o% %a2x%I%a3x% %a3o% %a3xr%I
echo    X X X     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~	1 I 2 I 3
echo    XX XX     I%a4x% %a4o% %a4xr%I%a5x% %a5o% %a5xr%I%a6x% %a6o% %a6xr%I	---------
echo    XXXXX     I%a4om% %a4x% %a4o%I%a5om% %a5x% %a5o%I%a6om% %a6x% %a6o%I	3 I 4 I 5
echo              I%a4x% %a4o% %a4xr%I%a5x% %a5o% %a5xr%I%a6x% %a6o% %a6xr%I	---------
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~	7 I 8 I 9
echo              I%a7x% %a7o% %a7xr%I%a8x% %a8o% %a8xr%I%a9x% %a9o% %a9xr%I
echo              I%a7om% %a7x% %a7o%I%a8om% %a8x% %a8o%I%a9om% %a9x% %a9o%I
echo              I%a7x% %a7o% %a7xr%I%a8x% %a8o% %a8xr%I%a9x% %a9o% %a9xr%I
echo              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo. 
echo                      Unentschieden!
echo.
echo           Wähle n fuer Neustart oder x für Exit
set /p choice=                         

if %choice% == n goto PRESET
if %choice% == x exit
if %choice% == d goto DEBUG
goto PAR

:DEBUG
cls
echo _-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-
echo                 TIC       TAC        TOE
echo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
echo.
echo 1 = %a1%			a1x/a1o = %a1x%/%a1o%
echo 2 = %a2%			a2x/a2o = %a2x%/%a2o%
echo 3 = %a3%			a3x/a3o = %a3x%/%a3o%
echo 4 = %a4%			a4x/a4o = %a4x%/%a4o%
echo 5 = %a5%			a5x/a5o = %a5x%/%a5o%
echo 6 = %a6%			a6x/a6o = %a6x%/%a6o%
echo 7 = %a7%			a7x/a7o = %a7x%/%a7o%
echo 8 = %a8%			a8x/a8o = %a8x%/%a8o%
echo 9 = %a9%			a9x/a9o = %a9x%/%a9o%
echo axa = %axa%			space = "%SPACE%"
echo aoa = %aoa%			goto = %goto%
echo s1 = %s1%
echo s2 = %s2%
echo rd = %rd%
echo p1 = %p1%
echo p2 = %p2%
echo ausw = %ausw%
echo choice = %choice%
echo.

echo GOTO
set /p goto=

if %goto% == x goto START
goto %goto%

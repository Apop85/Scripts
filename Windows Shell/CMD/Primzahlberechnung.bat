@echo off & setlocal 
rem Erklärung auf: https://administrator.de/wissen/berechnen-nahezu-beliebige-kommastellen-batch-30000-stellen-getestet-204906.html
rem Dieses Script überprüft ob eine eingegebene Zahl (1-999999) eine Primzahl ist oder nicht. 

TITLE Primzahlenpruefer

:RESET
echo Gib hier deine Zahl ein (Max.:999'999):
set /p eingabe=
set counter=0
set "a=..." & set "b=..."
set "c=..." & set "d=..."
set "e=..." & set "f=..."
set "g=..." & set "h=..."
set "i=..." & set "j=..."
set eingabetmp=%eingabe%
set /a ziel=%eingabe%/2
set /a ziel+=1
if %eingabe% geq 1000000 ( set "ausgabe=ist zum berechnen zu gross!" & goto Done )
set counterz=0
goto Ziel

:Berechnung
set /a operator=%eingabe%*100
set /a counter+=1
set calctimer=0
set /a div0=%operator%/%counter%
set /a div1=%div0%/100
set div2=%div0:~-2%
set /a prozent=%counter%*100
set /a prozent=%prozent%/%ziel%
set "ausgabe2=" & set "divb="
set "ausgabe2=" & set "check="
cls
echo.
echo            Primzahl Ja oder Nein?
echo --------------------------------------------------
echo Berechnung: %eingabe% : %counter% = %div1%.%div2%
echo --------------------------------------------------
echo Ziel      : %eingabe% : %ziel% =
echo --------------------------------------------------
echo --------------------------------------------------
echo        Berechnung abgeschlossen: 
echo !!%a%%b%%c%%d%%e%%f%%g%%h%%i%%j%!!
echo             %prozent% Prozent
echo --------------------------------------------------
if "%counter%" neq "1" ( if "%counter%" neq "%ziel%" ( if "%div2%" == "00" ( goto CALCULATE ) ) )
if "%counter%" == "%ziel%" set "ausgabe=ist eine Primzahl!" & goto Done
if %prozent% geq 10 set "a=III"
if %prozent% geq 20 set "b=III"
if %prozent% geq 30 set "c=III"
if %prozent% geq 40 set "d=III"
if %prozent% geq 50 set "e=III"
if %prozent% geq 60 set "f=III"
if %prozent% geq 70 set "g=III"
if %prozent% geq 80 set "h=III"
if %prozent% geq 90 set "i=III"
if %prozent% geq 98 set "j=III"
goto Berechnung

:Done
cls
echo.
echo            Primzahl Ja oder Nein?
echo --------------------------------------------------
echo Berechnung: %eingabe% : %counter% = %div1%.%div2%
echo --------------------------------------------------
echo Die Zahl %eingabe% %ausgabe%
echo --------------------------------------------------
echo Um den dividend %counter% zu uebergehen j eingeben.
set /p next=
if "%next%" == "j" goto Berechnung
goto RESET

:CALCULATE
cls
set /a calctimer+=1
echo            Primzahl Ja oder Nein?
echo --------------------------------------------------
echo Berechnung: %eingabe% : %counter% = %div1%.%check%
echo --------------------------------------------------
echo Ziel      : %eingabe% : %ziel% =
echo --------------------------------------------------
echo --------------------------------------------------
echo        Berechnung abgeschlossen: 
echo !!%a%%b%%c%%d%%e%%f%%g%%h%%i%%j%!!
echo             %prozent% Prozent
echo --------------------------------------------------
set /a diva=%eingabetmp%/%counter%
if "%diva%" == "0" ( set "ausgabe2=0" & set "divb=0" )
if "%diva%" neq "0" ( set "ausgabe2=%diva%" & set /a divb=%diva%*%counter% )
if %diva% GEQ 1 ( set /a eingabetmp=%eingabetmp%-%divb% )
set /a eingabetmp=%eingabetmp%*10
if "%calctimer%" == "1" goto CALCULATE
if defined check set "check=%check%%ausgabe2%" 
if not defined check set "check=%ausgabe2%"
if "%check%" == "000000000000000000000000000000" set "ausgabe=Ist keine Primzahl sie ist durch %counter% Teilbar" & goto Done
if "%calctimer%" == "31" goto Berechnung  
goto CALCULATE

:Ziel
cls
echo.
echo Zielwert wird berechnet! Einen Augenblick bitte!
set /a counterz+=1
set /a QWT=%counterz%*%counterz% 
echo           %counterz%*%counterz% = %QWT%
if %QWT% geq %eingabe% set /a ziel=%counterz% & goto Berechnung
goto Ziel

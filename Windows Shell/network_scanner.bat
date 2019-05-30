@echo off
setlocal enabledelayedexpansion
rem Simple network scanner for Windows shell

rem Set IP-Range
rem --------------------------------------------------------------------
echo Set base address (xxx.0.0.1): 
set /p input=
set /a address=input

echo Set base address (1.xxx.0.1):
set /p input=
set /a  address2=input

echo Set subnet range(123.45.0-y.1):
set /p input=
set /a subnet=input

echo Set IP range (123.45.6.1-z):
set /p input=
set /a ip=input
echo %ip%

rem ping defined ip range and output if answer = true
rem --------------------------------------------------------------------
cls
for /l %%y in (0,1,%subnet%) do for /l %%x in (1,1,%ip%) do ping -n 1 %address%.%address2%.%%y.%%x | find ": Bytes" && nslookup %address%.%address2%.%%y.%%x && echo %address%.%address2%.%%y.%%x >> %UserProfile%\Desktop\output_new.txt
cls
echo. && echo. && echo. && echo Scan done!
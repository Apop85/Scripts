@echo off
rem Simple network scanner for Windows shell

rem Set IP-Range
rem --------------------------------------------------------------------
echo Set base adress (xxx.0.0.1): 
set /p adress=

echo Set base adress (1.xxx.0.1):
set /p adress2=
set /a adress2=%adress2%

echo Set subnet range(123.45.0-y.1):
set /p subnet=
set /a subnet=%subnet&%

echo Set IP range (123.45.6.1-z):
set /p ip=
set /a ip=%ip%

rem ping defined ip range and output if answer = true
rem --------------------------------------------------------------------
for /l %y in (0,1,%subnet%) do for /l %x in (1,1,%ip%) do ping -n 1 %adress%.%adress2%.%y.%x | find ": Bytes" && nslookup %adress%.%adress2%.%y.%x

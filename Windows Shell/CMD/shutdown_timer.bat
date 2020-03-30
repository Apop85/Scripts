@echo off
rem
rem ###
rem  File: shutdown_timer.bat
rem  Project: Windows Shell
rem -----
rem  Created Date: Wednesday 11.09.2019, 20:58
rem  Author: Apop85
rem -----
rem  Last Modified: Wednesday 11.09.2019, 20:58
rem -----
rem  Copyright (c) 2019 Apop85
rem  This software is published under the MIT license.
rem  Check http://www.opensource.org/licenses/MIT for further informations
rem -----
rem  Description:
rem ###
setlocal
echo Stunden bis zum shutdown: 
set /p stunden=
set /a stunden=%stunden%*60*60
echo %stunden%


shutdown -s -t %stunden%
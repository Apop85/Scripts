@echo off
REM /*
REM  * File: py_2_exe.bat
REM  * Project: Python
REM  * Created Date: Sunday 24.02.2019, 23:44
REM  * Author: Apop85
REM  * -----
REM  * Last Modified: 
REM  * -----
REM  * Copyright (c) 2019 Apop85
REM  * This software is published under the MIT license.
REM  * Check http://www.opensource.org/licenses/MIT for further informations
REM  * -----
REM  * Description:
REM  */

REM pip3 install pyinstaller
REM pyinstaller --icon=app.ico pong.py
REM pyinstaller pong.spec

pyinstaller --paths C:\Windows\System32\downlevel --console --onefile getfilepath.py
del updateData.exe
del *.spec
copy .\dist\getfilepath.exe .\updateData.exe
rmdir /S /Q dist
rmdir /S /Q __pycache__
rmdir /S /Q build

pyinstaller --paths C:\Windows\System32\downlevel --console --onefile updateStefflix.py
del updateStefflix.exe
del *.spec
copy .\dist\updateStefflix.exe .\updateStefflix.exe
rmdir /S /Q dist
rmdir /S /Q __pycache__
rmdir /S /Q build

pyinstaller --paths C:\Windows\System32\downlevel --console --onefile updater.py
del updater.exe
del *.spec
copy .\dist\updater.exe .\updater.exe
rmdir /S /Q dist
rmdir /S /Q __pycache__
rmdir /S /Q build

pyinstaller --paths C:\Windows\System32\downlevel --console --onefile setup.py
del setup.exe
del *.spec
copy .\dist\setup.exe .\setup.exe
rmdir /S /Q dist
rmdir /S /Q __pycache__
rmdir /S /Q build


@echo off
REM ###
REM  File: folder_structure.bat
REM  Project: Batch
REM -----
REM  Created Date: Thursday 05.03.2020, 20:05
REM  Author: Raffael Baldinger
REM -----
REM  Last Modified: Thursday 05.03.2020, 20:08
REM -----
REM  Copyright (c) 2020 Raffael Baldinger
REM  This software is published under the MIT license.
REM  Check http://www.opensource.org/licenses/MIT for further informations
REM -----
REM  Description: Dieses Script legt eine vorgelegte Ordnerstruktur auf einem Laufwerk an
REM  Eventuelle Fehlermeldungen werden dabei unterdrückt
REM ###

SETLOCAL ENABLEDELAYEDEXPANSION

REM Definiere zu erstellende Ordner
set folder_layer1="U:\privat" "U:\pst-archive" "U:\zwischenablage"
set folder_layer2="U:\privat\fotogallerie" "U:\privat\musik U:\privat\videos" "U:\pst-archive\2016 U:\pst-archive\2017" "U:\pst-archive\2018" "U:\pst-archive\2019" "U:\pst-archive\2020" "U:\zwischenablage\1_tag" "U:\zwischenablage\2_woche" "U:\zwischenablage\3_monat" "U:\zwischenablage\4_jahr"
set folder_layer3="U:\privat\fotogallerie\ferien" "U:\privat\fotogallerie\freizeit" "U:\privat\musik\mp3" "U:\privat\musik\wav"

REM Iteriere durch alle Arrays
for %%n in (1,2,3) do (
    REM Lese alle Items aus den Arrays aus
    for %%f in (!folder_layer%%n!) do (
        REM Erstelle Ordner
        md %%f
    )
)

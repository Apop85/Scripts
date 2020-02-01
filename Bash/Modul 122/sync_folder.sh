#!/bin/bash
# -*- coding:utf-8 -*-

####
# File: sync_folder.sh
# Project: Modul 122
#-----
# Created Date: Saturday 01.02.2020, 10:58
# Author: Apop85
#-----
# Last Modified: Saturday 01.02.2020, 13:21
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Dieses Script vergleicht den Inhalt von zwei Ordnern und synchronisiert diese
####

readonly SOURCE_PATH="/home/apop85"             # Pfad des Quellordners
readonly TARGET_PATH="/synced_folder"           # Pfad des Zielordners

function read_directories() {
    # Funktion zum rekursiven Auslesen der Pfade
    # Lese Parameter aus
    local current_folder=$1
    local target_folder=$2
    declare -a local file_array=$(ls $current_folder)

    # Unterscheide zwischen Ordnern und Dateien durch aufruf der Funktion check_files
    local return_array=$(check_files "$current_folder" "${file_array[*]}")
    # Splitte return_array auf files = alles vor dem @ und folders alles danach
    declare -a local folders=$(echo $return_array | sed -e "s/@.*//g")
    declare -a local files=$(echo $return_array | sed -e "s/.*@//g")
    # Synchronisiere aktuellen Ordner
    sync_folder "$current_folder" "$target_folder" "${files[*]}" "${folders[*]}"

    # Prüfe alle Unterordner sofern vorhanden
    local subfolder=
    if [[ "${folders[*]}" != "" ]]; then
        for subfolder in ${folders[*]}; do
            # Rekursiver Funktionsaufruf mit neuem Quell- und Zielverzeichnis als Parameter
            read_directories "$current_folder/$subfolder" "$target_folder/$subfolder"
        done
    fi

    return
}

function check_files() {
    # Nehme Parameter entgegen
    declare -a local current_path=$1
    declare -a local file_array=$2
    # Definiere Lokale Variablen
    declare -a local folder_array=
    declare -a local new_file_array=
    local localfile=""
    # Prüfe ob Eintrag ein Ordner oder ein File ist
    for localfile in ${file_array[*]}; do
        if [ -d $current_path/$localfile ]; then
            # Ist der Eintrag ein Ordner, füge diesen in das Ordnerarray ein
            folder_array+=("$localfile")
        else
            # Ist der Eintrag eine Datei, füge diese in das Dateiarray ein
            new_file_array+=("$localfile")
        fi
    done
    # Bereite Werte zur Übergabe vor
    local return_value=${folder_array[*]}@${new_file_array[*]}
    echo $return_value
    return
}

function sync_folder() {
    # Nehme Parameter entgegen
    local current_folder=$1
    local target_folder=$2
    declare -a local file_array=$3
    declare -a local folder_array=$4

    # Erstelle Ordner falls diese noch nicht existieren
    local folder_name=
    for folder_name in ${folder_array[*]}; do
        if [ ! -d $target_folder/$folder_name ]; then
            mkdir $target_folder/$folder_name
            echo -e "\e[7mINFORMATION:\e[0m Ordner \e[93m$folder_name\e[0m in \e[92m$target_folder\e[0m erstellt"
        fi
    done

    # Synchronisiere Dateien
    local file_name=
    for file_name in ${file_array[*]}; do
        # Prüfe ob Datei im Zielverzeichnis bereits existiert
        if [ ! -f $target_folder/$file_name ]; then
            # Falls nicht, kopiere Datei in Zielverzeichnis
            cp $current_folder/$file_name $target_folder/$file_name
            echo -e "\e[7mINFORMATION:\e[0m Datei \e[93m$file_name\e[0m wurde in \e[92m$target_folder\e[0m erstellt"
        else
            # Falls schon, lese Hash-Werte aus
            source_file_sha512=$(sha512sum $current_folder/$file_name | awk '{print $1}')
            target_file_sha512=$(sha512sum $target_folder/$file_name | awk '{print $1}')

            # Stimmen die Hashwerte nicht überein, Prüfe welche datei neuer ist.
            if [[ ${source_file_sha512} != ${target_file_sha512} ]]; then
                # Ist die Datei im Ursprungsordner neuer, 
                if [ $current_folder/$file_name -nt $target_folder/$file_name ]; then
                    # dann ersetze die Datei im Zielverzeichnis
                    rm $target_folder/$file_name 
                    cp $current_folder/$file_name $target_folder/$file
                    echo -e "\e[7mINFORMATION:\e[0m Datei \e[93m$file_name\e[0m wurde in \e[92m$target_folder\e[0m aktualisiert"
                else
                    # sonst ersetze die Datei im Ursprungsverzeichnis
                    rm $current_folder/$file_name 
                    cp $target_folder/$file_name $current_folder/$file
                    echo -e "\e[7mINFORMATION:\e[0m Datei \e[93m$file_name\e[0m wurde in \e[92m$current_folder\e[0m aktualisiert"
                fi
            fi
        fi
    done
}

# Synchronisiere von Quell- zu Zielverzeichnis
echo -e "\e[102m\e[30mSYNC: \e[91m\e[1m$SOURCE_PATH ==> $TARGET_PATH\e[0m"
read_directories $SOURCE_PATH $TARGET_PATH
# Synchronisiere von Ziel- zu Quellverzeichnis
echo -e "\e[102m\e[30mSYNC: \e[91m\e[1m$SOURCE_PATH <== $TARGET_PATH\e[0m"
read_directories $TARGET_PATH $SOURCE_PATH
#!/bin/bash
# -*- coding:utf-8 -*-

####
# File: make_backup.sh
# Project: Desktop
#-----
# Created Date: Friday 31.01.2020, 19:15
# Author: Apop85
#-----
# Last Modified: Friday 31.01.2020, 19:15
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Dieses Script erstellt ein komprimiertes Backup von den angegebenen Ordnern. 
# Requirements: Das Paket "tar" sowie das Paket "dialog" muss installiert sein.
####

# Konstanten
readonly folders_2_backup="/home /etc"                                                  # Ordner (getrennt durch Leerschläge) von denen ein Backup erstellt werden soll
readonly target_folder="/backups"                                                       # Ordner in welchem die Backups gespeichert werden
readonly prefix=$(date +%Y%m%d)                                                         # Präfix des Backupfilenamens - Aufbau: <yahr><monat><tag>
readonly suffix=$(echo $folders_2_backup | sed -E 's-/-_-g' | sed -E 's/ //g')          # Suffix des Backupfilenamens - Aufbau: _<ordner1>_<ordner2>...
readonly filename="${prefix}_BACKUP${suffix}.tar.gz"                                    # Aufbau Dateiname: <präfix>_BACKUP_<suffix>.tar.gz
readonly abs_backup_path="$target_folder/$filename"                                     # Absoluter Backuppfad


function init() {
    # Prüfe ob "dialog" installiert ist
    check=$(dpkg -s dialog 2>&1 | sed -n 2p | awk '{print $3}')
    if [[ "$check" != "ok" ]]; then 
        echo -e "Fehlendes paket: dialog. Zum installieren \n\n\t\t\e[91msudo apt-get install dialog\e[0m\n\n ausführen"
        exit 1
    fi
}

function check_4_root() {
    # Prüfe ob Script Rootprivilegien benötigt
    if [ "$EUID" -ne 0 ]; then 
        # Lese Pfad des Scripts aus
        scriptpath=$(dirname "$0")/$(basename "$0")
        dialog --backtitle "INFO" --title "Bestätigung" --yesno "Script wird als Root neu gestartet!" 15 60 

        input=${?}
        if [ "$input" == "1" ]; then
            exit 0                  # Wurde Abbrechen gewählt Script beenden
        else
            exec sudo $scriptpath   # Öffne Script mit Rootrechten
        fi
    fi
}

function create_backup() {
    local backup_path=$1
    if [ ! -d $target_folder ]; then
        mkdir $target_folder
    elif [ -f $abs_backup_path ]; then
        echo "Backup kann nicht erstellt werden."
        echo "Datei bereits vorhanden!" 
    else
        # Komprimiere Dateien
        tar -cvzf $backup_path $folders_2_backup 
        echo -e "\n\n-----------BACKUP ABGESCHLOSSEN-----------\n"
    fi

}

init            # Rufe Initialisierungsfunktion auf
check_4_root    # Rufe Root-Check-Funktion auf

# Erstelle Backup und gebe Ausgaben der Funktion in Dialogbox aus
create_backup $abs_backup_path | dialog --title "Fortschritt" \
          --programbox "Komprimierungsfortschritt:" \
          20 60

# Lese Dateigrösse aus
backup_size=$(ls -lh $abs_backup_path | awk '{print $5}')
# Gebe Informationen über erstellte Datei aus
dialog --title "Dateiinformation" --msgbox "Name: $filename Grösse: ${backup_size}b" 10 45


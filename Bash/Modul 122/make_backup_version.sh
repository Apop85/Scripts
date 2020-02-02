#!/bin/bash
# -*- coding:utf-8 -*-

####
# File: make_backup_version.sh
# Project: Modul 122
#-----
# Created Date: Saturday 01.02.2020, 15:26
# Author: Apop85
#-----
# Last Modified: Sunday 02.02.2020, 12:10
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:  Dieses Script erstellt ein komprimiertes Backup von den angegebenen Ordnern. 
#               Bei einem weiteren Durchgang wird nur noch ein Backup von neu hinzugefügten 
#               oder veränderten Dateien erstellt
# Requirements: Das Paket "tar" sowie das Paket "dialog" muss installiert sein.
####

# Konstanten
readonly folders_2_backup="/home /etc"                                                  # Ordner (getrennt durch Leerschläge) von denen ein Backup erstellt werden soll
readonly target_folder="/backups"                                                       # Ordner in welchem die Backups gespeichert werden
readonly prefix=$(date +%Y%m%d_%H%M)                                                    # Präfix des Backupfilenamens - Aufbau: <yahr><monat><tag>
readonly suffix=$(echo $folders_2_backup | sed -E 's-/-_-g' | sed -E 's/ //g')          # Suffix des Backupfilenamens - Aufbau: _<ordner1>_<ordner2>...
readonly filename="${prefix}_BACKUP${suffix}.tar.gz"                                    # Aufbau Dateiname: <präfix>_BACKUP_<suffix>.tar.gz
readonly abs_backup_path="$target_folder/$filename"                                     # Absoluter Backuppfad
readonly tmp_path="/var/tmp/backup"
declare -a files_2_backup=
declare -A file_table=
new_backup=false

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
    # Lese Parameter aus
    local backup_path_abs=$1
    local tmp=$2
    local backup_path=$3
    local folders_to_backup=$4

    # Zähle Anzahl Dateien
    declare -i local file_index_size=0
    for index in ${files_2_backup[*]}; do 
        file_index_size+=1
    done

    # Prüfe ob Backupordner existiert
    if [ ! -d $backup_path ]; then
        mkdir $backup_path
    fi
    
    # Prüfe ob tmp-Ordner existiert
    if [ ! -d $tmp ]; then
        mkdir $tmp
    fi

    # Prüfe ob Backupfile existiert
    if [ -f $backup_path_abs ]; then
        echo "Backup kann nicht erstellt werden."
        echo "Datei bereits vorhanden!" 
    else
        # Falls nicht prüfe auf vorhergehende Backups
        local done_backups=("$(ls $backup_path)")

        # Sind keine Backups vorhanden erstelle Stamm-Backup
        if [[ "${done_backups[*]}" == "" ]]; then
            # Komprimiere Dateien
            tar -cvzf "$backup_path_abs" $folders_to_backup | dialog --title "Fortschritt" \
                    --programbox "Komprimierungsfortschritt:" \
                    20 60
            new_backup=true
        else
            # Sind bereits Backups vorhanden Vergleiche Inhalt mit neuen Dateien
            declare -i local counter=0
            local new_files=
            local current_file_2_backup=
            
            # Prüfe jede Datei im Verzeichnis
            for current_file_2_backup in ${files_2_backup[*]}; do
                # Lese Einträge aus Dateiindex aus
                local indexed_path=$current_file_2_backup
                local search_string=$(echo ${indexed_path} | sed -E "s-^/--g")
                local version_key=version$search_string
                local backup_key=backupfile$search_string
                local indexed_version=$(echo ${file_table[$version_key]} | sed -E "s/^.*@//g")
                local indexed_backupfile=$(echo ${file_table[$backup_key]} | sed -E "s/^.*@//g")
                local tmp_file="$indexed_path"

                # Prüfe ob Datei bereits in einem Backup existiert
                if [[ ${!file_table[*]} =~ ${search_string} ]]; then
                    # Entpacke entsprechende Datei in TMP-Verzeichnis
                    if (( $indexed_version == 0 )); then
                        tar -xf $indexed_backupfile --directory=$tmp $search_string
                    else
                        # Ist die Datei Versioniert, entferne die Versionsnummern vom Dateinamen
                        tar -xf $indexed_backupfile --directory=$tmp $search_string.$indexed_version
                        local new_tmp_file=$(echo $tmp_file | sed -E "s/\.[0-9]+$//")
                        mv $tmp$tmp_file.$indexed_version $tmp$new_tmp_file
                        local tmp_file=$new_tmp_file
                    fi

                    # Erstelle Hashwert der zu vergleichenden Dateien
                    if [ -f $tmp/$tmp_file ]; then
                        local indexed_file_hash=$(sha512sum $tmp/$tmp_file | awk '{print $1}')
                        local current_file_hash=$(sha512sum /$indexed_path | awk '{print $1}')

                        rm $tmp/$tmp_file

                        if [[ $indexed_file_hash != $current_file_hash ]]; then
                            # Füge Datei in die Liste zu komprimierenden Dateien hinzu
                            new_version=$((1+$indexed_version))
                            # Füge Versionsnummer der Datei hinzu
                            new_files+=("$indexed_path.$new_version")
                        fi
                    fi
                else
                    # Ist die Datei nicht vorhanden, diese zu komprimierende Dateien hinzufügen
                    new_files+=("$indexed_path")
                fi
                counter+=1
                # Berechne Fortschritt
                local status=$(echo "scale=5; 100/$file_index_size*$counter+1" | bc | sed -E "s/\.[0-9]*//")
                echo $status | dialog \
                    --title "Versionsprüfung" \
                    --backtitle "FORTSCHRITT" \
                    --gauge "Dateiversionen werden geprüft" 7 40 0 
            done 
            # Lösche temporäre Dateien und Verzeichnis
            rm -R $tmp
            
            # Sind veränderte oder neue Files hinzugekommen?
            if [[ "${new_files[*]}" != "" ]]; then
                declare -a new_names 
                # Versioniere Dateien
                for new_name in ${new_files[*]}; do
                    if [[ $new_name =~ .*[0-9]+$ ]]; then
                        local original_name=$(echo $new_name | sed -E "s/\.[0-9]*$//g")
                        cp $original_name $new_name
                    fi
                done
                # Komprimiere neue Dateien
                tar -cvzf "$backup_path_abs" ${new_files[*]} 2>/dev/null | dialog --title "Fortschritt" \
                    --programbox "Komprimierungsfortschritt:" \
                    20 60 
                new_backup=true

                # Lösche temporäre Dateien
                for new_name in ${new_files[*]}; do
                    if [[ $new_name =~ .*[0-9]+$ ]]; then 
                        rm $new_name
                    fi
                done
            fi

        fi
        
    fi

}

function get_files_2_backup() {
    declare -a local folders=$1
    local folder=
    # Gehe Alle Ordner durch
    for folder in ${folders[*]}; do
        # Prüfe jedes File und jeden Ordner
        for entry in $(ls -a $folder); do
            # . und .. sind nicht erlaubt
            if [[ ! $entry =~ ^\.*$ ]]; then   
                # Ist es eine Datei?
                if [[ -f $folder/$entry ]]; then
                    files_2_backup+=("$folder/$entry")
                # Ist es ein Ordner?
                elif [[ -d $folder/$entry ]]; then
                    # Rekursiver Aufruf der Funktion mit neuem Verzeichnis
                    get_files_2_backup "$folder/$entry"
                fi
            fi
        done
    done
    return
}

function get_file_index() {
    # Prüfe ob file bereits in einem Backup existiert
    local backup_path=$1
    # Lese Dateien im Backupverzeichnis aus
    declare -a local backup_files=$(ls $backup_path)
    # BEreite Variablen vor
    declare -a local stored_files=""
    local backup_file=
    # Öffne alle vorhandenen Dateien
    for backup_file in ${backup_files[*]}; do
        # Lese Liste aller Dateien aus dem Archiv aus
        local files=("$(tar -ztvf $backup_path/$backup_file | awk '{print $6}')")
        for file_name in ${files[*]}; do
            # Prüfe ob Datei Versioniert ist
            if [[ $file_name =~ .*[0-9]+$ ]]; then
                local current_filename=$(echo $file_name | sed -e "s/\.[0-9]*[0-9]*[0-9]*$//g")
                local file_version=$(echo $file_name | sed -e "s/.*\.//g")
            else
                local current_filename=$file_name
                local file_version=0
            fi
            # Füge Datei und deren Eigenschaften der Hash Table hinzu
            file_table+=(["version${current_filename}"]="")
            file_table+=(["backupfile${current_filename}"]="")
            file_table["version${current_filename}"]+="@$file_version"
            file_table["backupfile${current_filename}"]+="@$backup_path/$backup_file"
        done
    done
    return
}

init            # Rufe Initialisierungsfunktion auf
check_4_root    # Rufe Root-Check-Funktion auf

# Erstelle Index aus allen gesicherten Dateien
declare -a done_backups=("$(ls $backup_path)")
declare -i backups_before=${#done_backups[*]}

# Erstelle einen Index aus den Archivierten Dateien
if [[ "$backups_before" != "" ]]; then
    get_file_index "$target_folder"
fi

# Erstelle Liste der Dateien in den Verzeichnissen
get_files_2_backup "$folders_2_backup"

# Erstelle Backup und gebe Ausgaben der Funktion in Dialogbox aus
create_backup "$abs_backup_path" "$tmp_path" "$target_folder" "$folders_2_backup" 

if $new_backup; then
    # Lese Dateigrösse aus
    backup_size=$(ls -lh $abs_backup_path | awk '{print $5}')
    # Gebe Informationen über erstellte Datei aus
    dialog --title "Dateiinformation" --msgbox "Name: $filename Grösse: ${backup_size}b" 10 45
fi


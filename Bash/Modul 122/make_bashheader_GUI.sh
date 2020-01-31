#!/bin/bash
# -*- coding:utf-8 -*-

####
# File: make_bashheader_GUI.sh
# Project: scripts
#-----
# Created Date: Sunday 12.01.2020, 12:16
# Author: Apop85
#-----
# Last Modified: Sunday 12.01.2020, 12:23
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Dieses Script erstellt eine Datei, fügt einen Header ein und macht die Datei
# Wunschgemäss ausführbar. Diese Version besitzt ein dialog basiertes GUI
####
# Changelog
# Änderung                                      | Datum             | Author
# Grösse Hauptmenü angepasst                    | 15.01.2020        | rba
####

function initialize() {
    check=$(dpkg -s dialog 2>&1 | sed -n 2p | awk '{print $3}')
    if [[ "$check" != "ok" ]]; then 
        echo "Fehlendes paket: dialog. Zum installieren \"sudo apt-get install dialog\" ausführen"
        exit 1
    fi
}

function show_error() {
    local error=$1
    dialog --backtitle "FEHLER" --title "Fehlermeldungen" --msgbox "Folgende Fehler sind aufgetreten: $error" 15 25
    return
}

function menu() {
    while true; do
        # Hauptmenü
        auswahl=$(dialog --stdout --separate-output --backtitle "HAUPTMENÜ" --title "Auswahl" \
        --checklist "Optionen auswählen:" 16 50 10 \
        1 "Datei erstellen" on \
        2 "Dateiberechtigungen ändern" on \
        3 "Header einfügen" on)

        if [ $? -eq 0 ]; then
            # Wurde OK gedrückt?
            local aufgaben=()
            for wahl in $auswahl; do
                    if [ $wahl -eq 1 ]; then
                        # Dateiinformationen einholen
                        file=$(get_file_information)
                        # Füge Aufgabe hinzu
                        aufgaben+=("addfile" $file)
                    elif [ $wahl -eq 2 ]; then
                        if (( $(echo ${auswahl[*]} | grep 1 | wc -l) != 1 )); then
                            # Datei auswählen
                            file=$(choose_file)
                        fi
                        # Berechtigungen einholen
                        mods=$(choose_mods)
                        # Füge Aufgabe hinzu
                        aufgaben+=("modfile" $file $mods)
                    elif [ $wahl -eq 3 ]; then
                        if (( $(echo ${auswahl[*]} | grep -Eo "[1|2]" | wc -l) < 1 )); then
                            # Datei auswählen
                            file=$(choose_file)
                        fi
                        # Füge Aufgabe hinzu
                        aufgaben+=("addheader" $file)
                    fi
            done
        else
            exit 0
        fi

        erledige_aufgaben "${aufgaben[*]}"

    done
}

function get_file_information() {
    local error_message=""
    local file=""
    local path=""

    while true; do
        # Forumlardefinition
        values=$(dialog --stdout --backtitle "DATEIINFORMATIONEN" --title "Dateiinformationen" --form "Bitte folgende Dateiinfomrationen definieren" 15 40 0 \
                "Name:" 1 1 "$file" 1 13 35 0 \
                "Pfad:" 2 1 "$path" 2 13 35 0 )

        # Wurde OK gedrückt?
        if [ $? -eq 0 ]; then
            values=($values)
            file=${values[0]}
            path=${values[1]}
            local file_info="$path/$file"
            # Wurden alle Felder Ausgefüllt?
            if [[ "$file" != "" ]]&&[[ "$path" != "" ]]; then 
                # Existiert der angegebene Pfad und gibt es die Datei noch nicht?
                if [ -d $path ]&&[ ! -e $path/$file ]; then 
                    echo $file_info
                    return
                else
                    # Generiere Fehlermeldungen sollte Statement nicht zutreffen
                    if [ ! -d $path ]; then
                        error_message="Der angegebene Pfad existiert nicht! $error_message"
                    fi

                    if [ -e $path/$file ]; then
                        error_message="Die anegegebene Datei existiert bereits! $error_message"
                    fi
                    # Zeige Fehlermeldung über fehlerhafte Angaben an
                    show_error "$error_message"
                    error_message=""
                fi
            else
                # Schreibe nicht ausgefüllte Felder in error_message
                if [[ "$file" == "" ]]; then
                    error_message="Dateiname fehlt! $error_message"
                fi
                if [[ "$path" == "" ]]; then
                    error_message="Pfadangabe fehlt! $error_message"
                fi
                # Zeige Fehlermeldung über fehlende Angaben an
                show_error "$error_message"
                error_message=""
            fi


        else
            return
        fi
    done
}

function choose_file() {
    # Datei auswählen
    file=$(dialog --stdout --backtitle "DATEI" \
        --title "Von welcher Datei soll die Berechtigung editiert werden?" \
        --fselect $HOME/ 10 40)
    echo $file
    return
}

function choose_mods() {
    while true; do
        # Lasse Berechtigungen auslesen
        auswahl=$(dialog --stdout --separate-output --backtitle "BERECHTIGUNGEN" --title "Auswahl" \
        --checklist "Optionen auswählen:" 16 40 10 \
        1 "Eigentümer: Ausführen" on \
        2 "Eigentümer: Schreiben" on \
        3 "Eigentümer: Lesen" on \
        4 "Gruppe: Ausführen" off \
        5 "Gruppe: Schreiben" off \
        6 "Gruppe: Lesen" off \
        7 "Andere: Ausführen" off \
        8 "Andere: Schreiben" off \
        9 "Andere: Lesen" off )

        if [ $? -eq 0 ]; then
        # Wurde OK gedrückt?
            local r=4
            local w=2
            local x=1
            local owner=0
            local group=0
            local other=0
            # Generiere Berechtigungen
            for wahl in $auswahl; do
                if (( $wahl == 1 )); then
                    owner=$((owner+x))
                elif (( $wahl == 2 )); then
                    owner=$((owner+w))
                elif (( $wahl == 3 )); then
                    owner=$((owner+r))
                elif (( $wahl == 4 )); then
                    group=$((group+x))
                elif (( $wahl == 5 )); then
                    group=$((group+w))
                elif (( $wahl == 6 )); then
                    group=$((group+r))
                elif (( $wahl == 7 )); then
                    other=$((other+x))
                elif (( $wahl == 8 )); then
                    other=$((other+w))
                elif (( $wahl == 9 )); then
                    other=$((other+r))
                fi
            done
        else
            return 1
        fi

        # Gebe Berechtigungsmodifikation zurück
        value=${owner}${group}${other}
        echo $value
        return
    done
}

function add_header() {
    file=$1
    author=$(whoami)
    # Vordefinierter header
    header="#!/bin/bash
        \n# -*- coding:utf-8 -*- \n
        \n####
        \n# File: $file
        \n# Project: $project_name
        \n#-----
        \n# Created Date: $(date "+%d.%m.%Y %H:%M")
        \n# Author: $author
        \n#-----
        \n# Last Modified: $(date "+%d.%m.%Y %H:%M") $author
        \n#-----
        \n# Copyright (c) $(date "+%Y") $author
        \n# This software is published under the MIT license.
        \n# Check http://www.opensource.org/licenses/MIT for further informations
        \n#-----
        \n# Description:
        \n####
        \n"

        echo -e ${header} >> $file
        return
}

function erledige_aufgaben() {
    # Setze Funktionsinterne Variablen
    local aufgaben=($1)
    local counter=0
    local error_message=""
    # Für jede gespeicherte Aufgabe führe aus:
    for aufgabe in ${aufgaben[*]}; do
        if [[ $aufgabe == "addfile" ]]; then
            # Hinzufügen einer Datei
            path=${aufgaben[$((counter+1))]}
            touch $path >/dev/null 2>&1
            if [ ! -e $path ]; then
                # Existiert Datei nicht  --> Fehlermeldung generieren
                error_message="$error_message Datei konnte nicht erstellt werden!"
            fi
        elif [[ $aufgabe == "modfile" ]]; then
            # Editierem der Berechtigungen
            path=${aufgaben[$((counter+1))]}
            mods=${aufgaben[$((counter+2))]}
            chmod $mods $path >/dev/null 2>&1
            local test_value=$(stat -c "%a %n" $path | awk '{print $1}')
            if [[ "$mods" != "$test_value" ]]; then
                # Entsprechen die Zugriffsrechte nicht den Vorgaben --> Fehlermeldung generieren
                error_message="$error_message Dateiberechtigungen konnten nicht gesetzt werden!"
            fi
        elif [[ $aufgabe == "addheader" ]]; then
            # Füge Header bei Datei hinzu
            oldsize=$(wc -c $path | awk '{print $1}')
            add_header $file >/dev/null 2>&1
            newsize=$(wc -c $path | awk '{print $1}')
            if (( $oldsize >= $newsize )); then
                # Wenn Header nicht in Datei eingefügt werden konnte --> Fehlermeldung generieren
                error_message="$error_message Header konnte nicht in die Datei geschrieben werden!"
            fi
        fi

        counter=$((counter+1))
        faktor=$((counter/${#aufgaben[*]}))
        echo $((100*faktor))
        # Werte echos als Fortschritt in Fortschrittsbalken aus
    done | dialog --backtitle "AUSFÜHRUNG" --title "Operationen" --gauge "Anstehende Aufgaben werden durchgeführt" 10 40 0

    if [[ "$error_message" != "" ]]; then
        # Existieren Fehlermeldungen --> Ausgabe
        dialog --backtitle "FEHLER" --title "Fehlermeldung" --msgbox "Beim durchlaufen der Aufgaben sind Fehler aufgetreten: $error_message" 10 40
    else
        # Existieren keine Fehlermeldungen gebe Info aus. 
        dialog --backtitle "INFO" --title "Aufgaben abgeschlossen" --msgbox "Die Aufgaben für die Datei $path wurden erfolgreich abgeschlossen." 10 40
    fi
    exit 0
}

initialize
menu

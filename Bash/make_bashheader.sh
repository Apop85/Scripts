#!/bin/bash
# -*- coding:utf-8 -*-
###
# ###
#  File: make_header.sh
#  Project: Modul 122
# -----
#  Created Date: Thursday 09.01.2020, 20:59
#  Author: Raffael Baldinger
# -----
#  Last Modified: Friday 10.01.2020, 00:15
# -----
#  Copyright (c) 2020 Raffael Baldinger
#  This software is published under the MIT license.
#  Check http://www.opensource.org/licenses/MIT for further informations
# -----
#  Description: Dieses Script erstellt eine neue Datei und fügt automatisch einen Header mit
#  aktuellem Datum und Uhrzeit sowie Author ein und macht die Datei anschliessend ausführbar. 
# ###
###

function show_message () {
    message=$1
    type=$2

    # Farbcodes
    bg_white="\e[107m"
    bg_red="\e[101m"
    bg_blue="\e[104m"
    bg_green="\e[42m"
    txt_white="\e[97m"
    txt_black="\e[30m"
    txt_bold="\e[1m"
    default="\e[0m"

    # Welcher Typ wurde übergeben?
    if [[ "$type" == "info" ]]; then
        echo -e "${bg_white}${txt_black}INFO:${default}${txt_bold} ${message}" >&2
    elif [[ "$type" == "input" ]]; then
        echo -e "${bg_blue}${txt_bold}${txt_white}EINGABE ERFORDERLICH:${default}${txt_bold} ${message}" >&2
    elif [[ "$type" == "ok" ]]; then
        echo -e "${bg_green}${txt_bold}${txt_white}ERFOLGREICH:${default}${txt_bold} ${message}" >&2
    elif [[ "$type" == "error" ]]; then
        echo -e "${bg_red}${txt_bold}${txt_white}FEHLER:${default}${txt_bold} ${message}" >&2
        exit 1
    fi
    return
}

function get_filename {
    # Repetiere bis User Eingabe bestätigt
    while true; do
        show_message "Dateinamen inkl Pfad für neue Datei eingeben:" "input"
        read file
        show_message "Ist die Angabe $file korrekt? (J/n)" "input"
        read answer

        case $answer in
            [yY] | [jJ] | "")
            show_message "Datei wird erstellt..." "info"
            touch $file >/dev/null 2>&1
            # Konnte die Datei angelegt werden?
            if [ -e $file ]; then
                show_message "Datei $file wurde angelegt" "ok"
            else
                show_message "Datei $file konnte nicht angelegt werden" "error"
            fi
            echo $file
            return
            ;;
            [nN])
            continue
            ;;
        esac

    done
}

function get_project_name {
    # echo "enter function"
    while true; do
        show_message "Projektnamen angeben:" "input"
        read project_name
        show_message "Ist die Angabe $project_name korrekt? (J/n)" "input"
        read answer

        case $answer in
            [yY] | [jJ] | "")
            echo $project_name
            return
            ;;
            [nN])
            continue
            ;;
        esac
    done
}

function get_author {
    # Lese Usernamen aus
    author=$(whoami)
    echo $author
    return
}

function insert_header () {
    file=$1
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

    show_message "Schreibe Header in Datei..." "info"
    echo -e ${header} > $file

    # Ist die Datei noch leer?
    if [ -s $file ]; then
        show_message "Header wurde in Datei geschrieben" "ok"
    else
        show_message "Datei konnte nicht beschrieben werden" "error"
    fi

    return
}

function chmod_file () {
    show_message "Datei $file wird ausführbar gemacht." "info"
    file=$1
    chmod +x $file >/dev/null 2>&1
    if [ -x $file ]; then
        show_message "$file wurde ausführbar gemacht" "ok"
    else
        show_message "$file konnte nicht ausführbar gemacht werden"
    fi

    return
}

function edit_it () {
    file=$1
    show_message "Möchtest du die Datei mit Nano öffnen? (J/n)" "input"
    read answer

    case $answer in
        [yY] | [jJ] | "")
        nano $file 
        ;;
        [nN])
        exit 0
        ;;
    esac

}


author=$(get_author)
project_name=$(get_project_name)
file=$(get_filename)
insert_header $file
chmod_file $file
edit_it $file

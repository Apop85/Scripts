    #!/bin/bash
    # -*- coding:utf-8 -*-

    ####
    # File: guess_the_number.sh
    # Project: Modul 122
    #-----
    # Created Date: Wednesday 29.01.2020, 11:54
    # Author: Apop85
    #-----
    # Last Modified: Wednesday 29.01.2020, 17:41
    #-----
    # Copyright (c) 2020 Apop85
    # This software is published under the MIT license.
    # Check http://www.opensource.org/licenses/MIT for further informations
    #-----
    # Description:  Dieses Script generiert eine Zufallszahl zwischen 1 und 100
    #               Welche der User erraten muss um zu gewinnen. 
    # Requirements: Das Paket "dialog" und "bc" muss installiert sein.
    ####

    # Konstanten
    readonly SCORE_FILE=/var/tmp/highscore.tmp  # Speicherort des Highscores
    readonly MAX_TRY=10                         # Anzahl Versuche
    readonly ANZAHL_RUNDEN=10                   # Anzahl Runden
    readonly KLEINSTE_ZAHL=1                    # Kleinste Randomzahl
    readonly GROESSTE_ZAHL=100                  # Grösste Randomzahl
    readonly BOX_WIDTH=35                       # Dialogboxbreite
    readonly BOX_HEIGHT=18                      # Dialogboxhöhe
    readonly MENU_HEIGHT=14                     # Dialogmenühöhe

    function init() {
        # Prüfe ob das Paket "dialog" installiert ist
        check=$(dpkg -s dialog 2>&1 | sed -n 2p | awk '{print $3}')
        if [[ "$check" != "ok" ]]; then 
            echo -e "Fehlendes paket: dialog. Zum installieren \n\n\t\tsudo apt-get install dialog\n\n ausführen"
            exit 1
        fi
        
        check=$(dpkg -s bc 2>&1 | sed -n 2p | awk '{print $3}')
        if [[ "$check" != "ok" ]]; then 
            echo -e "Fehlendes paket: bc. Zum installieren \n\n\t\tsudo apt-get install bc\n\n ausführen"
            exit 1
        fi

        # Prüfe ob Highscore-Tabelle bereits existiert
        # Falls nicht lege die Datei an
        if [ ! -f $SCORE_FILE ]; then
            touch $SCORE_FILE
            echo "PLATZ        VERSUCHE        ZEIT        NAME" >> $SCORE_FILE
            for number in {1..5}; do 
                echo [$number] >> $SCORE_FILE
            done
        fi
    }

    function main_menu() {
        while true; do
            # Anzeige des Menüs
            choice=$(dialog --stdout --title "Auswahl" --backtitle "HAUPTMENÜ" --menu \
            "Menüpunkt wählen:" $BOX_HEIGHT $BOX_WIDTH $MENU_HEIGHT \
            1 "Spiel Starten" \
            2 "Highscores anzeigen" \
            3 "Beenden")

            # Wenn nicht Abbrechen ausgewählt wurde dann
            if [ $? -eq 0 ]; then
                # Auswahl auswerten
                if [ $choice -eq 1 ]; then
                    # Starte neues Spiel
                    play_game
                elif [ $choice -eq 2 ]; then
                    # Zeige Highscore an
                    show_highscore
                elif [ $choice -eq 3 ]; then
                    # Beende das Spiel
                    exit 0
                else
                    show_message "Eingabe konnte nicht verarbeitet werden"
                fi
            fi

        done
    }

    function show_message() {
        message=$1
        # Zeige Nachricht an die der Funktion übergeben wurde
        dialog --title "Benachrichtigung" --msgbox "$message" $BOX_HEIGHT $BOX_WIDTH
    }

    function show_highscore() {
        # Zeige Inhalt des Highscore-Files
        dialog --title "High Scores" --backtitle "BESTENLISTE" --textbox $SCORE_FILE $BOX_HEIGHT 55
    }

    function check_highscore() {
        # Nehme Funktionsparameter entgegen
        runden_aktuell=$1
        zeit_aktuell=$2

        # Entferne Kommastellen von Zeitmessung
        zeit_aktuell=$(echo "scale=0; $zeit_aktuell*1000" | bc | sed -E "s/\.0*//g" )

        # Erstelle Zeilencounter
        declare -i aktuelle_zeile=0

        # Soll die Auswertung ignoriert werden?
        ignore=false
        skip=false
        declare -i delta=0

        # Lese Highscorefile Zeile für Zeile aus
        while IFS= read -r line; do
            # Lese Einträge aus Datei aus
            rang=$(echo "$line" | awk '{print $1}' | sed -E 's/\[//g' | sed -E 's/\]//g')
            runden=$(echo "$line" | awk '{print $2}')
            zeit=$(echo "$line" | awk '{print $3}')
            name=$(echo "$line" | awk '{print $4}')

            # Ersetze leere Strings mit temporären Daten um die Auswertung zu ermöglichen
            if [[ "$runden" == "" ]]; then
                runden=$(($MAX_TRY+1))
            fi
            if [[ "$zeit" == "" ]]; then
                zeit=999999999
            else
                zeit=$(echo "scale=0; $zeit*1000" | bc | sed -E "s/\.000//g")
            fi
            
            if ! $ignore; then
                # Ignoriere 1. Zeile bei der Auswertung
                if [[ $aktuelle_zeile != 0 ]]; then
                    # Entferne Kommastellen

                    # Vergleiche EInträge mit aktuellem Score
                    if  (( $runden == $MAX_TRY+1 )) || (( $runden >= $runden_aktuell )) && (( $zeit >= $zeit_aktuell )); then

                        while true; do
                            # Frage nach Namen für die Highscore-Tabelle
                            mitkomma=$(echo "scale=3; $zeit_aktuell/1000" | bc)
                            newname=$(dialog --stdout --backtitle "HIGHSCORE!!" --title "Du hast einen neuen Highscore!" --inputbox \
                                "Du hast $runden_aktuell Runden benötigt und dafür nur $mitkomma Sekunden gebraucht. Dafür landest du auf dem Rang $rang! Nenne deinen Namen für die Highscores:" \
                                $BOX_HEIGHT $BOX_WIDTH)
                            
                            ignore=true
                            skip=true
                            
                            # Entspricht der eingegebene Name den Regeln?
                            if ! [[ $newname =~ ^[a-zA-Z0-9]+$ ]]; then
                                show_message "\"$newname\" Kann leider nicht als Name verwendet werden. Es sind nur Zahlen und Buchstaben erlaubt"
                                continue
                            else
                                # Entspricht der Name den Regeln unterbreche Loop
                                break
                            fi
                        
                        # ignoriere die danach folgenden Einträge
                        done

                    fi
                fi
            fi

            # Ersetze temporäre Werte wieder mit leeren Strings
            if (( $runden == $MAX_TRY+1 )); then
                runden=""
            fi
            if [[ $zeit == 999999999 ]]; then
                zeit=""
            else
                # Füge Kommastellen bei Zeit wieder hinzu
                zeit=$(echo "scale=3; $zeit/1000" | bc)
            fi

            # Schreibe Highscores zurück in die Datei
            if (( $aktuelle_zeile == 0 )); then
                echo "$rang            $runden            $zeit       $name" > $SCORE_FILE            
            else
                if ! $skip; then
                    echo "[$(($rang+$delta))]        $runden        $zeit         $name" >> $SCORE_FILE
                else
                    zeit_aktuell=$(echo "scale=3; $zeit_aktuell/1000" | bc)
                    echo "[$rang]        $runden_aktuell        $zeit_aktuell         $newname" >> $SCORE_FILE
                    echo "[$(($rang+1))]        $runden        $zeit         $name" >> $SCORE_FILE
                    declare -i delta=1
                    skip=false
                fi
            fi

            aktuelle_zeile+=1
        done < $SCORE_FILE
    }

    function play_game() {
        # Generiere Zufallszahl
        ZUFALLSZAHL=$(shuf -i $KLEINSTE_ZAHL-$GROESSTE_ZAHL -n 1)
        # Speichere Startzeitpunkt
        STARTZEIT=$(date +%s.%N)
        declare -i runde=1
        gewonnen=false              # Flag ob die Zahl erraten wurde
        prefix=""                   # Präfix für die Nachricht in der Inputbox

        # Solange Runde 10 nicht erreicht ist wiederhole:
        while (( $runde <= $ANZAHL_RUNDEN )); do
            guess=$(dialog --stdout --backtitle "RUNDE $runde" --title "Errate die Zahl" --inputbox \
                    "${prefix}An welche Zahl zwischen $KLEINSTE_ZAHL und $GROESSTE_ZAHL denke ich?" \
                    $BOX_HEIGHT $BOX_WIDTH)

            # Prüfe Eingabe
            # Ist die Eingabe eine Zahl?
            if [[ ! $guess =~ ^[0-9]+$ ]]; then
                show_message "Das ist keine Zahl!"
                # Beginne Loop von vorne
                continue
            # Ist die Zahl grösser als erlaubt?
            elif (( $guess > $GROESSTE_ZAHL )); then
                show_message "$guess ist grösser als $GROESSTE_ZAHL und daher nicht zulässig"
                # Beginne Loop von vorne
                continue
            # Ist die eingegebene Zahl 30 oder mehr von der Zufallszahl entfernt?
            elif (( $guess <= $ZUFALLSZAHL-30 )) || (( $guess >= $ZUFALLSZAHL+30 )); then
                prefix="SEHR KALT... "
            # Ist die eingegebene Zahl 20 oder mehr von der Zufallszahl entfernt?
            elif (( $guess <= $ZUFALLSZAHL-20 )) || (( $guess >= $ZUFALLSZAHL+20 )); then
                prefix="KALT... "
            # Ist die eingegebene Zahl 10 oder mehr von der Zufallszahl entfernt?
            elif (( $guess <= $ZUFALLSZAHL-10 )) || (( $guess >= $ZUFALLSZAHL+10 )); then
                prefix="WARM... "
            # Ist die eingegebene Zahl 5 oder mehr von der Zufallszahl entfernt?
            elif (( $guess <= $ZUFALLSZAHL-5 )) || (( $guess >= $ZUFALLSZAHL+5 )); then
                prefix="WÄRMER... "
            # Ist die eingegebene Zahl 2 oder mehr von der Zufallszahl entfernt?
            elif (( $guess <= $ZUFALLSZAHL-2 )) || (( $guess >= $ZUFALLSZAHL+2 )); then
                prefix="HEISS!!! "
            # Wurde die Zahl erraten?
            elif (( $guess == $ZUFALLSZAHL )); then
                # Setze Flag, dass die Zahl erraten wurde
                gewonnen=true
                # Unterbreche Loop
                break
            fi
            # Inkrementiere die Rundenzahl um 1
            runde+=1
        done

        # Wurde die Flag für ein gewonnenes Spiel gesetzt?
        if $gewonnen; then
            # Erfasse Endzeitpunkt
            ENDZEIT=$(date +%s.%N)
            # Berechne Zeitdifferenz
            GEBRAUCHTE_ZEIT=$(echo "scale=3; ($ENDZEIT - $STARTZEIT) / 1" | bc)
            # Zeige Gewinnnachricht
            show_message "Du hast gewonnen! Du hast $runde Runden benötigt und dafür $GEBRAUCHTE_ZEIT Sekunden benötigt. Mal sehen ob es für einen Highscore reicht ;)"
            # Prüfe ob der aktuelle Score besser als einer der Highscoreeinträge ist
            # show_message "Rang: $rang - Runden: $runden - Runden aktuell: $runden_aktuell - Zeit: $zeit - Zeit aktuell: $zeit_aktuell - Name: $name - Zeile: $aktualle_zeile"
            show_message "Gebrauchte Zeit: $GEBRAUCHTE_ZEIT, STARTZEIT: $STARTZEIT, ENDZEIT: $ENDZEIT"
            check_highscore $runde $GEBRAUCHTE_ZEIT
        else
            # Zeige Nachricht, dass die Zahl nicht erraten wurde
            show_message "Du hast es leider nicht geschafft :(. Ich habe an die Zahl $ZUFALLSZAHL gedacht. Beim nächsten mal schaffst du es..."
        fi

    }


    init        # Funktion zur initierung der grundvoraussetzungen des Scripts
    main_menu   # Rufe das Hauptmenü auf
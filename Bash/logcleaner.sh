#!/bin/bash
#Dieses Script beschränkt die Länge eines Logfiles auf eine gewisse Anzahl Zeilen und entfernt regelmässig die ältesten Einträge

readonly MAXLINES=49
readonly LOGFILE=$HOME/scriptexec.log

#Logfilefunktion mit Zeilenbeschränkung
function logsetup {
    TMP=$(tail -n $MAXLINES $LOGFILE 2>/dev/null) && echo "${TMP}" > $LOGFILE
    exec > >(tee -a $LOGFILE)
    exec 2>&1
}

logsetup

echo "$(date +"%d.%m.%Y - %H:%M:%S")" logcleaner.sh ausgeführt >> $LOGFILE

exit 0

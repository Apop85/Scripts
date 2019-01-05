#!/bin/bash
#Erklärung auf: https://wiki.ubuntuusers.de/Skripte/Updatebenachrichtigung_per_Messenger/
#Dieses Script überprüft ob Updates verfügbar sind und falls ja wird eine Benachrichtigung per Telegram versendet. 
#Alternativ kann dieses Script auch für LINE verwendet werden. 
#Ebenfalls können Programme hinterlegt werden welche bei einem anstehenden Update bei der Benachrichtigung gesondert hervorgehoben werden. 

#Messenger festlegen ( leer = Telegram / 1 = LINE )
readonly SELECT=

#Bot-ID's und Token hier eintragen
readonly TELEDIR='/home/pi/Autostart/telegram.inf'
source $TELEDIR
readonly ACCESS_TOKEN=''  	        #Für Benachrichtigung mit LINE

#Zu sendende Nachricht
function msg_create {
    if [ "$flagname" == "" ]; then
        flagname=N/A
    fi
    if (( $anzahl == 1 )); then
        plura1=ist
        plura2=Update
    else
        plura1=sind
        plura2=Updates
    fi
    MESSAGE="Es $plura1 $anzahl $plura2 für den Raspberry verfügbar! Flags: $flags - [$flagname]"
}

#Setze Konstanten
readonly UDLIST=/var/tmp/udlist.tmp
readonly UDLAST=/var/tmp/udlast.tmp
readonly REFLIST=$(xdg-user-dir DOCUMENTS)/reflist.txt
readonly LOGFILE=$HOME/update.log
readonly RUNLOG=$HOME/scriptexec.log
readonly MAXLINES=19
readonly TELEURL='https://api.telegram.org/bot'
readonly LINEURL='https://notify-api.line.me/api/notify'

# Falls nicht vorhanden, erstelle benötigte Dateien.
touch $UDLAST $UDLIST $LOGFILE $REFLIST $RUNLOG

#Logfilefunktion mit Zeilenbeschränkung
function logsetup {
    TMP=$(tail -n $MAXLINES $LOGFILE 2>/dev/null) && echo "${TMP}" > $LOGFILE
    exec > >(tee -a $LOGFILE)
    exec 2>&1
}

function log {
    echo "$(date +"%d.%m.%Y - %H:%M:%S"): $*"
}

# Vergleiche verfügbare Updates mit Referenzliste und gleiche mit letztem Run ab. 
anzahl=$(apt-get -q -y --ignore-hold --allow-unauthenticated -s upgrade | grep ^Inst | cut -d\  -f2 | wc -l)
if (( $anzahl != 0 )); then
    apt-get -q -y --ignore-hold --allow-unauthenticated -s upgrade | grep ^Inst | cut -d\  -f2 >> $UDLIST
    newupdates=$(grep -Fxv -f $UDLAST $UDLIST | wc -l)
    if (( $newupdates != 0 )); then
       flags=$(grep -f $REFLIST $UDLIST | wc -l)
       flagname=$(grep -f $REFLIST $UDLIST)
       msg_create
       if [ "$SELECT" == "" ]; then
           curl -s -k "$TELEURL$BOT_ID/sendMessage" -d text="$MESSAGE" -d chat_id=$CHAT_ID
       else
	   curl -X POST -H 'Authorization: Bearer $ACCESS_TOKEN' -F 'message=$MESSAGE' $LINEURL
       fi
       logsetup
       log Updates: $anzahl Flags: $flags - [$flagname]
    fi
fi

# Ersetze Log des letzten Runs mit dem aktuellen Log.
rm $UDLAST
mv $UDLIST $UDLAST

echo "$(date +"%d.%m.%Y - %H:%M:%S")" updatecheck.sh ausgeführt >> $RUNLOG

exit 0

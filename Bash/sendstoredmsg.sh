#!/bin/bash
#Dieses Script verschickt in einem File zwischengespeicherte Nachrichten. 

#-------------------------Konstanten-----------------------------
#IDs
readonly BOT_ID=''
readonly CHAT_ID=''
#URLs
readonly TELEGRAMAPIURL="https://api.telegram.org/bot$BOT_ID/sendMessage"
#TMP-Files
readonly MESSAGES='/var/tmp/messages.tmp'
readonly RUNLOG=$HOME/scriptexec.log
#----------------------------------------------------------------

touch $RUNLOG

#Gespeicherte Nachrichten auslesen.
message=$(<$MESSAGES)

#Gespeicherte Nachrichten versenden.
curl -s -k "$TELEGRAMAPIURL" -d text="$message" -d chat_id="$CHAT_ID"

#Lösche TMP-Files
if [ -e $MESSAGES ]; then
	rm $MESSAGES
	echo "$(date +"%d.%m.%Y - %H:%M:%S")" sendstoredmsg.sh ausgeführt, Nachrichten versendet. >> $RUNLOG
else
	echo "$(date +"%d.%m.%Y - %H:%M:%S")" sendstoredmsg.sh ausgeführt, Keine gespeicherten Nachrichten. >> $RUNLOG
fi

exit 0

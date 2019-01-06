#!/bin/bash
#Einfaches Script welches bei Neujahr eine Benachrichtigung mit dem richtigen Jahr versendet. 

#Telegram ID's
readonly TELEDIR='/home/pi/Autostart/telegram.inf'
source $TELEDIR

readonly RUNLOG=$HOME/scriptexec.log
touch $RUNLOG

#Ermittle Jahr
year=`date +'%Y'`

#Zu sendende Nachricht
readonly MESSAGE="Happy New Year $year!"

curl -s -k "https://api.telegram.org/bot$BOT_ID/sendMessage" -d text="$MESSAGE" -d chat_id=$CHAT_ID

echo "$(date +"%d.%m.%Y - %H:%M:%S")" hny.sh ausgeführt >> $RUNLOG

exit 0

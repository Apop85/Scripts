#!/bin/bash
#Simples Script zur Benachrichtigung nach einem Reboot

#Telegram ID's
readonly TELEDIR='/home/pi/Autostart/telegram.inf'
#Lese Telegraminformationen aus
source $TELEDIR

readonly MESSAGE="PiHole ist wieder online!"
readonly RUNLOG=$HOME/scriptexec.log
touch $RUNLOG

#Warten auf Netzwerk
sleep 10

#Nachricht Senden
curl -s -k "https://api.telegram.org/bot$BOT_ID/sendMessage" -d text="$MESSAGE" -d chat_id=$CHAT_ID

echo "$(date +"%d.%m.%Y - %H:%M:%S")" autostart.sh ausgeführt >> $RUNLOG

exit 0

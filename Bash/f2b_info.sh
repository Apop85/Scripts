#!/bin/bash
#Dieses Script übermittelt Benachrichtigungen zu Fail2Ban

#Telegram ID's
readonly TELEURL='https://api.telegram.org/bot'
readonly TELEDIR='/home/pi/Autostart/telegram.inf'
source $TELEDIR

#Zu sendende Nachricht:
readonly MSG_STATIC="[Fail2Ban] Die IP $2"
readonly MSG_UNBAN=" wurde von der Banliste entfernt"
readonly MSG_BAN=" wurde gebannt!"

function function_unban {
	curl -s -k "$TELEURL$BOT_ID/sendMessage" -d text="$MSG_STATIC$MSG_UNBAN" -d chat_id=$CHAT_ID 1>/dev/null
	echo Folgende IP wurde entbannt: $2 > /home/pi/f2b_ban.log
}

function function_ban {
	curl -s -k "$TELEURL$BOT_ID/sendMessage" -d text="$MSG_STATIC$MSG_BAN" -d chat_id=$CHAT_ID 1>/dev/null
	echo Folgende IP wurde gebannt: $2 > /home/pi/f2b_ban.log
}

function_$1

exit 0


#!/bin/bash

readonly PUB_IP=$(wget -qO - http://myip.dnsomatic.com/ 2> /dev/null)
readonly RUNLOG=/home/pi/scriptexec.log
readonly MESSAGES='/var/tmp/messages.tmp'
#Telegram ID's
readonly TELEDIR='/home/pi/Autostart/telegram.inf'
source $TELEDIR
#read BOT_ID CHAT_ID < <(cat $TELEDIR)

readonly TEMPFILE="/var/tmp/$common_name.tmp"

#Zu sendende Nachricht
MESSAGE="VPN-Verbindung wurde hergestellt! Benutzer: $common_name. IP-Adresse: $trusted_ip."

touch $RUNLOG

#Lese Uhrzeit
zeit=$(date +"%H")
echo "$(date +"%d.%m.%Y - %H:%M:%S")" VPNconnect.sh ausgeführt >> $RUNLOG

function storemessage {
    touch $MESSAGES
    MESSAGE="$(date +"%H:%M"): $MESSAGE"
    echo $MESSAGE >> $MESSAGES
    exit 0
}

function msgsend {
	curl -s -k "https://api.telegram.org/bot$BOT_ID/sendMessage" -d text="$MESSAGE" -d chat_id=$CHAT_ID
}


#Sende Nachricht falls IP von ausserhalb. 
if [ "$PUB_IP" != "$trusted_ip" ]; then
    if (( $zeit <= 9 )); then
        storemessage
    else
		if [ -f $TEMPFILE ]; then
			if (( $(find "$TEMPFILE" -mmin +60 | wc -l) >= 1 )); then 
				touch $TEMPFILE
				msgsend
			else 
				storemessage
			fi
		else
			touch $TEMPFILE
			msgsend
		fi
    fi
fi

exit 0

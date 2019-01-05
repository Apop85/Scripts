#!/bin/bash
#Script zur Überprüfung der öffentlichen IP-Adresse, bei einer Änderung wird eine Benachrichtigung per Telegram verschickt. 

readonly RUNLOG='/home/pi/scriptexec.log'
readonly MESSAGES='/var/tmp/messages.tmp'
readonly TMPFILE='/var/tmp/ip.tmp'
#Telegram ID's
readonly TELEDIR='/home/pi/Autostart/telegram.inf'
source $TELEDIR


#Benachrichtigungen
function createmsg {
	message="Neue IP-Adresse festgestellt: $pub_ip"
}

function createlogmsg {
	readonly LOGMESSAGE="$(date +"%d.%m.%Y - %H:%M:%S") checkpublicip.sh ausgeführt - $message"
}

#Öffentliche IP auslesen mit redundanten Quellen
function getpubip {
	pub_ip=$(wget -qO - http://myip.dnsomatic.com/ 2> /dev/null)
	if [ "$pub_ip" == "" ]; then
		pub_ip=$(wget -qO - ipinfo.io/ip 2> /dev/null)
		if [ "$pub_ip" == "" ]; then
			pub_ip=$(wget -qO - http://checkip4.spdyn.de 2> /dev/null)
		fi
	fi
	if [ "$pub_ip" == "" ]; then
		let counter++
		if (( $counter == 10 )); then
			echo IP konnte nicht ermittelt werden.  Verbindung überprüfen! >> $RUNLOG
			exit 0
		else
			sleep 10
			getpubip
		fi
	fi
}

#Lese Uhrzeit
zeit=$(date +"%H")

#Nachricht speichern.
function storemessage {
	touch $MESSAGES
	createlogmsg
	echo $LOGMESSAGE >> $RUNLOG
	storedmsg="$(date +"%H:%M"): $message"
	echo $storedmsg >> $MESSAGES
	exit 0
}

#Lese IP aus TMP-File und lese aktuelle IP-Adresse aus.
touch $TMPFILE
lastip=$(cat $TMPFILE)
getpubip
createmsg

#Vergleiche aktuelle IP mit gespeicherter IP
if [ "$lastip" == "$pub_ip" ]; then
	exit 0
else
	if (( $zeit <= 9 )); then
        	storemessage
	else
		if [ "$pub_ip" != "" ]; then
	        	curl -s -k "https://api.telegram.org/bot$BOT_ID/sendMessage" -d text="$message" -d chat_id=$CHAT_ID
			echo $LOGMESSAGE >> $RUNLOG
			echo $pub_ip > $TMPFILE
		else
			echo Fehler! IP-Variable leer!
			exit 0
		fi
	fi
fi 

exit 0
	






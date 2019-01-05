#!/bin/bash
#Client Updatescript

#Telegram Informationen
readonly BOT_ID=''
readonly CHAT_ID=''
readonly TELEURL='https://api.telegram.org/bot'

#Userinformationen
readonly USERINFO="$HOME/scripts/user.inf"
source $USERINFO

#Konstanten
readonly UPDATE_URL='http://apop85.bplaced.net/raspiremote/remote_slave.sh.gpg' 	#Quelle update.txt
readonly LOGFILE="$HOME/remote_master.log"						#Logfile
readonly MAXLINES=49									#Maximale Anzahl Zeilen im Logfile +1
readonly INSTDIR="$HOME/scripts/remote/remote_slave.sh.new"
readonly OLDDIR="$HOME/scripts/remote/remote_slave.sh"
readonly ENCRYPTED="$HOME/remote_slave.sh.gpg"

#Logfilefunktion mit Zeilenbeschränkung
function logsetup {
    TMP=$(tail -n $MAXLINES $LOGFILE 2>/dev/null) && echo "${TMP}" > $LOGFILE
    exec > >(tee -a $LOGFILE)
    exec 2>&1
}

function log {
    echo "$(date +"%d.%m.%Y - %H:%M:%S"): $*"
}

#Sendefunktion für Telegram
function message_send {
	curl -s -k "$TELEURL$BOT_ID/sendMessage" -d text="$MESSAGE" -d chat_id=$CHAT_ID 1>/dev/null
}

#Installiere neue Version
function rdy2go {
	chmod +x $OLDDIR
	log Installation der neuen Version abgeschlossen
	MESSAGE="[User:$user]Neue Version von remote_slave installiert."
	message_send
}

touch $LOGFILE
logsetup
log Clientupdate gestartet

#Lese Remotedatei aus
wget $UPDATE_URL &>/dev/null

#Generiere key und entschlüssle Files
if [ -e $ENCRYPTED ]; then
	passit=$(printf '%x\n' )
	gpg -d --passphrase $passit --batch --no-tty < $ENCRYPTED >$INSTDIR
	if [ -e $INSTDIR ]; then
		rm $ENCRYPTED
	else
		log Entpacken fehlgeschlagen.
		exit 0
	fi
fi

#Überprüfung ob Datei downgeloadet wurde und von alter Version ein Backup anlegen
if [ -e $INSTDIR ]; then
	if [ -e $OLDDIR.bak ]; then
			if [ -e $OLDDIR.bak.bak ]; then
				rm $OLDDIR.bak.bak
			fi
		mv $OLDDIR.bak $OLDDIR.bak.bak
	fi
	mv $OLDDIR $OLDDIR.bak
	log Bakups von remote_slave wurden angelegt.
	mv $INSTDIR $OLDDIR
	if [ -e $OLDDIR ]; then
		rm $INSTDIR
		rdy2go
	else
		log Installation fehlerhaft
		exit 1
	fi
fi

if [ -e $INSTDIR ]; then
	rm $INSTDIR
fi

$OLDDIR &
exit 0

#appid=$(ps aux | grep remote_slave.sh | grep -v grep | awk '{print $2}')
#kill $appid

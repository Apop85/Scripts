#!/bin/bash
# Raspberry updaten
#     ____  ____  ____  ______
#    / __ \/ __ \/ __ \/_  __/
#   / /_/ / / / / / / / / /   
#  / _, _/ /_/ / /_/ / / /    
# /_/ |_|\____/\____/ /_/     

readonly TMPFILE='/var/tmp/rem_update.tmp'
readonly LOGFILE=$HOME/remote_slave.log
readonly BOT_ID=''
readonly CHAT_ID=''
readonly TELEURL='https://api.telegram.org/bot'
readonly MAXLINES=49
readonly USERINFO='$HOME/scripts/user.inf'
source $USERINFO

#Logfilefunktion
function logsetup {
    TMP=$(tail -n $MAXLINES $LOGFILE 2>/dev/null) && echo "${TMP}" > $LOGFILE
    exec > >(tee -a $LOGFILE)
    exec 2>&1
}

function log {
    echo "$(date +"%d.%m.%Y - %H:%M:%S"): $*" >> $LOGFILE
}

#Sende Nachricht
function message_send {
	readonly MESSAGE='[USER: $user] Updates wurden installiert'
	curl -s -k "$TELEURL$BOT_ID/sendMessage" -d text="$MESSAGE" -d chat_id=$CHAT_ID 1>/dev/null
}

logsetup

if [ -f $TMPFILE ]; then
	source $TMPFILE
	apt-get update
	log apt-get Update ausgeführt
	apt-get upgrade -y
	log apt-get Upgrade ausgeführt
	rm $TMPFILE
fi

message_send
exit 0


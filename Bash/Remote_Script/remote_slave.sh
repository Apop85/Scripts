#!/bin/bash

#Scriptversion
readonly VERSION_NOW="5"
readonly USERDIR="$HOME/scripts/user.inf"
source $USERDIR


#Telegram Informationen
readonly BOT_ID=''
readonly CHAT_ID=''
readonly TELEURL='https://api.telegram.org/bot'

#Userinformationen
readonly USERINFO="$HOME/scripts/user.inf"
source $USERINFO

#Konstanten
readonly COMMANDLIST="$HOME/remote.txt"								#Heruntergeladene remote-datei
readonly UPDATE_URL='' 	#Quelle update.txt
readonly REMOTE_URL='' 	#Quelle remote.txt
readonly SYSUD_TMP="/var/tmp/rem_update.tmp"					#Systemremote_update
readonly REBOOT_TMP="/var/tmp/rem_reboot.tmp"							#Pi Neustarten
readonly LOGFILE="$HOME/remote_master.log"						#Logfile
readonly MAXLINES=49												#Maximale Anzahl Zeilen im Logfile +1
readonly STARTUPDATE="$HOME/scripts/remote/updateclient.sh &"	#Updatescript
readonly ENCRYPT="$HOME/scripts/remote/encrypt.sh"

touch $LOGFILE

if [ -f $COMMANDLIST ]; then
        rm $COMMANDLIST
fi

#-----FUNKTIONEN-BENUTZERERKENNUNG-UND-VERIFIZIERUNG------
#Funktion Verifizierungscode
function remote_verify {
	inp_code=$*
	export code_hex=$inp_code
	ver_code=$($ENCRYPT)
	verify_check
}

function verify_check {
        if [ "$ver_code" != "OK" ]; then
                exit 0
        fi
}

#Funktion Username setzen und prüfen
function remote_setuser {
	verify_check
	REMOTEUSER=$*
	if [ “$user“ == “$REMOTEUSER“ ] || [ “$REMOTEUSER“ == “alle“ ]; then
		userstatus=ok
		log Verifizierung OK. Ausführen der Funktionen von remote.txt.
		client_update
	else
		exit 0		#Error: Wrong user!
	fi
}

#------------------USER-FUNKTIONEN------------------------
#Funktion Statusnotification
function remote_status {
	if [ “$userstatus“ == “ok“ ]; then
		log Ausgeführte Funktion: status
		gather_status
	fi
}
+
#Funktion PiHole - Update
function remote_piholeupdate {
	if [ “$userstatus“ == “ok“ ]; then
		log Ausgeführte Funktion: piholeupdate
		pihole -up
		log PiHole-Software aktualisiert.
		MESSAGE="User:$user: Pi-Hole Software wurde aktualisiert"
		message_send
	fi
}

#Funktion PiHole - Domain zu Whitelist hinzufügen
function remote_piholeaddwhite {
	if [ “$userstatus“ == “ok“ ]; then
		ph_list=$*
		log Ausgeführte Funktion: piholeaddwhite
		pihole -w $ph_list
		log Domain [$ph_list] wurde zur PiHole-Whitelist hinzugefügt
		MESSAGE="User:$user: Pi-Hole Whitelist aktualisiert"
		message_send
	fi
}

#Funktion VNC-Server aktivieren
function remote_enablevnc {
	if [ “$userstatus“ == “ok“ ]; then
		log Ausgeführte Funktion: enablevnc
		vncserver :1
		log VNC-Server mit dem Display 1 wurde aktiviert. 
		MESSAGE="User:$user: VNC Server aktiviert"
		message_send
	fi
}

#Funktion Funktionen auflisten
function remote_function {
	log Ausgeführte Funktion: function
	MESSAGE=$(declare -F | grep -e remote | cut -d_ -f2)
	MESSAGE="Verfügbare Funktion von User [$user]: $MESSAGE"
	message_send
	log Verfügbare Funktionen übermittelt
}

#------------------ROOT-FUNKTIONEN------------------------
#Funktion Update
function remote_update {
	if [ “$userstatus“ == “ok“ ]; then
		touch $SYSUD_TMP
		echo user=$user >> $SYSUD_TMP
		log Ausgeführte Funktion: remote_update
	fi
}

#Funktion Neustart
function remote_neustart {
	if [ “$userstatus“ == “ok“ ]; then
		touch $REBOOT_TMP
		echo user=$user >> $REBOOT_TMP
		log Ausgeführte Funktion: remote_neustart
	fi
}

#-----------VOM-SCRIPT-VERWENDETE-FUNKTIONEN--------------
#Falls neue Version verfügbar Script updaten
function client_update {
	version_new=$(curl $UPDATE_URL 2>/dev/null)
	if [ "$version_new" != "$VERSION_NOW" ]; then
		log Neues Update verfügbar - Starte Updatescript
		$STARTUPDATE
		exit 0
	fi
}

#Logfilefunktion mit Zeilenbeschränkung
function logsetup {
    TMP=$(tail -n $MAXLINES $LOGFILE 2>/dev/null) && echo "${TMP}" > $LOGFILE
    exec > >(tee -a $LOGFILE)
    exec 2>&1
}

function log {
    echo "$(date +"%d.%m.%Y - %H:%M:%S"): $*"
}

function message_send {
	sleep 5
	curl -s -k "$TELEURL$BOT_ID/sendMessage" -d text="$MESSAGE" -d chat_id=$CHAT_ID 1>/dev/null
}

#------------------UNTERFUNKTIONEN-------------------------
#Unterfunktion zur Geräteanalyse (remote_status)
function gather_status {
	#Defaultausgaben:
	ssh_stat=OFF
	piho_stat=OFF
	vpn_stat=OFF
	f2b_stat=OFF
	ftp_stat=OFF
	vnc_stat=OFF
	duc_stat=OFF
	
	#Informationen zusammenstellen
	readonly PUB_IP=$(wget -qO - http://myip.dnsomatic.com/ 2> /dev/null)
	readonly LAN_IP=$(hostname -I)
	readonly LASTBOOT=$(uptime -s)
	readonly BANNED_IP=$(awk '($(NF-1) = /Ban/){print $NF}' /var/log/fail2ban.log | sort | uniq -c | sort -n)

	#Prüfe Services
	if (( $(ps ax | grep "sshd" | wc -l) > 1 )); then
		ssh_stat=ON
	fi

	if (( $(ps ax | grep "pihole" | wc -l) > 1 )); then
		piho_stat=ON
	fi

	if (( $(ps ax | grep "openvpn" | wc -l) > 1 )); then    
		vpn_stat=ON
	fi

	if (( $(ps ax | grep "fail2ban" | wc -l) > 1 )); then    
		f2b_stat=ON
	fi
	
	if (( $(ps ax | grep "ftp" | wc -l) > 1 )); then    
		ftp_stat=ON
	fi
	
	if (( $(ps ax | grep "vnc" | wc -l) > 1 )); then    
		vnc_stat=ON
	fi

	if (( $(ps ax | grep "noip" | wc -l) > 1 )); then    
		duc_stat=ON
	fi
	
	#Sende Nachricht
	MESSAGE="User: [$user] | pubIP: [$PUB_IP] | lanIP: [$LAN_IP] | SSH: [$ssh_stat] | VNC: [$vnc_stat] | PiHole: [$piho_stat] | VPN: [$vpn_stat] | DUC: [$duc_stat] | Fail2Ban: [$f2b_stat] | FTP: [$ftp_stat] | Uptime: [$LASTBOOT] | Gebannte IP: [$BANNED_IP]"
	message_send
	log Allgemeine Statusangaben übermittelt
}

#-----------------------AUSFÜHREN DER FUNKTIONEN DURCH remote.txt--------------------------

#Lese Remotedatei aus
wget $REMOTE_URL &>/dev/null

#Lade Remotebefehle und führe sie aus.
if [ -f $COMMANDLIST ];then 
	check=$(grep -c '' $COMMANDLIST)
	if (( $check > 2 )); then
		logsetup
		while IFS= read -r var
		do
			remote_$var
		done < $COMMANDLIST
	fi
	rm $COMMANDLIST
fi

exit 0



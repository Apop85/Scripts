#!/bin/bash

readonly KEYGEN="$HOME/RMF/encrypt.sh"
readonly REM_DIR="$HOME/RMF/slave/update/remote.txt"
readonly VER_DIR="$HOME/RMF/slave/update/update.txt"


#FTP-Zugang:
readonly USERFTP='apop85_raspberry'
readonly FTP_HOST=''
readonly FTP_PW=''
readonly merk=""
readonly UPDATEFILE="$HOME/RMF/slave/update/remote_slave.sh.gpg"
readonly UPDATE_URL='http://apop85.bplaced.net/raspiremote/update.txt' 
updatedir="$HOME/RMF/slave/remote_slave.sh"

#clear old files
if [ -f $REM_DIR ]; then
	rm $REM_DIR
fi
if [ -f $VER_DIR ]; then
	rm $VER_DIR
fi
touch $VER_DIR

versionnow=$(curl $UPDATE_URL 2>/dev/null)
let versionnow++

#Auswahl Update planen oder Funktion ausführen
function update_or_run {
	auswahl=$(dialog --stdout --backtitle "Bitte Wählen" --title Aktion --radiolist "Was soll ausgeführt werden?." 16 60 5 \
		"CODE" "Code generieren" on \
		"UPDATE" "Update einreihen" off)
	case "$auswahl" in
		CODE)
			choose_option
			;;
		UPDATE)
			create_update
			;;
	esac
}

#Generiere passwort für Updatefile und definiere Dateipfad
function create_update {
	dialog --backtitle INFO --title "Dateipfad" --yesno "$updatedir als Ursprung verwenden?" 15 60
	inp2=${?}
	if [ "$inp2" == "1" ]; then
		while true; do
			updatedir=$(dialog --inputbox "Dateipfad angeben:" 15 60  --output-fd 1)
			if [ -e $updatedir ]; then
				break
			else
				dialog --backtitle FEHLER --title Achtung --msgbox "Datei $updatedir nicht gefunden!" 15 40
				continue
			fi
		done
	fi

	encrypt_update
}

function encrypt_update {
	passit=$($KEYGEN $merk)
	gpg -o $UPDATEFILE --yes --pinentry-mode loopback --passphrase $passit --symmetric $updatedir
	if [ -e $UPDATEFILE ]; then
		echo "Verschlüsselung erfolgreich mit $passit"
		touch $VER_DIR
		while true; do
			clear
			dialog --backtitle INFO --title "Versionsinfo" --yesno "Achtung! Update mit der Versionsnummer <<${versionnow}>> wird eingereiht! Bitte bestätigen oder ändern.\n\nWenn das neue Script eine niedrigere Versionsnummer aufweist wird das Script dauerhauft Loopen." 15 60
			inp2=${?}
			if [ "$inp2" == "0" ]; then
				break
			else
				versionnow=$(dialog --inputbox "Aktuelle Version eingeben:" 15 60  --output-fd 1)
				continue
			fi
		done
		echo $versionnow > $VER_DIR
		upload_update | grep uploaded
		exit 0
	else
		echo "Verschlüsselung fehlgeschlagen!"
		create_update
	fi
}

#Userinput
function usr_inp {
	output=$(dialog --inputbox "Zeitstempel Angeben (YYYY MM DD HH):" 15 60  --output-fd 1)
	code_now
}

function code_now {
	ver_code=$($KEYGEN $output)
	code_output
}

function choose_option {
	auswahl=$(dialog --stdout --backtitle "Bitte Wählen" --title Aktion --radiolist "Für welchen Zeitpunkt soll der Code generiert werden?." 16 60 5 \
		 "JETZT" "Code generieren" on \
		 "SPATER" "Update einreihen" off)
	case "$auswahl" in
		JETZT)
			code_now
			;;
		SPATER)
			usr_inp
			;;
	esac
}

function code_output {
	client_user=$(dialog --inputbox "Generierter Code: \n>>${ver_code}<< \n\nUsernamen angeben (leer für alle):" 15 60  --output-fd 1)
	echo Der Verifizierungscode lautet: [$ver_code]
	if [ "$client_user" == "" ]; then
		client_user=alle
	fi
	create
}

function create {
	clear
	echo verify $ver_code >> $REM_DIR
	echo setuser $client_user >> $REM_DIR
	echo verify $ver_code
	echo setuser $client_user
	get_commands
}

function get_commands {
	while true; do
		unset input
		input=$(dialog --inputbox "Auszuführender Befehl (leer für fertig):" 15 60  --output-fd 1)
		if [ "$input" == "x" ]; then
			clear
			echo Script abgebrochen
			rm $REM_DIR
			exit 0
		elif [ "$input" != "" ];then
			echo $input >> $REM_DIR
			continue
		else
			upload_file | grep uploaded
			exit 0
		fi
	done
}

function upload_file {
	ftp -inv $FTP_HOST << ENDOFUPLOAD
	user $USERFTP $FTP_PW
	del remote.txt
	put $REM_DIR /remote.txt
	bye
ENDOFUPLOAD

	rm $REM_DIR
}

function upload_update {
	cd $HOME/RMF/slave/update/
	ftp -inv $FTP_HOST << ENDOFUPLOAD
	user $USERFTP $FTP_PW
	# del remote_slave.sh.gpg
	# del update.txt
	put $UPDATEFILE /remote_slave.sh.gpg
	put $VER_DIR /update.txt
	bye
ENDOFUPLOAD

	rm $UPDATEFILE
	rm $VER_DIR
}

update_or_run

#EOF

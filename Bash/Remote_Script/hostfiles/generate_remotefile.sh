#!/bin/bash

readonly KEYGEN="$HOME/RMF/keygen.sh"
readonly REM_DIR="$HOME/RMF/slave/update/remote.txt"
readonly VER_DIR="$HOME/RMF/slave/update/update.txt"


#FTP-Zugang:
readonly USERFTP=''
readonly FTP_HOST=''
readonly FTP_PW=''
readonly merk=""
readonly UPDATEFILE="$HOME/RMF/slave/update/remote_slave.sh.gpg"
readonly UPDATE_URL='' 
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
	clear
	echo "Funktion ausführen (1) oder Update planen (2)?"
	read inp
	if [ "$inp" == "1" ]; then
		choose_option
	elif [ "$inp" == "2" ]; then
		create_update
	else
		echo "Eingabe Falsch. Erneut versuchen"
		sleep 5
		update_or_run
	fi
}

#Generiere passwort für Updatefile und definiere Dateipfad
function create_update {
	echo "$updatedir als Ursprung verwenden? (Y/n)"
	read inp2
	if [ "$inp2" == "n" ]; then
		echo "Pfad zur Datei angeben:"
		read updatedir
			if [ -e $updatedir ]; then
				echo "OK"
			else
				echo "File nicht gefunden."
				create_update
			fi
        elif [ "$inp2" != "" ]; then
                if [ "$inp2" != "y" ]; then
                        echo "Eingabe falsch [leer, y, n]"
                        create_update
                fi
        fi

	encrypt_update
}

function encrypt_update {
	export dec_var=$merk
	passit=$($KEYGEN)
	gpg -o $UPDATEFILE --yes --pinentry-mode loopback --passphrase $passit --symmetric $updatedir
	if [ -e $UPDATEFILE ]; then
		echo "Verschlüsselung erfolgreich mit $passit"
		touch $VER_DIR
		echo "ACHTUNG! Update mit der Versionsnummer $versionnow wird eingereiht! Ist dies Korrekt? Stimmt diese Nummer nicht mit der im Script hinterlegten Nummer überein endet das Script in einer Endlosschleife. [Y/n]"
		read dangerinp
		if [ "$dangerinp" != "" ];then 
			echo "Welche Versionsnummer ist aktuell?"
			read versionnow
		fi
		echo $versionnow > $VER_DIR
		upload_update | grep uploaded
	else
		echo "Verschlüsselung fehlgeschlagen!"
		create_update
	fi
}

#Userinput
function usr_inp {
	clear
	echo Yahr:
	read var1
	echo Monat:
	read var2
	echo Tag:
	read var3
	echo Stunde:
	read var4

	var4=$var4$var3
	var3=$var2$var3
	ver_code=$((-1*(3*(var4-var2-var3-(5*var1)))))
	export dec_var=$ver_code
	code_now
}

function code_now {
	ver_code=$($KEYGEN)
	code_output
}

function choose_option {
	clear
	echo 'Code für jetzt [leer] | Code für manuellen Zeitpunkt [1]'
	echo Option eingeben:
	read option

	if [ "$option" == "" ]; then
		code_now
	fi
	if [ "$option" == "1" ]; then
		usr_inp
	else
		echo Eingabe falsch!
		choose_option
	fi
}

function code_output {
	echo Der Verifizierungscode lautet: [$ver_code]
	echo 'Usernamen angeben (leer für alle Clients):'
	read client_user
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
	echo 'Befehl beim Client ausführen (Info: function Exit: Leer Abbrechen: x):'
	get_commands
}

function get_commands {
	echo Befehl eingeben:
	input=''
	read input
	if [ "$input" == "x" ]; then
		clear
		echo Script abgebrochen
		rm $REM_DIR
		exit 0
	elif [ "$input" != "" ];then
		echo $input >> $REM_DIR
		get_commands
	else
		upload_file | grep uploaded
	fi
}

function upload_file {
	ftp -inv $FTP_HOST << ENDOFUPLOAD
	user $USERFTP $FTP_PW
	del remote.txt
	put $REM_DIR /remote.txt
	bye
ENDOFUPLOAD

	rm $REM_DIR
	sleep 5
	clear
	exit 0
}

function upload_update {
	cd $HOME/RMF/slave/update/
	ftp -inv $FTP_HOST << ENDOFUPLOAD
	user $USERFTP $FTP_PW
	put $UPDATEFILE /remote_slave.sh.gpg
	put $VER_DIR /update.txt
	bye
ENDOFUPLOAD

	rm $UPDATEFILE
	rm $VER_DIR
	sleep 5
	clear
	exit 0
}

update_or_run
clear

#EOF

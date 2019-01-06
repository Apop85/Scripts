#!/bin/bash
#Script zum semiautomatischen Einrichten des Raspberry mit PiHole PiVPN FTP DUC und Fail2Ban

#Lese Usernamen aus
iam=$(whoami)

#Color-Codes und Textsfx-Codes
cGREEN="\e[92m"
cRED="\e[31m"
cNOR="\e[0m"
cBLON="\e[5m" #Blinken Ein
cBLOFF="\e[25m" #Blinken Aus

#Funktion Adduser
function addnewuser {
	echo "Bitte gewünschten Usernamen angeben:"
	read uname
	echo -e "Ist der Username [$cGREEN $uname $cNOR] korrekt? [Y/n]"
	read input
	if [ "$input" =! "y" ] || [ "$input" =! "" ]; then
		addnewuser
	fi
	echo "Nutzer $uname wird angelegt"
	adduser $uname
}

#Funktion zur Überprüfung der Privilegien des neuen Users
function userisroot {
	sudocheck=$(cat /etc/sudoers | grep $uname | wc -l)
	if (( $sudocheck < 1 )); then 
		echo -e "$cRED ACHTUNG! $cNOR der angelegte User hat keine Root priviliegen!"
		echo -e "$cRED Script wird geschlossen! $cNOR Bitte Rootrechte manuell eintragen mittels dem Befehl $cGREEN sudo visudo $cNOR. Dort unterhalb von $cRED root    ALL=(ALL:ALL) ALL $cNOR die Zeile $cGREEN $cBLON $uname    ALL=(ALL:ALL) ALL $cBLOFF $cNOR einfügen."
		exit 1
	else
		echo -e "Rootberechtigung für den User $uname: $cGREEN OK $cNOR"
	fi
}

function userisok {
	uname=$iam
	echo -e "Username: $cGREEN $iam $cNOR ist sicher"
	echo "Setze Rootberechtigungen für den neuen User"
	sudocheck=$(cat /etc/sudoers | grep $uname | wc -l)
	if (( $sudocheck = 0 )); then
		usermod -aG sudo $uname
	fi
	userisroot
}

#Neuer User bereits erstellt?
function checkuser {
	if [ "$iam" == "pi" ]; then
		echo -e "Username: $cRED $iam $cNOR"
		echo -e "Der Username ist $cRED unsicher$cNOR. Erstelle neuen User..."
		addnewuser
		if [ -d "/home/$uname" ]; then
			echo "Benutzer $cGREEN $uname $cNOR erfolgreich angelegt."
			userisok
		else
			echo -e "$cRED Fehler! $cNOR User konnte nicht angelegt werden!"
			echo -e "$cRED Script wird abgebrochen, $cNOR bitte User manuell mittels dem Befehl $cGREEN sudo adduser $cBLON USERNAME $cBLOFF $cNOR anlegen."
			exit 1
		fi
	else
		userisok
	fi
}

#Benutzer Pi entfernen. 
function removepiuser {
	loggedin=$(who | grep ^"$uname" | wc -l)
	if (( $loggedin < 1 )); then
		if [ -d "/home/pi" ]; then
			echo -e "Benutzer $cGREEN pi $cNOR ist noch vorhanden. Aus sicherheitsgründen wird dieser entfernt."
			echo -e "$cGREEN raspi-config $cNOR wird gestartet. Bitte unter $cGREEN Bootoptions $cNOR dann $cGREEN Desktop/CLI $cNOR die Einstellung $cRED Console $cNOR wählen"
			echo -e "Ebenfalls gleich zu erledigen $cGREEN Localisation-Options $cNOR die Einstellung auf $cGREEN de-ch UTF 8 $cNOR stellen"
			echo -e "$cRED ACHTUNG! $cNOR Das Gerät wird nach erfolgreicher Anwendung neu gestartet und das Script geschlossen. Script muss nach neustart erneut gestartet werden. $cBLON Danach mit neuem Userdaten anmelden! $cBLOFF"
			echo "Enter zum fortfahren"
			read blank
			raspi-config
		fi
	else
		if [ -d "/home/pi" ]; then
			loggedin=$(who | grep ^"pi" | wc -l)
			# if (( $loggedin > 0 )); then
				# echo -e "$cRED ACHTUNG! $cNOR mit falschem User angemeldet! Bitte mit neuem User anmelden!"
			# fi
			echo -e "User $cGREEN pi $cNOR sowie die entsprechenden Ordner werden entfernt."
			deluser -remove-home pi
		else
			echo -e "User pi: $cGREEN Nicht vorhanden $cNOR"
		fi
	fi
}

function installpihole {
	checkpihole=$(ps ax | grep pihole | wc -l)
	if (( $checkpihole > 1 )); then
		echo -e "PiHole: $cGREEN Installiert $cNOR"
	else
		echo -e "PiHole: $cRED Nicht Installiert $cNOR"
		echo -e "$cGREEN PiHole $cNOR wird installiert"
		curl -sSL https://install.pi-hole.net | bash
		echo -e "$cGREEN Passwort $cNOR für PiHole Webclient eingeben. Leer lassen für kein Passwort"
		pihole -a -p
	fi
}

function installpivpn {
	checkvpn=$(ps ax | grep openvpn | wc -l)
	if (( $checkvpn > 1 )); then
		echo -e "PiVPN: $cGREEN Installiert $cNOR"
	else
		echo -e "PiVPN: $cRED Nicht Installiert $cNOR"
		echo -e "PiVPN: Installation wird gestartet"
		curl -L https://install.pivpn.io | bash
		echo "listen-address=127.0.0.1" >> /etc/dnsmasq.conf
		echo "listen-address=10.8.0.1" >> /etc/dnsmasq.conf
		echo -e "$cRED ACHTUNG! $cNOR Es wird ein Editor geöffnet. Die Zeile welche $cGREEN push dhcp-option $cNOR beinhaltet folgendermassen abändern: $cBLON "'push "dhcp-option DNS 10.8.0.1"'" $cBLOFF ändern"
		echo "Enter zum fortfahren"
		read blank
		nano /etc/openvpn/server.conf
		service dnsmasq restart
		service openvpn restart
	fi
}

function installftp {
	checkftp=$(ps ax | grep ftp | wc -l)
	if (( $checkftp < 1 )); then
		echo -e "FTP Dienst: $cRED Nicht Installiert $cNOR" 
		apt-get install vsftpd -y
		echo -e "Entsprechende Ordner werden erstellt"
		mkdir $HOME/ftp
		mkdir $HOME/ftp/files
		echo -e "Setze Berechtigungen für Ordner."
		chown $uname:$uname $HOME/ftp
		echo -e "Füge benötigte Einträge in vsftpd.conf ein"
		echo "user_sub_token=$uname" >> /etc/vsftpd.conf
		echo "local_root=/home/$uname/ftp" >> /etc/vsftpd.conf
		service vsftpd restart
	else
		echo -e "FTP Dienst: $cGREEN Installiert $cNOR"
	fi
		
}

function installduc {
	checkduc=$(ps ax | grep noip | wc -l)
	if (( $checkduc < 1 )); then 
		echo -e "DUC: $cRED Nicht Installiert $cNOR"
		cd /usr/local/src/
		echo "Downloade DUC Files"
		wget http://www.no-ip.com/client/linux/noip-duclinux.tar.gz
		echo "Entpacke DUC Files"
		tar xf noip-duc-linux.tar.gz
		cd /usr/local/src/noip-*
		echo -e "$cRED ACHTUNG! $cNOR Spätestens jetzt sollte eine Dynamische DNS-Adresse auf http://noip.com erstellt worden sein!"
		echo "Enter zum Fortfahren"
		read blank
		echo "Installiere DUC"
		make install
		echo -e "$cGREEN Zugangsdaten $cNOR für noip.com eingeben:"
		/usr/local/bin/noip2 -C
		echo "Starte DUC Dienst"
		/usr/local/bin/noip2
		ducinitd
		update-rc.d noip2 defaults
	else
		echo -e "DUC: $cGREEN Installiert $cNOR"
	fi
}

function ducinitd {
	touch /etc/init.d/noip2
	echo -e "Init.d Service wird eingerichtet"
	echo '#! /bin/sh' >> /etc/init.d/noip2
	echo '# /etc/init.d/noip2.sh' >> /etc/init.d/noip2
	echo '### BEGIN INIT INFO' >> /etc/init.d/noip2
	echo '# Provides: noip2' >> /etc/init.d/noip2
	echo '# Required-Start: $remote_fs $local_fs' >> /etc/init.d/noip2
	echo '# Required-Stop: $remote_fs $local_fs' >> /etc/init.d/noip2
	echo '# Should-Start:' >> /etc/init.d/noip2
	echo '# Should-Stop:' >> /etc/init.d/noip2
	echo '# Default-Start: 2 3 4 5' >> /etc/init.d/noip2
	echo '# Default-Stop: 0 1 6' >> /etc/init.d/noip2
	echo '# Short-Description: Dynamic IP client updater' >> /etc/init.d/noip2
	echo '# Description:' >> /etc/init.d/noip2
	echo '### END INIT INFO' >> /etc/init.d/noip2
	echo 'DAEMON=/usr/local/bin/noip2' >> /etc/init.d/noip2
	echo 'NAME=noip2' >> /etc/init.d/noip2
	echo 'test -x $DAEMON || exit 0' >> /etc/init.d/noip2
	echo 'case "$1" in' >> /etc/init.d/noip2
	echo '    start)' >> /etc/init.d/noip2
	echo '    echo -n "Starting dynamic address update: "' >> /etc/init.d/noip2
	echo '    start-stop-daemon --start --exec $DAEMON' >> /etc/init.d/noip2
	echo '    echo "noip2."' >> /etc/init.d/noip2
	echo '    ;;' >> /etc/init.d/noip2
	echo '    stop)' >> /etc/init.d/noip2
	echo '    echo -n "Shutting down dynamic address update:"' >> /etc/init.d/noip2
	echo '    start-stop-daemon --stop --oknodo --retry 30 --exec $DAEMON' >> /etc/init.d/noip2
	echo '    echo "noip2."' >> /etc/init.d/noip2
	echo '    ;;' >> /etc/init.d/noip2
	echo '    restart)' >> /etc/init.d/noip2
	echo '    echo -n "Restarting dynamic address update: "' >> /etc/init.d/noip2
	echo '    start-stop-daemon --stop --oknodo --retry 30 --exec $DAEMON' >> /etc/init.d/noip2
	echo '    start-stop-daemon --start --exec $DAEMON' >> /etc/init.d/noip2
	echo '    echo "noip2."' >> /etc/init.d/noip2
	echo '    ;;' >> /etc/init.d/noip2
	echo '    *)' >> /etc/init.d/noip2
	echo '    echo "Usage: $0 {start|stop|restart}"' >> /etc/init.d/noip2
	echo '    exit 1' >> /etc/init.d/noip2
	echo 'esac' >> /etc/init.d/noip2
	echo 'exit 0' >> /etc/init.d/noip2
	chmod +x /etc/init.d/noip2
}

function installf2b {
	checkf2b=$(ps ax | grep fail2ban | wc -l)
	if (( $checkf2b < 1 )); then
		echo -e "Fail2Ban: $cRED Nicht Installiert $cNOR"
		echo "Installiere Fail2Ban"
		apt-get install fail2ban -y
		cp /etc/fail2ban/jail.conf /etc/fail2ban/jail.local
		echo "Folgende Einträge anpassen: $cGREEN ignoreip, bantime, findtime, maxretry $cNOR"
		echo "Enter zum fortfahren"
		read blank
		nano /etc/fail2ban/jail.local
		systemctl restart fail2ban.service
	else
		echo -e "Fail2Ban: $cGREEN Installiert $cNOR"
	fi	
}

function abschluss {
	clear
	if (( $(ps ax | grep "pihole" | wc -l) > 1 )); then
		echo -e "PiHole: $cGREEN Installiert $cNOR"
	else
		echo -e "PiHole: $cRED Nicht Installiert! $cNOR"
	fi
	
	if (( $(ps ax | grep "openvpn" | wc -l) > 1 )); then
		echo -e "PiVPN: $cGREEN Installiert $cNOR"
	else
		echo -e "PiVPN: $cRED Nicht Installiert! $cNOR"
	fi
	
	if (( $(ps ax | grep "ftp" | wc -l) > 1 )); then
		echo -e "FTP-Server: $cGREEN Installiert $cNOR"
	else
		echo -e "FTP-Server: $cRED Nicht Installiert! $cNOR"
	fi	
	
	if (( $(ps ax | grep "noip" | wc -l) > 1 )); then
		echo -e "DUC: $cGREEN Installiert $cNOR"
	else
		echo -e "DUC: $cRED Nicht Installiert! $cNOR"
	fi	
	
	if (( $(ps ax | grep "fail2ban" | wc -l) > 1 )); then
		echo -e "Fail2Ban: $cGREEN Installiert $cNOR"
	else
		echo -e "Fail2Ban: $cRED Nicht Installiert! $cNOR"
	fi
	echo "Enter zum fortfahren"
	clear 
	echo -e "$cGREEN Die Installation ist abgeschlossen $cNOR"
}

#Überprüfe ob das Script Rootprivilegien besitzt.
if [ "$EUID" -ne 0 ]; then 
	echo -e "Rootberechtigung $cRED Nein $cNOR"
	sudo -i
else
	echo -e "Rootberechtigung \e[92mOK\e[0m"
fi

#Updated installieren falls vorhanden. 
newupdates=$(apt-get -q -y --ignore-hold --allow-unauthenticated -s upgrade | grep ^Inst | cut -d\  -f2 | wc -l)

if (( $newupdates =! 0 )); then
	echo -e "$cRED Updates sind vorhanden! $cNOR Die Entsprechenden Updates werden vor dem fortfahren installiert"
	sleep 3
	apt-get update
	apt-get upgrade -y
else
	echo -e "Updates vorhanden: $cGREEN Nein $cNOR"
fi	


checkuser
removepiuser
installpihole
installpivpn
installduc
installf2b
abschluss

exit 0

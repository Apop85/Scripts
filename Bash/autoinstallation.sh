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


# _____ _   _____________ _   _ _   _ _____ _____ _____ _____ _   _  _____ 
#/  ___| | | | ___ \  ___| | | | \ | /  __ \_   _|_   _|  _  | \ | |/  ___|
#\ `--.| | | | |_/ / |_  | | | |  \| | /  \/ | |   | | | | | |  \| |\ `--. 
# `--. \ | | | ___ \  _| | | | | . ` | |     | |   | | | | | | . ` | `--. \
#/\__/ / |_| | |_/ / |   | |_| | |\  | \__/\ | |  _| |_\ \_/ / |\  |/\__/ /
#\____/ \___/\____/\_|    \___/\_| \_/\____/ \_/  \___/ \___/\_| \_/\____/

function chownit {
	echo -e "Ändere Zugriffsberechtigungen von $target"
	chown $uname:$uname $target
}

function chmodit {
	echo "$target wird Ausführbar gemacht"
	chmod +x $target
}

function showerror {
	echo -e "$name $cRED nicht vorhanden! $cNOR"
}

function showok {
	echo -e "$name $cGREEN Vorhanden $cNOR"
}


#___  ___  ___  _____ _   _ ______ _   _ _   _ _____ _____ _____ _____ _   _  _____ 
#|  \/  | / _ \|_   _| \ | ||  ___| | | | \ | /  __ \_   _|_   _|  _  | \ | |/  ___|
#| .  . |/ /_\ \ | | |  \| || |_  | | | |  \| | /  \/ | |   | | | | | |  \| |\ `--. 
#| |\/| ||  _  | | | | . ` ||  _| | | | | . ` | |     | |   | | | | | | . ` | `--. \
#| |  | || | | |_| |_| |\  || |   | |_| | |\  | \__/\ | |  _| |_\ \_/ / |\  |/\__/ /
#\_|  |_/\_| |_/\___/\_| \_/\_|    \___/\_| \_/\____/ \_/  \___/ \___/\_| \_/\____/ 
#                                      _____             
#                                     / __  \            
# _ __  _ __ ___ _ __   __ _ _ __ ___ `' / /' __ _  ___  
#| '_ \| '__/ _ \ '_ \ / _` | '__/ _ \  / /  / _` |/ _ \ 
#| |_) | | |  __/ |_) | (_| | | |  __/./ /__| (_| | (_) |
#| .__/|_|  \___| .__/ \__,_|_|  \___|\_____/\__, |\___/ 
#| |            | |                           __/ |      
#|_|            |_|                          |___/ 
#Teste ob Script Rootberechtigung besitzt und ob Updates vorhanden sind
function prepare2go {
	#Überprüfe ob das Script Rootprivilegien besitzt.
	name="Rootberechtigung"
	if [ "$EUID" -ne 0 ]; then 
		showerror
		sudo -i
	else
		showok
	fi

	#Installiere vorhandene updates
	newupdates=$(apt-get -q -y --ignore-hold --allow-unauthenticated -s upgrade | grep ^Inst | cut -d\  -f2 | wc -l)

	if (( $newupdates =! 0 )); then
		echo -e "$cRED $newupdates Updates sind vorhanden! $cNOR Die Entsprechenden Updates werden vor dem fortfahren installiert"
		sleep 3
		apt-get update
		apt-get upgrade -y
	else
		echo -e "Updates vorhanden: $cGREEN Nein $cNOR"
	fi	
}

#      _               _                        
#     | |             | |                       
#  ___| |__   ___  ___| | ___   _ ___  ___ _ __ 
# / __| '_ \ / _ \/ __| |/ / | | / __|/ _ \ '__|
#| (__| | | |  __/ (__|   <| |_| \__ \  __/ |   
# \___|_| |_|\___|\___|_|\_\\__,_|___/\___|_|  
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

#Funktion zur Überprüfung der Privilegien des neuen Users
function userisroot {
	name="Rootberechtigung für den User $uname:"
	sudocheck=$(cat /etc/sudoers | grep $uname | wc -l)
	if (( $sudocheck < 1 )); then 
		showerror
		echo -e "$cRED Script wird geschlossen! $cNOR Bitte Rootrechte manuell eintragen mittels dem Befehl $cGREEN sudo visudo $cNOR. Dort unterhalb von $cRED root    ALL=(ALL:ALL) ALL $cNOR die Zeile $cGREEN $cBLON $uname    ALL=(ALL:ALL) ALL $cBLOFF $cNOR einfügen."
		exit 1
	else
		showok
	fi
}

#                                         _                     
#                                        (_)                    
# _ __ ___ _ __ ___   _____   _____ _ __  _ _   _ ___  ___ _ __ 
#| '__/ _ \ '_ ` _ \ / _ \ \ / / _ \ '_ \| | | | / __|/ _ \ '__|
#| | |  __/ | | | | | (_) \ V /  __/ |_) | | |_| \__ \  __/ |   
#|_|  \___|_| |_| |_|\___/ \_/ \___| .__/|_|\__,_|___/\___|_|   
#                                  | |                          
#                                  |_| 
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

# _           _        _ _       _ _           _      
#(_)         | |      | | |     (_) |         | |     
# _ _ __  ___| |_ __ _| | |_ __  _| |__   ___ | | ___ 
#| | '_ \/ __| __/ _` | | | '_ \| | '_ \ / _ \| |/ _ \
#| | | | \__ \ || (_| | | | |_) | | | | | (_) | |  __/
#|_|_| |_|___/\__\__,_|_|_| .__/|_|_| |_|\___/|_|\___|
#                         | |                         
#                         |_|
#Falls noch nicht vorhanden PiHole installieren
function installpihole {
	name="PiHole:"
	checkpihole=$(ps ax | grep pihole | wc -l)
	if (( $checkpihole > 1 )); then
		showok
	else
		showerror
		echo -e "$cGREEN PiHole $cNOR wird installiert"
		curl -sSL https://install.pi-hole.net | bash
		echo -e "$cGREEN Passwort $cNOR für PiHole Webclient eingeben. Leer lassen für kein Passwort"
		pihole -a -p
	fi
}


# _           _        _ _       _                   
#(_)         | |      | | |     (_)                  
# _ _ __  ___| |_ __ _| | |_ __  ___   ___ __  _ __  
#| | '_ \/ __| __/ _` | | | '_ \| \ \ / / '_ \| '_ \ 
#| | | | \__ \ || (_| | | | |_) | |\ V /| |_) | | | |
#|_|_| |_|___/\__\__,_|_|_| .__/|_| \_/ | .__/|_| |_|
#                         | |           | |          
#                         |_|           |_|  
#Falls noch nicht vorhanden PiVPN installieren
function installpivpn {
	name="PiVPN:"
	checkvpn=$(ps ax | grep openvpn | wc -l)
	if (( $checkvpn > 1 )); then
		showok
	else
		showerror
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


# _           _        _ _  __ _         
#(_)         | |      | | |/ _| |        
# _ _ __  ___| |_ __ _| | | |_| |_ _ __  
#| | '_ \/ __| __/ _` | | |  _| __| '_ \ 
#| | | | \__ \ || (_| | | | | | |_| |_) |
#|_|_| |_|___/\__\__,_|_|_|_|  \__| .__/ 
#                                 | |    
#                                 |_| 
#Falls noch nicht vorhanden FTP Service installieren.
function installftp {
	name="FTP Dienst:"
	target="$HOME/ftp"
	checkftp=$(ps ax | grep ftp | wc -l)
	if (( $checkftp < 1 )); then
		showerror
		apt-get install vsftpd -y
		echo -e "Entsprechende Ordner werden erstellt"
		mkdir $target
		mkdir $target/files
		chownit
		echo -e "Füge benötigte Einträge in vsftpd.conf ein"
		echo "user_sub_token=$uname" >> /etc/vsftpd.conf
		echo "local_root=/home/$uname/ftp" >> /etc/vsftpd.conf
		service vsftpd restart
	else
		showok
	fi
		
}

# _           _        _ _     _            
#(_)         | |      | | |   | |           
# _ _ __  ___| |_ __ _| | | __| |_   _  ___ 
#| | '_ \/ __| __/ _` | | |/ _` | | | |/ __|
#| | | | \__ \ || (_| | | | (_| | |_| | (__ 
#|_|_| |_|___/\__\__,_|_|_|\__,_|\__,_|\___|
#Falls noch nicht vorhanden DUC für noip.com installieren. 
function installduc {
	name="DUC:"
	checkduc=$(ps ax | grep noip | wc -l)
	if (( $checkduc < 1 )); then 
		showerror
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
		showok
	fi
}

#Erstelle noip2 script in /etc/init.d/
function ducinitd {
	name="Init.d Eintrag für DUC:"
	target="/etc/init.d/noip2"
	if [ -e $target ]; then
		showok
	else
		showerror	
		touch $target
		echo -e "Init.d Service wird eingerichtet"
		echo '#! /bin/sh' >>$target
		echo '# /etc/init.d/noip2.sh' >> $target
		echo '### BEGIN INIT INFO' >> $target
		echo '# Provides: noip2' >> $target
		echo '# Required-Start: $remote_fs $local_fs' >> $target
		echo '# Required-Stop: $remote_fs $local_fs' >> $target
		echo '# Should-Start:' >> $target
		echo '# Should-Stop:' >> $target
		echo '# Default-Start: 2 3 4 5' >> $target
		echo '# Default-Stop: 0 1 6' >> $target
		echo '# Short-Description: Dynamic IP client updater' >> $target
		echo '# Description:' >> $target
		echo '### END INIT INFO' >> $target
		echo 'DAEMON=/usr/local/bin/noip2' >> $target
		echo 'NAME=noip2' >> $target
		echo 'test -x $DAEMON || exit 0' >> $target
		echo 'case "$1" in' >> $target
		echo '    start)' >> $target
		echo '    echo -n "Starting dynamic address update: "' >> $target
		echo '    start-stop-daemon --start --exec $DAEMON' >> $target
		echo '    echo "noip2."' >> $target
		echo '    ;;' >> $target
		echo '    stop)' >> $target
		echo '    echo -n "Shutting down dynamic address update:"' >> $target
		echo '    start-stop-daemon --stop --oknodo --retry 30 --exec $DAEMON' >> $target
		echo '    echo "noip2."' >> $target
		echo '    ;;' >> $target
		echo '    restart)' >> $target
		echo '    echo -n "Restarting dynamic address update: "' >> $target
		echo '    start-stop-daemon --stop --oknodo --retry 30 --exec $DAEMON' >> $target
		echo '    start-stop-daemon --start --exec $DAEMON' >> $target
		echo '    echo "noip2."' >> $target
		echo '    ;;' >> $target
		echo '    *)' >> $target
		echo '    echo "Usage: $0 {start|stop|restart}"' >> $target
		echo '    exit 1' >> $target
		echo 'esac' >> $target
		echo 'exit 0' >> $target
		chmodit
	fi
}

# _           _        _ _  __ _____  _     
#(_)         | |      | | |/ _/ __  \| |    
# _ _ __  ___| |_ __ _| | | |_`' / /'| |__  
#| | '_ \/ __| __/ _` | | |  _| / /  | '_ \ 
#| | | | \__ \ || (_| | | | | ./ /___| |_) |
#|_|_| |_|___/\__\__,_|_|_|_| \_____/|_.__/
function installf2b {
	checkf2b=$(ps ax | grep fail2ban | wc -l)
	name="Fail2Ban:"
	if (( $checkf2b < 1 )); then
		showerror
		echo "Installiere Fail2Ban"
		apt-get install fail2ban -y
		cp /etc/fail2ban/jail.conf /etc/fail2ban/jail.local
		echo "$cRED Editor wird geöffnet! $cNOR Folgende Einträge anpassen: $cGREEN ignoreip, bantime, findtime, maxretry $cNOR"
		echo "Enter zum fortfahren"
		read blank
		nano /etc/fail2ban/jail.local
		systemctl restart fail2ban.service
	else
		showok
	fi	
}

#       _              _     _               
#      | |            | |   | |              
#  __ _| |__  ___  ___| |__ | |_   _ ___ ___ 
# / _` | '_ \/ __|/ __| '_ \| | | | / __/ __|
#| (_| | |_) \__ \ (__| | | | | |_| \__ \__ \
# \__,_|_.__/|___/\___|_| |_|_|\__,_|___/___/
#Abschluss des Installationsscripts
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
	echo -e "$cGREEN Die Installation ist abgeschlossen. Starte Konfiguration $cNOR"
}

#                  __ _           _          __  __ 
#                 / _(_)         | |        / _|/ _|
#  ___ ___  _ __ | |_ _  __ _ ___| |_ _   _| |_| |_ 
# / __/ _ \| '_ \|  _| |/ _` / __| __| | | |  _|  _|
#| (_| (_) | | | | | | | (_| \__ \ |_| |_| | | | |  
# \___\___/|_| |_|_| |_|\__, |___/\__|\__,_|_| |_|  
#                        __/ |                      
#                       |___/  
#Erstelle für die Scripts benötigte Dateien und Ordner
function configstuff {
	name="Scriptsordner:"
	target="$HOME/scripts"
	#Prüfe vorhandensein des Scriptordners
	if [ -d $target; then
		showok
	else
		showerror
		echo -e "Scriptordner wird angelegt unter $target"
		mkdir $target
		chownit
	fi

	#Prüfe vorhandensein von telegram.inf
	name="Telegram.inf:"
	target="$HOME/scripts/telegram.inf"
	if [ -e $target ]; then
		showok
	else
		showerror
		touch $target
		chownit
		echo "BOT_ID=" >> $target
		echo "CHAT_ID=" >> $target
	fi

	#Prüfe vorhandensein von user.inf
	name="User.inf:"
	target="$HOME/scripts/user.inf"
	if [ -e $target ]
		showok
	else
		showerror
		echo -e "User.inf wird unter $target erstellt."
		touch $target
		chownit
		echo "user=$uname" >> $target
	fi
}


#            _                 _       _       
#           | |               (_)     | |      
#  __ _  ___| |_ ___  ___ _ __ _ _ __ | |_ ___ 
# / _` |/ _ \ __/ __|/ __| '__| | '_ \| __/ __|
#| (_| |  __/ |_\__ \ (__| |  | | |_) | |_\__ \
# \__, |\___|\__|___/\___|_|  |_| .__/ \__|___/
#  __/ |                        | |            
# |___/                         |_|
#Downloade gewünschte Scripts
function getscripts {
	cd $HOME/scripts
	cmd=(dialog --separate-output --checklist "Wähle zu verwendende Scripts aus:" 22 76 16)
	options=(1 "Boot Benachrichtigung" off    # any option can be set to default to "on"
			 2 "Updatecheck" off
			 3 "VPN Zertifikaterneuerung" off
			 4 "Benachrichtigung wenn Mail von noip eintrifft" off
			 5 "Benachricgtigung wenn öffentliche IP wechselt" off
			 6 "Benachrichtigung bei Neujahr" off
			 7 "Benachrichtigung bei eingehender VPN Verbindung" off
			 8 "Benachrichtigung wenn Fail2Ban eine IP blockt/entblockt" off
			 9 "Command & Control Script (slave)" off
			 10 "Logcleaner" off)
	choices=$("${cmd[@]}" "${options[@]}" 2>&1 >/dev/tty)
	clear
	for choice in $choices
	do
		case $choice in
			1)
				target="$HOME/scripts/startup.sh"
				name="startup.sh:"
				if [ -e $target ]; then
					showok
				else
					showerror
					echo "Bootbenachrichtigungsscript wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/startup.sh
					chownit
					chmodit
				fi
				;;
			2)
				name="Updatecheck:"
				target="$HOME/scripts/updatecheck.sh"
				if [ -e $target ]: then
					showok
				else
					showerror
					echo "Updatecheck wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/updatecheck.sh
					chownit
					chmodit
				fi
				;;
			3)
				name="VPN Zertifikatserneuerungsscript:"
				target="$HOME/scripts/VPN_renew_server_cert.sh"
				if [ -e $target ]; then
					showok
				else
					showerror
					echo "VPN Zertifikatserneuerungsscript wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/VPN_renew_server_cert.sh
					chownit
					chmodit
				fi
				;;
			4)
				name="Checkpublicip:"
				target="$HOME/scripts/checkpublicip.sh"				
				if [ -e $target ]; then
					showok
				else
					showerror
					echo "Script zur überprüfung der öffentlichen IP wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/checkpublicip.sh
					chownit
					chmodit
				fi
				;;
			5)
				name="Checkmail:"
				target="$HOME/scripts/checkmail2.py"				
				if [ -e $target ]; then
					showok
				else
					showerror
					echo "Benachrichtigungsscript für eingehende Mails von Noip wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Python/checkmail2.py
					chownit
					chmodit
				fi
				;;
			6)
				name="Happy New Year Script:"
				target="$HOME/scripts/Happy_New_Year.sh"				
				if [ -e $target ]; then
					showok
				else
					showerror
					echo "Happy New Year Script wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/Happy_New_Year.sh
					chownit
					chmodit
				fi
				;;
			7)
				name="VPNconnect:"
				target="$HOME/scripts/VPNconnect.sh"
				if [ -e $target ]; then
					showok
				else
					showerror
					echo "Benachrichtigungsscript für eingehende VPN-Verbindungen wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/VPNconnect.sh
					chownit
					chmodit
				fi
				;;
			8)
				name="Fail2Ban Benachrichtigungen:"
				target="$HOME/scripts/f2b_info.sh"
				if [ -e $target ]; then
					showok
				else	
					showerror
					echo "Benachrichtigungsscript für Fail2Ban wird gedownloaded"
					wget https://github.com/Apop85/Scripts/blob/master/Bash/f2b_info.sh
					chownit
					chmodit
				fi
				;;
			9)
				name="Remote Slave Ordner:"
				target="$HOME/scripts/remote"
				if [ -d $target ]; then
					showok
				else
					showerror
					echo -e "Erstelle remote Ordner $target"
					mkdir $target
					chownit
				fi
				cd $target
				
				name="Remote Slave:"
				target="$HOME/scripts/remote/remote_slave.sh"
				if [ -e $target ]; then
					showok
				else
					showerror
					echo "Command & Controlscript (slave) wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/Remote_Script/remote_slave.sh
					chownit
					chmodit
				fi
				
				echo "Übrige Remotescripts werden gedownloaded."
				
				name="remote_reboot:"
				target="$HOME/scripts/remote/remote_reboot.sh"
				if [ -e $target ]; then
					showok
				else
					showerror
					echo -e "Downloade $name"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/Remote_Script/remote_reboot.sh
					chmodit
				fi
				
				name="remote_update"
				target="$HOME/scripts/remote/remote_update.sh"
				if [ -e $target ]; then 
					showok
				else
					showerror
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/Remote_Script/remote_update.sh
					chmodit
				fi
				
				name="updateclient"
				target="$HOME/scripts/remote/updateclient.sh"
				if [ -e $target ]; then
					showok
				else
					showerror
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/Remote_Script/updateclient.sh
					chownit
					chmodit
				fi
				cd $HOME/scripts
				;;
			10)
				name="Logcleaner"
				target="$HOME/scripts/logcleaner.sh"				
				if [ -e $target ]; then
					showok
				else
					showerror
					echo "Logcleaner wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/logcleaner.sh
					chownit
					chmodit
				fi
				;;
		esac
	done
}

# _____ _   _______   ___________  ______ _   _ _   _ _____ _____ _____ _____ _   _  _____ 
#|  ___| \ | |  _  \ |  _  |  ___| |  ___| | | | \ | /  __ \_   _|_   _|  _  | \ | |/  ___|
#| |__ |  \| | | | | | | | | |_    | |_  | | | |  \| | /  \/ | |   | | | | | |  \| |\ `--. 
#|  __|| . ` | | | | | | | |  _|   |  _| | | | | . ` | |     | |   | | | | | | . ` | `--. \
#| |___| |\  | |/ /  \ \_/ / |     | |   | |_| | |\  | \__/\ | |  _| |_\ \_/ / |\  |/\__/ /
#\____/\_| \_/___/    \___/\_|     \_|    \___/\_| \_/\____/ \_/  \___/ \___/\_| \_/\____/ 
                                                                                         


#~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
#~~~~~~~~~~~~~~~~~~~~~~~~~~ Führe Funktionen aus ~~~~~~~~~~~~~~~~~~~~~~~
#~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

prepare2go
checkuser
removepiuser
installpihole
installpivpn
installduc
installf2b
abschluss
configstuff
getscripts

exit 0

#!/bin/bash
#Script zum semiautomatischen Einrichten des Raspberry mit PiHole PiVPN FTP DUC und Fail2Ban

#Noch zu prüfen:
#Installation von PiVPN nicht möglich auf VM
#VPN User erstellen auf VM-Maschine nicht möglich da PiVPN inkompatibel mit Version 
#Autologin wechseln auf VM nicht möglich
#DUC init.d file wird bei download verwurschtelt. 


#Color-Codes und Textsfx-Codes
cGREEN="\e[92m"
cRED="\e[31m"
cNOR="\e[0m" #Reset to default
cBLON="\e[5m" #Blinken Ein
cBLOFF="\e[25m"
cINVON="\e[7m" #Invertiert
cINVOFF="\e[27m"
cBLU="\e[94m"
cBOON="\e[1m" # Fett/Weiss
cWHITBG="\e[107m"
wait4input="${cINVON}${cBLU}[EINGABE ERFORDERLICH]${cNOR}${cINVOFF}"
info="${cINVON}[INFO]${cINVOFF}"
errorout="${cRED}${cINVON}[ERROR]${cINVOFF}${cNOR}"

#Lese Usernamen aus
iam=$(who am i | awk '{print $1}')
if [ "$iam" == "" ]; then
	iam=$(who | awk '{print $1}')
	if [ "$iam" == "" ]; then
		echo -e "$errorout Benutzername konnte nicht festgestellt werden!"
		echo -e "$errorout Script wird abgebrochen!"
		exit 1
	fi
fi

# _____ _   _____________ _   _ _   _ _____ _____ _____ _____ _   _  _____ 
#/  ___| | | | ___ \  ___| | | | \ | /  __ \_   _|_   _|  _  | \ | |/  ___|
#\ `--.| | | | |_/ / |_  | | | |  \| | /  \/ | |   | | | | | |  \| |\ `--. 
# `--. \ | | | ___ \  _| | | | | . ` | |     | |   | | | | | | . ` | `--. \
#/\__/ / |_| | |_/ / |   | |_| | |\  | \__/\ | |  _| |_\ \_/ / |\  |/\__/ /
#\____/ \___/\____/\_|    \___/\_| \_/\____/ \_/  \___/ \___/\_| \_/\____/

function chownit {
	echo -e "${info} Ändere Zugriffsberechtigungen von $target"
	chown $uname:$uname $target
}

function chmodit {
	echo -e "${info} $target wird Ausführbar gemacht"
	chmod +x $target
}

function showerror {
	echo -e "${info} $name ${cRED}nicht vorhanden!${cNOR}"
}

function showok {
	echo -e "${info} $name ${cGREEN}Vorhanden${cNOR}"
}

function wait4it {
	echo -e "${wait4input} ${cGREEN}Enter${cNOR} zum fortfahren" 
	read blank
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
		[ `whoami` = root ] || exec sudo $0
	else
		showok
	fi
	
	export LANG=de_CH.UTF-8
	
	#Installiere vorhandene updates
	newupdates=0
	newupdates=$(apt-get -q -y --ignore-hold --allow-unauthenticated -s upgrade | grep ^Inst | cut -d\  -f2 | wc -l)

	if (( $newupdates > 0 )); then
		echo -e "${info} ${cRED}$newupdates Updates sind vorhanden!${cNOR} Die entsprechenden Updates werden vor dem fortfahren installiert"
		echo -e "${info} ${cBLON}Die Installation kann eine Weile dauern. Bitte warten...${cBLOFF}"
		apt-get update >/dev/null 2>&1
		echo -e "${info} Schritt ${cRED}1${cNOR} von 2 abgeschlossen."
		apt-get upgrade -y >/dev/null 2>&1
		echo -e "${info} ${cGREEN}Installation der Updates abgeschlossen${cNOR}"
	else
		echo -e "${info} Updates vorhanden: ${cGREEN}Nein${cNOR}"
	fi
	
	echo -e "${info} Vorausgesetzte Pakete werden gegebenenfalls installiert."
	apt-get install dialog make gcc tar curl gpg -y >/dev/null 2>&1
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
		echo -e "${info} Username: ${cRED}${iam}${cNOR}"
		echo -e "${info} Der Username ist ${cRED}unsicher${cNOR}. Erstelle neuen User..."
		addnewuser
		if [ -d "/home/$uname" ]; then
			echo -e "${info} Benutzer ${cGREEN}${uname}${cNOR} erfolgreich angelegt."
			userisok
		else
			echo -e "${errorout} ${cRED}Fehler!${cNOR} User konnte nicht angelegt werden!"
			echo -e "${errorout} ${cRED}Script wird abgebrochen,${cNOR} bitte User manuell mittels dem Befehl ${cGREEN}sudo adduser ${cINVON}USERNAME${cINVOFF}${cNOR} anlegen. Danach Script neu starten."
			exit 1
		fi
	else
		userisok
	fi
}

#Funktion zum Anlegen eines neuen Users
function addnewuser {
	while true; do
		echo -e "${wait4input} Bitte gewünschten Usernamen angeben:"  
		read uname
		echo -e "${wait4input} Ist der Username [${cGREEN}$uname${cNOR}] korrekt? [Y/n]" 
		read input
		if [ "$input" == "y" -o "$input" == "" ]; then
			break
		fi
	done
	echo -e "${info} Nutzer $uname wird angelegt"
	adduser $uname
}

#Überprüfe Usernamen und falls noch Pi setze Rootberechtigungen für den neuen User
function userisok {
	if [ "$iam" != "pi" ]; then
		echo -e "${info} Username: ${cGREEN}${iam}${cNOR} ist sicher"
		uname=$iam
	else
		echo -e "${info} Setze Rootberechtigungen für den neuen User"
		sudocheck=$(cat /etc/sudoers | grep $uname | wc -l)
		if (( $sudocheck == 0 )); then
			echo "$uname  ALL=(ALL:ALL) ALL" >> /etc/sudoers
		fi
	fi
	userisroot
}

#Funktion zur Überprüfung der Privilegien des neuen Users
function userisroot {
	name="Rootberechtigung für den User $uname:"
	sudocheck=$(cat /etc/sudoers | grep $uname | wc -l)
	if (( $sudocheck < 1 )); then 
		showerror
		echo -e "${errorout} ${cRED}Script wird geschlossen!${cNOR} Bitte Rootrechte manuell eintragen mittels dem Befehl ${cGREEN}sudo visudo${cNOR}. Dort unterhalb von ${cRED}root    ALL=(ALL:ALL) ALL${cNOR} die Zeile ${cGREEN}${cINVON}$uname    ALL=(ALL:ALL) ALL${cINVOFF}${cNOR} einfügen."
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
#Benutzer Pi entfernen sowie Autologin, Lokalisation und Zeitzone setzen. 
function removepiuser {
	loggedin=$(who | grep ^"$uname" | wc -l)
	if (( $loggedin < 1 )); then
		if [ -d "/home/pi" ]; then
			echo -e "${info} Benutzer ${cGREEN}pi${cNOR} ist noch vorhanden. Aus sicherheitsgründen wird dieser entfernt."
			echo -e "${info} Autologin für Benutzer ${cGREEN}pi${cNOR} wird auf Benutzer ${cGREEN}$uname${cNOR} geändert."
			#Ändere Autologin von Pi zu neuem User
			sed -i '/autologin-user=\(.*\)/c\autologin-user=$uname' /etc/lightdm/lightdm.conf
			echo -e "${info} Die Lokalisationseinstellung wird auf ${cGREEN}de-ch UTF 8${cNOR} gestellt"
			echo -e "${info} ${cRED}ACHTUNG!${cNOR} Das Gerät wird nach erfolgreicher Anwendung neu gestartet und das Script geschlossen. Script muss nach neustart erneut manuell gestartet werden. ${cBLON}Nach Neustart mit neuen Userdaten anmelden!${cBLOFF}"
			wait4it
			#Lokalisation auf de_CH UTF-8 wechseln. 
			echo -e "${info} Wechsle Layout zu ${cGREEN}de_CH.UTF-8${cNOR}"
			update-locale LANG=de_CH.UTF-8
			echo -e "${info} Wende neue Lokalisation auf vorhandene Anwendungen an"
			locale-gen --purge "de_CH.UTF-8" >/dev/null 2>&1
			dpkg-reconfigure --frontend noninteractive locales >/dev/null 2>&1
			#Setze Zeitzone auf Europe/Zurich
			echo -e "${info} Setze lokale Zeitzone auf ${cGREEN}Europa/Zürich${cNOR}"
			timezonenow=$(cat /etc/timezone)
			timezone="Europa/Zurich"
			if [ "$timezonenow" =! "timezone" ]; then
				echo $timezone > /etc/timezone
				cp /usr/share/zoneinfo/${TIMEZONE} /etc/localtime
				systemctl restart systemd-timesyncd.service
			fi
			echo -e "${info} ${cRED}REBOOT IN 5 SEKUNDEN${cNOR}"
			sleep 5
			reboot
		fi
	else
		#User Pi, falls noch vorhanden, entfernen. 
		if [ -d "/home/pi" ]; then
			loggedin=$(who | grep ^"pi" | wc -l)
			echo -e "${info} User ${cGREEN}pi${cNOR} sowie die entsprechenden Ordner werden entfernt."
			deluser -remove-home pi
		else
			echo -e "${info} User pi: ${cGREEN}Nicht vorhanden${cNOR}"
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
		echo -e "${info} ${cGREEN}PiHole${cNOR} wird installiert"
		curl -sSL https://install.pi-hole.net | bash
		echo -e "${info} ${cGREEN}Passwort${cNOR} für PiHole Webclient eingeben. Leer lassen für kein Passwort"
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
		echo -e "${info} PiVPN: Installation wird gestartet"
		curl -L https://install.pivpn.io | bash
		echo "listen-address=127.0.0.1" >> /etc/dnsmasq.conf
		echo "listen-address=10.8.0.1" >> /etc/dnsmasq.conf
		sed -i '/push "dhcp-option\(.*\)/c\push "dhcp-option DNS 10.8.0.1"' /etc/openvpn/server.conf
		countopt=$(cat /etc/openvpn/server.conf | grep 'push "dhcp-option DNS' | wc -l)
		if (( $countopt != 1 )); then
			if (( $countopt == 0 )); then
				echo 'push "dhcp-option DNS 10.8.0.1"' >> /etc/openvpn/server.conf
			else
				echo -e "${errorout} ${cRED}ACHTUNG!${cNOR} Es sind $countopt Einträge mit der Option${cRED}"'push "dhcp-option DNS 10.8.0.1"'"${cNOR} vorhanden. Bitte manuell ${cBLON}alle bis auf einen${cBLOFF} Eintrag löschen. Editor wird geöffnet..."
				wait4it
				nano /etc/openvpn/server.conf
			fi
		else
			echo -e "${info} Der Eintrag "'push "dhcp-option DNS 10.8.0.1"'" ist ordnungsgemäss."
		fi
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
	if (( $checkftp < 2 )); then
		showerror
		apt-get install vsftpd -y >/dev/null 2>&1
		echo -e "${info} Entsprechende Ordner werden erstellt"
		mkdir $target
		mkdir $target/files
		chownit
		echo -e "${info} Füge benötigte Einträge in vsftpd.conf ein"
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
	if (( $checkduc < 2 )); then 
		showerror
		cd /usr/local/src/
		echo -e "${info} Downloade DUC Files"
		wget http://www.no-ip.com/client/linux/noip-duc-linux.tar.gz >/dev/null 2>&1
		echo -e "${info} Entpacke DUC Files"
		tar xf noip-duc-linux.tar.gz
		rm noip-duc-linux.tar.gz
		cd /usr/local/src/noip-*
		echo -e "${info} ${cRED}ACHTUNG!${cNOR} Spätestens jetzt sollte eine Dynamische DNS-Adresse auf http://noip.com erstellt worden sein!"
		wait4it
		echo -e "${info} Installiere DUC"
		make install
		ducinitd
#		echo -e "${info} ${cGREEN}Zugangsdaten${cNOR} für noip.com eingeben:"
#		/usr/local/bin/noip2 -C
		echo -e "${info} Starte DUC Dienst"
		update-rc.d noip2 defaults
		systemctl daemon-reload
		systemctl restart noip2.service
		cd $HOME
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
		echo -e "${info} noip2 wird gedownloaded"
		tmpfile=$(/bin/mktemp)
		wget -q -O $tmpfile https://raw.githubusercontent.com/Apop85/Scripts/master/Raspberry-Files/noip2 >/dev/null 2>&1
		mv $tmpfile $target
		chown root:root $target
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
	if (( $checkf2b < 2 )); then
		showerror
		echo -e "${info} Installiere Fail2Ban"
		apt-get install fail2ban -y >/dev/null 2>&1
		cp /etc/fail2ban/jail.conf /etc/fail2ban/jail.local
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
	name="PiHole:"
	if (( $(ps ax | grep "pihole" | wc -l) > 1 )); then
		showok
	else
		showerror
	fi
	
	name="OpenVPN:"
	if (( $(ps ax | grep "openvpn" | wc -l) > 1 )); then
		showok
	else
		showerror
	fi
	
	name="FTP-Server:"
	if (( $(ps ax | grep "ftp" | wc -l) > 1 )); then
		showok
	else
		showerror
	fi	
	
	name="Dynamic Update Client (DUC) für Noip.com:"
	if (( $(ps ax | grep "noip" | wc -l) > 1 )); then
		showok
	else
		showerror
	fi	
	
	name="Fail2Ban:"
	if (( $(ps ax | grep "fail2ban" | wc -l) > 1 )); then
		showok
	else
		showerror
	fi
	wait4it
	clear 
	echo -e "${cGREEN}Die Installation ist abgeschlossen. Starte Konfiguration${cNOR}"
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
		echo -e "${info} Scriptordner wird angelegt unter $target"
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
	if [ -e $target ]; then
		showok
	else
		showerror
		echo -e "${info} User.inf wird unter $target erstellt."
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
	options=(1 "Boot Benachrichtigung" off
			 2 "Updatecheck" off
			 3 "VPN Zertifikaterneuerung" off
			 4 "Benachrichtigung wenn Mail (gmail) von noip.com eintrifft" off
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
					echo -e "${info} Bootbenachrichtigungsscript wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/startup.sh >/dev/null 2>&1
					chownit
					chmodit
				fi
				;;
			2)
				name="Updatecheck:"
				target="$HOME/scripts/updatecheck.sh"
				if [ -e $target ]; then
					showok
				else
					showerror
					echo -e "${info} Updatecheck wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/updatecheck.sh >/dev/null 2>&1
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
					echo -e "${info} VPN Zertifikatserneuerungsscript wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/VPN_renew_server_cert.sh >/dev/null 2>&1
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
					echo -e "${info} Script zur überprüfung der öffentlichen IP wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/checkpublicip.sh >/dev/null 2>&1
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
					echo -e "${info} Benachrichtigungsscript für eingehende Mails (gmail) von Noip wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Python/checkmail2.py >/dev/null 2>&1
					while true; do
						echo -e "${wait4input} Gmaillogin:"
						read glogin
						echo -e "${wait4input} Gmailpasswort:${cBOON}${cWHITBG}"
						read gpw1
						echo -e "${cNOR}${wait4input} Passwort erneut eingeben:${cBOON}${cWHITBG}"
						read gpw2
						if [ "$gpw1" != "$gpw2" ]; then
							echo -e "${cNOR}${errorout} Passwörter ${cRED}stimmen nicht überein!${cNOR}"
							continue
						fi
						echo -e "${cNOR}${wait4input} Sind die Angaben korrekt? [y/N]"
						read choose
						if [ "$choose" == "y" ]; then
							sed -i "s/'Login': ''/\'Login': '"$glogin"'/g" $target
							sed -i "s/'Password': ''/\'Password': '"$gpw1"'/g" $target
							break
						elif [ "$choose" == "n" -o "$choose" == "" ]; then
							continue
						else
							echo -e "${errorout} Eingabe ungültig. Bitte erneut versuchen."
							continue
						fi
					done
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
					echo -e "${info} Happy New Year Script wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/Happy_New_Year.sh >/dev/null 2>&1
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
					echo -e "${info} Benachrichtigungsscript für eingehende VPN-Verbindungen wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/VPNconnect.sh >/dev/null 2>&1
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
					echo -e "${info} Benachrichtigungsscript für Fail2Ban wird gedownloaded"
					wget https://github.com/Apop85/Scripts/blob/master/Bash/f2b_info.sh >/dev/null 2>&1
					chownit
					chmodit
				fi
				;;
			9)
				while true; do
					echo -e "${wait4input} Bot Token des Administrators angeben:"
					read admbot
					echo -e "${wait4input} Chat-ID des Administrators angeben:"
					read admcha
					echo -e "${wait4input} Sind die Angaben korrekt? [Y/n]"
					read choose
					if [ "$choose" == "" -o "$choose" == "y" ]; then
						break
					elif [ "$choose" == "n" ]; then
						continue
					else
						echo -e "${errorout} Eingabe ungültig. Bitte erneut versuchen."
						continue
					fi
				done
				
				name="Remote Slave Ordner:"
				target="$HOME/scripts/remote"
				if [ -d $target ]; then
					showok
				else
					showerror
					echo -e "${info} Erstelle remote Ordner $target"
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
					echo -e "${info} Command & Controlscript (slave) wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/Remote_Script/remote_slave.sh >/dev/null 2>&1
					sed -i "/readonly BOT_ID\(.*\)/c\readonly BOT_ID=""'"$admbot"'" $target
					sed -i "/readonly CHAT_ID\(.*\)/c\readonly CHAT_ID=""'"$admcha"'" $target
					chownit
					chmodit
				fi
				
				echo -e "${info} Übrige Remotescripts werden gedownloaded."
				
				name="remote_reboot.sh"
				target="$HOME/scripts/remote/remote_reboot.sh"
				if [ -e $target ]; then
					showok
				else
					showerror
					echo -e "${info} Downloade $name"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/Remote_Script/remote_reboot.sh >/dev/null 2>&1
					sed -i "/readonly BOT_ID\(.*\)/c\readonly BOT_ID=""'"$admbot"'" $target
					sed -i "/readonly CHAT_ID\(.*\)/c\readonly CHAT_ID=""'"$admcha"'" $target
					chmodit
				fi
				
				name="remote_update.sh"
				target="$HOME/scripts/remote/remote_update.sh"
				if [ -e $target ]; then 
					showok
				else
					showerror
					echo -e "${info} Downloade $name"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/Remote_Script/remote_update.sh >/dev/null 2>&1
					sed -i "/readonly BOT_ID\(.*\)/c\readonly BOT_ID=""'"$admbot"'" $target
					sed -i "/readonly CHAT_ID\(.*\)/c\readonly CHAT_ID=""'"$admcha"'" $target
					chmodit
				fi
				
				name="updateclient.sh"
				target="$HOME/scripts/remote/updateclient.sh"
				if [ -e $target ]; then
					showok
				else
					showerror
					echo -e "${info} Downloade $name"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/Remote_Script/updateclient.sh >/dev/null 2>&1
					sed -i "/readonly BOT_ID\(.*\)/c\readonly BOT_ID=""'"$admbot"'" $target
					sed -i "/readonly CHAT_ID\(.*\)/c\readonly CHAT_ID=""'"$admcha"'" $target
					chownit
					chmodit		
				fi
				
				name="encrypt.sh"
				target="$HOME/scripts/remote/encrypt.sh"
				if [ -e $target ]; then
					showok
				else
					showerror
					echo -e "${info} Downloade $name"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/Remote_Script/encrypt.sh.gpg >/dev/null 2>&1
					echo -e "${cRED}Passwort${cNOR} zum entschlüsseln von $name.sh.gpg notwendig!"
					wait4it
					gpg $target.gpg
					if [ -e $target ]; then 
						showok
						chownit
						chmodit
					else
						showerror
					fi
					rm $target.gpg
				fi
				cd $HOME/scripts
				;;
			10)
				name="Logcleaner.sh"
				target="$HOME/scripts/logcleaner.sh"				
				if [ -e $target ]; then
					showok
				else
					showerror
					echo -e "${info} Logcleaner wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/logcleaner.sh >/dev/null 2>&1
					chownit
					chmodit
				fi
				;;
		esac
	done
	name="sendstoredmsg.sh"
	target="$HOME/scripts/sendstoredmsg.sh"
	if [ -e $target ]; then
		showok
	else
		showerror
		echo -e "${info} $name wird gedownloaded"
		wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/sendstoredmsg.sh >/dev/null 2>&1
		mv $name $target
		chownit
		chmodit
	fi
	cd $HOME
}


#                                      _   _                 
#                                     | | (_)                
# _ __ ___   ___  _ __ ___  ___  _ __ | |_ _  ___  _ __  ___ 
#| '_ ` _ \ / _ \| '__/ _ \/ _ \| '_ \| __| |/ _ \| '_ \/ __|
#| | | | | | (_) | | |  __/ (_) | |_) | |_| | (_) | | | \__ \
#|_| |_| |_|\___/|_|  \___|\___/| .__/ \__|_|\___/|_| |_|___/
#                               | |                          
#                               |_|
#Weitere Optionen wie PiHole listen / whitelists / jail.conf downloaden
function moreoptions {
	cmd=(dialog --separate-output --checklist "Wähle Optionen:" 22 76 16)
	options=(1 "[PiHole] Whitelist downloaden und anwenden" off
			 2 "[PiHole] Gravitylisten downloaden und anwenden" off
			 3 "[Fail2Ban] jail.local ohne Scriptausführung downloaden und anwenden" off
			 4 "[Fail2Ban] jail.local mit Scriptausführung downloaden und anwenden (überschreibt Punkt 3)" off
			 5 "[VPN] User einrichten" off
			 6 "[VPN] Scriptausführung einrichten" off
			 7 "[Telegram] CHAT_ID und BOT_ID eintragen" off
			 8 "[Telegram] Testnachricht versenden" off
			 9 "[CronJob] CronJobs einrichten" off)
	choices=$("${cmd[@]}" "${options[@]}" 2>&1 >/dev/tty)
	clear
	for choice in $choices
	do
		case $choice in
			1)
				target="/etc/pihole/whitelist.txt"
				name="whitelist.txt"
				echo -e "${info} $name wird gedownloaded"
				wget https://raw.githubusercontent.com/Apop85/Scripts/master/Raspberry-Files/whitelist.txt >/dev/null 2>&1
				echo -e "${info} Verschiebe $name nach $target"
				mv $name $target
				echo -e "${info} Zugriffsrechte für $name werden gesetzt."
				chown pihole:www-data $target
				chmod 664 $target
				echo -e "${info} Piholeliste wird upgedated."
				pihole -g
				echo -e "${info} Konfiguration von $name abgeschlossen."
				;;
			2)
				name="adlists.list"
				target="/etc/pihole/adlists.list"
				echo -e "${info} $name wird gedownloaded"
				wget https://raw.githubusercontent.com/Apop85/Scripts/master/Raspberry-Files/adlists.list >/dev/null 2>&1
				mv $name $target
				echo -e "${info} Zugriffsrechte für $name werden gesetzt."
				chown pihole:www-data $target 
				chmod 664 $target
				echo -e "${info} Piholeliste wird upgedated."
				pihole -g
				echo -e "${info} Konfiguration von $name abgeschlossen."
				;;
			3)
				name="jail.local"
				target="/etc/fail2ban/jail.local"
				echo -e "${info} $name wird gedownloaded"
				wget  https://raw.githubusercontent.com/Apop85/Scripts/master/Raspberry-Files/jail.local.noscript >/dev/null 2>&1
				echo -e "${info} Zugriffsrechte für $name werden gesetzt."
				mv $name $target
				chown root:root $target
				chmod 664 $target
				echo -e "${info} Starte ${cGREEN}Fail2Ban${cNOR} neu"
				systemctl restart fail2ban.service
				;;
			4)
				name="jail.local"
				target="/etc/fail2ban/jail.local"	
				echo -e "${info} $name wird gedownloaded"
				wget  https://raw.githubusercontent.com/Apop85/Scripts/master/Raspberry-Files/jail.local >/dev/null 2>&1
				echo -e "${info} Zugriffsrechte für $name werden gesetzt."
				mv $name $target
				chown root:root $target
				chmod 664 $target
				echo -e "${info} Scriptausführung wird eingerichtet." 
				name="runscript.conf"
				target="/etc/fail2ban/action.d/runscript.conf"
				echo -e "${info} $name wird gedownloaded"
				wget https://raw.githubusercontent.com/Apop85/Scripts/master/Raspberry-Files/runscript.conf >/dev/null 2>&1
				mv $name $target
				chown root:root $target
				chmod 664 $target
				echo -e "${info} Fail2Ban mit Scriptausführung eingerichtet."
				echo -e "${info} Starte ${cGREEN}Fail2Ban${cNOR} neu"
				name="f2b_info.sh"
				target="$HOME/scripts/f2b_info.sh"
				if [ -e $target ]; then
					showok
				else
					showerror
					echo -e "$name wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/f2b_info.sh >/dev/null 2>&1
					mv $name $target
					chownit
					chmodit
				fi
				systemctl restart fail2ban.service
				;;
			5)
				while true; do 
					echo -e "${wait4input} Bitte gewünschten ${cGREEN}Usernamen${cNOR} für den VPN Client eingeben:"
					read vpnname
					echo -e "${wait4input} Ist ${cGREEN}${vpnname}${cNOR} korrekt? [y/n]"
					read choose
					if [ "$choose" == "y" ]; then
						pivpn -a $vpnname
						break
					elif [ "$choose" == "n" ]; then
						continue
					else
						echo -e "${info} ${cRED}Fehlerhafte Eingabe, erneut versuchen${cNOR}"
						continue
					fi
				done
				;;
			6)
				name="VPNconnect.sh"
				target2="/etc/openvpn/server.conf"
				target="$HOME/scripts/VPNconnect.sh"
				echo -e "${info} Scriptausführung bei eingehender VPN Verbindung wird eingerichtet"
				echo "script-security 2" >> $target2
				echo "client-connect $target2" >> $target2
				if [ -e $target ]; then
					showok
				else
					showerror
					echo -e "${info} $name wird gedownloaded"
					wget https://raw.githubusercontent.com/Apop85/Scripts/master/Bash/VPNconnect.sh >/dev/null 2>&1
					mv $name $target
					chownit
					chmodit
				fi
				echo -e "${info} Einrichtung abgeschlossen, Dienst wird neu gestartet"				
				;;
			7)
				name="telegram.inf"
				target="$HOME/scripts/telegram.inf"
				if [ -e $target ]; then
					showok
					source $target
				else
					showerror
					touch $target
				fi
				
				while true; do
					echo -e "${info} Aktuelles Bot Token: ${cGREEN}${BOT_ID}${cNOR}"
					echo -e "${wait4input} ${cGREEN}Bot Token${cNOR} angeben:"
					read BOT_ID
					echo -e "${info} Aktuelle Chat-ID: ${cGREEN}${CHAT_ID}${cNOR}"
					echo -e "${wait4input} ${cGREEN}Chat ID${cNOR} angeben:"
					read CHAT_ID
					echo -e "${wait4input} Sind die Angaben korrekt? [Y/n]"
					read choose
					if [ "$choose" == "" -o "$choose" == "y" ]; then
						echo "BOT_ID='"${BOT_ID}"'" > $target
						echo "CHAT_ID='"${CHAT_ID}"'" >> $target
						break
					elif [ "$choose" == "n" ]; then
						continue
					else
						echo -e "${errorout} Eingabe ungültig. Bitte erneut versuchen."
						continue
					fi
				done
				echo -e "${info} Einrichtung von $name ist abgeschlossen."
				;;
			8)
				target="$HOME/scripts/telegram.inf"
				source $target
				echo -e "${wait4input} Gewünschte ${cGREEN}Telegramnachricht${cNOR} angeben:"
				read MESSAGE
				curl -s -k "https://api.telegram.org/bot$BOT_ID/sendMessage" -d text="$MESSAGE" -d chat_id=$CHAT_ID
				;;			
			9)
				#Kopiere Usercron
				echo -e "${info} Cronjobs für ${cGREEN}${iam}${cNOR} werden eingerichtet"
				crontab -u $iam -l > cron${iam}
				
				#Logcleaner
				name="logcleaner.sh"
				target="$HOME/scripts/logcleaner.sh"
				if [ -e $target ]; then
					showok
					echo "15 0 * * * /bin/bash $target" >> cron${iam}
				fi
				
				#Remote_Slave
				name="remote_slave.sh"
				target="$HOME/scripts/remote/remote_slave.sh"
				if [ -e $target ]; then
					showok
					echo "0 */1 * * * /bin/bash $target" >> cron${iam}
				fi

				#Happy_New_Year
				name="Happy_New_Year.sh"
				target="$HOME/scripts/Happy_New_Year.sh"
				if [ -e $target ]; then
					showok
					echo "@yearly /bin/bash $target" >> cron${iam}
				fi

				#Checkmail
				name="checkmail2.py"
				target="$HOME/scripts/checkmail2.py"
				if [ -e $target ]; then
					showok
					echo "0 12 * * * /usr/bin/python2 $target" >> cron${iam}
				fi

				#CheckpublicIP
				name="checkpublicip.sh"
				target="$HOME/scripts/checkpublicip.sh"
				if [ -e $target ]; then
					showok
					echo "0 */1 * * * /bin/bash $target" >> cron${iam}
				fi

				#Updatecheck
				name="updatecheck.sh"
				target="$HOME/scripts/updatecheck.sh"
				if [ -e $target ]; then
					showok
					echo "5 12 * * * /bin/bash $target" >> cron${iam}
				fi

				#Updatecheck
				name="startup.sh"
				target="$HOME/scripts/startup.sh"
				if [ -e $target ]; then
					showok
					echo "@reboot /bin/bash $target" >> cron${iam}
				fi
				
				#Sendstoredmsg
				name="sendstoredmsg.sh"
				target="$HOME/scripts/sendstoredmsg.sh"
				if [ -e $target ]; then
					showok
					echo "0 9 * * * /bin/bash $target" >> cron${iam}
				fi
				
				#Installiere Cronjobs für User
				crontab -u $iam cron${iam}
				rm cron${iam}
				echo -e "${info} Cronjobs für ${cGREEN}${iam}${cNOR} wurden eingerichtet"
				
				#Kopiere Rootcron
				echo -e "${info} Cronjobs für ${cGREEN}root${cNOR} werden eingerichtet"
				crontab -u root -l > cronroot

				#Remote_Update
				name="remote_update.sh"
				target="$HOME/scripts/remote/remote_update.sh"
				if [ -e $target ]; then
					showok
					echo "15 */1 * * * /bin/bash $target" >> cronroot
				fi

				#Remote_Reboot
				name="remote_reboot.sh"
				target="$HOME/scripts/remote/remote_reboot.sh"
				if [ -e $target ]; then
					showok
					echo "30 */1 * * * /bin/bash $target" >> cronroot
				fi

				#VPN_renew_server_cert
				name="VPN_renew_server_cert.sh"
				target="$HOME/scripts/VPN_renew_server_cert.sh"
				if [ -e $target ]; then
					showok
					echo "0 5 1 */4 * /bin/bash $target" >> cronroot
				fi

				#Installiere Cronjobs für Root
				crontab -u root cronroot
				rm cronroot
				echo -e "${info} Cronjobs für ${cGREEN}root${cNOR} wurden eingerichtet"
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
installftp
installduc
installf2b
abschluss
configstuff
getscripts
moreoptions

echo -e "${info} Einrichtung des Raspberrys abgeschlossen."
echo -e "${info} ${cRED}Raspberry wird neu gestartet${cNOR}"
sleep 10
reboot

exit 0
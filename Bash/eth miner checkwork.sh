#!/bin/bash
#Dieses Script Prüft das Guthaben eines Ethereumwallets und überwacht so die Aktivität des Miners

#-------------------------Konstanten-----------------------------
#IDs
readonly ETHWALLET=''
readonly BOT_ID=''
readonly CHAT_ID=''
#Messages
MESSAGE="ACHTUNG! Miner inaktiv!"
readonly MESSAGE2="Miner wieder aktiv!"
#URLs
readonly TELEGRAMAPIURL="https://api.telegram.org/bot$BOT_ID/sendMessage"
readonly ETHWALLETURL="https://api.nanopool.org/v1/eth/balance/$ETHWALLET"
#TMP-Files
readonly TMPFILE='/var/tmp/balcheck.tmp'
readonly CHKFILE='/var/tmp/inactive.tmp'
readonly MESSAGES='/var/tmp/messages.tmp'
#----------------------------------------------------------------


#Erstelle TMP-File
touch $TMPFILE

#Lese Uhrzeit
zeit=$(date +"%H")

#Lese letzten Kontostand aus.
ballast=$(grep -e . $TMPFILE)

#Funktionen differenz Kontostand
function differenz {
	#Lese aktuellen Kontostand
	balance=$(curl $ETHWALLETURL | cut -c 23-41)
	#Teste ob Kontostand gültig
	test_true=$(echo $balance | grep -e '<' | wc -l)
	if (($test_true == 0)); then
		if [ "$balance" = '":"Account not foun' ]; then
			#Bei fehlendem Kontostand, wiederhole Funktion nach 20sek.
			sleep 20
			differenz
		else
			#Schreibe aktuellen Kontostand in temporäres File
			echo $balance > $TMPFILE
			#Wenn letzter Kontostand nicht vorhanden, schliesse Script
			if [[ $ballast == "" ]]; then
				exit 0
			fi
			#Berechne differenz zwischen letztem und aktuellem Kontostand
			balance_cal=`echo "scale=12 ; $balance-$ballast" | bc`
			balance_chk=$(echo $balance_cal | cut 1-1)
			if [ $balance_chk == "." ]; then	
				balance_cal="0$balance_cal"
			fi
			#Wenn differenz = 0, sende Warnung. 
			if (( balance_cal == 0 )); then	
				senden
			else
				#Lösche CHKFILE wenn Miner läuft
				if [ -e $CHKFILE ]; then
					if (( $zeit <= 9 )); then
						MESSAGE=$MESSAGE2
						storemessage
					else
						rm $CHKFILE
						curl -s -k "$TELEGRAMAPIURL" -d text="$MESSAGE2" -d chat_id="$CHAT_ID"
					fi
				fi
				exit 0
			fi
		fi
	else
		#Bei fehlendem Kontostand, wiederhole Funktion nach 20sek.
		let counter++
		if (( $counter == 10 )); then 
			echo Kontostand konnte nicht ermittelt werden. Internetverbindung Prüfen. 
			exit 0
		fi
		sleep 20
		differenz
	fi
}

#Senden mit Telegram, erstellen von CHKFILE und Exit
function senden {
	if [ -e $CHKFILE ]; then
		exit 0
	else
		if (( $zeit <= 9 )); then
			storemessage
		else
			touch $CHKFILE
			curl -s -k "$TELEGRAMAPIURL" -d text="$MESSAGE" -d chat_id="$CHAT_ID"
			exit 0
		fi
	fi
}

#Speichere Nachrichten
function storemessage {
	touch $MESSAGES
	MESSAGE="$(date +"%H:%M"): $MESSAGE"
	echo $MESSAGE >> $MESSAGES
	exit 0
}

differenz

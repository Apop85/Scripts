#!/bin/bash
# Dieses Script automatisiert das erneuern des VPN-Serverzertifikats.
#        Auszuführen als:
#     ____  ____  ____  ______
#    / __ \/ __ \/ __ \/_  __/
#   / /_/ / / / / / / / / /   
#  / _, _/ /_/ / /_/ / / /    
# /_/ |_|\____/\____/ /_/     

#Konstanten & Variablen
readonly RSAFOLDER='/etc/openvpn/easy-rsa'
readonly TELEDIR='/home/pi/Autostart/telegram.inf'
readonly CRTDIR='/etc/openvpn/crl.pem'
readonly MESSAGE='Neues Server-Zertifikat für OpenVPN wurde erstellt.'
readonly TELEURL='https://api.telegram.org/bot'
readonly RUNLOG='$HOME/scriptexec.log'

#Lese Telegram-ID's
source $TELEDIR

#Erstelle neues Server-Zertifikat
$RSAFOLDER/easyrsa gen-crl

#Kopiere neues Zertifikat in Zielordner
cp $RSAFOLDER/pki/crl.pem $CRTDIR

#OpenVPN-Dienst neu starten.
systemctl restart openvpn@server.service

#Sende Telegramnachricht & schreibe Log
curl -s -k "$TELEURL$BOT_ID/sendMessage" -d text="$MESSAGE" -d chat_id=$CHAT_ID
echo "$(date +"%d.%m.%Y - %H:%M:%S")" root_renew_cert.sh ausgeführt >> $RUNLOG

exit 0

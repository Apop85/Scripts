#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: check_corona.py
# Project: Sonstige_Uebungen
#-----
# Created Date: Wednesday 11.03.2020, 11:08
# Author: Apop85
#-----
# Last Modified: Thursday 12.03.2020, 16:28
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:
####

import requests, re, os, hashlib
from datetime import datetime

# Definiere verwendeter Pfad und URL
tmp_file = r'/var/tmp/corona.tmp'
tmp_download = r'/var/tmp/download.tmp'
logfile = r'/var/log/corona_check.log'
source_url = r'https://www.bag.admin.ch/bag/de/home/krankheiten/ausbrueche-epidemien-pandemien/aktuelle-ausbrueche-epidemien/novel-cov/so-schuetzen-wir-uns.html'

# Regex Suchmuster
title_pattern = re.compile(r'Erkrankungen, chronische Atemwegserkrankungen, geschwächtes Immunsystem, Krebs.*?<ul>(.*?)</ul>', re.DOTALL)

# HTML-Fake-Header
html_header = { 'Host': 'www.bag.admin.ch', 'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:73.0) Gecko/20100101 Firefox/73.0',
                'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8', 'Accept-Language': 'de,en-US;q=0.7,en;q=0.3',
                'Accept-Encoding': 'gzip, deflate, br', 'DNT': '1', 'Connection': 'keep-alive', 
                'Cookie': 'BITS-Persistence=1686872074.20480.0000; TS0142722c=019832244b57234f41a83751764ae845d680ab79bbc62e852c063ed407ffe991bae00c245fbbacf79497936632a928607cc3a14d154c284b788d1fafd2fb1df461c58572b6',
                'Upgrade-Insecure-Requests': '1', 'Cache-Control': 'max-age=0' }

def download_content(url, tmpfile, html_header):
    try:
        # Prüfe ob URL erreichbar
        content = requests.get(url, headers=html_header)
        # Prüfe auf Fehlermeldung beim Abrufen
        content.raise_for_status()
    except Exception as error:
        abort_script(error)

    # Download des Seiteninhalts
    file_writer = open(tmpfile, 'wb')
    # Splitte die Datei in chunks und lade diese einzeln herunter
    for chunk in content.iter_content(10**6):
        file_writer.write(chunk)
    # Speichere Datei
    file_writer.close()


def process_data(tmpfile, pattern):
    # Lese Dateiinhalt aus
    file_reader = open(tmpfile, "r", encoding="UTF-8")
    file_content = file_reader.read()
    
    # Durchsuche Dateiinhalt nach dem vorgegebenen Muster
    search_result = pattern.findall(file_content)
    file_reader.close()

    # Entferne Unnötige Informationen
    search_result = "".join(str(search_result[0]).split(r'<li>'))
    search_result = "".join(search_result.split('\n'))
    search_result = search_result.rstrip('</li>')
    search_result = search_result.split(r'</li>')

    return search_result

def compare_data(data):
    if not os.path.exists(tmp_file):
        # Existiert noch kein Resultat. Tmp-File mit Daten anlegen
        file_writer = open(tmp_file, 'w', encoding='UTF-8')
        file_writer.write(str(data))
        file_writer.close()
    else:
        # Existiert bereits ein Resultat dieses mit aktuellem Resultat vergleichen
        # Schreibe aktuelle Daten in Datei
        file_reader = open(tmp_file, 'r', encoding='UTF-8')
        file_content = file_reader.read()
        file_reader.close()

        # Hashwerte auslesen
        old_hash = hashlib.sha384()
        old_hash.update(file_content.encode('utf-8'))
        new_hash = hashlib.sha384()
        new_hash.update(str(data).encode('utf-8'))

        if old_hash.hexdigest() != new_hash.hexdigest():
            # Sind die Daten unterschiedlich, per Telegram eine Nachricht senden.
            bot_token = '1111111111:ABCDEfghijklmnopQRSTUVWXYZ'
            chat_id = '1111111'
            message = "BAG-Richtlinien wurden geändert. " + source_url
            telegram_api = 'https://api.telegram.org/bot'+bot_token+'/sendMessage?chat_id='+chat_id+'&text='+message
            requests.get(telegram_api)
            abort_script("Webseite aktualisiert! " + str(old_hash) + " ist nicht gleich " + str(new_hash))
            file_writer = open(tmp_file, 'w', encoding='UTF-8')
            file_writer.write(str(data))
            file_writer.close()

def abort_script(error_message):
    # Download konnte nicht durchgeführt werden.
    file_writer = open(logfile, 'a+', encoding="UTF-8")
    file_writer.write(str(datetime.today())+": "+str(error_message)+"\n")
    file_writer.close()


# Lese Zeit aus
time_now = datetime.now()
hour_now = time_now.hour
# Keine Nachrichten zwischen 22 und 7 uhr
if 7 > int(hour_now) or int(hour_now) > 22:
    exit()

download_content(source_url, tmp_download, html_header)
data = process_data(tmp_download, title_pattern)

# Wurden Daten empfangen?
if len(data) > 0:
    # Vergleiche die Daten mit letztem Eintrag
    compare_data(data)
else:
    abort_script("Keine Daten heruntergeladen")


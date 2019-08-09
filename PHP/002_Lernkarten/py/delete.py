#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: delete.py
# Project: py
#-----
# Created Date: Friday 09.08.2019, 11:28
# Author: rbald
#-----
# Last Modified: Friday 09.08.2019, 13:57
#-----
# Copyright (c) 2019 rbald
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Delete a file given by argv
####

from sys import argv
import os

if argv[1] == "del" and argv[2] != "" and os.path.isfile(argv[2]) and argv[2].endswith(".php"):
    directory = os.path.dirname(argv[2])
    directory_content = len(os.listdir(directory))
    try:
        os.remove(argv[2])
        if len(os.listdir(directory)) == directory_content-1:
            print('<div class="output_message info">Datei erfolgreich entfernt</div>')
        elif len(os.listdir(directory)) == directory_content:
            print('<div class="output_message error">Datei konnte nicht entfernt werden</div>')
        elif len(os.listdir(directory)) < directory_content-1:
            print('<div class="output_message error">Es wurden mehr Dateien entfernt als erwartet</div>')
        else:
            print('<div class="output_message error">SYSTEM: Ein unbekannter Fehler ist aufgetreten</div>')
    except:
        print('<div class="output_message error">SYSTEM: Ein schwerwiegender Fehler ist aufgetreten</div>')

        
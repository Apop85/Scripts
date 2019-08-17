#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: conf.py
# Project: py
#-----
# Created Date: Tuesday 13.08.2019, 14:57
# Author: rbald
#-----
# Last Modified: Saturday 17.08.2019, 19:24
#-----
# Copyright (c) 2019 rbald
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Create or edit config file. Used in get_help.php
####

import os, re
from sys import argv
os.chdir(os.path.dirname(__file__))

def create_ini():
    if os.path.exists("../cards/placeholder.file"):
        os.remove("../cards/placeholder.file")
    root_dir = argv[2]
    directory_dir = argv[3]
    qa_intro = argv[4]
    seperator = argv[5]

    if os.path.exists(directory_dir) and os.path.exists(root_dir):
        conf_content = '// Pfad zur Webseite\nROOT_PATH="{}"\n\n// Standard Dokumentenpfad\nDOK_PATH="{}"\n\n// Formulierung der Fragen innerhalb der Word-Dateien.\n// Standardformatiertung //qa<fach<frage<antwort<\n// Beispielfrage: //qa<Mathematik<Was gibt 2 x 2?<4<\nQUESTION_START="{}"\nSEPERATOR="{}"\n'.format(root_dir, directory_dir, qa_intro, seperator)

        try:
            file_writer = open("../conf/cards.ini", "w", encoding="utf-8")
            file_writer.write(conf_content)
            file_writer.close()

            print('<div class="output_message info">Konfigurationsdatei gespeichert</div>')
        except:
            print('<div class="output_message error">Konfigurationsdatei konnte nicht erstellt werden</div>')
    else:
        print('<div class="output_message error">Ung&uuml;ltige Pfadangabe</div>')
        

def get_ini_array():
    source_file = '../conf/cards.ini'
    file_handler = open(source_file, "r", encoding="utf-8")
    file_content = file_handler.read()
    file_handler.close()

    root_dir_pattern=re.compile(r'ROOT_PATH="(.*)"\n')
    doc_dir_pattern=re.compile(r'DOK_PATH="(.*)"\n')
    qa_intro_pattern=re.compile(r'QUESTION_START="(.*)"\n')
    seperator_pattern=re.compile(r'SEPERATOR="(.*)"\n')
    
    ROOT_DIR = root_dir_pattern.findall(file_content)
    ROOT_DIR = ROOT_DIR[0]
    DOK_DIR = doc_dir_pattern.findall(file_content)
    DOK_DIR = DOK_DIR[0]
    QA_INTRO = qa_intro_pattern.findall(file_content)
    QA_INTRO = QA_INTRO[0]
    SEPERATOR = seperator_pattern.findall(file_content)
    SEPERATOR = SEPERATOR[0]

    ini_array = [ROOT_DIR, DOK_DIR, QA_INTRO, SEPERATOR]

    output_message = "?".join(ini_array)
    output_message += "?"
    output_message = output_message.split("\\")
    output_message = "/".join(output_message)
    print(output_message) 

# argv = ["xxx", "setup", "C:\"]

if argv[1] == "setup":
    create_ini()
elif argv[1] == "get_content":
    get_ini_array()
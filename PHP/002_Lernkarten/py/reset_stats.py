#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: reset_stats.py
# Project: py
#-----
# Created Date: Saturday 17.08.2019, 17:11
# Author: rbald
#-----
# Last Modified: Saturday 17.08.2019, 18:10
#-----
# Copyright (c) 2019 rbald
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: resets statistic
####

import os, re
from sys import argv

def get_root():
    # Konfigurationsdatei einlesen
    os.chdir(os.path.dirname(__file__))
    rel_dir = "../conf/cards.ini"
    file_reader = open(rel_dir)
    file_content = file_reader.readlines()
    file_reader.close()
    for line in file_content:
        if "ROOT_PATH" in line:
            ROOT_PATH = line.lstrip('ROOT_PATH="')
            ROOT_PATH = ROOT_PATH.rstrip('"\n')

    return ROOT_PATH

ROOT_PATH = get_root()

def reset_file(filename):
    file_handler = open(filename, 'r', encoding='utf-8')
    file_content = file_handler.readlines()
    file_handler.close()

    for i in range(len(file_content)):
        if r"$s = " in file_content[i]:
            file_content[i] = "\t$s = 0;\n"
        elif r"$ra = " in file_content[i]:
            file_content[i] = "\t$ra = 0;\n"
        elif r"$fa = " in file_content[i]:
            file_content[i] = "\t$fa = 0;\n"
    
    output = ""
    for line in file_content:
        output += line

    file_handler = open(filename, 'w', encoding='utf-8')
    file_handler.write(output)
    file_handler.close()

def reset_all():
    os.chdir(ROOT_PATH)
    directories = os.walk(r".\cards")
    for folder in directories:
        for files in folder:
            if isinstance(files, list):
                for filename in files:
                    if filename.endswith(".php"):
                        reset_file(folder[0]+"\\"+filename)

    stats_output = '<?php\n\t$r = 0;\n\t$f = 0;\n?>'
    file_handler = open('./php/stats.php', 'w', encoding='utf-8')
    file_handler.write(stats_output)
    file_handler.close()

if argv[1] == "reset":
    try:
        reset_all()
        print('<div class="output_message info">Statistiken zur&uuml;ckgesetzt</div>')
    except:
        print('<div class="output_message error">Fehler beim Zur&uuml;cksetzen</div>')
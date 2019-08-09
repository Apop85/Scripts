#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: edit.py
# Project: py
#-----
# Created Date: Friday 09.08.2019, 12:48
# Author: rbald
#-----
# Last Modified: Friday 09.08.2019, 13:51
#-----
# Copyright (c) 2019 rbald
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Edit php flip card files
####

from sys import argv
import os, re

root_path = r"C:/Users/rbald/OneDrive/Dokumente/http/lernkarten"

if argv[1] == "edit" and argv[2] != "" and argv[3] != "" and argv[4] != "":
    datei, frage, antwort = argv[2], argv[3], argv[4]
    abs_path = root_path+datei[1:]
    if os.path.isfile(abs_path):
        file_handler = open(abs_path, "r", encoding="utf-8")
        file_content = file_handler.readlines()
        file_handler.close()
        
        question_pattern = re.compile(r'\$q = (.*);')
        answer_pattern = re.compile(r'\$a = (.*);')
        file_pattern = re.compile(r'\$f = (.*);')
        score_pattern = re.compile(r'\$s = (.*);')
        for line in file_content:
            if "$q" in line:
                question = question_pattern.findall(line)
            elif "$a" in line:
                answer = answer_pattern.findall(line)
            elif "$f" in line:
                file_path = file_pattern.findall(line)
            elif "$s" in line:
                score = score_pattern.findall(line)

        question, answer, file_path, score = question[0], answer[0], file_path[0], score[0]
        output = "<?php\n\t$q = '{}';\n\t$a = '{}';\n\t$f = {};\n\t$s = {};\n?>".format(frage, antwort, file_path, score)
        
        try:
            file_handler = open(abs_path, "w", encoding="utf-8")
            file_handler.write(output)
            file_handler.close()
        except:
            print("<div class='output_message error'>Fehler bei der Editierung</div>")    
        print("<div class='output_message info'>Gespeichert</div>")
    else:
        print("<div class='output_message error'>Datei nicht gefunden</div>")
else:
    print("<div class='output_message error'>Zu wenig Argumente</div>")

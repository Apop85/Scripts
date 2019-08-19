#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: edit.py
# Project: py
#-----
# Created Date: Friday 09.08.2019, 12:48
# Author: rbald
#-----
# Last Modified: Monday 19.08.2019, 22:25
#-----
# Copyright (c) 2019 rbald
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Edit php flip card files
####

from sys import argv
import os, re

# argv = ["yy", "edit", "./cards/304 grundlagen/047274c9af7e4836e54d58a515dd4fbe.php", "Was bedeutet RJ45?", "Registered Jack 45!"]

def get_root():
    os.chdir(os.path.dirname(__file__))
    rel_dir = "../conf/cards.ini"
    file_reader = open(rel_dir)
    file_content = file_reader.readlines()
    file_reader.close()
    for line in file_content:
        if "ROOT_PATH" in line:
            ROOT_PATH = line.lstrip('ROOT_PATH="')
            ROOT_PATH = ROOT_PATH.rstrip('"\n')
        elif "DOK_PATH" in line:
            DOK_PATH = line.lstrip('DOK_PATH="')
            DOK_PATH = DOK_PATH.rstrip('"\n') 
    return ROOT_PATH, DOK_PATH

ROOT_PATH, DOK_PATH = get_root()
root_path = ROOT_PATH

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
            if "$q =" in line:
                question = question_pattern.findall(line)
            elif "$a =" in line:
                answer = answer_pattern.findall(line)
            elif "$f =" in line:
                file_path = file_pattern.findall(line)
            elif "$s =" in line:
                score = score_pattern.findall(line)

        question, answer, file_path, score = question[0], answer[0], file_path[0], score[0]
        output = "<?php\n\t$q = '{}';\n\t$a = '{}';\n\t$f = {};\n\t$s = {};\n\t$ra = 0;\n\t$fa = 0;\n?>".format(frage, antwort, file_path, score)
        
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

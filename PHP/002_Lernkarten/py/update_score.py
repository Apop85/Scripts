#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: update_score.py
# Project: py
#-----
# Created Date: Friday 09.08.2019, 21:19
# Author: rbald
#-----
# Last Modified: Saturday 17.08.2019, 18:25
#-----
# Copyright (c) 2019 rbald
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Update Score in file
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
        elif "DOK_PATH" in line:
            DOK_PATH = line.lstrip('DOK_PATH="')
            DOK_PATH = DOK_PATH.rstrip('"\n')
        elif "QUESTION_START" in line:
            LINE_START = line.lstrip('QUESTION_START="')
            LINE_START = LINE_START.rstrip('"\n')
        elif "SEPERATOR" in line:
            SEPERATOR = line.lstrip('SEPERATOR="')
            SEPERATOR = SEPERATOR.rstrip('"\n')

    return ROOT_PATH, DOK_PATH, LINE_START, SEPERATOR

ROOT_PATH, DOK_PATH, LINE_START, SEPERATOR = get_root()
os.chdir(ROOT_PATH)

def update_statistics(value):
    main = ROOT_PATH+"/php/stats.php"
    if not os.path.exists(main):
        file_handler = open(main, "w", encoding="utf-8")
        if value < 0:
            content = "<?php\n\t$r = {};\n\t$f = {};\n?>".format(0, 1)
        else:
            content = "<?php\n\t$r = {};\n\t$f = {};\n?>".format(1, 0)
        file_handler.write(content)
        file_handler.close()
    else:
        file_handler = open(main, "r", encoding="utf-8")
        file_content = file_handler.read()
        file_handler.close()
        
        right_pattern = re.compile(r'\t\$r = (.*);\n')
        false_pattern = re.compile(r'\t\$f = (.*);\n')

        right_score = (right_pattern.findall(file_content))
        right_score = int(right_score[0])
        false_score = (false_pattern.findall(file_content))
        false_score = int(false_score[0])

        if value < 0:
            false_score+=1
        else:
            right_score+=1
        
        file_handler = open(main, "w", encoding="utf-8")
        content = "<?php\n\t$r = {};\n\t$f = {};\n?>".format(right_score, false_score)
        file_handler.write(content)
        file_handler.close()


# argv = ['mmm','update','-1','./cards/Netzwerktechnik/dec3dbce6a3f73637820c0b9c5fc1b4d.php']

if argv[1] == "update" and argv[2] != "" and os.path.isfile(argv[3]):
    score_delta = int(argv[2]) 
    file_name = argv[3] 
    file_handler = open(file_name, "r", encoding="utf-8")
    file_content = file_handler.read()
    file_handler.close()

    score_pattern = re.compile(r'\$s = (.*);')
    score = int(score_pattern.findall(file_content)[0])
    new_score = score + score_delta
    new_string = '$s = '+str(new_score)+';'
    file_content = re.sub(r'\$s = (.*);', new_string, file_content)

    if score_delta > 0:
        ra_pattern = re.compile(r'\$ra = (.*);')
        ra = int(ra_pattern.findall(file_content)[0])
        new_score = ra + score_delta
        new_string = '$ra = '+str(new_score)+';'
        file_content = re.sub(r'\$ra = (.*);', new_string, file_content)
    else:
        fa_pattern = re.compile(r'\$fa = (.*);')
        fa = int(fa_pattern.findall(file_content)[0])
        new_score = fa - score_delta 
        new_string = '$fa = '+str(new_score)+';'
        file_content = re.sub(r'\$fa = (.*);', new_string, file_content)


    update_statistics(score_delta)


    try:
        file_handler = open(file_name, "w", encoding="utf-8")
        file_handler.write(file_content)
        file_handler.close()
    except:
        print("<div class='output_message error'>Score Write Error</div>")

else:
    print("<div class='output_message error'>Score Argument Error</div>")
    


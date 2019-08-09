#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: test.py
# Project: py
#-----
# Created Date: Thursday 08.08.2019, 23:46
# Author: rbald
#-----
# Last Modified: Friday 09.08.2019, 13:30
#-----
# Copyright (c) 2019 rbald
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: testscript to run by php
####
import re, os


abs_path = "C:/Users/rbald/OneDrive/Dokumente/http/lernkarten/cards/mathe/a65111f3c59eef30843201d52779bfbf.php"
if os.path.isfile(abs_path):
    file_handler = open(abs_path, "r", encoding="utf-8")
    file_content = file_handler.readlines()
    file_handler.close()
    
    question_pattern = re.compile(r'\$q = (.*)')
    answer_pattern = re.compile(r'\$a = (.*)')
    file_pattern = re.compile(r'\$f = (.*)')
    score_pattern = re.compile(r'\$s = (.*)')
    for line in file_content:
        if "$q" in line:
            question = question_pattern.findall(line)
        elif "$a" in line:
            answer = answer_pattern.findall(line)
        elif "$f" in line:
            file_path = file_pattern.findall(line)
        elif "$s" in line:
            score = score_pattern.findall(line)
        
    print(len(question), len(answer), len(file_path), score)
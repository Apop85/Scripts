#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: update_score.py
# Project: py
#-----
# Created Date: Friday 09.08.2019, 21:19
# Author: rbald
#-----
# Last Modified: Friday 09.08.2019, 21:59
#-----
# Copyright (c) 2019 rbald
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Update Score in file
####

import os, re
from sys import argv

# argv = ["","update", "-1" , "C:/Users/rbald/OneDrive/Dokumente/http/lernkarten/cards/mathe/a65111f3c59eef30843201d52779bfbf.php"]

if argv[1] == "update" and argv[2] != "" and os.path.isfile(argv[3]):
    score_delta = int(argv[2]) 
    file_name = argv[3] 
    file_handler = open(file_name, "r", encoding="utf-8")
    file_content = file_handler.read()
    file_handler.close()
    score_pattern = re.compile(r'\$s = (.*);')
    score = int(score_pattern.findall(file_content)[0])
    # print("1")
    new_score = score + score_delta
    new_string = '$s = '+str(new_score)+';'
    file_content = re.sub(r'\$s = (.*);', new_string, file_content)

    try:
        file_handler = open(file_name, "w", encoding="utf-8")
        file_handler.write(file_content)
        file_handler.close()
    except:
        print("<div class='output_message error'>Score Write Error</div>")

else:
    print("<div class='output_message error'>Score Argument Error</div>")
    


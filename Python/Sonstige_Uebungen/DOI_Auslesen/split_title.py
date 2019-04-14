#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: split_title.py
# Project: DOI_Auslesen
# Created Date: Monday 25.02.2019, 12:12
# Author: Apop85
# -----
# Last Modified: Sunday 14.04.2019, 16:58
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Split title_results.txt into smaller pieces
###

import os

os.chdir(os.path.dirname(__file__))

source_file=r'.\title_results.txt'
file_reader=open(source_file, 'r', encoding='UTF-8')
file_content=file_reader.readlines()
file_reader.close()

split_amount=52
counter=0
splits=1
for line in file_content:
    target_folder='.\\title_split_'+str(splits)
    if not os.path.exists(target_folder):
        os.mkdir(target_folder)
        file_writer=open(target_folder+'\\title_results.txt', 'w', encoding='UTF-8')
    if "Title" in line:
        file_writer.write(line.lstrip("Title: "))
        counter+=1
    if counter == split_amount:
        counter=0
        splits+=1
        file_writer.close()


    
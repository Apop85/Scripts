#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: pdf_fulltext_search.py
# Project: Sonstige_Uebungen
#-----
# Created Date: Tuesday 10.09.2019, 06:46
# Author: rbald
#-----
# Last Modified: Tue Sep 10 2019
#-----
# Copyright (c) 2019 rbald
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:
####

import os, PyPDF2
os.chdir(os.path.dirname(__file__))

dir_content=os.listdir(os.getcwd())

file_list = []
for filename in dir_content:
    if filename.endswith('.pdf'):
        file_list += [filename]

for pdf_document in file_list:
    file_reader = open(filename, 'rb')
    pdf_content = PyPDF2.PdfFileReader(file_reader)
    
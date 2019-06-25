#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: charset_detect.py
# Project: Sonstige_Uebungen
#-----
# Created Date: Tuesday 25.06.2019, 10:34
# Author: Apop85
#-----
# Last Modified: Tuesday 25.06.2019, 11:30
#-----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:  Detect encoding of text only based files.
####

import chardet, os
from pyperclip import copy, paste
from time import sleep

def wait_4_file():
    print('\r'*100+'Get filepath into clipboard.')
    while True:
        copy(0)
        while paste() == '0':
            sleep(0.5)
            print('\r'*100+'Get filepath... Waiting', end=' ')
        if os.path.exists(paste().strip('"')):
            print('\r'*7+'Get filepath... OK'+' '*5)
            return paste().strip('"')
        else:
            print('\r'+'Get filepath... Invalid')

def get_encoding(file_path):
    try:
        file_content = open(file_path, 'rb').read()
        return chardet.detect(file_content)['encoding']
    except Exception as errorvalue:
        print(errorvalue)
        return None

file_path = wait_4_file()
encoding = get_encoding(file_path)
if encoding != None:
    print('Detected encoding: '+encoding)
    copy(encoding)
else:
    print('Not able to detect encoding.')
#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: count_remove_twins.py
# Project: Sonstige_Uebungen
#-----
# Created Date: Sunday 04.08.2019, 15:01
# Author: Apop85
#-----
# Last Modified: Sunday 04.08.2019, 15:34
#-----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: get list from clipboard, count length and remove twins
####

from pyperclip import copy, paste
from time import sleep

def get_clipboard():
    copy("0")
    print("Waiting for clipboard")
    while paste() == "0":
        sleep(0.5)
    data = process_data(paste())
    return data

def process_data(data):
    data = paste().split("\n")
    data_dict = remove_twins(data)
    return (data, data_dict)

def remove_twins(data):
    data_dict = {}
    for entry in data:
        data_dict.setdefault(entry, 0)
        data_dict[entry]+=1
    return data_dict


while True:
    data = get_clipboard()
    print("Länge der Liste: "+str(len(data[0])))
    print("Doppelte einträge: "+str(len(data[0])-len(data[1])))
    print("Einzelne Einträge: "+str(len(data[1])))

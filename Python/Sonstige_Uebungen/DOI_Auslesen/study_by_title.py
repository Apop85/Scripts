#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: study_by_doi.py
# Project: DOI_Auslesen
# Created Date: Monday 25.02.2019, 11:12
# Author: Apop85
# -----
# Last Modified: Sunday 14.04.2019, 17:44
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: This script will read doi_results.txt and work trough the entrys to download them
# by opening a webbrowser with the download-link.
###

import webbrowser, os, msvcrt

os.chdir(os.path.dirname(__file__))

target_url='https://sci-hub.tw/'
source_file=r'.\title_results.txt'

def get_file_content():
    file_reader=open(source_file, 'r', encoding='UTF-8')
    file_content=file_reader.readlines()
    file_reader.close()
    return file_content

def user_input():
    while True:
        print('\n\n\n\n')
        print(''.center(90,'█'))
        print('\n\t\tUm in der Liste fortzufahren bitte letzten Eintrag angeben.')
        print('\t\tBeim ersten mal einfach Enter drücken.\n')
        print(''.center(90,'█'))
        start_entry=input('\t\t\t')
        if start_entry == '':
            start_entry=0
            return start_entry
        elif start_entry.isdecimal() and int(start_entry) >= 0:
            return int(start_entry)-1
        else:
            print('\t\t\tEingabe Ungültig.')
    
def start_download(file_content, start_entry):
    counter=start_entry
    print(''.center(90,'█'))
    for i in range(counter, len(file_content)):
        print('\t\t\tÖffne DOI '+str(i+1))
        webbrowser.open(target_url+file_content[i])
        # input('\t\t\tEnter zum fortfahren')
        print('\t\t\tWar der Download erfolgreich? (j/n)')
        while True:
            key=msvcrt.getch()
            if key in [b'j',b'y', b'Y',b'J',b'1']:
                break
            elif key in [b'n',b'N',b'0']:
                file_writer=open(r'.\noch_zu_pruefende_doi.txt', 'a', encoding='UTF-8')
                file_writer.write(" ".join(file_content[i].split("%20")+'\n'))
                file_writer.close()
                break
    print("\t\t\tDie Liste wurde vollständig abgearbeitet.")
    input("Enter um das Programm zu schliessen.")
    exit
        

file_content=get_file_content()
start_entry=user_input()
start_download(file_content, start_entry)
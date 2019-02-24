#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: DOI_auslesen.py
# Project: Sonstige_Uebungen
# Created Date: Sunday 24.02.2019, 19:05
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 20:07
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Search for DOI and NCT numbers in files.
###

import os, re

os.chdir(os.path.dirname(__file__))

target_dir='.\\read_me'
target_file='.\\doi_and_nct.txt'

file_list=os.listdir(target_dir)

def search_all(file_list):
    target_writer=open(target_file, 'w', encoding='UTF-8')
    total_amount=0 
    for file_name in file_list:
        file_reader=open(target_dir+'\\'+file_name, 'r', encoding='UTF-8')
        try:
            file_lines=file_reader.readlines()
        except:
            file_reader.close()
            file_reader=open(target_dir+'\\'+file_name, 'r', encoding='mbcs')
            file_lines=file_reader.readlines()
        file_reader.close()
        search_pattern_doi=re.compile(r'DO  - (.*)')
        search_pattern_nct=re.compile(r'(NCT\d{8})')
        result_doi=[]
        result_nct=[]
        for line in file_lines:
            result_doi+=search_pattern_doi.findall(line)
            result_nct+=search_pattern_nct.findall(line)
        if (len(result_doi) or len(result_nct)) != 0:
            target_writer.write(''.center(70, '█')+'\n')
            target_writer.write('Start of file: '+file_name+'\n')
            target_writer.write(''.center(70, '▼')+'\n')
            amount_now=total_amount
            if len(result_doi) != 0:
                for entry in result_doi:
                    total_amount+=1
                    entry=entry.lstrip('http://dx.doi.org')
                    entry=entry.lstrip('https://dx.doi.org')
                    entry=entry.lstrip()
                    target_writer.write(entry+'\n')
            if len(result_nct) != 0:
                for entry in result_nct:
                    total_amount+=1
                    target_writer.write(entry+'\n')
            target_writer.write(''.center(70, '▲')+'\n')
            target_writer.write('End of file: '+file_name+'\tFound entrys: '+str(total_amount-amount_now)+'\n')
            target_writer.write(''.center(70, '█')+'\n')
    target_writer.write('\n\nTotal amount: '+str(total_amount))

search_all(file_list)

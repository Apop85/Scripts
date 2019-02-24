#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: DOI_auslesen.py
# Project: Sonstige_Uebungen
# Created Date: Sunday 24.02.2019, 19:05
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 23:36
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Search for DOI, ISSN and NCT numbers in files.
###

import os, re

os.chdir(os.path.dirname(__file__))

target_dir='.\\read_me'
target_file='.\\doi_and_nct.txt'

file_list=os.listdir(target_dir)

def search_all(file_list):
    id_dictionary={}
    total_amount=0 
    for file_name in file_list:
        id_dictionary.setdefault(file_name, [])
        file_reader=open(target_dir+'\\'+file_name, 'r', encoding='UTF-8')
        try:
            file_lines=file_reader.readlines()
        except:
            file_reader.close()
            file_reader=open(target_dir+'\\'+file_name, 'r', encoding='mbcs')
            file_lines=file_reader.readlines()
        file_reader.close()
        records=find_records(file_lines)
        search_pattern_doi=re.compile(r'DO  - (.*)')
        search_pattern_nct=re.compile(r'(NCT\d{8})')
        search_pattern_issn=re.compile(r'SN  - (\d+-\d+)')
        search_pattern_title=re.compile(r'TI  - (.*)')
        search_pattern_author=re.compile(r'AU  - (.*)')
        if len(records) != 0:
            for entry in records:
                result_all, result_title, result_author, result_issn = [],[],[], []
                for line in file_lines[entry[0]:entry[1]]:
                    result_all+=search_pattern_doi.findall(line)
                    result_issn+=search_pattern_issn.findall(line)
                    result_all+=search_pattern_nct.findall(line)
                    result_title+=search_pattern_title.findall(line)
                    result_author+=search_pattern_author.findall(line)
                if len(result_all) + len(result_issn) == 0:
                    information=(result_title,result_author)
                    id_dictionary[file_name]+=[information]
                    total_amount+=1
                else:
                    if len(result_issn) != 0:
                        result_new=[]
                        for entry in result_issn:
                            result_new+=['ISSN '+entry]
                        result_issn=result_new
                        result_all+=result_issn
                    for id_entry in result_all:
                        id_entry=id_entry.lstrip('http://dx.doi.org')
                        id_entry=id_entry.lstrip('https://dx.doi.org')
                        id_entry=id_entry.lstrip()
                        id_dictionary[file_name]+=[id_entry]
                        total_amount+=1
    return total_amount, id_dictionary

def find_records(file_lines):
    record=''
    counter=1
    records=[]
    for i in range(len(file_lines)):
        if "Record" in file_lines[i] or "Result:" in file_lines[i] or '<'+str(counter)+'. >' in file_lines[i] or '<Trial>' in file_lines[i] or "Study "+str(counter) in file_lines[i] or '<study ' in file_lines[i]:
            if record == '':
                record=i
            else:
                records+=[(record,i)]
                record=i
            counter+=1
    return records

def print_it(id_dictionary, file_list):
    for file_name in id_dictionary:
        counter=1
        print(''.center(70, '█'))
        print('Start of file: '+file_name)
        print(''.center(70, '▼'))
        for entry in id_dictionary[file_name]:
            if isinstance(entry, tuple):
                print(str(counter)+'.  Title: '+entry[0][0])
                print(str(counter)+'.  Authors: '+' '.join(entry[1][0].split('[Author]')))
                counter+=1
            else:
                print(str(counter)+'.  '+entry)
                counter+=1
        print(''.center(70, '▲'))
        print('End of file: '+file_name+'\tAmount: '+str(counter))
        print(''.center(70, '█'))
    if len(file_list) == len(list(id_dictionary.keys())):
        print('Alle Files durchsucht.')

def write_it(id_dictionary, file_list):
    file_writer=open(target_file, 'w', encoding='UTF-8')
    for file_name in id_dictionary:
        counter=1
        file_writer.write(''.center(70, '█')+'\n')
        file_writer.write('Start of file: '+file_name+'\n')
        file_writer.write(''.center(70, '▼')+'\n')
        for entry in id_dictionary[file_name]:
            if isinstance(entry, tuple):
                file_writer.write(str(counter)+'. Title: '+entry[0][0]+'\n')
                file_writer.write(str(counter)+'. Authors: '+' '.join(entry[1][0].split('[Author]'))+'\n')
                counter+=1
            else:
                file_writer.write(str(counter)+'.  '+entry+'\n')
                counter+=1
        file_writer.write(''.center(70, '▲')+'\n')
        file_writer.write('End of file: '+file_name+'\tAmount: '+str(counter)+'\n')
        file_writer.write(''.center(70, '█')+'\n')
    if len(file_list) == len(list(id_dictionary.keys())):
        file_writer.write('Alle Files durchsucht.\n')
    file_writer.close()

total_amount, id_dictionary=search_all(file_list)
# print_it(id_dictionary, file_list)
write_it(id_dictionary, file_list)

file_writer=open(target_file, 'a', encoding='UTF-8')
file_writer.write('Total amount: '+str(total_amount))
file_writer.close()

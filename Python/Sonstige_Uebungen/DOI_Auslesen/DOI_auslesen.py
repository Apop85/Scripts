#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: DOI_auslesen.py
# Project: Sonstige_Uebungen
# Created Date: Sunday 24.02.2019, 19:05
# Author: Apop85
# -----
# Last Modified: Monday 25.02.2019, 11:06
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Search for DOI, ISSN and NCT numbers in files.
###

import os, re

os.chdir(os.path.dirname(__file__))

target_dir=r'.\read_me'
target_file=r'.\results.txt'
doi_file=r'.\doi_results.txt'
nct_file=r'.\nct_results.txt'
issn_file=r'.\issn_results.txt'
title_file=r'.\title_results.txt'

file_list=os.listdir(target_dir)

file_length={}
def search_all(file_list):
    global file_length
    id_dictionary={}
    total_amount=0 
    for file_name in file_list:
        if file_name == 'put_files_here.txt':
            continue
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
        file_length.setdefault(file_name, len(records))
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

total_studies=0
def find_records(file_lines):
    global total_studies
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
    total_studies+=counter-1
    return records

def write_it(id_dictionary, file_list):
    global file_length
    file_writer=open(target_file, 'w', encoding='UTF-8')
    for file_name in id_dictionary:
        counter=1
        file_writer.write(''.center(70, '█')+'\n')
        file_writer.write('Start of file: '+file_name+'\n')
        file_writer.write(''.center(70, '▼')+'\n')
        for entry in id_dictionary[file_name]:
            if isinstance(entry, tuple):
                file_writer.write(str(counter)+'.\tTitle: '+entry[0][0]+'\n')
                file_writer.write(str(counter)+'.\tAuthors: '+' '.join(entry[1][0].split('[Author]'))+'\n')
                counter+=1
            else:
                file_writer.write(str(counter)+'.\t'+entry+'\n')
                counter+=1
        file_writer.write(''.center(70, '▲')+'\n')
        file_writer.write('End of file: '+file_name+'\tAmount: '+str(counter-1)+' of '+str(file_length[file_name])+'\n')
        file_writer.write(''.center(70, '█')+'\n')
    if len(file_list) == len(list(id_dictionary.keys())):
        file_writer.write('All files have been searched.\n')
    file_writer.close()

def sort_it(id_dictionary):
    doi_writer=open(doi_file, 'w', encoding='UTF-8')
    issn_writer=open(issn_file, 'w', encoding='UTF-8')
    nct_writer=open(nct_file, 'w', encoding='UTF-8')
    title_writer=open(title_file, 'w', encoding='UTF-8')
    counter=0
    for file_name in id_dictionary:
        for entry in id_dictionary[file_name]:
            if '/' in entry:
                doi_writer.write(entry+'\n')
            elif isinstance(entry, tuple):
                title_writer.write('Title: '+entry[0][0]+'\n')
                title_writer.write('Author: '+' '.join(entry[1][0].split('[Author]'))+'\n\n')
            elif 'ISSN' in entry:
                issn_writer.write(entry+'\n')
            elif 'NCT' in entry:
                nct_writer.write(entry+'\n')
            else:
                print(entry)
            counter+=1
    doi_writer.close()
    issn_writer.close()
    title_writer.close()
    nct_writer.close()

total_amount, id_dictionary=search_all(file_list)
write_it(id_dictionary, file_list)
sort_it(id_dictionary)

file_writer=open(target_file, 'a', encoding='UTF-8')
file_writer.write('Found '+str(total_amount)+' entrys in '+str(total_studies)+' studies.')
file_writer.close()

print('\n\n\n\n\n')
print('Task done.'.center(70))
print(('Found '+str(total_amount)+' entrys in '+str(total_studies)+' studies.').center(70))
print('Results saved under results.txt.'.center(70))
print('Press enter to quit...'.center(70))
print('\n\n')
input()
#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: get_study_informations.py
# Project: DOI_Auslesen
# Created Date: Thursday 23.05.2019, 19:26
# Author: Apop85
# -----
# Last Modified: Thursday 23.05.2019, 22:38
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Read out the details of any study
###

import os, re, openpyxl, PyPDF2

os.chdir(os.path.dirname(__file__))
print(os.getcwd())

source_dir='.\\read_me'
target_file='results.xlsx'
studies_path='.\\studies'
target_dois_file='target_dois.txt'
studies=os.listdir(studies_path)
source_files=os.listdir(source_dir)
if os.path.exists(target_file):
    os.remove(target_file)
excel_sheet=openpyxl.Workbook()
sheet_names=excel_sheet.sheetnames
active_sheet=excel_sheet[sheet_names[0]]

doi_file=open(target_dois_file, 'r')
raw_dois=doi_file.readlines()
needed_dois=[]
for doi in raw_dois:
    needed_dois+=[doi.strip('\n')]

# Prepare the Excel file to write data
active_sheet.title='Studieninformationen'
active_sheet['A1']='DOI/PMID'
active_sheet['B1']='Studientitel'
active_sheet['C1']='Authoren'
active_sheet['D1']='Jahr'
active_sheet['E1']='Dateiname'
active_sheet['F1']='Studie Ja/Nein'
active_sheet['G1']='Studientyp'
active_row=2

def search_databases(source_dir, source_files):
    # Open Files and try to read out any line to analyze
    global active_row
    for file_name in source_files:
        file_reader=open(source_dir+'\\'+file_name, 'r', encoding='UTF-8')
        try:
            file_lines=file_reader.readlines()
        except:
            file_reader=open(source_dir+'\\'+file_name, 'r', encoding='mbcs')
            file_lines=file_reader.readlines()
        file_reader.close()
        database_records=find_records_in_db(file_lines)

        # Regex search pattern
        search_pattern_doi=re.compile(r'DO  - (.*)')
        search_pattern_nct=re.compile(r'(NCT\d{8})')
        search_pattern_issn=re.compile(r'SN  - (\d+-\d+)')
        search_pattern_title=re.compile(r'TI  - (.*)')
        search_pattern_author=re.compile(r'AU  - (.*)')
        search_pattern_year=re.compile(r'YR  - (\d{4})')
        search_pattern_study_type=re.compile(r'MH  - (.* stud\w+)', re.DOTALL)

        # Get the Informations about the Results
        for entry in database_records:
            result_doi, result_title, result_author, result_issn, result_nct, result_year, result_study_type, found = '','','','','','','', 0
            for i in range(entry[0],entry[1]):
                result_doi+=(str(search_pattern_doi.findall(file_lines[i])).strip('[\' ]')).lstrip('http://dx.doi.org/')+' '
                check_doi=(str(search_pattern_doi.findall(file_lines[i])).strip('[\' ]')).lstrip('http://dx.doi.org/')
                if check_doi != '' and check_doi in needed_dois:
                    found=1
                result_title+=str(search_pattern_title.findall(file_lines[i])).strip('[\' ]')+' '
                result_author+=str(search_pattern_author.findall(file_lines[i])).strip('[\' ]')+' '
                result_issn+=str(search_pattern_issn.findall(file_lines[i])).strip('[\' ]')+' '
                result_nct+=str(search_pattern_nct.findall(file_lines[i])).strip('[\' ]')+' '
                result_year+=str(search_pattern_year.findall(file_lines[i])).strip('[\' ]')+' '
                result_study_type+=str(search_pattern_study_type.findall(file_lines[i])).strip('[\' ]')+' '

            #Write all informations into the excel file
            if found == 1:
                active_sheet['A'+str(active_row)]=result_doi.strip()+' '+result_issn.strip()+' '+result_nct.strip()
                active_sheet['B'+str(active_row)]=result_title.strip()
                active_sheet['C'+str(active_row)]=result_author.strip()
                active_sheet['D'+str(active_row)]=result_year.strip()
                active_sheet['G'+str(active_row)]=result_study_type.strip()
                active_row+=1
                found = 0

def find_records_in_db(file_lines):
    # Check lines for begin and end of an entry and save the position
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

def search_for_filename(studies_path, studies, needed_dois):
    # Check which file contains the searched study
    active_row=2
    
    # for file_name in studies:
    #     try:
    #         searched_doi=active_sheet['A'+str(active_row)].value
    #         searched_title=active_sheet['B'+str(active_row)].value
    #         print('Open: '+file_name)
    #         print('Search for: '+searched_doi+ ' - ' + searched_title)
    #         pdf_file_open=open(studies_path+'\\'+file_name, 'rb')
    #         pdf_content=PyPDF2.PdfFileReader(pdf_file_open)
            
    #         for i in range(pdf_content.numPages):
    #             current_page=pdf_content.getPage(i)
    #             page_content=current_page.extractText()
    #             if searched_doi in page_content or searched_title in page_content:
    #                 active_sheet['E'+str(active_row)]=file_name
    #                 print('Found DOI')
    #                 break

    #     except:
    #         print('File_Error: '+file_name)
    #         pdf_file_open.close()
                


search_databases(source_dir, source_files)
search_for_filename(studies_path, studies, needed_dois)
excel_sheet.save(target_file)
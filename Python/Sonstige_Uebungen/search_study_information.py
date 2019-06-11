#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: search_study_information.py
# Project: get_informations
# Created Date: Monday 10.06.2019, 19:44
# Author: Apop85
# -----
# Last Modified: Tuesday 11.06.2019, 11:22
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description:
###
import os, re, openpyxl, sys, ctypes

def init_script():
    global file_name, excel_dir, source_dir, txt_dir
    os.chdir(os.path.dirname(__file__))
    source_dir = r'.\read_me'
    target_doi_file = r'.\target_dois.txt'
    excel_dir = r'.\excel_files'
    txt_dir = r'.\txt'
    # Create dir if not exists
    if not os.path.exists(excel_dir):
        os.mkdir(excel_dir)
    # Get DOI list
    searched_dois = open(target_doi_file).readlines()
    input_valid = False
    while not input_valid:
        file_name = input('Part number: part_')
        if file_name.isdecimal():
            file_name = 'part_'+str(file_name)+'.xlsx'
            input_valid = True
        else:
            print('Invalid filename')
    return searched_dois

def init_excel(searched_dois):
    # Grundgerü¨st des Excelfiles erstellen
    global excel_sheet, top_line
    excel_sheet = openpyxl.Workbook()
    line_counter = 1
    active_sheet = excel_sheet.active

    # DOIS eintragen
    for doi in searched_dois:
        line_counter += 1
        current_doi = doi.rstrip('\n')
        # Zellen beschreiben und Text formatieren
        active_sheet.cell(row=line_counter, column=2, value=current_doi)
        active_cell = active_sheet['B'+str(line_counter)]
        active_cell.font = openpyxl.styles.Font(bold=True)
        active_cell.fill = openpyxl.styles.PatternFill(fill_type='solid', start_color='DCDCDC', end_color='DCDCDC')
    # Spaltenbreite einstellen
    column_widths = [1.25, 4.51, 5.85, 6.24, 6.27, 1.0, 5.0, 3.24]
    for i in range(len(column_widths)):
        active_sheet.column_dimensions[openpyxl.utils.get_column_letter(i+1)].width = column_widths[i]*5
    top_line = ['Paket','DOI','Ausgelesene DOI','Titel','Authoren','Jahr','Dateiname','Studientyp']
    for i in range(len(top_line)):
        # Titelzeile beschreiben und formatieren
        active_cell = active_sheet[openpyxl.utils.get_column_letter(i+1)+'1']
        active_cell.font = openpyxl.styles.Font(bold=True)
        active_cell.fill = openpyxl.styles.PatternFill(fill_type='solid', start_color='DCDCDC', end_color='DCDCDC')
        active_sheet.cell(row=1, column=i+1, value=top_line[i])
    
    return active_sheet

def process_active_sheet(active_sheet):
    # Erfasse maximale anzahl Spalten und Zeilen
    max_rows = active_sheet.max_row
    max_colums = active_sheet.max_column
    # Zeilen abarbeiten
    for y in range(2,max_rows+1):
        active_doi = active_sheet['B'+str(y)].value
        # Durchsuche Datenbanken
        search_results = crawl_for_informations(active_doi)
        for x in range(1,max_colums+1):
            # Spalten abarbeiten
            active_col = top_line[x-1]
            if active_col == 'DOI':
                continue
            elif active_col == 'Paket':
                search_pattern=re.compile(r'.*?(\d+).*?')
                file_number = search_pattern.findall(file_name)
                part_number=file_number[0]
                active_sheet.cell(row=y, column=x, value=part_number)
            else:    
                cell_value = (' '.join(search_results[active_col]))
                active_cell = active_sheet[openpyxl.utils.get_column_letter(x)+str(y)]  
                active_cell.alignment = openpyxl.styles.Alignment(wrap_text=True)
                active_sheet.cell(row=y, column=x, value=cell_value) 

def crawl_for_informations(current_doi):
    database_files = os.listdir(source_dir)
    result_dictionary = {'Ausgelesene DOI':[], 'Titel':[], 'Authoren':[], 'Jahr':[],'Dateiname':[],'Studientyp':[]}
    for database in database_files:
        try:
            database_content = open(source_dir+'\\'+database, 'r', encoding='UTF-8').readlines()
        except:
            database_content = open(source_dir+'\\'+database, 'r', encoding='mbcs').readlines()
            new_db_content = []
            for line in database_content:
                line.encode('mbcs').decode('utf-8')
                new_db_content += [line]
            database_content = new_db_content
            pass

        marked_lines = find_records_in_db(database_content)
        for record in marked_lines:
            result_doi, result_title, result_author, result_yr, result_type = [],[],[],[],[]
            for i in range(record[0],record[1]+1):
                current_line = database_content[i]

                search_pattern_doi = re.compile(r'DO  - (.*)')
                search_pattern_title = re.compile(r'TI  - (.*)')
                search_pattern_author = re.compile(r'AU  - (.*)')
                search_pattern_year = re.compile(r'YR  - (\d{4})')
                search_pattern_study_type = re.compile(r'MH  - (.* stud\w+)', re.DOTALL)

                result_doi += search_pattern_doi.findall(current_line)
                result_title += search_pattern_title.findall(current_line)
                result_author += search_pattern_author.findall(current_line)
                result_yr += search_pattern_year.findall(current_line)
                result_type += search_pattern_study_type.findall(current_line)

            if result_doi != []:
                for i in range(len(result_doi)):
                    if current_doi in result_doi[i]:
                        result_dictionary['Ausgelesene DOI']+=result_doi
                        result_dictionary['Titel']+=result_title
                        result_dictionary['Jahr']+=result_yr
                        result_dictionary['Studientyp']+=result_type
                        result_dictionary['Authoren']+=result_author

    result_filename = find_file(result_dictionary, current_doi)
    if not result_filename == False:
        result_dictionary = result_filename
    result_dictionary['Ausgelesene DOI'].sort()
    result_dictionary['Titel'].sort()
    result_dictionary['Jahr'].sort()
    result_dictionary['Studientyp'].sort()
    result_dictionary['Authoren'].sort()
    result_dictionary['Dateiname'].sort()
    return result_dictionary

def find_file(results, current_doi):
    file_list = os.listdir(txt_dir)
    major_match=False
    for txt_file in file_list:
        match = False
        # txt_content = open(txt_dir+'\\'+txt_file, 'r', encoding='utf-8').read()
        txt_content = open(txt_dir+'\\'+txt_file, 'r', encoding='utf-8').readlines()
        # txt_content = ' '.join(txt_content.split('\n'))
        results.setdefault('Dateiname', [])
        for i in range(len(txt_content)):
            if current_doi in txt_content[i]:
                match = True
                results['Dateiname']+=[str(round(i/len(txt_content),2))+' '+txt_file.rstrip('.txt')+'.pdf (DOI)\n' ]
            if len(results['Titel'][0]) > 20 and results['Titel'][0][:25] in txt_content[i]:
                match = True
                results['Dateiname']+=[str(round(i/len(txt_content),2))+' '+txt_file.rstrip('.txt')+'.pdf (Part-TITEL)\n' ]
            if results['Titel'][0] in txt_content[i]:
                match = True
                results['Dateiname']+=[str(round(i/len(txt_content),2))+' '+txt_file.rstrip('.txt')+'.pdf (TITEL)\n' ]
            if match:
                searched_file=txt_file
                major_match=True
    if major_match:
        print('Found File: '+searched_file+' DOI: '+current_doi)
        return results
    else:
        return False
        


def find_records_in_db(file_lines):
    # Check lines for begin and end of an entry and save the positions
    record = ''
    counter = 1
    records = []
    for i in range(len(file_lines)):
        if "Record" in file_lines[i] or "Result:" in file_lines[i] or '<'+str(counter)+'. >' in file_lines[i] or '<Trial>' in file_lines[i] or "Study "+str(counter) in file_lines[i] or '<study ' in file_lines[i]:
            if record == '':
                record = i
            else:
                records += [(record,i)]
                record = i
            counter += 1
    return records


searched_dois = init_script()
active_sheet = init_excel(searched_dois)
process_active_sheet(active_sheet)
excel_sheet.save(excel_dir+'//'+file_name)
ctypes.windll.user32.MessageBoxW(0, "Processing done", "search_study_information.py", 1)
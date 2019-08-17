#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: search_all.py
# Project: py
#-----
# Created Date: Thursday 08.08.2019, 23:52
# Author: rbald
#-----
# Last Modified: Saturday 17.08.2019, 00:42
#-----
# Copyright (c) 2019 rbald
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Search working directory for docx files and get Questions out of them
####
import os, docx, hashlib
from sys import argv

def get_root():
    # Konfigurationsdatei einlesen
    os.chdir(os.path.dirname(__file__))
    rel_dir = "../conf/cards.ini"
    file_reader = open(rel_dir)
    file_content = file_reader.readlines()
    file_reader.close()
    for line in file_content:
        if "ROOT_PATH" in line:
            ROOT_PATH = line.lstrip('ROOT_PATH="')
            ROOT_PATH = ROOT_PATH.rstrip('"\n')
        elif "DOK_PATH" in line:
            DOK_PATH = line.lstrip('DOK_PATH="')
            DOK_PATH = DOK_PATH.rstrip('"\n')
        elif "QUESTION_START" in line:
            LINE_START = line.lstrip('QUESTION_START="')
            LINE_START = LINE_START.rstrip('"\n')
        elif "SEPERATOR" in line:
            SEPERATOR = line.lstrip('SEPERATOR="')
            SEPERATOR = SEPERATOR.rstrip('"\n')

    return ROOT_PATH, DOK_PATH, LINE_START, SEPERATOR

ROOT_PATH, DOK_PATH, LINE_START, SEPERATOR = get_root()

def search_all(doc_directory):
    # Alle docx Dateien im angegebenen Ordner finden
    result_list = []
    os.chdir(doc_directory)
    file_list = os.walk(doc_directory)
    for foldername in file_list:
        for filename in foldername[2]:
            if filename.endswith("docx"):
                result_list += [foldername[0]+"/"+filename]
    
    process_files(result_list)

def process_files(array):
    # Erstelle Fragen-Array
    found_questions = {}
    for name in array:
        found_questions = process_dox(found_questions, name)
    create_files(found_questions)

def process_dox(found_questions, name):
    # Lese jede Zeile aus dem Worddokument aus und prüfe diese auf das Suchmuster
    doc_file = docx.Document(name)
    for i in range(len(doc_file.paragraphs)):
        if  LINE_START+SEPERATOR in doc_file.paragraphs[i].text:
            raw_question = doc_file.paragraphs[i].text
            raw_question = raw_question.lstrip(LINE_START+SEPERATOR).split(SEPERATOR)
            found_questions.setdefault(raw_question[0], [])
            found_questions[raw_question[0]] += [(raw_question[1], raw_question[2], name)]
    return found_questions

def create_files(questions, root_dir = ROOT_PATH):
    # Erstelle Files anhand der gefundenen Daten
    count=0

    for fach in questions.keys():
        if "/" in fach:
            temp_array = []
            path = fach.split("/")
            for entry in path:
                temp_array += [entry.capitalize()]
            path = "/".join(temp_array)
        else:
            path = fach.capitalize()

        if not os.path.exists(root_dir+"/cards/"+path):
            os.mkdir(root_dir+"/cards/"+path)
            
        current_dir = root_dir+"/cards/"+path
        
        for frage in questions[fach]:
            # Generiere Dateiname aus Hashwert der Frage
            filename = hashlib.md5(bytes(frage[0], "utf-8"))
            filename = filename.hexdigest()+".php"
            
            if not os.path.exists(current_dir+"/"+filename):
                question = '"'+frage[0]+'"'
                answer = '"'+frage[1]+'"'
                file_path = '"'+frage[2]+'"'
                
                output = "<?php\n\t$q = {};\n\t$a = {};\n\t$f = {};\n\t$s = 0;\n\t$ra = 0;\n\t$fa = 0;\n?>".format(question, answer, file_path)
                file_writer = open(current_dir+"/"+filename, "w", encoding="utf-8")
                file_writer.write(output)
                file_writer.close()
                count+=1
    
    print("<div class='output_message good'>Karteikarten erstellt: "+str(count)+'</div>')

def search_folder():
    folder = argv[2]
    if folder.endswith('"'):
        folder = folder.strip('"')
    if os.path.exists(folder) and os.path.isdir(folder):
        search_all(doc_directory=folder)
    else:
        print("<div class='output_message error'>Pfad ung&#252;ltig "+argv[2]+"</div>")
        
def search_file():
    filename = argv[2]
    if filename.endswith('"'):
        filename = filename.strip('"')
    if os.path.exists(filename) and os.path.isfile(filename):
        found_questions = process_dox({}, filename)
        create_files(found_questions)
    else:
        print("<div class='output_message error'>Dateipfad ung&#252;ltig"+argv[2]+"</div>")
        
def add_data(root_dir = ROOT_PATH+"/cards"):
    if os.path.exists(root_dir+'/'+argv[4]):
        fach = argv[4]
        frage = argv[2]
        antwort = argv[3]
        if fach != "" and frage != "" and antwort != "":
            question_array = {fach : [(frage, antwort, "Manuell erstellt")]}
            create_files(question_array)
        else:
            print("<div class='output_message error'>Fehlende Angaben</div>")
    else:
        print("<div class='output_message error'>Fehlerhafte Ordnerangabe</div>")

# search_all(DOK_PATH)

if argv[1] == "all":
    search_all(DOK_PATH)
elif argv[1] == "dir":
    search_folder()
elif argv[1] == "file":
    search_file()
elif argv[1] == "manadd":
    add_data()
elif argv[1] == "test":
    print('<div class="output_message info">test erfolgreich</div>')
#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: search_all.py
# Project: py
#-----
# Created Date: Thursday 08.08.2019, 23:52
# Author: rbald
#-----
# Last Modified: Friday 09.08.2019, 20:14
#-----
# Copyright (c) 2019 rbald
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Search working directory for docx files and get Questions out of them
####
import os, docx, hashlib
from sys import argv

def search_all(doc_directory = "C:/Users/rbald/OneDrive/Dokumente"):
    result_list = []
    os.chdir(doc_directory)
    file_list = os.walk(doc_directory)
    for foldername in file_list:
        for filename in foldername[2]:
            if filename.endswith("docx"):
                result_list += [foldername[0]+"/"+filename]
    
    process_files(result_list)

def process_files(array):
    found_questions = {}
    for name in array:
        found_questions = process_dox(found_questions, name)
    create_files(found_questions)

def process_dox(found_questions, name):
    doc_file = docx.Document(name)
    for i in range(len(doc_file.paragraphs)):
        if  "//qa<" in doc_file.paragraphs[i].text:
            raw_question = doc_file.paragraphs[i].text
            raw_question = raw_question.lstrip("//qa<").split("<")
            found_questions.setdefault(raw_question[0], [])
            found_questions[raw_question[0]] += [(raw_question[1], raw_question[2], name)]
    return found_questions

def create_files(questions, root_dir = "C:/Users/rbald/OneDrive/Dokumente/http/lernkarten/"):
    count=0
    for fach in questions.keys():
        if not os.path.exists(root_dir+"cards/"+fach):
            os.mkdir(root_dir+"cards/"+fach)
            
        current_dir = root_dir+"cards/"+fach
        
        for frage in questions[fach]:
            filename = hashlib.md5(bytes(frage[0], "utf-8"))
            filename = filename.hexdigest()+".php"
            
            if not os.path.exists(current_dir+"/"+filename):
                question = '"'+frage[0]+'"'
                answer = '"'+frage[1]+'"'
                file_path = '"'+frage[2]+'"'
                score = "0"
                
                output = "<?php\n\t$q = {};\n\t$a = {};\n\t$f = {};\n\t$s = {};\n?>".format(question, answer, file_path, score)
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
        
def add_data(root_dir = "C:/Users/rbald/OneDrive/Dokumente/http/lernkarten/cards"):
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



        
if argv[1] == "all":
    search_all()
elif argv[1] == "dir":
    search_folder()
elif argv[1] == "file":
    search_file()
elif argv[1] == "manadd":
    add_data()
elif argv[1] == "test":
    print('<div class="output_message info">test erfolgreich</div>')
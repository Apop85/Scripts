#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: create_cards.py
# Project: lernkarten
#-----
# Created Date: Thursday 08.08.2019, 18:59
# Author: rbald
#-----
# Last Modified: Thursday 08.08.2019, 21:55
#-----
# Copyright (c) 2019 rbald
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Search trough docx files for a Q/A - Pattern and create a flip card out of it
####

import re, os, docx, hashlib
from pyperclip import copy, paste




def main_menu():
    while True:
        menu_items = { 1: "Alle Dokumente durchsuchen",
                    2: "Bestimmten Ordner durchsuchen",
                    3: "Bestimmtes File durchsuchen",
                    0: "Beenden" }
        print_menu(menu_items)
        choose = input("Auswahl: ")
        if choose.isdecimal() and int(choose) in menu_items.keys():
            if choose == "1":
                search_all()
            elif choose == "2":
                search_folder()
            elif choose == "3":
                search_file()
            elif choose == "0":
                exit()
            else:
                print(" INPUT WRONG ".center(100, "☻"))

def print_menu(menu_elements):
    print('█'*100)
    for key in menu_elements.keys():
        if key != 0:
            print('█'+str(key).center(5)+": █"+menu_elements[key].center(100-10)+"█")
        else:
            print('█'*100)
            print('█'+str(key).center(5)+": █"+menu_elements[key].center(100-10)+"█")
            print('█'*100)


def search_all(doc_directory = "C:/Users/rbald/OneDrive/Dokumente"):
    result_list = []
    os.chdir(doc_directory)
    file_list = os.walk(doc_directory)
    for foldername in file_list:
        for filename in foldername[2]:
            if filename.endswith("docx"):
                result_list += [foldername[0]+"/"+filename]
    
    process_files(result_list)


def search_folder():
    print("<"*50+">"*50)
    folder = input("Ordnerpfad angeben: ")
    if folder.endswith('"'):
        folder = folder.strip('"')
    if os.path.exists(folder) and os.path.isdir(folder):
        search_all(doc_directory=folder)
            

def search_file():
    print("<"*50+">"*50)
    filename = input("Dateipfad angeben: ")
    if filename.endswith('"'):
        filename = filename.strip('"')
    if os.path.exists(filename) and os.path.isfile(filename):
        found_questions = process_dox({}, filename)
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

def process_files(array):
    found_questions = {}
    count = 0
    percent_per_file = 100/len(array)
    for name in array:
        count+=1
        print(">"*(4+int(percent_per_file)*count)+"\r"*150, end="")
        found_questions = process_dox(found_questions, name)
    print()
    create_files(found_questions)

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
                
                output = "<?php\n\t$q = {};\n\t$a = {};\n\t$f = {};\n?>".format(question, answer, file_path)
                file_writer = open(current_dir+"/"+filename, "w", encoding="utf-8")
                file_writer.write(output)
                file_writer.close()
                count+=1
    
    print("Karteikarten erstellt: "+str(count))
    print()



    
while True:
    main_menu()
        
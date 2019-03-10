#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 05_chapter_09_repetition_task_3_phone_book.py
# Project: 05_chapter_09_repetition_task_3_phone_book
# Created Date: Sunday 10.03.2019, 15:14
# Author: Apop85
# -----
# Last Modified: Sunday 10.03.2019, 18:25
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 9. Page 275. Task 3. Script to manage a phone book and 
# search numbers by names.
###
import pickle,os
os.chdir(os.path.dirname(__file__))
phone_file='.\\phonebook.dmp'

if os.path.exists(phone_file):
    with open(phone_file, 'rb') as file_writer:
        phone_book=pickle.load(file_writer)
else:
    phone_book={}

def menu():
    while True:
        entrys=[(1,'Create new entry'),(2,'Search by name'),(3,'Show all entrys'),(4,'Delete phone book'),(5,'Exit')]
        for entry in entrys:
            print(format(entry[0],'5')+'.\t'+entry[1])
        choosen=input()
        if choosen.isdecimal() and 0 < int(choosen) < len(entrys)+1:
            if choosen == '1':
                new_entry()
            elif choosen == '2':
                search_name()
            elif choosen == '3':
                show_all()
            elif choosen == '4':
                delete()
            elif choosen == '5':
                return

def new_entry():
    global phone_book
    name=input('Enter full name: ')
    name=tuple(name.split())
    phone=input('Enter phone number: ')
    phone_book.setdefault(name, phone)
    try:
        with open(phone_file, 'wb') as file_writer:
            pickle.dump(phone_book, file_writer)
    except Exception:
        print('Not able to write new entry in file! Entry not saved.')

def search_name():
    global phone_book
    result=[]
    search_term=input('Enter name: ')
    if len(search_term.split()) > 1:
        search_term=tuple(search_term.split())
    if phone_book.get(search_term, None) == None:
        for key in list(phone_book.keys()):
            if search_term in key:
                result+=[[key,phone_book[key]]]
    else:
        result+=[[search_term,phone_book[search_term]]]
    if len(result) == 0:
        print('No entry found with this name.\n')
        return
    print('Name'.center(20)+'|'+'Number'.center(20))
    print('-'*41)
    for single_result in result:
        print(' '.join(single_result[0]).center(20)+'|'+single_result[1].center(20))
    input()

def show_all():
    print('Name'.center(20)+'|'+'Number'.center(20))
    print('-'*41)
    for key in phone_book:
        print(' '.join(key).center(20)+'|'+phone_book[key].center(20))
    input()

def delete():
    os.remove(phone_file)

menu()


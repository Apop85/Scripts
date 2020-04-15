#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: create_links.py
# Project: excelllink
#-----
# Created Date: Wednesday 15.04.2020, 12:29
# Author: Raffael Baldinger
#-----
# Last Modified: Wednesday 15.04.2020, 12:29
#-----
# Copyright (c) 2020 Raffael Baldinger
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: This script replaces network paths with links inside a excel file
####


import os, openpyxl, re
from pyperclip import copy, paste
from time import sleep


def print_menu(menu_items, menu_name, infotext):
    # Width of the menu
    menu_width = 60
    # Fill character
    filler = "█"
    # Search pattern to split the infotext into lines
    pattern = re.compile(r'.{0,' + str(menu_width-5) + r'}[ .]', re.DOTALL)
    info_lines = pattern.findall(infotext)

    # Print infotext
    print(filler*menu_width)
    for infoline in info_lines:
        print(filler + " " + infoline.center(menu_width-4) + " " + filler)
    
    # Print menu title
    print(filler*menu_width)
    print(filler + menu_name.center(menu_width-2)+filler)
    print(filler*menu_width)

    # Print menu items
    for key in menu_items.keys():
        if key == "0":
            print(filler*menu_width)
        print(filler + str(key).center(int(menu_width/4-1)) + menu_items[key].ljust(int(menu_width/4*3-1)) + filler)
    print(filler*menu_width)

def main_menu():
    menu_name = "HAUPTMENÜ"
    options = { "1": "Pfad manuell angeben",
                "2": "Pfad aus zwischenablage",
                "0": "Beenden"
              }
    infotext = "Dieses Script ersetzt die Pfadangaben in einem Excelfile mit direkten Links zu diesen Pfad um diesen mit einem klick öffnen zu können."
    print_menu(options, menu_name, infotext)
    choice = input("Auswahl: ")
    # Check choice
    if choice in options.keys():
        if choice == "1":
            get_by_input()
        if choice == "2":
            get_by_clipboard()
        if choice == "0":
            exit()

def check_input(excel_path):
    # Remove "" from the string    
    if excel_path.endswith('"'):
        excel_path = excel_path.strip('"')
    if os.path.exists(excel_path) and os.path.isfile(excel_path) and excel_path.endswith("xlsx"):
        print("\nINFO: Datei gefunden.\n")
        return True, excel_path
    else:
        print("\nFEHLER: Angegebener Pfad existiert nicht oder ist keine xlsx-Datei!\n")
        return False, excel_path

def get_by_input():
    # Function to get the path of the excel file by user input
    found = False
    while not found:
        print("\n Bitte Pfad angeben:")
        excel_path = input()
        found, excel_path = check_input(excel_path)
       
    edit_excelfile(excel_path)


def get_by_clipboard():
    # Set clipboard to "0"
    found = False
    while not found:
        copy("0")
        print("Warte auf Pfad aus Zwischenablage")
        while paste() == "0":
            sleep(0.5)
        print("\nINFO: Zwischenablage wurde gefunden")
        excel_path = paste()
        found, excel_path = check_input(excel_path)

    edit_excelfile(excel_path)

def edit_excelfile(filepath):
    # Open excel file
    excel_file = openpyxl.load_workbook(filepath)
    # Read out workbook names
    sheet_names = excel_file.sheetnames
    # Select first workbook
    active_sheet = excel_file[sheet_names[0]]
    # Get max rows
    max_row = active_sheet.max_row
    # Get max colums
    max_col = active_sheet.max_column
    # Define patten of the future links
    path_pattern = re.compile(r'\\\\[A-Za-z0-9]+\\.*')
    current_path, counter = "", 0
    # Iterate trough all data
    for row in range(2, max_row+1):
        for col in range(1, max_col+1):
            # Translate integer to row letter
            col_letter = openpyxl.utils.get_column_letter(col)
            # Read cell value
            active_cell_value = (active_sheet[col_letter+str(row)].value)
            if active_cell_value != None:
                # If value is path
                if path_pattern.match(active_cell_value):
                    current_path = active_cell_value
                    active_sheet[col_letter+str(row)].hyperlink = "file:///"+current_path
                    active_sheet[col_letter+str(row)].value = current_path
                    counter += 1
                # If value is subfolder
                elif current_path != "" and not "->" in active_cell_value:
                    link_path = current_path + "\\" + active_cell_value
                    active_sheet[col_letter+str(row)].hyperlink = "file:///" + link_path
                    active_sheet[col_letter+str(row)].value = active_cell_value
                    counter += 1
                # if value contains "->"
                elif current_path != "" and "->" in active_cell_value:
                    link_path = current_path
                    active_sheet[col_letter+str(row)].hyperlink = "file:///"+current_path
                    counter += 1

    excel_file.save(filepath)
    print("Es wurden "+ str(counter) +" links erzeugt.")
    input("Enter zum fortfahren...")
    

while True:
    main_menu()
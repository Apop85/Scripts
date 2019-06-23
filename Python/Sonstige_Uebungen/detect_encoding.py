#!/usr/bin/env python3
# -*- coding:utf-8 -*-
#File: c:\Users\Apop85\Desktop\ScriptProjekte\GitHub\Python\Sonstige_Uebungen\detect_encoding.py
#Project: c:\Users\Apop85\Desktop\ScriptProjekte\GitHub\Python\Sonstige_Uebungen
#Created Date: Sunday 23.06.2019, 17:19
#Author: Apop85
#-----
#Last Modified: Sunday 23.06.2019, 19:27
#-----
#Copyright (c) 2019 Apop85
#This software is published under the MIT license.
#Check http://www.opensource.org/licenses/MIT for further informations
#-----
#Description: Checks a file for useable encodings

import os
from pyperclip import copy, paste
from time import sleep

def setup_vars(mode):
    # Returns the wanted encodings
    encodings = ['ascii','big5','big5hkscs','cp037','cp424','cp437','cp500','cp737','cp775','cp850','cp852','cp855',
                'cp856','cp857','cp860','cp861','cp862','cp863','cp864','cp865','cp866','cp869','cp874','cp875',
                'cp932','cp949','cp950','cp1006','cp1026','cp1140','cp1250','cp1251','cp1252','cp1253','cp1254',
                'cp1255','cp1256','cp1257','cp1258','euc_jp','euc_jis_2004','euc_jisx0213','euc_kr','gb2312','gbk',
                'gb18030','hz','iso2022_jp','iso2022_jp_1','iso2022_jp_2','iso2022_jp_2004','iso2022_jp_3','iso2022_jp_ext',
                'iso2022_kr','latin_1','iso8859_2','iso8859_3','iso8859_4','iso8859_5','iso8859_6','iso8859_7','iso8859_8',
                'iso8859_9','iso8859_10','iso8859_13','iso8859_14','iso8859_15','johab','koi8_r','koi8_u','mac_cyrillic',
                'mac_greek','mac_iceland','mac_latin2','mac_roman','mac_turkish','ptcp154','shift_jis','shift_jis_2004',
                'shift_jisx0213','utf_16','utf_16_be','utf_16_le','utf_7','utf_8']

    special_encodings = ['base64_codec','bz2_codec','hex_codec','idna','mbcs','palmos','punycode','quopri_codec',
                        'raw_unicode_escape','rot_13','string_escape','undefined','unicode_escape','unicode_internal',
                        'uu_codec','zlib_codec']

    encoding_list = []
    if mode[0]:
        encoding_list += encodings
    if mode[1]:
        encoding_list += special_encodings
    
    return encoding_list

def choose_option():
        options = {1: ['Scan for all encodings', (True, True)],
                    2: ['Scan for int. encodings', (True, False)],
                    3: ['Scan for special encodings', (False, True)],
                    0: ['Exit']}

        choose = print_menu(options)

        if choose == 0:
            exit()
        return options[choose][1]

def print_menu(options):
    # Takes a Dictionary with an integer as key and a list as values 
    while True:    
        print('█'*70)
        for key in options.keys():
            if key != 0:
                print('█'+str(key).rjust(15)+'.  '+options[key][0].ljust(50)+'█')
            else:            
                print('█'*70)
                print('█'+str(key).rjust(15)+'.  '+options[key][0].ljust(50)+'█')
        print('█'*70)
        choose = input('Choose Option: '.rjust(30, '>'))

        # Check if choice is a valid option
        if choose.isdecimal() and 0 <= int(choose) <= len(options.keys()):
            choose = int(choose)
            return choose
        else:
            print(' Invalid choice! '.center(70,'#'))

def get_file_path():
    # Get path from clipboard
    while True:
        copy(0)
        while paste() == '0':
            sleep(0.5)
            message = 'Copy path/to/fi.le to clipboard. Waiting for input...'
            print('\r'*len(message)+message, end='')
        print()
        print('Checking path...'.center(70, '/'), end='')
        if os.path.exists(paste().strip('"')):
            print('OK')
            file_path = paste().strip('"')
            return file_path
        else:
            print('Invalid')
            print(' Path invalid! '.center(70, '#'))


def check_encoding(path, mode):
    encoding_list = setup_vars(mode)
    result = {}

    # Loop trough all possible encodings and check if the file is readable
    for codec in encoding_list:
        message = 'Checking codec: '+codec+' '*50
        print('\r'*100+message, end='')
        try:
            tmp = open(path,'r',encoding=codec).read()
            del tmp
            check = True
        except:
            check = False
        result.setdefault(codec, check)
    print()
    return result

def show_result(results):
    options = {1: ['Positive results', (True, False)], 
                2: ['Negative results', (False, True)], 
                3: ['All results', (True,True)]}

    choose = print_menu(options)

    # Prints a table of all results
    print('╔'+'═'*33+'╦'+'═'*32+'╗')
    true_chk = 0
    false_chk = 0
    for key in results.keys():
        if options[choose][1][0] and results[key]:
            # If show True is on and result is True 
            print('║'+key.center(33)+'║'+str(results[key]).center(32)+'║')
            print('╠'+'═'*33+'╬'+'═'*32+'╣')
            true_chk += 1
        elif options[choose][1][1] and not results[key]:
            # If show False is on and result is False
            print('║'+key.center(33)+'║'+str(results[key]).center(32)+'║')
            print('╠'+'═'*33+'╬'+'═'*32+'╣')
            false_chk += 1

    print('║'+'Positives'.center(33)+'║'+str(true_chk).center(32)+'║')
    print('║'+'Negatives'.center(33)+'║'+str(false_chk).center(32)+'║')
    print('║'+'TOTAL CHECKED'.center(33)+'║'+str(len(list(results.keys()))).center(32)+'║')
    print('╚'+'═'*33+'╩'+'═'*32+'╝')

scan_mode = choose_option()
file_path = get_file_path()
result = check_encoding(file_path, scan_mode)
show_result(result)

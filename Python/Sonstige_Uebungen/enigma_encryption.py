#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: enigma_encryption.py
# Project: Sonstige_Uebungen
# Created Date: Thursday 28.02.2019, 23:33
# Author: Apop85
# -----
# Last Modified: Friday 01.03.2019, 01:05
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Create enigma encrypted strings.
###

def alphabet_reset():
    global alphabet
    alphabet='abcdefghijklmnopqrstuvwxyzäöü ?!,.0123456789"\'/@éàè€%'

def counter(a,b,c):
    global alphabet
    for i in range(a,a+len(alphabet)):
        alphabet=alphabet[i:]+alphabet[:i]
        for j in range(b,b+len(alphabet)):
            alphabet=alphabet[j:]+alphabet[:j]
            for k in range(c,c+len(alphabet)):
                alphabet=alphabet[k:]+alphabet[:k]
                return a%len(alphabet),b%len(alphabet),c%len(alphabet),int(str(i)+str(j)+str(k))

def encrypt(string,a=1,b=1,c=1):
    alphabet_reset()
    encrypted=''
    for i in range(len(string)):
        a,b,c,counter_now=counter(a,b,c+1)
        letter=alphabet[(counter_now+list(alphabet).index(string[i].lower()))%len(alphabet)]
        encrypted+=letter.upper()
    return encrypted

def decrypt(string,a=1,b=1,c=1):
    alphabet_reset()
    decrypted=''
    for i in range(len(string)):
        a,b,c,counter_now=counter(a,b,c+1)
        letter=alphabet[(-counter_now+list(alphabet).index(string[i].lower()))%len(alphabet)]
        decrypted+=letter.upper()
    return decrypted

def input_message():
    string=input('Enter message: ')
    while True:
        mode=input('Encrypt (0) or decrypt (1): ')
        if mode.isdecimal() and int(mode) in [0,1]:
            mode=int(mode)
            break
    while True:
        print('Enter en-/decoding values seperated with commas (a,b,c):')
        numbers=input('Values: ')
        if len(numbers.split(',')) == 3:
            a,b,c=numbers.split(',')
            if a.isdecimal() and b.isdecimal() and c.isdecimal():
                a,b,c=int(a), int(b), int(c)
                break
    return string,mode,a,b,c

def print_it(string, value, mode):
    print(mode[0]+'ed message: '+string+'\n'+mode[1]+'ion value: '+','.join(value))

while True:
    string,mode,a,b,c=input_message()
    print(''.center(70,'█'))
    if mode == 0:
        message=encrypt(string,a,b,c)
        print_it(message,[str(a),str(b),str(c)],('Encrypt','Decrypt'))
    elif mode == 1:
        message=decrypt(string,a,b,c)
        print_it(message,[str(a),str(b),str(c)],('Decrypt',"Encrypt"))
    else:
        break
    print(''.center(70,'█'))

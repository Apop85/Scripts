#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: enigma_encryption_2.py
# Project: Sonstige_Uebungen
# Created Date: Saturday 02.03.2019, 06:31
# Author: Apop85
# -----
# Last Modified: Saturday 02.03.2019, 15:34
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Second attempt to create an encrypted string by enigma algorithm.
###

def shuffle(a=5,b=5,c=5,alphabet='abcdefghijklmnopqrstuvwxyz0123456789"\'/\\@éàèç€%_-=¢$|¬§°~#´^<>+*)(äöü ?!,.;:'):
    for i in range(len(alphabet)):
        p1=alphabet[len(alphabet)//2:]
        p2=alphabet[:len(alphabet)//2]
        alphabet=p1+p2
        for j in range(len(alphabet)):
            p1=alphabet[:len(alphabet)//5]
            p2=alphabet[len(alphabet)//5:len(alphabet)//5*2]
            p3=alphabet[len(alphabet)//5*2:len(alphabet)//5*3]
            p4=alphabet[len(alphabet)//5*3:len(alphabet)//5*4]
            p5=alphabet[-len(alphabet)//5:]
            alphabet=p1+p5+p2+p4+p3
            for k in range(len(alphabet)):
                p1=alphabet[0]
                p2=alphabet[1]
                p3=alphabet[2]
                p4=alphabet[3:]
                alphabet=p2+p4+p3+p1
                if (i,j,k) >= (a%len(alphabet),b%len(alphabet),c%len(alphabet)):
                    yield alphabet

def encrypt_message(a,b,c,string):
    alphabet=shuffle(a,b,c)
    encrypted=''
    for i in range(len(string)):
        current_iteration=next(alphabet)
        letter=current_iteration[current_iteration.index(string[i].lower())-i]
        encrypted+=letter.upper()
    return encrypted

def decrypt_message(a,b,c,string):
    alphabet=shuffle(a,b,c)
    decrypted=''
    for i in range(len(string)):
        current_iteration=next(alphabet)
        letter=current_iteration[(current_iteration.index(string[i].lower())+i)%len(current_iteration)]
        decrypted+=letter.upper()
    return decrypted

def get_information():
    message=input('Your message: ')
    while True:
        mode=input('Encrypt(0) or decrypt(1): ')
        if mode.isdecimal() and int(mode) in [0,1]:
            break
    while True:
        values=input('Encryption values seperated by comma: ')
        if ''.join(values.split(',')).isdecimal() and len(values.split(',')) == 3:
            a=int(values.split(',')[0])
            b=int(values.split(',')[1])
            c=int(values.split(',')[2])
            break
        else:
            print('Example: 1,2,3')
    return message, int(mode), a, b, c

def print_it(message,values,mode):
    print(mode[0]+'ed message: »»'+message+'««\n'+mode[1]+'ion value: '+','.join(values))

while True:
    message,mode,a,b,c=get_information()
    print(''.center(70,'█'))
    if mode == 0:
        encrypted=encrypt_message(a,b,c,message)
        print_it(encrypted,[str(a),str(b),str(c)],('Encrypt','Decrypt'))
    elif mode == 1:
        decrypted=decrypt_message(a,b,c,message)
        print_it(decrypted,[str(a),str(b),str(c)],('Decrypt','Encrypt'))
    print(''.center(70,'█'))

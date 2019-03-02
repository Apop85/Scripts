#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: enigma_encryption_2.py
# Project: Sonstige_Uebungen
# Created Date: Saturday 02.03.2019, 06:31
# Author: Apop85
# -----
# Last Modified: Saturday 02.03.2019, 20:08
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Second attempt to create an encrypted string by enigma algorithm.
###


def shuffle(a=5,b=5,c=5,alphabet='abcdefghijklmnopqrstuvwxyz0123456789"\'/\\@éàèç€%_-=¢$|¬§°~#´^<>+*)(äöü ?!,.;:'):
    global length_original
    if length_original == '':
        length_original=len(alphabet)
    for i in range(len(alphabet)):
        if len(alphabet)%2 != 0:
            alphabet+='█'
        p1=alphabet[len(alphabet)//2:].strip('█')
        p2=alphabet[:len(alphabet)//2]
        alphabet=p1+p2
        for j in range(len(alphabet)):
            if len(alphabet)%5 != 0:
                alphabet=alphabet+'█'*(5-(len(alphabet)%5))
            p1=alphabet[:len(alphabet)//5]
            p2=alphabet[len(alphabet)//5:len(alphabet)//5*2]
            p3=alphabet[len(alphabet)//5*2:len(alphabet)//5*3]
            p4=alphabet[len(alphabet)//5*3:len(alphabet)//5*4]
            p5=alphabet[-len(alphabet)//5:].strip('█')
            alphabet=p1+p5+p2+p4+p3
            for k in range(len(alphabet)):
                p1=alphabet[0]
                p2=alphabet[1]
                p3=alphabet[2]
                p4=alphabet[3:]
                alphabet=p2+p4+p3+p1
                if len(alphabet) != length_original:
                    raise Exception('AlphabetError: Algorythm not matching current alphabet length of '+str(length_original)+'!')
                if (i,j,k) >= (a%len(alphabet),b%len(alphabet),c%len(alphabet)):
                    while (i,j,k) == (len(alphabet)-1,)*3:
                        alphabet=next(shuffle(0,0,0,alphabet))
                        yield alphabet
                    yield alphabet

def encrypt_message(a,b,c,string,alphabet,values):
    try:
        if alphabet == '':
            alphabet=shuffle(a,b,c)
        else:
            alphabet=shuffle(a,b,c,alphabet)
        encrypted=''
        for i in range(len(string)):
            current_iteration=next(alphabet)
            letter=current_iteration[(current_iteration.index(string[i].lower())-i)%len(current_iteration)]
            encrypted+=letter.upper()
        if values != '':
            encrypted=change_it(encrypted,values)
        return encrypted
    except Exception as error_message:
            raise Exception(error_message)
        
def decrypt_message(a,b,c,string,alphabet,values):
    try:
        if alphabet == '':
            alphabet=shuffle(a,b,c)
        else:
            alphabet=shuffle(a,b,c,alphabet)
        decrypted=''
        for i in range(len(string)):
            current_iteration=next(alphabet)
            letter=current_iteration[(current_iteration.index(string[i].lower())+i)%len(current_iteration)]
            decrypted+=letter.upper()
        if values != '':
            decrypted=change_it(decrypted,values)
        return decrypted
    except:
        raise Exception('Unable to decrypt. Character flips not correlating with given alphabet.')

def change_it(message,values):
    char_list=list(message)
    for value in values:
        for i in range(len(char_list)):
            if value[0].upper() == char_list[i].upper():
                char_list[i] = value[1].upper()
            elif value[1].upper() == char_list[i].upper():
                char_list[i] = value[0].upper()
    return ''.join(char_list)
                
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
    while True:
        invalid=False
        print('Set custom character flips seperated by comma \n(Example: AB,9F,+N) or press enter to use none.')
        exchanges_list=input('Custom flips: ')
        if exchanges_list != '':
            exchanges_list=exchanges_list.split(',')
            for item in exchanges_list:
                if len(item) != 2:
                    print('Exchange value need to have a length of 2. Invalid value: '+item)
                    invalid=True
            if invalid:
                continue
            message=change_it(message,exchanges_list)
        break
    while True:
        print('Enter custom alphabet or press enter to use default:')
        custom_alpha=input()
        if custom_alpha != '':
            found=False
            for char in message:
                if char.lower() not in custom_alpha.lower():
                    if not found:
                        print('Invalid alphabet. Adding missing character:', end=' ')
                    found=True
                    print(char.lower()+',', end=' ')
                    custom_alpha+=char.lower()
            custom_alpha=list(custom_alpha)
            custom_alpha.sort()
            custom_alpha=''.join(custom_alpha)
            print()
        break
    return message.upper(), int(mode), a, b, c, custom_alpha, exchanges_list

def print_it(message,values,mode,alphabet,flips):
    print(mode[0]+'ed message: »»'+message+'««\n'+mode[1]+'ion value: »»'+','.join(values)+'««')
    if custom_alpha == '':
        print('Used default alphabet.')
    else:
        print('Used custom alphabet:\n»»'+custom_alpha+'««')
    if flips != '':
        print('Used character flips: '+','.join(flips))

while True:
    try:
        length_original=''
        print(''.center(70,'█'))
        message,mode,a,b,c,custom_alpha,change_list=get_information()
        print(''.center(70,'█'))
        if mode == 0:
            encrypted=encrypt_message(a,b,c,message,custom_alpha,change_list)
            print_it(encrypted,[str(a),str(b),str(c)],('Encrypt','Decrypt'),custom_alpha, change_list)
        elif mode == 1:
            decrypted=decrypt_message(a,b,c,message,custom_alpha,change_list)
            print_it(decrypted,[str(a),str(b),str(c)],('Decrypt','Encrypt'),custom_alpha, change_list)
        print(''.center(70,'█'))
    except Exception as error_message:
        print(''.center(70,'█'))
        print('CryptError: '+str(error_message))
        print(''.center(70,'█'))

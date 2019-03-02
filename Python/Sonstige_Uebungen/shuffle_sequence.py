#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: shuffle_sequence.py
# Project: Sonstige_Uebungen
# Created Date: Saturday 02.03.2019, 06:01
# Author: Apop85
# -----
# Last Modified: Saturday 02.03.2019, 15:33
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Attempt to improve the enigma script
###

def shuffle(a=5,b=5,c=5,alphabet='abcdefghijklmnopqrstuvwxyz'):
    global n_counter, d_counter
    liste=[]
    d_counter,n_counter=0,0
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
                n_counter+=1
                if (i,j,k) >= (a%len(alphabet),b%len(alphabet),c%len(alphabet)):
                    yield alphabet
                if alphabet in liste:
                    d_counter+=1
                liste+=[alphabet]

# print(type(shuffle))
# print(len('abcdefghijklmnopqrstuvwxyzäöü ?!,.0123456789"\'/\\@éàèç€%_-=¢$|¬§°#´^<>+*)(;:'))
# alphabet=shuffle(34,34,34,alphabet='abcdefghijklmnopqrstuvwxyz0123456789')
# alphabet=shuffle(44,44,44,alphabet='abcdefghijklmnopqrstuvwxyz0123456789"\'/\\@éàèç€')
# alphabet=shuffle(54,54,54,alphabet='abcdefghijklmnopqrstuvwxyz0123456789"\'/\\@éàèç€%_-=¢$|¬§°')
# alphabet=shuffle(64,64,64,alphabet='abcdefghijklmnopqrstuvwxyz0123456789"\'/\\@éàèç€%_-=¢$|¬§°°#´^<>+*)(')
alphabet=shuffle(74,74,74,alphabet='abcdefghijklmnopqrstuvwxyz0123456789"\'/\\@éàèç€%_-=¢$|¬§°#´^<>+*)(äöü ?!,.;:~')
# alphabet=shuffle(74,74,74,alphabet='abcdefghijklmnopqrstuvwxyzäöü ?!,.0123456789"\'/\\@éàèç€%_-=¢$|¬§°#´^<>+*)(;:')
# alphabet=shuffle(24,24,24)
print(next(alphabet))
# print(next(alphabet))
# print(next(alphabet))
# print(next(alphabet))
print('Duplicates: '+str(d_counter)+' Total: '+str(n_counter))
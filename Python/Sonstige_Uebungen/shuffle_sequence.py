#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: shuffle_sequence.py
# Project: Sonstige_Uebungen
# Created Date: Saturday 02.03.2019, 06:01
# Author: Apop85
# -----
# Last Modified: Saturday 02.03.2019, 23:05
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Attempt to improve the enigma script
###
original_len=0
def shuffle(a=5,b=5,c=5,alphabet='abcdefghijklmnopqrstuvwxyz'):
    from math import factorial
    a,b,c=a%(len(alphabet)),b%(len(alphabet)),c%(len(alphabet))
    alpha_factorial=factorial(len(alphabet))
    global n_counter, d_counter, original_len
    liste=[]
    d_counter,n_counter=0,0
    i,j,k=0,0,0
    alphabet=list(alphabet)
    if original_len == 0:
        original_len=len(alphabet)
    for z in range(alpha_factorial):
        n_counter+=1
        check_alph=''.join(alphabet)
        if check_alph in liste:
            d_counter+=1
            string='Duplicates: '+str(d_counter)+' Iteration: '+str(n_counter)+' Fail-Rate:'+str(round(100/n_counter*d_counter,2))+'% Generation:'+str(i)+'-'+str(j)+'-'+str(k)+'   '
            print('\r'*len(string)+string, end=' ')
        else:
            liste+=[check_alph]
        k+=1
        if k == len(alphabet):
            k=0
            j+=1
        if j == len(alphabet):
            j=0
            i+=1
        if i == len(alphabet):
            i=0
        p1=alphabet[i]
        del alphabet[i]
        alphabet.insert(-3,p1)
        p2=alphabet[j]
        del alphabet[j]
        alphabet.insert(-2,p2)
        p3=alphabet[k]
        del alphabet[k]
        alphabet.insert(-1,p3)
        if len(alphabet) != original_len:
            raise Exception('AlgorythmError: Algorythm changes length of alphabet.')
        if (i,j,k) == (a,b,c):
            print()
            yield alphabet


alphabet=shuffle(74,74,74,alphabet='abcdefghijklmnopqrstuvwxyzäöü ?!,.0123456789"\'/\\@éàèç€%_-=¢$|¬§°#´^<>+*)(;:')
print(''.join(next(alphabet)))
print('Duplicates: '+str(d_counter)+' Total: '+str(n_counter))
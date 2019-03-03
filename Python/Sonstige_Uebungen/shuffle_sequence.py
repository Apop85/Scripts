#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: shuffle_sequence.py
# Project: Sonstige_Uebungen
# Created Date: Saturday 02.03.2019, 06:01
# Author: Apop85
# -----
# Last Modified: Sunday 03.03.2019, 12:58
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
    global d_counter, original_len
    liste=[]
    d_counter=0
    i,j,k=0,0,0
    alphabet=list(alphabet)
    if original_len == 0:
        original_len=len(alphabet)
    print('Iteration'.center(15)+'|'+'Duplicates'.center(15)+'|'+'Fail-rate'.center(15)+'|'+'Generation'.center(15)+'|'+'Done'.center(15))
    for z in range(alpha_factorial):
        check_alph=''.join(alphabet)
        if check_alph in liste:
            d_counter+=1
        else:
            liste+=[check_alph]
        string=str(z+1).center(15)+'|'+str(d_counter).center(15)+'|'+(str(round(100/(z+1)*d_counter,7))+'%').center(15)+'|'+(str(i)+'-'+str(j)+'-'+str(k)).center(15)+'|'+(str(round(100/(len(alphabet)**3)*z))+'%').center(15)+'   '
        print('\r'*len(string)+string, end=' ')
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
        alphabet.insert((i-1),p1)
        p2=alphabet[j]
        del alphabet[j]
        alphabet.insert(-1,p2)
        p3=alphabet[k]
        del alphabet[k]
        alphabet.insert((k-5),p3)
        if len(alphabet) != original_len:
            raise Exception('AlgorythmError: Algorythm changes length of alphabet.')
        if (i,j,k) == (a,b,c):
            print()
            yield alphabet


alphabet=shuffle(74,74,74,alphabet='abcdefghijklmnopqrstuvwxyzäöü ?!,.0123456789"\'/\\@éàèç€%_-=¢$|¬§°#´^<>+*)(;:')
print(''.join(next(alphabet)))
print('Duplicates: '+str(d_counter)+' Total: '+str(n_counter))
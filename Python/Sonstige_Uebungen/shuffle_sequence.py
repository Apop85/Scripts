#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: shuffle_sequence.py
# Project: Sonstige_Uebungen
# Created Date: Saturday 02.03.2019, 06:01
# Author: Apop85
# -----
# Last Modified: Saturday 02.03.2019, 21:51
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
            string='Duplicates: '+str(d_counter)+' counter: '+str(n_counter)+' Fail-Rate:'+str(round(100/n_counter*d_counter,2))+'% Generation:'+str(i)+'-'+str(j)+'-'+str(k)+'   '
            print('\r'*len(string)+string, end=' ')
        else:
            liste+=[check_alph]
        k+=1
        if k == len(alphabet)-1:
            k=0
            j+=1
        if j == len(alphabet)-1:
            j=0
            i+=1
        if i == len(alphabet)-1:
            i=0
        p1=alphabet[i]
        del alphabet[i]
        alphabet+=p1
        p2=alphabet[j]
        del alphabet[j]
        alphabet+=p2
        p3=alphabet[k]
        del alphabet[k]
        alphabet+=p3
        if len(alphabet) != original_len:
            raise Exception('Länge ändert sich')
        # p1=alphabet[0]
        # p2=alphabet[1]
        # p3=alphabet[2]
        # p4=alphabet[3]
        # p5=alphabet[4]
        # p6=alphabet[5:]
        # alphabet=p5+p1+p6+p3+p4+p2
        if (i,j,k) == (a,b,c):
            print()
            yield alphabet

# def shuffle(a=5,b=5,c=5,alphabet='abcdefghijklmnopqrstuvwxyz'):
#     global n_counter, d_counter
#     liste=[]
#     d_counter,n_counter=0,0
#     for i in range(len(alphabet)):
#         if len(alphabet)%2 != 0:
#             alphabet+='█'
#         p1=alphabet[len(alphabet)//2:].strip('█')
#         p2=alphabet[:len(alphabet)//2]
#         alphabet=p1+p2
#         for j in range(len(alphabet)):
#             if len(alphabet)%5 != 0:
#                 alphabet=alphabet+'█'*(5-len(alphabet)%5)
#             p1=alphabet[:len(alphabet)//5]
#             p2=alphabet[len(alphabet)//5:len(alphabet)//5*2]
#             p3=alphabet[len(alphabet)//5*2:len(alphabet)//5*3]
#             p4=alphabet[len(alphabet)//5*3:len(alphabet)//5*4]
#             p5=alphabet[-len(alphabet)//5:].strip('█')
#             alphabet=p1+p5+p2+p4+p3
#             for k in range(len(alphabet)):
#                 p1=alphabet[0]
#                 p2=alphabet[1]
#                 p3=alphabet[2]
#                 p4=alphabet[3:]
#                 alphabet=p2+p4+p3+p1
#                 n_counter+=1
#                 if (i,j,k) >= (a%len(alphabet),b%len(alphabet),c%len(alphabet)):
#                     yield alphabet
#                 if alphabet in liste:
#                     d_counter+=1
#                 liste+=[alphabet]

# print(type(shuffle))
# print(len('abcdefghijklmnopqrstuvwxyzäöü ?!,.0123456789"\'/\\@éàèç€%_-=¢$|¬§°#´^<>+*)(;:'))
# alphabet=shuffle(35,35,35,alphabet='abcdefghijklmnopqrstuvwxyz0123456789')
# alphabet=shuffle(45,45,45,alphabet='abcdefghijklmnopqrstuvwxyz0123456789"\'/\\@éàèç€')
# alphabet=shuffle(55,55,55,alphabet='abcdefghijklmnopqrstuvwxyz0123456789"\'/\\@éàèç€%_-=¢$|¬§°')
# alphabet=shuffle(65,65,65,alphabet='abcdefghijklmnopqrstuvwxyz0123456789"\'/\\@éàèç€%_-=¢$|¬§°°#´^<>+*)(')
# alphabet=shuffle(75,75,75,alphabet='abcdefghijklmnopqrstuvwxyz0123456789"\'/\\@éàèç€%_-=¢$|¬§°#´^<>+*)(äöü ?!,.;:~')
alphabet=shuffle(74,74,74,alphabet='abcdefghijklmnopqrstuvwxyzäöü ?!,.0123456789"\'/\\@éàèç€%_-=¢$|¬§°#´^<>+*)(;:')
# alphabet=shuffle(24,24,24)
print(next(alphabet))
# print(next(alphabet))
# print(next(alphabet))
# print(next(alphabet))
print('Duplicates: '+str(d_counter)+' Total: '+str(n_counter))
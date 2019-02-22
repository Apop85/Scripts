#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 02_uebung_betrag_in_geldscheine_zerlegen.py
# Project: Kapitel_4_Standarddatentypen
# Created Date: Friday 22.02.2019, 21:14
# Author: Apop85
# -----
# Last Modified: Friday 22.02.2019, 21:44
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: This script will split the input amount into valid cash output.
###


print('-----------------------')
total_cash=float(input('Betrag eingeben: '))
print('-----------------------')

tausender=total_cash//1000
rest_cash=total_cash-tausender*1000
zweihunderter=rest_cash//200
rest_cash=round(rest_cash-zweihunderter*200,2)
hunderter=rest_cash//100
rest_cash=round(rest_cash-hunderter*100,2)
fuenfziger=rest_cash//50
rest_cash=round(rest_cash-fuenfziger*50,2)
zwanziger=rest_cash//20
rest_cash=round(rest_cash-zwanziger*20,2)
zehner=rest_cash//10
rest_cash=round(rest_cash-zehner*10,2)
fuenfer=rest_cash//5
rest_cash=round(rest_cash-fuenfer*5,2)
zweier=rest_cash//2
rest_cash=round(rest_cash-zweier*2,2)
einer=rest_cash//1
rest_cash=round(rest_cash-einer*1,2)
halbe=rest_cash//0.5
rest_cash=round(rest_cash-halbe*0.5,2)
punktzwanzig=rest_cash//0.2
rest_cash=round(rest_cash-punktzwanzig*0.2,2)
punktzehn=rest_cash//0.1
rest_cash=round(rest_cash-punktzehn*0.1,2)
punktfuenf=rest_cash//0.05
rest_cash=round(rest_cash-punktfuenf*0.05,2)

print('Folgende Noten und Münzen würde man für den Betrag '+str(total_cash)+' Chf erhalten.')
print('--------------------------------------------')
print('Tausendernoten:\t\t'+str(int(tausender)))
print('Zweihunderternoten:\t'+str(int(zweihunderter)))
print('Hunderternoten:\t\t'+str(int(hunderter)))
print('Fünfzigernoten:\t\t'+str(int(fuenfziger)))
print('Zwanzigernoten:\t\t'+str(int(zwanziger)))
print('Zehnernoten:\t\t'+str(int(zehner)))
print('5-Fränkler:\t\t'+str(int(fuenfer)))
print('2-Fränkler:\t\t'+str(int(zweier)))
print('Einfränkler:\t\t'+str(int(einer)))
print('50 Rappen:\t\t'+str(int(halbe)))
print('20 Rappen:\t\t'+str(int(punktzwanzig)))
print('10 Rappen:\t\t'+str(int(punktzehn)))
print('5 Rappen:\t\t'+str(int(punktfuenf)))
print('--------------------------------------------')
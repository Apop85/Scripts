#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 01_kapitel_03_repetitionsaufgabe_2.py
# Project: Buch_Python3_Das_umfassende_Praxisbuch
# Created Date: Friday 22.02.2019, 19:59
# Author: Apop85
# -----
# Last Modified: Friday 22.02.2019, 20:08
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: This script will ask for the price of different products and services summarize
# them and divide them by the total amount of participants
###
print('-----------------------------------')
total_travel_cost=int(input('Kosten für den Reisebus: '))
total_hotel_cost=int(input('Kosten für das Hotel: '))
total_event_cost=int(input('Kosten für touristische Ausflüge: '))
total_participants=int(input('Totale Anzahl Teilnehmer: '))
print('-----------------------------------')
total_cost=total_event_cost+total_hotel_cost+total_travel_cost
print('Die Gesammtkosten betragen: '+str(total_cost)+' CHF')
print('Pro Person müssen '+str(round(total_cost/total_participants,2))+' CHF entrichtet werden.')


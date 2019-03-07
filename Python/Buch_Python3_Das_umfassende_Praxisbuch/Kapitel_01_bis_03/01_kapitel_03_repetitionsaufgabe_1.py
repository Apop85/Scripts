#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 01_kapitel_03_repetitionsaufgabe_1.py
# Project: Buch_Python3_Das_umfassende_Praxisbuch
# Created Date: Friday 22.02.2019, 16:12
# Author: Apop85
# -----
# Last Modified: Friday 22.02.2019, 19:58
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: This script will ask the user for informations and will implement them into 
# a short story.
###

print('Dieses Script schreibt eine Kurzgeschichte in welcher Sie vorkommen.')
user_name=input('Bitte Vornamen angeben: ')
month_of_birth=input('Bitte Geburtsmonat angeben: ')
hair_color=input('Bitte Haarfarbe angeben: ')
current_city=input('Bitte Wohnort angeben: ')

story_text='Die Verabredung mit dem Kommissar\n\nEs war ein grauer Morgen im',month_of_birth,'Die Sonne war grade erst aufgegangen\nund es war noch wenig Betrieb im Stadtzentrum von',current_city+'.\nHauptkommissar Hartmann stand vor dem Bistro und schaute auf die Uhr.\nWo bleibt',user_name,'nur?, dachte er. Ist etwas schiefgelaufen?\nVielleicht hatte',user_name+'s Freundin Wind von der Sache bekommen und seine\nPläne durchkreuzt.\nEine Person mit struwweligen',hair_color+'en Haaren näherte sich mit raschen\nSchritten.\nDer Kommissar atmete auf, als er den Menschen erkannte. Es war',user_name+'.\nJetzt konnte eigentlich nichts mehr schiefgehen...'
print(' '.join(story_text))
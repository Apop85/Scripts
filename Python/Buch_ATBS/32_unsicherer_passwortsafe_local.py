#!/usr/bin/python3
# Kopiert das Passwort des lokalen Windowsusers in die Zwischenablage

from pyperclip import copy, paste
import sys

PASS={'login' : 'User',
      'passw' : 'helloworld123',
      'beila' : '12345'}

user=((sys.argv[0]).split('/'))[2]
if user == PASS['login']:
    copy(PASS['passw'])
    print('Passwort für den User', PASS['login'], 'wurde kopiert')
else:
    print('Dieser Account ist nicht vorhanden')

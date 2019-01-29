#!/usr/bin/python3
# Kopiert das Passwort des über das Kommandozeilenargument übergebenen Users

from pyperclip import copy, paste
import sys
if len(sys.argv[1]) < 3:
    print('Eingabe Ungültig')
    print('Ausführen mit: "file.py username"')
    sys.exit()

PASS={'login' : 'User',
      'passw' : 'helloworld123',
      'beila' : '12345'}

user=sys.argv[1]
if user == PASS['login']:
    copy(PASS['passw'])
    print('Passwort für den User', PASS['login'], 'wurde kopiert')
else:
    print('Dieser Account ist nicht vorhanden')

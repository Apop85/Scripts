# Suche in String nach Telefonnummer und Mailadresse
import re
from pyperclip import copy, paste
from time import sleep

copy(0)
print('Folgenden Text kopieren:')
print('Tanjas Mailadresse lautet: tanja_lo.23@yah_oo.oof.com. Ihre Telefonnummer ist +49 41 123 1551.')
while paste() == '0':
    sleep(0.5)
    continue
string=paste()
    
# rege2=re.compile(r'([\+|\S]\d[\w\S]{,12})')
phoneregex=re.compile(r'''
    (\D\d{2?}|\D\d+)*    # Ländervorwahl
    [^ \d]*(\d{3}|\d{2})     # Vorwahl
    \D*(\d{3})           # Nummer erste 3 Stellen
    \D*(\d{2})           # Dritt und viertletzte Nummern
    \D*(\d{2})*          # Letzte 2 Nummern
    ''', re.VERBOSE)

# Mail: x Zeichen lang, mindestens 1x punkt, @=1, nur buchstaben und zahlen
mailregex=re.compile(r'\w[\d|\w|.]+\w@[\d\w.]+\.[a-z]{2,4}', re.DOTALL)

##print('Telefonnummer und Mail eingeben:')
stringc=''
find=phoneregex.findall(string)
findm=mailregex.findall(string)
if len(find) != 0:
    print('Telefonnummern gefunden:')
    for i in range(len(find)):
        print(str(i+1)+'.', ''.join(find[i]), end=' ')
        stringc+=str(''.join(find[i])+' ')
        copy(stringc.rstrip())
    print()
else:
    print('Keine Telefonnummer gefunden!')
if len(findm) != 0:
    print('Mailadressen gefunden:')
    for i in range(len(findm)):
        print(str(i+1)+'.', findm[i], end=' ')
        stringc+=str(findm[i]+' ')
        copy(stringc.rstrip())
else:
    print('Keine Mailadresse gefunden')

##copy(stringc.rstrip())

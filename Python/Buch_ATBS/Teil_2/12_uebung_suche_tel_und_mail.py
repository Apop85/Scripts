# Suche in String nach Telefonnummer und Mailadresse
import re
# rege2=re.compile(r'([\+|\S]\d[\w\S]{,12})')
phoneregex=re.compile(r'''
    (\D\d{2?}|\D\d+)           # Ländervorwahl
    \D*(\d{3}|\d{2})     # Vorwahl
    \D*(\d{3})           # Nummer erste 3 Stellen
    \D*(\d{2})           # Nummer
    \D*(\d{2})*         # Nummer
    ''', re.VERBOSE)

# Mail: x Zeichen lang, mindestens 1x punkt, @=1, nur buchstaben und zahlen
mailregex=re.compile(r'[\d|\w|.]+\w@[\d|\w|.]+')

print('Telefonnummer und Mail eingeben:')
string=input()
find=phoneregex.findall(string)
print('Telefonnummern gefunden:')
for i in range(len(find)):
    print(str(i+1)+'.', ''.join(find[i]), end=' ')
print()

findm=mailregex.findall(string)
print('Mailadressen gefunden:')
for i in range(len(findm)):
    print(str(i+1)+'.', findm[i], end=' ')

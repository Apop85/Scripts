# 02_google_maps_suche.py
# Dieses erste Projekt soll eine Hausanschrift per Kommandozeilenargument oder manueller Eingabe entgegennehmen und diese in Google-Maps aufrufen
# https://www.google.com/maps/place/STRASSE+STRASSENNUMMER,+PLZ+STADT
import re, webbrowser, sys

def regex_search(string):
    search_pattern=re.compile(r'([\Sa-zA-Z]+)[,| ]{1,2}(\d{1,3})[,| |\n]{1,3}(\d{4,6})[,| ]{1,2}([^\s\d ]+)')
    results=search_pattern.findall(string)
    return results
print(sys.argv)

if sys.argv == '':
    print('Bitte Anschrift angeben (Strasse Nummer, PLZ Ort):')
    adress=input()
    results=regex_search(adress)
    results=(list(results[0]))
    newurl='https://www.google.com/maps/place/'+results[0]+'+'+results[1]+',+'+results[2]+'+'+results[3]
    webbrowser.open(newurl)
else:
    adress=sys.argv
    adress=' '.join(adress[1:])
    results=regex_search(adress)
    results=(list(results[0]))
    newurl='https://www.google.com/maps/place/'+results[0]+'+'+results[1]+',+'+results[2]+'+'+results[3]
    webbrowser.open(newurl)
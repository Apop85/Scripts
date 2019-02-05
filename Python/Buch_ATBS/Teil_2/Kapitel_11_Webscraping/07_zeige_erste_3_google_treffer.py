# 07_zeige_erste_3_google_treffer.py
# Dieses Script soll die Aufgabe erfüllen mit BeautifulSoup die ersten 3 Links einer Googlesuche
# in einem neuen Browser-Tab aufzurufen.
# https://www.google.com/search?q=TEST+TEST+TEST

import requests, bs4, sys, logging, webbrowser

logging.basicConfig(level=logging.DEBUG, format='%(asctime)s - %(levelname)s ==> %(message)s')

if len(sys.argv) == 1:
    print('Suchbegriff eingeben')
    suchbegriff=input()
    suchbegriff=suchbegriff.split(' ')
else:
    suchbegriff='+'.join(sys.argv[1:])
url_name='https://www.google.com/search?q='+'+'.join(suchbegriff)
print(url_name)
url_content=requests.get(url_name)
url_content.raise_for_status()
bs4_objekt=bs4.BeautifulSoup(url_content.text, features="html.parser")
print(type(bs4_objekt))
a_classes=bs4_objekt.select('.r a') # Suche a-Elemente mit der Klasse r
for i in range(3):
    url_in_a_classes=a_classes[i].get('href') # hole href Element aus der c Klasse
    webbrowser.open('https://www.google.ch/'+url_in_a_classes)

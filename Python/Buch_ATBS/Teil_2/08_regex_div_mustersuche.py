# Uterschiedliche Prüfmuster für Regex
import re
from time import sleep
delay=0.3

def exreg():
    for i in range(len(string)-withc+1):
        chunk=string[i:i+withc]
        ergebnis=suchnach.search(chunk)

        if ergebnis != None:
            sleep(delay)
            print(ergebnis.group(), end=' ')
            
string=('Batman, Batwoman, Batwowoman.')
print('Der Teststring lautet:')
print(string)
input()

# ('Bat(wo)?man') Dieser Ausdruck sucht nach Batman. Zwischen Bat und man kann jedoch auch ein wo sein.
print('Suche nach (Bat(wo)?man). Heisst das "wo" in Batwoman \nkann vorkommen, muss jedoch nicht. \nAuch Batman ist in diesem Fall Gültig')
print('Suchergebnis in:', string)
suchmuster=(r'Bat(wo)?man')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.search(string)
if ergebnis != False:
    print(ergebnis.group())
print()
input()

# ('Batwo(ma)*n') Sucht nach dem Ausdruck Batwoman wobei das ma mehrmals vorkommen kann.
print('Suche nach (Bat(wo)*man). Heisst das "wo" in Batwoman \nkann mehrmals vorkommen, muss jedoch nicht')
print('Suchergebnis in:', string)
suchmuster=(r'Bat(wo)*man')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.search(string)
if ergebnis != False:
    print(ergebnis.group())
print()
input()

# ('Batwo(ma)+n') ist ein + angegeben heisst dass das Element muss mindestens 1x vorkommen
print('Suche nach (Bat(wo)+man). Heisst dass "wo" in Batwoman \nmindestens einmal vorkommen muss')
print('Suchergebnis in:', string)
suchmuster=(r'Bat(wo)+man')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.search(string)
if ergebnis != False:
    print(ergebnis.group())
print()
input()

# ('(Batwo(ma){2})+n') ist ein + angegeben heisst dass das Element muss mindestens 2x vorkommen
print('Suche nach ((Bat(wo){2})+man). Heisst das "wo" in \nBatwoman muss mindestens 2x vorkommen')
print('Suchergebnis in:', string)
suchmuster=(r'(Bat(wo){2})+man')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.search(string)
if ergebnis != False:
    print(ergebnis.group())
print()
input()

# findall() gibt alle übereinstimmenden Werte eines Strings aus.
print('Alle bisherigen suchen wurden mit search() ausgeführt.\nDadurch wird nur immer die erste Übereinstimmung\n ausgegeben und der Rest wird ignoriert.\n Mit der funkntion findall() werden alle \nÜbereinstimmungen in einem String als Liste ausgelesen')
print('Suchergebnis in:', string)
suchmuster=(r'(Bat)(wo)*(man)')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.findall(string)
if ergebnis != False:
    print((''.join(ergebnis[0])),(''.join(ergebnis[1])), (''.join(ergebnis[2])))
print()
input()

# Man kann auch ein Muster überprüfen von welchem man den genauen Aufbau nicht kennt.
print('Mit ((\\w+)\\W+) wird nach einem String gesucht der \naus ainer unbekannten Anzahl Buchstaben besteht \nund mit einem Satzzeichen auffhört')
print('Suchergebnis in:', string)
suchmuster=(r'(\w+)\W+')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.findall(string)
if ergebnis != False:
    print(' '.join(ergebnis))
print()
input()

# Regex unterscheidet gierige von nicht gierigen Anweisungen. Bei einer gierigen Anweisung wird stets der grösstmögliche String gesucht
string='HaHaHaHaHaHaHa'
print('Suche nach (Ha{3,5}?). Sucht einen String in \nwelchem "Ha" 5 bis drei mal vorkommt. Durch das \nFragezeichen wird 3 bevorzugt. Man sagt auch dass \ndiese Anweisung nicht gierig ist.')
print('Suchergebnis in:', string)
suchmuster=(r'(Ha){3,5}?')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.search(string)
if ergebnis != False:
    print(ergebnis.group())
print()
input()


print('Suche nach (Ha{3,5}). Sucht einen String in \nwelchem "Ha" 5 bis drei mal vorkommt. Durch das fehlen\ndes Fragezeichen wird 5 bevorzugt. Man sagt auch dass \ndiese Anweisung gierig ist.')
print('Suchergebnis in:', string)
suchmuster=(r'(Ha){3,5}')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.search(string)
if ergebnis != False:
    print(ergebnis.group())
print()
input()


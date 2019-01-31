# Uterschiedliche Prüfmuster für Regex
import re
delay=0.3

string=('Batman, Batwoman, Batwowoman.')

def foobar():
    print(''.center(50, '█'))

# ('Bat(wo)?man') Dieser Ausdruck sucht nach Batman. Zwischen Bat und man kann jedoch auch ein wo sein.
print('Suche nach (Bat(wo)?man). Heisst das "wo" in Batwoman \nkann vorkommen, muss jedoch nicht. \nAuch Batman ist in diesem Fall Gültig')
foobar()
print('Ergebnis in:', string)
foobar()
suchmuster=(r'Bat(wo)?man')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.search(string)
if ergebnis != False:
    print(ergebnis.group())
foobar()
input()

# ('Batwo(ma)*n') Sucht nach dem Ausdruck Batwoman wobei das ma mehrmals vorkommen kann.
print('Suche nach (Bat(wo)*man). Heisst das "wo" in Batwoman \nkann mehrmals vorkommen, muss jedoch nicht')
foobar()
print('Ergebnis in:', string)
foobar()
suchmuster=(r'Bat(wo)*man')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.search(string)
if ergebnis != False:
    print(ergebnis.group())
foobar()
input()

# ('Batwo(ma)+n') ist ein + angegeben heisst dass das Element muss mindestens 1x vorkommen
print('Suche nach (Bat(wo)+man). Heisst dass "wo" in Batwoman \nmindestens einmal vorkommen muss')
foobar()
print('Ergebnis in:', string)
foobar()
suchmuster=(r'Bat(wo)+man')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.search(string)
if ergebnis != False:
    print(ergebnis.group())
foobar()
input()

# ('(Batwo(ma){2})+n') ist ein + angegeben heisst dass das Element muss mindestens 2x vorkommen
print('Suche nach ((Bat(wo){2})+man). Heisst das "wo" in \nBatwoman muss mindestens 2x vorkommen')
foobar()
print('Ergebnis in:', string)
foobar()
suchmuster=(r'(Bat(wo){2})+man')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.search(string)
if ergebnis != False:
    print(ergebnis.group())
foobar()
input()

# findall() gibt alle übereinstimmenden Werte eines Strings aus.
print('Alle bisherigen suchen wurden mit search() ausgeführt.\nDadurch wird nur immer die erste Übereinstimmung\nausgegeben und der Rest wird ignoriert.\nMit der funkntion findall() werden alle \nÜbereinstimmungen in einem String als Liste ausgelesen')
foobar()
print('Ergebnis in:', string)
foobar()
suchmuster=(r'(Bat)(wo)*(man)')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.findall(string)
if ergebnis != False:
    print((''.join(ergebnis[0])),(''.join(ergebnis[1])), (''.join(ergebnis[2])))
foobar()
input()

# Man kann auch ein Muster überprüfen von welchem man den genauen Aufbau nicht kennt.
print('Mit ((\\w+)\\W+) wird nach einem String gesucht der \naus ainer unbekannten Anzahl Buchstaben besteht \nund mit einem Satzzeichen auffhört')
foobar()
print('Ergebnis in:', string)
foobar()
suchmuster=(r'(\w+)\W+')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.findall(string)
if ergebnis != False:
    print(' '.join(ergebnis))
foobar()
input()


# Man kann auch direkt nach einzelnen Buchstaben bzw Buchstabengruppen suchen
print('Man kann mit (r\'[Bmw]\') auch Definieren welche \nZeichen gesucht werden soll.')
foobar()
print('Ergebnis in:', string)
foobar()
suchmuster=(r'[Bmw]')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.findall(string)
if ergebnis != False:
    print(ergebnis)
foobar()
input()

# Man kann auch Zeichensequenzen angeben nach welchen gesucht werden soll.
print('Man kann mit (r\'[a-bA-D0-9]\') auch Definieren welche \nZeichensequenzen gesucht sind.\n Stellt man noch ein "^" vor die Zeichenkette\n Heisst dass diese Zeichen/Buchstaben nicht vorkommen \ndürfen es bedeutet also NOT')
foobar()
print('Ergebnis in:', string)
foobar()
suchmuster=(r'[a-bA-D0-9]')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.findall(string)
if ergebnis != False:
    print(ergebnis)
foobar()
input()

# Regex unterscheidet gierige von nicht gierigen Anweisungen. Bei einer gierigen Anweisung wird stets der grösstmögliche String gesucht
string='HaHaHaHaHaHaHa'
print('Suche nach (Ha{3,5}?). Sucht einen String in \nwelchem "Ha" 5 bis drei mal vorkommt. Durch das \nFragezeichen wird 3 bevorzugt. Man sagt auch dass \ndiese Anweisung nicht gierig ist.')
foobar()
print('Ergebnis in:', string)
foobar()
suchmuster=(r'(Ha){3,5}?')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.search(string)
if ergebnis != False:
    print(ergebnis.group())
foobar()
input()


print('Suche nach (Ha{3,5}). Sucht einen String in \nwelchem "Ha" 5 bis drei mal vorkommt. Durch das fehlen\ndes Fragezeichen wird 5 bevorzugt. Man sagt auch dass \ndiese Anweisung gierig ist.')
foobar()
print('Ergebnis in:', string)
foobar()
suchmuster=(r'(Ha){3,5}')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.search(string)
if ergebnis != False:
    print(ergebnis.group())
foobar()
input()

# Mit dem Joker kann man nach unbekannten Sequenzen suchen.
string='Hey, wie gehts dir? Danke! Es geht. Gut?'
print('Das (*) in (\'\w(.*)[?]) ist der Joker. Wenn man\nNicht weiss wonach man genau sucht kann\ndieser verwendet werden. Hier werden\nBeispielsweise Fragen gesucht. Der Joker\nist gierig und sucht immer die grösste mögliche\nübereinstimmung. Dirch die Angabe von (.*?) kann\nman jedoch nach einem möglichst kleinen Treffer suchen')
foobar()
print('Ergebnis in:', string)
foobar()
suchmuster=(r'\w((.*)[?])[^!.:;]')
suchnach=re.compile(suchmuster)
ergebnis=suchnach.search(string)
if ergebnis != False:
    print(ergebnis.group())
foobar()
input()

# Unterschiedliche Listenformen sowie Änderungen an Listeneinträgen

var=1
var2=2
var3=3

stringliste=['katze', 'hund', 'maus', 'elephant']
zahlenliste=[1, 1.123]
typenliste=[True, False, None]
mixedliste=[True, 'String', 42, 42.234]
variablenliste=[var, var2, var3]

listoflist=[stringliste, zahlenliste, typenliste, mixedliste, variablenliste]

print(listoflist[0][2]) # Vorwärtsausgabe - Zeige Item 2 aus Liste 0
print(listoflist[-1][-3]) # Rückwärtsausgabe - Zeige drittletzten Eintrag aus letzter Liste

del mixedliste[2] # Eintrag aus Liste entfernen


print(len(listoflist)) # Zähle anzahl Einträge in dieser Liste

# Listeneinträge Überschreiben

while True:
    print('Welche Liste soll geändert werden?')
    try:
        ausw=int(input())
        if ausw > len(listoflist)-1:
            print('So viele Einträge gibt es nicht')
            continue
        break
    except ValueError:
        print('Das ist keine Zahl!')
        continue

while True:
    print('Welcher Eintrag soll geändert werden?')
    try:
        ausw1=int(input())
        if ausw1 > len(listoflist[ausw])-1:
            print('So viele Einträge gibt es nicht')
            continue
        break
    except ValueError:
        print('Das ist keine Zahl!')
        continue

print('Der Eintrag der Liste', ausw, 'auf der Position', ausw1, '\nbeinhaltet die Information', listoflist[ausw][ausw1])
print('Neuer Eintrag:')
new=input()
listoflist[ausw][ausw1]=new
print('Der Eintrag der Liste', ausw, 'auf der Position', ausw1, '\nbeinhaltet nun die Information', listoflist[ausw][ausw1])


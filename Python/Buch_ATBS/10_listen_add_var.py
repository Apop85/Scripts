# Erstellen, auslesen und ändern von variablen Listen

print('Lass uns eine Party organisieren!\nWen sollen wir alles einladen?')
liste=[]
counter=[0, 0]
while True:
    print('Nenne einen Namen für deinen', str(counter[0]+1) + '. Gast oder beende die Liste mit Enter')
    name=str(input())
    if name != '':
        liste=liste + [name]
        counter[0]+=1
        continue
    else:
        break

print('Deine Party wird toll! \nEs werden', len(liste), 'Leute zu deiner Party eingeladen!')
print('Folgende Personen sind eingeladen:')
for namen in liste:
    print('---', namen)

print('Wir haben ein Problem! Leider ist eine Person zuviel :(')
while True:
    print('Wen sollen wir wieder ausladen?')
    name=str(input())
    if name not in liste:
        print('Eintrag nicht gefunden')
        continue
    else:
        liste.remove(name)
        break

print('Perfekt! Folgende', len(liste),'Personen sind nun eingeladen:')
for namen in liste:
    print('---', namen)

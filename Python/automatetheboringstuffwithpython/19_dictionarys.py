mycat={'size' : 'normal', 'color' : 'schwarzes', 'eyes' : 'grün'}
mycat2={'size' : 'normal', 'eyes' : 'grün', 'color' : 'schwarzes'}

print('Meine Katze ist', mycat['size'], 'gross hat', mycat['color'], 'Fell und ihre Augen sind', mycat['eyes'] + '.')

if mycat == mycat2:
    print('Dictionarys können gleich sein auch wenn die Sortierung sich unterscheidet')
    print('Wogegen Listen identisch sein müssen um als gleich erkannt zu werden.')


try:
    mycat['age']
except KeyError:
    print('Versucht man in einem Dictionary auf einen Eintrag zuzugreiffen\nwelcher nicht existiert gibt Python einen KeyError aus.')

birth={'friend1' : '25.Mar', 'friend2' : '12.Dec'}
def addbirth():
    print('Wann wurde', name, 'Geboren?')
    date=input()
    birth[name] = date

while True:
    print('Von wem möchtest du den Geburtstag wissen?')
    name=input()
    if name == '':
        print('Bitte Name angeben!')
        continue
    elif name not in birth:
        print('Name nicht gefunden!')
        print('möchtest du einen Eintrag für', name, 'erstellen? [y/N]')
        choose=input()
        if choose == 'y':
            addbirth()
        continue
    break


print(birth[name])

print('Die Geburtstage folgender Personen sind im Dictionary gespeichert:')
for i in birth.keys():
    print(i)

print('Folgende Geburtstage sind im Dictionary gespeichert:')
for i in birth.values():
    print(i)

print('Zusammengefügt sind das folgende Einträge:')
for i in birth.items():
    print(i)

print('Alternativ kann man das auch so ausdrücken:')
for i, k in birth.items():
    print('######', i, '###########', k, '######')

    

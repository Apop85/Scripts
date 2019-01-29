# Das Verwenden von Dictionarys innerhalb von Dictionarys

allguests={'Ronny' : {'Äpfel' : 3, 'Grillfleisch': 1},
           'Miriam' : {'Grillfleisch' : 3, 'Getränke' : 2},
           'Tony' : {'Äpfel' : 2, 'Getränke' : 4}}

stuff=['Äpfel', 'Getränke', 'Grillfleisch']

print('Ronny bringt folgendes mit:', allguests['Ronny'])
print('Miriam bringt folgendes mit:', allguests['Miriam'])
print('Tony bringt folgendes mit:', allguests['Tony'])
input('Enter zum Fortfahren')
print()

def cntstuff(item):
    total=0
    for names, items in allguests.items():
        total=total+items.get(item, 0)
    return total

print(stuff[0], 'werden', cntstuff(stuff[0]), 'mal mitgebracht.')
print(stuff[1], 'werden', cntstuff(stuff[1]), 'mal mitgebracht.')
print(stuff[2], 'werden', cntstuff(stuff[2]), 'mal mitgebracht.')
input('Enter zum Fortfahren')

print()
print('Mit der methode .items() wird der Komplette Inhalt der Listen aufgeführt:')
print(allguests.items())
print()
input('Enter zum Fortfahren')
      
print()
print('Mit der methode .keys() werden die in der Liste enthaltenen Items aufgeführt.')
print(allguests.keys())
print()
input('Enter zum Fortfahren')

print()
print('Mit der methode .values() werden die Werte der Items in der Liste ausgegeben.')
print(allguests.values())
print()
input('Enter zum Fortfahren')

print()
print('Mit der Funktion get() kann man Prüfen ob ein Item in einer Liste vorhanden ist. \nFalls nicht den angegebenen Wert ausgeben.')
total=0
for names, item in allguests.items():
    total+=item.get(stuff[0], 0)
print('Es gibt', total, 'Getränke')
total=0
for names, item in allguests.items():
    total+=item.get('Zeugs', 0)
print('Und es gibt', total, 'Zeugs')
print()
input('Enter zum Fortfahren')

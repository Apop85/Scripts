# Möglichkeiten zur Überprüfung eines Strings

print('Beispielanwendung für Alters- und Namensangabe mit \nisdecimal(), isalpha() und istitle()')
while True:
    print('\n\n\n')
    age=input('Wie alt bist du? ')
    if age.isdecimal():
        print('OK')
        break
    else:
        print('WHUUT? Das ist keine Zahl!')

while True:
    print('\n\n\n')
    name=input('Wie ist dein Name? ')
    if name.istitle() and name.isalpha():
        print('OK')
        break
    else:
        print('Das ist kein Name.')

print('\n\nHallo', name, 'Nächstes Jahr wirst du', int(age)+1, 'Jahre alt!')
print()
input()

print('Das nächste Beispiel ist startswith() und endswith().\nHier muss der String mit "Hallo" beginnen und mit "Welt" enden.')
string=input()
if string.startswith('Hallo') and string.endswith('Welt'):
    print('True')
else:
    print('False')
print()
input()





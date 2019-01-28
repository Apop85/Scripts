while True:
    print('\n\n\n')
    age=input('\t\t\t Wie alt bist du? ')
    if age.isdecimal():
        print('\t\t\t OK')
        break
    else:
        print('\t\t\t WHUUT? Das ist keine Zahl!')

while True:
    print('\n\n\n')
    name=input('\t\t\t Wie ist dein Name? ')
    if name.istitle() and name.isalpha():
        print('\t\t\t OK')
        break
    else:
        print('\t\t\t Das ist kein Name.')

print('\n\n\t\t\tHallo', name, '\n\t\t\tNächstes Jahr wirst du', int(age)+1, 'Jahre alt!')

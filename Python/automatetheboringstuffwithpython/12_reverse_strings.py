# Inhalte von Strings umdrehen oder alphabetisch sortieren

print('Gib deinen Vollständigen Namen ein')
name=input()
liste=list(name)

print('Rückwärts:')
for i in range(len(liste)):
    print(liste[len(liste)-1-i], end='')
else:
    print(end='\n')

print('Alphabetisch sortiert:')
liste.sort(key=str.lower)
for buchstabe in liste:
    print(buchstabe, end='')
else:
    print(end='\n')

print('Alphabetisch Rückwärts:')
liste.sort(reverse=True, key=str.lower)
for buchstabe in liste:
    print(buchstabe, end='')
else:
    print(end='\n')

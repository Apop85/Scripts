liste=['eins', 'zwei', 'drei', 'vier', 'fünf', 'sechs', 'sieben', 'acht', 'neun', 'zehn']
print('Nach welchem Eintrag soll gesucht werden?')
zahl = str(input())

for wort in liste:
    if wort == zahl:
        print(wort, 'GLEICH', zahl)
    else:
        print(wort, 'UNGLEICH', zahl)



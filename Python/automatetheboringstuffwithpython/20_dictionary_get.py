print('Die get() Anweisung auf ein Dictionary prüft ob ein\nentsprechender Eintrag vorhanden ist, \nfalls nicht wird ein zuvor angegebener Standardwert ausgegeben.')

dicti={'katze' : 13, 'hund' : 15, 'maus' : 5}

print('Nach welchem Tier soll gesucht werden?')
tier=input()
wert=25

print(dicti.get(tier, wert))
print('')

print('Man kann auch direkt Prüfen ob ein Eintrag vorhanden ist\nund falls nicht, diesen direkt erstellen.')
print('\nFalls ein Eintrag schon vorhanden ist wird dieser NICHT überschrieben. \nStatdessen wird der Inhalt de gesuchten Eintrags ausgegeben.')
dicti2={'katze' : 'rot', 'hund' : 'schwarz', 'maus' : 'grau'}
print('Nenne ein Gegenstand/Tier')
tier=input()
print('Welche Farbe hat', tier + '.')
color=input()
dicti2.setdefault(tier, color)
print(dicti2)

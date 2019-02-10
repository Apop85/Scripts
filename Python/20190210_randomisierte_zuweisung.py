# 20190210_randomisierte_zuweisung.py
# Randomisierte zuweisung von aufgaben

from random import randint as rng

# Alle Teilnehmer welchen Aufgaben zugewiesen werden sollen.
teilnehmer=['Claudia', 'Sarah', 'Miri', 'Lexi', 'Sandra']
# Schonliste für Mitglieder die weniger Arbeit verrichten können.
schonliste=['Lexi']
# Zu verteilende Aufgaben:
aufgaben=['Kriterienliste Interventionsstudie',
        'Messinstrument',
        'Kontrolle',
        'Zielsetzung',
        'Stichprobe',
        'Intervention',
        'Kontrolle',
        'Datenanalyse']

# Randomisierte Zuweisung der Aufgaben:
aufgaben_verteilung={}
for name in teilnehmer:
    aufgaben_verteilung.setdefault(name, [])

for aufgabe in aufgaben:
    zuweisung=True
    while zuweisung:
        random=rng(0, len(teilnehmer)-1)
        if len(aufgaben_verteilung[teilnehmer[random]]) < round(len(aufgaben)/len(teilnehmer)):
            if teilnehmer[random] in schonliste and len(aufgaben_verteilung[teilnehmer[random]]) == round(len(aufgaben)/len(teilnehmer)/2):
                continue
            aufgaben_verteilung[teilnehmer[random]]+=[aufgabe]
            zuweisung=False

# Ausgabe der Aufgaben:
for name in aufgaben_verteilung:
    print(name+':')
    for aufgabe in aufgaben_verteilung[name]:
        print('  ╚══ '+aufgabe)


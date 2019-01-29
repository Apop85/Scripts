# Kleines Zufallsspiel bei welchem man eine Frage stellen kann und evt die richtige Antwort kriegt

from random import randint as rng
print('Kennst du 8-Ball? Die Kugel die dir die Zukunft voraussagen soll? \nNein? Dann versuchs doch gleich mal')
print('Stelle der Magischen Kugel nun deine Frage')
frage=str(input())

antworten=['Ich glaube nicht',
           'Wohl kaum',
           'Möglich wäre es',
           'Mach dir nich zu viel Hoffnung',
           'In ferner Zukunft... wer weiss...',
           'Wir werden sehen... aber möglich ist es',
           'Diese Frage ist unrealistisch',
           'Keinen blassen schimmer...']

print('Die Antwort auf deine Frage lautet:')
print(antworten[rng(0,7)])

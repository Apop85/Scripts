# Aufzählung aus Zwischenspeicher
from pyperclip import copy, paste
size, ausgl=50,0
liste=[]

print('Warte auf Zwischenablage')
copy('0')
while paste() == '0':
    continue
else:
    print(''.center(size, '▓'))
    print(' Zwischenablage gefunden '.center(size, '▓'))
    print(''.center(size, '▓'))



string=(paste()).split('\n')
for eintrag in string:
    ausgabe=((' * '+eintrag.strip(' ')+' '))
    if ausgl < len(ausgabe):
        ausgl=len(ausgabe)
    liste+=[ausgabe]
    # print(ausgabe.center(size, '▓'))
    print('▓'.ljust((ausgl//2-1), '▓') + ausgabe.ljust(ausgl) + ''.rjust((ausgl//2), '▓'))

print(''.center(size, '▓'))

copy('\n'.join(liste))

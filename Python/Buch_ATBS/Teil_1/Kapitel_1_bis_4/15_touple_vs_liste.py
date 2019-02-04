# Unterschied von Listen und Tuples

KONSTANTE=('Shadow', 'grün', 'schwarz')
variable=['eintrag1', 'eintrag2', 'eintrag3']
print('Meine Katze heisst', KONSTANTE[0], 'seine Augen sind', KONSTANTE[1], 'und sein Fell ist', KONSTANTE[2] + '.')

try:
    KONSTANTE.remove('grün')
except AttributeError:
    print('Es ist nicht möglich Einträge in einem Tuple zu ändern')
    print(KONSTANTE)

print('Bei Listen ist das kein Problem')
print(variable)
variable.remove('eintrag1')
variable.append('hallo')
print(variable)

print('Man kann jedoch Tuples auch in Listen umwandeln und umgekehrt.')
KONSTANTE=list(KONSTANTE)
print(KONSTANTE)
KONSTANTE=tuple(KONSTANTE)
print(KONSTANTE)

print('Man kann Listen und Tuples auch Kopieren')
from copy import copy as copy
from copy import deepcopy as dcopy
variable2=copy(variable)
print(variable2)

print('Man Kann auch Listen die Listen beinhalten kopieren.')
listoflist=[variable, variable2]
lol2=dcopy(listoflist)
print(lol2)

print('Und auch Tuples die Tuples enthalten')
dtup=(KONSTANTE, KONSTANTE)
dtup2=dcopy(dtup)
print(dtup)

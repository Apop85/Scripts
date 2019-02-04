# Dieses Script prüft ob eine eingegebene Zahl eine Primzahl ist oder nicht.
from math import sqrt

def getnum():
    while True:
        print('Zahl eingeben')
        num=input()
        if not num.isdecimal():
            print('Eingabe war keine Zahl!\n')
            continue
        else:
            num=int(num)
            return num




def checknum():
    num=getnum()
    maxtry=int(round(sqrt(num)))
    for i in range(1, maxtry):
        if num % (i+1) != 0.0 and i+1 == maxtry:
            print(num, 'ist eine Primzahl')
            break
        elif num % (i+1) != 0.0:
            continue
        else:
            print(num, 'ist durch', str(i+1), 'teilbar!')
            break
try:
    while True:
        checknum()
except KeyboardInterrupt:
    print('Bye')

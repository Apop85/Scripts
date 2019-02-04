# Dieses Script soll alle Primzahlen ausgeben bis es unterbrochen wird.
from math import sqrt
from copy import copy
maxl=50

def choose():
    while True:
        print(''.center(maxl, '█'))
        print(' Wähle Ausgabe: '.center(maxl, '█'))
        print(''.center(maxl, '█'))
        print('  Absolut: 1  '.center(maxl, '█'))
        print(' Relativ: 2 '.center(maxl, '█'))
        print(' Prozentual: 3 '.center(maxl, '█'))
        print(' Proz (graph): 4 '.center(maxl, '█'))
        print(''.center(maxl, '█'))
        chos=input()
        if chos == '1':
            iterate_abs()
        elif chos == '2':
            iterate_rel()
        elif chos == '3':
            iterate_per()
        elif chos == '4':
            iterate_perg()
        else:
            print('Whut?')
            continue

        

def iterate_abs():
    try:
        num=0.0
        while True:
            num+=1
            value=checknum(num)
            if value != None:
                print(str(int(value)), end=' ')
    except KeyboardInterrupt:
        print('\nAusgabe Abgebrochen')

def iterate_rel():
    try:
        num, count=0.0, 0
        while True:
            num+=1
            value=checknum(num)
            if value == None:
                count+=1
            else:
                print(count, end=' ')
                count=0
    except KeyboardInterrupt:
        print('\nAusgabe Abgebrochen')
                
def iterate_per():
    try:
        num, count, last = 0.0, 0, 0
        while True:
            num+=1
            value=checknum(num)
            if value == None:
                count+=1
                continue
            elif last == 0 and value != None:
                last=copy(count)
                continue
            elif value != None and last != 0:
                if count > last:
                    print(round((100/count*last),2), end=' ')
                else:
                    print(round((100/last*count),2), end=' ')
                last=copy(count)
                count=0
    except KeyboardInterrupt:
        print('\nAusgabe Abgebrochen')

def iterate_perg():
    try:
        num, count, last = 0.0, 0, 0
        while True:
            num+=1
            value=checknum(num)
            if value == None:
                count+=1
                continue
            elif last == 0 and value != None:
                last=copy(count)
                continue
            elif value != None and last != 0:
                if count > last:
                    coord=int(round(((100/count*last))/2))
                else:
                    coord=int(round(((100/last*count))/2))
                print('╣'.rjust(coord, '═'))

                last=copy(count)
                count=0
    except KeyboardInterrupt:
        print('\nAusgabe Abgebrochen')
        


def checknum(num):
    maxtry=int(round(sqrt(num)))
    for i in range(maxtry+1):
        if num % (i+2) == 0.0:
            break
        elif i == maxtry and num % (i+1) != 0.0:
            return int(num)
try:
    while True:
        choose()
except KeyboardInterrupt:
    print('\nBye')

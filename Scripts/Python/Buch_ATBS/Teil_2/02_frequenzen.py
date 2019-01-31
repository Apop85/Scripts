# Frequenzmuster ausgeben: Digital, Sinus, Dreieck und was sonst noch...
from time import sleep
from math import sin, pi
from random import randint as rng

maxl=50
sleeptimer=0.1
def choose():
    try:
        while True:
            print(''.center(maxl, '█'))
            print(' Wähle Frequenzmuster: '.center(maxl, '█'))
            print(''.center(maxl, '█'))
            print('    Binär  : 1  '.center(maxl, '█'))
            print('   Dreieck : 2  '.center(maxl, '█'))
            print('    Sinus  : 3  '.center(maxl, '█'))
            print(' Aktienkurs: 4  '.center(maxl, '█'))
            print(''.center(maxl, '█'))
            choose=input()
            if choose == '1':
                binary()
            elif choose == '2':
                zickzack()
            elif choose == '3':
                sinus()
            elif choose == '4':
                stock()
            else:
                continue
    except KeyboardInterrupt:
        print('Bye!')

def getfreq():
    while True:
        print('Frequenz 1-10:')
        freq=input()
        if freq.isdecimal() and int(freq) < 11:
            freq=int(freq)
            return freq
        else:
            print('Das ist keine Zahl zwischen 1 und 10')

def zickzack():
    try:
        zahl=1
        freq=getfreq()
        while True:
            for i in range(maxl//freq-1):
                print(('@'*freq).rjust((i+1)*freq, '▒'))
                sleep(sleeptimer)
            for i in range(maxl//freq-1):
                print(('@'*freq).rjust(maxl-i*freq, '▒'))
                sleep(sleeptimer)
    except KeyboardInterrupt:
        print('\nAusgabe abgebrochen')

def binary():
    freq=getfreq()
    try:
        while True:
            for i in range(freq):
                print('@'.ljust(maxl))
                sleep(sleeptimer)
            print('@'.center(maxl, '@'))
            sleep(0.2)
            for i in range(freq):
                print('@'.rjust(maxl, '▒'))
                sleep(0.15)
            print('@'.center(maxl, '@'))
            sleep(sleeptimer)
    except KeyboardInterrupt:
        print('\nAusgabe abgebrochen')

def sinus():
    freq=getfreq()
    freq=100000/int(freq)
    i=0
    try:
        while True:
            i+=1
            position=int((((sin(2*pi*500*i/freq))*(maxl//2)))+maxl//2+1)
            if position == 51:
                position = 50
            print(('@').rjust(position, '▒'))
            sleep(sleeptimer)
    except KeyboardInterrupt:
        print('\nAusgabe abgebrochen')

def stock():
    index=rng(1,maxl)
    try:
        freq=getfreq()
        while True:
            random=rng(1,20)
            if random == 15:
                modfreq=freq*2
            else:
                modfreq=freq
            index=index+rng((modfreq)*-1,modfreq)
            if index <= maxl and index >= 1:
                print(('@'+str(index)).rjust(index+2, '▒'))
                sleep(sleeptimer)
            elif index <= 0:
                index=1
                print(('@'+str(index)).rjust(index+2, '▒'))
                sleep(sleeptimer)
            elif index >= maxl:
                index=maxl
                print(('@'+str(index)).rjust(index+2, '▒'))
                sleep(sleeptimer)
            else:
                print('ERROR:', index)
                break
    except KeyboardInterrupt:
        print('\nAusgabe abgebrochen')

choose()

# Frequenzmuster ausgeben: Digital, Sinus, Dreieck und was sonst noch...
from time import sleep
from math import sin, pi
from random import randint as rng
global maxl
maxl=70
sleeptimer=0.1
def choose():
    try:
        while True:
            print(''.center(maxl, '█'))
            print(' Wähle Frequenzmuster: '.center(maxl, '█'))
            print(''.center(maxl, '█'))
            print('      Binär: 1  '.center(maxl, '█'))
            print('    Dreieck: 2  '.center(maxl, '█'))
            print('      Sinus: 3  '.center(maxl, '█'))
            print('RandomSinus: 4  '.center(maxl, '█'))
            print(' JumpingSin: 5  '.center(maxl, '█'))
            print(' Aktienkurs: 6  '.center(maxl, '█'))
            print(''.center(maxl, '█'))
            choose=input()
            if choose == '1':
                binary()
            elif choose == '2':
                zickzack()
            elif choose == '3':
                sinus()
            elif choose == '4':
                ransin()
            elif choose == '5':
                jmpsin()
            elif choose == '6':
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

def ransin():
    cnt, i=0, 0
    try:
        freq=100000/getfreq()
        while True:
            inverter=rng(0,1)
            target=rng(5,25)
            while cnt != target:
                if inverter == 0:
                    i+=1
                else:
                    i-=1
                cnt+=1
                position=int((((sin(2*pi*500*i/freq))*(maxl//2)))+maxl//2+1)
                if position == 51:
                    position = 50
                print(('@').rjust(position, '▒'))
                sleep(sleeptimer)
            cnt=0
    except KeyboardInterrupt:
        print('\nAusgabe abgebrochen')

def jmpsin():
    cnt, i, inverter=1, 10, 0
    maxll=maxl
    try:
        freq=100000/getfreq()
        freqs=freq
        while True:
            if inverter == 1:
                inverter = 0
                i+=1
            else:
                inverter = 1
                i-=1
            
            while True:
                if inverter == 0:
                    i+=1
                else:
                    i-=1
                position=int((((sin(2*pi*500*i/freqs))*(maxll//2)))+maxll//2+1)
                if position == maxll+1:
                    position = maxll
                elif position < maxll//10 or position == 3:
                    break
                elif position <= 2:
                    position=4
                    break
                elif maxll <= 8:
                    for z in range(120):
                        maxll=7
                        if inverter == 0:
                            i+=1
                        else:
                            if i == 0:
                                inverter=1
                            else:
                                i-=1
                        position=int((((sin(2*pi*500*i/freqs))*(maxll//2)))+maxll//2+1)
                        print(('@').rjust(position, '▒'))
                        sleep((sleeptimer-0.07))
                        if freqs > 8500:
                            freqs=int(freqs*0.99)
                    maxll=maxl
                    freqs=freq
                    i=10
                    inverter=0
                    break
                print(('@').rjust(position, '▒'))
                sleep((sleeptimer-0.07))

            maxll-=3
            freqs=int(freqs*0.9499)
            

            
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

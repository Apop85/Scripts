# Frequenzmuster ausgeben: Digital, Sinus, Dreieck und was sonst noch...
maxl=50
def choose():
    try:
        while True:
            print(''.center(maxl, '█'))
            print(' Wähle Frequenzmuster: '.center(maxl, '█'))
            print(''.center(maxl, '█'))
            print('  Binär: 1  '.center(maxl, '█'))
            print(' Dreieck: 2 '.center(maxl, '█'))
            print('  Sinus: 3  '.center(maxl, '█'))
            print(''.center(maxl, '█'))
            choose=input()
            if choose == '1':
                binary()
            elif choose == '2':
                zickzack()
            elif choose == '3':
                sinus()
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
            for i in range(maxl//freq-1):
                print(('@'*freq).rjust(maxl-i*freq, '▒'))
    except KeyboardInterrupt:
        print('\nAusgabe abgebrochen')

def binary():
    freq=getfreq()
    try:
        while True:
            for i in range(freq):
                print('@'.ljust(maxl))
            print('@'.center(maxl, '@'))
            for i in range(freq):
                print('@'.rjust(maxl, '▒'))
            print('@'.center(maxl, '@'))
    except KeyboardInterrupt:
        print('\nAusgabe abgebrochen')

def sinus():
    from math import sin, pi
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
    except KeyboardInterrupt:
        print('\nAusgabe abgebrochen')

choose()

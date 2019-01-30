# Generiere Schlüssel aus zufälligen Primzahlen
from random import randint as rng
from math import sqrt
values=[]
scrwith=48

while True:
    print(''.center(scrwith+4, '█'))
    print(' Versuche einen Key zu generieren welcher auf '.center(scrwith+4, '█'))
    print(' dieser Webseite nicht geknackt werden kann: '.center(scrwith+4, '█'))
    print(''.center(scrwith+4, '█'))
    print(' https://www.alpertron.com.ar/ (Factor) '.center(scrwith+4, '█'))
    print(''.center(scrwith+4, '█'))
    print(' Die Primzahlen werden zufällig '.center(scrwith+4, '█'))
    print(' zwischen [10^14] und [10^16] generiert '.center(scrwith+4, '█'))
    print(''.center(scrwith+4, '█'))
    print(' Wieviele Primzahlen sollen verwendet werden? '.center(scrwith+4, '█'))
    print(''.center(scrwith+4, '█'))
    amount=input(' '*22)
    if amount.isdecimal():
        amount=int(amount)
        print(' Primzahlen werden berechnet '.center(scrwith+4, '█'))
        break

def getprime(num):
    maxdiv=int(round(sqrt(value)))
    while True:
        num+=1
        for i in range(1, maxdiv):
            if num % (i+1) == 0:
                break
            elif i == maxdiv-1 and value % (i+1) != 0:
                return num

while True:
    rounds=0
    while rounds != amount:
        value=rng(10**14, 10**16)
        prime=getprime(value)
        values+=[prime]
        rounds+=1
        done=int(round(scrwith/amount*rounds))
        bar=('█'*done)+(' '*(scrwith-done))
        print(('['+bar+']' +str(round(100/amount*rounds))+'%'))

    
    if rounds == amount:
        total=1
        print('Verwendete Primzahlen:')
        for i in range(len(values)):
            total*=values[i]
            values[i]=str(values[i])
        print(' x '.join(values))
        print('\nSchlüssel:', total, end='\n')
        break
        

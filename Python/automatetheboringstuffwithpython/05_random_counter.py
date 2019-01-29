# Würfle x mal eine Zahl zwischen 0 und 9 und werte aus wie oft die einzelnen Zahlen gewürfelt wurden. 

import random
rngvar=[0]*10
while True:
    print('Wie oft soll gewürfelt werden?')
    try:
        total=int(input())
        break
    except ValueError:
        print('Das ist keine Zahl!')

faktor=100/total

for i in range(total):
    rng=random.randint(0, 9)
    rngvar[rng]+=1

for i in range(10):
    print(i, ':', str(round(rngvar[i]*faktor, 3)) + '%')

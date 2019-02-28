# Abbildung der Collatzfolge mit der Zahl x

actor1='Mr. Collatz:'
actor2='Mr. Miller:'

print(actor1, 'Sie werden es nicht glauben!')
print(actor2, 'Worum geht es?')
print(actor1, 'Ich habe eine Berechung gefunden die egal welche \nZahl man als Ursprung nimmt immer 1 ergibt!')
print(actor2, 'Was ist daran Besonders?')
print(actor1, 'Na, das sollte eigentlich nicht möglich sein!')
print(actor2, 'Können sie mir das mal vorführen?')
print(actor1, 'Na klar!')

def collatz(num):
    global counter
    counter=0
    while num != 1:
        print(num, end=' ')
        if num % 2 == 0:
            num=num//2
        else:
            num=3*num+1
        counter+=1
        print('\r'*(len(str(num))+5), end=' ')
    else:
        print('Heureka!', end='\n')
        return num
while True:
    while True:
        try:
            print(actor1, 'Nennen sie mir eine Zahl.')
            zahl = int(input ())
            break
        except ValueError:
            print(actor1, 'Eine Zahl habe ich gesagt!')
    zahl=collatz(zahl)
        
    print(actor1, 'Und schlussendlich das Resultat:', zahl, 'und das nach', counter, 'berechnungen.')
    print(actor2, 'Unglaublich!')

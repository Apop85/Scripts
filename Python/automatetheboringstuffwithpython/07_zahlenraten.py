# Kleines Spiel bei welchem eine Zahl zwischen 1 und x erraten werden soll.
from random import randint as rng

print('Lass uns ein Spiel spielen.\n\nIch denke mir eine Zahl zwischen 1 und X aus \nund du musst sie erraten.')
print('Welche Zahl sollen wir für X nehmen?')
while True:
    try:
        maxcnt = int(input())
        if maxcnt < 15:
            print('Etwas schwerer sollte es schon sein...')
        else:
            rounds=int((maxcnt/100)*33)
            break
    except ValueError:
        print('Das soll eine Zahl sein?')
        continue

print('Los geht\'s! Ich habe mir eine Zahl zwischen 1 und', maxcnt, 'ausgedacht, \nrate welche es ist. Du darfst,', rounds, 'mal raten.')
def game():
    goal=rng(1,maxcnt)
    for runde in range(rounds):
        try:
            if runde != 0:
                print('Du darfst noch', rounds-runde, 'mal raten.')
            global wahl
            wahl=int(input())
        except ValueError:
            print('Das ist keine gültige Nummer!')
            continue
        if wahl < (goal/2) or wahl > (goal*2):
            print('Ganz weit weg!')
        elif wahl < (goal-7) or wahl > (goal+7):
            print('Es wird Wärmer!')
        elif wahl <= (goal-2) or wahl >= (goal+2):
            print('Es ist Warm!')
        elif wahl == (goal-1) or wahl == (goal+1):
            print('HEISS!')
        elif wahl == goal:
            print('Du hast meine Zahl erraten! Es war', str(goal) + '. \nDafür hast du nur', runde+1, 'Runden gebraucht!')
            break
        else:
            print('Da ging was schief!')
            break
    try:
        if wahl != goal:
            print('Du hast es leider nicht geschafft. \nDie Zahl an welche ich gedacht habe ist', goal)
    except NameError:
        print('Offensichtlich willst du gar nicht spielen!')

game()

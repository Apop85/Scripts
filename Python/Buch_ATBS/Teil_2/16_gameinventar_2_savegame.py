# Abgewandelte Version aus dem 1. Teil des Buchs. Hier nun mit Savegame Funktion.
from random import randint as rng
import shelve
inventory={}
message=''

items=['Heiltrank', 'Schwert', 'Münzen', 'Manatrank', 'Bananenschale', 'Pilz']
maxrng=len(items)

def choice():
    global answ
    print(' _____  _   _ _____  _____ _____ _____ '.center(50))
    print('/  __ \| | | |  _  ||  _  /  ___|  ___|'.center(50))
    print('| /  \/| |_| | | | || | | \ `--.| |__  '.center(50))
    print('| |    |  _  | | | || | | |`--. \  __| '.center(50))
    print('| \__/\| | | \ \_/ /\ \_/ /\__/ / |___ '.center(50))
    print(' \____/\_| |_/\___/  \___/\____/\____/ '.center(50))
    print('')
    print('1. Items Sammeln'.center(50))
    print('2. Inventar anzeigen'.center(50))
    print('3. Savegame laden'.center(50))
    print('4. Beenden'.center(50))
    print('             ', message)
    answ=input('                    ')

def savegame():
    savegame=shelve.open('gamedata')
    savegame['inventory'] = inventory
    savegame.close()

def loadgame():
    global inventory
    savegame=shelve.open('gamedata')
    inventory=savegame['inventory']
    savegame.close()

def getitem():
    global message
    num=rng(1, maxrng)-1
    message=(items[num]+' erhalten.')
    if items[num] not in inventory:
        inventory.setdefault(items[num], 1)
    else:
        inventory[items[num]]+=1

def debug():
    savegame=shelve.open('gamedata')
    print(list(savegame.keys()))
    print(list(savegame.values()))
    print(savegame['inventory'])
    savegame.close()
    input()

def showinv():
    global message
    message=''
    total=0
    print(''.center(50, '█'))
    for i, k in inventory.items():
        print((' '+str(i)+' : '+str(k)+' ').center(50, '█'))
        total+=k
    print(('  Total: '+ str(total)+'  ').center(50, '█'))
    print(''.center(50, '█'))
    input()

while True:
    choice()
    try:
        answ=int(answ)
        if answ == 1:
            getitem()
            savegame()
        elif answ == 2:
            showinv()
            savegame()
        elif answ == 3:
            loadgame()
        else:
            break
    except ValueError:
        print('Da ging was schief! Spiel gespeichert!')
        debug()

from random import randint as rng
inventory={}
message=''

items=['Heiltrank', 'Schwert', 'Münzen', 'Manatrank', 'Bananenschale', 'Pilz']
maxrng=len(items)

def choice():
    global answ
    print(' _____  _   _ _____  _____ _____ _____ ')
    print('/  __ \| | | |  _  ||  _  /  ___|  ___|')
    print('| /  \/| |_| | | | || | | \ `--.| |__  ')
    print('| |    |  _  | | | || | | |`--. \  __| ')
    print('| \__/\| | | \ \_/ /\ \_/ /\__/ / |___ ')
    print(' \____/\_| |_/\___/  \___/\____/\____/')
    print('\n\n')
    print('            1. Items Sammeln')
    print('            2. Inventar anzeigen')
    print('            3. Beenden')
    print()
    print()
    print('             ', message)
    answ=int(input('                    '))

def getitem():
    global message
    num=rng(1, maxrng)-1
    message=(items[num]+' erhalten.')
    if items[num] not in inventory:
        inventory.setdefault(items[num], 1)
    else:
        inventory[items[num]]+=1

def showinv():
    global message
    message=''
    for i, k in inventory.items():
        print('              ', i, ':', k)
    input()

while True:
    choice()
    if answ == 1:
        getitem()
    elif answ ==2:
        showinv()
    else:
        break

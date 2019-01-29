# Schere-Stein-Papier gegen den Computer
from random import randint as rng

objekte=['✂', '◙', '▒']

print('   _____  _____ _    _ ______ _____  ______ ')
print('  / ____|/ ____| |  | |  ____|  __ \|  ____|')
print(' | (___ | |    | |__| | |__  | |__) | |__   ')
print('  \___ \| |    |  __  |  __| |  _  /|  __|  ')
print('  ____) | |____| |  | | |____| | \ \| |____ ')
print(' |_____/ \_____|_|__|_|______|_|  \_\______|')
print('      / ____|__   __|  ____|_   _| \ | |        ')
print('     | (___    | |  | |__    | | |  \| |        ')
print('      \___ \   | |  |  __|   | | | . ` |        ')
print('      ____) |  | |  | |____ _| |_| |\  |        ')
print('    _|_____/   |_|__|______|_____|_|_\_|_    ')
print('   |  __ \ /\   |  __ \_   _|  ____|  __ \    ')
print('   | |__) /  \  | |__) || | | |__  | |__) |   ')
print('   |  ___/ /\ \ |  ___/ | | |  __| |  _  /    ')
print('   | |  / ____ \| |    _| |_| |____| | \ \    ')
print('   |_| /_/    \_\_|   |_____|______|_|  \_\   ')

pcp, plp = 0, 0
while True:
    print('Zielpunktzahl angeben:')
    ziel=input()
    if not ziel.isdecimal():
        print('Das ist keine Zahl')
        continue
    ziel=int(ziel)
    break

# Führe Loop solange durch bis winner definiert ist.
while 'winner' not in locals():
    print('Wähle: Schere (1), Stein(2) oder Papier(3)?')
    choose=input()
    if not choose.isdecimal() or int(choose) > 3:
        print('Gib eine Zahl zwischen 1 und 3 ein.')
        continue
    else:
        choose=int(choose)
    print()
    pc=rng(0,2)
    print(('Spieler:['+objekte[choose-1]+']').ljust(15)+' VS '+('['+(objekte[pc]+']:Computer')).rjust(15))
    if objekte[choose-1] == objekte[pc]:
        print('Unentschieden')
    elif pc == 0 and choose == 3 or pc == 1 and choose == 1 or pc == 2 and choose == 2:
        print('PC Gewinnt diese Runde')
        pcp+=1
    else:
        print('Player gewinnt diese Runde')
        plp+=1
    if plp == ziel:
        winner='Player'
    elif pcp == ziel:
        winner='PC'
    else:
        print('Der Punktestand lautet:')
        print('Player:', str(plp)+' zu '+str(pcp), ':Computer')

print(''.center(50, '▓'))
print(' ENDERGEBNIS '.center(50, '▓'))
print(''.center(50, '▓'))
print(('▓▓▓ Player:').ljust(20) + (str(plp)+' zu '+str(pcp)).center(10) + ':Computer ▓▓▓'.rjust(20))
print(''.center(50, '▓'))
print(('   ' + winner + ' GEWINNT!    ').center(50, '▓'))
print(''.center(50, '▓'))



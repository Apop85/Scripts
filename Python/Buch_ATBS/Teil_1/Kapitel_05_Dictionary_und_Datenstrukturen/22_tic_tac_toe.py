# Klassisches Tic-Tac-Toe für zwei Spieler

grid={'f7' : ' ', 'f8' : ' ', 'f9' : ' ',
      'f4' : ' ', 'f5' : ' ', 'f6' : ' ',
      'f1' : ' ', 'f2' : ' ', 'f3' : ' '}

def board(board):
    print('')
    print(board['f7']+'|'+board['f8']+'|'+board['f9'])
    print('-+-+-')
    print(board['f4']+'|'+board['f5']+'|'+board['f6'])
    print('-+-+-')
    print(board['f1']+'|'+board['f2']+'|'+board['f3'])

def winner():
    winlist=[[1,2,3],[4,5,6],[7,8,9],[1,4,7],[2,5,8],[3,6,9],[1,5,9],[3,5,7]]
    for i in winlist:
        if grid['f'+str(i[0])] == grid['f'+str(i[1])] == grid['f'+str(i[2])] != ' ':
            return True
    else:
        return False

player='X'
board(grid)
i=0

while i != 9:
    print('\nPlayer', player + ': Wähle ein Feld von 1-9')
    wahl=input()
    if int(wahl) > 9:
        print('Ungültige Zahl')
        continue
    elif grid['f'+wahl] == ' ':
        grid['f'+wahl]=player
        i+=1
    else:
        print('Besetzt!')
        continue
    if not winner():
        if player == 'X':
            player = 'O'
        else:
            player = 'X'
        board(grid)
        continue
    else:        
        print('\nPLAYER', player, 'HAT GEWONNEN!')
        board(grid)
        print('\nPLAYER', player, 'HAT GEWONNEN!\n')
        break
else:
    print('\n\nUntentschieden!\n')

    

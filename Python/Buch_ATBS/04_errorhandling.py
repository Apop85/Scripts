# Abfangen von Fehlermeldungen um zu verhindern dass das Script abstürzt.

def divide(numb):        # Division ohne Errorhandling
    return 42 / numb

def divide_er(numb):     # Division mit Errorhandling
    try:
        return 42 / numb
    except ZeroDivisionError:
        print('Division durch NULL ist nicht möglich!')

print(divide(2))
print(divide(12))
print(divide_er(0))
print(divide(15))



while True:
    print('Eine Nummer eingeben')
    try:
        inp=int(input())
        break
    except ValueError:
        print('Dir ist schon bewusst was eine Nummer ist?')

if int(inp) == 0:
    print('Du kannst auch höher gehen')
elif int(inp) < 10:
    print('Bravo')
else:
    print('Übertreibs mal nicht')


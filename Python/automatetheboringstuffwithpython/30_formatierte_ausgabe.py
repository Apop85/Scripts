dic={'Käse' : 5, 'Brot' : 3, 'Wein' : 2,
     'Eier' : 6, 'Nuss' : 12, 'Tee' : 14,
     'Müsli' : 1}

print(('Inventar'.center(16, '#')).center(60))

for namen, anzahl in dic.items():
    print((namen.ljust(13, '.') + str(anzahl).rjust(3, '.')).center(60))

print(('#'.center(16, '#')).center(60))

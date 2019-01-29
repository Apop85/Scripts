# Warten bis etwas neues in die Zwischenablage kopiert wurde und dann Zwischenablage ausgeben

from pyperclip import copy, paste
print('Warte auf CTRL+C')
if paste() != '0':
    copy(0)
while True:
    if paste() != '0':
        print(paste())
        break

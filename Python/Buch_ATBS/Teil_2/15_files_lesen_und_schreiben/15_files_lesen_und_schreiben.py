# Hier geht es darum Files zu lesen und zu schreiben

import os, re
maxl=53
dv=25
def output(string):
    delta=len(string)%maxl
    if delta != 0:
        string+=' '*(maxl-delta)
    pattern=re.compile(r'[\S].{'+str(maxl-dv)+r','+str(maxl)+r'}[ .,]')
    output=pattern.findall(string)
    for i in range(len(output)):
        print(output[i])

if not os.path.exists('readme.txt'):
    print('readme.txt nicht gefunden')
    os.exit()

print(''.center(maxl, '█'))
string='Man kann files direkt ganz auslesen wie hier mit read(): \n'
output(string)
content=open('readme.txt')
inhalt=content.read()
print(inhalt)
content.close()
print(''.center(maxl, '█'))
input()

print(''.center(maxl, '█'))
string='Oder man liest sie Zeilenweise aus so wie hier mit readlines(): \n'
output(string)
content=open('readme.txt')
inhalt=content.readlines()
output(str((inhalt)))
content.close()
print(''.center(maxl, '█'))
input()

print(''.center(maxl, '█'))
string='Um in Files zu schreiben muss man sie mittels open(FILE, \'w\') (w=write) öffnen. In diesem Modus wird der Komplette Inhalt des Files überschrieben! Wenn das File noch nicht vorhanden ist wird es mit dieser Funktion direkt erstellt.'
output(string)
content=open('readme.txt', 'w')
content.write("When, in disgrace with fortune and men's eyes,\nI all alone beweep my outcast state,\nAnd trouble deaf heaven with my bootless cries,\nAnd Took upon myself and curse my fate.\n")
content.close()
content=open('readme.txt')
inhalt=content.read()
output(str((inhalt)))
content.close()
print(''.center(maxl, '█'))
input()

print(''.center(maxl, '█'))
string='Anstatt ein File zu übeschreiben gibt es auch die Möglichket text am Ende des Files anzuhängen mittels read(FILE, \'a\')'
output(string)
content=open('readme.txt', 'a')
content.write("\nHello World!!")
content.close()
content=open('readme.txt')
inhalt=content.read()
output(str((inhalt)))
content.close()
print(''.center(maxl, '█'))
input()

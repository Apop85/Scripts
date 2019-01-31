# Hier geht es darum Files zu lesen und zu schreiben

import os, re
maxl=50
dv=12
def output(string):
    delta=len(string)%maxl
    if delta != 0:
        string+=' '*(maxl-delta)
    pattern=re.compile(r'[\S].{'+str(maxl-dv)+r','+str(maxl)+r'}[ ]')
    output=pattern.findall(string)
    for i in range(len(output)):
        print(output[i])

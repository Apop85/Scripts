# Zähle wie oft die einzelnen Buchstaben in einem String vorkommen. 

print('Gebe einen beliebigen Satz oder eine beliebige Zeichenfolge ein')
string=input()

dict1={}

for i in string:
        dict1.setdefault(i, 0)
        dict1[i]+=1

while True:
        print('Sortierte (0) oder normale (1) ausgabe?')
        inp=int(input())
        if inp == 0:
                from pprint import pprint as pprint
                pprint(dict1)
                break
        elif inp ==1:
                for i, k in dict1.items():
                    print('[\'' + str(i) + '\'] = [\'' + str(k) + '\']')
                break
        else:
                print('1 oder 0, so schwer ist das nicht!')
                continue

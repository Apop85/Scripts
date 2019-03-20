﻿# Kokosnuss-Problem
# Beschreibung: 3 Piraten finden einen Haufen Kokosnüsse. In der Nacht steht einer nach dem anderen auf und 
# nimmt ein Drittel der Nüsse weg und spendiert einem Affen eine Nuss. Wieviele Nüsse wurden mindestens gesammelt?

for nuts in range(1,10**6,3):
	split_1=((nuts-1)/3*2)
	split_2=((split_1-1)/3*2)
	split_3=((split_2-1)/3*2)
	if (nuts%3,split_1%3,split_2%3,split_3%3) == (1,1,1,1):
		print('Die Piraten haben mindestens '+str(nuts)+' Nüsse gesammelt.') 




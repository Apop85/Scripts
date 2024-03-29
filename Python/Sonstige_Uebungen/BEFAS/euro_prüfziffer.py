# Prüfziffer auf Euro-Banknoten
# Author: Apop85
# Test number: N15000723228

from random import randint as rng

def check_num(check_this):
	check_this=list(check_this)
	check_this[0]=str(ord(check_this[0].upper())-64)
	check_this=''.join(check_this)
	cross_sum=0
	for i in range(0,len(check_this)-1):
		cross_sum+=int(check_this[i])
	if 8-cross_sum%9 == int(check_this[-1]):
		return True
	elif (8-cross_sum%9,check_this[-1]) == (0,9):
		return True
	else:
		return False

def gen_num():
	country_codes=["D","E","H","J","M","N","P","R","S","T","U","V","W","X","Y","Z"]
	random_serial_number=rng(10**9,10**10-1)
	country_code=country_codes[rng(0,len(country_codes)-1)]
	serial_number=str(ord(country_code)-64)+str(random_serial_number)
	cross_sum=0
	for i in range(0,len(serial_number)):
		cross_sum+=int(serial_number[i])
	if 8-cross_sum%9 == 0:
		check_number=9
	else:
		check_number=8-cross_sum%9
	return country_code+str(random_serial_number)+str(check_number)

def output(check):
	if check:
		print('Prüfnummer ist gültig'.center(50))
	else:
		print('Prüfnummer ist ungültig'.center(50))

while True:
	try:
		options=["1. Ziffern prüfen", "2. Prüfziffern generieren", "3. Beenden"]
		for item in options:
			print(item.center(50))
		choosen=int(input("Auswahl tätigen:".rjust(34)))
		if choosen == 1:
			check_this=input('Prüfnummer eingeben: '.rjust(34))
			check=check_num(check_this)
			output(check)
		elif choosen == 2:
			print(gen_num().center(50))
		elif choosen == 3:
			break
		else:
			raise Exception('Invalid option')
	except:
		print("Auswahl ungültig")

# 35_assertion_plausiblitaetspruefung.py
# Mit der Methode assertion kann man im Script hinterlegen welche Werte man für die Variablen 
# an dieser Stelle vermutet. Erfüllt sich diese Vermutung nicht wird ein AssertionError ausgelöst
from random import randint as rng 
from time import sleep


for i in range(10):
    try:
        sleep(0.2)
        dice=rng(0,11)
        assert dice <= 10 and dice >=1, 'Hoppla. Da ging was schief.'
        print(dice)
    except Exception as error_message:
        print(error_message)
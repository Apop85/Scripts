import os
from pyperclip import copy, paste
from time import sleep
os.chdir(os.path.dirname(__file__)+r'\\readme')
pfad=os.getcwd()
counter=len(os.listdir(pfad))

def wait_4_it():
	global counter
	counter+=1
	copy(0)
	while paste() == '0':
		sleep(0.5)
	create_file(counter)

def create_file(nummer):
	file=open('file'+str(nummer)+'.txt', 'w')
	file.write(paste())
	file.close()
	print('File erstellt: file'+str(nummer)+'.txt')

while True:
	wait_4_it()
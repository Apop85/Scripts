# Dies ist die 2. Übungsaufgabe in welcher es darum geht ein Script zu erstellen welches mehrere vorgegebene Strings nacheinander in die Zwischenablage kopiert.

# Die Endung pyw heisst dass Python beim ausführen kein Shellfenster anzeigt.

# Dieses File solle mit der entsprechenden Bat-Datei gestartet werden mit dem Schlüsselname als Anhang 
# z.b. python3 filename.pyw save schlüssel    --> Speichert Inhalt der Zwischenablage unter dem Schlüssel
# und python3 filename.pyw keys				  --> Speichert die Keys zu dem vorhergehenden Schlüssel
# oder python3 filename.pyw load              --> Lädt die Schlüsselwörter in die Zwischenablage

import shelve, sys, os
from pyperclip import copy, paste
keys={}

arg=str(sys.argv)

def save_it(name):
	file=shelve.open('mcp')
	file[name]=keys
	file.close()	

def load_it():
	global keys
	if os.path.exists('mcp.dat'):
		file=shelve.open('mcp')
		keys=file['keys']
		file.close()

def delete_it():
	if os.path.exists('mcp.dat'):
		os.remove('mcp.dat')
		os.remove('mcp.bak')
		os.remove('mcp.dir')

def check_argv():
	load_it()
	global keys
	if len(sys.argv) == 3 and sys.argv[1].lower() == 'save':
		# Speichere keys
		keys.setdefault(sys.argv[2], paste())
		save_it('keys')
	elif len(sys.argv) == 3 and sys.argv[1].lower() == 'load':
		# Kopiere die values der keys eins nach dem anderen.
		if sys.argv[2] in keys.keys():
			copy(keys.get(sys.argv[2], ''))
		else:
			print('Kein Eintrag mit dem Namen', sys.argv[2], 'vorhanden!\nVorhandene Einträge:\n')
			print(' '.join(list(keys.keys())))
	elif len(sys.argv) == 2 and sys.argv[1].lower() == 'del':
		keys={}
		delete_it()
	else:
		print('Argument Ungültig! Der Syntax lautet:\nsave <schlüsselwort>, load <schlüsselwort> und del')

check_argv()
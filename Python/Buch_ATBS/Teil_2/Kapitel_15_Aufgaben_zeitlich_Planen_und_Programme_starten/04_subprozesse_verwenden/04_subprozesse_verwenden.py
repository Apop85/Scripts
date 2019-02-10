# 04_subprozesse_verwenden.py
# In diesem Beispiel geht es darum andere Prozesse mittels Python aufzurufen.
# Seien dies eigenständige Programme oder Scripts.
# Dazu wird das Modul subprocess benötigt

import os, subprocess, re, sys
os.chdir(os.path.dirname(__file__))
for name in sys.path:
    if 'Python' in name[-10:]:
	    python_path=name+'\\python.exe'

target_python_file='.\\subprozess.py'

max_text_length=70
max_text_delta=24

def output(title, string):
    print('╔'+''.center(max_text_length+8, '═')+'╗')
    print('║ '+title.center(max_text_length+7).upper()+'║')
    print('╠'+''.center(max_text_length+8, '═')+'╣')
    string=string+' '*max_text_length
    search_pattern=re.compile(r'\w+.{'+str(max_text_length-max_text_delta-7)+r','+str(max_text_length-7)+r'}[ |.|,|\n|>|\W]', re.DOTALL)
    results=search_pattern.findall(string)
    for line in results:
        print('║ '+line.strip()+'║'.rjust(max_text_length+8-len(line.strip())))
    print('╚'+''.center(max_text_length+8, '═')+'╝')
    input()

output('Modul Subprocess', 'Das Modul subprocess ermöglicht es weitere Programme oder Scripts innerhalb des Codes aufzurufen')

output('subprocess.Popen()', 'Die Funktion subprocess.Popen() öffnet die angegebene Datei als neuen Subprozess. In diesem Beispiel den Windows-Rechner Calc.')
subprocess.Popen('C:\\Windows\\System32\\calc.exe')

output('.poll()', 'Mit subprocess.poll() kann man abfragen ob das aufgerufene Programm oder Script noch läuft. So lange der Subprozess läuft gibt poll() die Antwort None zurück. Sobald dieser abgschlossen ist wird ein Integerwert mit dem Exitcode ausgegeben.')

output('.wait()', 'Mit subprocess.wait() kann ein Script unterbrochen werden so lange der Subprozess noch am laufen ist. In diesem Beispiel kommt die nächste Infobox erst wenn die Windows-Kommandozeile wieder geschlossen wurde.')
process=subprocess.Popen('C:\\Windows\\System32\\cmd.exe')
process.wait()

output('Mehrere Subprozesse', 'Man kann auch mehrere Subprozesse starten indem man .Popen() eine Liste mit den auszuführenden Prozessen übergibt.')

output('Kommandozeilenargumente übergeben', 'Auch um Kommandozeilenargumente zu übergeben kann man .Ppopen eine Liste übergeben. Die sieht dann folgendermassen aus: subprocess.Popen("C:\\Windows\\notepad.exe", "C:\\hallo.txt"). Dieses Beispiel öffnet das Notepad und übergibt das Argument um die Datei Hallo zu öffnen.')

output('Pythonscripts als Subprozess starten', 'Man kann auch Pythonprozesse als Subprozess starten indem man python.exe den Pfad zur Datei übergibt. Beispiel: subprocess.Popen(python_path, target_python_file')
subprocess.Popen([python_path, target_python_file])

output('Mit Standardanwendung öffnen', 'Mit der Funktion .Popen() kann man auch Files mit ihrer Standardanwendung öffnen. Dazu verwendet man beispielsweise subprocess.Popen(["start". "testfile.txt"], shell=True)')
process=subprocess.Popen(['start', '.\\testfile.txt'], shell=True)
output('Betriebssysteme', 'Je nach Betriebssystem muss man eine andere Anwendung aufrufen um ein File in der Standardanwendung zu öffnen. In Windows ist es "start" unter OSx ist es "open" und unter Linux "see". Auch ist shell=True nur unter Windows notwendig.')

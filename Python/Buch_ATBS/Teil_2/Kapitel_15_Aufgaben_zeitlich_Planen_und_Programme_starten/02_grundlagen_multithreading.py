# 02_grundlagen_multithreading.py

import os, re, time, datetime, threading

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

def say_hello():
    time.sleep(2)
    print('Dieser print wurde durch Multithreading erzeugt.')

output('Was ist Multithreading?', 'Alle bisher angewendeten Scripts waren single threaded da sie nur immer eine Zeile nach der anderen abgearbeitet hat. Python liest den Code also nur mit einem Finger. Mit Multithreading kann man weitere Finger hinzufügen. Heisst es können Teile vom Code simultan und/oder versetzt ausgeführt werden. Dazu wird das Modul threading benötigt.')
thread_object=threading.Thread(target=say_hello)
thread_object.start()
output('threading.Thread()', 'Mit threading.Thread lässt sich eine Funktion als zweitprozess ausführen. Beispiel mit der vorhandenen Funktion say_hello(): threading.Thread(target=say_hello) damit wird die Funktion say_hello() neben dem Laufenden Code ausgeführt.')

output('Funktionen mit Argumenten', 'Mit Multithreading können auch Funktionen als Ziel angegeben werden welche Argumente benötigen. Beispiel print-Funktion: threading.Thread(target=print, args=["Cats", "Dogs", "Frogs",], kwargs={"sep": " & "})')
threading.Thread(target=print, args=["Cats", "Dogs", "Frogs",], kwargs={'sep': ' & '}).start()
time.sleep(0.5)

output('Lesen/Schreiben', 'Das Problem beim Multithreading ist wenn mehrere Prozesse versuchen die selbe Variabel zu lesen oder zu schreiben. Dann kann es zu Konflikten kommen die nur schwer zu erkennen sind, daher sollte man darauf auchten dass die unterschiedlichen Threads nicht versuchen gleichzeitig auf eine Variable zuzugreiffen.')

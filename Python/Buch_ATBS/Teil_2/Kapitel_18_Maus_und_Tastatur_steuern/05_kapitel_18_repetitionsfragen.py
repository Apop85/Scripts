# 05_kapitel_18_repetitionsfragen.py

import re

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

output('Frage 1', 'Wie können sie Sicherheitseinrichtungen von PyAutoGUI auslösen, um ein Programm anzuhalten?')
output('Antwort', 'Mittels pyautogui.FAILSAFE = True kann man die Maus an die linke obere Ecke setzen um das Programm zu unterbrechen.')

output('Frage 2', 'Welche Funktion gibt die aktuelle Auflösung zurück?')
output('Antwort', 'Mit pyautogui.size() erhält man die momentane Auflösung des Bildschirms zurück')

output('Frage 3', 'Welche Funktion gibt die Koordinaten der aktuellen Mauszeigerposition aus?')
output('Antwort', 'Die Funktion pyautogui.position() gibt die X- und Y-Koordinaten für die aktuelle Mauszeigerposition aus.')

output('Frage 4', 'Was ist der Unterschied zwischen pyautogui.moveTo() und pyautogui.moveRel()?')
output('Antwort', 'Mit .moveTo() Geht die Maus zu der angegebenen Koordinate. Mit .moveRel() bewegt sich der Mauszeiger Relativ zur aktuellen Posiotion um die angegebenen Koordinaten.')

output('Frage 5', 'Welche Funktionen können sie benutzen um die Maus zu ziehen?')
output('Antwort', 'Man kann wie .moveTo() oder moveRel() die Funktionen .dragTo() und .dragRel() verwenden.')

output('Frage 6', 'Welcher Funktionsaufruf gibt die Zeichen des Strings "Hello World" zurück?')
output('Antwort', 'Mit pyautogui.typewrite("Hello World") wird der String in das aktuell aktive Fenster eingegeben.')

output('Frage 7', 'Wie können sie Sondertasten wie Beispielsweise den Linkspfeil verwenden?')
output('Antwort', 'Sondertasten kann man mit ihrem Namen übergeben. Beispielsweise "left" für Linkspfeil, oder "ctrl", "alt", "tab" usw... Dies funktioniert sowohl mit .typewrite(), mit .press(), mit .keyDown/Up() und mit .hotkey()')

output('Frage 8', 'Wie können sie den aktuellen Bildschirminhalt als Screenshot.png abspeichern?')
output('Antwort', 'Mittels pyautogui.screenshot("Screenshot.png") kann man den Screenshot speichern.')

output('Frage 9', 'Mit welchem Code legen sie nach jedem Aufruf von PyAutoGUI eine Pause ein?')
output('Antwort', 'Mittels pyautogui.PAUSE=1 würde nach jedem Aufruf eine Pause von einer Sekunde eingelegt werden.')
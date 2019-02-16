# 01_grundlagen_pyautogui.py


import re, pyautogui, os
from time import sleep as pause
from subprocess import Popen
os.chdir(os.path.dirname(__file__))

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

output('PyAutoGUI', 'PyAutoGUI kann sowohl Maus als auch Tastaturinteraktionen simulieren. Da PyAutoGUI jedoch nicht in der Standardbibliothek von Python inbegriffen ist muss dieses Modul erst nachinstalliert werden.')
output('Probleme mit PyAutoGUI', 'Bei PyAutoGUI können Probleme etwas schwerer zu beheben sein als sonst da das laufende Script die Maus steuert. Falls nicht anders möglich kann man sich mit dem Aktuellen Benutzer abmelden um so alle laufenden Programme zu schliessen.')
output('pyautogui.PAUSE =', 'Mit pyautogui.PAUSE = 2 lässt sich eine Pause einplanen bei welcher man die Möglichkeit hat die Kontrolle über die Maus zu erlangen um so das Programm zu schliessen.')
output('pyautogui.FAILSAVE', 'Standardmässig ist pyautogui.FAILSAVE = True gesetzt. Heisst wenn man den Mauszeiger in die linke obere Ecke reisst das Programm mit einer Fehlermeldung abgebrochen wird. Dies kann man auch verhindern wenn man FAILSAFE auf False setzt.')

output('Maussteuerung', 'Die Maussteuerung bei pyautogui funktioniert über ein zweidimensionales Koordinatensystem bei welchem die X- und die Y-Achse verwendet werden um die Position des Mauszeigers zu bestimmen.')

output('Auflösung', 'Das Koordinatensystem wird durch die Bildschirmauflösung definiert. Heisst bei einer Auflösung von 1900 x 1200 ist die untere rechte Ecke des Bildschirms bei 1899 x 1199.')
output('pyautogui.size()', 'Um die aktuell verwendete Auflösung herauszufinden kann man die Funktion pyautogui.size() verwenden. Diese gibt ein Tuple mit der X- und Y-Koordinate aus. Aktuell wird folgende Auflösung verwendet: '+str(list(pyautogui.size())[0])+' x '+str(list(pyautogui.size())[1]))
output('pyautogui.moveTo()', 'Um den Mauszeiger zu bewegen verwendet man die Funktion pyautogui.moveTo(X-Achste, Y-Achse, duration=Zeitangabe). In diesem Beispiel wird der Mauszeiger in die mitte des Bildschirms gesetzt.')
pyautogui.moveTo(list(pyautogui.size())[0]/2, list(pyautogui.size())[1]/2, duration=0.5)

output('pyautogui.moveRel()', 'Mit pyautogui.moveRel(X-Achse, Y-Achse, Dauer) muss man die genaue Position des Mauszeigers nicht wissen. Man kann so den Mauszeiger relativ zur momentanen Position bewegen.')
pyautogui.moveRel(200,0,duration=0.5)
pyautogui.moveRel(0,-200,duration=0.5)
pyautogui.moveRel(-200,0,duration=0.5)
pyautogui.moveRel(0,200,duration=0.5)

output('pyautogui.position()', 'Mit der Funktion pyautogui.position() kann man die aktuelle Position des Mauszeigers auslesen. Momentan ist der Mauszeiger bei folgender Position: X: '+str(list(pyautogui.position())[0])+' Y: '+str(list(pyautogui.position())[1]))

output('pyautogui.click()', 'Mit der Funktion pyautogui.click() lassen sich Mausklicks simulieren. Optional kann man auch die Koordinaten übergeben an welcher der Klick stattfinden soll. Will man eine spezielle Maustaste ansteuern kann man button=left/right/middle als Argument übergeben.')
pyautogui.click(10,10, button='right')
pause(1)
pyautogui.click(500,500, button='left')
output('.rightClick(), .leftClick(), .middleClick()', 'Alternativ zum Argument button= kann man auch direkt die Funktionen .rightClick(), .middleClick() und .leftClick() verwenden.')
output('.doubleClick()', 'Mit pyautogui.doubleClick() lassen sich Doppelklicks simulieren.')
output('pyautogui.mouseDown() und pyautogui.mouseUp()', 'Mit der Funktion .mouseDown() lässt sich ein langer Klick simulieren der erst aufhört wenn die Funktion .mouseUp() aufgerufen wird.')

output('pyautogui.dragTo() und pyautogui.dragRel()', 'Die Funktionen pyautogui.dragTo() und pyautogui.dragRel() nehmen die selben Argumente entgegen wie moveTo() oder moveRel(). Diese Funktionen halten den Mauszeiger gedrückt bis sie an der angegebenen Position angekommen sind.')
Popen(r'C:\WINDOWS\system32\mspaint.exe')
pyautogui.moveTo(list(pyautogui.size())[0]/2, list(pyautogui.size())[1]/2, duration=1.5)
distance=300
d_time=0.2
delta=10
while distance > 0:
    pyautogui.dragRel(distance, 0, duration=d_time) # Nach rechts
    distance-=delta
    pyautogui.dragRel(0, distance, duration=d_time) # Nach unten
    pyautogui.dragRel(-distance, 0, duration=d_time) # Nach links
    distance-=delta
    pyautogui.dragRel(0, -distance, duration=d_time) # Nach oben
pyautogui.click(list(pyautogui.size())[0], 5)
pyautogui.click(list(pyautogui.size())[0]/2, list(pyautogui.size())[1]/2)

output('pyautogui.scroll()', 'Mitt der .scroll()-Funktion kann man auf dem Bildschirm hoch und runter scrollen. Dafür wird jeweils ein positiver oder negativer Integer als Argument übergeben um die Richtung zu bestimmen.')
pyautogui.scroll(200)
pause(1)
pyautogui.scroll(-200)

output('pyautogui.screenshot()', 'Mit pyautogui.screenshot() wird ein Image-Objekt mit dem aktuellen Bildschirminhalt erstellt.')
output('Bildschirminhalt analysieren', 'Durch den Screenshot lässt sich nun der Bildschirminhalt anhand der Farben analysieren. Mann kann zum Beispiel Prüfen ob der Button den man drücken will wirklich das Grau aufweist welches der Button haben sollte. Falls nicht kann man das Programm anweisen zu unterbrechen da der Button nicht an der erwarteten Stelle aufgetaucht ist. Um die Farben auszulesen muss das Modul Image aus dem Modul Pillow importiert werden. Mit .getpixel((x,y)) kann man die Farbwerte des entsprechenden Pixels dann auslesen.')
output('pyautogui.pixelMatchesColor()', 'Mit der Funktion .pixelMatchesColor(x,y,(R,G,B)) kann man Prüfen ob der erwartete Farbwert mit dem gemessenen Wert übereinstimmt. Stimmen die Farbwerte überein so gibt die Funktion "True" zurück.')

output('Bilderkennung', 'Mit pyautogui kann man auch nach Bestimmten Inhalten auf dem Bildschirm suchen indem man ein Bild des gesuchten Objekts bereitstellt.')
start_location=pyautogui.locateOnScreen('.\\start_logo.png')
output('pyautogui.locateOnScreen()', 'Mit der Funktion .locateOnScreen(filename) kann man die Koordinaten des gesuchten Objektes als Vierecktuple auslesen. Zu beachte ist jedoch dass pyautogui nur Objekte findet die genau der Grösse und den vorgegebenen Farben entspricht. Beispielsweise befindet sich der Startbutton an folgender Position: '+str(start_location))
output('pyautogui.center()', 'Um nun das Zentrum des gefundenen Bildes herauszukriegen, also das Zentrum des Buttons, kann man die Funktion .center((vierecktupel)) verwenden. Beim Startbutton wäre das Zentrum dann: '+str(pyautogui.center(start_location)))

output('Steuern der Tastatur', 'Mit pyautogui lassen sich ebenfalls Tastenanschläge simulieren.'); pyautogui.typewrite('Hello World')
output('pyautogui.typewrite()', 'Mit der Funktion .typewrite(string) lässt sich Text in das aktive Fenster eingeben. Man kann auch eine Verzögerung zwischen den Zeichen eingeben indem man als zweites Argument die länge der Pause übergibt')
output('Sonderzeichen mit .typewrite()', 'Um Sonderzeichen oder Tastenabfolgen zu Schreiben muss man diese als Liste übergeben. Die Sondertasten die man betätigen möchte kann man mit ihrem Namen als String übergeben. Beispiel: .typewrite("backspace", "up", "left", "capslock", ect...)')
output('Tastenkombinationen', 'Ähnlich wie bei .mouseUp() und .mouseDown() kann man bei der Tastatur .keyDown() und .keyUp() verwenden um Tastenkombinationen oder generell das längere Drücken einer Taste zu simulieren.')
for i in range(2):
    pyautogui.keyDown('alt')
    pyautogui.keyDown('tab')
    pyautogui.keyUp('alt')
    pyautogui.keyUp('tab')
    pause(0.5)
output('Tastenkombinationen', 'Alternativ kann man mit .hotkey() auch eine Abfolge von Tasten angeben die nacheinander gedrückt und in umgekehrter Reihenfolge wieder losgelassen werden sollen.')
for i in range(2):
    pyautogui.hotkey('alt', 'tab')
    pause(0.5)


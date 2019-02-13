# 05_kapitel_17_repetitionsfragen.py

from PIL import ImageColor
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

output('Frage 1', 'Was ist ein RGBA-Wert?')
output('Antwort', 'Mit RGBA wird der Rot-, Gelb-, Blau und Alphaanteil (Transparenz) angegeben. Das ganze wird aus einem Tuple mit 4 Zahlen von 0-255 angegeben welche für die einzelnen Kanäle stehen.')

output('Frage 2', 'Wie bestimmen sie mit pillow den RGBA-Wert von magenta?')
output('Antwort', 'Dazu benötigt man die Funktionen von ImageColor aus dem Modul PIL. ImageColor.getcolor("magenta", "RGBA")')

output('Frage 3', 'Was ist ein Rechtecktuple?')
output('Antwort', 'Das ist ein Tuple welches die Eckkoordinaten eines Rechtecks aufweisen mit der Reihenfolge links, oben, rechts, unten. Beispiel: (10,10,60,60) Dies zeichnet Die Eckpunkte eines Rechtecks ab welches bei der Position x10 y10 startet und eine Kantenlänge von 50 Pixel hat')

output('Frage 4', 'Felche Funktion gibt das Image-Objekt einer Bilddatei zurück? z.B. zophie.png?')
output('Antwort', 'Die Funktion Image.open("zophie.png") gibt ein entsprechendes Image-Objekt zurück.')

output('Frage 5', 'Wie können sie Breite und Höhe des Bildes bestimmen, für das ein Image-Objekt steht?')
output('Antwort', 'Die Abmassungen eines Bildes kann man mit image_object.size herausfinden. Dies gibt dann ein Tuple zurück mit den Abmassungen in Höhe und Breite')

output('Frage 6', 'Wie können sie ein Bild auf die obere rechte Ecke (50 x 50) zuschneiden?')
output('Antwort', 'image_object.crop((0,50,50,50))')

output('Frage 7', 'Wie speichern sie ein Image-Objekt in einer Bilddatei nachdem sie Änderungen daran vorgenommen haben?')
output('Antwort', 'Mittels image_object.save(file_name)')

output('Frage 8', 'Welches Modul von pillow kann man Formen zeichnen?')
output('Antwort', 'Mit dem Modul ImageDraw aus PIL kann man freie Formen zeichnen. Punkte, Linien, Ellipsen, Vierecke und Polygone')

output('Frage 9', 'Image-Objekte verfügen über keine Zeichenmethoden. Welche Arten von Objekten verfügen darüber? Wie können sie ein solches Objekt erstellen?')
output('Antwort', 'Mittels draw=ImageDraw.draw(image_object) erstellt man ein draw-Objekt mit dem Namen draw mit welchem man Zeichnen kann')
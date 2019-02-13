# 01_grundlagen_bildbearbeitung.py
# Um Bilder zu bearbeiten wird in den nachfolgenden Scripts das Modul pillow verwendet
# welches gegebenenfalls noch nachinstalliert werden muss.

import re, os
from PIL import ImageColor, Image
from random import randint as rng

os.chdir(os.path.dirname(__file__))
file_list=os.listdir('.\\')
for file_name in file_list:
    if file_name != 'zophie.png' and file_name.endswith('.png') or file_name.endswith('.jpg'):
        os.remove(file_name)

example_image='.\\zophie.png'

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

output('Aufbau eines Bildes', 'Bilder kann man in ein Zweidimensionales Kordinatensystem übertragen in welchem jeder Pixel wie ein Eintrag in einer Zelle zu verstehen ist. In dieser Zelle stehen dann die Farbeigenschaften dieses Pixels.')

output('RGBA', 'Die Meisten Bilder stellen ihre Farben im RGBA-Modus dar. Heisst der Anteil an Rot, Gelb, Blau und Transparenz (Alpha).')
output('RGBA Ausdruck', 'Diese RGBA-Werte werden mittels einem Integer von 0 (kein Anteil) bis 255 (maximaler Anteil) definiert.')
output('Angabe von RGBA-Werten', 'RGBA-Werte werden als Tuple angegeben. Beispielsweise (255, 0, 0, 255) welches die Farbe Rot definiert und gleichzeitig nicht Transparent ist. (A=0 volle Transparenz, A=255 keine Transparenz).')
output('Beispiel RGBA-Werte', 'Weiss: 255,255,255,255. Rot: 255,0,0,255. Blau: 0,0,255,255. Gelb: 0,255,0,255. Eine Mischfarbe, beispielsweise Grün: 0,255,255,255')

output('from PIL import ImageColor', 'Die Funktion ImageColor hilft dabei Tuples für bestimmte Farbnamen auszugeben')
output('ImageColor.getcolor()', 'Damit man sich die einzelnen Farben nicht ausswendig Merken muss gibt es beim Modul pillow die Funktion ImageColor.getcolor(). Mit diesem kann man Farbnamen in Tuples umwandeln. Im ersten Argument übergibt man den Farbnamen und im zweiten die Farbpalette. Beispiel: ImageColor.getcolor("red", "RGBA") Beispielsweise ergibt die Farbe chocolate: '+str(ImageColor.getcolor('chocolate', 'RGBA')))

output('additives/substraktives Farbmodell', 'Drucker, Tinten, Malfarben und Pigmente benutzen das substraktive Farbmodell CMYK. Computermonitore erzeugen die Farben jedoch durch Licht welches mit dem additiven Farbmodell RGB dargestellt werden.')

output('Rechtecktuple', 'Viele Funktionen und Methoden nehmen ein Rechtecktuple entgegen. Dazu werden vier Argumente benötigt: Die X-Koordinate der Linken Kante des Rechtecks, Die Y-Koordinate der oberkante des Rechtecks, eine X-Koordinate welche bestimmt wie weit das Rechteck vom Bildrand entfernt ist. Die Y-Koordinate welche bestimmt bis wo das Rechteck in der Y-Koordinate gehen soll.')

output('from PIL import Image', 'Die Funktion Image aus dem Modul PIL lassen sich Bilder öffnen und bearbeiten.')
image_object=Image.open(example_image)
output('Image.open()', 'Die Funktion Image.open(file_name) erstellt ein pillow-Objekt mit den entsprechenden Bildinformationen. Der Typ dieses Objekts ist: '+str(type(image_object)))
output('image_object.save()', 'Mit der Funktion image_object.save(file_name) kann man das Bild anschliessend in ein beliebiges Format speichern.')
image_object.save('.\\zophie.jpg')

output('image_object.size', 'Mittels der Funktion image_object.size() lässt sich ein Tuble-Wert auslesen mit Höhe und Breite des Bildes. Beispiel: width, height = image_object.size(). Die Grösse des Beispielbildes wäre hier: '+str(image_object.size))
output('image_object.filename', 'Die Funktion image_object.filename gibt den originalen Dateinamen zurück. Beispiel: '+str(image_object.filename))
output('image_object.format', 'Die Funktion image_object.format gibt das Dateiformat der originalen Datei zurück. Beispiel: '+str(image_object.format))
output('image_object.format_description', 'Die Funktion .format_description gibt die selbe Information zurück wie .format einfach nicht als Kürzel sondern als ausgeschriebener Name. Beispiel: '+str(image_object.format_description))

output('Image.new()', 'Im gegensatz zu Image.open() gibt Image.new() ein leeres Image-Objekt zurück. Um ein neues Bild zu erstellen müssen der Funktion die drei Nachfolgenden Attribute übergeben werden')
output('Image.new() Farbmodus', 'Als erstes Argument muss der Farbmodus angegeben werden in welchem das neue Bild erstellt werden soll. Beispiel: Image.new("RGBA", ....)')
output('Image.new() Grösse', 'Als Zweites Argument übergibt man der Funktion den Tuple-Wert mit der gewünschten Grösse des Bildes. Beispiel: Image.new("RGBA", (100,200),.....)')
output('Image.new() Hintergrundfarbe', 'Als letzztes Argument übergibt man die gewünschte Hintergrundfarbe des neuen Bildes. Beispiel: Image.new("RGBA", (100,200), "purple"). Wird keine Hintergrundfarbe angegeben ist das Bild standardmässig Transparent')
new_image=Image.new('RGBA', (100,200), 'purple')
new_image.save('.\\purple.png')

output('Bilder zuschneiden', 'Bilder lassen sich mit der Funktion image_object.crop() auf einen gewünschten Bereich des Bildes zuschneiden.')
output('image_object.crop()', 'Der Funktion .crop() muss man das bereits erwähnte Rechtecktuple angeben. Beispielsweise: image_object.crop((335,345,565,560))')
cropped_image=image_object.crop((335,345,565,560))
cropped_image.save('.\\cropped_image.jpg')

output('Bilder kopieren und in andere Bilder einfügen', 'Es lassen sich Image-Objekte kopieren um so das Ursprüngliche Bild nicht zu verändern. Auch kann man diese anschliessend in andere Bilder einfügen.')
output('image_object.copy()', 'Mit der Funktion image_object.copy() wird der Inhalt des Image-Objektes kopiert')
copy_of_image_object=image_object.copy()
cat_face=copy_of_image_object.crop((335,345,565,560))

output('image_object.paste()', 'Mit der Funktion image_object.paste(paste_object, (x_axis, y_axis)) lässt sich der Inhalt des Image-Objekts in ein anderes Bild einfügen. Dazu müssen die Eck-Koordinaten der gewünschten Stelle als Argument übergeben werden. Beispiel: image_object.paste(cat_face,(0,0)) um das kopierte Objekt in der oberen linken Ecke einzufügen.')
copy_of_image_object.paste(cat_face, (0,0))
copy_of_image_object.paste(cat_face, (400,500))
copy_of_image_object.save('.\\paste_example.png')

output('Kachelmuster erstellen', 'Mit Hilfe der .paste()-Funktion lassen sich Beispielsweise Kachelmuster mittels einem For-Loop erstellen')
original_width, original_height = copy_of_image_object.size
cat_face_width, cat_face_height = cat_face.size
for x_axis in range(0, original_width, cat_face_width):
    for y_axis in range(0, original_height, cat_face_height):
        copy_of_image_object.paste(cat_face, (x_axis, y_axis))

output('image_object.resize()', 'Mit der Funktion .resize() lässt sich die Grösse eines Bildes ändern. .resize() ändert das Bild selbst nicht sondern gibt ein neuse Image-Objekt zurück')
resized_image=copy_of_image_object.resize((int(original_width/2), int(original_height/2)))
resized_image.save('.\\tiles_resized.png')

output('image_object.rotate()', 'Die funktion image_object.rotate(90) dreht das Bild um 90° gegen den Uhrzeigersinn')
resized_image.rotate(90).save('.\\rotated.png')

output('image_object.rotate(6, expand=True)', 'Mit dem Attribut expand=True kann man definieren dass, falls das Bild nicht auf die ursprüngliche FLäche passt, das Bild entsprechend gestreckt wird.')
resized_image.rotate(6, expand=True).save('.\\rotate_and_expand.png')

output('image_object.transpose()', 'Mit der Funktion .transpose() lässt sich ein Bild spiegeln. Dafür muss man das Argument "Image.FLIP_LEFT_RIGHT" für horizontale und "Image.FLIP_TOP_BOTTOM" für vertikale Spiegelung.')
resized_image.transpose(Image.FLIP_LEFT_RIGHT).save('.\\mirrored_image.png')

output('image_object.getpixel()', 'Mit der Funktion .getpixel() lassen sich die aktuellen Werte eines Pixels aufrufen. Beispiel: image_object.getpixel((0,0)) ergibt: '+str(image_object.getpixel((0,0))))
output('image_object.putpixel()', 'Mit der Funktion .putpixel() lassen sich einzelne Pixel im Bild verändern. So lassen sich Beispielsweise in einem For-Loop eine ganze FLäche füllen. Auch hier ändert sich nicht das Objekt selber sondern es wird ein neues Image-Objekt erstellt')
new_image=Image.new('RGBA', (200,100))
for x_axis in range(200):
    for y_axis in range(100):
        new_image.putpixel((x_axis, y_axis), (rng(0,255),rng(0,255),rng(0,255)))
new_image.save('.\\putpixel_example.png')

# 03_formen_zeichnen.py
# In dieser Übung geht es darum Formen zu zeichnen mit der Funktion ImageDraw

from PIL import Image, ImageDraw
import os

os.chdir(os.path.dirname(__file__))
target_file='.\\drawed_image.png'
if os.path.exists(target_file):
    os.remove(target_file)

new_image=Image.new('RGBA', (200,200), 'white')
# Erstelle Draw-Objekt
draw=ImageDraw.Draw(new_image)

# Die Parameter "fill" und "outline" sind Optional, werden diese weggelassen verwendet DrawImage dafür die Farbe Weiss
# Punkte zeichnen
point_coordinates=[(160,10),(160,30),(160,50),(160,70),(160,90)]
draw.point(point_coordinates, fill='black')
# Linien zeichnen
line_coordinates=[(10,10),(10,60),(60,60)]
draw.line(line_coordinates, fill='black', width=5)
# Rechtecke zeichnen mit Rechtecktuple (links,oben,rechts,unten)
square_props=(100,100,150,150)
draw.rectangle(square_props, fill='red', outline='green')
# Ellypsen zeichnen mit Rechtecktuple
ellipse_props=(50,150,100,200)
draw.ellipse(ellipse_props, fill='blue', outline='magenta')
# Polygone zeichnen
polygon_props=[(10,180), (30,170), (45,150), (25,145), (15,160)]
draw.polygon(polygon_props, fill='black')

for i in range(110, 200, 10):
    line_coordinates=[(0,i),(i-100,200)]
    draw.line(line_coordinates, fill='red', width=2)
new_image.save(target_file)
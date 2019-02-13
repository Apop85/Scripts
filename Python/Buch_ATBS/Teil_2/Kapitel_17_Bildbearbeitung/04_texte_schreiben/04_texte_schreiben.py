# 04_texte_schreiben.py
# In diesem Beispiel geht es darum Texte in ein Bild zu schreiben mittels ImageFont aus dem Modul PIL

from PIL import Image, ImageFont, ImageDraw
import os

os.chdir(os.path.dirname(__file__))
target_file='.\\text_in_image.png'
if os.path.exists(target_file):
    os.remove(target_file)
windows_font_dir='C:\\Windows\\Fonts'

image_object=Image.new('RGBA', (300,300), 'white')
draw=ImageDraw.Draw(image_object)
draw.text((20,150), 'Hello', fill='brown')

arial_font=ImageFont.truetype(windows_font_dir+'\\arial.ttf', 32)
draw.text((80,150), 'World', fill='purple', font=arial_font)

image_object.save(target_file)
# 02_logo_in_bilder_einfügen.py
# In diesem Beispiel geht es darum alle Bilder in einem Unterordner zu finden und diese
# mit einem Logo zu versehen. 

from PIL import Image
from random import randint as rng
import os
os.chdir(os.path.dirname(__file__))

target_dir='.\\change_me'
logo='.\\logo.png'
logo_picture=Image.open(logo)
logo_dimensions=logo_picture.size
copied_logo=logo_picture.resize((int(logo_dimensions[0]//4), int(logo_dimensions[1]/4))).copy()
logo_dimensions=copied_logo.size

def file_generator():
    for file_name in os.listdir(target_dir):
        if file_name.endswith('png'):
            os.remove(target_dir+'\\'+file_name)
    for i in range(50):
        new_image=Image.new('RGBA', (500,500), (rng(0,255), rng(0,255), rng(0,255)))
        new_image.save(target_dir+'\\picture'+str(i)+'.png')

def get_pic_names():
    for file_name in os.listdir(target_dir):
        if file_name.lower().endswith('png') or file_name.lower().endswith('.jpg'):
            place_logo(file_name)

def place_logo(file_name):
    image_object=Image.open(target_dir+'\\'+file_name)
    dimensions=image_object.size
    position=(dimensions[0]-logo_dimensions[0],0)
    # copied_logo muss zwei mal angegeben werden damit die Transparenten werte nicht einfach Weiss dargestellt werden.
    image_object.paste(copied_logo, position, copied_logo)
    image_object.save(target_dir+'\\'+file_name)

file_generator()
get_pic_names()
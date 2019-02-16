# 03_maus_position.py
# In dieser Übung geht es darum die aktuelle Mauszeigerposition anzuzeigen.

import pyautogui
from time import sleep
from PIL import Image

print('CTRL+C oder mit der Maus an die obere linke Ecke um abzubrechen.')
while True:
    scr_shot=pyautogui.screenshot()
    r,g,b=scr_shot.getpixel((list(pyautogui.position())[0],list(pyautogui.position())[1]))
    string='X: '+str(list(pyautogui.position())[0])+'  Y: '+str(list(pyautogui.position())[1])+' R:'+str(r)+' G:'+str(g)+' B:'+str(b)+'       '
    print(string, end='')
    print('\b'*len(string), end='', flush=True)
    if list(pyautogui.position())[0] == 0 and list(pyautogui.position())[1] == 0:
        break
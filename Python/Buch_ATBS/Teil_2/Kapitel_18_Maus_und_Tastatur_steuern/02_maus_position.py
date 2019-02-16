# 02_maus_position.py
# In dieser Übung geht es darum die aktuelle Mauszeigerposition anzuzeigen.

import pyautogui
from time import sleep

print('CTRL+C oder mit der Maus an die obere linke Ecke um abzubrechen.')
while True:
    string='X: '+str(list(pyautogui.position())[0])+'  Y: '+str(list(pyautogui.position())[1])+'      '
    print(string, end='')
    print('\b'*len(string), end='', flush=True)
    if list(pyautogui.position())[0] == 0 and list(pyautogui.position())[1] == 0:
        break
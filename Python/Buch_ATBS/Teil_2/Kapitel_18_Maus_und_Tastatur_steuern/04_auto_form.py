# 04_auto_form.py
# In dieser Übung geht es darum ein Forumlar automatisch mit unterschiedlichen Daten auszufüllen

import pyautogui, os, webbrowser, time, logging
from random import randint as rng

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

os.chdir(os.path.dirname(__file__))
first_field='.\\field.png'
form_url='https://docs.google.com/forms/d/e/1FAIpQLScSVDFU76rZvbO_tiIwSt6d9sOK0CZyS9KKMCP6cP5O5W5lVQ/viewform'

webbrowser.open(form_url)
time.sleep(5)

names=['Susi', 'Bernhard', 'Lisa', 'Otto', 'Waalkes', 'Superman', 'Batman', 'Batwoman', 'Laika', 'Pretzel', 'Robocop']
fears=['Mäuse', 'Spinnen', 'Kinder', 'Wasser', 'Männer', 'Frauen', 'Kryptonit', 'Teetassen', 'Kirchentürme', 'Leitungswasser', 'Kohlensäure', 'Käse', 'Laktose', 'Kühlschrankmagnet', 'Haar in der Suppe']
comments=['Nice Book', 'Learned a lot', 'Thank you', 'No comment', 'Nice Job', 'No Way you just did this...', 'Love it', 'Reddit Forum is almost inactive', '10/10 Would buy again']

for i in range(len(names)):
    for j in range(2):
        pyautogui.hotkey('tab')
    logging.info('Beginne Formular für '+names[i])
    pyautogui.typewrite(names[i])
    pyautogui.hotkey('tab')
    pyautogui.typewrite(fears[rng(0,len(fears)-1)])
    pyautogui.hotkey('tab')
    for j in range(rng(1,4)):
        pyautogui.hotkey('down')
    pyautogui.hotkey('enter')
    time.sleep(0.5)
    pyautogui.hotkey('tab')
    for j in range(rng(0,4)):
        pyautogui.hotkey('right')
    pyautogui.hotkey('tab')
    pyautogui.typewrite(comments[rng(0,len(comments)-1)])
    pyautogui.hotkey('tab')
    pyautogui.hotkey('enter')
    time.sleep(3)
    pyautogui.hotkey('tab')
    pyautogui.hotkey('enter')
    time.sleep(3)

pyautogui.hotkey('ctrl','w')
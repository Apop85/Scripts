# 14_2048_autoplay.py
# Dieses Script soll mittels Selenium die Website https://play2048.co/ öffnen und
# durch ein Bestimmtes Muster von Tasteneingaben das entsprechende Spiel spielen.
# https://play2048.co/
# Spielmuster: hoch, rechs, unten, links

import os
from selenium import webdriver
from selenium.webdriver.common.keys import Keys

gecko = os.path.normpath(os.path.join(os.path.dirname(__file__), 'geckodriver'))
binary = r'C:\Program Files (x86)\Mozilla Firefox\firefox.exe'

target_url='https://play2048.co/'

browser=webdriver.Firefox(firefox_binary=binary, executable_path=gecko+'.exe')
browser.get(target_url)
html_content=browser.find_element_by_tag_name('html')
while True:
    html_content.send_keys(Keys.UP)
    html_content.send_keys(Keys.RIGHT)
    html_content.send_keys(Keys.DOWN)
    html_content.send_keys(Keys.LEFT)
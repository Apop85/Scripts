# 13_uebung_digitec_suche.py
# Dieses Übungsscript soll auf Dicitec nach einem Produkt suchen und 
# die Suche direkt auf die vorgegebenen Eigenschaften beschränken

import os
from selenium import webdriver
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.support.ui import Select
from time import sleep

gecko = os.path.normpath(os.path.join(os.path.dirname(__file__), 'geckodriver'))
binary = r'C:\Program Files (x86)\Mozilla Firefox\firefox.exe'

target_url='https://www.conrad.ch/'

while True:
    print('Bitte Produkt angeben:')
    product=input()
    # product='Druckerpapier'
    print('Bitte maximaler Preis angeben:')
    max_price=input()
    # max_price='15'
    if max_price.isdecimal():
        break

browser=webdriver.Firefox(firefox_binary=binary, executable_path=gecko+'.exe')
try:
    browser.get(target_url)
    search_field=browser.find_element_by_id('search-input')
    search_field.send_keys(product)
    search_field.submit()
    search_field.clear()
    sleep(7)
    search_field=browser.find_element_by_id('maxValue-price')
    select_all=Keys.CONTROL + 'a'
    search_field.send_keys(select_all)
    search_field.send_keys(max_price)
    html_content=browser.find_element_by_tag_name('html')
    search_field.send_keys(Keys.ENTER)
    sleep(7)
    select=Select(browser.find_element_by_name('sort'))
    select.select_by_visible_text('Preis aufsteigend')
    sleep(2)
    html_content.send_keys(Keys.PAGE_DOWN)


except Exception as error_message:
    print(error_message)
    browser.close()


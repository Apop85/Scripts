# 10_virtueller_programmierer.py
# Kleines Script zur Entspannung welches ein Code einer Webseite in das Terminal ausgibt als 
# würde es ein Mensch schreiben
from random import randint as rng
from time import sleep
import requests, bs4

print('Webseite angeben:')
website=input()
website_content=requests.get(website)
website_content.raise_for_status()
content=bs4.BeautifulSoup(website_content.text, features="html.parser")
for i in range(len(str(content))):
    if str(content)[i].isalnum() or str(content)[i] == ' ' or str(content)[i] == '\n' or str(content)[i] == '.':
        sleep(rng(3,10)/100)
    else:
        sleep(rng(15,30)/100)
    print(str(content)[i], end='')
# 06_scraping_mit_beatifulsoup.py
# Dies ist ein Beispiel wie man mit BeautifullSoup eine Webseite durchforsten kann.
# Dazu muss das Modul beautifullsoup4 installiert sein. Ggf mit pip3 install beautifulsoup4 nachinstallieren

import requests, bs4

url_name='https://de.wikipedia.org/wiki/Aspirin'
url_content=requests.get(url_name)
url_content.raise_for_status()
bs4_element=bs4.BeautifulSoup(url_content.text)
print(type(bs4_element))
titel=bs4_element.select('title')
print(titel)
print('\n')
span=bs4_element.findAll('span')[0]
print(span)
print('\n')
print(span.get('class'))

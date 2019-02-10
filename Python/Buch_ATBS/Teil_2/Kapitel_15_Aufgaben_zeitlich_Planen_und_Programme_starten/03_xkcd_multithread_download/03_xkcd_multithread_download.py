# 03_xkcd_multithread_download.py
# In dieser Übung geht es darum den Download der Comics zu beschleunigen
# indem man mehrere Threads zum downloaden nutzt.

import os, threading, requests, bs4
os.chdir(os.path.dirname(__file__))

target_dir='.\\comics'
source_url='https://xkcd.com'

url_content=requests.get(source_url)
try:
    url_content.raise_for_status()
except:
    print('URL xkcd.com kann nicht aufgerufen werden. Script wird beendet.')
    exit()

def download_comic(comic_url):
    None

def scrape_comic_links():
    # while link_counter != comic_target_amount:
    bs4_object=bs4.BeautifulSoup(url_content.text, features='html.parser')
    bs4_next_result=bs4_object.select('a[rel="prev"]')
    next_url=bs4_next_result[0].get('href')
    bs4_comic_result=bs4_object.select('div #comic img ')
    # link_counter+=1
    # threading.Thread(target=download_comic, args=[source_url+comic_url])
    print(bs4_comic_result)   

while True:
    print('Wieviele Comics sollen heruntergeladen werden?')
    comic_target_amount=input()
    if comic_target_amount.isdecimal():
        scrape_comic_links()
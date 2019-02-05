# 08_downloade_alle_xkcd_comics.py
# In diesem Projekt geht es darum auf einer Webseite tieferführenden Schaltflächen, 
# namentlich Prev, zu folgen und auf jeder Seite das entsprechende Comic herunterladen

import requests, bs4, os, shutil
os.chdir(os.path.dirname(__file__))

if not os.path.exists('.\\comics'):
    os.mkdir('.\\comics')
else:
    shutil.rmtree('.\\comics')
    os.mkdir('.\\comics')
os.chdir('.\\comics')

url_name='https://xkcd.com/'
next_url, counter='', 0


def get_file_and_url(url_name):
    global counter
    url_content=requests.get(url_name)
    url_content.raise_for_status()
    bs4_element=bs4.BeautifulSoup(url_content.text, features='html.parser')
    buttons=bs4_element.select('a[rel="prev"]')
    next_url=buttons[0].get('href')
    picture=bs4_element.select('#comic img')
    picture_url=picture[0].get('src')
    if counter < 10:
        filename='000'+str(counter)+' '+picture_url.split('/')[-1]
    elif counter < 100:
        filename='00'+str(counter)+' '+picture_url.split('/')[-1]
    elif counter < 1000:
        filename='0'+str(counter)+' '+picture_url.split('/')[-1]
    elif counter < 10000:
        filename=str(counter)+' '+picture_url.split('/')[-1]
    else:
        print('End of possible Filenameiterations')
        return ''
    counter+=1
    download_file_now(filename, picture_url)
    return next_url

def download_file_now(filename, picture_url):
    global counter
    download_file=open(filename, 'wb')
    if picture_url[:2] != '//':
        picture_url='//imgs.xkcd.com'+picture_url
    picture_url_absolute=requests.get('https:'+picture_url)
    try:
        picture_url_absolute.raise_for_status()
    except Exception as error_message:
        print('Datei auf Server nicht gefunden')
        counter-=1
        return
    print('Download: '+filename)
    for chunk in picture_url_absolute.iter_content(10**6):
        download_file.write(chunk)
    download_file.close()

while True:
    current_url_name=url_name+next_url
    next_url=get_file_and_url(current_url_name)
    if next_url == '' or []:
        print('Done')
        break

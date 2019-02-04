# 03_download_mittels_request.py
# Da request nicht standardmässig installiert ist muss es mit pip install request nachinstalliert werden.

import requests, os, logging
os.chdir(os.path.dirname(__file__))

fake_url='http://inventwithpython.com/nicht_existierende_seite'
real_url='https://raw.githubusercontent.com/Apop85/Scripts/master/Python/Buch_ATBS/Teil_2/Kapitel_09_Dateien_verwalten/08_selektives_verschieben/sort_me/unterordner/gruen.jpg'

logging.basicConfig(level=logging.INFO, format=' %(asctime)s - %(levelname)s - %(message)s ')

def log_it():
    logging.debug('Type:'+str(type(ressource))+' Statuscode OK = '+str(ressource.status_code == requests.codes.ok)+' Länge: '+str(len(ressource.text)))


ressource=requests.get(real_url)
log_it()

try:
    ressource=requests.get(fake_url)
    ressource.raise_for_status()
except Exception as error_message:
    print('Ein Fehler ist aufgetreten: %s' % (error_message))
    input()


if os.path.exists(r'.\gruen.jpg'):
    logging.debug('File \\gruen.jpg wird entfernt')
    os.remove(r'.\gruen.jpg')

ressource=requests.get(real_url)
log_it()
ressource.raise_for_status()
logging.info('Download der Datei beginnt...')
downloaded_file=open(r'.\gruen.jpg', 'wb')
for chunk in ressource.iter_content(10**5):
    downloaded_file.write(chunk)
downloaded_file.close()
logging.info('Download der Datei abgeschlossen')

    
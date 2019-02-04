# 04_suche_nach_summenformel_auf_wikipedia.py
# Webscraper für Summenformeln aus Wikipedia

import requests, logging, re, os
os.chdir(os.path.dirname(__file__))

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

def search_in_string(content):
    global attempt
    logging.debug('Suche nach Summenformeln...')
    search_pattern=re.compile(r'>Summenformel<(.*?)</tr>', re.DOTALL)
    results=search_pattern.findall(content)
    logging.debug('results:  '+str(results))
    search_pattern=re.compile(r'>([[A-Za-z]{1,}).*?>(\d*)')
    result=search_pattern.findall(str(results))
    logging.debug('result:  '+str(result))
    if len(result) != 0:
        summenformel=''
        for i in range(len(result)):
            logging.debug('Loop vergleich:'+''.join(result[i])+'--->'+''.join(result[i-1]))
            if result[i] == result[i-1]:
                break
            summenformel+=''.join(result[i])
        logging.info('Summenformel gefunden:'+summenformel)
    else:
        if attempt != 1:
            url_name='https://de.wikipedia.org/wiki/'+s_name.title()+'eigenschaften'
            attempt=1
            content=get_content(url_name)
            download_file(content)
        else:
            logging.info('Keine Summenformel gefunden.')

def download_file(content):
    temp_file=open('.\\tempfile.tmp', 'wb')
    logging.info('Download beginnt:')
    for chunks in content.iter_content(10**6):
        temp_file.write(chunks)
    temp_file.close()
    logging.info('Download abgeschlossen:')
    read_content()

def read_content():
    temp_file=open('.\\tempfile.tmp', encoding='UTF-8')
    try:
        content=(temp_file.read())
    except Exception as error_message:
        logging.error('Fehler beim verarbeiten des Dateiinhalts')
        print('Fehler aufgetreten: %s' % [error_message])
        return
    temp_file.close()
    search_in_string(str(content))

def get_content(url_name):
    logging.debug('Prüfe URL')
    content=requests.get(url_name)
    content.raise_for_status()
    return content

while True:
    try:
        attempt=''
        print('Stoffname eingeben:')
        s_name=input()
        url_name='https://de.wikipedia.org/wiki/'+s_name.title()
        content=get_content(url_name)
        logging.debug('URL gefunden:'+url_name)
        download_file(content)
    except Exception as error_message:
        logging.error('URL nicht gefunden.')
        print('Fehler aufgetreten: %s' % [error_message])
        continue

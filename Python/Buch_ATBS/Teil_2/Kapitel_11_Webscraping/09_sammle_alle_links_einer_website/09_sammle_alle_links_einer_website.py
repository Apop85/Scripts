# 09_sammle_alle_links_einer_website.py
# Dieses Script soll eine beliebige Webseite nach links in allen Unterseiten der Webseite suchen 
# und in einem File übersichtlich speichern
##### Probleme: Keine Ausgabe in Datei und url_name wird nach abschluss der durchsuchung eines 
##### links nicht zurückgesetzt. Anstatt blah.com/chapter1 --> blah.com/chapter2
##### entsteht blah.com/chapter1 --> blah.com/chapter1/chapter2
##### adressen in liste speichern und mit der variabel link_depth den richtigen unterordner finden?


import requests, bs4, os, logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
os.chdir(os.path.dirname(__file__))

save_file='.\\Websitelinks.txt'

if os.path.exists(save_file):
    logging.debug(save_file+' wird entfernt')
    os.remove(save_file)

def choose_website():
    global base_name
    print('Bitte zu durchsuchende URL angeben:')
    url_name=input()
    base_name=url_name.split('.')[1]
    link_depth=-1
    crawl_function(url_name, link_depth)

def crawl_function(url_name, link_depth):
    global url_list, save_file
    link_depth+=1
    url_content=check_url(url_name)

    if url_content == None:
        return
    bs4_container=bs4.BeautifulSoup(url_content.text, features="html.parser")
    for a_class in bs4_container.find_all('a', href=True):
        if 'http' not in a_class['href']:
            url_name+=a_class['href'][1:]
        if base_name not in url_name:
            continue
        save_file_write=open(save_file, 'a')
        if len(a_class) > 0:
            logging.info('URL Gefunden: '+a_class['href'])
            save_file_write.write(str('-'*link_depth)+a_class['href']+'\n')
            if url_name not in url_list:
                url_list+=[url_name]
                print(url_name)
                crawl_function(url_name, link_depth)
        else:
            link_depth=-1
        logging.debug('Keine weiterführenden Links gefunden.')
        save_file_write.close()    
    else:
        logging.debug('Keine weiterführenden Links gefunden.')


def check_url(url_name):
    global url_list
    if url_name[:3] == 'www':
        url_name='https://'+url_name
    url_list+=[url_name]
    try:
        url_content=requests.get(url_name)
        url_content.raise_for_status()
        return url_content
    except Exception:
        logging.error('URL '+url_name+' ist ungültig')
        print('URL ungültig.')


while True:
    url_list=[]
    save_file_write=open(save_file, 'w')
    save_file_write.close()
    choose_website()

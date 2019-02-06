# 09_sammle_alle_links_einer_website.py
# Dieses Script soll eine beliebige Webseite nach links in allen Unterseiten der Webseite suchen 
# und in einem File übersichtlich speichern

import requests, bs4, os, logging
logging.basicConfig(level=logging.ERROR, format='%(asctime)s - %(levelname)s - %(message)s')
os.chdir(os.path.dirname(__file__))

save_file='.\\Websitelinks.txt'

if os.path.exists(save_file):
    logging.debug(save_file+' wird entfernt')
    os.remove(save_file)

def choose_website():
    global base_name, base_url, save_file_write
    print('Bitte zu durchsuchende URL angeben:')
    url_name=input()
    base_name=url_name.split('.')[1]
    link_depth=0
    base_url=check_url(url_name)[1]
    save_file_write=open(save_file, 'w')
    save_file_write.write(base_url+'\n\n\n')
    save_file_write.close()
    crawl_function(url_name, link_depth)

def crawl_function(url_name, link_depth):
    global save_file, url_list
    link_depth+=1
    url_content, url_name=check_url(url_name)
    if url_content != None:
        bs4_container=bs4.BeautifulSoup(url_content.text, features='html.parser')
    else:
        return
    for css_a_class in bs4_container.find_all('a', href=True):
        if len(css_a_class) > 0:
            logging.info('URL Gefunden: '+css_a_class['href'])
            save_file_write=open(save_file, 'a')
            save_file_write.write(str('-'*link_depth)+str(css_a_class['href'])+'\n')
            save_file_write.close()
            if base_name in css_a_class['href'] or '/' in css_a_class['href'][0] and '#' not in css_a_class['href']:
                if css_a_class['href'][0] == '/' and '#' not in url_name and '.'+base_url.split('/')[-1] not in url_name[-5:]:
                    subfolder=css_a_class['href'].split('/')
                    for entry in subfolder:
                        if entry != '':
                            css_a_class['href']='/'+entry+'/'
                    if len(url_name.split('/')) > 4:
                        url_name='/'.join(url_name.split('/')[:-2])
                    url_name+=css_a_class['href']
                elif 'http' in css_a_class['href'] and len(url_name) < len(css_a_class['href']):
                    url_name=css_a_class['href']
                elif base_name in css_a_class['href']:
                    continue
                else:
                    url_name='/'.join(url_name.split('/')[:-1])+'/'
                    url_name+=css_a_class['href']
                if url_name not in url_list:
                    crawl_function(url_name, link_depth)
        else:
            return
    link_depth-=1

def check_url(url_name):
    global url_list
    if url_name[:3] == 'www':
        url_name='https://'+url_name
    url_list+=[url_name]
    try:
        url_content=requests.get(url_name)
        url_content.raise_for_status()
        return url_content, url_name
    except Exception:
        logging.error('URL '+url_name+' ist ungültig')
        print('URL ungültig.')
        return None, url_name

while True:
    url_list=[]
    choose_website()

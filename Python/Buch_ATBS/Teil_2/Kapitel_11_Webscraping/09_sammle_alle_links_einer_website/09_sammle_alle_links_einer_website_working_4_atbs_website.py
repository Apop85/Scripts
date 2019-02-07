# 09_sammle_alle_links_einer_website.py
# Dieses Script soll eine beliebige Webseite nach links in allen Unterseiten der Webseite suchen 
# und in einem File übersichtlich speichern

import requests, bs4, os, logging, re
logging.basicConfig(level=logging.ERROR, format='%(asctime)s - %(levelname)s - %(message)s')
os.chdir(os.path.dirname(__file__))

# Globals
site_links={}
site_counter={}
save_file=r'.\Websitelinks.txt'

def choose_website():
    global save_file, base_name, crawl_loop_number
    crawl_loop_number=-3
    print('Zu durchsuchende Webseite angeben:')
    website_choice=input()
    save_file_write=open(save_file, 'w')
    save_file_write.write(website_choice+'\n'+'~'*len(website_choice)+'\n\n')
    save_file_write.close()
    search_pattern=re.compile(r'(http://|https://)?(www\.)?([\w]+\.[\w]{2,3}.*)')
    base_name=search_pattern.findall(website_choice)
    base_name=list(base_name[0])
    for entry in range(len(base_name)):
        if entry == 0 and base_name[entry] == '':
            base_name[entry]='https://'
    base_name=''.join(base_name)
    website_choice=''.join(base_name)
    site_links.setdefault('base', base_name+'/')
    crawl_loop(website_choice)

    
def crawl_loop(url_name):
    global save_file, base_name, crawl_loop_number, site_links, site_counter
    crawl_loop_number+=3
    try:
        if len(site_links) > 1:
            del site_links[crawl_loop_number]
            breakpoint
    except Exception:
        None
    site_links.setdefault(crawl_loop_number, [])
    url_content=check_url(url_name)
    if url_content != None:
        bs4_container=bs4.BeautifulSoup(url_content.text, features='html.parser')
    else:
        return
    site_counter.setdefault(crawl_loop_number, 0)
    for class_a_object in bs4_container.find_all('a', href=True):
        if 'http' in class_a_object['href']:
            site_links[crawl_loop_number]+=[class_a_object['href'].strip('/')]
        else:
            site_links[crawl_loop_number]+=['/'+class_a_object['href'].strip('/')]
        site_counter[crawl_loop_number]+=1
    for entry in site_links[crawl_loop_number]:
        save_file_append=open(save_file, 'a')
        save_file_append.write('-'*crawl_loop_number+entry+'\n')
        save_file_append.close()
        if crawl_loop_number == 0:
            breakpoint
        if 'http' not in entry and 'www' not in entry and not '#toc' in entry and '/' != entry:
            if base_name in entry or '/' in entry or '#' in entry:
                if '#' in entry or '/' in entry:
                    if crawl_loop_number > 0:
                        new_url='/'.join(url_name.split('/')[:-1])+'/'+entry.strip('/')
                        logging.info('Neue URL gefunden:'+new_url)
                    else:
                        new_url=url_name+entry
                        logging.info('Neue URL gefunden:'+new_url)
                    if len(new_url) < len(base_name):
                        new_url=base_name+entry
                        breakpoint
        else:
            if base_name in entry:
                new_url=entry
            else:
                continue
            logging.info('Neue URL gefunden:'+new_url)
        check_value=check_if_ignored(new_url)
        if base_name in new_url and check_value == False:
            logging.info('Starte neuen Loop in der Tiefe: '+str(crawl_loop_number/3+1))
            crawl_loop(new_url)
            crawl_loop_number-=3
        elif base_name in new_url and crawl_loop_number == 0:
            logging.info('Starte neuen Loop in der Tiefe: '+str(crawl_loop_number/3+1))
            crawl_loop(new_url)
            crawl_loop_number-=3
        else:
            breakpoint
    del site_links[crawl_loop_number] 
    del site_counter[crawl_loop_number]

def check_url(url_name):
    url_content=requests.get(url_name)
    try:
        url_content.raise_for_status()
        return url_content
    except Exception:
        logging.error('URL Fehlerhaft: '+url_name)
        return None

def check_if_ignored(new_url):
    global site_links
    max_try=len(site_links)-1
    attempt=0
    fragment='/'+'/'.join(new_url.split('/')[-1:])
    for url_list in site_links.values():
        if max_try == attempt:
            return False
        elif new_url in url_list:
            return True
        elif fragment in url_list:
            return True
        else:
            attempt+=1
    return False

while True:
    choose_website()
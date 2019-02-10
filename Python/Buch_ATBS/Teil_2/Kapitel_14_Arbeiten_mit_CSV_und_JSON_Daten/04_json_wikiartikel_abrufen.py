# 04_json_wikieintrag_abrufen.py
# In dieser Übung geht es darum über die API von Wikipedia einen Artikel
# abzurufen und anschliessend auszugeben. Ziel ist es auch dass man das sowohl über ein
# kommandozeilenelement als auch über das Script selber machen kann. 

import json, sys, requests, re

max_text_length=70
max_text_delta=20

def output(title, string):
    print('╔'+''.center(max_text_length+8, '═')+'╗')
    print('║ '+title.center(max_text_length+7).upper()+'║')
    print('╠'+''.center(max_text_length+8, '═')+'╣')
    string=string+' '*max_text_length
    search_pattern=re.compile(r'\w+.{'+str(max_text_length-max_text_delta-7)+r','+str(max_text_length-7)+r'}[ |.|,|\n|>|\W]', re.DOTALL)
    results=search_pattern.findall(string)
    for line in results:
        print('║ '+line.strip()+'║'.rjust(max_text_length+8-len(line.strip())))
    print('╚'+''.center(max_text_length+8, '═')+'╝')
    input()

api_url='https://de.wikipedia.org/w/api.php?format=json&action=query&prop=extracts&exintro&explaintext&redirects=1&titles='
print('Welcher Wikipedia-Artikel soll angezeigt werden?')
wiki_search=input()
wiki_search='%20'.join(wiki_search.split(' '))

response=requests.get(api_url+wiki_search)
response.raise_for_status()
json_content=json.loads(response.text)
get_text_key=list(json_content['query']['pages'].keys())
wiki_text=json_content['query']['pages'][get_text_key[0]]['extract']
wiki_text=' '.join(wiki_text.split('\n'))
title=json_content['query']['redirects'][0]['from']

output(title, wiki_text)
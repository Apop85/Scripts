# Kontrabass
# Author: Apop85

import re

strophe='''Drei Chinesen mit dem Kontrabass 
sassen auf der Strasse und erzählten sich was. 
Da kam die Polizei, fragt: "Was ist denn das?" 
Drei Chinesen mit dem Kontrabass.'''

replace=['a','e','i','o','u','ä','ö','ü']
replace_with=['a','e','i','o','u','ä','ö','ü','au','ei','eu']

def choose_option():
  options=["(1) Replace with single vocal", "(2) Replace with diphtong", "(3) Exit"]
  for item in options:
    print(item)
  choose=int(input('Choose option: '))
  if choose != "" and 0 < choose <= len(options)-1:
    print('Choose letter from following list:')
    replace_list=[item for item in replace_with if len(item) == choose]
    for i in range(len(replace_list)):
      print(str(i+1)+'. '+replace_list[i])
    choose=int(input('Choice: '))
    if 0 < choose <= len(replace_list):
      replace_it(replace_list[choose-1])
  elif choose == 3:
    exit()
  else:
    print('Invalid option')

def replace_it(replace_with):
  print(replace_with)
  if len(replace_with) == 2:
    search_pattern=re.compile(r'([AEIOUaeiouäöüÄÖÜ]+)([h]?)')
  else:
    search_pattern=re.compile(r'([AEIOUÄÖÜaeiouäöü]+)')
  result=search_pattern.sub(replace_with,strophe)
  print(result)

choose_option()
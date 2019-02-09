# 10_kapitel_13_repetitionsfragen.py

import re

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

output('Frage 01', '')
output('Antwort', '')

output('Frage 02', '')
output('Antwort', '')

output('Frage 03', '')
output('Antwort', '')

output('Frage 04', '')
output('Antwort', '')

output('Frage 05', '')
output('Antwort', '')

output('Frage 06', '')
output('Antwort', '')

output('Frage 07', '')
output('Antwort', '')

output('Frage 08', '')
output('Antwort', '')

output('Frage 09', '')
output('Antwort', '')

output('Frage 10', '')
output('Antwort', '')

output('Frage 11', '')
output('Antwort', '')

output('Frage 12', '')
output('Antwort', '')

output('Frage 13', '')
output('Antwort', '')

output('Frage 14', '')
output('Antwort', '')

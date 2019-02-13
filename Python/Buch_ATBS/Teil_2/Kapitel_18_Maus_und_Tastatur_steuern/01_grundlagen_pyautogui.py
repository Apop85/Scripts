# 01_grundlagen_pyautogui.py

import re

max_text_length=70
max_text_delta=24

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

output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
output('', '')
# Dieses Script sucht mittels regex nach einem bestimmten Muster in einem String$
import re

checkme='''Michael: Hey Sarah, wie gehts? Ruf mich doch bei gelegeneheit mal an. Meine Nummer lautet (079)-123-4567. Grüsse.
Sarah: Hallo Michael. Deine angegebene Nummer scheint falsch zu sein. Ruf mich doch unter 123-456-7890 zurück. Danke.
Tino: Meine neue nummer lautet 345-243-2354.'''

regex_pattern1=re.compile(r'\W\d\d\d-\d\d\d-\d\d\d\d\W')
regex_pattern2=re.compile(r'\W\d\d\d\W-\d\d\d-\d\d\d\d')

# Regex Syntax: \d=(0-9) \D=(^0-9) \s=(\n\t\r\f\v) \S=(^\n\t\r\f\v)
# \w=(a-z,A-Z,0-9, ) \W=(^a-z,A-Z,0-9, )
# Regex testen: https://www.regexpal.com/

for i in range(len(checkme)-13):
    chunk=checkme[i:i+14]
    number=regex_pattern1.search(chunk)
    if number != None:
        print('Nummer erkannt:', number.group()[1:-1])
    number=regex_pattern2.search(chunk)
    if number != None:
        print('Nummer erkannt:', number.group())
        

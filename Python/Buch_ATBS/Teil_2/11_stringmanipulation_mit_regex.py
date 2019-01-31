# Hier geht es darum einen String mittels Regexs zu manipulieren
# Dafür gibt es speziell die Methode sub() für substitude also ersetzen
import re

string='Agent Alex war auf geheimer Mission mit Agentin Sandra'
pattern=r'Agent\w*\s(\w+)'
ex_it=re.compile(pattern)
print(string)
newstr=ex_it.sub('▓▓▓▓▓▓▓▓▓', string)
print(newstr)
input()

# Man kann einzelne Ergebnisse der Suche mit \1\2\3... wieder abrufen.
pattern=r'Agent\w*\s(\w)(\w)\w+'
ex_it=re.compile(pattern)
newstr=ex_it.sub(r'\1\2******', string)
print(newstr)




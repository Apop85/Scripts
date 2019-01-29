# Verwerten und Überschreiben von globalen und lokalen Variablen

def chgglob():
    global var           # Variable von Funktion an globale Variable übergeben
    var = 'Overwritten'

def locvar():
    var = 'Wrong method' # Lokale, nur für diese Funktion gültige Variable

var = 'Overwrite me'
print(var)
chgglob()
locvar()
print(var)

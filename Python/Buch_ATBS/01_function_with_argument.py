# Einleitung mit Funktion und Argument

def test(name, age):
    print('Hallo', name + '. Bist du wirklich schon', str(age), 'Jahre alt?')

print('Wie ist dein Name?')
inp1=input()
print('Wie alt bist du?')
inp2=input()
test(inp1, inp2)

from time import sleep

def loading():
    # Setze globale Variablen
    global current_direction
    global current_position
    # Defniere Aussehen des Ladebalkens
    loading_bar = "[                                ]"
    # Definiere das zu bewegende Objekt
    loading_symbol_fw = ">++('>"
    loading_symbol_bw = "<')++<"

    # Ist die Variabel current_direction noch nicht gesetzt wird ein Error ausgelöst
    try:
        test = current_direction*5
    except:
        # Der Error wird abgefangen und die Variablen gesetzt
        current_direction = 0
        current_position = 1

    # Ist die Bewegung vorwärts erhöhe Position um 1
    if current_direction == 0:
        current_position += 1
        symbol = loading_symbol_fw
    # Ist die Bewegung rückwärts vermindere Position um 1
    else:
        current_position -= 1
        symbol = loading_symbol_bw

    # Prüfe ob der Rand des Ladebalkens erreicht wurde und wechsle Laufrichtung
    if current_position >= len(loading_bar)-1:
        current_direction = 1
    elif current_position == 1:
        current_direction = 0

    # Gebe Ladebalken aus
    print("\r"*(len(loading_bar)+len(symbol)) + loading_bar[0:current_position] + symbol + loading_bar[current_position:], end="")

for i in range(0, 1000):
    loading()
    sleep(0.3)
print()
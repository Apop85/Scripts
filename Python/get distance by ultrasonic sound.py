#!/usr/bin/python3

import math

# Konstanten
GASKONSTANTE = 8.3145
DIFF_KELVIN = 273.15
MOLAREMASSE = 0.02896
AD_EXPO = 1.402
MAXDELTA = 0.375

# Variablen
dauer = 0.2            # in ms
temp = 20              # in °C
feuchtigkeit = 50      # in %

# Berechnete Variablen
temp_abs = temp + DIFF_KELVIN
abs_delta = 1 + ( ( MAXDELTA * ( feuchtigkeit / 100 ) ) / 100 )
    
# Schallgeschwindigkeit und Distanzberechnung
cluft = abs_delta*(math.sqrt(AD_EXPO*((GASKONSTANTE*temp_abs)/MOLAREMASSE)))
distanz = ( cluft * dauer ) / 2
distanz = round(distanz, 3)

print(distanz, "cm")

input("Press Enter to continue ...")

# EOF

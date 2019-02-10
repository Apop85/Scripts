# 05_stoppuhr.py
# Wie der Dateiname schon sagt handelt es sich hier um ein Script welches 
# die Funktion einer Stoppuhr übernimmt

import time

print('Enter um Runde zu starten')
total_time, lap, last_time=0, 0, 0
while True:
    input()
    if last_time != 0:
        lap_duration=round(time.time()-last_time,2)
        total_time+=time.time()-last_time
        print(('Lap: '+str(lap)).rjust(15)+'|'.center(5)+(str(lap_duration)+' Sek').center(15)+'| Total:'.center(9)+(str(round(total_time,2))+' Sek').ljust(10))
    last_time=time.time()
    lap+=1
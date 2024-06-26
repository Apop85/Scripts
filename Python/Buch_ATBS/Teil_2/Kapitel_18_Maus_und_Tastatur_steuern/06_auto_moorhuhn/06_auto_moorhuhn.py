# 06_auto_moorhuhn.py
# In dieser Übung geht es darum das Spiel Moorhuhn zu automatisieren.
# Um das Script abzubrechen die Maus nach oben links ziehen.

import pyautogui, time, webbrowser, threading, logging
from PIL import Image

logging.basicConfig(level=logging.INFO, format=('%(asctime)s - %(levelname)s - %(message)s'))

game_url='http://www.jetztspielen.de/spiel/moorhuhn-shooter'
webbrowser.open(game_url)
time.sleep(6)
pyautogui.click(1238,1367)
time.sleep(1)
pyautogui.click(640,1479)
time.sleep(0.5)
# max height=1350 min height=410 
first_colors={}
shoot_amount=0
shooting_jobs=[]
threads=[]

def provide_screenshots():
    global scr_shot
    scr_shot=pyautogui.screenshot()
    time.sleep(0.5)
thread_object=threading.Thread(target=provide_screenshots, name='Screenshots_Thread')
thread_object.start()
threads.append(thread_object)

def shoot_em_thread(x,y):
    global first_colors, shooting_jobs, scr_shot, shoot_amount
    # scr_shot=pyautogui.screenshot()
    first_color=scr_shot.getpixel((x,y))
    first_colors.setdefault(y,first_color)
    if y >= 1385:
        pyautogui.click((x,y))
        logging.info('Es kann losgehen')
    while True:
        scr_shot=pyautogui.screenshot()
        color=scr_shot.getpixel((x,y))
        if color != first_colors[y]:
            delta_r=color[0]-first_colors[y][0]
            if delta_r < 0:
                delta_r*=-1
            delta_g=color[1]-first_colors[y][1]
            if delta_g < 0:
                delta_g*=-1
            delta_b=color[2]-first_colors[y][2]
            if delta_b < 0:
                delta_b*=-1
            if delta_r + delta_g + delta_b > 150:
                logging.debug('Delta: '+str(delta_r)+' - '+str(delta_g)+' - '+str(delta_b)+' SUMME='+str(delta_b+delta_g+delta_r))
                # shooting_jobs.setdefault(y,[])
                # shooting_jobs+=[(x,y)]
                pyautogui.click((x,y))
                shoot_amount+=1
                # time.sleep(1)
        if list(pyautogui.position())[0] == 0 and list(pyautogui.position())[1] == 0:
            logging.debug('Thread '+str(y)+' geschlossen')
            break

# def shoot_job_thread():
#     global shooting_jobs, shoot_amount
#     logging.info('Ready to shoot.')
#     while True:
#         ############
#         # for key in shooting_jobs:
#         #     for i in range(len(shooting_jobs[key])):
#         #         pyautogui.click(shooting_jobs[key][i])
#         #         logging.info('Moorhuhn entdeckt! '+str(shooting_jobs[key][i]))
#         #         shoot_amount+=1
#         #     del shooting_jobs[key]
#         ################
#         if len(shooting_jobs) > 0:
#             pyautogui.click(shooting_jobs[0])
#             logging.info('Moorhuhn entdeckt! '+str(shooting_jobs[0]))
#             shoot_amount+=1
#             del shooting_jobs[0]
#         if list(pyautogui.position())[0] == 0 and list(pyautogui.position())[1] == 0:
#             logging.debug('Thread shoot_job geschlossen')
#             break
# thread_object=threading.Thread(target=shoot_job_thread, name='Shoot_Thread')
# thread_object.start()
# threads.append(thread_object)

def auto_reload_thread():
    global shoot_amount
    while True:
        if shoot_amount > 8:
            pyautogui.press('space')
            logging.info('Magazin nachgeladen.')
            shoot_amount=0
        if list(pyautogui.position())[0] == 0 and list(pyautogui.position())[1] == 0:
            logging.debug('Thread auto_reload geschlossen')
            break
thread_object=threading.Thread(target=auto_reload_thread, name='Reload_Thread')
thread_object.start()
threads.append(thread_object)

pyautogui.click(0,864)
for i in range(410,1410,40):
    thread_object=threading.Thread(target=shoot_em_thread, args=(1115,i), name='Shoot_em_'+str(i))
    thread_object.start()
    threads.append(thread_object)

for thread in threads:
    thread.join()
print('Alle Threads geschlossen.')

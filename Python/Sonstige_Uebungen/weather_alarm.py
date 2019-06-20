#!/usr/bin/env python3
# -*- coding:utf-8 -*-
#File: c:\Users\Apop85\Desktop\ScriptProjekte\GitHub\Python\Sonstige_Uebungen\weather_alarm.py
#Project: c:\Users\Apop85\Desktop\ScriptProjekte\GitHub\Python\Sonstige_Uebungen
#Created Date: Wednesday 19.06.2019, 20:54
#Author: Apop85
#-----
#Last Modified: Wednesday 19.06.2019, 20:54
#-----
#Copyright (c) 2019 Apop85
#This software is published under the MIT license.
#Check http://www.opensource.org/licenses/MIT for further informations
#-----
#Description: Check weather and send report by telegram API

import os, re, requests

def init_telegram_vars(message):
    # Readout telegram.inf for bot token and chat id
    os.chdir(os.path.dirname(__file__))
    telegram_informations = open(r'./telegram.inf', 'r', encoding='UTF-8').read()

    chat_id_pattern = re.compile(r'CHAT_ID=\'(\d+)\'')
    bot_id_pattern = re.compile(r'BOT_ID=\'(.*)\'')
    chat_id = chat_id_pattern.findall(telegram_informations)[0]
    bot_id = bot_id_pattern.findall(telegram_informations)[0]

    send_telegram_message(bot_id, chat_id, message)


def send_telegram_message(bot_id, chat_id, message):
    # Send telegram message
    telegram_url = 'https://api.telegram.org/bot'+bot_id+'/sendMessage?chat_id='+chat_id+'&text='+message
    requests.get(telegram_url)


def start_collecting_data():
    locations = {'city': 'city_id'}
    keys=locations.keys()
    weather_information={}
    # Check different locations and save in dictionary
    for location in keys:
        id_code = locations[location]
        weather_information = get_weather_by_location(location, id_code, weather_information)
    analyze_data(weather_information)

def get_weather_by_location(location, location_id, weather_information):
    # Download weather informations
    tmp_file = r'./weather.tmp'
    url= 'https://ch.wetter.com/schweiz/'+location+'/'+location_id+'.html'
    url_content = requests.get(url)
    tmp_file_write = open(tmp_file, 'wb')
    for chunk in url_content.iter_content(10**5):
        tmp_file_write.write(chunk)
    tmp_file_write.close()

    # Regex tempfile
    degrees, percent_rain, order, wind = read_data(tmp_file)

    # Setup dictionary
    weather_information.setdefault(location, {})
    for i in range(len(degrees)):
        weather_information[location].setdefault(order[i], {'temp' : int(degrees[i][:-1]), 'rain' : int(percent_rain[i][0]), 'rain_amount' : float('.'.join(percent_rain[i][1].split(','))), 'wind' : int(wind[i])}) 
    return weather_information
    
def read_data(tmp_file):
    # Read out tmpfile and delete it
    tmp_file_content = open(tmp_file, 'r', encoding='UTF-8').read()

    # Search for main part 
    big_chunk_pattern=re.compile(r'<table class="weather-overview mb mt">(.*?)<h2 class="gamma desk-pr">', re.DOTALL)
    big_chunk_result = big_chunk_pattern.findall(tmp_file_content)[0]
    big_chunk_result = ''.join(big_chunk_result.split('\n'))

    # Check order of information
    order_pattern = re.compile(r'(Morgens|Mittags|Abends|Nachts)')
    order=order_pattern.findall(big_chunk_result)
    
    # Search temperature information
    degrees_pattern = re.compile(r'>(-?\d{1,2}°)<')
    degrees = degrees_pattern.findall(big_chunk_result)

    # Search rain possibility
    percent_rain_pattern = re.compile(r'>(\d{1,2})&.*?;%<.*?\d{1,2},\d&.*?;l')
    percent_rain_str = percent_rain_pattern.findall(big_chunk_result)

    amount_rain_pattern = re.compile(r'>\d{1,2}&.*?;%<.*?(\d{1,2},\d)&.*?;l')
    amount_rain = amount_rain_pattern.findall(big_chunk_result)
    
    percent_rain = [(i, j) for i in percent_rain_str for j in amount_rain]


    # Search wind strength
    wind_pattern = re.compile(r'>(\d{1,3}).{7}km/h')
    wind_result = wind_pattern.findall(big_chunk_result)

    if os.path.exists(tmp_file): 
        os.remove(tmp_file)

    return degrees, percent_rain, order, wind_result

def analyze_data(weather):
    for location in weather.keys():
        timestamp=[]
        rain, rain_amount, wind, time, temp = False, False, False, False, False
        for daytime in weather[location].keys():
            # Check if it's during daytime
            if daytime in ['Morgens','Mittags','Abends']:
                time = True
            else:
                time = False
            for stat in weather[location][daytime].keys():
                # Set flags for messaging
                if stat == 'rain' and weather[location][daytime][stat] > 70 and time:
                    rain = True
                if stat == 'rain_amount' and weather[location][daytime][stat] > 10.0 and time:
                    rain_amount = True
                if stat == 'wind' and weather[location][daytime][stat] > 50 and time:
                    wind = True
                if stat == 'temp' and weather[location][daytime][stat] >= 30 and time:
                    temp = True
            if (rain or rain_amount or wind or temp) and time:
                timestamp += [daytime]
        
        # Create message and send if one or more flags where true
        message = None
        if rain and rain_amount and wind:
            message = 'Starkregen und Sturm'
        elif rain and rain_amount and temp:
            message = 'Heiss und Starkregen'
        elif rain and rain_amount:
            message = 'Starkregen'
        elif rain and wind:
            message = 'Stürmischer Regen'
        elif wind and temp:
            message = 'Heiss und Stürmisch'
        elif rain and temp:
            message = 'Heiss und Regnerisch'
        elif wind:
            message = 'Stürmisch'
        elif rain:
            message = 'Regnerisch'
        elif rain:
            message = 'Hitzetag'

        if message:
            message = location.upper()+' - '+' | '.join(timestamp)+"'%0A'╔════════════════════╗'%0A'║"+message.center(31, '_')+"║'%0A'╚════════════════════╝"
            init_telegram_vars(message)

start_collecting_data()


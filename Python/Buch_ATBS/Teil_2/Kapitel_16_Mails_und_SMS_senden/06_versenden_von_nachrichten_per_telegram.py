# 06_versenden_von_nachrichten_per_telegram.py

import requests

print('Chat-ID eingeben:')
chat_id=input()
print('Bot-Token eingeben:')
bot_token=input()
print('Zu sendende Nachricht eingeben:')
message=input()

telegram_api='https://api.telegram.org/bot'+bot_token+'/sendMessage?chat_id='+chat_id+'&text='+message
requests.get(telegram_api)

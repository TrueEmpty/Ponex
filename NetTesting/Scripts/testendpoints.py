import requests

base_url = 'https://trueempty-mdb.raymondmcleod1.repl.co'
chat_room_route = lambda player_id: base_url + f'/chat/room/{player_id}'
chat_message_route = lambda room_id, player_id: base_url + f'/chat/messages/{room_id}/{player_id}'
create_account_url = base_url + '/create_account'
chat_messages_route = lambda room_id: base_url + f'/chat/messages/{room_id}'

def create_account():
    response = requests.post(url=create_account_url, data={
        'username': 'name',
        'password': 'pass',
    })
    print(response)

def create_room():
    response = requests.post(chat_room_route('1234'))
    print(response)

def send_message():
    room_id = 'wz1Hi2'
    response = requests.post(chat_message_route(room_id,'1234'), data={
        'message': 'hi my name is dave'
    })
    print(response.text)

def get_room():
    room_id = 'wz1Hi2'
    response = requests.get(chat_messages_route(room_id=room_id))
    print(response)
#create_account()
#create_room()
#send_message()
#get_room()


import requests
import asyncio
url = "https://trueempty-mdb.raymondmcleod1.repl.co/chat/messages/add/0000000001/MQqZ/admin1/penis"

async def hi():
    for i in range(1000):
        response = requests.post(url)
        print(response)

asyncio.run(hi())
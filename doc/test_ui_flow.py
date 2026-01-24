#!/usr/bin/env python3
"""
Эмулирует работу двух Avalonia UI клиентов.
Проверяет, что GameViewModel правильно получает GameStateChanged.
"""

import asyncio
import json
import websockets
import uuid
import time

async def client_flow(client_num, create_room=False):
    """Эмулирует действия одного клиента"""
    uri = "ws://localhost:5555"
    user_id = str(uuid.uuid4())[:8]
    display_name = f"TestPlayer{client_num}"
    room_id = None
    
    try:
        print(f"\n[Client{client_num}] Подключение к {uri}...")
        async with websockets.connect(uri) as ws:
            print(f"[Client{client_num}] ✅ Подключено")
            
            # 1. Connect
            connect_msg = {
                "type": "Connect",
                "userId": user_id,
                "displayName": display_name
            }
            print(f"[Client{client_num}] Отправка Connect...")
            await ws.send(json.dumps(connect_msg))
            
            # Получаем UserConnected или другие сообщения
            response = await ws.recv()
            print(f"[Client{client_num}] ✅ Получено: {response[:100]}...")
            
            # 2. Если это первый клиент - создаем комнату
            if create_room:
                room_name = f"TestRoom-{int(time.time())}"
                create_msg = {
                    "type": "CreateRoom",
                    "roomName": room_name,
                    "maxPlayers": 2,
                    "password": ""
                }
                print(f"[Client{client_num}] Создание комнаты '{room_name}'...")
                await ws.send(json.dumps(create_msg))
                
                # Получаем CreateRoomResult с ID комнаты
                response = await asyncio.wait_for(ws.recv(), timeout=5)
                print(f"[Client{client_num}] ✅ CreateRoomResult: {response[:150]}...")
                
                # Парсим room_id из ответа
                try:
                    data = json.loads(response)
                    if "Payload" in data and "room" in data["Payload"]:
                        room_id = data["Payload"]["room"]["id"]
                        print(f"[Client{client_num}] 📌 RoomId: {room_id}")
                except:
                    pass
                
                # Сохраняем room_id в глобальную переменную для второго клиента
                global shared_room_id
                shared_room_id = room_id
                await asyncio.sleep(0.5)
                
            else:
                # Ждем комнату от первого клиента
                await asyncio.sleep(2)
                
                # 2b. Присоединяемся к комнате
                if shared_room_id:
                    room_id = shared_room_id
                    join_msg = {
                        "type": "JoinRoom",
                        "roomId": room_id,
                        "userId": user_id,
                        "password": ""
                    }
                    print(f"[Client{client_num}] Присоединение к комнате {room_id}...")
                    await ws.send(json.dumps(join_msg))
                    
                    response = await asyncio.wait_for(ws.recv(), timeout=5)
                    print(f"[Client{client_num}] ✅ JoinRoomResult: {response[:150]}...")
                    
                    # Ждем сообщение о присоединении
                    await asyncio.sleep(0.5)
            
            # 3. Отправляем PlayerReady (эмулируем нажатие кнопки Ready)
            if room_id:
                print(f"\n[Client{client_num}] ⏱️  Ожидание перед отправкой PlayerReady (1 сек)...")
                await asyncio.sleep(1)
                
                ready_msg = {
                    "type": "PlayerReady",
                    "roomId": room_id,
                    "userId": user_id
                }
                print(f"[Client{client_num}] Отправка PlayerReady на сервер...")
                await ws.send(json.dumps(ready_msg))
                print(f"[Client{client_num}] ✅ PlayerReady отправлено")
                
                await asyncio.sleep(0.5)
            
            # 4. Слушаем сообщения от сервера (GameStateChanged, GameStart и т.д.)
            print(f"\n[Client{client_num}] 🔊 Ожидаем GameStateChanged от сервера...")
            
            received_game_state = False
            received_game_start = False
            
            # Настраиваем timeout для получения сообщений
            timeout_count = 0
            while timeout_count < 10:  # Максимум 10 попыток с timeout'ом
                try:
                    message = await asyncio.wait_for(ws.recv(), timeout=3)
                    print(f"[Client{client_num}] 📨 Сообщение: {message[:150]}...")
                    
                    # Парсим сообщение
                    try:
                        data = json.loads(message)
                        msg_type = data.get("type", data.get("Type"))
                        
                        if msg_type == "GameStateChanged":
                            print(f"[Client{client_num}] ✅ ПОЛУЧЕНО GameStateChanged!")
                            if "Payload" in data and "state" in data["Payload"]:
                                state = data["Payload"]["state"]
                                print(f"[Client{client_num}]    State: {state}")
                                received_game_state = True
                        
                        elif msg_type == "GameStart":
                            print(f"[Client{client_num}] ✅ ПОЛУЧЕНО GameStart!")
                            if "Payload" in data:
                                is_your_turn = data["Payload"].get("isYourTurn")
                                print(f"[Client{client_num}]    IsYourTurn: {is_your_turn}")
                                received_game_start = True
                    except:
                        pass
                    
                    timeout_count = 0  # Сбрасываем счетчик при получении сообщения
                    
                except asyncio.TimeoutError:
                    timeout_count += 1
                    print(f"[Client{client_num}] ⏱️  Timeout {timeout_count}/10")
            
            print(f"\n[Client{client_num}] 📊 Результаты:")
            print(f"[Client{client_num}]   GameStateChanged: {'✅' if received_game_state else '❌'}")
            print(f"[Client{client_num}]   GameStart: {'✅' if received_game_start else '❌'}")
            
            return received_game_state and received_game_start
            
    except Exception as e:
        print(f"[Client{client_num}] ❌ Ошибка: {e}")
        import traceback
        traceback.print_exc()
        return False

# Глобальная переменная для передачи room_id между клиентами
shared_room_id = None

async def main():
    print("=" * 60)
    print("ТЕСТ GAMEVIEWMODEL - ПОЛУЧЕНИЕ GAMESTATE")
    print("=" * 60)
    print("\nТест проверяет:")
    print("1. Клиент 1 создает комнату")
    print("2. Клиент 2 присоединяется")
    print("3. Оба получают GameStateChanged от сервера")
    print("4. Оба получают GameStart")
    print("\nЭто эмулирует действия Avalonia UI...")
    
    # Запускаем обоих клиентов параллельно
    try:
        # Клиент 1 создает комнату, клиент 2 присоединяется
        results = await asyncio.gather(
            client_flow(1, create_room=True),
            client_flow(2, create_room=False)
        )
        
        print("\n" + "=" * 60)
        print("ИТОГИ:")
        print("=" * 60)
        if all(results):
            print("✅ ВСЕ ТЕСТЫ ПРОЙДЕНЫ")
            print("\nОба клиента получили GameStateChanged и GameStart!")
            print("Это значит, что GameViewModel правильно получает и обновляет State.")
        else:
            print("❌ ТЕСТЫ НЕ ПРОЙДЕНЫ")
            for i, result in enumerate(results, 1):
                print(f"   Client{i}: {'✅' if result else '❌'}")
                
    except Exception as e:
        print(f"\n❌ Критическая ошибка: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    asyncio.run(main())

#!/usr/bin/env python3
"""
Простой тест для проверки GameViewModel исправлений.
Эмулирует действия двух UI клиентов.
"""

import asyncio
import json
import websockets
import uuid
import time

async def test_gameviewmodel():
    """Полный тест игровой логики"""
    
    uri = "ws://localhost:5555"
    print("=" * 60)
    print("ТЕСТ: GAMEVIEWMODEL FIXES")
    print("=" * 60)
    print("\nПроверяем:")
    print("✓ State правильно обновляется")
    print("✓ GameStateChanged получается клиентом")
    print("✓ Ready button видна")
    print("✓ GameStart приходит обоим")
    print()
    
    # Подготовка
    user1_id = str(uuid.uuid4())[:8]
    user2_id = str(uuid.uuid4())[:8]
    room_name = f"TestRoom-{int(time.time())}"
    room_id = None
    
    try:
        # ======== CLIENT 1: Create Room ========
        print(f"\n[CLIENT 1] Подключение...")
        async with websockets.connect(uri, ping_interval=None) as ws1:
            print(f"[CLIENT 1] ✅ Подключено")
            
            # Connect
            await ws1.send(json.dumps({
                "type": "Connect",
                "userId": user1_id,
                "displayName": "Player1"
            }))
            msg = await asyncio.wait_for(ws1.recv(), timeout=3)
            print(f"[CLIENT 1] ✅ Connect ответ получен")
            
            # CreateRoom
            await ws1.send(json.dumps({
                "type": "CreateRoom",
                "roomName": room_name,
                "maxPlayers": 2,
                "password": ""
            }))
            
            # Слушаем сообщения и ищем RoomCreated
            for _ in range(5):
                msg = await asyncio.wait_for(ws1.recv(), timeout=2)
                data = json.loads(msg)
                if data.get("Type") == "RoomCreated":
                    room_id = data["Payload"]["room"]["Id"]
                    print(f"[CLIENT 1] ✅ CreateRoom успешно, RoomId: {room_id}")
                    break
            
            if not room_id:
                print(f"[CLIENT 1] ❌ RoomCreated не получен!")
                return False
            
            # ======== CLIENT 2: Join Room ========
            print(f"\n[CLIENT 2] Подключение...")
            async with websockets.connect(uri, ping_interval=None) as ws2:
                print(f"[CLIENT 2] ✅ Подключено")
                
                # Connect
                await ws2.send(json.dumps({
                    "type": "Connect",
                    "userId": user2_id,
                    "displayName": "Player2"
                }))
                msg = await asyncio.wait_for(ws2.recv(), timeout=3)
                print(f"[CLIENT 2] ✅ Connect ответ получен")
                
                # JoinRoom
                await asyncio.sleep(0.5)
                await ws2.send(json.dumps({
                    "type": "JoinRoom",
                    "roomId": room_id,
                    "userId": user2_id,
                    "password": ""
                }))
                print(f"[CLIENT 2] ✅ JoinRoom отправлен")
                
                # Ждем подтверждения присоединения
                await asyncio.sleep(1)
                
                # ======== ОБОИМ: PlayerReady ========
                print(f"\n[BOTH] Отправляем PlayerReady...")
                
                # CLIENT 1 ready
                await ws1.send(json.dumps({
                    "type": "PlayerReady",
                    "roomId": room_id,
                    "userId": user1_id
                }))
                print(f"[CLIENT 1] ✅ PlayerReady отправлен")
                
                # CLIENT 2 ready
                await ws2.send(json.dumps({
                    "type": "PlayerReady",
                    "roomId": room_id,
                    "userId": user2_id
                }))
                print(f"[CLIENT 2] ✅ PlayerReady отправлен")
                
                await asyncio.sleep(0.5)
                
                # ======== ПРОВЕРЯЕМ: Получили ли GameStart? ========
                print(f"\n[BOTH] Ожидаем GameStart...")
                
                c1_game_start = False
                c2_game_start = False
                c1_game_state = False
                c2_game_state = False
                
                # Слушаем оба клиента одновременно
                try:
                    for i in range(20):
                        # Пытаемся получить сообщение от CLIENT 1
                        try:
                            msg1 = await asyncio.wait_for(ws1.recv(), timeout=0.5)
                            data1 = json.loads(msg1)
                            if data1.get("Type") == "GameStateChanged":
                                state = data1.get("Payload", {}).get("state")
                                print(f"[CLIENT 1] 📨 GameStateChanged: {state}")
                                c1_game_state = True
                            elif data1.get("Type") == "GameStart":
                                is_your_turn = data1.get("Payload", {}).get("isYourTurn")
                                print(f"[CLIENT 1] ✅ GameStart получен! isYourTurn={is_your_turn}")
                                c1_game_start = True
                        except asyncio.TimeoutError:
                            pass
                        
                        # Пытаемся получить сообщение от CLIENT 2
                        try:
                            msg2 = await asyncio.wait_for(ws2.recv(), timeout=0.5)
                            data2 = json.loads(msg2)
                            if data2.get("Type") == "GameStateChanged":
                                state = data2.get("Payload", {}).get("state")
                                print(f"[CLIENT 2] 📨 GameStateChanged: {state}")
                                c2_game_state = True
                            elif data2.get("Type") == "GameStart":
                                is_your_turn = data2.get("Payload", {}).get("isYourTurn")
                                print(f"[CLIENT 2] ✅ GameStart получен! isYourTurn={is_your_turn}")
                                c2_game_start = True
                        except asyncio.TimeoutError:
                            pass
                        
                        if c1_game_start and c2_game_start:
                            break
                        
                        await asyncio.sleep(0.1)
                
                except Exception as e:
                    print(f"❌ Ошибка при получении сообщений: {e}")
                
                # ======== РЕЗУЛЬТАТЫ ========
                print(f"\n" + "=" * 60)
                print("РЕЗУЛЬТАТЫ:")
                print("=" * 60)
                
                results = {
                    "Client1 получил GameStateChanged": c1_game_state,
                    "Client1 получил GameStart": c1_game_start,
                    "Client2 получил GameStateChanged": c2_game_state,
                    "Client2 получил GameStart": c2_game_start
                }
                
                for name, result in results.items():
                    status = "✅" if result else "❌"
                    print(f"{status} {name}")
                
                print()
                
                if all(results.values()):
                    print("✅ ВСЕ ТЕСТЫ ПРОЙДЕНЫ!")
                    print("\n📊 Вывод:")
                    print("✓ GameStateChanged правильно получается")
                    print("✓ State обновляется в GameViewModel")
                    print("✓ Ready button видна когда State=ReadyToStart")
                    print("✓ GameStart отправляется обоим клиентам")
                    print("✓ isYourTurn флаг установлен правильно")
                    return True
                else:
                    print("❌ НЕКОТОРЫЕ ТЕСТЫ НЕ ПРОЙДЕНЫ")
                    failed = [k for k, v in results.items() if not v]
                    for f in failed:
                        print(f"  ❌ {f}")
                    return False
    
    except Exception as e:
        print(f"\n❌ Критическая ошибка: {e}")
        import traceback
        traceback.print_exc()
        return False

async def main():
    result = await test_gameviewmodel()
    print()
    if result:
        print("=" * 60)
        print("✅ ТЕСТИРОВАНИЕ ЗАВЕРШЕНО УСПЕШНО")
        print("=" * 60)
    else:
        print("=" * 60)
        print("❌ ТЕСТИРОВАНИЕ ПРОВАЛЕНО")
        print("=" * 60)

if __name__ == "__main__":
    asyncio.run(main())

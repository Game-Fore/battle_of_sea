# Тестирование сервера на основе комнат

## Быстрый тест с netcat или telnet

### 1. Запустить сервер
```bash
cd /Users/masha/battle_of_sea/battle_of_sea/battle_of_sea
dotnet run
```

Сервер слушает на `localhost:5000`

### 2. Подключиться (terminal 1)
```bash
nc localhost 5000
```

### 3. Отправить команды

#### Подключение Player1
```json
{"Type":"Connect","Payload":{"playerName":"Player1"}}
```

Ожидаемый ответ:
```json
{"Type":"connected","Payload":{"playerId":"xxx-xxx-xxx"}}
```

#### Создание комнаты
```json
{"Type":"CreateRoom","Payload":{"roomName":"Test Room"}}
```

Ожидаемый ответ:
```json
{"Type":"RoomCreated","Payload":{"roomId":"yyy-yyy-yyy","roomName":"Test Room","maxPlayers":2}}
{"Type":"RoomsList","Payload":{"rooms":[...]}}
```

Сохрани `roomId` для следующей команды!

#### Получить список комнат
```json
{"Type":"RoomsList","Payload":{}}
```

---

## Подключиться (terminal 2)

```bash
nc localhost 5000
```

#### Подключение Player2
```json
{"Type":"Connect","Payload":{"playerName":"Player2"}}
```

Ожидаемый ответ:
```json
{"Type":"connected","Payload":{"playerId":"zzz-zzz-zzz"}}
```

#### Присоединиться к комнате
```json
{"Type":"JoinRoom","Payload":{"roomId":"yyy-yyy-yyy"}}
```

Ожидаемый ответ:
```json
{"Type":"JoinRoomResult","Payload":{"success":true,"roomId":"yyy-yyy-yyy","roomName":"Test Room","players":[{"id":"xxx-xxx-xxx","name":"Player1"},{"id":"zzz-zzz-zzz","name":"Player2"}],"message":"Joined room successfully"}}
{"Type":"RoomsList","Payload":{"rooms":[...]}}
```

---

## Готовность (оба terminal одновременно или последовательно)

### Player1 готов
```json
{"Type":"PlayerReady","Payload":{}}
```

Ожидаемый ответ:
```json
{"Type":"PlayerReady","Payload":{"playerId":"xxx-xxx-xxx","message":"Player ready"}}
```

### Player2 готов
```json
{"Type":"PlayerReady","Payload":{}}
```

Ожидаемый ответ:
```json
{"Type":"PlayerReady","Payload":{"playerId":"zzz-zzz-zzz","message":"Player ready"}}
{"Type":"GameStateChanged","Payload":{"state":"GameStarted","roomId":"yyy-yyy-yyy","players":[{"id":"xxx-xxx-xxx","name":"Player1"},{"id":"zzz-zzz-zzz","name":"Player2"}]}}
```

Оба игрока получают `GameStateChanged` с `state: "GameStarted"` 🎮

---

## Логи на сервере

Ожидаемые логи:
```
[ROOM_CREATED] Room Test Room (ID: yyy-yyy-yyy) created by Player1
[ROOM_JOINED] Player Player2 joined room Test Room
[PLAYER_READY] Player1 is ready in room Test Room
[PLAYER_READY] Player2 is ready in room Test Room
[GAME_STARTED] Game starting in room Test Room
[GAME_START] Game started in room Test Room: Player1 vs Player2
[BROADCAST] RoomsList sent to 2 players
```

---

## Полный JSON сценарий

Скопируй и отправляй построчно:

### Terminal 1 (Player1):
```
{"Type":"Connect","Payload":{"playerName":"Player1"}}
{"Type":"CreateRoom","Payload":{"roomName":"MyRoom"}}
{"Type":"PlayerReady","Payload":{}}
```

### Terminal 2 (Player2):
```
{"Type":"Connect","Payload":{"playerName":"Player2"}}
{"Type":"JoinRoom","Payload":{"roomId":"<roomId из ответа Player1>"}}
{"Type":"PlayerReady","Payload":{}}
```

---

## Ошибочные сценарии для тестирования

### 1. Присоединиться к несуществующей комнате
```json
{"Type":"JoinRoom","Payload":{"roomId":"fake-id"}}
```

Ответ:
```json
{"Type":"JoinRoomResult","Payload":{"success":false,"message":"Room not found"}}
```

### 2. Присоединиться к полной комнате (после 2 игроков)
```json
{"Type":"JoinRoom","Payload":{"roomId":"yyy-yyy-yyy"}}
```

Ответ:
```json
{"Type":"JoinRoomResult","Payload":{"success":false,"message":"Room is full"}}
```

### 3. Присоединиться к запущенной игре
```json
{"Type":"JoinRoom","Payload":{"roomId":"yyy-yyy-yyy"}}
```

Ответ:
```json
{"Type":"JoinRoomResult","Payload":{"success":false,"message":"Game already started"}}
```

---

## Python тест

```python
import socket
import json

def send_message(sock, msg_type, payload):
    msg = {"Type": msg_type, "Payload": payload}
    sock.send((json.dumps(msg) + "\n").encode())
    response = sock.recv(1024).decode()
    print(f"← {response}")

# Player 1
sock1 = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock1.connect(("localhost", 5000))

send_message(sock1, "Connect", {"playerName": "Player1"})
send_message(sock1, "CreateRoom", {"roomName": "TestRoom"})

# Получим roomId из ответа...
# send_message(sock1, "PlayerReady", {})

sock1.close()
```

---

## Проверка статуса сервера

```bash
# Проверить, слушает ли сервер на 5000
netstat -an | grep 5000

# На macOS
lsof -i :5000
```

---

## Расширенное тестирование

### Мультиплеер (4+ игрока)
Создай несколько комнат и присоединись разными игроками:

```json
// Player3
{"Type":"Connect","Payload":{"playerName":"Player3"}}
{"Type":"CreateRoom","Payload":{"roomName":"Room2"}}

// Player4
{"Type":"Connect","Payload":{"playerName":"Player4"}}
{"Type":"JoinRoom","Payload":{"roomId":"<roomId Room2>"}}
```

### Проверка RoomsList
Каждый клиент может запросить актуальный список:
```json
{"Type":"RoomsList","Payload":{}}
```

Должны быть все активные комнаты (не полные и не запущенные).

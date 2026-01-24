# Справка по API сервера

## Протокол связи
- **Протокол**: TCP
- **Порт**: 5000
- **Формат**: JSON (строки, разделённые `\n`)
- **Кодировка**: UTF-8

---

## Все команды сервера

### 1. Connect (подключение)
```json
{
  "Type": "Connect",
  "Payload": {
    "playerName": "string"
  }
}
```

**Ответ сервера:**
```json
{
  "Type": "connected",
  "Payload": {
    "playerId": "string (UUID)"
  }
}
```

**Описание**: Подключает клиента к серверу и присваивает ему уникальный ID.

---

### 2. CreateRoom (создание комнаты)
```json
{
  "Type": "CreateRoom",
  "Payload": {
    "roomName": "string"
  }
}
```

**Ответ сервера (создатель):**
```json
{
  "Type": "RoomCreated",
  "Payload": {
    "roomId": "string (UUID)",
    "roomName": "string",
    "maxPlayers": 2
  }
}
```

**Трансляция всем клиентам:**
```json
{
  "Type": "RoomsList",
  "Payload": {
    "rooms": [
      {
        "id": "string",
        "name": "string",
        "maxPlayers": 2,
        "currentPlayers": 1,
        "isGameStarted": false,
        "players": [
          {
            "id": "string",
            "name": "string"
          }
        ]
      }
    ]
  }
}
```

**Описание**: Создаёт новую комнату на 2 игроков и добавляет создателя в неё.

---

### 3. JoinRoom (присоединение к комнате)
```json
{
  "Type": "JoinRoom",
  "Payload": {
    "roomId": "string (UUID)"
  }
}
```

**Успешный ответ:**
```json
{
  "Type": "JoinRoomResult",
  "Payload": {
    "success": true,
    "roomId": "string",
    "roomName": "string",
    "players": [
      {
        "id": "string",
        "name": "string"
      },
      {
        "id": "string",
        "name": "string"
      }
    ],
    "message": "Joined room successfully"
  }
}
```

**Ошибка (комната не найдена):**
```json
{
  "Type": "JoinRoomResult",
  "Payload": {
    "success": false,
    "message": "Room not found"
  }
}
```

**Ошибка (комната полна):**
```json
{
  "Type": "JoinRoomResult",
  "Payload": {
    "success": false,
    "message": "Room is full"
  }
}
```

**Ошибка (игра уже запущена):**
```json
{
  "Type": "JoinRoomResult",
  "Payload": {
    "success": false,
    "message": "Game already started"
  }
}
```

**Трансляция всем клиентам:**
```json
{
  "Type": "RoomsList",
  "Payload": { ... }
}
```

**Описание**: Добавляет клиента в существующую комнату.

---

### 4. PlayerReady (готовность игрока)
```json
{
  "Type": "PlayerReady",
  "Payload": {}
}
```

**Ответ сервера:**
```json
{
  "Type": "PlayerReady",
  "Payload": {
    "playerId": "string",
    "message": "Player ready"
  }
}
```

**Если оба игрока готовы, отправляется обоим:**
```json
{
  "Type": "GameStateChanged",
  "Payload": {
    "state": "GameStarted",
    "roomId": "string",
    "players": [
      {
        "id": "string",
        "name": "string"
      },
      {
        "id": "string",
        "name": "string"
      }
    ]
  }
}
```

**Описание**: Помечает игрока как готового к игре. Когда оба игрока готовы, сервер запускает игру.

---

### 5. RoomsList (запрос списка комнат)
```json
{
  "Type": "RoomsList",
  "Payload": {}
}
```

**Ответ сервера:**
```json
{
  "Type": "RoomsList",
  "Payload": {
    "rooms": [
      {
        "id": "string",
        "name": "string",
        "maxPlayers": 2,
        "currentPlayers": 1,
        "isGameStarted": false,
        "players": [
          {
            "id": "string",
            "name": "string"
          }
        ]
      }
    ]
  }
}
```

**Описание**: Получает актуальный список доступных комнат (не полных и не запущенных).

---

### 6. Ping (проверка соединения)
```json
{
  "Type": "Ping",
  "Payload": {}
}
```

**Ответ сервера:**
```json
{
  "Type": "pong",
  "Payload": {}
}
```

**Описание**: Проверяет соединение с сервером.

---

### 7. Shoot (выстрел)
```json
{
  "Type": "Shoot",
  "Payload": {
    "x": 0,
    "y": 0
  }
}
```

**Ответ сервера:**
```json
{
  "Type": "ShootResult",
  "Payload": {
    "x": 0,
    "y": 0,
    "result": "Hit|Miss|Sink"
  }
}
```

**Описание**: Отправляет выстрел во время активной игры.

---

### 8. Reconnect (переподключение)
```json
{
  "Type": "Reconnect",
  "Payload": {
    "playerId": "string (UUID)"
  }
}
```

**Ответ сервера:**
```json
{
  "Type": "reconnected",
  "Payload": {
    "playerId": "string",
    "opponent": "string (имя противника)"
  }
}
```

**Описание**: Переподключает существующего игрока.

---

## Сообщения об ошибках

**Формат:**
```json
{
  "Type": "error",
  "Payload": {
    "message": "string"
  }
}
```

**Возможные ошибки:**
- `"Invalid JSON"` - Неверный формат JSON
- `"Missing Type"` - Отсутствует поле Type
- `"Unknown command"` - Неизвестная команда
- `"Not connected"` - Клиент не подключен (нужен Connect)
- `"Not in a room"` - Клиент не в комнате
- `"Not in a game"` - Клиент не в игре
- `"Failed to mark player as ready"` - Ошибка при пометке готовности
- `"Room not found"` - Комната не найдена
- `"Room is full"` - Комната полна
- `"Game already started"` - Игра уже запущена
- `"Player not found"` - Игрок не найден

---

## Состояния комнаты

- **Доступна для присоединения**: `currentPlayers < maxPlayers && !isGameStarted`
- **Ожидание готовности**: `currentPlayers == maxPlayers && !isGameStarted`
- **Игра запущена**: `isGameStarted == true`

---

## Состояния игрока в комнате

1. **Присоединен**: Игрок добавлен в комнату, `PlayerReadyStatus[playerId] == false`
2. **Готов**: `PlayerReadyStatus[playerId] == true`
3. **Игра запущена**: Оба игрока имеют `PlayerReadyStatus == true`, создана `GameSession`

---

## Поток типичной игровой сессии

```
1. CLIENT A: Connect
   SERVER: connected (playerId: A)

2. CLIENT A: CreateRoom (roomName: "Game")
   SERVER: RoomCreated (roomId: 123)
   SERVER: RoomsList (broadcast) [Room: 123, 1/2 players]

3. CLIENT B: Connect
   SERVER: connected (playerId: B)

4. CLIENT B: RoomsList
   SERVER: RoomsList [Room: 123, 1/2 players]

5. CLIENT B: JoinRoom (roomId: 123)
   SERVER: JoinRoomResult (success: true)
   SERVER: RoomsList (broadcast) [Room: 123, 2/2 players]

6. CLIENT A: PlayerReady
   SERVER: PlayerReady (playerId: A)

7. CLIENT B: PlayerReady
   SERVER: PlayerReady (playerId: B)
   SERVER: GameStateChanged (state: GameStarted) [broadcast to A and B]

8. CLIENT A: Shoot (x: 0, y: 0)
   SERVER: ShootResult

... и так далее до конца игры
```

---

## Замечания

- **Максимум игроков в комнате**: 2 (жёстко задано)
- **Максимум комнат**: не ограничено
- **Максимум подключённых клиентов**: не ограничено
- **Timeout соединения**: не установлен (постоянное соединение)
- **Таймаут хода**: 30 секунд (реализовано в GameSession)

---

## Примеры на разных языках

### JavaScript / Node.js
```javascript
const net = require('net');

const socket = net.createConnection(5000, 'localhost');

socket.on('connect', () => {
  socket.write(JSON.stringify({
    Type: 'Connect',
    Payload: { playerName: 'TestPlayer' }
  }) + '\n');
});

socket.on('data', (data) => {
  console.log(JSON.parse(data.toString()));
});
```

### Python
```python
import socket
import json

sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.connect(('localhost', 5000))

msg = {
    'Type': 'Connect',
    'Payload': {'playerName': 'TestPlayer'}
}

sock.send((json.dumps(msg) + '\n').encode())
response = sock.recv(1024).decode()
print(json.loads(response))
```

### C#
```csharp
using (var client = new TcpClient("localhost", 5000))
using (var stream = client.GetStream())
using (var writer = new StreamWriter(stream) { AutoFlush = true })
{
    var msg = new
    {
        Type = "Connect",
        Payload = new { playerName = "TestPlayer" }
    };
    
    writer.WriteLine(JsonSerializer.Serialize(msg));
}
```

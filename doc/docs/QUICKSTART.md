# 🚀 Быстрый старт: Сервер комнат

## Что было сделано?

Реализована полная система управления комнатами и готовностью игроков на C# сервере.

Теперь сервер обрабатывает все команды, которые отправляет клиент:
- ✅ CreateRoom
- ✅ JoinRoom  
- ✅ PlayerReady
- ✅ RoomsList
- ✅ Connect (существовал)
- ✅ Ping (существовал)
- ✅ Shoot (существовал)
- ✅ Reconnect (существовал)

---

## Запуск сервера

```bash
cd /Users/masha/battle_of_sea/battle_of_sea/battle_of_sea
dotnet run
```

Сервер будет слушать на `localhost:5000`

---

## Быстрый тест (2 клиента)

### Терминал 1: Подключить Player1 и создать комнату

```bash
nc localhost 5000
```

Скопируй и отправь:
```json
{"Type":"Connect","Payload":{"playerName":"Player1"}}
{"Type":"CreateRoom","Payload":{"roomName":"TestGame"}}
```

Ты получишь:
```json
{"Type":"connected","Payload":{"playerId":"xxx-xxx-xxx"}}
{"Type":"RoomCreated","Payload":{"roomId":"yyy-yyy-yyy","roomName":"TestGame","maxPlayers":2}}
```

**Сохрани roomId!** (yyy-yyy-yyy)

Затем:
```json
{"Type":"PlayerReady","Payload":{}}
```

---

### Терминал 2: Подключить Player2 и присоединиться

```bash
nc localhost 5000
```

Скопируй и отправь:
```json
{"Type":"Connect","Payload":{"playerName":"Player2"}}
{"Type":"JoinRoom","Payload":{"roomId":"yyy-yyy-yyy"}}
```

Ты получишь:
```json
{"Type":"connected","Payload":{"playerId":"zzz-zzz-zzz"}}
{"Type":"JoinRoomResult","Payload":{"success":true,"roomId":"yyy-yyy-yyy",...}}
```

Затем пометь себя как готовый:
```json
{"Type":"PlayerReady","Payload":{}}
```

---

### Что произойдёт?

Оба клиента получат:
```json
{
  "Type":"GameStateChanged",
  "Payload":{
    "state":"GameStarted",
    "roomId":"yyy-yyy-yyy",
    "players":[
      {"id":"xxx-xxx-xxx","name":"Player1"},
      {"id":"zzz-zzz-zzz","name":"Player2"}
    ]
  }
}
```

**🎮 Игра запущена!**

---

## Логи сервера

На сервере ты увидишь:
```
[ROOM_CREATED] Room TestGame (ID: yyy-yyy-yyy) created by Player1
[ROOM_JOINED] Player Player2 joined room TestGame
[PLAYER_READY] Player1 is ready in room TestGame
[PLAYER_READY] Player2 is ready in room TestGame
[GAME_STARTED] Game starting in room TestGame
[GAME_START] Game started in room TestGame: Player1 vs Player2
[BROADCAST] RoomsList sent to 2 players
```

---

## Все команды

| Команда | Использование |
|---------|-------|
| Connect | `{"Type":"Connect","Payload":{"playerName":"Player1"}}` |
| CreateRoom | `{"Type":"CreateRoom","Payload":{"roomName":"Room"}}` |
| JoinRoom | `{"Type":"JoinRoom","Payload":{"roomId":"xxx"}}` |
| PlayerReady | `{"Type":"PlayerReady","Payload":{}}` |
| RoomsList | `{"Type":"RoomsList","Payload":{}}` |
| Ping | `{"Type":"Ping","Payload":{}}` |
| Shoot | `{"Type":"Shoot","Payload":{"x":0,"y":0}}` |
| Reconnect | `{"Type":"Reconnect","Payload":{"playerId":"xxx"}}` |

---

## Основные ответы сервера

| Ответ | Когда |
|-------|-------|
| `connected` | После Connect |
| `RoomCreated` | После CreateRoom |
| `RoomsList` | После CreateRoom, JoinRoom, RoomsList или PlayerReady |
| `JoinRoomResult` | После JoinRoom (успех или ошибка) |
| `PlayerReady` | После PlayerReady (подтверждение) |
| `GameStateChanged` | Когда оба игрока готовы (запуск игры) |
| `ShootResult` | Ответ на Shoot |
| `pong` | Ответ на Ping |
| `error` | При любых ошибках |

---

## Что если...

### Я присоединяюсь к несуществующей комнате?
```json
{
  "Type":"JoinRoomResult",
  "Payload":{
    "success":false,
    "message":"Room not found"
  }
}
```

### Я пытаюсь присоединиться к полной комнате?
```json
{
  "Type":"JoinRoomResult",
  "Payload":{
    "success":false,
    "message":"Room is full"
  }
}
```

### Я хочу создать ещё одну комнату?
Просто отправь `CreateRoom` снова - новая комната будет создана!

### Я хочу посмотреть список доступных комнат?
```json
{"Type":"RoomsList","Payload":{}}
```

---

## Файлы документации

- 📖 [SERVER_API_REFERENCE.md](./SERVER_API_REFERENCE.md) - полный API
- 📖 [TESTING_GUIDE.md](./TESTING_GUIDE.md) -详細 примеры тестирования
- 📖 [SERVER_ROOM_IMPLEMENTATION.md](./SERVER_ROOM_IMPLEMENTATION.md) - технические детали

---

## Основные классы

### Room
```csharp
public class Room
{
    public string Id { get; set; }                           // UUID комнаты
    public string Name { get; set; }                         // Название
    public int MaxPlayers { get; set; }                      // Макс 2 игрока
    public List<Player> Players { get; set; }                // Список игроков
    public Dictionary<string, bool> PlayerReadyStatus { get; set; }  // Готовность
    public bool IsGameStarted { get; set; }                  // Статус игры
}
```

### GameManager (новые методы)
```csharp
Room CreateRoom(string name, int maxPlayers)
void JoinRoom(Player player, Room room)
bool MarkPlayerReady(string playerId)
bool CheckAndStartGame(Room room)
Room? FindRoomByPlayerId(string playerId)
List<Room> GetAvailableRooms()
```

### ClientConnection (новые обработчики)
```csharp
case "createroom":      // Создание комнаты
case "joinroom":        // Присоединение к комнате
case "playerready":     // Готовность игрока
case "roomslist":       // Список комнат
```

---

## Инструменты для тестирования

### netcat (macOS/Linux)
```bash
nc localhost 5000
```

### telnet
```bash
telnet localhost 5000
```

### Python
```python
import socket, json
sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.connect(('localhost', 5000))
msg = {"Type":"Connect","Payload":{"playerName":"Test"}}
sock.send((json.dumps(msg) + "\n").encode())
print(json.loads(sock.recv(1024).decode()))
```

---

## Компиляция ✅

```
Build succeeded.
0 Error(s), 22 Warning(s)
```

Код готов к использованию!

---

## Что дальше?

1. Запусти сервер: `dotnet run`
2. Откой 2 терминала с `nc localhost 5000`
3. Следуй инструкциям выше
4. Смотри логи на сервере для отладки

**Всё готово! Удачи! 🚀**

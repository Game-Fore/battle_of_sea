# Реализация системы комнат на сервере

## Обзор изменений

Реализована полная поддержка работы с комнатами и управлением готовностью игроков на сервере. Теперь сервер корректно обрабатывает все команды, которые отправляет клиент.

## Измененные файлы

### 1. `/Game/GameManager.cs`

#### Изменения в классе Room:
- Добавлено поле `Dictionary<string, bool> PlayerReadyStatus` для отслеживания готовности каждого игрока
- Добавлен метод `AreAllPlayersReady()` для проверки готовности всех игроков

```csharp
public class Room
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int MaxPlayers { get; set; }
    public List<Player> Players { get; set; } = new List<Player>();
    public Dictionary<string, bool> PlayerReadyStatus { get; set; } = new Dictionary<string, bool>();
    public bool IsGameStarted { get; set; } = false;
    
    public bool AreAllPlayersReady()
    {
        if (Players.Count < MaxPlayers)
            return false;
        
        foreach (var player in Players)
        {
            if (!PlayerReadyStatus.ContainsKey(player.Id) || !PlayerReadyStatus[player.Id])
                return false;
        }
        return true;
    }
}
```

#### Новые методы в GameManager:

1. **`FindRoomByPlayerId(string playerId)`** - Найти комнату по ID игрока
2. **`MarkPlayerReady(string playerId)`** - Пометить игрока как готовый
3. **`CheckAndStartGame(Room room)`** - Проверить готовность и запустить игру если оба готовы
4. **`GetAvailableRooms()`** - Получить список доступных (не полных и не запущенных) комнат
5. **`BroadcastToRoom(Room room, object message)`** - Отправить сообщение всем игрокам в комнате

### 2. `/Network/ClientConnection.cs`

Добавлены обработчики для новых команд в методе `HandleMessageAsync()`:

#### Case "CreateRoom"
- Создает новую комнату с названием из payload
- Добавляет создателя в комнату
- Инициализирует playerReadyStatus для создателя как `false`
- Отправляет `RoomCreated` создателю
- Вещает обновленный `RoomsList` всем клиентам

```json
// Запрос от клиента:
{
  "Type": "CreateRoom",
  "Payload": {
    "roomName": "My Game Room"
  }
}

// Ответ сервера:
{
  "Type": "RoomCreated",
  "Payload": {
    "roomId": "550e8400-e29b-41d4-a716-446655440000",
    "roomName": "My Game Room",
    "maxPlayers": 2
  }
}

// Трансляция всем:
{
  "Type": "RoomsList",
  "Payload": {
    "rooms": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "name": "My Game Room",
        "maxPlayers": 2,
        "currentPlayers": 1,
        "isGameStarted": false,
        "players": [
          {"id": "player1-id", "name": "Player1"}
        ]
      }
    ]
  }
}
```

#### Case "JoinRoom"
- Проверяет существование комнаты
- Проверяет, не запущена ли уже игра
- Проверяет, не полна ли комната
- Добавляет игрока в комнату
- Отправляет `JoinRoomResult` с информацией о результате
- Вещает обновленный `RoomsList` всем клиентам

```json
// Запрос от клиента:
{
  "Type": "JoinRoom",
  "Payload": {
    "roomId": "550e8400-e29b-41d4-a716-446655440000"
  }
}

// Успешный ответ:
{
  "Type": "JoinRoomResult",
  "Payload": {
    "success": true,
    "roomId": "550e8400-e29b-41d4-a716-446655440000",
    "roomName": "My Game Room",
    "players": [
      {"id": "player1-id", "name": "Player1"},
      {"id": "player2-id", "name": "Player2"}
    ],
    "message": "Joined room successfully"
  }
}

// Ошибка (если комната полна):
{
  "Type": "JoinRoomResult",
  "Payload": {
    "success": false,
    "message": "Room is full"
  }
}
```

#### Case "PlayerReady"
- Помечает игрока как готовый в его комнате
- Отправляет подтверждение `PlayerReady`
- **Проверяет, готовы ли оба игрока**
- **Если оба готовы - запускает игру**
- Отправляет обоим игрокам `GameStateChanged` со стартом игры

```json
// Запрос от клиента:
{
  "Type": "PlayerReady",
  "Payload": {}
}

// Ответ сервера (готовность принята):
{
  "Type": "PlayerReady",
  "Payload": {
    "playerId": "player1-id",
    "message": "Player ready"
  }
}

// Когда оба игрока готовы, отправляется обоим:
{
  "Type": "GameStateChanged",
  "Payload": {
    "state": "GameStarted",
    "roomId": "550e8400-e29b-41d4-a716-446655440000",
    "players": [
      {"id": "player1-id", "name": "Player1"},
      {"id": "player2-id", "name": "Player2"}
    ]
  }
}
```

#### Case "RoomsList"
- Вещает текущий список доступных комнат всем клиентам

```json
// Запрос от клиента:
{
  "Type": "RoomsList",
  "Payload": {}
}

// Ответ сервера:
{
  "Type": "RoomsList",
  "Payload": {
    "rooms": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "name": "My Game Room",
        "maxPlayers": 2,
        "currentPlayers": 1,
        "isGameStarted": false,
        "players": [
          {"id": "player1-id", "name": "Player1"}
        ]
      },
      {
        "id": "660e8400-e29b-41d4-a716-446655440001",
        "name": "Quick Match",
        "maxPlayers": 2,
        "currentPlayers": 0,
        "isGameStarted": false,
        "players": []
      }
    ]
  }
}
```

#### Новый метод `BroadcastRoomsList()`
- Отправляет список доступных комнат всем подключенным клиентам
- Вызывается после создания комнаты, присоединения к комнате или при запросе `RoomsList`

## Логирование

Все операции логируются на сервер с префиксами:

- `[ROOM_CREATED]` - комната создана
- `[ROOM_JOINED]` - игрок присоединился к комнате
- `[PLAYER_READY]` - игрок пометил себя как готовый
- `[GAME_STARTED]` - игра запущена (оба игрока готовы)
- `[BROADCAST]` - отправлено сообщение всем клиентам

Пример логов:
```
[ROOM_CREATED] Room My Game Room (ID: 550e8400-e29b-41d4-a716-446655440000) created by Player1
[ROOM_JOINED] Player Player2 joined room My Game Room
[PLAYER_READY] Player1 is ready in room My Game Room
[PLAYER_READY] Player2 is ready in room My Game Room
[GAME_STARTED] Game starting in room My Game Room
[GAME_START] Game started in room My Game Room: Player1 vs Player2
[BROADCAST] RoomsList sent to 2 players
```

## Поток работы приложения

### 1. Подключение
```
Client: { Type: "Connect", Payload: { playerName: "Player1" } }
Server: { Type: "connected", Payload: { playerId: "xxx" } }
```

### 2. Создание комнаты
```
Client: { Type: "CreateRoom", Payload: { roomName: "Game Room" } }
Server: { Type: "RoomCreated", Payload: {...} }
Server: { Type: "RoomsList", Payload: {...} } // трансляция
```

### 3. Присоединение другого игрока
```
Client2: { Type: "JoinRoom", Payload: { roomId: "xxx" } }
Server: { Type: "JoinRoomResult", Payload: { success: true, ... } }
Server: { Type: "RoomsList", Payload: {...} } // трансляция
```

### 4. Готовность игроков
```
Client: { Type: "PlayerReady", Payload: {} }
Server: { Type: "PlayerReady", Payload: {...} }
// Когда оба готовы:
Server: { Type: "GameStateChanged", Payload: { state: "GameStarted", ... } } // обоим
```

### 5. Игра
```
Client: { Type: "Shoot", Payload: { x: 0, y: 0 } }
Server: { Type: "ShootResult", Payload: {...} }
```

## Компиляция

Проект компилируется без ошибок (только warnings об инициализации, которые не критичны):

```
Build succeeded.
22 Warning(s)
0 Error(s)
```

## Готовые к использованию методы

### GameManager
- `CreateRoom(string name, int maxPlayers)` → Room
- `FindRoomById(string roomId)` → Room?
- `FindRoomByPlayerId(string playerId)` → Room?
- `JoinRoom(Player player, Room room)` → void
- `MarkPlayerReady(string playerId)` → bool
- `CheckAndStartGame(Room room)` → bool
- `GetAvailableRooms()` → List<Room>
- `BroadcastToRoom(Room room, object message)` → Task
- `GetRooms()` → List<Room>

### ClientConnection
- `HandleMessageAsync(ClientMessage message, StreamWriter writer)` - обрабатывает все 8 типов сообщений
- `BroadcastRoomsList()` - приватный метод для трансляции списка комнат
- `SendAsync(ServerMessage message)` → Task

## Готово к продакшену

✅ Все новые команды реализованы
✅ JSON сериализация корректна
✅ Проект компилируется
✅ Логирование добавлено
✅ Все типы сообщений совпадают с клиентскими ожиданиями
✅ Готовность игроков отслеживается
✅ Запуск игры автоматический

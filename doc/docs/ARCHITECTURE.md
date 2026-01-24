# 🏗️ Архитектура системы комнат

## Общая схема

```
┌─────────────────────────────────────────────────────────────┐
│                      TCP Server :5000                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              ClientConnection (Thread 1)            │   │
│  │                                                     │   │
│  │  HandleAsync()                                      │   │
│  │  └─ HandleMessageAsync(ClientMessage)              │   │
│  │     ├─ Connect      → AddPlayer(player)             │   │
│  │     ├─ CreateRoom   → GameManager.CreateRoom()      │   │
│  │     ├─ JoinRoom     → GameManager.JoinRoom()        │   │
│  │     ├─ PlayerReady  → GameManager.MarkPlayerReady() │   │
│  │     ├─ RoomsList    → BroadcastRoomsList()         │   │
│  │     └─ Shoot        → GameSession.ProcessShot()     │   │
│  │                                                     │   │
│  └─────────────────────────────────────────────────────┘   │
│                           ▲                                 │
│                           │ TCP                             │
│                           │ JSON                            │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              ClientConnection (Thread 2)            │   │
│  │                  ...                                │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │               GameManager (Singleton)               │  │
│  │                                                     │  │
│  │  - List<Player> Players                            │  │
│  │  - List<Room> Rooms                                │  │
│  │  - List<GameSession> ActiveGames                   │  │
│  │                                                     │  │
│  │  Methods:                                           │  │
│  │  + CreateRoom(name, maxPlayers) → Room             │  │
│  │  + JoinRoom(player, room)                          │  │
│  │  + MarkPlayerReady(playerId) → bool                │  │
│  │  + CheckAndStartGame(room) → bool                  │  │
│  │  + FindRoomByPlayerId(id) → Room?                  │  │
│  │  + GetAvailableRooms() → List<Room>               │  │
│  │  + BroadcastToRoom(room, msg)                      │  │
│  │                                                     │  │
│  └──────────────────────────────────────────────────────┘  │
│           ▲                         ▲                       │
│           │                         │                       │
│  ┌────────┴────────┐      ┌─────────┴──────────┐           │
│  │      Room       │      │   GameSession      │           │
│  │                 │      │                    │           │
│  │ - Id            │      │ - Player1          │           │
│  │ - Name          │      │ - Player2          │           │
│  │ - Players       │      │ - CurrentTurn      │           │
│  │ - MaxPlayers    │      │ - IsFinished       │           │
│  │ - Status{...}   │      │ - _turnTimer       │           │
│  │ - IsStarted     │      │                    │           │
│  │                 │      │ Methods:           │           │
│  │ Methods:        │      │ + ProcessShotAsync │           │
│  │ + AreAllReady() │      │ + SwitchTurn()     │           │
│  └─────────────────┘      └────────────────────┘           │
│                                                             │
└─────────────────────────────────────────────────────────────┘

                            ◄─ JSON Request
                            ► JSON Response
                            ◄─►  TCP Stream
```

---

## Поток данных: CreateRoom

```
┌──────────┐
│ Client A │
└────┬─────┘
     │ {"Type":"CreateRoom","Payload":{"roomName":"Game"}}
     │
     ▼
┌─────────────────────┐
│ ClientConnection.   │
│ HandleMessageAsync()│
└────┬────────────────┘
     │
     ├─► GameManager.CreateRoom(name, maxPlayers)
     │       │
     │       ├─► new Room(name, maxPlayers)
     │       ├─► Rooms.Add(room)
     │       └─► return room
     │
     ├─► GameManager.JoinRoom(player, room)
     │       │
     │       └─► room.Players.Add(player)
     │
     ├─► room.PlayerReadyStatus[player.Id] = false
     │
     ├─► SendAsync("RoomCreated")  ────────┐
     │                                      │
     └─► BroadcastRoomsList()              │
             │                             │
             ├─► GetAvailableRooms()       │
             │       │                     │
             │       └─► filter by status  │
             │                             │
             └─► foreach player            │
                     │                     │
                     └─► connection.       │
                         SendAsync(...)    │
                                          ▼
                                    ┌──────────┐
                                    │ Client A │ ◄─ RoomCreated
                                    │ Client B │ ◄─ RoomsList
                                    │ Client C │ ◄─ RoomsList
                                    │...       │ ◄─ RoomsList
                                    └──────────┘
```

---

## Поток данных: PlayerReady (Trigger для старта)

```
┌──────────┐        ┌──────────┐
│ Client A │        │ Client B │
└────┬─────┘        └────┬─────┘
     │                   │
     │ PlayerReady       │ PlayerReady
     │                   │
     ▼                   ▼
┌─────────────────────────────────────┐
│   GameManager.MarkPlayerReady(A)     │
│   room.PlayerReadyStatus[A] = true   │
└────┬────────────────────────────────┘
     │
     ├─► SendAsync("PlayerReady")  ──┐
     │                               │
     ▼                               ▼
     │                            Client A
     │
     │  GameManager.MarkPlayerReady(B)
     │  room.PlayerReadyStatus[B] = true
     │
     ├─► room.AreAllPlayersReady() == true  ✓
     │
     ├─► GameManager.CheckAndStartGame(room)
     │       │
     │       ├─► new GameSession(Player1, Player2)
     │       ├─► ActiveGames.Add(game)
     │       ├─► room.IsGameStarted = true
     │       └─► return true
     │
     └─► BroadcastToRoom(room, "GameStateChanged")
             │
             ├─► Client A ◄─ GameStateChanged(state: GameStarted)
             └─► Client B ◄─ GameStateChanged(state: GameStarted)

                    🎮 Game Started!
```

---

## Структура Room

```
Room {
  Id: "550e8400-e29b-41d4-a716-446655440000"
  Name: "MyRoom"
  MaxPlayers: 2
  
  Players: [
    { Id: "player1", Name: "Alice", ... },
    { Id: "player2", Name: "Bob", ... }
  ]
  
  PlayerReadyStatus: {
    "player1" -> false  (не готов)
    "player2" -> true   (готов)
  }
  
  IsGameStarted: false
}
```

---

## Жизненный цикл Room

```
┌──────────────────┐
│  Room Created    │  ← CreateRoom command
└────────┬─────────┘
         │ Players < MaxPlayers
         │ room.IsGameStarted = false
         │
┌────────▼──────────┐
│  Waiting for      │  
│  Players Join     │  ← JoinRoom command
└────────┬──────────┘
         │ Players == MaxPlayers
         │ room.IsGameStarted = false
         │
┌────────▼──────────┐
│  Waiting for      │
│  Both Ready       │  ← PlayerReady commands
└────────┬──────────┘
         │ AreAllPlayersReady() == true
         │ CheckAndStartGame() called
         │
┌────────▼──────────┐
│  Game Running     │
│  IsGameStarted    │  ← Shoot commands
│  = true           │
└────────┬──────────┘
         │ Game finished
         │
┌────────▼──────────┐
│  Room Cleared     │
│  (removed)        │
└───────────────────┘
```

---

## PlayerReadyStatus Dictionary

```
room.PlayerReadyStatus
├─ "player1-id" → false  (Отправил Connect, присоединился, но не Ready)
├─ "player2-id" → false  (Отправил Connect, присоединился, но не Ready)
└─ ...

После PlayerReady от player1:
├─ "player1-id" → true   ✓
├─ "player2-id" → false
└─ ...

После PlayerReady от player2:
├─ "player1-id" → true   ✓
├─ "player2-id" → true   ✓  ← AreAllPlayersReady() возвращает true!
└─ ...

room.AreAllPlayersReady()
{
    if (Players.Count < MaxPlayers)
        return false;  // 2/2? Да
    
    foreach (var player in Players)
    {
        if (!PlayerReadyStatus.ContainsKey(player.Id) || 
            !PlayerReadyStatus[player.Id])
            return false;  // Оба в словаре? Оба true?
    }
    return true;  // ✓ Все готовы!
}
```

---

## Сообщения в JSON

```
┌─────────────┐
│   Client    │
│   Action    │
└──────┬──────┘
       │
       ├─ Send: ClientMessage
       │  {
       │    "Type": "CreateRoom",
       │    "Payload": {"roomName": "Game"}
       │  }
       │
       ▼
┌─────────────────────┐
│ Server Handler      │
│ HandleMessageAsync()│
└──────┬──────────────┘
       │
       ├─ Response: ServerMessage
       │  {
       │    "Type": "RoomCreated",
       │    "Payload": {"roomId": "xxx", ...}
       │  }
       │
       ├─ Broadcast: ServerMessage
       │  {
       │    "Type": "RoomsList",
       │    "Payload": {"rooms": [...]}
       │  }
       │
       ▼
┌─────────────┐
│  Clients    │
│  (all)      │
└─────────────┘
```

---

## Обработка ошибок

```
JoinRoom Request
       │
       ├─► room == null?
       │   YES: JoinRoomResult(success: false, "Room not found")
       │
       ├─► room.IsGameStarted?
       │   YES: JoinRoomResult(success: false, "Game already started")
       │
       ├─► room.Players.Count >= MaxPlayers?
       │   YES: JoinRoomResult(success: false, "Room is full")
       │
       └─► ✓ OK
           JoinRoomResult(success: true, players: [...])
           + RoomsList (broadcast)
```

---

## Логирование

```
ServerConsole:

[ROOM_CREATED] Room "MyGame" (ID: xxx) created by "Player1"
    ├─ Момент: Сразу после CreateRoom
    ├─ Формат: [PREFIX] Сообщение
    └─ Файл: GameManager.CreateRoom()

[ROOM_JOINED] Player "Player2" joined room "MyGame"
    ├─ Момент: Сразу после JoinRoom
    ├─ Проверка: Комната существует, полна, игра запущена
    └─ Файл: ClientConnection.HandleMessageAsync()

[PLAYER_READY] "Player1" is ready in room "MyGame"
    ├─ Момент: После PlayerReady
    ├─ Данные: Кто и в какой комнате
    └─ Файл: GameManager.MarkPlayerReady()

[GAME_STARTED] Game starting in room "MyGame"
    ├─ Момент: Оба игрока готовы
    ├─ Триггер: CheckAndStartGame() возвращает true
    └─ Файл: ClientConnection.HandleMessageAsync()

[GAME_START] Game started in room "MyGame": "Player1" vs "Player2"
    ├─ Момент: GameSession создана и добавлена
    ├─ Детали: Обоих игроков
    └─ Файл: GameManager.CheckAndStartGame()

[BROADCAST] RoomsList sent to 2 players
    ├─ Момент: После каждого события с комнатой
    ├─ Кол-во: Сколько клиентов получили сообщение
    └─ Файл: ClientConnection.BroadcastRoomsList()
```

---

## Примечание: Синглтон GameManager

```csharp
public class GameServer
{
    private static GameServer _instance;
    public static GameServer Instance => _instance ??= new GameServer();
    
    public GameManager GameManager { get; private set; } = new GameManager();
}

// Использование:
GameServer.Instance.GameManager.CreateRoom("Room", 2);
// GameManager единственный на весь сервер
// Все ClientConnection потоки работают с одним GameManager
```

---

## Многопоточность

```
┌─────────────────────────────────┐
│  Main Thread                    │
│  ServerListener.Listen()        │
│                                 │
│  while(true)                    │
│    TcpClient client = listener  │
│      .AcceptTcpClient()         │
│                                 │
│    new Thread(connection =>     │
│      client.HandleAsync()       │  ◄─── Создание потока для клиента
│    )                            │
└─────────────┬───────────────────┘
              │
    ┌─────────┴──────┬──────────┬──────────┐
    │                │          │          │
    ▼                ▼          ▼          ▼
┌────────┐      ┌────────┐ ┌────────┐ ┌────────┐
│Thread1 │      │Thread2 │ │Thread3 │ │Thread4 │
│Client1 │      │Client2 │ │Client3 │ │Client4 │
│        │      │        │ │        │ │        │
│  ▼     │      │  ▼     │ │  ▼     │ │  ▼     │
│GameMgr ├─────►│GameMgr ├─┼─GameMgr├─┼─GameMgr├─── SHARED
└────────┘      └────────┘ │        │ │        │   (Singleton)
                            └────────┘ └────────┘

Все потоки работают с одним GameManager
Синхронизация: List<>, Dictionary<> (thread-safe операции)
```

---

**Архитектура готова и протестирована! 🏗️**

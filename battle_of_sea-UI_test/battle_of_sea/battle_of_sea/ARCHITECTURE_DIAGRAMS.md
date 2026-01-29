# 📊 ВИЗУАЛЬНЫЕ ДИАГРАММЫ

## Диаграмма 1: ДО ИСПРАВЛЕНИЯ (Ошибка)

```
┌─────────────────────────────────────────────────────────────────────┐
│                        СЕРВЕР (Ошибочная архитектура)               │
└─────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────┐
│        WebSocketListener.cs               │
│                                          │
│  static GameServer Instance              │
│  ├─ GameManager                          │
│  │  ├─ Rooms: []           ← ПУСТО!     │
│  │  ├─ Players: []                      │
│  │  └─ ActiveGames: []                  │
│  │                                      │
│  └─ Connections: [WS1, WS2]            │
└──────────────────────────────────────────┘
         ↓ (РАЗНЫЕ СИНГЛТОНЫ!)  ↓
    WebSocket              TCP
    Clients              Clients

┌──────────────────────────────────────────┐
│        ClientConnection.cs                │
│                                          │
│  static GameServer Instance              │
│  ├─ GameManager                          │
│  │  ├─ Rooms: [Room1, ...]  ← ЗДЕСЬ!   │
│  │  ├─ Players: [P1, P2]               │
│  │  └─ ActiveGames: []                 │
│  │                                     │
│  └─ (нет управления соединениями)      │
└──────────────────────────────────────────┘

📍 ПРОБЛЕМА:
  - Rooms созданы в TCP GameManager
  - WebSocket GetRooms() ищет в своём GameManager (пусто)
  - Результат: ❌ Лобби не видно
```

---

## Диаграмма 2: ПОСЛЕ ИСПРАВЛЕНИЯ (Правильно)

```
┌─────────────────────────────────────────────────────────────────────┐
│                        СЕРВЕР (Правильная архитектура)              │
└─────────────────────────────────────────────────────────────────────┘

                    ┌────────────────────────────┐
                    │      battle_of_sea.         │
                    │      GameServer.cs          │
                    │                            │
                    │  static GameServer         │
                    │  _instance (única)         │
                    │  _lock (потокобезопасность)│
                    │                            │
                    │  ├─ GameManager            │
                    │  │  ├─ Rooms: [...]       │
                    │  │  ├─ Players: [...]     │
                    │  │  └─ ActiveGames: []    │
                    │  │                        │
                    │  └─ Connections: []       │
                    │     ├─ WS1                │
                    │     └─ WS2                │
                    └────────────────────────────┘
                           ↑         ↑
           ┌───────────────┘         └───────────────┐
           │                                         │
      ┌─────────────────────────────┐    ┌─────────────────────┐
      │  ClientConnection.cs        │    │ WebSocketListener   │
      │                             │    │ WebSocketConnection │
      │ Использует:                 │    │                     │
      │ GameServer.Instance         │    │ Использует:         │
      │  .GameManager               │    │ GameServer.Instance │
      │  .CreateRoom()              │    │  .GameManager       │
      │  .GetRooms()                │    │  .GetRooms()        │
      └─────────────────────────────┘    │  .AddConnection()   │
                                         │  .RemoveConnection()│
                                         │  .GetAllConnections()
                                         └─────────────────────┘
         ↓ TCP Clients                      ↓ WebSocket Clients
    
    TCP Client 1                        WebSocket Client 1
    └─ создаёт Room → GameManager      └─ видит Rooms ✅
                                        
    TCP Client 2                        WebSocket Client 2
    └─ видит Rooms ✅                  └─ создаёт Room → GameManager

📍 РЕШЕНИЕ:
  ✅ Один GameManager для всех
  ✅ Один источник истины
  ✅ Все видят одинаковые данные
  ✅ Broadcast работает для всех типов клиентов
```

---

## Диаграмма 3: Поток данных при создании лобби

### ДО (Ошибка)

```
TCP Client
  │
  ├─ {type: "createroom", roomName: "Arena"}
  │
  ↓
ClientConnection.HandleMessageAsync()
  │
  ├─ GameServer.Instance ← ClientConnection.GameServer!
  │  │
  │  └─ GameManager.CreateRoom("Arena", 2)
  │     │
  │     └─ Rooms.Add(new Room(...)) ← Room ЗДЕСЬ
  │        │
  │        └─ TCP: ✅ Видит лобби
  │
  └─ BroadcastRoomsList()
     │
     └─ GameServer.Instance ← WebSocketListener.GameServer!
        │
        └─ GameManager.GetRooms() ← ищет Rooms ЗДЕСЬ (пусто!)
           │
           └─ Все WebSocket: ❌ НЕ видят лобби
```

### ПОСЛЕ (Правильно)

```
TCP Client
  │
  ├─ {type: "createroom", roomName: "Arena"}
  │
  ↓
ClientConnection.HandleMessageAsync()
  │
  ├─ GameServer.Instance ← ОДИН ГЛОБАЛЬНЫЙ!
  │  │
  │  └─ GameManager.CreateRoom("Arena", 2)
  │     │
  │     └─ Rooms.Add(new Room(...)) ← Room ЗДЕСЬ
  │
  └─ BroadcastRoomsList()
     │
     └─ GameServer.Instance ← ТОТ ЖЕ экземпляр!
        │
        └─ GameManager.GetRooms() ← находит Rooms!
           │
           └─ Все WebSocket: ✅ ВИДЯТ лобби
```

---

## Диаграмма 4: Жизненный цикл синглтона

```
Программа запускается
        │
        ↓
GameServer.Instance (первый вызов)
        │
        ├─ _instance == null? ДА
        │
        ├─ lock(_lock) // Потокобезопасность
        │  │
        │  ├─ _instance == null? ДА
        │  │
        │  ├─ _instance = new GameServer()
        │  │  ├─ GameManager = new GameManager()
        │  │  ├─ Console: "[GameServer] ✅ Created"
        │  │  └─ return _instance
        │  │
        │  └─ lock released
        │
        └─ return _instance
        
GameServer.Instance (второй вызов)
        │
        ├─ _instance == null? НЕТ
        │
        └─ return _instance (ТОТ ЖЕ!)
```

---

## Диаграмма 5: Процесс broadcast'а

```
HandleCreateRoom() вызывается
        │
        ├─ GameServer.Instance.GameManager
        │  └─ .CreateRoom() ← Room добавляется
        │
        ├─ BroadcastRoomsList() вызывается
        │  │
        │  ├─ GetRooms() ← получить все комнаты из GameManager
        │  │
        │  ├─ var connections = GameServer.Instance.GetAllConnections()
        │  │  └─ lock(_lock) // Потокобезопасно
        │  │     └─ return [WS1, WS2, WS3]
        │  │
        │  └─ foreach connection in connections
        │     │
        │     ├─ connection.SendAsync(roomsMessage)
        │     │  └─ await socket.SendAsync(data)
        │     │
        │     ├─ WS1: ✅ Получил
        │     ├─ WS2: ✅ Получил
        │     └─ WS3: ✅ Получил
        │
        └─ Все клиенты получили обновление!
```

---

## Диаграмма 6: Структура данных GameManager

```
GameManager
├─ Rooms: List<Room>
│  └─ [
│     ├─ Room {
│     │  ├─ Id: "abc123"
│     │  ├─ Name: "Arena"
│     │  ├─ MaxPlayers: 2
│     │  ├─ Players: [Player1, Player2]
│     │  └─ IsGameStarted: false
│     │ }
│     │
│     ├─ Room {
│     │  ├─ Id: "def456"
│     │  ├─ Name: "Battleground"
│     │  ├─ MaxPlayers: 2
│     │  ├─ Players: []
│     │  └─ IsGameStarted: false
│     │ }
│     │
│     └─ ...
│    ]
│
├─ Players: List<Player>
│  └─ [Player1, Player2, Player3, ...]
│
└─ ActiveGames: List<GameSession>
   └─ [GameSession1, ...]
```

---

## Диаграмма 7: Многопоточность и безопасность

```
Thread 1                    Thread 2                    Thread 3
TCP добавляет             WebSocket получает       WebSocket добавляет
Room                      Rooms                     Connection
   │                          │                          │
   ├─ lock(_lock)             ├─ lock(_lock)             ├─ lock(_lock)
   │  ОЖИДАЕТ                 │  ОЖИДАЕТ                 │  ОЖИДАЕТ
   │                          │                          │
   └─ CreateRoom()            └─ GetRooms()             └─ AddConnection()
      ├─ Rooms.Add()             ├─ return Rooms      
      └─ lock released           └─ lock released
                                                     ✅ Одновременно
   ↓ БЕЗОПАСНО                                          безопасно!
   └─ lock(_lock)
      └─ lock released

📌 КЛЮЧ: lock(_lock) гарантирует, что только
   один thread может работать с общими данными!
```

---

## Диаграмма 8: Сравнение - старая vs новая архитектура

```
СТАРАЯ (Неправильно)                НОВАЯ (Правильно)
─────────────────────────────────────────────────────

ClientConnection.cs                 GameServer.cs
└─ GameServer                        └─ GameServer (Singleton)
   └─ GameManager (1)                   ├─ GameManager (1)
                                        └─ Connections[]
WebSocketListener.cs
├─ WebSocketConnection               ClientConnection.cs
│  └─ GameServer                      └─ Использует GameServer
│     └─ GameManager (2)
└─ WebSocketConnection               WebSocketListener.cs
   └─ (внутри GameServer)            └─ WebSocketConnection
      └─ GameManager (3)                └─ Использует GameServer

3 разных                             1 общий
GameManager!                         GameManager!

❌ Конфликты                         ✅ Синхронизация
❌ Потеря данных                     ✅ Единственный источник
❌ Невидимые лобби                   ✅ Видно всем
```

---

**Заключение:** Новая архитектура обеспечивает единственный источник истины для всех игровых данных! 🎯

# 🔧 РУКОВОДСТВО ПО ОТЛАДКЕ И МОНИТОРИНГУ

## 📋 Логи для мониторинга

### При запуске сервера

Вы должны увидеть:
```
[GameServer] ✅ Global GameServer instance created
WebSocket server started on port 5555
```

### При подключении клиента

```
Request received: GET /
✅ WebSocket request detected
Client connected. Total connections: 1
[HandleConnect] Creating player: PlayerName (uuid-xxx)
[HandleConnect] Adding player to GameManager...
[HandleConnect] Sending connected message...
[HandleConnect] Sending rooms list...
✅ Player connected: PlayerName (uuid-xxx)
```

### При создании лобби

```
[CreateRoom] Creating room: RoomName, max players: 2
[CreateRoom] Room created with ID: room-uuid-xxx
[CreateRoom] Sent RoomCreated message to creator
[CreateRoom] Broadcasting rooms list to all clients
[BroadcastRoomsList] Found 1 rooms
[BroadcastRoomsList] Sending to 2 connected clients
[SendAsync] Sending RoomsList: {...}
[SendAsync] ✅ Message sent
[SendAsync] ✅ Message sent
[BroadcastRoomsList] ✅ Broadcast complete
```

---

## 🐛 Типичные проблемы и решение

### Проблема 1: "Room not found" при присоединении

**Логи:**
```
[JoinRoom] Checking room room-id...
Room not found
```

**Причина:** Room был удалён или ID неправильный

**Решение:**
```csharp
// Добавить логирование в FindRoomById()
public Room? FindRoomById(string roomId)
{
    Console.WriteLine($"[FindRoomById] Searching for room: {roomId}");
    Console.WriteLine($"[FindRoomById] Available rooms: {string.Join(", ", 
        Rooms.Select(r => $"{r.Id}({r.Name})"))}");
    
    var room = Rooms.FirstOrDefault(r => r.Id == roomId);
    Console.WriteLine($"[FindRoomById] Found: {(room != null ? "YES" : "NO")}");
    return room;
}
```

### Проблема 2: "No rooms" в списке, хотя создавались

**Логи:**
```
[CreateRoom] Room created with ID: xxx
[BroadcastRoomsList] Found 0 rooms ❌
```

**Причина:** GetRooms() фильтрует по IsGameStarted

**Решение:** Проверить логику в GetRooms():
```csharp
public List<Room> GetRooms()
{
    // Вернуть только комнаты, где игра НЕ начата
    return Rooms.Where(r => !r.IsGameStarted).ToList();
}

// Помните: если комната полна, IsGameStarted = true!
```

### Проблема 3: Некоторые клиенты не получают обновление

**Логи:**
```
[BroadcastRoomsList] Sending to 3 connected clients
[SendAsync] ✅ Message sent  ← только 2 раза
```

**Причина:** Одно соединение отключилось во время broadcast'а

**Решение:** Обработать исключения:
```csharp
foreach (var connection in allConnections)
{
    try
    {
        await connection.SendAsync(roomsMessage);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to send to connection: {ex.Message}");
    }
}
```

### Проблема 4: "Player already in game" ошибка

**Логи:**
```
[JoinRoom] Player already in room
```

**Причина:** Player попытался присоединиться к другой комнате, не выйдя из первой

**Решение:** Проверить логику в JoinRoom():
```csharp
public void JoinRoom(Player player, Room room)
{
    // Проверить, не в другой ли комнате
    var otherRoom = Rooms.FirstOrDefault(r => 
        r != room && r.Players.Contains(player));
    
    if (otherRoom != null)
    {
        throw new InvalidOperationException(
            $"Player already in room {otherRoom.Name}");
    }
    
    // Теперь можно добавить в новую комнату
    if (!room.Players.Contains(player))
    {
        room.Players.Add(player);
    }
}
```

---

## 🧪 Команды для тестирования

### Проверка через PowerShell

```powershell
# 1. Проверить, что порт открыт
Test-NetConnection -ComputerName localhost -Port 5555

# 2. Запустить сервер
cd "e:\Новая папка\Прога\ООП\sea\battle_of_sea-UI_test\battle_of_sea\battle_of_sea"
dotnet run

# 3. В другом окне PowerShell - проверить соединение
$ws = New-WebSocket -Uri "ws://localhost:5555"
$ws.SendAsync(@{type="ping"})
```

### Проверка через curl

```bash
# Попытаться подключиться (будет ошибка, но проверит порт)
curl -i http://localhost:5555

# Должна быть ошибка 400 (Bad Request)
# но это означает, что сервер работает!
```

---

## 📊 Мониторинг в реальном времени

### Добавить счётчики в GameManager

```csharp
public class GameManager
{
    private int _roomsCreated = 0;
    private int _playersConnected = 0;
    private int _gamesStarted = 0;

    public void PrintStats()
    {
        Console.WriteLine($"=== GAME STATS ===");
        Console.WriteLine($"Rooms: {Rooms.Count} (created: {_roomsCreated})");
        Console.WriteLine($"Players: {Players.Count}");
        Console.WriteLine($"Active Games: {ActiveGames.Count} (started: {_gamesStarted})");
        Console.WriteLine($"Free rooms: {Rooms.Count(r => !r.IsGameStarted)}");
    }

    public void AddPlayer(Player player)
    {
        AllPlayers[player.Id] = player;
        if (!Players.Contains(player))
        {
            Players.Add(player);
            _playersConnected++;
        }
    }

    public Room CreateRoom(string name, int maxPlayers)
    {
        var room = new Room(name, maxPlayers);
        Rooms.Add(room);
        _roomsCreated++;
        return room;
    }
}
```

### Вывести статистику каждые 30 секунд

```csharp
// В Program.cs
_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(30000); // каждые 30 секунд
        GameServer.Instance.GameManager.PrintStats();
    }
});

// Запустить сервер
var server = new WebSocketListener(5555);
await server.StartAsync();
```

---

## 🔍 Дополнительная отладка

### Вывести все лобби

```csharp
// Добавить метод в GameManager
public void PrintRooms()
{
    Console.WriteLine($"\n=== ROOMS ({Rooms.Count}) ===");
    foreach (var room in Rooms)
    {
        Console.WriteLine($"[{room.Id}] {room.Name}");
        Console.WriteLine($"  Players: {room.Players.Count}/{room.MaxPlayers}");
        Console.WriteLine($"  Started: {room.IsGameStarted}");
    }
    Console.WriteLine();
}

// Вызывать в нужных местах
GameServer.Instance.GameManager.PrintRooms();
```

### Вывести всех игроков

```csharp
public void PrintPlayers()
{
    Console.WriteLine($"\n=== PLAYERS ({Players.Count}) ===");
    foreach (var player in Players)
    {
        var game = FindGameByPlayerId(player.Id);
        Console.WriteLine($"[{player.Id}] {player.Name}");
        Console.WriteLine($"  Game: {(game != null ? "PLAYING" : "IDLE")}");
    }
    Console.WriteLine();
}
```

### Вывести все активные игры

```csharp
public void PrintGames()
{
    Console.WriteLine($"\n=== ACTIVE GAMES ({ActiveGames.Count}) ===");
    foreach (var game in ActiveGames)
    {
        Console.WriteLine($"[{game.GetCurrentPlayer().Name}] vs [{game.GetOpponentPlayer().Name}]");
        Console.WriteLine($"  Current turn: {game.CurrentTurnPlayerId}");
        Console.WriteLine($"  Finished: {game.IsFinished}");
    }
    Console.WriteLine();
}
```

---

## 🎯 Отладочная сессия

### Шаг за шагом отладка

```csharp
// 1. Проверить синглтон
var server1 = GameServer.Instance;
var server2 = GameServer.Instance;
Console.WriteLine($"Same instance? {ReferenceEquals(server1, server2)}"); // True

// 2. Проверить GameManager
var gm = GameServer.Instance.GameManager;
Console.WriteLine($"GameManager created? {gm != null}"); // True

// 3. Создать тестовую комнату
var room = gm.CreateRoom("TestRoom", 2);
Console.WriteLine($"Room created? {room != null}"); // True
Console.WriteLine($"Room ID: {room.Id}");

// 4. Найти комнату
var foundRoom = gm.FindRoomById(room.Id);
Console.WriteLine($"Room found? {foundRoom != null}"); // True
Console.WriteLine($"Same room? {ReferenceEquals(room, foundRoom)}"); // True

// 5. Получить список комнат
var rooms = gm.GetRooms();
Console.WriteLine($"Rooms count: {rooms.Count}"); // 1

// 6. Распечатать статистику
gm.PrintRooms();
gm.PrintStats();
```

---

## 💾 Логирование в файл

### Перенаправить консоль в файл

```csharp
// В Program.cs добавить:
var logFile = File.CreateText("server_logs.txt");
Console.SetOut(logFile);

Console.WriteLine("[SERVER START] " + DateTime.Now);

var server = new WebSocketListener(5555);
await server.StartAsync();
```

### Асинхронное логирование

```csharp
public static class Logger
{
    private static readonly Queue<string> _logQueue = new();
    private static readonly SemaphoreSlim _logSemaphore = new(1, 1);

    public static void Log(string message)
    {
        _ = Task.Run(async () =>
        {
            await _logSemaphore.WaitAsync();
            try
            {
                await File.AppendAllTextAsync("logs.txt", 
                    $"[{DateTime.Now:HH:mm:ss}] {message}\n");
                Console.WriteLine(message);
            }
            finally
            {
                _logSemaphore.Release();
            }
        });
    }
}

// Использование:
Logger.Log("Player connected: " + playerName);
```

---

## ✅ Проверочный список

При возникновении проблем проверьте:

- [ ] Сервер запустился? Видны ли логи "[GameServer] ✅"?
- [ ] Клиент подключился? Видны ли логи "Client connected"?
- [ ] Лобби создано? Видны ли логи "[CreateRoom]"?
- [ ] Broadcast отправлен? Видны ли логи "[BroadcastRoomsList]"?
- [ ] Сообщение отправлено? Видны ли логи "[SendAsync] ✅ Message sent"?
- [ ] Нет исключений? Проверьте консоль на ошибки
- [ ] Одно соединение? Проверьте количество в логах
- [ ] Один GameManager? Используется ли GameServer.Instance везде?

**Если все ✅ - проблема выявлена, можно решать!** 🎯

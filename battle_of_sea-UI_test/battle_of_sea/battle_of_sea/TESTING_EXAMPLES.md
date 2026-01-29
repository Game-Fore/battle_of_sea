# 🚀 ПРИМЕРЫ ИСПОЛЬЗОВАНИЯ И ТЕСТИРОВАНИЕ

## Как работает синглтон после исправления

### Пример 1: TCP клиент создаёт лобби

```csharp
// В ClientConnection.cs - HandleMessage("createroom")

// ✅ ПРАВИЛЬНО - используем глобальный синглтон
var room = GameServer.Instance.GameManager.CreateRoom(roomName, maxPlayers);

// За кулисами:
// 1. GameServer.Instance вернёт единственный экземпляр
// 2. GameManager добавит Room в Rooms список
// 3. Тот же самый список, который видят WebSocket клиенты!
```

### Пример 2: WebSocket клиент получает список лобби

```csharp
// В WebSocketListener.cs - HandleGetRooms()

// ✅ ПРАВИЛЬНО - используем тот же GameManager
var rooms = GameServer.Instance.GameManager.GetRooms();

// За кулисами:
// 1. GameServer.Instance вернёт ТОТ ЖЕ экземпляр
// 2. GetRooms() вернёт актуальный список
// 3. Включая комнаты, созданные TCP клиентами!
```

### Пример 3: Broadcast обновления

```csharp
// Когда создаётся новая комната:
await BroadcastRoomsList();

// ✅ ПРАВИЛЬНО - отправляет всем подключённым клиентам
var allConnections = GameServer.Instance.GetAllConnections();
foreach (var connection in allConnections)
{
    await connection.SendAsync(roomsMessage);
}

// Результат: и TCP, и WebSocket клиенты получат обновление!
```

---

## 📝 Сценарии тестирования

### Сценарий 1: Создание лобби одним типом, просмотр другим

**Шаги:**
1. Запустить сервер: `dotnet run`
2. Подключить TCP клиента
3. TCP клиент создаёт лобби "GameRoom1"
4. Подключить WebSocket клиента
5. WebSocket клиент запрашивает список лобби

**Ожидаемый результат:**
```
✅ WebSocket должен видеть "GameRoom1" в списке
```

**Логи сервера:**
```
[CreateRoom] Creating room: GameRoom1, max players: 2
[CreateRoom] Room created with ID: 12345...
[CreateRoom] Broadcasting rooms list to all clients
[BroadcastRoomsList] Found 1 rooms
[BroadcastRoomsList] Sending to 2 connected clients
[SendAsync] ✅ Message sent
```

---

### Сценарий 2: Синхронизация между клиентами

**Шаги:**
1. Запустить сервер
2. Подключить WebSocket клиента1
3. WebSocket клиента1 создаёт лобби "Arena"
4. Подключить TCP клиента
5. TCP клиент запрашивает список лобби

**Ожидаемый результат:**
```
✅ TCP должен видеть "Arena" в списке
```

**Логи сервера:**
```
[HandleConnect] Adding player to GameManager...
[HandleConnect] Sending connected message...
[HandleConnect] Sending rooms list...
[CreateRoom] Creating room: Arena, max players: 2
[BroadcastRoomsList] Found 1 rooms
[BroadcastRoomsList] Sending to 2 connected clients
[SendAsync] ✅ Message sent
```

---

### Сценарий 3: Присоединение к лобби

**Шаги:**
1. TCP создаёт лобби "BattleField"
2. WebSocket присоединяется к "BattleField"
3. Проверить, что оба игрока в одной игре

**Ожидаемый результат:**
```
✅ Игра начинается с двумя игроками
✅ Оба получают сообщение о начале игры
```

**Логи сервера:**
```
[CreateRoom] Room created with ID: abc...
[JoinRoom] Checking room abc...
[GameManager.JoinRoom] Player 2 joins room
JoinRoom: room.Players.Count >= room.MaxPlayers = true
Game started in room BattleField: Player1 vs Player2
```

---

## 🧪 Проверка потокобезопасности

### Проверка 1: Одновременное создание лобби

```csharp
// Имитировать множество одновременных запросов:

Parallel.For(0, 10, i =>
{
    var room = GameServer.Instance.GameManager.CreateRoom($"Room{i}", 2);
    Console.WriteLine($"Created room: {room.Id}");
});

// ✅ Все 10 комнат должны быть созданы без конфликтов
```

**Ожидаемый результат:**
```
Created room: xxx-1
Created room: xxx-2
...
Created room: xxx-10

// Все в GameManager.Rooms
// Никакие не потеряны благодаря lock'ам
```

### Проверка 2: Одновременное получение соединений

```csharp
// Broadcast в то время, как присоединяются новые клиенты:

Parallel.For(0, 5, i =>
{
    var allConnections = GameServer.Instance.GetAllConnections();
    Console.WriteLine($"Connections: {allConnections.Count}");
});

// ✅ Все операции безопасны благодаря lock в GetAllConnections()
```

---

## 📊 Как проверить, что работает правильно

### Проверка 1: Только один экземпляр GameManager

```csharp
// Можно добавить этот код в Program.cs для проверки:

var server1 = GameServer.Instance;
var server2 = GameServer.Instance;

Console.WriteLine($"Same instance? {ReferenceEquals(server1, server2)}");
// ✅ Должно быть: True
```

### Проверка 2: Синхронизация Rooms

```csharp
// В ClientConnection.cs
var roomsFromTCP = GameServer.Instance.GameManager.GetRooms();

// В WebSocketListener.cs  
var roomsFromWS = GameServer.Instance.GameManager.GetRooms();

Console.WriteLine($"Same rooms? {ReferenceEquals(roomsFromTCP, roomsFromWS)}");
// ✅ Должно быть: True (один и тот же список)
```

### Проверка 3: Broadcast доходит до всех

```csharp
// Добавить счётчик в BroadcastRoomsList():

Console.WriteLine($"Broadcasting to {GameServer.Instance.GetAllConnections().Count} clients");
foreach (var connection in GameServer.Instance.GetAllConnections())
{
    Console.WriteLine($"  - Sending to connection...");
    await connection.SendAsync(roomsMessage);
    Console.WriteLine($"  - ✅ Sent!");
}
```

**Ожидаемый вывод:**
```
Broadcasting to 2 clients
  - Sending to connection...
  - ✅ Sent!
  - Sending to connection...
  - ✅ Sent!
```

---

## ⚠️ Потенциальные проблемы и решения

### Проблема 1: NullReferenceException при GetAllConnections()

```
Error: Object reference not set to an instance of an object
```

**Причина:** RemoveConnection() вызвана после того, как соединение было удалено

**Решение:**
```csharp
public void RemoveConnection(WebSocketConnection connection)
{
    lock (_lock)
    {
        if (_allConnections.Contains(connection))
            _allConnections.Remove(connection);
    }
}
```

### Проблема 2: Лобби не обновляется после создания

```
Created room, but not in GetRooms()
```

**Причина:** BroadcastRoomsList() не вызвана

**Решение:**
```csharp
// В HandleCreateRoom() всегда вызывать:
await BroadcastRoomsList();
```

### Проблема 3: Слишком много соединений в памяти

```
Memory usage grows over time
```

**Причина:** RemoveConnection() не вызывается при отключении

**Решение:**
```csharp
// В HandleAsync() finally:
finally
{
    GameServer.Instance.RemoveConnection(this);
    // ...
}
```

---

## 🎯 Итоговый чеклист

- [ ] Проект компилируется без критических ошибок
- [ ] GameServer.Instance работает как синглтон
- [ ] TCP создаёт лобби → видно WebSocket
- [ ] WebSocket создаёт лобби → видно TCP
- [ ] Broadcast доходит до всех клиентов
- [ ] Потокобезопасность (lock) работает
- [ ] Нет утечек памяти в Connections
- [ ] Логи показывают корректные сообщения
- [ ] Игра может начаться с двумя игроками
- [ ] Перезагрузка сервера очищает лобби

✅ **Когда все пункты отмечены - исправление готово!**

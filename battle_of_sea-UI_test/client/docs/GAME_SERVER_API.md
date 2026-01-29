# Интеграция с игровым сервером

## Обзор

Клиент Battle of Sea интегрирован с двумя серверами:

1. **Сервер лобби/комнат** (порт 5000) - WebSocket для управления комнатами и игровой логики
2. **Игровой сервер** (порт 5000) - TCP для всех операций игры

## Клиент игрового сервера

### Класс: `GameServerClient`

Расположение: `Services/GameServerClient.cs`

### Подключение

```csharp
var gameServer = new GameServerClient("localhost", 5000);
await gameServer.ConnectAsync(userId);
```

### События

- `Connected` - успешное подключение
- `Disconnected` - отключение
- `ErrorOccurred` - ошибка
- `ShootResultReceived` - результат выстрела
- `GameStateChanged` - изменение состояния игры
- `JoinRoomResultReceived` - результат присоединения к комнате

## API запросы к игровому серверу

Все запросы отправляются в формате JSON, разделяются символом `\n`.

### 1. Подключение

**Действие:** `connect`

```json
{
  "action": "connect",
  "userId": "player1",
  "timestamp": "2026-01-20T12:00:00Z"
}
```

### 2. Присоединение к комнате

**Действие:** `joinRoom`

```json
{
  "action": "joinRoom",
  "userId": "player1",
  "roomId": "Room1",
  "timestamp": "2026-01-20T12:00:00Z"
}
```

**Ответ (событие `JoinRoomResultReceived`):**

```json
{
  "type": "JoinRoom",
  "success": true,
  "roomId": "Room1",
  "message": "Successfully joined room",
  "timestamp": "2026-01-20T12:00:00Z"
}
```

### 3. Размещение кораблей

**Действие:** `shipPlacement`

```json
{
  "action": "shipPlacement",
  "userId": "player1",
  "roomId": "Room1",
  "ships": [
    {
      "row": 0,
      "col": 0,
      "size": 4,
      "isHorizontal": true
    },
    {
      "row": 1,
      "col": 1,
      "size": 3,
      "isHorizontal": false
    }
  ],
  "timestamp": "2026-01-20T12:00:00Z"
}
```

Параметры:
- `row` (0-9) - строка начала корабля
- `col` (0-9) - столбец начала корабля
- `size` - размер корабля (1-4)
- `isHorizontal` - ориентация (true = горизонтально, false = вертикально)

### 4. Выстрел

**Действие:** `shoot`

```json
{
  "action": "shoot",
  "userId": "player1",
  "roomId": "Room1",
  "row": 3,
  "col": 5,
  "timestamp": "2026-01-20T12:00:00Z"
}
```

**Ответ (событие `ShootResultReceived`):**

```json
{
  "type": "ShootResult",
  "row": 3,
  "col": 5,
  "isHit": true,
  "isSunk": false,
  "isGameOver": false,
  "isWinner": false,
  "timestamp": "2026-01-20T12:00:00Z"
}
```

### 5. Готовность к игре

**Действие:** `ready`

```json
{
  "action": "ready",
  "userId": "player1",
  "roomId": "Room1",
  "timestamp": "2026-01-20T12:00:00Z"
}
```

### 6. Сдача

**Действие:** `surrender`

```json
{
  "action": "surrender",
  "userId": "player1",
  "roomId": "Room1",
  "timestamp": "2026-01-20T12:00:00Z"
}
```

**Ответ (событие `GameStateChanged`):**

```json
{
  "type": "GameState",
  "state": "OpponentSurrender",
  "roomId": "Room1",
  "timestamp": "2026-01-20T12:00:00Z"
}
```

### 7. Покидание комнаты

**Действие:** `leaveRoom`

```json
{
  "action": "leaveRoom",
  "userId": "player1",
  "roomId": "Room1",
  "timestamp": "2026-01-20T12:00:00Z"
}
```

### 8. Отключение

**Действие:** `disconnect`

```json
{
  "action": "disconnect",
  "userId": "player1",
  "timestamp": "2026-01-20T12:00:00Z"
}
```

## Типы ошибок

Ошибки отправляются как события `ErrorOccurred`:

```json
{
  "type": "Error",
  "message": "Invalid move",
  "timestamp": "2026-01-20T12:00:00Z"
}
```

## Поток событий игры

1. **Подключение** → `connect` → событие `Connected`
2. **Присоединение к комнате** → `joinRoom` → событие `JoinRoomResultReceived`
3. **Размещение кораблей** → `shipPlacement`
4. **Готовность** → `ready` → событие `GameStateChanged`
5. **Выстрелы** → `shoot` → событие `ShootResultReceived`
6. **Изменения состояния игры** → событие `GameStateChanged`
7. **Конец игры** → событие `GameStateChanged` (победа/поражение)
8. **Выход** → `leaveRoom` + `disconnect`

## Интеграция в GameViewModel

`GameServerClient` используется в `GameViewModel` для:
- Размещения кораблей (метод `SendShipPlacementAsync`)
- Выстрелов (метод `ShootAt`)
- Выхода из игры (метод `RequestExit`)

## Использование в коде

```csharp
// Автоматическое создание и подключение
var gameVM = new GameViewModel(room, networkService);

// Размещение кораблей - происходит при нажатии "Начать игру"
gameVM.PlaceShip(0, 0, 4, true);

// Выстрелы - происходит при клике на ячейку противника
await gameVM.ShootAt(boardCell);

// Выход - происходит при нажатии "Выйти в лобби"
gameVM.RequestExit();
```

## Примечания

- Все координаты 0-индексированы (от 0 до 9)
- Оборот сообщений - JSON строка, заканчивающаяся символом `\n`
- Все временные метки в формате ISO 8601 UTC
- При ошибке клиент автоматически пытается переподключиться

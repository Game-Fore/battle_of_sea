# 📊 Анализ использования GameManager

## Методы GameManager и их использование

### ✅ ИСПОЛЬЗУЕМЫЕ методы

| Метод | Используется где | Частота |
|-------|------------------|---------|
| `AddPlayer(player)` | HandleConnect (WebSocketListener.cs:297) | При подключении игрока |
| `FindGameByPlayerId(playerId)` | HandleShoot (x2), HandleReconnect, HandleChatMessage | При действиях в игре |
| `FindPlayerById(playerId)` | HandleReconnect, HandleChatMessage | При переподключении |
| `CreateRoom(name, maxPlayers)` | HandleCreateRoom | При создании комнаты |
| `FindRoomById(roomId)` | HandleJoinRoom | При присоединении к комнате |
| `GetRooms()` | HandleGetRooms | При запросе списка комнат |
| `JoinRoom(player, room)` | HandleJoinRoom | При присоединении игрока к комнате |

### ❌ НЕ ИСПОЛЬЗУЕМЫЕ методы

| Метод | Причина | Что нужно сделать |
|-------|---------|------------------|
| `RemoveGame(game)` | Никогда не вызывается | Должен вызваться при завершении игры |
| `WaitingPlayers` | Список заполняется, но не используется корректно | Логика смешана между JoinRoom и AddPlayer |
| `ActiveGames` | Заполняется, но игры не завершаются | Должны удаляться при окончании |

## 🔴 Проблемы

### 1. **Противоречивая логика создания игр**

**В `AddPlayer`:**
```csharp
// Если есть 2 игрока — создаём игру
if (WaitingPlayers.Count >= 2)
{
    var p1 = WaitingPlayers[0];
    var p2 = WaitingPlayers[1];
    WaitingPlayers.RemoveRange(0, 2);
    var game = new GameSession(p1, p2);
    ActiveGames.Add(game);
}
```

**В `JoinRoom`:**
```csharp
// Если комната полна, начинаем игру
if (room.Players.Count >= room.MaxPlayers)
{
    var game = new GameSession(room.Players[0], room.Players[1]);
    ActiveGames.Add(game);
    room.IsGameStarted = true;
}
```

**Проблема:** Непонятно, какой способ используется. Игра может создаваться двумя способами, что приводит к путанице.

### 2. **RemoveGame не вызывается**

Нет мест, где вызывается `RemoveGame()`. Это означает:
- ✗ ActiveGames растет бесконечно
- ✗ Утечка памяти
- ✗ Невозможно полностью завершить игру

### 3. **Логика AddPlayer конфликтует с комнатами**

- Игроки добавляются в WaitingPlayers при подключении
- Но затем они должны присоединиться к комнате через JoinRoom
- Логика смешана: не ясно, используется ли WaitingPlayers или Rooms

### 4. **Отсутствует обработка завершения игры**

Когда игра заканчивается:
- ✗ GameSession не удаляется из ActiveGames
- ✗ RemoveGame не вызывается
- ✗ Игроки не удаляются из комнаты

## ✨ РЕШЕНИЕ

### 1. **Унифицировать логику создания игр**
Использовать ТОЛЬКО комнаты для игр:
- При подключении НЕ создавать игру автоматически
- Игра создается ТОЛЬКО при JoinRoom, когда комната заполнена
- Удалить автоматическое создание из AddPlayer

### 2. **Реализовать завершение игры**
```csharp
public void FinishGame(GameSession game)
{
    RemoveGame(game);
    // Отправить уведомление игрокам
    Console.WriteLine($"Game finished: {game.Player1.Name} vs {game.Player2.Name}");
}
```

### 3. **Удалить WaitingPlayers** или использовать его правильно
- Либо удалить список
- Либо использовать его для очереди, если нет комнат

### 4. **Добавить очистку при отключении**
Когда игрок отключается, удалять его из:
- Комнаты (если там находится)
- ActiveGames (если в игре)
- WaitingPlayers (если ждет)

## 📝 РЕКОМЕНДАЦИИ

1. **Использовать комнаты как основную модель** ✓ Сейчас работает
2. **Удалить автоматическое создание игр в AddPlayer** - Это конфликтует с системой комнат
3. **Вызывать RemoveGame при завершении** - Нужно добавить обработчик в GameSession
4. **Добавить метод удаления игрока** - RemovePlayer для отключения
5. **Добавить метод очистки игры** - FinishGame для завершения

## 📍 Где это нужно исправить

- [x] GameManager.cs - Логика создания игр и удаления
- [x] GameSession.cs - Сигнал о завершении игры
- [x] WebSocketListener.cs - Обработка отключения и завершения

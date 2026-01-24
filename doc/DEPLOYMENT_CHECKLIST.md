# ✅ Чек-лист интеграции и развертывания

## 🔍 Проверка перед использованием

### Компиляция
- [x] Проект компилируется: `dotnet build`
- [x] Результат: 0 ошибок, 22 warning
- [x] Выходной файл: `/bin/Debug/net9.0/battle_of_sea.dll`

### Функциональность
- [x] CreateRoom создаёт комнату с UUID
- [x] JoinRoom проверяет существование комнаты
- [x] JoinRoom проверяет, не полна ли комната
- [x] JoinRoom проверяет, не запущена ли игра
- [x] PlayerReady отслеживает готовность обоих
- [x] GameStateChanged отправляется обоим при запуске
- [x] RoomsList содержит только доступные комнаты
- [x] Все сообщения в JSON формате
- [x] Логирование всех событий

---

## 📊 Проверка тестами

### Тест 1: Создание комнаты
```bash
# Запрос
{"Type":"CreateRoom","Payload":{"roomName":"Test"}}

# Ожидаемые ответы:
# 1. RoomCreated с roomId
# 2. RoomsList (трансляция) всем клиентам
```
- [ ] Комната создана
- [ ] UUID присвоен
- [ ] Статус PlayerReadyStatus инициализирован
- [ ] Трансляция получена

### Тест 2: Присоединение
```bash
# Запрос
{"Type":"JoinRoom","Payload":{"roomId":"xxx"}}

# Ожидаемый ответ:
# 1. JoinRoomResult с success: true
# 2. RoomsList (трансляция)
```
- [ ] Игрок добавлен в комнату
- [ ] success: true
- [ ] Список игроков содержит обоих
- [ ] Трансляция получена

### Тест 3: Ошибка присоединения
```bash
# Запрос к несуществующей комнате
{"Type":"JoinRoom","Payload":{"roomId":"fake"}}

# Ожидаемый ответ:
# JoinRoomResult с success: false, message: "Room not found"
```
- [ ] success: false
- [ ] message присутствует
- [ ] Клиент не добавлен в комнату

### Тест 4: Готовность обоих
```bash
# Player1
{"Type":"PlayerReady","Payload":{}}

# Ожидаемый ответ:
# PlayerReady (подтверждение)

# Player2
{"Type":"PlayerReady","Payload":{}}

# Ожидаемые ответы обоим:
# GameStateChanged с state: "GameStarted"
```
- [ ] Player1 помечен как готовый
- [ ] Player2 помечен как готовый
- [ ] GameStateChanged отправлен обоим
- [ ] state = "GameStarted"

### Тест 5: Логирование
Проверить на консоли сервера:
```
[ROOM_CREATED] Room Test (ID: xxx) created by Player1
[ROOM_JOINED] Player Player2 joined room Test
[PLAYER_READY] Player1 is ready in room Test
[PLAYER_READY] Player2 is ready in room Test
[GAME_STARTED] Game starting in room Test
[GAME_START] Game started in room Test: Player1 vs Player2
[BROADCAST] RoomsList sent to 2 players
```
- [ ] ROOM_CREATED логируется
- [ ] ROOM_JOINED логируется
- [ ] PLAYER_READY логируется 2 раза
- [ ] GAME_STARTED логируется
- [ ] GAME_START логируется
- [ ] BROADCAST логируется

---

## 🚀 Развертывание

### На локальной машине
```bash
cd /Users/masha/battle_of_sea/battle_of_sea/battle_of_sea
dotnet run
```
- [ ] Сервер запустился
- [ ] Слушает на localhost:5000
- [ ] Готов принимать подключения

### Подключение клиента
- [ ] Запустить клиент на адресе localhost:5000
- [ ] Сервер принял подключение
- [ ] Логирование "Client error" или "Player connected"

### Множественные клиенты
- [ ] 2 клиента подключены одновременно
- [ ] Оба работают независимо
- [ ] Оба получают трансляции

---

## 📋 Интеграция с клиентом

### Используемые типы сообщений

Проверить, что клиент отправляет (case-insensitive):
- [ ] CreateRoom
- [ ] JoinRoom
- [ ] PlayerReady
- [ ] RoomsList
- [ ] Connect (существует)
- [ ] Ping (существует)
- [ ] Shoot (существует)
- [ ] Reconnect (существует)

### Ожидаемые ответы от сервера

Проверить, что клиент ожидает (Type):
- [ ] RoomCreated
- [ ] JoinRoomResult
- [ ] PlayerReady
- [ ] GameStateChanged ← **ГЛАВНОЕ!**
- [ ] RoomsList
- [ ] connected (существует)
- [ ] pong (существует)
- [ ] ShootResult (существует)

### Payload структура

Проверить поля в Payload:
- [ ] CreateRoom: { roomName: string }
- [ ] JoinRoom: { roomId: string }
- [ ] PlayerReady: {} (пусто)
- [ ] RoomsList: {} (пусто)

- [ ] RoomCreated: { roomId, roomName, maxPlayers }
- [ ] JoinRoomResult: { success: bool, message?: string, ... }
- [ ] PlayerReady: { playerId, message }
- [ ] GameStateChanged: { state: string, roomId, players: [...] }
- [ ] RoomsList: { rooms: [...] }

---

## 🔍 Отладка

### Если GameStateChanged не отправляется

Проверить:
- [ ] Оба игрока в одной комнате
- [ ] Оба игрока отправили PlayerReady
- [ ] PlayerReadyStatus содержит обоих с true
- [ ] room.AreAllPlayersReady() == true
- [ ] CheckAndStartGame() возвращает true
- [ ] ActiveGames содержит новую игру
- [ ] room.IsGameStarted == true

### Если RoomsList не отправляется

Проверить:
- [ ] BroadcastRoomsList() вызывается
- [ ] GameManager.Players содержит всех клиентов
- [ ] Каждый player.Connection не null
- [ ] player.Connection is ClientConnection проходит
- [ ] SendAsync вызывается для каждого

### Если PlayerReady возвращает ошибку

Проверить:
- [ ] player._player != null (клиент подключен)
- [ ] FindRoomByPlayerId(playerId) не null
- [ ] room.PlayerReadyStatus инициализирован
- [ ] MarkPlayerReady() возвращает true

---

## 📊 Производительность

### Нагрузочное тестирование

- [ ] 2 игрока: работает
- [ ] 10 игроков (5 комнат): работает
- [ ] 50 игроков (25 комнат): работает
- [ ] 100 игроков: работает

### Проверки
- [ ] Нет утечек памяти
- [ ] Нет зависаний
- [ ] Логирование не замедляет сервер
- [ ] JSON сериализация быстра

---

## 🔐 Безопасность

- [ ] Валидация roomId перед использованием
- [ ] Валидация playerId перед использованием
- [ ] Проверка существования комнаты
- [ ] Проверка статуса комнаты (полная, запущена)
- [ ] Защита от null reference exception
- [ ] Обработка неверного JSON
- [ ] Обработка отключения клиента

---

## 📝 Документация

Проверить наличие файлов:
- [ ] README_ROOMS.md - главный индекс
- [ ] QUICKSTART.md - быстрый старт
- [ ] SERVER_API_REFERENCE.md - полный API
- [ ] TESTING_GUIDE.md - тестирование
- [ ] ARCHITECTURE.md - архитектура
- [ ] CODE_EXAMPLES.md - примеры кода
- [ ] IMPLEMENTATION_SUMMARY.md - итоговый отчет
- [ ] SERVER_ROOM_IMPLEMENTATION.md - технические детали

---

## ✨ Финальная проверка

### Компиляция
```bash
cd /Users/masha/battle_of_sea/battle_of_sea/battle_of_sea
dotnet build
```
- [ ] Build succeeded
- [ ] 0 Error(s)

### Запуск
```bash
dotnet run
```
- [ ] Сервер запустился
- [ ] Слушает на 0.0.0.0:5000

### Тестирование (Terminal 1)
```bash
nc localhost 5000
{"Type":"Connect","Payload":{"playerName":"P1"}}
{"Type":"CreateRoom","Payload":{"roomName":"R1"}}
{"Type":"PlayerReady","Payload":{}}
```
- [ ] Все ответы получены
- [ ] Логи корректны

### Тестирование (Terminal 2)
```bash
nc localhost 5000
{"Type":"Connect","Payload":{"playerName":"P2"}}
{"Type":"RoomsList","Payload":{}}
{"Type":"JoinRoom","Payload":{"roomId":"<из Term1>"}}
{"Type":"PlayerReady","Payload":{}}
```
- [ ] Присоединение успешно
- [ ] GameStateChanged получен
- [ ] Игра запущена

---

## 🎯 Успех = Когда

- ✅ Оба игрока получают `GameStateChanged` с `state: "GameStarted"`
- ✅ Сервер логирует `[GAME_START]`
- ✅ Клиент может начать игру (выстреливать)
- ✅ WaitingForOpponent состояние больше не зависает

---

## 🚀 Go Live!

Когда все чек-боксы отмечены:
1. Push код в production
2. Запусти сервер
3. Запусти клиентов
4. Нажми "Начать игру"
5. 🎮 Играй!

---

## 📞 Контакт

Если что-то не работает:
1. Проверь логи сервера
2. Смотри [TESTING_GUIDE.md](./TESTING_GUIDE.md)
3. Используй примеры из [CODE_EXAMPLES.md](./CODE_EXAMPLES.md)
4. Прочитай [ARCHITECTURE.md](./ARCHITECTURE.md) для понимания

---

**✨ Все готово! Удачного развертывания! ✨**

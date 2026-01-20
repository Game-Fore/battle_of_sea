SERVER REQUESTS — запросы к серверу с кнопок

Сервер: ws://localhost:5500

Кнопки и соответствующие запросы:

1) ПОДКЛЮЧЕНИЕ (App Start / Login Button)
   Запрос: Connect
   {
     "type": "Connect",
     "userId": "user123",
     "displayName": "PlayerName"
   }
   Ответ ожидается: UserConnected

2) СПИСОК КОМНАТ (Refresh / Show Rooms Button)
   Запрос: GetRooms
   {
     "type": "GetRooms"
   }
   Ответ ожидается: RoomsList

3) СОЗДАНИЕ КОМНАТЫ (Create Room Button)
   Запрос: CreateRoom
   {
     "type": "CreateRoom",
     "roomName": "R1",
     "maxPlayers": 2,
     "password": null
   }
   Ответ ожидается: JoinRoom с success=true

4) ПРИСОЕДИНЕНИЕ К КОМНАТЕ (Join Button)
   Запрос: JoinRoom
   {
     "type": "JoinRoom",
     "roomId": "R1",
     "userId": "user123",
     "password": null
   }
   Ответ ожидается: JoinRoom с success=true

5) РАЗМЕЩЕНИЕ КОРАБЛЕЙ (Start Game Button)
   Запрос: ShipPlacement
   {
     "type": "ShipPlacement",
     "roomId": "R1",
     "userId": "user123",
     "ships": [
       { "row": 0, "col": 0, "size": 4, "isHorizontal": true },
       { "row": 2, "col": 3, "size": 3, "isHorizontal": false }
     ]
   }
   Ответ ожидается: GameState (сервер подтверждает готовность)

6) ВЫСТРЕЛ (Click on Enemy Cell / Shoot Button)
   Запрос: Shoot
   {
     "type": "Shoot",
     "roomId": "R1",
     "userId": "user123",
     "row": 3,
     "col": 4
   }
   Ответ ожидается: ShootResult

7) ВЫХОД ИЗ КОМНАТЫ (Leave / Back Button)
   Запрос: LeaveRoom
   {
     "type": "LeaveRoom",
     "roomId": "R1",
     "userId": "user123"
   }
   Ответ ожидается: success message или GameState обновление

8) ОТКЛЮЧЕНИЕ (Exit / Logout Button)
   Запрос: Disconnect
   Действие: закрыть WebSocket соединение
   Ответ ожидается: none (соединение закрыто)

СОБЫТИЯ ОТ СЕРВЕРА (без запроса от клиента):
- OpponentShoot: сервер отправляет ход противника
  {
    "type": "OpponentShoot",
    "row": 2,
    "col": 5,
    "roomId": "R1"
  }

- GameState: сервер отправляет обновление состояния игры
  {
    "type": "GameState",
    "state": "YourTurn",
    "roomId": "R1"
  }

ПРИМЕЧАНИЕ: Порт сервера установлен на 5500. Для изменения используйте конструктор NetworkService:
  var service = new NetworkService("localhost", 5500);

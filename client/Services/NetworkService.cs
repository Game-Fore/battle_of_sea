// Реал WS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BattleOfSea.Models;

namespace BattleOfSea.Services
{
    // Реальный WebSocket сетевой сервис
    public class NetworkService : INetworkService
    {
        private ClientWebSocket? _webSocket;
        private readonly string _serverUrl;
        private bool _isConnected;
        private CancellationTokenSource? _cancellationTokenSource;
        private string? _currentUserId;
        private string? _currentRoomId;

        // Флаг подключения к серверу (публичное свойство)
        public bool IsConnected => _isConnected;

        // Событие получения результата выстрела (публичное событие)
        public event Action<ShootResultMessage>? ShootResultReceived;
        // Событие получения выстрела противника (публичное событие)
        public event Action<ShootMessage>? OpponentShootReceived;
        // Событие изменения состояния игры (публичное событие)
        public event Action<GameStateMessage>? GameStateChanged;
        // Событие обновления списка комнат (публичное событие)
        public event Action<RoomsListMessage>? RoomsListUpdated;
        // Событие результата присоединения к комнате (публичное событие)
        public event Action<JoinRoomMessage>? JoinRoomResult;
        // Событие подключения пользователя (публичное событие)
        public event Action<UserConnectedMessage>? UserConnected;
        // Событие получения сообщения чата (публичное событие)
        public event Action<ChatMessage>? ChatMessageReceived;
        // Событие ошибки соединения (публичное событие)
        public event Action<string>? ConnectionError;

        // Конструктор сетевого сервиса (публичный)
        public NetworkService(string serverHost = "localhost", int serverPort = 5555)
        {
            _serverUrl = $"ws://{serverHost}:{serverPort}";
            _isConnected = false;
        }

        // Подключение асинхр (публичный метод)
        public async Task<bool> ConnectAsync(string userId, string displayName)
        {
            try
            {
                Console.WriteLine($"[NetworkService] Attempting to connect to {_serverUrl}");
                
                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource();
                
                Console.WriteLine($"[NetworkService] Connecting to WebSocket...");
                await _webSocket.ConnectAsync(
                    new Uri(_serverUrl), 
                    _cancellationTokenSource.Token
                );
                
                Console.WriteLine($"[NetworkService] ✅ WebSocket connected! State: {_webSocket.State}");

                _isConnected = true;
                _currentUserId = userId;

                // Отправляем данные пользователя на сервер
                var connectMessage = new
                {
                    type = "Connect",
                    userId = userId,
                    displayName = displayName
                };

                Console.WriteLine($"[NetworkService] Sending Connect message...");
                await SendMessageAsync(connectMessage);
                Console.WriteLine($"[NetworkService] Connect message sent");

                // Даем серверу время на обработку Connect
                await Task.Delay(100);
                
                // Запускаем слушатель сообщений
                _ = ListenForMessagesAsync();

                // Просим сервер отправить список комнат
                await GetRoomsAsync();

                UserConnected?.Invoke(new UserConnectedMessage
                {
                    UserId = userId,
                    DisplayName = displayName,
                    Type = "UserConnected"
                });

                Console.WriteLine($"[NetworkService] Connected successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NetworkService] ❌ Connection error: {ex.Message}");
                Console.WriteLine($"[NetworkService] Exception type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[NetworkService] Inner exception: {ex.InnerException.Message}");
                
                ConnectionError?.Invoke($"Failed to connect: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        // Отключение асинхр (публичный метод)
        public async Task DisconnectAsync()
        {
            try
            {
                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing connection",
                        CancellationToken.None
                    );
                }

                _cancellationTokenSource?.Cancel();
                _webSocket?.Dispose();
                _isConnected = false;
                _currentUserId = null;
                _currentRoomId = null;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Disconnection error: {ex.Message}");
            }
        }

        // Получение списка комнат (публичный метод)
        public async Task<List<Room>> GetRoomsAsync()
        {
            try
            {
                Console.WriteLine($"[GetRoomsAsync] Requesting rooms from server...");
                var message = new { type = "GetRooms" };
                await SendMessageAsync(message);
                Console.WriteLine($"[GetRoomsAsync] GetRooms request sent");
                
                // Сервер отправит RoomsListMessage через WebSocket
                // Это обработается в ListenForMessagesAsync -> HandleRoomsListMessage
                // -> RoomsListUpdated.Invoke()
                return new List<Room>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetRoomsAsync] ❌ Error getting rooms: {ex.Message}");
                ConnectionError?.Invoke($"Failed to get rooms: {ex.Message}");
                return new List<Room>();
            }
        }

        // Создание комнаты (публичный метод)
        public async Task<bool> CreateRoomAsync(Room room, string? password = null)
        {
            try
            {
                var message = new
                {
                    type = "CreateRoom",
                    roomName = room.Name,
                    maxPlayers = room.MaxPlayers,
                    password = password
                };

                await SendMessageAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating room: {ex.Message}");
                ConnectionError?.Invoke($"Failed to create room: {ex.Message}");
                return false;
            }
        }

        // Присоединение к комнате (публичный метод)
        public async Task<bool> JoinRoomAsync(Room room, string? password = null)
        {
            try
            {
                Console.WriteLine($"[DEBUG] ============ JoinRoomAsync START ============");
                Console.WriteLine($"[DEBUG] Input room: Name={room.Name}, Id={room.Id}");
                
                // Use room.Id when communicating with server (server rooms identified by Id)
                _currentRoomId = room.Id;
                Console.WriteLine($"[DEBUG] Set _currentRoomId = '{_currentRoomId}'");
                Console.WriteLine($"[DEBUG] _currentUserId = '{_currentUserId}'");
                
                var message = new
                {
                    type = "JoinRoom",
                    roomId = room.Id,
                    userId = _currentUserId,
                    password = password
                };

                string jsonDebug = JsonSerializer.Serialize(message);
                Console.WriteLine($"[DEBUG] JoinRoom JSON = {jsonDebug}");
                
                await SendMessageAsync(message);
                Console.WriteLine($"[DEBUG] ============ JoinRoomAsync END ============");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] JoinRoomAsync: {ex.Message}");
                ConnectionError?.Invoke($"Ошибка присоединения: {ex.Message}");
                return false;
            }
        }

        // Выход из комнаты (публичный метод)
        public async Task LeaveRoomAsync()
        {
            try
            {
                var message = new
                {
                    type = "LeaveRoom",
                    roomId = _currentRoomId,
                    userId = _currentUserId
                };

                await SendMessageAsync(message);
                _currentRoomId = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error leaving room: {ex.Message}");
                ConnectionError?.Invoke($"Failed to leave room: {ex.Message}");
            }
        }

        // Отправить выстрел (публичный метод)
        public async Task<bool> SendShootAsync(int row, int col, string roomId)
        {
            try
            {
                var message = new
                {
                    type = "Shoot",
                    roomId = roomId,
                    userId = _currentUserId,
                    row = row,
                    col = col
                };

                await SendMessageAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending shoot: {ex.Message}");
                ConnectionError?.Invoke($"Failed to send shoot: {ex.Message}");
                return false;
            }
        }

        // Отправить расстановку кораблей (публичный метод)
        public async Task<bool> SendShipPlacementAsync(List<ShipPlacementData> ships, string roomId)
        {
            try
            {
                var message = new
                {
                    type = "ShipPlacement",
                    roomId = roomId,
                    userId = _currentUserId,
                    ships = ships
                };

                await SendMessageAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending ship placement: {ex.Message}");
                ConnectionError?.Invoke($"Failed to send ship placement: {ex.Message}");
                return false;
            }
        }

        // Отправить сигнал готовности (публичный метод)
        public async Task<bool> SendPlayerReadyAsync(string roomId)
        {
            try
            {
                Console.WriteLine($"[DEBUG] ============ SendPlayerReadyAsync START ============");
                Console.WriteLine($"[DEBUG] Input params: roomId='{roomId}'");
                Console.WriteLine($"[DEBUG] Instance state: _currentUserId='{_currentUserId}', _currentRoomId='{_currentRoomId}'");
                Console.WriteLine($"[DEBUG] WebSocket state: {_webSocket?.State}");
                
                // Валидация roomId
                if (string.IsNullOrEmpty(roomId) || roomId == "*")
                {
                    Console.WriteLine($"[ERROR] SendPlayerReadyAsync: roomId некорректен = '{roomId}'. Используем _currentRoomId.");
                    roomId = _currentRoomId ?? "";
                }
                
                if (string.IsNullOrEmpty(roomId))
                {
                    throw new InvalidOperationException("roomId не установлен. Сначала присоедините к комнате!");
                }

                Console.WriteLine($"[DEBUG] Final roomId for message: '{roomId}'");
                Console.WriteLine($"[DEBUG] Final userId for message: '{_currentUserId}'");

                var message = new
                {
                    type = "PlayerReady",
                    roomId = roomId,
                    userId = _currentUserId
                };

                string jsonDebug = JsonSerializer.Serialize(message);
                Console.WriteLine($"[DEBUG] PlayerReady JSON = {jsonDebug}");
                Console.WriteLine($"[DEBUG] Calling SendMessageAsync...");
                
                await SendMessageAsync(message);
                Console.WriteLine($"[NetworkService] ✅ PlayerReady отправлена для комнаты {roomId}");
                Console.WriteLine($"[DEBUG] ============ SendPlayerReadyAsync END ============");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SendPlayerReadyAsync: {ex.Message}");
                ConnectionError?.Invoke($"Ошибка отправки PlayerReady: {ex.Message}");
                return false;
            }
        }

        // Отправить сообщение чата (публичный метод)
        public async Task<bool> SendChatMessageAsync(string text, string? roomId = null)
        {
            try
            {
                var message = new
                {
                    type = "ChatMessage",
                    userId = _currentUserId,
                    text = text,
                    roomId = roomId ?? _currentRoomId,
                    timestamp = DateTime.UtcNow.ToString("O")
                };

                await SendMessageAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending chat message: {ex.Message}");
                ConnectionError?.Invoke($"Failed to send chat message: {ex.Message}");
                return false;
            }
        }

        // Отправить сообщение на сервер (приватный метод)
        private async Task SendMessageAsync(object message)
        {
            Console.WriteLine($"[SendMessage] WebSocket state: {_webSocket?.State}");
            if (_webSocket?.State != WebSocketState.Open)
            {
                Console.WriteLine($"[SendMessage] ❌ WebSocket is not connected! State: {_webSocket?.State}");
                throw new InvalidOperationException("WebSocket is not connected");
            }

            var json = JsonSerializer.Serialize(message);
            Console.WriteLine($"[DEBUG] RAW JSON OUT = {json}");
            Console.WriteLine($"[SendMessage] Sending: {json}");
            var buffer = Encoding.UTF8.GetBytes(json);

            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            
            Console.WriteLine($"[SendMessage] ✅ Message sent");

        }

        // Прослушивание сообщений от сервера (приватный метод)
        private async Task ListenForMessagesAsync()
        {
            try
            {
                var buffer = new byte[1024 * 4];

                while (_webSocket?.State == WebSocketState.Open && _cancellationTokenSource?.Token.IsCancellationRequested == false)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource.Token
                    );

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        HandleServerMessage(json);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            CancellationToken.None
                        );
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Listener cancelled normally; no log needed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in message listener: {ex.Message}");
                ConnectionError?.Invoke($"Connection lost: {ex.Message}");
                _isConnected = false;
            }
        }

        // Обработать сообщение от сервера (приватный метод)
        private void HandleServerMessage(string json)
        {
            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    
                    // Пытаемся получить "type" или "Type" (case-insensitive)
                    string? type = null;
                    if (root.TryGetProperty("type", out var typeElem))
                    {
                        type = typeElem.GetString();
                    }
                    else if (root.TryGetProperty("Type", out typeElem))
                    {
                        type = typeElem.GetString();
                    }
                    
                    if (string.IsNullOrEmpty(type))
                    {
                        Console.WriteLine($"[HandleServerMessage] ❌ Message has no 'type' field: {json.Substring(0, Math.Min(100, json.Length))}");
                        return;
                    }

                    switch (type.ToLower())
                    {
                        case "connected":
                        case "roomscreated":
                        case "roomcreated":
                            // These are just confirmations, we can ignore or log them
                            Console.WriteLine($"[HandleServerMessage] Received {type} confirmation");
                            break;
                        case "roomslist":
                            HandleRoomsListMessage(json);
                            break;
                        case "joinroom":
                            HandleJoinRoomMessage(json);
                            break;
                        case "shootresult":
                            HandleShootResultMessage(json);
                            break;
                        case "opponentshoot":
                            HandleOpponentShootMessage(json);
                            break;
                        case "gamestate":
                            HandleGameStateMessage(json);
                            break;
                        case "gamestart":
                            HandleGameStartMessage(json);
                            break;
                        case "gamechanged":
                        case "gamestatechanged":
                            Console.WriteLine($"[ListenForMessagesAsync] ✅ Received gamechanged/gamestatechanged message");
                            HandleGameStateChangedMessage(json);
                            break;
                        case "chatmessage":
                            HandleChatMessage(json);
                            break;
                        case "error":
                            Console.WriteLine($"[HandleServerMessage] Server error");
                            break;
                        default:
                            // Unknown message type - ignore or log in future if needed
                            Console.WriteLine($"[HandleServerMessage] Unknown message type: {type}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling message: {ex.Message}");
                Console.WriteLine($"Error stack trace: {ex.StackTrace}");
            }
        }

        // Обработка списка комнат (приватный метод)
        private void HandleRoomsListMessage(string json)
        {
            try
            {
                Console.WriteLine($"[HandleRoomsListMessage] Raw JSON: {json}");
                Console.WriteLine($"[HandleRoomsListMessage] Parsing rooms list...");
                
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    Console.WriteLine($"[HandleRoomsListMessage] Root ValueKind: {root.ValueKind}");
                    
                    // Try both "Payload" and "payload"
                    JsonElement payloadElem = default;
                    if (!root.TryGetProperty("Payload", out payloadElem) && !root.TryGetProperty("payload", out payloadElem))
                    {
                        Console.WriteLine($"[HandleRoomsListMessage] ❌ No 'Payload' or 'payload' found in root");
                        return;
                    }
                    
                    Console.WriteLine($"[HandleRoomsListMessage] Found payload");
                    if (payloadElem.TryGetProperty("rooms", out var roomsElem))
                    {
                        Console.WriteLine($"[HandleRoomsListMessage] Found rooms array with {roomsElem.GetArrayLength()} elements");
                        var rooms = new List<Room>();
                        foreach (var roomElem in roomsElem.EnumerateArray())
                        {
                            Console.WriteLine($"[HandleRoomsListMessage] Parsing room: {roomElem}");
                            var room = new Room(
                                roomElem.GetProperty("Name").GetString() ?? "Unknown",
                                roomElem.GetProperty("Players").GetInt32(),
                                roomElem.GetProperty("MaxPlayers").GetInt32()
                            );
                            
                            if (roomElem.TryGetProperty("Id", out var idElem))
                            {
                                room.Id = idElem.GetString() ?? "";
                            }
                            
                            rooms.Add(room);
                            Console.WriteLine($"[HandleRoomsListMessage] Added room: {room.Name} ({room.Players}/{room.MaxPlayers})");
                        }
                        
                        Console.WriteLine($"[HandleRoomsListMessage] ✅ Got {rooms.Count} rooms");
                        
                        var message = new RoomsListMessage { Rooms = rooms };
                        RoomsListUpdated?.Invoke(message);
                    }
                    else
                    {
                        Console.WriteLine($"[HandleRoomsListMessage] ❌ No 'rooms' array in payload");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HandleRoomsListMessage] ❌ Error deserializing rooms list: {ex.Message}");
                Console.WriteLine($"[HandleRoomsListMessage] Stack trace: {ex.StackTrace}");
            }
        }

        // Обработка присоединения к комнате (приватный метод)
        private void HandleJoinRoomMessage(string json)
    {
        try
        {
            Console.WriteLine($"[HandleJoinRoomMessage] ✅ Joined room successfully");
            var message = JsonSerializer.Deserialize<JoinRoomMessage>(json);
            if (message != null)
            {
                // 🔧 FIX: если сервер не прислал RoomId — используем локальный
                if (string.IsNullOrWhiteSpace(message.RoomId))
                {
                    Console.WriteLine(
                        $"[HandleJoinRoomMessage] ⚠️ Server did not send RoomId. Using local _currentRoomId = '{_currentRoomId}'"
                    );
                    message.RoomId = _currentRoomId;
                }
                else
                {
                    _currentRoomId = message.RoomId;
                    Console.WriteLine(
                        $"[HandleJoinRoomMessage] ✅ RoomId received from server = '{_currentRoomId}'"
                    );
                }

                JoinRoomResult?.Invoke(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HandleJoinRoomMessage] ❌ Error deserializing join room: {ex.Message}");
        }
    }


        // Результат выстрела (приватный метод)
        private void HandleShootResultMessage(string json)
        {
            try
            {
                var message = JsonSerializer.Deserialize<ShootResultMessage>(json);
                if (message != null)
                {
                    ShootResultReceived?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing shoot result: {ex.Message}");
            }
        }

        // Ход противника (приватный метод)
        private void HandleOpponentShootMessage(string json)
        {
            try
            {
                var message = JsonSerializer.Deserialize<ShootMessage>(json);
                if (message != null)
                {
                    OpponentShootReceived?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing opponent shoot: {ex.Message}");
            }
        }

        // Обработка состояния игры (приватный метод)
        private void HandleGameStateMessage(string json)
        {
            try
            {
                var message = JsonSerializer.Deserialize<GameStateMessage>(json);
                if (message != null)
                {
                    GameStateChanged?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing game state: {ex.Message}");
            }
        }

        private void HandleGameStateChangedMessage(string json)
        {
            try
            {
                Console.WriteLine($"[DEBUG] ============ HandleGameStateChangedMessage START ============");
                Console.WriteLine($"[HandleGameStateChangedMessage] ✅ Received game state change message");
                Console.WriteLine($"[HandleGameStateChangedMessage] Raw JSON: {json}");
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    var propNames = string.Join(", ", root.EnumerateObject().Select(p => p.Name));
                    Console.WriteLine($"[DEBUG] Root properties: {propNames}");
                    Console.WriteLine($"[HandleGameStateChangedMessage] Root properties: {propNames}");
                    
                    if (root.TryGetProperty("Payload", out var payloadElem) || root.TryGetProperty("payload", out payloadElem))
                    {
                        Console.WriteLine($"[DEBUG] Found payload element");
                        Console.WriteLine($"[HandleGameStateChangedMessage] Found payload");
                        if (payloadElem.TryGetProperty("state", out var stateElem))
                        {
                            var state = stateElem.GetString();
                            Console.WriteLine($"[DEBUG] Parsed state: '{state}'");
                            Console.WriteLine($"[HandleGameStateChangedMessage] ✅ New state: {state}");
                            
                            var gameStateMessage = new GameStateMessage
                            {
                                Type = "GameState",
                                State = state ?? "WaitingForOpponent"
                            };
                            Console.WriteLine($"[DEBUG] GameStateMessage created: Type='{gameStateMessage.Type}', State='{gameStateMessage.State}'");
                            Console.WriteLine($"[DEBUG] Invoking GameStateChanged event with state: {gameStateMessage.State}");
                            Console.WriteLine($"[HandleGameStateChangedMessage] Invoking GameStateChanged event with state: {gameStateMessage.State}");
                            
                            GameStateChanged?.Invoke(gameStateMessage);
                            
                            Console.WriteLine($"[DEBUG] ✅ GameStateChanged event invoked successfully");
                        }
                        else
                        {
                            Console.WriteLine($"[DEBUG] ❌ No 'state' property in payload");
                            Console.WriteLine($"[HandleGameStateChangedMessage] ❌ No 'state' property in payload");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] ❌ No Payload found in message");
                        Console.WriteLine($"[HandleGameStateChangedMessage] ❌ No Payload found");
                    }
                }
                Console.WriteLine($"[DEBUG] ============ HandleGameStateChangedMessage END ============");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] ============ HandleGameStateChangedMessage EXCEPTION ============");
                Console.WriteLine($"[HandleGameStateChangedMessage] ❌ Error: {ex.Message}");
                Console.WriteLine($"[HandleGameStateChangedMessage] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[DEBUG] Exception details: {ex}");
                Console.WriteLine($"[DEBUG] ============ HandleGameStateChangedMessage EXCEPTION END ============");
            }
        }

        private void HandleGameStartMessage(string json)
        {
            try
            {
                Console.WriteLine($"[HandleGameStartMessage] Game is starting!");
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("Payload", out var payloadElem) || root.TryGetProperty("payload", out payloadElem))
                    {
                        bool isYourTurn = false;
                        if (payloadElem.TryGetProperty("isYourTurn", out var yourTurnElem))
                        {
                            isYourTurn = yourTurnElem.GetBoolean();
                        }

                        Console.WriteLine($"[HandleGameStartMessage] Your turn: {isYourTurn}");

                        // Отправляем GameStateMessage для обновления состояния игры
                        var gameStateMessage = new GameStateMessage
                        {
                            Type = "GameState",
                            State = isYourTurn ? "YourTurn" : "OpponentTurn"
                        };
                        GameStateChanged?.Invoke(gameStateMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HandleGameStartMessage] Error: {ex.Message}");
            }
        }

        // Обработка сообщения чата (приватный метод)
        private void HandleChatMessage(string json)
        {
            try
            {
                var message = JsonSerializer.Deserialize<ChatMessage>(json);
                if (message != null)
                {
                    ChatMessageReceived?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing chat message: {ex.Message}");
            }
        }
    }
}
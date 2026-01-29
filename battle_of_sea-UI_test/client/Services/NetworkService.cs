// Реал WS
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private TaskCompletionSource<bool>? _joinRoomCompletionSource;

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
        // Событие результата создания комнаты (публичное событие)
        public event Action<Room>? RoomCreated;
        // Событие результата присоединения к комнате (публичное событие)
        public event Action<JoinRoomMessage>? JoinRoomResult;
        // Событие начала игры (публичное событие)
        public event Action? GameStarted;
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
                Program.LogMessage($"[NetworkService] Attempting to connect to {_serverUrl}");
                Program.LogMessage($"[NetworkService] Current time: {DateTime.Now:HH:mm:ss.fff}");
                
                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource(); // Без timeout
                
                Program.LogMessage($"[NetworkService] Connecting to WebSocket...");
                Program.LogMessage($"[NetworkService] WebSocket URI: {new Uri(_serverUrl)}");
                
                // Используем отдельный timeout только для подключения
                using (var connectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    await _webSocket.ConnectAsync(
                        new Uri(_serverUrl), 
                        connectTimeout.Token
                    );
                }
                
                Program.LogMessage($"[NetworkService] ✅ WebSocket connected! State: {_webSocket.State}");

                _isConnected = true;
                _currentUserId = userId;

                // Отправляем данные пользователя на сервер
                var connectMessage = new
                {
                    type = "Connect",
                    userId = userId,
                    displayName = displayName
                };

                Program.LogMessage($"[NetworkService] Sending Connect message...");
                await SendMessageAsync(connectMessage);
                Program.LogMessage($"[NetworkService] Connect message sent");

                // Запускаем слушатель сообщений
                _ = ListenForMessagesAsync();

                UserConnected?.Invoke(new UserConnectedMessage
                {
                    UserId = userId,
                    DisplayName = displayName,
                    Type = "UserConnected"
                });

                Program.LogMessage($"[NetworkService] Connected successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Program.LogMessage($"[NetworkService] ❌ Connection error: {ex.Message}");
                Program.LogMessage($"[NetworkService] Exception type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                    Program.LogMessage($"[NetworkService] Inner exception: {ex.InnerException.Message}");
                
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
                Program.LogMessage($"Disconnection error: {ex.Message}");
            }
        }

        // Получение списка комнат (публичный метод)
        public async Task<List<Room>> GetRoomsAsync()
        {
            try
            {
                var message = new { type = "GetRooms" };
                await SendMessageAsync(message);
                
                // Сервер отправит RoomsListMessage через WebSocket
                // Это обработается в ListenForMessagesAsync
                return new List<Room>();
            }
            catch (Exception ex)
            {
                Program.LogMessage($"Error getting rooms: {ex.Message}");
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
                Program.LogMessage($"Error creating room: {ex.Message}");
                ConnectionError?.Invoke($"Failed to create room: {ex.Message}");
                return false;
            }
        }

        // Присоединение к комнате (публичный метод)
        public async Task<bool> JoinRoomAsync(Room room, string? password = null)
        {
            try
            {
                _currentRoomId = room.Id;
                
                // Создаем TaskCompletionSource для ожидания результата
                _joinRoomCompletionSource = new TaskCompletionSource<bool>();
                
                var message = new
                {
                    type = "JoinRoom",
                    roomId = room.Id,
                    userId = _currentUserId,
                    password = password
                };

                await SendMessageAsync(message);
                Program.LogMessage($"[JoinRoomAsync] Sent JoinRoom message for room {room.Id}, waiting for response...");
                
                // Ждем результата с timeout 5 секунд
                var task = _joinRoomCompletionSource.Task;
                var completedTask = await Task.WhenAny(task, Task.Delay(5000));
                
                if (completedTask == task)
                {
                    var result = await task;
                    Program.LogMessage($"[JoinRoomAsync] Received result: {result}");
                    return result;
                }
                else
                {
                    Program.LogMessage($"[JoinRoomAsync] ⏱️ Timeout waiting for JoinRoom result");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Program.LogMessage($"Error joining room: {ex.Message}");
                ConnectionError?.Invoke($"Failed to join room: {ex.Message}");
                return false;
            }
            finally
            {
                _joinRoomCompletionSource = null;
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
                Program.LogMessage($"Error leaving room: {ex.Message}");
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
                Program.LogMessage($"Error sending shoot: {ex.Message}");
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
                Program.LogMessage($"Error sending ship placement: {ex.Message}");
                ConnectionError?.Invoke($"Failed to send ship placement: {ex.Message}");
                return false;
            }
        }

        // Отправить сдачу (публичный метод)
        public async Task<bool> SendSurrenderAsync(string roomId)
        {
            try
            {
                var message = new
                {
                    type = "Surrender",
                    roomId = roomId,
                    userId = _currentUserId
                };

                await SendMessageAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                Program.LogMessage($"Error sending surrender: {ex.Message}");
                ConnectionError?.Invoke($"Failed to send surrender: {ex.Message}");
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
                Program.LogMessage($"Error sending chat message: {ex.Message}");
                ConnectionError?.Invoke($"Failed to send chat message: {ex.Message}");
                return false;
            }
        }

        // Отправить сообщение на сервер (приватный метод)
        private async Task SendMessageAsync(object message)
        {
            Program.LogMessage($"[SendMessage] WebSocket state: {_webSocket?.State}");
            if (_webSocket?.State != WebSocketState.Open)
            {
                Program.LogMessage($"[SendMessage] ❌ WebSocket is not connected! State: {_webSocket?.State}");
                throw new InvalidOperationException("WebSocket is not connected");
            }

            var json = JsonSerializer.Serialize(message);
            Program.LogMessage($"[SendMessage] Sending: {json}");
            var buffer = Encoding.UTF8.GetBytes(json);

            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            
            Program.LogMessage($"[SendMessage] ✅ Message sent");

        }

        // Прослушивание сообщений от сервера (приватный метод)
        private async Task ListenForMessagesAsync()
        {
            try
            {
                Program.LogMessage($"[ListenForMessagesAsync] ✅ Started listening for messages");
                var buffer = new byte[1024 * 16]; // Увеличен буфер на 16KB для больших сообщений
                var messageBuffer = new StringBuilder(); // Для сборки фрагментированных сообщений

                while (_webSocket?.State == WebSocketState.Open && _cancellationTokenSource?.Token.IsCancellationRequested == false)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None // Без timeout для приема сообщений
                    );

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Program.LogMessage($"[ListenForMessagesAsync] Received {json.Length} bytes: {json.Substring(0, Math.Min(100, json.Length))}...");
                        messageBuffer.Append(json);

                        // Если это конец сообщения, обрабатываем его
                        if (result.EndOfMessage)
                        {
                            HandleServerMessage(messageBuffer.ToString());
                            messageBuffer.Clear();
                        }
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
                Program.LogMessage($"[ListenForMessagesAsync] ⚠️ Loop ended. WebSocket state: {_webSocket?.State}");
            }
            catch (OperationCanceledException)
            {
                Program.LogMessage($"[ListenForMessagesAsync] ⚠️ Listener cancelled normally");
            }
            catch (Exception ex)
            {
                Program.LogMessage($"[ListenForMessagesAsync] ❌ Error in message listener: {ex.Message}");
                Program.LogMessage($"[ListenForMessagesAsync] ❌ Stack trace: {ex.StackTrace}");
                ConnectionError?.Invoke($"Connection lost: {ex.Message}");
                _isConnected = false;
            }
        }

        // Обработать сообщение от сервера (приватный метод)
        private void HandleServerMessage(string json)
        {
            try
            {
                Program.LogMessage($"[HandleServerMessage] 🔍 Raw message start: {json.Substring(0, Math.Min(100, json.Length))}");
                
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    
                    // Пытаемся получить "type" или "Type" (case-insensitive)
                    string? type = null;
                    if (root.TryGetProperty("type", out var typeElem))
                    {
                        type = typeElem.GetString();
                        Program.LogMessage($"[HandleServerMessage] Found 'type' (lowercase): {type}");
                    }
                    else if (root.TryGetProperty("Type", out typeElem))
                    {
                        type = typeElem.GetString();
                        Program.LogMessage($"[HandleServerMessage] Found 'Type' (uppercase): {type}");
                    }
                    
                    if (string.IsNullOrEmpty(type))
                    {
                        Program.LogMessage($"[HandleServerMessage] ❌ Message has no 'type' field: {json.Substring(0, Math.Min(100, json.Length))}");
                        return;
                    }

                    var typeLower = type.ToLower();
                    Program.LogMessage($"[HandleServerMessage] 📨 Message type='{type}' | Lowercase='{typeLower}'");

                    switch (typeLower)
                    {
                        case "connected":
                        case "roomscreated":
                            // These are just confirmations, we can ignore or log them
                            Program.LogMessage($"[HandleServerMessage] ✓ Received {type} confirmation");
                            break;
                        case "roomcreated":
                            Program.LogMessage($"[HandleServerMessage] 🎯 MATCHED 'roomcreated' case! Invoking HandleRoomCreatedMessage");
                            HandleRoomCreatedMessage(json);
                            break;
                        case "roomslist":
                            HandleRoomsListMessage(json);
                            break;
                        case "joinroom":
                            HandleJoinRoomMessage(json);
                            break;
                        case "gamestarted":
                            Program.LogMessage($"[HandleServerMessage] 🎮 GAME STARTED!");
                            GameStarted?.Invoke();
                            break;
                        case "your_turn":
                            Program.LogMessage($"[HandleServerMessage] 🔔 YOUR TURN message received");
                            var yourTurnMsg = new GameStateMessage { Type = "your_turn", State = GameState.YourTurn, RoomId = _currentRoomId ?? string.Empty };
                            GameStateChanged?.Invoke(yourTurnMsg);
                            break;
                        case "opponent_turn":
                            Program.LogMessage($"[HandleServerMessage] 🔕 OPPONENT TURN message received");
                            var oppMsg = new GameStateMessage { Type = "opponent_turn", State = GameState.OpponentTurn, RoomId = _currentRoomId ?? string.Empty };
                            GameStateChanged?.Invoke(oppMsg);
                            break;
                        case "turn_timeout":
                            Program.LogMessage($"[HandleServerMessage] ⏱️ TURN TIMEOUT received");
                            var timeoutMsg = new GameStateMessage { Type = "turn_timeout", State = GameState.OpponentTurn, RoomId = _currentRoomId ?? string.Empty };
                            GameStateChanged?.Invoke(timeoutMsg);
                            break;
                        case "shootresult":
                            HandleShootResultMessage(json);
                            break;
                        case "opponentshoot":
                            HandleOpponentShootMessage(json);
                            break;
                        case "yourturn":
                            Program.LogMessage($"[HandleServerMessage] 🔔 YourTurn (capitalized) received");
                            var yourTurnMsg2 = new GameStateMessage { Type = "yourturn", State = GameState.YourTurn, RoomId = _currentRoomId ?? string.Empty };
                            GameStateChanged?.Invoke(yourTurnMsg2);
                            break;
                        case "shipplacementresult":
                            Program.LogMessage($"[HandleServerMessage] ✅ ShipPlacement result received");
                            break;
                        case "gamestate":
                            HandleGameStateMessage(json);
                            break;
                        case "chatmessage":
                            HandleChatMessage(json);
                            break;
                        case "error":
                            Program.LogMessage($"[HandleServerMessage] Server error");
                            break;
                        default:
                            // Unknown message type - ignore or log in future if needed
                            Program.LogMessage($"[HandleServerMessage] Unknown message type: {type}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogMessage($"Error handling message: {ex.Message}");
                Program.LogMessage($"Error stack trace: {ex.StackTrace}");
            }
        }

        // Обработка создания комнаты (приватный метод)
        private void HandleRoomCreatedMessage(string json)
        {
            try
            {
                Program.LogMessage($"[HandleRoomCreatedMessage] 🏠 ENTERED - Processing RoomCreated message");
                Program.LogMessage($"[HandleRoomCreatedMessage] Raw JSON: {json}");
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    
                    JsonElement payloadElem;
                    if (!root.TryGetProperty("Payload", out payloadElem) && !root.TryGetProperty("payload", out payloadElem))
                    {
                        Program.LogMessage($"[HandleRoomCreatedMessage] ❌ No Payload in message");
                        return;
                    }
                    
                    Program.LogMessage($"[HandleRoomCreatedMessage] ✓ Found Payload");
                    
                    if (!payloadElem.TryGetProperty("room", out var roomElem))
                    {
                        Program.LogMessage($"[HandleRoomCreatedMessage] ❌ No room in payload");
                        return;
                    }
                    
                    Program.LogMessage($"[HandleRoomCreatedMessage] ✓ Found room in payload");
                    
                    var roomJson = roomElem.GetRawText();
                    Program.LogMessage($"[HandleRoomCreatedMessage] Room JSON: {roomJson}");
                    
                    var room = JsonSerializer.Deserialize<Room>(roomJson, options);
                    if (room != null)
                    {
                        Program.LogMessage($"[HandleRoomCreatedMessage] ✅ Room deserialized: Name='{room.Name}' ID='{room.Id}'");
                        Program.LogMessage($"[HandleRoomCreatedMessage] 🔔 INVOKING RoomCreated event with room ID: {room.Id}");
                        RoomCreated?.Invoke(room);
                        Program.LogMessage($"[HandleRoomCreatedMessage] 🔔 Event invoked successfully");
                    }
                    else
                    {
                        Program.LogMessage($"[HandleRoomCreatedMessage] ❌ Room deserialization returned null");
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogMessage($"[HandleRoomCreatedMessage] ❌ ERROR: {ex.Message}");
                Program.LogMessage($"[HandleRoomCreatedMessage] Stack trace: {ex.StackTrace}");
            }
        }

        // Обработка списка комнат (приватный метод)
        private void HandleRoomsListMessage(string json)
        {
            try
            {
                Program.LogMessage($"[HandleRoomsListMessage] Parsing rooms list...");
                Program.LogMessage($"[HandleRoomsListMessage] Raw JSON: {json}");
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    
                    // Пытаемся найти Payload (с большой буквы) или payload (с маленькой)
                    JsonElement payloadElem;
                    if (!root.TryGetProperty("Payload", out payloadElem) && !root.TryGetProperty("payload", out payloadElem))
                    {
                        Program.LogMessage($"[HandleRoomsListMessage] ❌ No Payload in message");
                        return;
                    }
                    
                    if (!payloadElem.TryGetProperty("rooms", out var roomsElem))
                    {
                        Program.LogMessage($"[HandleRoomsListMessage] ❌ No rooms in payload");
                        return;
                    }
                    
                    var rooms = new List<Room>();
                    foreach (var roomElem in roomsElem.EnumerateArray())
                    {
                        // Десериализуем каждую комнату с case-insensitive опцией
                        var roomJson = roomElem.GetRawText();
                        var room = JsonSerializer.Deserialize<Room>(roomJson, options);
                        if (room != null)
                        {
                            rooms.Add(room);
                            Program.LogMessage($"[HandleRoomsListMessage] Room: {room.Name} (Players: {room.Players}/{room.MaxPlayers})");
                        }
                    }
                    
                    Program.LogMessage($"[HandleRoomsListMessage] ✅ Got {rooms.Count} rooms");
                    
                    var message = new RoomsListMessage { Rooms = rooms };
                    RoomsListUpdated?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Program.LogMessage($"[HandleRoomsListMessage] ❌ Error deserializing rooms list: {ex.Message}");
                Program.LogMessage($"[HandleRoomsListMessage] Stack trace: {ex.StackTrace}");
            }
        }

        // Обработка присоединения к комнате (приватный метод)
        private void HandleJoinRoomMessage(string json)
        {
            try
            {
                Program.LogMessage($"[HandleJoinRoomMessage] 🔵 Processing JoinRoom message (full): {json}");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var message = JsonSerializer.Deserialize<JoinRoomMessage>(json, options);
                if (message != null)
                {
                    Program.LogMessage($"[HandleJoinRoomMessage] ✓ Deserialized successfully - Success={message.Success}, RoomId={message.RoomId}");
                    JoinRoomResult?.Invoke(message);
                    
                    // Если присоединение успешно, сохраняем текущую комнату
                    if (message.Success)
                    {
                        _currentRoomId = message.RoomId;
                        Program.LogMessage($"[HandleJoinRoomMessage] ✅ Set _currentRoomId = {message.RoomId}");
                    }

                    // Уведомляем ожидающий JoinRoomAsync
                    if (_joinRoomCompletionSource != null)
                    {
                        _joinRoomCompletionSource.SetResult(message.Success);
                        Program.LogMessage($"[HandleJoinRoomMessage] ✅ Notified JoinRoomAsync of result");
                    }
                    else
                    {
                        Program.LogMessage($"[HandleJoinRoomMessage] ⚠️ _joinRoomCompletionSource is null");
                    }
                }
                else
                {
                    Program.LogMessage($"[HandleJoinRoomMessage] ❌ Deserialization returned null");
                }
            }
            catch (Exception ex)
            {
                Program.LogMessage($"[HandleJoinRoomMessage] ❌ Error: {ex.Message}");
                Program.LogMessage($"[HandleJoinRoomMessage] Stack trace: {ex.StackTrace}");
                if (_joinRoomCompletionSource != null)
                {
                    _joinRoomCompletionSource.SetException(ex);
                }
            }
        }

        // Результат выстрела (приватный метод)
        private void HandleShootResultMessage(string json)
        {
            try
            {
                Program.LogMessage($"[HandleShootResultMessage] 🎯 Processing ShootResult (full): {json}");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var message = JsonSerializer.Deserialize<ShootResultMessage>(json, options);
                if (message != null)
                {
                    Program.LogMessage($"[HandleShootResultMessage] ✓ Deserialized: Row={message.Row}, Col={message.Col}, IsHit={message.IsHit}");
                    ShootResultReceived?.Invoke(message);
                }
                else
                {
                    Program.LogMessage($"[HandleShootResultMessage] ❌ Deserialization returned null");
                }
            }
            catch (Exception ex)
            {
                Program.LogMessage($"[HandleShootResultMessage] ❌ Error: {ex.Message}");
                Program.LogMessage($"[HandleShootResultMessage] Stack trace: {ex.StackTrace}");
            }
        }

        // Ход противника (приватный метод)
        private void HandleOpponentShootMessage(string json)
        {
            try
            {
                Program.LogMessage($"[HandleOpponentShootMessage] 💥 Processing OpponentShoot (full): {json}");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var message = JsonSerializer.Deserialize<ShootMessage>(json, options);
                if (message != null)
                {
                    Program.LogMessage($"[HandleOpponentShootMessage] ✓ Deserialized: Row={message.Row}, Col={message.Col}, RoomId={message.RoomId}");
                    OpponentShootReceived?.Invoke(message);
                }
                else
                {
                    Program.LogMessage($"[HandleOpponentShootMessage] ❌ Deserialization returned null");
                }
            }
            catch (Exception ex)
            {
                Program.LogMessage($"[HandleOpponentShootMessage] ❌ Error: {ex.Message}");
                Program.LogMessage($"[HandleOpponentShootMessage] Stack trace: {ex.StackTrace}");
            }
        }

        // Обработка состояния игры (приватный метод)
        private void HandleGameStateMessage(string json)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var message = JsonSerializer.Deserialize<GameStateMessage>(json, options);
                if (message != null)
                {
                    GameStateChanged?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Program.LogMessage($"Error deserializing game state: {ex.Message}");
            }
        }

        // Обработка сообщения чата (приватный метод)
        private void HandleChatMessage(string json)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var message = JsonSerializer.Deserialize<ChatMessage>(json, options);
                if (message != null)
                {
                    ChatMessageReceived?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Program.LogMessage($"Error deserializing chat message: {ex.Message}");
            }
        }
    }
}


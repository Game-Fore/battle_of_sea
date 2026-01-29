using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BattleOfSea.Models;

namespace BattleOfSea.Services
{
    /// <summary>
    /// Клиент для обмена данными с игровым сервером (порт 5555, WebSocket)
    /// Используется для всех игровых операций: расстановка кораблей, выстрелы, изменение состояния игры
    /// </summary>
    public class GameServerClient : IDisposable
    {
        private ClientWebSocket? _webSocket;
        private bool _isConnected = false;
        private readonly string _serverHost;
        private readonly int _serverPort;
        private string? _currentUserId;
        private string? _currentRoomId;
        private string? _displayName;
        private CancellationTokenSource? _cancellationTokenSource;

        // События
        public event Action<string>? Connected;
        public event Action<string>? Disconnected;
        public event Action<string>? ErrorOccurred;
        public event Action<ShootResultMessage>? ShootResultReceived;
        public event Action<GameStateMessage>? GameStateChanged;
        public event Action<JoinRoomMessage>? JoinRoomResultReceived;

        public bool IsConnected => _isConnected && (_webSocket?.State == WebSocketState.Open);

        public GameServerClient(string serverHost = "localhost", int serverPort = 5555)
        {
            _serverHost = serverHost;
            _serverPort = serverPort;
        }

        /// <summary>
        /// Подключиться к игровому серверу
        /// </summary>
        public async Task<bool> ConnectAsync(string userId, string displayName = "Player")
        {
            try
            {
                _currentUserId = userId;
                _displayName = displayName;
                _cancellationTokenSource = new CancellationTokenSource();
                _webSocket = new ClientWebSocket();

                string wsUrl = $"ws://{_serverHost}:{_serverPort}/";
                await _webSocket.ConnectAsync(new Uri(wsUrl), CancellationToken.None);

                _isConnected = true;

                // Отправляем команду подключения
                await SendMessageAsync(new
                {
                    type = "Connect",
                    userId = userId,
                    displayName = displayName
                });

                // Запускаем слушатель ответов от сервера
                _ = ListenForMessagesAsync();

                Connected?.Invoke($"Connected to game server {_serverHost}:{_serverPort}");
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Connection error: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Отключиться от игрового сервера
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Disconnecting",
                        CancellationToken.None
                    );
                }

                _webSocket?.Dispose();
                _isConnected = false;
                Disconnected?.Invoke("Disconnected from game server");
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Disconnection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Присоединиться к комнате
        /// </summary>
        public async Task<bool> JoinRoomAsync(string roomId)
        {
            try
            {
                if (!IsConnected)
                {
                    ErrorOccurred?.Invoke("Not connected to game server");
                    return false;
                }

                _currentRoomId = roomId;

                await SendMessageAsync(new
                {
                    type = "joinRoom",
                    payload = new
                    {
                        userId = _currentUserId,
                        roomId = roomId
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Join room error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Покинуть комнату
        /// </summary>
        public async Task<bool> LeaveRoomAsync()
        {
            try
            {
                if (!IsConnected || string.IsNullOrEmpty(_currentRoomId))
                {
                    return false;
                }

                await SendMessageAsync(new
                {
                    type = "leaveRoom",
                    payload = new
                    {
                        userId = _currentUserId,
                        roomId = _currentRoomId
                    }
                });

                _currentRoomId = null;
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Leave room error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Отправить расстановку кораблей
        /// </summary>
        public async Task<bool> SendShipPlacementAsync(List<ShipPlacementData> ships)
        {
            try
            {
                if (!IsConnected || string.IsNullOrEmpty(_currentRoomId))
                {
                    ErrorOccurred?.Invoke("Not connected or not in a room");
                    return false;
                }

                await SendMessageAsync(new
                {
                    type = "shipPlacement",
                    payload = new
                    {
                        userId = _currentUserId,
                        roomId = _currentRoomId,
                        ships = ships
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Ship placement error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Отправить выстрел
        /// </summary>
        public async Task<bool> SendShootAsync(int row, int col)
        {
            try
            {
                if (!IsConnected || string.IsNullOrEmpty(_currentRoomId))
                {
                    ErrorOccurred?.Invoke("Not connected or not in a room");
                    return false;
                }

                await SendMessageAsync(new
                {
                    type = "shoot",
                    payload = new
                    {
                        userId = _currentUserId,
                        roomId = _currentRoomId,
                        row = row,
                        col = col
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Shoot error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Отправить готовность к игре
        /// </summary>
        public async Task<bool> SendReadyAsync()
        {
            try
            {
                if (!IsConnected || string.IsNullOrEmpty(_currentRoomId))
                {
                    ErrorOccurred?.Invoke("Not connected or not in a room");
                    return false;
                }

                await SendMessageAsync(new
                {
                    type = "ready",
                    payload = new
                    {
                        userId = _currentUserId,
                        roomId = _currentRoomId
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Ready error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Отправить сдачу
        /// </summary>
        public async Task<bool> SendSurrenderAsync()
        {
            try
            {
                if (!IsConnected || string.IsNullOrEmpty(_currentRoomId))
                {
                    ErrorOccurred?.Invoke("Not connected or not in a room");
                    return false;
                }

                await SendMessageAsync(new
                {
                    type = "surrender",
                    payload = new
                    {
                        userId = _currentUserId,
                        roomId = _currentRoomId
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Surrender error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Отправить сообщение на сервер
        /// </summary>
        private async Task SendMessageAsync(object message)
        {
            if (_webSocket?.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            try
            {
                string json = JsonSerializer.Serialize(message);
                byte[] data = Encoding.UTF8.GetBytes(json);
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(data),
                    WebSocketMessageType.Text,
                    true,
                    _cancellationTokenSource?.Token ?? CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to send message: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Слушать сообщения от сервера
        /// </summary>
        private async Task ListenForMessagesAsync()
        {
            byte[] buffer = new byte[1024 * 4];

            try
            {
                while (IsConnected)
                {
                    WebSocketReceiveResult result = await _webSocket!.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource?.Token ?? CancellationToken.None
                    );

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            CancellationToken.None
                        );
                        _isConnected = false;
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        ProcessServerMessage(message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальное завершение при отключении
            }
            catch (Exception ex)
            {
                _isConnected = false;
                ErrorOccurred?.Invoke($"Listen error: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработать сообщение от сервера
        /// </summary>
        private void ProcessServerMessage(string jsonMessage)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonMessage))
                {
                    JsonElement root = doc.RootElement;

                    if (!root.TryGetProperty("Type", out JsonElement typeElement) &&
                        !root.TryGetProperty("type", out typeElement))
                    {
                        return;
                    }

                    string messageType = typeElement.GetString() ?? "";

                    // Пытаемся получить payload
                    JsonElement payload = default;
                    if (root.TryGetProperty("Payload", out var p) || root.TryGetProperty("payload", out p))
                    {
                        payload = p;
                    }

                    switch (messageType.ToLower())
                    {
                        case "connected":
                            HandleConnected(payload);
                            break;

                        case "roomslist":
                            HandleRoomsList(payload);
                            break;

                        case "shootresult":
                            HandleShootResult(payload);
                            break;

                        case "gamestate":
                            HandleGameState(payload);
                            break;

                        case "joinroom":
                            HandleJoinRoom(payload);
                            break;

                        case "error":
                            if (payload.ValueKind != JsonValueKind.Undefined &&
                                payload.TryGetProperty("message", out JsonElement msgElement))
                            {
                                ErrorOccurred?.Invoke(msgElement.GetString() ?? "Unknown error");
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Error processing message: {ex.Message}");
            }
        }

        private void HandleConnected(JsonElement payload)
        {
            // Сервер подтвердил подключение
            Console.WriteLine("✅ Connected to server");
        }

        private void HandleRoomsList(JsonElement payload)
        {
            // Обработка списка комнат если нужно
            Console.WriteLine("📋 Rooms list received");
        }

        private void HandleShootResult(JsonElement payload)
        {
            try
            {
                var message = new ShootResultMessage
                {
                    Type = "ShootResult",
                    Timestamp = DateTime.UtcNow
                };

                if (payload.ValueKind != JsonValueKind.Undefined)
                {
                    if (payload.TryGetProperty("row", out JsonElement rowElement))
                        message.Row = rowElement.GetInt32();

                    if (payload.TryGetProperty("col", out JsonElement colElement))
                        message.Col = colElement.GetInt32();

                    if (payload.TryGetProperty("isHit", out JsonElement hitElement))
                        message.IsHit = hitElement.GetBoolean();

                    if (payload.TryGetProperty("isSunk", out JsonElement sunkElement))
                        message.IsSunk = sunkElement.GetBoolean();

                    if (payload.TryGetProperty("isGameOver", out JsonElement gameOverElement))
                        message.IsGameOver = gameOverElement.GetBoolean();

                    if (payload.TryGetProperty("isWinner", out JsonElement winnerElement))
                        message.IsWinner = winnerElement.GetBoolean();
                }

                ShootResultReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Error handling shoot result: {ex.Message}");
            }
        }

        private void HandleGameState(JsonElement payload)
        {
            try
            {
                var message = new GameStateMessage
                {
                    Type = "GameState",
                    Timestamp = DateTime.UtcNow
                };

                if (payload.ValueKind != JsonValueKind.Undefined)
                {
                    if (payload.TryGetProperty("state", out JsonElement stateElement))
                    {
                        if (Enum.TryParse<GameState>(stateElement.GetString(), out var state))
                        {
                            message.State = state;
                        }
                    }

                    if (payload.TryGetProperty("roomId", out JsonElement roomElement))
                        message.RoomId = roomElement.GetString() ?? "";
                }

                GameStateChanged?.Invoke(message);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Error handling game state: {ex.Message}");
            }
        }

        private void HandleJoinRoom(JsonElement payload)
        {
            try
            {
                var message = new JoinRoomMessage
                {
                    Type = "JoinRoom",
                    Timestamp = DateTime.UtcNow
                };

                if (payload.ValueKind != JsonValueKind.Undefined)
                {
                    if (payload.TryGetProperty("success", out JsonElement successElement))
                        message.Success = successElement.GetBoolean();

                    if (payload.TryGetProperty("roomId", out JsonElement roomElement))
                        message.RoomId = roomElement.GetString() ?? "";

                    if (payload.TryGetProperty("message", out JsonElement msgElement))
                        message.Message = msgElement.GetString() ?? "";
                }

                JoinRoomResultReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Error handling join room: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _ = DisconnectAsync();
        }
    }
}

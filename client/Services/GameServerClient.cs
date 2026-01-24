using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BattleOfSea.Models;

namespace BattleOfSea.Services
{
    /// <summary>
    /// Клиент для обмена данными с игровым сервером (порт 5000)
    /// Используется для всех игровых операций: расстановка кораблей, выстрелы, изменение состояния игры
    /// </summary>
    public class GameServerClient : IDisposable
    {
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private bool _isConnected = false;
        private readonly string _serverHost;
        private readonly int _serverPort;
        private string? _currentUserId;
        private string? _currentRoomId;

        // События
        public event Action<string>? Connected;
        public event Action<string>? Disconnected;
        public event Action<string>? ErrorOccurred;
        public event Action<ShootResultMessage>? ShootResultReceived;
        public event Action<GameStateMessage>? GameStateChanged;
        public event Action<JoinRoomMessage>? JoinRoomResultReceived;

        public bool IsConnected => _isConnected && (_tcpClient?.Connected ?? false);

        public GameServerClient(string serverHost = "localhost", int serverPort = 5000)
        {
            _serverHost = serverHost;
            _serverPort = serverPort;
        }

        /// <summary>
        /// Подключиться к игровому серверу
        /// </summary>
        public async Task<bool> ConnectAsync(string userId)
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(_serverHost, _serverPort);
                _stream = _tcpClient.GetStream();
                _isConnected = true;
                _currentUserId = userId;

                // Отправляем команду подключения
                await SendCommandAsync(new
                {
                    action = "connect",
                    userId = userId,
                    timestamp = DateTime.UtcNow
                });

                // Запускаем слушатель ответов от сервера
                _ = ListenForResponsesAsync();

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
                if (_isConnected)
                {
                    // Отправляем команду отключения
                    await SendCommandAsync(new
                    {
                        action = "disconnect",
                        userId = _currentUserId,
                        timestamp = DateTime.UtcNow
                    });
                }

                _stream?.Close();
                _tcpClient?.Close();
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

                await SendCommandAsync(new
                {
                    action = "joinRoom",
                    userId = _currentUserId,
                    roomId = roomId,
                    timestamp = DateTime.UtcNow
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

                await SendCommandAsync(new
                {
                    action = "leaveRoom",
                    userId = _currentUserId,
                    roomId = _currentRoomId,
                    timestamp = DateTime.UtcNow
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

                await SendCommandAsync(new
                {
                    action = "shipPlacement",
                    userId = _currentUserId,
                    roomId = _currentRoomId,
                    ships = ships,
                    timestamp = DateTime.UtcNow
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

                await SendCommandAsync(new
                {
                    action = "shoot",
                    userId = _currentUserId,
                    roomId = _currentRoomId,
                    row = row,
                    col = col,
                    timestamp = DateTime.UtcNow
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

                await SendCommandAsync(new
                {
                    action = "ready",
                    userId = _currentUserId,
                    roomId = _currentRoomId,
                    timestamp = DateTime.UtcNow
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

                await SendCommandAsync(new
                {
                    action = "surrender",
                    userId = _currentUserId,
                    roomId = _currentRoomId,
                    timestamp = DateTime.UtcNow
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
        /// Отправить команду на сервер
        /// </summary>
        private async Task SendCommandAsync(object command)
        {
            if (_stream == null || !_isConnected)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            try
            {
                string json = JsonSerializer.Serialize(command);
                byte[] data = Encoding.UTF8.GetBytes(json + "\n");
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to send command: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Слушать ответы от сервера
        /// </summary>
        private async Task ListenForResponsesAsync()
        {
            byte[] buffer = new byte[4096];
            StringBuilder messageBuilder = new StringBuilder();

            try
            {
                while (IsConnected && _stream != null)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        _isConnected = false;
                        ErrorOccurred?.Invoke("Server closed connection");
                        break;
                    }

                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(chunk);

                    string data = messageBuilder.ToString();
                    int newLineIndex;

                    while ((newLineIndex = data.IndexOf('\n')) >= 0)
                    {
                        string message = data.Substring(0, newLineIndex).Trim();
                        data = data.Substring(newLineIndex + 1);

                        if (!string.IsNullOrEmpty(message))
                        {
                            ProcessServerResponse(message);
                        }
                    }

                    messageBuilder.Clear();
                    messageBuilder.Append(data);
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                ErrorOccurred?.Invoke($"Listen error: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработать ответ от сервера
        /// </summary>
        private void ProcessServerResponse(string jsonMessage)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonMessage))
                {
                    JsonElement root = doc.RootElement;

                    if (!root.TryGetProperty("type", out JsonElement typeElement))
                    {
                        return;
                    }

                    string messageType = typeElement.GetString() ?? "";

                    switch (messageType)
                    {
                        case "ShootResult":
                            HandleShootResult(root);
                            break;

                        case "GameState":
                            HandleGameState(root);
                            break;

                        case "JoinRoom":
                            HandleJoinRoom(root);
                            break;

                        case "Error":
                            if (root.TryGetProperty("message", out JsonElement msgElement))
                            {
                                ErrorOccurred?.Invoke(msgElement.GetString() ?? "Unknown error");
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Error processing response: {ex.Message}");
            }
        }

        private void HandleShootResult(JsonElement root)
        {
            try
            {
                var message = new ShootResultMessage
                {
                    Type = "ShootResult",
                    Timestamp = DateTime.UtcNow
                };

                if (root.TryGetProperty("row", out JsonElement rowElement))
                    message.Row = rowElement.GetInt32();

                if (root.TryGetProperty("col", out JsonElement colElement))
                    message.Col = colElement.GetInt32();

                if (root.TryGetProperty("isHit", out JsonElement hitElement))
                    message.IsHit = hitElement.GetBoolean();

                if (root.TryGetProperty("isSunk", out JsonElement sunkElement))
                    message.IsSunk = sunkElement.GetBoolean();

                if (root.TryGetProperty("isGameOver", out JsonElement gameOverElement))
                    message.IsGameOver = gameOverElement.GetBoolean();

                if (root.TryGetProperty("isWinner", out JsonElement winnerElement))
                    message.IsWinner = winnerElement.GetBoolean();

                ShootResultReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Error handling shoot result: {ex.Message}");
            }
        }

        private void HandleGameState(JsonElement root)
        {
            try
            {
                var message = new GameStateMessage
                {
                    Type = "GameState",
                    Timestamp = DateTime.UtcNow,
                    State = string.Empty
                };

                if (root.TryGetProperty("state", out JsonElement stateElement))
                {
                    var stateStr = stateElement.GetString();
                    if (!string.IsNullOrEmpty(stateStr))
                    {
                        message.State = stateStr;
                    }
                }

                if (root.TryGetProperty("roomId", out JsonElement roomElement))
                    message.RoomId = roomElement.GetString() ?? "";

                GameStateChanged?.Invoke(message);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Error handling game state: {ex.Message}");
            }
        }

        private void HandleJoinRoom(JsonElement root)
        {
            try
            {
                var message = new JoinRoomMessage
                {
                    Type = "JoinRoom",
                    Timestamp = DateTime.UtcNow
                };

                if (root.TryGetProperty("success", out JsonElement successElement))
                    message.Success = successElement.GetBoolean();

                if (root.TryGetProperty("roomId", out JsonElement roomElement))
                    message.RoomId = roomElement.GetString() ?? "";

                if (root.TryGetProperty("message", out JsonElement msgElement))
                    message.Message = msgElement.GetString() ?? "";

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

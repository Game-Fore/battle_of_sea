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
        // Событие ошибки соединения (публичное событие)
        public event Action<string>? ConnectionError;

        // Конструктор сетевого сервиса (публичный)
        public NetworkService(string serverHost = "localhost", int serverPort = 5000)
        {
            _serverUrl = $"ws://{serverHost}:{serverPort}";
            _isConnected = false;
        }

        // Подключение асинхр (публичный метод)
        public async Task<bool> ConnectAsync(string userId, string displayName)
        {
            try
            {
                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource();
                
                await _webSocket.ConnectAsync(
                    new Uri($"{_serverUrl}/connect"), 
                    _cancellationTokenSource.Token
                );

                _isConnected = true;
                _currentUserId = userId;

                // Отправляем данные пользователя на сервер
                var connectMessage = new
                {
                    type = "Connect",
                    userId = userId,
                    displayName = displayName
                };

                await SendMessageAsync(connectMessage);

                // Запускаем слушатель сообщений
                _ = ListenForMessagesAsync();

                UserConnected?.Invoke(new UserConnectedMessage
                {
                    UserId = userId,
                    DisplayName = displayName,
                    Type = "UserConnected"
                });


                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}");
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
                var message = new { type = "GetRooms" };
                await SendMessageAsync(message);
                
                // Сервер отправит RoomsListMessage через WebSocket
                // Это обработается в ListenForMessagesAsync
                return new List<Room>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting rooms: {ex.Message}");
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
                _currentRoomId = room.Name;
                var message = new
                {
                    type = "JoinRoom",
                    roomId = room.Name,
                    userId = _currentUserId,
                    password = password
                };

                await SendMessageAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error joining room: {ex.Message}");
                ConnectionError?.Invoke($"Failed to join room: {ex.Message}");
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

        // Отправить сообщение на сервер (приватный метод)
        private async Task SendMessageAsync(object message)
        {
            if (_webSocket?.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected");
            }

            var json = JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(json);

            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );


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
                    var type = root.GetProperty("type").GetString();

                    switch (type)
                    {
                        case "RoomsList":
                            HandleRoomsListMessage(json);
                            break;
                        case "JoinRoom":
                            HandleJoinRoomMessage(json);
                            break;
                        case "ShootResult":
                            HandleShootResultMessage(json);
                            break;
                        case "OpponentShoot":
                            HandleOpponentShootMessage(json);
                            break;
                        case "GameState":
                            HandleGameStateMessage(json);
                            break;
                        default:
                            // Unknown message type - ignore or log in future if needed
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling message: {ex.Message}");
            }
        }

        // Обработка списка комнат (приватный метод)
        private void HandleRoomsListMessage(string json)
        {
            try
            {
                var message = JsonSerializer.Deserialize<RoomsListMessage>(json);
                if (message != null)
                {
                    RoomsListUpdated?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing rooms list: {ex.Message}");
            }
        }

        // Обработка присоединения к комнате (приватный метод)
        private void HandleJoinRoomMessage(string json)
        {
            try
            {
                var message = JsonSerializer.Deserialize<JoinRoomMessage>(json);
                if (message != null)
                {
                    JoinRoomResult?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing join room: {ex.Message}");
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
    }
}
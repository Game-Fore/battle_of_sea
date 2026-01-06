using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BattleOfSea.Models;

namespace BattleOfSea.Services
{
    public class MockNetworkService : INetworkService
    {
        private bool _isConnected = false;
        private string? _currentUserId;
        private string? _currentRoomId;
        private readonly Random _random = new Random();
        private readonly List<Room> _mockRooms = new List<Room>();

        public bool IsConnected => _isConnected;

        public event Action<ShootResultMessage>? ShootResultReceived;
        public event Action<ShootMessage>? OpponentShootReceived;
        public event Action<GameStateMessage>? GameStateChanged;
        public event Action<RoomsListMessage>? RoomsListUpdated;
        public event Action<JoinRoomMessage>? JoinRoomResult;
        public event Action<UserConnectedMessage>? UserConnected;
        public event Action<string>? ConnectionError;

        public MockNetworkService()
        {
            Console.WriteLine("MockNetworkService created");
            InitializeMockRooms();
        }

        private void InitializeMockRooms()
        {
            _mockRooms.AddRange(new[]
            {
                new Room("Alpha", 1, 2),
                new Room("Bravo", 0, 2),
                new Room("Charlie", 2, 4),
                new Room("Delta", 1, 4),
            });
        }

        public async Task<bool> ConnectAsync(string userId, string displayName)
        {
            Console.WriteLine($"Mock connect: {userId} ({displayName})");
            await Task.Delay(200);
            _isConnected = true;
            _currentUserId = userId;
            UserConnected?.Invoke(new UserConnectedMessage
            {
                UserId = userId,
                DisplayName = displayName,
                Type = "UserConnected"
            });
            return true;
        }

        public async Task DisconnectAsync()
        {
            Console.WriteLine("Mock disconnect");
            await Task.Delay(100);
            _isConnected = false;
            _currentUserId = null;
            _currentRoomId = null;
        }

        public async Task<List<Room>> GetRoomsAsync()
        {
            Console.WriteLine("Mock get rooms");
            await Task.Delay(150);
            
            // Симулируем обновление списка комнат
            var message = new RoomsListMessage
            {
                Rooms = _mockRooms.Where(r => r.Players < r.MaxPlayers).ToList(),
                Type = "RoomsList"
            };
            RoomsListUpdated?.Invoke(message);
            
            return message.Rooms;
        }

        public async Task<bool> JoinRoomAsync(Room room, string? password = null)
        {
            Console.WriteLine($"Mock join room: {room.Name}");
            await Task.Delay(300);
            
            _currentRoomId = room.Name;
            var result = new JoinRoomMessage
            {
                RoomId = room.Name,
                UserId = _currentUserId ?? "unknown",
                Success = true,
                Type = "JoinRoom"
            };
            JoinRoomResult?.Invoke(result);
            
            // Симулируем изменение состояния игры
            GameStateChanged?.Invoke(new GameStateMessage
            {
                State = GameState.WaitingForOpponent,
                RoomId = room.Name,
                Type = "GameState"
            });
            
            return true;
        }

        public async Task<bool> CreateRoomAsync(Room room, string? password = null)
        {
            Console.WriteLine($"Mock create room: {room.Name}");
            await Task.Delay(300);
            
            _mockRooms.Add(room);
            _currentRoomId = room.Name;
            
            var result = new JoinRoomMessage
            {
                RoomId = room.Name,
                UserId = _currentUserId ?? "unknown",
                Success = true,
                Type = "JoinRoom"
            };
            JoinRoomResult?.Invoke(result);
            
            return true;
        }

        public async Task LeaveRoomAsync()
        {
            Console.WriteLine("Mock leave room");
            await Task.Delay(100);
            _currentRoomId = null;
        }

        public async Task<bool> SendShootAsync(int row, int col, string roomId)
        {
            Console.WriteLine($"Mock send shoot: ({row}, {col}) in room {roomId}");
            await Task.Delay(200);
            
            // Симулируем ответ сервера (случайный результат для демо)
            var isHit = _random.Next(0, 3) == 0; // ~33% попаданий
            var isSunk = isHit && _random.Next(0, 3) == 0; // ~11% потоплений
            
            var result = new ShootResultMessage
            {
                Row = row,
                Col = col,
                IsHit = isHit,
                IsSunk = isSunk,
                IsGameOver = false, // В реальной игре проверяется через подсчет кораблей
                IsWinner = false,
                Type = "ShootResult"
            };
            
            ShootResultReceived?.Invoke(result);
            
            // Симулируем выстрел противника через некоторое время
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                if (_currentRoomId != null)
                {
                    OpponentShootReceived?.Invoke(new ShootMessage
                    {
                        Row = _random.Next(0, 10),
                        Col = _random.Next(0, 10),
                        RoomId = _currentRoomId,
                        Type = "Shoot"
                    });
                }
            });
            
            return true;
        }

        public async Task<bool> SendShipPlacementAsync(List<ShipPlacementData> ships, string roomId)
        {
            Console.WriteLine($"Mock send ship placement: {ships.Count} ships in room {roomId}");
            await Task.Delay(200);
            
            // Симулируем готовность к игре
            GameStateChanged?.Invoke(new GameStateMessage
            {
                State = GameState.YourTurn,
                RoomId = roomId,
                Type = "GameState"
            });
            
            return true;
        }
    }
}

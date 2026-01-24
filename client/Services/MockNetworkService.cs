using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BattleOfSea.Models;

namespace BattleOfSea.Services
{
    [Obsolete("MockNetworkService is deprecated. Use NetworkService with a real server.", true)]
    public class MockNetworkService : INetworkService
    {
        public bool IsConnected => throw new NotSupportedException("MockNetworkService is not supported. Connect to a real server on port 5000.");
        public Room? CurrentRoom => throw new NotSupportedException("MockNetworkService is not supported. Connect to a real server on port 5000.");

        // События интерфейса
        public event Action<ShootResultMessage>? ShootResultReceived { add { } remove { } }
        public event Action<ShootMessage>? OpponentShootReceived { add { } remove { } }
        public event Action<GameStateMessage>? GameStateChanged { add { } remove { } }
        public event Action<RoomsListMessage>? RoomsListUpdated { add { } remove { } }
        public event Action<JoinRoomMessage>? JoinRoomResult { add { } remove { } }
        public event Action<UserConnectedMessage>? UserConnected { add { } remove { } }
        public event Action<string>? ConnectionError { add { } remove { } }
        public event Action<ChatMessage>? ChatMessageReceived { add { } remove { } } // исправлено

        // Методы интерфейса
        public Task<bool> ConnectAsync(string userId, string displayName) =>
            throw new NotSupportedException("MockNetworkService is not supported. Use NetworkService to connect to the real server on port 5000.");

        public Task DisconnectAsync() =>
            throw new NotSupportedException("MockNetworkService is not supported. Use NetworkService.");

        public Task<List<Room>> GetRoomsAsync() =>
            throw new NotSupportedException("MockNetworkService is not supported. Use NetworkService to connect to the real server.");

        public Task<bool> JoinRoomAsync(Room room, string? password = null) =>
            throw new NotSupportedException("MockNetworkService is not supported. Use NetworkService.");

        public Task<bool> CreateRoomAsync(Room room, string? password = null) =>
            throw new NotSupportedException("MockNetworkService is not supported. Use NetworkService.");

        public Task LeaveRoomAsync() =>
            throw new NotSupportedException("MockNetworkService is not supported. Use NetworkService.");

        public Task<bool> SendShootAsync(int row, int col, string roomId) =>
            throw new NotSupportedException("MockNetworkService is not supported. Use NetworkService.");

        public Task<bool> SendShipPlacementAsync(List<ShipPlacementData> ships, string roomId) =>
            throw new NotSupportedException("MockNetworkService is not supported. Use NetworkService.");

        public Task<bool> SendPlayerReadyAsync(string roomId) =>
            throw new NotSupportedException("MockNetworkService is not supported. Use NetworkService.");

        public Task<bool> SendChatMessageAsync(string message, string? roomId = null) =>
            throw new NotSupportedException("MockNetworkService is not supported. Use NetworkService."); // исправлено на Task<bool>
    }
}

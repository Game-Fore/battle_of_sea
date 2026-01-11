// Интерфейс сети
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BattleOfSea.Models;

namespace BattleOfSea.Services
{
    public interface INetworkService
    {
        // Подключение
        Task<bool> ConnectAsync(string userId, string displayName);
        Task DisconnectAsync();
        bool IsConnected { get; }

        // Комнаты
        Task<List<Room>> GetRoomsAsync();
        Task<bool> JoinRoomAsync(Room room, string? password = null);
        Task<bool> CreateRoomAsync(Room room, string? password = null);
        Task LeaveRoomAsync();

        // Игровые действия
        Task<bool> SendShootAsync(int row, int col, string roomId);
        Task<bool> SendShipPlacementAsync(List<ShipPlacementData> ships, string roomId);

        // События
        event Action<ShootResultMessage>? ShootResultReceived;
        event Action<ShootMessage>? OpponentShootReceived;
        event Action<GameStateMessage>? GameStateChanged;
        event Action<RoomsListMessage>? RoomsListUpdated;
        event Action<JoinRoomMessage>? JoinRoomResult;
        event Action<UserConnectedMessage>? UserConnected;
        event Action<string>? ConnectionError;
    }
}

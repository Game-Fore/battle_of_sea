// Схемы сообщений
using System;
using System.Collections.Generic;

namespace BattleOfSea.Models
{
    /// <summary>
    /// Базовый класс для всех сетевых сообщений
    /// </summary>
    public abstract class NetworkMessage
    {
        public string Type { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Сообщение о выстреле
    /// </summary>
    public class ShootMessage : NetworkMessage
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public string RoomId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Результат выстрела
    /// </summary>
    public class ShootResultMessage : NetworkMessage
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public bool IsHit { get; set; }
        public bool IsSunk { get; set; }
        public bool IsGameOver { get; set; }
        public bool IsWinner { get; set; }
    }

    /// <summary>
    /// Сообщение о размещении кораблей
    /// </summary>
    public class ShipPlacementMessage : NetworkMessage
    {
        public List<ShipPlacementData> Ships { get; set; } = new List<ShipPlacementData>();
        public string RoomId { get; set; } = string.Empty;
    }

    public class ShipPlacementData
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public int Size { get; set; }
        public bool IsHorizontal { get; set; }
    }

    /// <summary>
    /// Сообщение о смене состояния игры
    /// </summary>
    public class GameStateMessage : NetworkMessage
    {
        public GameState State { get; set; }
        public string RoomId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Сообщение о списке комнат
    /// </summary>
    public class RoomsListMessage : NetworkMessage
    {
        public List<Room> Rooms { get; set; } = new List<Room>();
    }

    /// <summary>
    /// Сообщение о присоединении к комнате
    /// </summary>
    public class JoinRoomMessage : NetworkMessage
    {
        public string RoomId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public bool Success { get; set; }
    }

    /// <summary>
    /// Сообщение о создании комнаты
    /// </summary>
    public class CreateRoomMessage : NetworkMessage
    {
        public string RoomName { get; set; } = string.Empty;
        public int MaxPlayers { get; set; }
        public bool IsPrivate { get; set; }
        public string? Password { get; set; }
    }

    /// <summary>
    /// Сообщение о подключении пользователя
    /// </summary>
    public class UserConnectedMessage : NetworkMessage
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}


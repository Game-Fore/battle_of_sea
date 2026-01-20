// Схемы сообщений
using System;
using System.Collections.Generic;

namespace BattleOfSea.Models
{
    // Базовый класс для всех сетевых сообщений
    public abstract class NetworkMessage
    {
        // Тип сообщения
        public string Type { get; set; } = string.Empty;
        // Время отправки
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // Сообщение о выстреле
    public class ShootMessage : NetworkMessage
    {
        // Строка выстрела
        public int Row { get; set; }
        // Столбец выстрела
        public int Col { get; set; }
        // ID комнаты
        public string RoomId { get; set; } = string.Empty;
    }

    // Результат выстрела
    public class ShootResultMessage : NetworkMessage
    {
        // Строка выстрела
        public int Row { get; set; }
        // Столбец выстрела
        public int Col { get; set; }
        // Попадание ли
        public bool IsHit { get; set; }
        // Потоплен ли корабль
        public bool IsSunk { get; set; }
        // Завершена ли игра
        public bool IsGameOver { get; set; }
        // Победитель ли
        public bool IsWinner { get; set; }
    }

    // Сообщение о размещении кораблей
    public class ShipPlacementMessage : NetworkMessage
    {
        // Список размещенных кораблей
        public List<ShipPlacementData> Ships { get; set; } = new List<ShipPlacementData>();
        // ID комнаты
        public string RoomId { get; set; } = string.Empty;
    }

    // Данные размещения корабля
    public class ShipPlacementData
    {
        // Строка размещения
        public int Row { get; set; }
        // Столбец размещения
        public int Col { get; set; }
        // Размер корабля
        public int Size { get; set; }
        // Горизонтальное ли размещение
        public bool IsHorizontal { get; set; }
    }

    // Сообщение о смене состояния игры
    public class GameStateMessage : NetworkMessage
    {
        // Новое состояние игры
        public GameState State { get; set; }
        // ID комнаты
        public string RoomId { get; set; } = string.Empty;
    }

    // Сообщение о списке комнат
    public class RoomsListMessage : NetworkMessage
    {
        // Список доступных комнат
        public List<Room> Rooms { get; set; } = new List<Room>();
    }

    // Сообщение о присоединении к комнате
    public class JoinRoomMessage : NetworkMessage
    {
        // ID комнаты
        public string RoomId { get; set; } = string.Empty;
        // ID пользователя
        public string UserId { get; set; } = string.Empty;
        // Успешно ли присоединение
        public bool Success { get; set; }
        // Сообщение об ошибке или статус
        public string Message { get; set; } = string.Empty;
    }

    // Сообщение о создании комнаты
    public class CreateRoomMessage : NetworkMessage
    {
        // Название комнаты
        public string RoomName { get; set; } = string.Empty;
        // Максимум игроков
        public int MaxPlayers { get; set; }
        // Приватная ли комната
        public bool IsPrivate { get; set; }
        // Пароль комнаты
        public string? Password { get; set; }
    }

    // Сообщение о подключении пользователя
    public class UserConnectedMessage : NetworkMessage
    {
        // ID пользователя
        public string UserId { get; set; } = string.Empty;
        // Отображаемое имя
        public string DisplayName { get; set; } = string.Empty;
    }
}
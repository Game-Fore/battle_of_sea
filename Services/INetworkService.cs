// Интерфейс сети
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BattleOfSea.Models;

namespace BattleOfSea.Services
{
    public interface INetworkService
    {
        // Подключение к серверу
        Task<bool> ConnectAsync(string userId, string displayName);
        // Отключение от сервера
        Task DisconnectAsync();
        // Флаг подключения
        bool IsConnected { get; }

        // Комнаты
        Task<List<Room>> GetRoomsAsync(); // Получить список игровых комнат
        Task<bool> JoinRoomAsync(Room room, string? password = null); // Присоединиться к комнате
        Task<bool> CreateRoomAsync(Room room, string? password = null); // Создать новую комнату
        Task LeaveRoomAsync(); // Покинуть текущую комнату

        // Игровые действия
        Task<bool> SendShootAsync(int row, int col, string roomId); // Отправить выстрел
        Task<bool> SendShipPlacementAsync(List<ShipPlacementData> ships, string roomId); // Отправить расстановку кораблей

        // События сетевого взаимодействия
        event Action<ShootResultMessage>? ShootResultReceived; // Получен результат выстрела
        event Action<ShootMessage>? OpponentShootReceived; // Противник сделал выстрел
        event Action<GameStateMessage>? GameStateChanged; // Изменилось состояние игры
        event Action<RoomsListMessage>? RoomsListUpdated; // Обновился список комнат
        event Action<JoinRoomMessage>? JoinRoomResult; // Результат присоединения к комнате
        event Action<UserConnectedMessage>? UserConnected; // Пользователь подключился
        event Action<string>? ConnectionError; // Ошибка соединения
    }
}
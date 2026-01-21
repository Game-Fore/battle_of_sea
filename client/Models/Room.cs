// Комната данных
namespace BattleOfSea.Models
{
    // Класс игровой комнаты
    public class Room
    {
        // ID комнаты
        public string Id { get; set; } = string.Empty;
        // Название комнаты
        public string Name { get; }
        // Текущее количество игроков
        public int Players { get; }
        // Максимальное количество игроков
        public int MaxPlayers { get; }
        // Приватная ли комната
        public bool IsPrivate { get; }
        // Тип игры
        public string? GameType { get; }
        // Отображение количества игроков
        public string PlayersDisplay => $"{Players}/{MaxPlayers}";
        // Можно ли присоединиться
        public bool IsJoinAllowed => Players < MaxPlayers;

        // Конструктор комнаты
        public Room(string name, int players, int maxPlayers)
            : this(name, players, maxPlayers, false, null)
        {
        }

        // Расширенный конструктор комнаты
        public Room(string name, int players, int maxPlayers, bool isPrivate, string? gameType)
        {
            Name = name;
            Players = players;
            MaxPlayers = maxPlayers;
            IsPrivate = isPrivate;
            GameType = gameType;
        }
    }
}
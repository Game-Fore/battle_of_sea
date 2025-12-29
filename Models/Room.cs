namespace BattleOfSea.Models
{
    public class Room
    {
        public string Name { get; }
        public int Players { get; }
        public int MaxPlayers { get; }
        public bool IsPrivate { get; }
        public string? GameType { get; }
        public string PlayersDisplay => $"{Players}/{MaxPlayers}";

        public Room(string name, int players, int maxPlayers)
            : this(name, players, maxPlayers, false, null)
        {
        }

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

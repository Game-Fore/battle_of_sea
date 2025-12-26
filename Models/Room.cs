namespace BattleOfSea.Models
{
    public class Room
    {
        public string Name { get; }
        public int Players { get; }
        public int MaxPlayers { get; }
        public string PlayersDisplay => $"{Players}/{MaxPlayers}";

        public Room(string name, int players, int maxPlayers)
        {
            Name = name;
            Players = players;
            MaxPlayers = maxPlayers;
        }
    }
}

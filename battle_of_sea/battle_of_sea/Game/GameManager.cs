using System.Collections.Generic;

namespace battle_of_sea.Game
{
    public class GameManager
    {
        public List<GameSession> ActiveGames { get; private set; } = new List<GameSession>();
        public List<Player> WaitingPlayers { get; private set; } = new List<Player>();

        public void AddPlayer(Player player)
        {
            WaitingPlayers.Add(player);

            // Если есть 2 игрока — создаём игру
            if (WaitingPlayers.Count >= 2)
            {
                var p1 = WaitingPlayers[0];
                var p2 = WaitingPlayers[1];
                WaitingPlayers.RemoveRange(0, 2);

                var game = new GameSession(p1, p2);
                ActiveGames.Add(game);

                Console.WriteLine($"Game created: {p1.Name} vs {p2.Name}");
            }
        }
        public GameSession FindGameByPlayerId(string playerId)
        {
            return ActiveGames.Find(g => g.Player1.Id == playerId || g.Player2.Id == playerId);
        }
        public Player FindPlayerById(string playerId)
        {
            foreach (var game in ActiveGames)
            {
                if (game.Player1.Id == playerId) return game.Player1;
                if (game.Player2.Id == playerId) return game.Player2;
            }
            return null;
        }

    }
}

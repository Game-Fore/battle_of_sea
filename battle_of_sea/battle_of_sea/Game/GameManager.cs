using System.Collections.Generic;
using System.Linq;

namespace battle_of_sea.Game
{
    public class Room
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int MaxPlayers { get; set; }
        public List<Player> Players { get; set; } = new List<Player>();
        public Dictionary<string, bool> PlayerReadyStatus { get; set; } = new Dictionary<string, bool>();
        public string? Password { get; set; }
        public bool IsGameStarted { get; set; } = false;

        public Room(string name, int maxPlayers)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            MaxPlayers = maxPlayers;
        }

        public bool AreAllPlayersReady()
        {
            if (Players.Count < MaxPlayers)
                return false;

            foreach (var player in Players)
            {
                if (!PlayerReadyStatus.ContainsKey(player.Id) || !PlayerReadyStatus[player.Id])
                    return false;
            }
            return true;
        }
    }

    public class GameManager
    {
        public List<GameSession> ActiveGames { get; private set; } = new List<GameSession>();
        public List<Player> Players { get; private set; } = new List<Player>(); // Все игроки
        public List<Room> Rooms { get; private set; } = new List<Room>();
        private Dictionary<string, Player> AllPlayers { get; set; } = new Dictionary<string, Player>();

        public void AddPlayer(Player player)
        {
            AllPlayers[player.Id] = player;
            if (!Players.Contains(player))
            {
                Players.Add(player);
            }
            Console.WriteLine($"Player added: {player.Name}");
        }

        public void RemovePlayer(string playerId)
        {
            if (AllPlayers.TryGetValue(playerId, out var player))
            {
                AllPlayers.Remove(playerId);
                Players.Remove(player);
                
                // Удаляем игрока из комнаты
                var room = Rooms.FirstOrDefault(r => r.Players.Contains(player));
                if (room != null)
                {
                    room.Players.Remove(player);
                }
                
                // Если игрок был в игре, удаляем игру
                var game = ActiveGames.FirstOrDefault(g => g.Player1.Id == playerId || g.Player2.Id == playerId);
                if (game != null)
                {
                    RemoveGame(game);
                }
                
                Console.WriteLine($"Player removed: {player.Name}");
            }
        }

        public GameSession? FindGameByPlayerId(string playerId)
        {
            return ActiveGames.Find(g => g.Player1.Id == playerId || g.Player2.Id == playerId);
        }

        public Player? FindPlayerById(string playerId)
        {
            if (AllPlayers.TryGetValue(playerId, out var player))
                return player;

            foreach (var game in ActiveGames)
            {
                if (game.Player1.Id == playerId) return game.Player1;
                if (game.Player2.Id == playerId) return game.Player2;
            }
            return null;
        }

        public Room CreateRoom(string name, int maxPlayers)
        {
            var room = new Room(name, maxPlayers);
            Rooms.Add(room);
            return room;
        }

        public Room? FindRoomById(string roomId)
        {
            return Rooms.FirstOrDefault(r => r.Id == roomId);
        }

        public List<Room> GetRooms()
        {
            // Возвращаем ВСЕ комнаты - как пустые (только созданные), так и с игроками
            return Rooms.ToList();
        }

        public void JoinRoom(Player player, Room room)
        {
            if (!room.Players.Contains(player))
            {
                room.Players.Add(player);
            }

            // Если комната полна, начинаем игру
            if (room.Players.Count >= room.MaxPlayers)
            {
                var game = new GameSession(room.Players[0], room.Players[1]);
                
                // Подписываемся на событие завершения игры
                game.GameFinished += FinishGame;
                
                ActiveGames.Add(game);
                room.IsGameStarted = true;

                Console.WriteLine($"Game started in room {room.Name}: {room.Players[0].Name} vs {room.Players[1].Name}");
            }
        }

        public void RemoveGame(GameSession game)
        {
            ActiveGames.Remove(game);
            var room = Rooms.FirstOrDefault(r => r.Players.Contains(game.Player1) || r.Players.Contains(game.Player2));
            if (room != null)
            {
                room.Players.Clear();
                room.IsGameStarted = false;
            }
            Console.WriteLine($"Game removed: {game.Player1.Name} vs {game.Player2.Name}");
        }

        public void FinishGame(GameSession game)
        {
            Console.WriteLine($"Game finished: {game.Player1.Name} vs {game.Player2.Name}");
            RemoveGame(game);
        }

        public Room? FindRoomByPlayerId(string playerId)
        {
            return Rooms.FirstOrDefault(r => r.Players.Any(p => p.Id == playerId));
        }

        public bool MarkPlayerReady(string playerId)
        {
            var room = FindRoomByPlayerId(playerId);
            if (room == null)
            {
                Console.WriteLine($"[ERROR] Player {playerId} not in any room");
                return false;
            }

            room.PlayerReadyStatus[playerId] = true;
            Console.WriteLine($"[READY] Player {playerId} is ready in room {room.Name}");
            
            return true;
        }

        public bool CheckAndStartGame(Room room)
        {
            if (!room.AreAllPlayersReady() || room.Players.Count < room.MaxPlayers)
            {
                return false;
            }

            var game = new GameSession(room.Players[0], room.Players[1]);
            game.GameFinished += FinishGame;
            ActiveGames.Add(game);
            room.IsGameStarted = true;

            Console.WriteLine($"[GAME_START] Game started in room {room.Name}: {room.Players[0].Name} vs {room.Players[1].Name}");
            return true;
        }

        public List<Room> GetAvailableRooms()
        {
            return Rooms.Where(r => !r.IsGameStarted && r.Players.Count < r.MaxPlayers).ToList();
        }

        public async Task BroadcastToRoom(Room room, object message)
        {
            foreach (var player in room.Players)
            {
                if (player.Connection is Network.ClientConnection connection)
                {
                    await connection.SendAsync((Protocol.ServerMessage)message);
                }
            }
        }
    }
}

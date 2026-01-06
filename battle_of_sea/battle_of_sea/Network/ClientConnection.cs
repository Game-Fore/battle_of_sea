using battle_of_sea.Game;
using battle_of_sea.Protocol;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace battle_of_sea.Network
{
    public class ClientConnection
    {
        private readonly TcpClient _client;
        private Player _player;

        public ClientConnection(TcpClient client)
        {
            _client = client;
        }
        public class GameServer
        {
            private static GameServer _instance;
            public static GameServer Instance => _instance ??= new GameServer();

            public GameManager GameManager { get; private set; } = new GameManager();
        }
        public async Task HandleAsync()
        {
            try
            {
                using (_client)
                {
                    var stream = _client.GetStream();
                    var reader = new StreamReader(stream, Encoding.UTF8);
                    var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    while (true)
                    {
                        string line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        Console.WriteLine($"Received: {line}");

                        ClientMessage message;
                        try
                        {
                            message = JsonSerializer.Deserialize<ClientMessage>(line, options);
                        }
                        catch
                        {
                            await SendAsync( new ServerMessage { Type = "error", Payload = new { message = "Invalid JSON" } });
                            continue;
                        }

                        if (message == null || string.IsNullOrEmpty(message.Type))
                        {
                            await SendAsync( new ServerMessage { Type = "error", Payload = new { message = "Missing Type" } });
                            continue;
                        }

                        await HandleMessageAsync(message, writer);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Client disconnected");
            }
        }

        private async Task HandleMessageAsync(ClientMessage message, StreamWriter writer)
        {
            switch (message.Type.ToLower())
            {
                case "connect":
                    string playerName = message.Payload.GetProperty("playerName").GetString();
                    string playerId = Guid.NewGuid().ToString();

                    _player = new Player
                    {
                        Id = playerId,
                        Name = playerName,
                        Connection = this
                    };

                    GameServer.Instance.GameManager.AddPlayer(_player);

                    await SendAsync( new ServerMessage
                    {
                        Type = "connected",
                        Payload = new { playerId }
                    });

                    Console.WriteLine($"Player connected: {playerName} ({playerId})");
                    break;

                case "ping":
                    await SendAsync( new ServerMessage { Type = "pong", Payload = new { } });
                    break;

                case "shoot":
                    if (_player == null)
                    {
                        await SendAsync(new ServerMessage { Type = "error", Payload = new { message = "Not connected" } });
                        break;
                    }

                    int x = message.Payload.GetProperty("x").GetInt32();
                    int y = message.Payload.GetProperty("y").GetInt32();

                    var game = GameServer.Instance.GameManager.FindGameByPlayerId(_player.Id);
                    if (game == null)
                    {
                        await SendAsync(new ServerMessage { Type = "error", Payload = new { message = "Not in a game" } });
                        break;
                    }

                    if (game.CurrentTurnPlayerId != _player.Id)
                    {
                        await SendAsync(new ServerMessage { Type = "error", Payload = new { message = "Not your turn" } });
                        break;
                    }

                    var opponent = game.GetOpponentPlayer();
                    bool hit = opponent.Board.Shoot(x, y);

                    // Отправляем результат стреляющему
                    await SendAsync(new ServerMessage { Type = "shoot_result", Payload = new { x, y, hit } });

                    // Уведомление противнику
                    await opponent.Connection.SendAsync(new ServerMessage { Type = "opponent_shot", Payload = new { x, y, hit } });

                    // Проверяем победу
                    if (opponent.Board.IsDefeated())
                    {
                        await SendAsync(new ServerMessage { Type = "game_over", Payload = new { winner = _player.Name } });
                        await opponent.Connection.SendAsync(new ServerMessage { Type = "game_over", Payload = new { winner = _player.Name } });
                    }
                    else
                    {
                        // Передача хода
                        game.SwitchTurn();
                    }
                    break;


                default:
                    await SendAsync( new ServerMessage { Type = "error", Payload = new { message = "Unknown command" } });
                    break;
            }
        }

        public Task SendAsync(ServerMessage message)
        {
            var stream = _client.GetStream();
            var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            string json = JsonSerializer.Serialize(message);
            return writer.WriteLineAsync(json);
        }
    }
}

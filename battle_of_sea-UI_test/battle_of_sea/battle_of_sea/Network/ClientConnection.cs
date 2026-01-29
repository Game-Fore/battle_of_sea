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
        public async Task HandleAsync()
        {
            try
            {
                using (_client)
                {
                    var stream = _client.GetStream();
                    var reader = new StreamReader(stream, new UTF8Encoding(false));
                    var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

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
                if (_player != null)
                {
                    Console.WriteLine($"Player disconnected: {_player.Name}");
                    // ❗ НЕ удаляем игрока из игры
                    // он может переподключиться
                }
                ;
            }
        }

        private async Task HandleMessageAsync(ClientMessage message, StreamWriter writer)
        {
            switch (message.Type.ToLower())
            {
                case "connect":
                    {
                        string playerName = message.Payload.GetProperty("playerName").GetString();
                        string playerId = Guid.NewGuid().ToString();

                        _player = new Player
                        {
                            Id = playerId,
                            Name = playerName,
                            Connection = this
                        };

                        GameServer.Instance.GameManager.AddPlayer(_player);

                        await SendAsync(new ServerMessage
                        {
                            Type = "connected",
                            Payload = new { playerId }
                        });

                        Console.WriteLine($"Player connected: {playerName} ({playerId})");
                        break;
                    }
                case "ping":
                    await SendAsync( new ServerMessage { Type = "pong", Payload = new { } });
                    break;

                case "shoot":
                   
                    {
                        if (_player == null)
                        {
                            await SendAsync(new ServerMessage
                            {
                                Type = "error",
                                Payload = new { message = "Not connected" }
                            });
                            break;
                        }

                        var game = GameServer.Instance.GameManager.FindGameByPlayerId(_player.Id);
                        if (game == null)
                        {
                            await SendAsync(new ServerMessage
                            {
                                Type = "error",
                                Payload = new { message = "Not in a game" }
                            });
                            break;
                        }

                        int x = message.Payload.GetProperty("x").GetInt32();
                        int y = message.Payload.GetProperty("y").GetInt32();

                        await game.ProcessShotAsync(_player, x, y);

                        break;
                    }
                case "reconnect":
                    {
                        string playerId = message.Payload.GetProperty("playerId").GetString();

                        var player = GameServer.Instance.GameManager.FindPlayerById(playerId);
                        if (player == null)
                        {
                            await SendAsync(new ServerMessage
                            {
                                Type = "error",
                                Payload = new { message = "Player not found" }
                            });
                            break;
                        }

                        // 🔄 Перепривязываем соединение
                        player.Connection = this;
                        _player = player;

                        await SendAsync(new ServerMessage
                        {
                            Type = "reconnected",
                            Payload = new
                            {
                                playerId = player.Id,
                                opponent = GameServer.Instance.GameManager
                                    .FindGameByPlayerId(player.Id)
                                    ?.GetOpponentPlayer().Name
                            }
                        });

                        Console.WriteLine($"Player reconnected: {player.Name}");
                        break;
                    }

                

                default:
                    await SendAsync( new ServerMessage { Type = "error", Payload = new { message = "Unknown command" } });
                    break;
            }
        }

        public Task SendAsync(ServerMessage message)
        {
            var stream = _client.GetStream();
            var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            string json = JsonSerializer.Serialize(message);
            return writer.WriteLineAsync(json);
        }

    }
}

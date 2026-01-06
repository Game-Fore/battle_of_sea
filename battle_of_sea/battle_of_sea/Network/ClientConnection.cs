using battle_of_sea.Game;
using battle_of_sea.Protocol;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using battle_of_sea.Game;

namespace battle_of_sea.Network
{
    public class GameServer
    {
        private static GameServer _instance;
        public static GameServer Instance => _instance ??= new GameServer();

        public GameManager GameManager { get; private set; } = new GameManager();
    }
    public class ClientConnection
    {
        private readonly TcpClient _client;

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
                    var reader = new StreamReader(stream, Encoding.UTF8);
                    var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // Игнорируем регистр JSON
                    };

                    while (true)
                    {
                        string line = await reader.ReadLineAsync();

                        if (string.IsNullOrWhiteSpace(line))
                        {
                            // Игнорируем пустые строки
                            continue;
                        }

                        Console.WriteLine($"Received: {line}");

                        ClientMessage message;
                        try
                        {
                            message = JsonSerializer.Deserialize<ClientMessage>(line, options);
                        }
                        catch (JsonException)
                        {
                            await SendAsync(writer, new ServerMessage
                            {
                                Type = "error",
                                Payload = new { message = "Invalid JSON" }
                            });
                            continue;
                        }

                        if (message == null || string.IsNullOrEmpty(message.Type))
                        {
                            await SendAsync(writer, new ServerMessage
                            {
                                Type = "error",
                                Payload = new { message = "Missing Type" }
                            });
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

                    var player = new Player
                    {
                        Id = playerId,
                        Name = playerName,
                        Connection = this
                    };

                    GameServer.Instance.GameManager.AddPlayer(player);

                    await SendAsync(writer, new ServerMessage
                    {
                        Type = "connected",
                        Payload = new { playerId }
                    });

                    Console.WriteLine($"Player connected: {playerName} ({playerId})");
                    break;


                case "ping":
                    await SendAsync(writer, new ServerMessage
                    {
                        Type = "pong",
                        Payload = new { }
                    });
                    break;

                default:
                    await SendAsync(writer, new ServerMessage
                    {
                        Type = "error",
                        Payload = new { message = "Unknown command" }
                    });
                    break;
            }
        }

        private Task SendAsync(StreamWriter writer, ServerMessage message)
        {
            string json = JsonSerializer.Serialize(message);
            return writer.WriteLineAsync(json);
        }
    }
}

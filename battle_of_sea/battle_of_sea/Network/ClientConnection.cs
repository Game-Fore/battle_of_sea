using System.Net.Sockets;

namespace battle_of_sea.Network
{
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
                    var buffer = new byte[1024];

                    while (true)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                        {
                            break; // клиент отключился
                        }

                        var message = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"Received: {message}");
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
    }
}

using battle_of_sea.Network;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting WebSocket server...");

        var server = new WebSocketListener(5555);
        await server.StartAsync();
    }
}

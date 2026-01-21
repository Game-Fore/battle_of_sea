using battle_of_sea.Network;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting server...");

        var server = new ServerListener(5000);
        await server.StartAsync();
    }
}

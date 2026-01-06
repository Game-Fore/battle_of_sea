using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BattleSea.ChatServer.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _users = new();

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"✅ Новый пользователь подключился: {Context.ConnectionId}");
            await Clients.Caller.SendAsync("ReceiveMessage", "System", "Вы подключены к серверу чата!");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"❌ Пользователь отключился: {Context.ConnectionId}");
            if (_users.TryRemove(Context.ConnectionId, out var userName))
            {
                await Clients.All.SendAsync("ReceiveMessage", "System", $"{userName} покинул чат");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task Register(string userName)
        {
            Console.WriteLine($"📝 Регистрация: {userName} (ConnectionId: {Context.ConnectionId})");
            _users[Context.ConnectionId] = userName;
            await Clients.All.SendAsync("ReceiveMessage", "System", $"{userName} присоединился к чату");
        }

        public async Task SendMessage(string user, string message)
        {
            Console.WriteLine($"💬 Сообщение от {user}: {message}");
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task<string[]> GetOnlineUsers()
        {
            return _users.Values.ToArray();
        }
    }
}
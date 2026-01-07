using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BattleSea.ChatServer.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _users = new();

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("ReceiveSystemMessage", "Добро пожаловать в чат!");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_users.TryRemove(Context.ConnectionId, out var userName))
            {
                await Clients.All.SendAsync("ReceiveMessage", "System", $"{userName} покинул чат");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task Register(string userName)
        {
            _users[Context.ConnectionId] = userName;
            await Clients.Caller.SendAsync("ReceiveSystemMessage", $"Вы зарегистрированы как {userName}");
            await Clients.Others.SendAsync("ReceiveMessage", "System", $"{userName} присоединился к чату");
        }

        public async Task SendMessage(string message)
        {
            if (_users.TryGetValue(Context.ConnectionId, out var userName))
            {
                await Clients.All.SendAsync("ReceiveMessage", userName, message);
            }
        }

        public async Task SendPrivateMessage(string targetUserName, string message)
        {
            if (_users.TryGetValue(Context.ConnectionId, out var senderName))
            {
                // Находим получателя
                var receiver = _users.FirstOrDefault(x => x.Value == targetUserName);

                if (!string.IsNullOrEmpty(receiver.Key))
                {
                    // Отправляем отправителю
                    await Clients.Caller.SendAsync("ReceiveMessage", $"[Приватно] {senderName}", message);

                    // Отправляем получателю
                    await Clients.Client(receiver.Key).SendAsync("ReceiveMessage", $"[Приватно] {senderName}", message);
                }
                else
                {
                    await Clients.Caller.SendAsync("ReceiveSystemMessage", $"Пользователь {targetUserName} не найден");
                }
            }
        }
    }
}
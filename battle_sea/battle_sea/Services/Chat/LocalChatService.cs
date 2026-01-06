using battle_sea.Models.Chat;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace battle_sea.Services.Chat
{
    // Временная реализация для тестов. Все сообщения хранятся в памяти.
    public class LocalChatService : IChatService
    {
        private string _currentPlayerName = string.Empty;

        public event Action<ChatMessage>? MessageReceived;
        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();

        public Task ConnectAsync(string playerName)
        {
            _currentPlayerName = playerName;
            // Имитируем подключение
            var welcomeMessage = new ChatMessage
            {
                SenderName = "System",
                Message = $"Добро пожаловать в чат, {playerName}!",
                Channel = ChatChannel.Global
            };
            Messages.Add(welcomeMessage);
            MessageReceived?.Invoke(welcomeMessage);

            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            // Ничего не делаем в локальной версии
            return Task.CompletedTask;
        }

        public async Task SendMessageAsync(ChatMessage message)
        {
            // Заполняем отправителя, если не указан
            if (string.IsNullOrEmpty(message.SenderName))
            {
                message.SenderName = _currentPlayerName;
            }

            // Имитируем небольшую задержку сети
            await Task.Delay(50);

            // Добавляем сообщение в историю
            Messages.Add(message);
            MessageReceived?.Invoke(message);

            // Локально имитируем "ответ бота" на глобальные сообщения
            if (message.Channel == ChatChannel.Global &&
                !message.SenderName.Equals("System", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(200);
                var echoMessage = new ChatMessage
                {
                    SenderName = "Bot",
                    Message = $"Получил: {message.Message}",
                    Channel = message.Channel
                };
                Messages.Add(echoMessage);
                MessageReceived?.Invoke(echoMessage);
            }
        }
    }
}
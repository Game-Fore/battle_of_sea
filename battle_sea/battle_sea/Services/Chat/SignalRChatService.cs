using battle_sea.Models.Chat;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace battle_sea.Services.Chat
{
    public class SignalRChatService : IChatService, IDisposable
    {
        private HubConnection _hubConnection;
        private string _currentPlayerName = string.Empty;

        public event Action<ChatMessage> MessageReceived;
        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();

        public async Task ConnectAsync(string playerName)
        {
            _currentPlayerName = playerName;

            Console.WriteLine($"🔄 Подключение к серверу чата как {playerName}...");

            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5172/chathub")
                .Build();

            // Подписываемся на сообщения от сервера
            _hubConnection.On<string, string>("ReceiveMessage", (sender, message) =>
            {
                Console.WriteLine($"📨 Получено сообщение от {sender}: {message}");

                var chatMessage = new ChatMessage
                {
                    SenderName = sender,
                    Message = message,
                    Channel = ChatChannel.Global,
                    Timestamp = DateTime.Now
                };

                Messages.Add(chatMessage);
                MessageReceived?.Invoke(chatMessage);
            });

            _hubConnection.On<string>("ReceiveSystemMessage", (message) =>
            {
                Console.WriteLine($"ℹ️ Системное сообщение: {message}");

                var systemMessage = new ChatMessage
                {
                    SenderName = "System",
                    Message = message,
                    Channel = ChatChannel.Global,
                    Timestamp = DateTime.Now
                };

                Messages.Add(systemMessage);
                MessageReceived?.Invoke(systemMessage);
            });

            try
            {
                Console.WriteLine("🔄 Устанавливаем соединение...");
                await _hubConnection.StartAsync();
                Console.WriteLine("✅ Подключено к серверу чата");

                // Регистрируем пользователя на сервере
                await _hubConnection.InvokeAsync("Register", playerName);
                Console.WriteLine($"✅ Зарегистрирован как {playerName}");

                // Добавляем системное сообщение
                var welcomeMessage = new ChatMessage
                {
                    SenderName = "System",
                    Message = "Подключено к серверу чата",
                    Channel = ChatChannel.Global,
                    Timestamp = DateTime.Now
                };
                Messages.Add(welcomeMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка подключения: {ex.Message}");

                var errorMessage = new ChatMessage
                {
                    SenderName = "System",
                    Message = $"Ошибка подключения: {ex.Message}",
                    Channel = ChatChannel.Global,
                    Timestamp = DateTime.Now
                };
                Messages.Add(errorMessage);
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                Console.WriteLine("🔄 Отключение от сервера...");
                await _hubConnection.StopAsync();
                Console.WriteLine("✅ Отключено от сервера");
            }
        }

        public async Task SendMessageAsync(ChatMessage message)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                Console.WriteLine("❌ Не подключен к серверу");

                var errorMessage = new ChatMessage
                {
                    SenderName = "System",
                    Message = "Не подключен к серверу",
                    Channel = ChatChannel.Global,
                    Timestamp = DateTime.Now
                };
                Messages.Add(errorMessage);
                return;
            }

            try
            {
                Console.WriteLine($"📤 Отправка сообщения: {message.Message}");

                if (message.Channel == ChatChannel.Private && !string.IsNullOrEmpty(message.TargetPlayerId))
                {
                    await _hubConnection.InvokeAsync("SendPrivateMessage", message.TargetPlayerId, message.Message);
                    Console.WriteLine($"📨 Приватное сообщение для {message.TargetPlayerId}");
                }
                else
                {
                    await _hubConnection.InvokeAsync("SendMessage", message.Message);
                    Console.WriteLine("✅ Сообщение отправлено в общий чат");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки: {ex.Message}");

                var errorMessage = new ChatMessage
                {
                    SenderName = "System",
                    Message = $"Ошибка отправки: {ex.Message}",
                    Channel = ChatChannel.Global,
                    Timestamp = DateTime.Now
                };
                Messages.Add(errorMessage);
            }
        }

        public void Dispose()
        {
            _hubConnection?.DisposeAsync();
        }
    }
}
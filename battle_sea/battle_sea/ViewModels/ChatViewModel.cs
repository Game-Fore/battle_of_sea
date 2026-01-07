using Avalonia.Controls;
using battle_sea.Models.Chat;
using battle_sea.Services.Chat;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace battle_sea.ViewModels
{
    public class ChatViewModel : ViewModelBase
    {
        private readonly IChatService _chatService;
        private string _currentMessage = string.Empty;
        private string _selectedChannel = "Global";
        private string _targetPlayer = string.Empty;
        private bool _isConnected;

        public ChatViewModel(IChatService chatService)
        {
            _chatService = new SignalRChatService();

            // Инициализируем коллекции
            AvailableChannels = new ObservableCollection<string>(
                Enum.GetNames(typeof(ChatChannel)));

            // Подписываемся на входящие сообщения
            _chatService.MessageReceived += OnMessageReceived;

            // Создаем команды
            SendCommand = ReactiveCommand.CreateFromTask(async () => await SendMessageAsync());
            ConnectCommand = ReactiveCommand.CreateFromTask(async () => await ConnectAsync("Player_" + new Random().Next(1000)));
            DisconnectCommand = ReactiveCommand.CreateFromTask(async () => await DisconnectAsync());

            // Загружаем историю сообщений
            Messages = _chatService.Messages;
        }

        // Текущий текст сообщения
        public string CurrentMessage
        {
            get => _currentMessage;
            set => this.RaiseAndSetIfChanged(ref _currentMessage, value);
        }

        // Выбранный канал чата
        public string SelectedChannel
        {
            get => _selectedChannel;
            set => this.RaiseAndSetIfChanged(ref _selectedChannel, value);
        }

        // Имя игрока для личных сообщений
        public string TargetPlayer
        {
            get => _targetPlayer;
            set => this.RaiseAndSetIfChanged(ref _targetPlayer, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => this.RaiseAndSetIfChanged(ref _isConnected, value);
        }

        // Коллекция всех сообщений
        public ObservableCollection<ChatMessage> Messages { get; }

        // Список доступных каналов
        public ObservableCollection<string> AvailableChannels { get; }

        // Команды для UI
        public ICommand SendCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }

        private void OnMessageReceived(ChatMessage message)
        {
            // Можно добавить дополнительную логику
        }

        // Публичные методы для вызова из View
        public async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentMessage) || !IsConnected)
                return;

            var message = new ChatMessage
            {
                Message = CurrentMessage.Trim(),
                Channel = (ChatChannel)Enum.Parse(typeof(ChatChannel), SelectedChannel),
                TargetPlayerId = (SelectedChannel == "Private") ? TargetPlayer : null
            };

            try
            {
                await _chatService.SendMessageAsync(message);
                CurrentMessage = string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка отправки: {ex.Message}");
            }
        }

        public async Task ConnectAsync(string playerName)
        {
            try
            {
                await _chatService.ConnectAsync(playerName);
                IsConnected = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка подключения: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                await _chatService.DisconnectAsync();
                IsConnected = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка отключения: {ex.Message}");
            }
        }

        // Альтернативные публичные методы для более понятного вызова
        public async Task ConnectToChat(string playerName) => await ConnectAsync(playerName);
        public async Task DisconnectFromChat() => await DisconnectAsync();
        public async Task SendChatMessage() => await SendMessageAsync();
    }
}
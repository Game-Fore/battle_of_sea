using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using battle_sea.Models.Chat;
using battle_sea.Services.Chat;
using battle_sea.ViewModels;
using System;

namespace battle_sea.Views
{
    public partial class ChatView : UserControl
    {
        private ChatViewModel? _viewModel;
        private readonly IChatService _chatService;

        public ChatView()
        {
            InitializeComponent();

            // Создаем сервис и ViewModel
            _chatService = new LocalChatService();
            _viewModel = new ChatViewModel(_chatService);

            // Подписываемся на сообщения
            _chatService.MessageReceived += OnMessageReceived;

            // Обновляем кнопки при подключении
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ChatViewModel.IsConnected))
                {
                    UpdateConnectionButtons();
                }
            };
        }

        private void OnMessageReceived(Models.Chat.ChatMessage message)
        {
            // Добавляем сообщение в список
            Dispatcher.UIThread.Post(() =>
            {
                MessagesContainer.Items.Add($"[{message.Timestamp:HH:mm}] {message.SenderName}: {message.Message}");
            });
        }

        private void UpdateConnectionButtons()
        {
            if (_viewModel == null) return;

            ConnectButton.IsVisible = !_viewModel.IsConnected;
            DisconnectButton.IsVisible = _viewModel.IsConnected;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                await _viewModel.ConnectToChat($"Player_{new Random().Next(1000)}");
                UpdateConnectionButtons();
            }
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                await _viewModel.DisconnectFromChat();
                UpdateConnectionButtons();
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && !string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                _viewModel.CurrentMessage = MessageTextBox.Text;
                await _viewModel.SendChatMessage();
                MessageTextBox.Text = string.Empty;
            }
        }
    }
}
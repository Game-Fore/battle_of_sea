using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BattleOfSea.Services;
using BattleOfSea.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BattleOfSea.Views
{
    public partial class ChatWindow : Window
    {
        private bool isConnected = false;
        private string playerName = "Адмирал";
        private string userId = "";

        // Сетевой сервис для связи с сервером
        private NetworkService? _networkService;
        // Флаг использования только локального чата (без сервера)
        private bool _useLocalChat = false;

        // Элементы управления из XAML
        private TextBox? messageTextBox;
        private TextBox? serverTextBox;
        private TextBox? portTextBox;
        private TextBox? playerNameTextBox;
        private Button? connectButton;
        private Button? disconnectButton;
        private Button? sendButton;
        private Button? clearButton;
        private TextBlock? statusText;
        private Border? statusIndicator;
        private ScrollViewer? messagesScrollViewer;
        private StackPanel? messagesPanel;

        public ChatWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => ChatWindow_Loaded(s, e); // Когда окно загрузится
        }

        private void ChatWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            InitializeControls(); // Находим элементы
            InitializeChat();     // Настраиваем чат
        }

        private void InitializeControls()
        {
            try
            {
                // Получаем элементы из XAML по их именам
                messageTextBox = this.FindControl<TextBox>("MessageTextBox");
                serverTextBox = this.FindControl<TextBox>("ServerTextBox");
                portTextBox = this.FindControl<TextBox>("PortTextBox");
                playerNameTextBox = this.FindControl<TextBox>("PlayerNameTextBox");
                connectButton = this.FindControl<Button>("ConnectButton");
                disconnectButton = this.FindControl<Button>("DisconnectButton");
                sendButton = this.FindControl<Button>("SendButton");
                clearButton = this.FindControl<Button>("ClearButton");
                statusText = this.FindControl<TextBlock>("StatusText");
                statusIndicator = this.FindControl<Border>("StatusIndicator");
                messagesScrollViewer = this.FindControl<ScrollViewer>("MessagesScrollViewer");
                messagesPanel = this.FindControl<StackPanel>("MessagesPanel");
            }
            catch (Exception ex)
            {
                AddSystemMessage($"Ошибка загрузки интерфейса: {ex.Message}", true);
            }
        }

        private void InitializeChat()
        {
            // Устанавливаем имя игрока
            if (playerNameTextBox != null)
            {
                playerName = playerNameTextBox.Text?.Trim() ?? "Адмирал";
                playerNameTextBox.LostFocus += (s, e) => PlayerNameTextBox_LostFocus(s, e);
                playerNameTextBox.KeyDown += (s, e) => PlayerNameTextBox_KeyDown(s, e);
            }

            // Генерируем уникальный ID для пользователя
            userId = $"player_{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Назначаем обработчики кнопок
            if (connectButton != null)
                connectButton.Click += (s, e) => ConnectButton_Click(s, e);

            if (disconnectButton != null)
                disconnectButton.Click += (s, e) => DisconnectButton_Click(s, e);

            if (sendButton != null)
                sendButton.Click += (s, e) => SendButton_Click(s, e);

            if (clearButton != null)
                clearButton.Click += (s, e) => ClearButton_Click(s, e);

            // Enter в поле сообщения
            if (messageTextBox != null)
                messageTextBox.KeyDown += (s, e) => MessageTextBox_KeyDown(s, e);

            // Приветственные сообщения
            AddSystemMessage($"Вы вошли как: {playerName}");
            AddSystemMessage("Чат для переговоров во время морского сражения");
            AddSystemMessage("Подключитесь к серверу для общения с другими игроками");
        }

        // Обновляем имя при потере фокуса
        private void PlayerNameTextBox_LostFocus(object? sender, RoutedEventArgs e)
        {
            UpdatePlayerName();
        }

        // Обновляем имя при нажатии Enter
        private void PlayerNameTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UpdatePlayerName();
                e.Handled = true;
            }
        }

        // Обновление имени игрока
        private void UpdatePlayerName()
        {
            if (playerNameTextBox != null)
            {
                string newName = playerNameTextBox.Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(newName) && newName != playerName)
                {
                    playerName = newName;
                    AddSystemMessage($"Вы сменили имя на: {playerName}");
                }
            }
        }

        // Получили сообщение чата от сервера
        private void OnChatMessageReceived(Models.ChatMessage message)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (message.IsSystemMessage)
                {
                    AddSystemMessage(message.Text);
                }
                else if (message.UserId == userId)
                {
                    AddGameMessage(message.Text);
                }
                else
                {
                    AddOpponentMessage($"{message.SenderName}: {message.Text}");
                }
            });
        }

        // Подключение к серверу
        private async void ConnectButton_Click(object? sender, RoutedEventArgs e)
        {
            if (serverTextBox == null || playerNameTextBox == null)
                return;

            string server = serverTextBox.Text ?? "localhost";
            string name = playerNameTextBox.Text?.Trim() ?? "";

            // Проверяем имя
            if (string.IsNullOrWhiteSpace(name))
            {
                AddSystemMessage("Введите ваше имя!", true);
                return;
            }

            playerName = name;

            // Меняем состояние кнопок
            if (connectButton != null)
            {
                connectButton.IsEnabled = false;
                connectButton.Content = "Подключение...";
            }

            AddSystemMessage($"Подключаюсь к {server}:5000...");

            // Создаем сетевой сервис для WebSocket
            _networkService = new NetworkService(server, 5000);
            _networkService.ChatMessageReceived += OnChatMessageReceived;

            // Пытаемся подключиться
            bool connected = await _networkService.ConnectAsync(userId, playerName);

            if (connected)
            {
                isConnected = true;
                _useLocalChat = false;
                
                // Обновляем кнопки
                if (connectButton != null)
                {
                    connectButton.IsEnabled = false;
                    connectButton.Content = "Подключено";
                }
                if (disconnectButton != null)
                    disconnectButton.IsEnabled = true;
                if (sendButton != null)
                    sendButton.IsEnabled = true;
                if (messageTextBox != null)
                    messageTextBox.IsEnabled = true;
                if (statusIndicator != null)
                    statusIndicator.Background = new Avalonia.Media.SolidColorBrush(0xFF10B981);

                AddSystemMessage($"✅ Успешно подключены как: {playerName}");
            }
            else
            {
                // Ошибка подключения - используем локальный чат
                _useLocalChat = true;
                isConnected = true;
                
                if (connectButton != null)
                {
                    connectButton.IsEnabled = false;
                    connectButton.Content = "Локальный чат";
                }
                if (disconnectButton != null)
                    disconnectButton.IsEnabled = true;
                if (sendButton != null)
                    sendButton.IsEnabled = true;
                if (messageTextBox != null)
                    messageTextBox.IsEnabled = true;
                if (statusIndicator != null)
                    statusIndicator.Background = new Avalonia.Media.SolidColorBrush(0xFFFAA43A);

                AddSystemMessage($"⚠️ Не удалось подключиться к серверу. Используется локальный чат.");
            }
        }

        // Отключение от сервера
        private async void DisconnectButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_networkService != null && isConnected)
            {
                await _networkService.DisconnectAsync();
            }
            
            isConnected = false;
            _useLocalChat = false;
            
            if (connectButton != null)
            {
                connectButton.IsEnabled = true;
                connectButton.Content = "Подключиться";
            }
            if (disconnectButton != null)
                disconnectButton.IsEnabled = false;
            if (sendButton != null)
                sendButton.IsEnabled = false;
            if (messageTextBox != null)
                messageTextBox.IsEnabled = false;
            if (statusIndicator != null)
                statusIndicator.Background = new Avalonia.Media.SolidColorBrush(0xFFEF4444);
            
            AddSystemMessage("Отключены от сервера");
        }

        // Отправка сообщения по кнопке
        private void SendButton_Click(object? sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        // Отправка сообщения по Enter
        private void MessageTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        // Основной метод отправки
        private async void SendMessage()
        {
            if (messageTextBox == null) return;

            string messageText = messageTextBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(messageText))
            {
                // Мигание красной рамкой при пустом сообщении
                if (messageTextBox != null)
                {
                    var originalBorder = messageTextBox.BorderBrush;
                    messageTextBox.BorderBrush = new Avalonia.Media.SolidColorBrush(0xFFFF6B6B);

                    _ = Task.Delay(300).ContinueWith(t =>
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            messageTextBox.BorderBrush = originalBorder;
                        });
                    });
                }
                return;
            }

            // Если подключены к серверу
            if (!_useLocalChat && _networkService != null && _networkService.IsConnected)
            {
                bool sent = await _networkService.SendChatMessageAsync(messageText);
                if (sent)
                {
                    AddGameMessage(messageText);
                }
                else
                {
                    AddSystemMessage("Ошибка отправки сообщения", true);
                }
            }
            else
            {
                // Локальное сообщение
                AddGameMessage(messageText);
                if (!isConnected)
                    AddSystemMessage("Сообщение отправлено локально (не подключены)");
            }

            // Очищаем поле и фокусируемся
            messageTextBox.Text = "";
            messageTextBox.Focus();

            // Прокручиваем вниз
            if (messagesScrollViewer != null)
                messagesScrollViewer.ScrollToEnd();
        }

        // Очистка чата
        private void ClearButton_Click(object? sender, RoutedEventArgs e)
        {
            if (messagesPanel != null)
                messagesPanel.Children.Clear();

            AddSystemMessage("История чата очищена");
            // Восстанавливаем приветствие
            AddSystemMessage($"Вы вошли как: {playerName}");
        }

        // Добавить системное сообщение
        private void AddSystemMessage(string text, bool isError = false)
        {
            var message = new ChatMessage
            {
                Sender = isError ? "Система" : "Система",
                Text = text,
                Time = DateTime.Now,
                IsSystem = true,
                IsError = isError
            };

            AddMessageToUI(message);
        }

        // Добавить свое сообщение
        private void AddGameMessage(string text)
        {
            var message = new ChatMessage
            {
                Sender = playerName,
                Text = text,
                Time = DateTime.Now,
                IsMyMessage = true
            };

            AddMessageToUI(message);
        }

        // Добавить сообщение противника
        private void AddOpponentMessage(string text)
        {
            var message = new ChatMessage
            {
                Sender = "Противник",
                Text = text,
                Time = DateTime.Now,
                IsMyMessage = false
            };

            AddMessageToUI(message);
        }

        // Создать UI элемент для сообщения
        private void AddMessageToUI(ChatMessage message)
        {
            if (messagesPanel == null) return;

            // Создаем контейнер сообщения
            var messageBorder = new Border
            {
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 0, 6),
                HorizontalAlignment = message.IsMyMessage ?
                    Avalonia.Layout.HorizontalAlignment.Right : // Мои сообщения справа
                    Avalonia.Layout.HorizontalAlignment.Left,   // Чужие слева
                MaxWidth = 400,
                BorderThickness = new Thickness(1)
            };

            // Цвет фона в зависимости от типа сообщения
            if (message.IsError)
            {
                messageBorder.Background = new Avalonia.Media.SolidColorBrush(0xFFFEE2E2); // Красный
                messageBorder.BorderBrush = new Avalonia.Media.SolidColorBrush(0xFFFCA5A5);
            }
            else if (message.IsSystem)
            {
                messageBorder.Background = new Avalonia.Media.SolidColorBrush(0xFFF1F5F9); // Серый
                messageBorder.BorderBrush = new Avalonia.Media.SolidColorBrush(0xFFE2E8F0);
            }
            else if (message.IsMyMessage)
            {
                messageBorder.Background = new Avalonia.Media.SolidColorBrush(0xFFDBEAFE); // Синий
                messageBorder.BorderBrush = new Avalonia.Media.SolidColorBrush(0xFF93C5FD);
            }
            else
            {
                messageBorder.Background = new Avalonia.Media.SolidColorBrush(0xFFF0F9FF); // Голубой
                messageBorder.BorderBrush = new Avalonia.Media.SolidColorBrush(0xFFBAE6FD);
            }

            // Внутренний контейнер
            var stackPanel = new StackPanel();

            // Заголовок (имя + время)
            var headerPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8
            };

            var senderText = new TextBlock
            {
                Text = message.Sender,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                FontSize = 12
            };

            var timeText = new TextBlock
            {
                Text = message.Time.ToString("HH:mm:ss"),
                Foreground = new Avalonia.Media.SolidColorBrush(0xFF64748B),
                FontSize = 10,
                Margin = new Thickness(6, 0, 0, 0)
            };

            headerPanel.Children.Add(senderText);
            headerPanel.Children.Add(timeText);

            // Текст сообщения
            var messageText = new TextBlock
            {
                Text = message.Text,
                Margin = new Thickness(0, 6, 0, 0),
                FontSize = 13,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            // Собираем всё вместе
            stackPanel.Children.Add(headerPanel);
            stackPanel.Children.Add(messageText);
            messageBorder.Child = stackPanel;
            messagesPanel.Children.Add(messageBorder);

            // Прокручиваем к новому сообщению
            if (messagesScrollViewer != null)
                messagesScrollViewer.ScrollToEnd();
        }

        // При закрытии окна
        protected override async void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Отключаемся от сервера при закрытии окна
            if (_networkService != null && isConnected)
            {
                await _networkService.DisconnectAsync();
            }
        }

        // Класс для хранения информации о сообщении
        public class ChatMessage
        {
            public string Sender { get; set; } = "";
            public string Text { get; set; } = "";
            public DateTime Time { get; set; } = DateTime.Now;
            public bool IsSystem { get; set; } = false;
            public bool IsMyMessage { get; set; } = false;
            public bool IsError { get; set; } = false;
        }
    }
}

// Главное окно
using Avalonia.Controls;
using System;
using System.Threading.Tasks;

namespace BattleOfSea
{
    public partial class MainWindow : Window
    {
        private ContentControl? _mainContent;
        private Views.LobbyView? _lobbyView;
        private readonly Services.INetworkService _networkService;

        // Конструктор главного окна (публичный)
        public MainWindow()
        {
            InitializeComponent();

            // Инициализируем сетевой сервис - требует реального подключения к серверу (ws://localhost:5555)
            _networkService = new Services.NetworkService("localhost", 5555);

            // Кэшируем элементы управления и подписываемся на запросы присоединения к комнатам
            _mainContent = this.FindControl<ContentControl>("MainContent");
            _lobbyView = this.FindControl<Views.LobbyView>("LobbyViewControl");

            // Гарантируем, что LobbyView имеет DataContext с сетевым сервисом
            if (_lobbyView != null && _lobbyView.DataContext == null)
            {
                _lobbyView.DataContext = new ViewModels.LobbyViewModel(_networkService);
            }

            // Подписываемся на событие запроса присоединения к комнате
            if (_lobbyView?.DataContext is ViewModels.LobbyViewModel lvm)
            {
                lvm.JoinRequested += OnRoomJoinRequested;
            }

            // Подключаемся к серверу при загрузке окна
            this.Loaded += (s, e) => ConnectToServerAsync();
        }

        private async void ConnectToServerAsync()
        {
            try
            {
                string userId = $"player_{Guid.NewGuid().ToString().Substring(0, 8)}";
                string displayName = "Player";

                Console.WriteLine($"[MainWindow] Connecting to server with userId: {userId}");
                bool connected = await _networkService.ConnectAsync(userId, displayName);

                if (connected)
                {
                    Console.WriteLine($"[MainWindow] ✅ Connected to server!");
                }
                else
                {
                    Console.WriteLine($"[MainWindow] ❌ Failed to connect to server");
                    ShowConnectionWarning();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainWindow] ❌ Connection error: {ex.Message}");
                ShowConnectionWarning();
            }
        }

        // Показать предупреждение о подключении (приватный метод)
        private void ShowConnectionWarning()
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    var dialog = new Views.ConfirmDialog(
                        "Ошибка подключения",
                        "⚠️ Не удалось подключиться к серверу.\n\nКлиент будет работать в режиме демонстрации.\nВы сможете разместить корабли и просмотреть демонстрационные комнаты, но не сможете играть онлайн.\n\nУбедитесь, что сервер запущен на порту 5555.",
                        "Продолжить",
                        null
                    );
                    await dialog.ShowDialog(this);
                    Console.WriteLine("[MainWindow] User acknowledged connection warning");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MainWindow] Error showing warning dialog: {ex.Message}");
                }
            });
        }

        // Обработчик запроса присоединения к комнате (приватный метод)
        private void OnRoomJoinRequested(Models.Room? room)
        {
            if (room == null || _mainContent == null) return;

            // Создаем представление игры и модель представления с сетевым сервисом
            var gameView = new Views.GameView();
            var gvm = new ViewModels.GameViewModel(room, _networkService);
            gameView.DataContext = gvm;

            // Подписываемся на событие запроса выхода из игры
            gvm.ExitRequested += () =>
            {
                // Возвращаемся в лобби
                if (_mainContent != null && _lobbyView != null)
                {
                    _mainContent.Content = _lobbyView;
                }
            };

            // Устанавливаем представление игры как текущее содержимое
            _mainContent.Content = gameView;
        }
    }
}
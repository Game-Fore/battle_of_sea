// Главное окно
using Avalonia.Controls;

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

            // Инициализируем сетевой сервис
            _networkService = new Services.MockNetworkService();

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
using Avalonia.Controls;

namespace BattleOfSea
{
    public partial class MainWindow : Window
    {
        private ContentControl? _mainContent;
        private Views.LobbyView? _lobbyView;
        private readonly Services.INetworkService _networkService;

        public MainWindow()
        {
            InitializeComponent();

            // Инициализируем сетевой сервис (День 8-9: Сеть)
            _networkService = new Services.MockNetworkService();

            // Cache controls and subscribe to lobby join requests
            _mainContent = this.FindControl<ContentControl>("MainContent");
            _lobbyView = this.FindControl<Views.LobbyView>("LobbyViewControl");

            // Ensure LobbyView has DataContext с сетевым сервисом
            if (_lobbyView != null && _lobbyView.DataContext == null)
            {
                _lobbyView.DataContext = new ViewModels.LobbyViewModel(_networkService);
            }

            if (_lobbyView?.DataContext is ViewModels.LobbyViewModel lvm)
            {
                lvm.JoinRequested += OnRoomJoinRequested;
            }
        }

        private void OnRoomJoinRequested(Models.Room? room)
        {
            if (room == null || _mainContent == null) return;

            // Create game view and viewmodel с сетевым сервисом
            var gameView = new Views.GameView();
            var gvm = new ViewModels.GameViewModel(room, _networkService);
            gameView.DataContext = gvm;

            // Subscribe to exit request
            gvm.ExitRequested += () =>
            {
                // Return to lobby
                if (_mainContent != null && _lobbyView != null)
                {
                    _mainContent.Content = _lobbyView;
                }
            };

            _mainContent.Content = gameView;
        }
    }
}

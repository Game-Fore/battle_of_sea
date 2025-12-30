using Avalonia.Controls;

namespace BattleOfSea
{
    public partial class MainWindow : Window
    {
        private ContentControl? _mainContent;
        private Views.LobbyView? _lobbyView;

        public MainWindow()
        {
            InitializeComponent();

            // Cache controls and subscribe to lobby join requests
            _mainContent = this.FindControl<ContentControl>("MainContent");
            _lobbyView = this.FindControl<Views.LobbyView>("LobbyViewControl");

            if (_lobbyView?.DataContext is ViewModels.LobbyViewModel lvm)
            {
                lvm.JoinRequested += OnRoomJoinRequested;
            }
        }

        private void OnRoomJoinRequested(Models.Room? room)
        {
            if (room == null || _mainContent == null) return;

            // Create game view and viewmodel
            var gameView = new Views.GameView();
            var gvm = new ViewModels.GameViewModel(room);
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

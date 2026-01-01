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

                // Demo mode: auto-open the first available room shortly after startup so
                // a user can preview the Game UI without a live backend.
                if (App.DemoMode)
                {
                    var first = lvm.Rooms.Count > 0 ? lvm.Rooms[0] : null;
                    if (first != null)
                    {
                        // schedule with a short delay off the UI thread, then dispatch back
                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(250);
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => OnRoomJoinRequested(first));
                        });
                    }
                }
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

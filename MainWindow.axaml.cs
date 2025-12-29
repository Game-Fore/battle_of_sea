using Avalonia.Controls;

namespace BattleOfSea
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Subscribe to lobby join requests
            var lobby = this.FindControl<Views.LobbyView>("LobbyViewControl");
            if (lobby?.DataContext is ViewModels.LobbyViewModel lvm)
            {
                lvm.JoinRequested += OnRoomJoinRequested;
            }
        }

        private void OnRoomJoinRequested(Models.Room? room)
        {
            if (room == null) return;

            // Create game view and viewmodel
            var gameView = new Views.GameView();
            var gvm = new ViewModels.GameViewModel(room);
            gameView.DataContext = gvm;

            // Subscribe to exit request
            gvm.ExitRequested += () =>
            {
                // Return to lobby
                this.FindControl<ContentControl>("MainContent").Content = this.FindControl<Views.LobbyView>("LobbyViewControl");
            };

            this.FindControl<ContentControl>("MainContent").Content = gameView;
        }
    }
}

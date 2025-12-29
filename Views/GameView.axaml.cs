using Avalonia.Controls;

namespace BattleOfSea.Views
{
    public partial class GameView : UserControl
    {
        public GameView()
        {
            InitializeComponent();
            // DataContext will be set by the host (MainWindow) so we can pass a Room
        }

        private void ExitToLobby_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.GameViewModel gvm)
            {
                gvm.RequestExit();
            }
        }
    }
}

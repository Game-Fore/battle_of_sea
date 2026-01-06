using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BattleOfSea.Views
{
    public partial class GameView : UserControl
    {
        public GameView()
        {
            InitializeComponent();
            // DataContext will be set by the host (MainWindow) so we can pass a Room
        }

        // День 12: UX улучшения - подтверждение выхода
        private async void ExitToLobby_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.GameViewModel gvm)
            {
                // Показываем диалог подтверждения
                var dialog = new ConfirmDialog
                {
                    Message = "Вы уверены, что хотите выйти в лобби? Текущая игра будет завершена."
                };

                var parent = this.VisualRoot as Window;
                if (parent != null)
                {
                    var result = await dialog.ShowDialog<bool?>(parent);
                    if (result == true)
                    {
                        gvm.RequestExit();
                    }
                }
                else
                {
                    // Если не нашли родительское окно, просто выходим
                    gvm.RequestExit();
                }
            }
        }
    }
}

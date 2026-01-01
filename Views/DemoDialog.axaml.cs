using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia;

namespace BattleOfSea.Views
{
    public partial class DemoDialog : Window
    {
        public DemoDialog()
        {
            InitializeComponent();
            System.Console.WriteLine("DemoDialog created");
        }

        private void OpenLobby_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
            // Bring main window to front
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow?.Activate();
            }
        }

        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

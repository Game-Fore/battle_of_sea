using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia;

namespace BattleOfSea.Views
{
    public partial class DemoDialog : Window
    {
        // Конструктор демо-диалога (публичный)
        public DemoDialog()
        {
            InitializeComponent();
            System.Console.WriteLine("DemoDialog created");
        }

        // Обработчик клика "Открыть лобби" (приватный метод)
        private void OpenLobby_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
            // Выводим главное окно на передний план
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow?.Activate();
            }
        }

        // Обработчик клика "Закрыть" (приватный метод)
        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
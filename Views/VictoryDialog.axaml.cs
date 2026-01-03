using Avalonia.Controls;
using Avalonia.Media;

namespace BattleOfSea.Views
{
    public partial class VictoryDialog : Window
    {
        public bool IsVictory { get; }
        public string DialogTitle => IsVictory ? "Победа!" : "Поражение";
        public new string Icon => IsVictory ? "🎉" : "😢";
        public string Message => IsVictory ? "Вы победили!" : "Вы проиграли";
        public string SubMessage => IsVictory ? "Все корабли противника потоплены!" : "Все ваши корабли потоплены";
        public IBrush BackgroundColor => IsVictory 
            ? new SolidColorBrush(Color.FromRgb(34, 197, 94)) // Green
            : new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
        public IBrush TextColor => Brushes.White;

        public VictoryDialog(bool isVictory)
        {
            InitializeComponent();
            IsVictory = isVictory;
            Title = IsVictory ? "Победа!" : "Поражение";
            DataContext = this;
        }

        private void OK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
    }
}


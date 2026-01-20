// Логика победы
using Avalonia.Controls;
using Avalonia.Media;

namespace BattleOfSea.Views
{
    public partial class VictoryDialog : Window
    {
        // Флаг победы (публичное свойство)
        public bool IsVictory { get; }
        // Заголовок диалога (публичное свойство)
        public string DialogTitle => IsVictory ? "Победа!" : "Поражение";
        // Иконка диалога (публичное свойство)
        public new string Icon => IsVictory ? "🎉" : "😢";
        // Основное сообщение (публичное свойство)
        public string Message => IsVictory ? "Вы победили!" : "Вы проиграли";
        // Дополнительное сообщение (публичное свойство)
        public string SubMessage => IsVictory ? "Все корабли противника потоплены!" : "Все ваши корабли потоплены";
        // Цвет фона диалога (публичное свойство)
        public IBrush BackgroundColor => IsVictory 
            ? new SolidColorBrush(Color.FromRgb(34, 197, 94)) // Зеленый
            : new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Красный
        // Цвет текста (публичное свойство)
        public IBrush TextColor => Brushes.White;

        // Конструктор диалога победы/поражения (публичный)
        public VictoryDialog(bool isVictory)
        {
            InitializeComponent();
            IsVictory = isVictory;
            Title = IsVictory ? "Победа!" : "Поражение";
            DataContext = this;
        }

        // Обработчик клика "OK" (приватный метод)
        private void OK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }
    }
}
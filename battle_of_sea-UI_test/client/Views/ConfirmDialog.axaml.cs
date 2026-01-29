// Логика диалога
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BattleOfSea.Views
{
    public partial class ConfirmDialog : Window
    {
        // Результат диалога (публичное свойство)
        public bool Result { get; private set; } = false;

        // Сообщение диалога (публичное свойство)
        public string Message
        {
            get => (DataContext as ConfirmDialogViewModel)?.Message ?? "";
            set
            {
                if (DataContext == null)
                    DataContext = new ConfirmDialogViewModel { Message = value };
                else if (DataContext is ConfirmDialogViewModel vm)
                    vm.Message = value;
            }
        }

        // Конструктор диалога подтверждения (публичный)
        public ConfirmDialog()
        {
            InitializeComponent();
            DataContext = new ConfirmDialogViewModel();
        }

        // Конструктор диалога с параметрами (публичный)
        public ConfirmDialog(string title, string message, string yesText = "Да", string? noText = "Нет")
        {
            InitializeComponent();
            Title = title;
            var vm = new ConfirmDialogViewModel 
            { 
                Message = message,
                YesButtonText = yesText,
                NoButtonText = noText ?? ""
            };
            DataContext = vm;
        }

        // Обработчик клика "Да" (приватный метод)
        private void Yes_Click(object? sender, RoutedEventArgs e)
        {
            Result = true;
            Close(true);
        }

        // Обработчик клика "Нет" (приватный метод)
        private void No_Click(object? sender, RoutedEventArgs e)
        {
            Result = false;
            Close(false);
        }
    }

    // Модель представления диалога подтверждения
    public class ConfirmDialogViewModel
    {
        // Сообщение диалога (публичное свойство)
        public string Message { get; set; } = "Вы уверены?";
        // Текст кнопки "Да" (публичное свойство)
        public string YesButtonText { get; set; } = "Да";
        // Текст кнопки "Нет" (публичное свойство)
        public string NoButtonText { get; set; } = "Нет";
    }
}
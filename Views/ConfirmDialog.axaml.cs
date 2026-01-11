// Логика диалога
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BattleOfSea.Views
{
    public partial class ConfirmDialog : Window
    {
        public bool Result { get; private set; } = false;

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

        public ConfirmDialog()
        {
            InitializeComponent();
            DataContext = new ConfirmDialogViewModel();
        }

        private void Yes_Click(object? sender, RoutedEventArgs e)
        {
            Result = true;
            Close(true);
        }

        private void No_Click(object? sender, RoutedEventArgs e)
        {
            Result = false;
            Close(false);
        }
    }

    public class ConfirmDialogViewModel
    {
        public string Message { get; set; } = "Вы уверены?";
    }
}


using Avalonia.Controls;
using System;

namespace BattleOfSea.Views
{
    public partial class CreateRoomWindow : Window
    {
        public CreateRoomWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close(false);
        }

        private void Create_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var vm = DataContext as ViewModels.CreateRoomViewModel;
            if (vm == null)
            {
                Console.WriteLine("CreateRoom: DataContext not set");
                return;
            }

            if (string.IsNullOrWhiteSpace(vm.Name))
            {
                Console.WriteLine("CreateRoom: name is required");
                return;
            }

            Console.WriteLine($"Creating room: {vm.Name}, private={vm.IsPrivate}, max={vm.MaxPlayers}, type={vm.SelectedGameType}");
            // Return true to indicate creation
            this.Close(true);
        }
    }
}

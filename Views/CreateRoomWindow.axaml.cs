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
            this.Close((Models.Room?)null);
        }

        private void Create_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var vm = DataContext as ViewModels.CreateRoomViewModel;
            if (vm == null)
            {
                Console.WriteLine("CreateRoom: DataContext not set");
                this.Close((Models.Room?)null);
                return;
            }

            if (string.IsNullOrWhiteSpace(vm.Name))
            {
                Console.WriteLine("CreateRoom: name is required");
                this.Close((Models.Room?)null);
                return;
            }

            // Создаем комнату с 0 игроками изначально
            var room = new Models.Room(vm.Name, 0, vm.MaxPlayers, vm.IsPrivate, null);
            Console.WriteLine($"Creating room: {room.Name}, private={room.IsPrivate}, max={room.MaxPlayers}");
            this.Close(room);
        }
    }
}

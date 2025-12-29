using Avalonia.Controls;
using System;

namespace BattleOfSea.Views
{
    public partial class LobbyView : UserControl
    {
        public LobbyView()
        {
            InitializeComponent();
            DataContext = new ViewModels.LobbyViewModel();

            var lb = this.FindControl<ListBox>("RoomsList");
            if (lb != null)
            {
                lb.DoubleTapped += (s, e) =>
                {
                    if (lb.SelectedItem is Models.Room room)
                    {
                        Console.WriteLine($"Room double-clicked: {room.Name}");
                    }
                };

                lb.SelectionChanged += (s, e) =>
                {
                    if (lb.SelectedItem is Models.Room room)
                    {
                        Console.WriteLine($"Room clicked (selection): {room.Name}");
                    }
                };
            }
        }

        private async void CreateRoom_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var win = new CreateRoomWindow();
            var vm = new ViewModels.CreateRoomViewModel();
            win.DataContext = vm;

            var parent = this.VisualRoot as Avalonia.Controls.Window;
            var result = await win.ShowDialog<bool?>(parent);
            if (result == true)
            {
                // create room with 0 players initially
                var room = new Models.Room(vm.Name, 0, vm.MaxPlayers, vm.IsPrivate, vm.SelectedGameType);
                if (DataContext is ViewModels.LobbyViewModel lvm)
                {
                    lvm.AddRoom(room);
                }
                else
                {
                    Console.WriteLine($"CreateRoom: Lobby DataContext not found");
                }
            }
        }
    }
}

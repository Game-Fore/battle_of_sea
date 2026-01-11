// Логика лобби
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
                        if (DataContext is ViewModels.LobbyViewModel lvm)
                            lvm.JoinCommand.Execute(room);
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
            if (parent == null)
            {
                // If we can't find an owner window (rare), show modeless window as fallback
                win.Show();
                return;
            }

            var result = await win.ShowDialog<Models.Room?>(parent);
            if (result != null)
            {
                if (DataContext is ViewModels.LobbyViewModel lvm)
                {
                    // Комната уже создана в CreateRoomWindow, просто добавляем и присоединяемся
                    lvm.AddRoom(result);
                    // auto-join созданной комнаты - это вызовет JoinRequested и откроет игру
                    lvm.JoinCommand.Execute(result);
                }
                else
                {
                    Console.WriteLine($"CreateRoom: Lobby DataContext not found");
                }
            }
        }

        private void OpenChat_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Placeholder для открытия чата
            // Когда второй разработчик закончит чат, можно будет вызвать его здесь
            Console.WriteLine("Открытие чата из лобби...");
        }
    }
}

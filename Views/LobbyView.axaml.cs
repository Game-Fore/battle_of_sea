// Логика лобби
using Avalonia.Controls;
using System;

namespace BattleOfSea.Views
{
    public partial class LobbyView : UserControl
    {
        // Конструктор представления лобби (публичный)
        public LobbyView()
        {
            InitializeComponent();
            DataContext = new ViewModels.LobbyViewModel();

            var lb = this.FindControl<ListBox>("RoomsList");
            if (lb != null)
            {
                // Обработчик двойного клика по комнате
                lb.DoubleTapped += (s, e) =>
                {
                    if (lb.SelectedItem is Models.Room room)
                    {
                        Console.WriteLine($"Room double-clicked: {room.Name}");
                        if (DataContext is ViewModels.LobbyViewModel lvm)
                            lvm.JoinCommand.Execute(room);
                    }
                };

                // Обработчик изменения выбора комнаты
                lb.SelectionChanged += (s, e) =>
                {
                    if (lb.SelectedItem is Models.Room room)
                    {
                        Console.WriteLine($"Room clicked (selection): {room.Name}");
                    }
                };
            }
        }

        // Обработчик клика "Создать комнату" (приватный метод)
        private async void CreateRoom_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var win = new CreateRoomWindow();
            var vm = new ViewModels.CreateRoomViewModel();
            win.DataContext = vm;

            var parent = this.VisualRoot as Avalonia.Controls.Window;
            if (parent == null)
            {
                // Если не нашли родительское окно (редко), показываем немодальное окно как запасной вариант
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
                    // Автоматическое присоединение к созданной комнате - вызовет JoinRequested и откроет игру
                    lvm.JoinCommand.Execute(result);
                }
                else
                {
                    Console.WriteLine($"CreateRoom: Lobby DataContext not found");
                }
            }
        }
    }
}
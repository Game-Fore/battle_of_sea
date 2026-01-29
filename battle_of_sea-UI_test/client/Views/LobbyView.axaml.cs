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
            // DataContext устанавливается MainWindow после создания LobbyView

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
        private void CreateRoom_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.LobbyViewModel lvm)
            {
                // Вызываем CreateRoomCommand который требует сервера
                lvm.CreateRoomCommand.Execute(null);
            }
            else
            {
                Console.WriteLine($"CreateRoom: Lobby DataContext not found");
            }
        }
    }
}
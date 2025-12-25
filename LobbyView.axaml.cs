using Avalonia.Controls;
using System;

namespace BattleOfSea
{
    public partial class LobbyView : UserControl
    {
        public LobbyView()
        {
            InitializeComponent();
            DataContext = new LobbyViewModel();

            // Optional: handle double-click on a room
            var lb = this.FindControl<ListBox>("RoomsList");
            lb.DoubleTapped += (s, e) =>
            {
                if (lb.SelectedItem is Room room)
                {
                    Console.WriteLine($"Room clicked: {room.Name}");
                }
            };
        }
    }
}

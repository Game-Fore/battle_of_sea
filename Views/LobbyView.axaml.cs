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
}

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace BattleOfSea.ViewModels
{
    public class LobbyViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Models.Room> Rooms { get; } = new ObservableCollection<Models.Room>();

        private Models.Room? _selectedRoom;
        public Models.Room? SelectedRoom
        {
            get => _selectedRoom;
            set { _selectedRoom = value; OnPropertyChanged(); }
        }

        public ICommand JoinCommand { get; }
        public ICommand CreateRoomCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand QuickStartCommand { get; }
        public ICommand ExitCommand { get; }

        public LobbyViewModel()
        {
            // Static placeholder rooms (only free rooms are added to the visible list)
            var all = new[]
            {
                new Models.Room("Alpha", 1, 2),
                new Models.Room("Bravo", 0, 2),
                new Models.Room("Charlie", 2, 4),
                new Models.Room("Delta", 1, 4),
                new Models.Room("Echo (full)", 4, 4)
            };

            foreach (var r in all)
            {
                if (r.Players < r.MaxPlayers)
                    Rooms.Add(r);
            }

            JoinCommand = new Utils.RelayCommand(o => {
                var room = o as Models.Room ?? SelectedRoom;
                JoinRoom(room);
            });

            CreateRoomCommand = new Utils.RelayCommand(_ => Console.WriteLine("Create room clicked (default settings)"));
            RefreshCommand = new Utils.RelayCommand(_ => { Console.WriteLine("Refresh clicked"); });
            QuickStartCommand = new Utils.RelayCommand(_ => Console.WriteLine("Quick Start clicked"));
            ExitCommand = new Utils.RelayCommand(_ => Console.WriteLine("Exit clicked"));
        }

        private void JoinRoom(Models.Room? room)
        {
            if (room != null)
                Console.WriteLine($"Join requested: {room.Name}");
            else
                Console.WriteLine("Join requested: (no room selected)");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

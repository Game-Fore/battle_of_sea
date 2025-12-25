using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace BattleOfSea
{
    public class LobbyViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Room> Rooms { get; } = new ObservableCollection<Room>();

        private Room? _selectedRoom;
        public Room? SelectedRoom
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
            // Static placeholder rooms
            Rooms.Add(new Room("Alpha", 1, 2));
            Rooms.Add(new Room("Bravo", 0, 2));
            Rooms.Add(new Room("Charlie", 2, 4));
            Rooms.Add(new Room("Delta", 1, 4));

            JoinCommand = new RelayCommand(o => {
                var room = o as Room ?? SelectedRoom;
                if (room != null)
                    Console.WriteLine($"Join requested: {room.Name}");
                else
                    Console.WriteLine("Join requested: (no room selected)");
            });

            CreateRoomCommand = new RelayCommand(_ => Console.WriteLine("Create room clicked"));
            RefreshCommand = new RelayCommand(_ => Console.WriteLine("Refresh clicked"));
            QuickStartCommand = new RelayCommand(_ => Console.WriteLine("Quick Start clicked"));
            ExitCommand = new RelayCommand(_ => Console.WriteLine("Exit clicked"));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

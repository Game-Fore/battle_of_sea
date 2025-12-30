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
        private readonly Services.INetworkService _networkService;

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

        public LobbyViewModel() : this(new Services.MockNetworkService()) { }

        public LobbyViewModel(Services.INetworkService networkService)
        {
            _networkService = networkService;

            // Static placeholder rooms (only free rooms are added to the visible list)
            var all = new[]
            {//
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

            JoinCommand = new Utils.RelayCommand(async o => {
                var room = o as Models.Room ?? SelectedRoom;
                await JoinRoomAsync(room);
            });

            CreateRoomCommand = new Utils.RelayCommand(_ => Console.WriteLine("Create room clicked (default settings)"));
            RefreshCommand = new Utils.RelayCommand(_ => { Console.WriteLine("Refresh clicked"); });
            QuickStartCommand = new Utils.RelayCommand(_ => Console.WriteLine("Quick Start clicked"));
            ExitCommand = new Utils.RelayCommand(_ => Console.WriteLine("Exit clicked"));
        }

        public event Action<Models.Room?>? JoinRequested;

        public void AddRoom(Models.Room room)
        {
            if (room != null && room.Players < room.MaxPlayers)
            {
                Rooms.Add(room);
                Console.WriteLine($"Room created: {room.Name} ({room.Players}/{room.MaxPlayers})");
            }
            else
            {
                Console.WriteLine($"Room not added (full or invalid): {room?.Name}");
            }
        }

        private async System.Threading.Tasks.Task JoinRoomAsync(Models.Room? room)
        {
            if (room == null) return;

            Console.WriteLine($"Join requested: {room.Name}");
            try
            {
                var ok = await _networkService.JoinRoomAsync(room);
                if (ok)
                {
                    Console.WriteLine($"Joined room (mock): {room.Name}");
                    JoinRequested?.Invoke(room);
                }
                else
                {
                    Console.WriteLine($"Failed to join room: {room.Name}");
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Join error: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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

            // Подписываемся на обновления списка комнат (День 10: Лобби онлайн)
            _networkService.RoomsListUpdated += OnRoomsListUpdated;
            _networkService.JoinRoomResult += OnJoinRoomResult;

            // Загружаем комнаты с сервера
            _ = LoadRoomsAsync();

            JoinCommand = new Utils.RelayCommand(async o => {
                var room = o as Models.Room ?? SelectedRoom;
                await JoinRoomAsync(room);
            });

            CreateRoomCommand = new Utils.RelayCommand(async _ => await CreateRoomAsync());
            RefreshCommand = new Utils.RelayCommand(async _ => await LoadRoomsAsync());
            QuickStartCommand = new Utils.RelayCommand(_ => Console.WriteLine("Quick Start clicked"));
            ExitCommand = new Utils.RelayCommand(_ => Console.WriteLine("Exit clicked"));
        }

        // День 10: Загрузка комнат с сервера
        private async System.Threading.Tasks.Task LoadRoomsAsync()
        {
            try
            {
                var rooms = await _networkService.GetRoomsAsync();
                Rooms.Clear();
                foreach (var room in rooms)
                {
                    if (room.Players < room.MaxPlayers)
                    {
                        Rooms.Add(room);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error loading rooms: {ex.Message}");
            }
        }

        private void OnRoomsListUpdated(Models.RoomsListMessage message)
        {
            Rooms.Clear();
            foreach (var room in message.Rooms)
            {
                if (room.Players < room.MaxPlayers)
                {
                    Rooms.Add(room);
                }
            }
        }

        private async System.Threading.Tasks.Task CreateRoomAsync()
        {
            // Открываем окно создания комнаты
            var createWindow = new Views.CreateRoomWindow();
            var parent = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null;
            
            if (parent != null)
            {
                var result = await createWindow.ShowDialog<Models.Room?>(parent);
                if (result != null)
                {
                    var success = await _networkService.CreateRoomAsync(result);
                    if (success)
                    {
                        AddRoom(result);
                    }
                }
            }
        }

        private void OnJoinRoomResult(Models.JoinRoomMessage message)
        {
            if (message.Success)
            {
                JoinRequested?.Invoke(Rooms.FirstOrDefault(r => r.Name == message.RoomId));
            }
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

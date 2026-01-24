using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace BattleOfSea.ViewModels
{
    // Модель представления лобби игры
    public class LobbyViewModel : INotifyPropertyChanged
    {
        // Список доступных комнат (публичное свойство)
        public ObservableCollection<Models.Room> Rooms { get; } = new ObservableCollection<Models.Room>();
        private readonly Services.INetworkService _networkService;
        private bool _demoMode = false; // Флаг режима демонстрации

        // Флаг подключения к серверу (публичное свойство)
        public bool IsConnected => _networkService.IsConnected;

        // Флаг режима демонстрации (публичное свойство)
        public bool IsDemoMode => _demoMode;

        private string? _errorMessage;
        // Сообщение об ошибке (публичное свойство)
        public string? ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        private Models.Room? _selectedRoom;
        // Выбранная комната (публичное свойство)
        public Models.Room? SelectedRoom
        {
            get => _selectedRoom;
            set { _selectedRoom = value; OnPropertyChanged(); }
        }

        // Команды управления лобби (публичные свойства)
        public ICommand JoinCommand { get; }
        public ICommand CreateRoomCommand { get; }
        public ICommand RefreshCommand { get; }

        // Конструктор с использованием сетевого сервиса (публичный)
        // ПРИМЕЧАНИЕ: Требует реального подключения к серверу
        // public LobbyViewModel() : this(new Services.MockNetworkService()) { }

        // Конструктор с сетевым сервисом (публичный)
        public LobbyViewModel(Services.INetworkService networkService)
        {
            _networkService = networkService;

            // Подписываемся на обновления списка комнат
            _networkService.RoomsListUpdated += OnRoomsListUpdated;
            _networkService.JoinRoomResult += OnJoinRoomResult;

            // Загружаем комнаты с сервера
            _ = LoadRoomsAsync();
            
            // Периодически обновляем список комнат каждые 3 секунды
            StartPeriodicRoomsUpdate();

            // Инициализация команд
            JoinCommand = new Utils.RelayCommand(async o => {
                var room = o as Models.Room ?? SelectedRoom;
                await JoinRoomAsync(room);
            });

            CreateRoomCommand = new Utils.RelayCommand(async _ => await CreateRoomAsync());
            RefreshCommand = new Utils.RelayCommand(async _ => await LoadRoomsAsync());
        }

        // Загрузка комнат с сервера (приватный метод)
        private async System.Threading.Tasks.Task LoadRoomsAsync()
        {
            try
            {
                ErrorMessage = null;
                Console.WriteLine("[LoadRoomsAsync] Loading rooms...");
                
                // Проверяем подключение к серверу
                if (!_networkService.IsConnected)
                {
                    _demoMode = true;
                    LoadDemoRooms();
                    ErrorMessage = "⚠️ Сервер недоступен. Показаны демонстрационные комнаты.";
                    Console.WriteLine("[Lobby] Server is not available, loading demo rooms");
                    return;
                }

                _demoMode = false;
                var rooms = await _networkService.GetRoomsAsync();
                Console.WriteLine($"[LoadRoomsAsync] Got {rooms.Count} rooms from server");
                Rooms.Clear();
                foreach (var room in rooms)
                {
                    Rooms.Add(room);
                    Console.WriteLine($"[LoadRoomsAsync] Added room: {room.Name} ({room.Players}/{room.MaxPlayers})");
                }
            }
            catch (System.Exception ex)
            {
                // При ошибке загружаем демонстрационные комнаты
                _demoMode = true;
                LoadDemoRooms();
                ErrorMessage = $"⚠️ Ошибка подключения: {ex.Message}. Используется демо-режим.";
                Console.WriteLine($"[Lobby] Error loading rooms: {ex.Message}");
            }
        }

        // Загрузка демонстрационных комнат (приватный метод)
        private void LoadDemoRooms()
        {
            Rooms.Clear();
            Rooms.Add(new Models.Room("Демо-комната 1", 1, 2));
            Rooms.Add(new Models.Room("Демо-комната 2", 2, 2));
            Console.WriteLine("[Lobby] Demo rooms loaded");
        }

        // Обработчик обновления списка комнат (приватный метод)
        private void OnRoomsListUpdated(Models.RoomsListMessage message)
        {
            Console.WriteLine($"[Lobby] OnRoomsListUpdated: received {message.Rooms.Count} rooms");
            // Server is available if we receive a real rooms list — disable demo mode
            _demoMode = false;
            Rooms.Clear();
            foreach (var room in message.Rooms)
            {
                // Показываем все комнаты, даже если они полные
                Rooms.Add(room);
                Console.WriteLine($"[Lobby] Added room: {room.Name} ({room.Players}/{room.MaxPlayers})");
            }
            ErrorMessage = null;
        }
        
        // Периодическое обновление списка комнат (приватный метод)
        private void StartPeriodicRoomsUpdate()
        {
            var timer = new System.Timers.Timer(3000);
            timer.Elapsed += async (s, e) =>
            {
                if (_networkService.IsConnected && !_demoMode)
                {
                    await LoadRoomsAsync();
                }
            };
            timer.AutoReset = true;
            timer.Start();
        }

        // Создание комнаты (приватный метод)
        private async System.Threading.Tasks.Task CreateRoomAsync()
        {
            try
            {
                if (!_networkService.IsConnected)
                {
                    ErrorMessage = "❌ Сервер недоступен. Нельзя создать новую комнату в режиме демонстрации.";
                    Console.WriteLine("[CreateRoom] Cannot create room in demo mode");
                    return;
                }

                ErrorMessage = null;
                Console.WriteLine("[CreateRoom] Opening create room dialog...");
                // Открываем окно создания комнаты
                var createWindow = new Views.CreateRoomWindow();
                var createVm = new CreateRoomViewModel();
                createWindow.DataContext = createVm;
                
                var parent = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow : null;
                
                if (parent != null)
                {
                    var result = await createWindow.ShowDialog<Models.Room?>(parent);
                    if (result != null)
                    {
                        Console.WriteLine($"[CreateRoom] Creating room: {result.Name}");
                        var success = await _networkService.CreateRoomAsync(result);
                        if (success)
                        {
                            Console.WriteLine($"[CreateRoom] ✅ Room created successfully");
                            // Сервер отправит обновленный список комнат через RoomsListUpdated событие
                            // Автоматически присоединяемся к созданной комнате
                            await JoinRoomAsync(result);
                        }
                        else
                        {
                            Console.WriteLine($"[CreateRoom] ❌ Failed to create room");
                            ErrorMessage = "Не удалось создать комнату";
                        }
                    }
                    else
                    {
                        Console.WriteLine("[CreateRoom] Dialog canceled");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Ошибка создания комнаты: {ex.Message}";
                Console.WriteLine($"Create room error: {ex.Message}");
            }
        }

        // Обработчик результата присоединения к комнате (приватный метод)
        private void OnJoinRoomResult(Models.JoinRoomMessage message)
        {
            if (message.Success)
            {
                Console.WriteLine($"[DEBUG] OnJoinRoomResult: roomId from server = '{message.RoomId}'");
                // Ищем комнату в списке по ID, если не найдена - создаем объект с ID
                var room = Rooms.FirstOrDefault(r => r.Id == message.RoomId);
                if (room == null)
                {
                    Console.WriteLine($"[DEBUG] Room not found by RoomId, searching by Name...");
                    room = Rooms.FirstOrDefault(r => r.Name == message.RoomId);
                }
                if (room == null)
                {
                    Console.WriteLine($"[DEBUG] Room still not found! Creating new Room with Id={message.RoomId}");
                    room = new Models.Room(message.RoomId, 1, 2);
                    room.Id = message.RoomId; // CRITICAL: Set the Id
                }
                Console.WriteLine($"[DEBUG] Invoking JoinRequested with room: Name={room.Name}, Id={room.Id}");
                JoinRequested?.Invoke(room);
            }
        }

        // Событие запроса присоединения к комнате (публичное событие)
        public event Action<Models.Room?>? JoinRequested;

        // Присоединение к комнате (приватный метод)
        private async System.Threading.Tasks.Task JoinRoomAsync(Models.Room? room)
        {
            if (room == null) return;

            try
            {
                ErrorMessage = null;
                
                // Проверяем, не полная ли комната
                if (room.Players >= room.MaxPlayers)
                {
                    ErrorMessage = $"❌ Комната '{room.Name}' уже полная!";
                    Console.WriteLine($"[Lobby] Room {room.Name} is full");
                    return;
                }
                
                // В режиме демонстрации просто переходим в комнату
                if (_demoMode)
                {
                    Console.WriteLine($"[Lobby] Demo mode: joining room {room.Name}");
                    JoinRequested?.Invoke(room);
                    return;
                }

                Console.WriteLine($"Join requested: {room.Name}");
                var ok = await _networkService.JoinRoomAsync(room);
                if (ok)
                {
                    Console.WriteLine($"Joined room: {room.Name}");
                    // Небольшая задержка для гарантии срабатывания события после завершения асинхронной операции
                    await System.Threading.Tasks.Task.Delay(10);
                    // Всегда вызываем событие с комнатой, которая была передана
                }
                else
                {
                    ErrorMessage = $"Не удалось присоединиться к комнате {room.Name}";
                    Console.WriteLine($"Failed to join room: {room.Name}");
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Ошибка присоединения: {ex.Message}";
                Console.WriteLine($"Join error: {ex.Message}");
            }
        }

        // Событие изменения свойства (публичное событие)
        public event PropertyChangedEventHandler? PropertyChanged;
        // Уведомление об изменении свойства (приватный метод)
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
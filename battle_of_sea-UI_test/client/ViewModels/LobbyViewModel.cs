using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BattleOfSea.Models;
using BattleOfSea.Services;

namespace BattleOfSea.ViewModels
{
    // Модель представления лобби игры
    public class LobbyViewModel : INotifyPropertyChanged
    {
        // Список доступных комнат (публичное свойство)
        public ObservableCollection<Room> Rooms { get; } = new ObservableCollection<Room>();
        private readonly INetworkService _networkService;
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
        public Room? SelectedRoom
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
        public LobbyViewModel(INetworkService networkService)
        {
            _networkService = networkService;

            // Подписываемся на обновления списка комнат
            _networkService.RoomsListUpdated += OnRoomsListUpdated;
            _networkService.RoomCreated += OnRoomCreated;
            _networkService.JoinRoomResult += OnJoinRoomResult;

            // Загружаем комнаты с сервера
            _ = LoadRoomsAsync();
            
            // Периодически обновляем список комнат каждые 3 секунды
            StartPeriodicRoomsUpdate();

            // Инициализация команд
            JoinCommand = new Utils.RelayCommand(async o => {
                var room = o as Room ?? SelectedRoom;
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
            Rooms.Add(new Room("Демо-комната 1", 1, 2));
            Rooms.Add(new Room("Демо-комната 2", 2, 2));
            Console.WriteLine("[Lobby] Demo rooms loaded");
        }

        // Обработчик обновления списка комнат (приватный метод)
        private void OnRoomsListUpdated(RoomsListMessage message)
        {
            Console.WriteLine($"[Lobby] OnRoomsListUpdated: received {message.Rooms.Count} rooms");
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
                    var result = await createWindow.ShowDialog<Room?>(parent);
                    if (result != null)
                    {
                        Console.WriteLine($"[CreateRoom] 📌 Dialog returned: {result.Name}");
                        
                        // Подписываемся на событие создания комнаты чтобы получить правильный ID
                        Room? createdRoom = null;
                        var handler = new Action<Room>(room =>
                        {
                            Console.WriteLine($"[CreateRoom] 🎯 EVENT HANDLER INVOKED! Received RoomCreated event");
                            createdRoom = room;
                            Console.WriteLine($"[CreateRoom] 🎯 Event handler: Set createdRoom to ID: {room.Id} Name: {room.Name}");
                        });
                        
                        Console.WriteLine($"[CreateRoom] 📌 Subscribing to RoomCreated event");
                        _networkService.RoomCreated += handler;
                        Console.WriteLine($"[CreateRoom] 📌 Subscription complete. Calling CreateRoomAsync");
                        
                        try
                        {
                            var success = await _networkService.CreateRoomAsync(result);
                            Console.WriteLine($"[CreateRoom] 📌 CreateRoomAsync returned: {success}");
                            
                            // Даем время на получение события
                            Console.WriteLine($"[CreateRoom] 📌 Waiting for RoomCreated event (timeout 500ms)...");
                            for (int i = 0; i < 50 && createdRoom == null; i++)
                            {
                                await System.Threading.Tasks.Task.Delay(10);
                                if (i % 10 == 0)
                                    Console.WriteLine($"[CreateRoom] 📌 Still waiting... ({i*10}ms)");
                            }
                            
                            if (createdRoom != null)
                                Console.WriteLine($"[CreateRoom] 📌 Event received! Room ID: {createdRoom.Id}");
                            else
                                Console.WriteLine($"[CreateRoom] ⏱️ TIMEOUT: No event received after 500ms");
                            
                            if (success && createdRoom != null)
                            {
                                Console.WriteLine($"[CreateRoom] ✅ Room created successfully with ID: {createdRoom.Id}");
                                // Присоединяемся с правильным ID
                                await JoinRoomAsync(createdRoom);
                            }
                            else
                            {
                                Console.WriteLine($"[CreateRoom] ❌ Failed to create room or get room ID");
                                ErrorMessage = "Не удалось создать комнату";
                            }
                        }
                        finally
                        {
                            _networkService.RoomCreated -= handler;
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

        // Обработчик создания комнаты (приватный метод)
        private void OnRoomCreated(Room room)
        {
            Console.WriteLine($"[OnRoomCreated] Room created: {room.Name} (ID: {room.Id})");
            // Сохраняем созданную комнату для последующего присоединения
            _lastCreatedRoom = room;
        }

        // Последняя созданная комната (приватное поле)
        private Room? _lastCreatedRoom;

        // Обработчик результата присоединения к комнате (приватный метод)
        private void OnJoinRoomResult(JoinRoomMessage message)
        {
            if (message.Success)
            {
                // Ищем комнату в списке, если не найдена - создаем минимальный объект
                var room = Rooms.FirstOrDefault(r => r.Name == message.RoomId) 
                    ?? new Room(message.RoomId, 1, 2);
                JoinRequested?.Invoke(room);
            }
        }

        // Событие запроса присоединения к комнате (публичное событие)
        public event Action<Room?>? JoinRequested;

        // Присоединение к комнате (приватный метод)
        private async System.Threading.Tasks.Task JoinRoomAsync(Room? room)
        {
            if (room == null) return;

            try
            {
                ErrorMessage = null;
                
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
                    JoinRequested?.Invoke(room);
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
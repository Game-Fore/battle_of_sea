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

        // Флаг подключения к серверу (публичное свойство)
        public bool IsConnected => _networkService.IsConnected;

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
                ErrorMessage = $"Ошибка загрузки комнат: {ex.Message}";
                Console.WriteLine($"Error loading rooms: {ex.Message}");
            }
        }

        // Обработчик обновления списка комнат (приватный метод)
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

        // Создание комнаты (приватный метод)
        private async System.Threading.Tasks.Task CreateRoomAsync()
        {
            try
            {
                ErrorMessage = null;
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
                        var success = await _networkService.CreateRoomAsync(result);
                        if (success)
                        {
                            // Сервер отправит обновленный список комнат через RoomsListUpdated событие
                            // Автоматически присоединяемся к созданной комнате
                            await JoinRoomAsync(result);
                        }
                        else
                        {
                            ErrorMessage = "Не удалось создать комнату";
                        }
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
                // Ищем комнату в списке, если не найдена - создаем минимальный объект
                var room = Rooms.FirstOrDefault(r => r.Name == message.RoomId) 
                    ?? new Models.Room(message.RoomId, 1, 2);
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
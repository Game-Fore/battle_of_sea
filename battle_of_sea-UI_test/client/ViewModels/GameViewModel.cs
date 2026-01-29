// Управление игрой
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media;
using BattleOfSea.Models;
using BattleOfSea;

namespace BattleOfSea.ViewModels
{
    // Управляет игрой
    public class GameViewModel : INotifyPropertyChanged
    {
        private GameState _state = GameState.WaitingForOpponent;
        // Статус игры (публичное свойство)
        public GameState State
        {
            get => _state;
            set
            {
                // Предотвращение перезаписи конечных состояний
                if ((_state == GameState.YouWin || _state == GameState.YouLose) && value != _state)
                {
                    // Разрешаем переходы только между конечными состояниями
                    if (value != GameState.YouWin && value != GameState.YouLose)
                    {
                        return;
                    }
                }
                _state = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); 
                OnPropertyChanged(nameof(StatusBackground)); OnPropertyChanged(nameof(StatusForeground));
            }
        }

        // Текст статуса игры (публичное свойство)
        public string StatusText => State switch
        {
            GameState.WaitingForOpponent => "Ожидание соперника...",
            GameState.YourTurn => "Ваш ход",
            GameState.OpponentTurn => "Ход соперника",
            GameState.YouWin => "🎉 Вы победили! 🎉",
            GameState.YouLose => "😢 Вы проиграли",
            GameState.OpponentSurrender => "Соперник сдался.",
            GameState.Hit => "💥 Попал!",
            GameState.Miss => "💧 Промах",
            GameState.Sunk => "💥 Корабль потоплен!",
            _ => string.Empty
        };

        // Фон статуса игры (публичное свойство)
        public IBrush StatusBackground => State switch
        {
            GameState.YouWin => new SolidColorBrush(Color.FromRgb(34, 197, 94)), // Зеленый
            GameState.YouLose => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // Красный
            GameState.Hit => new SolidColorBrush(Color.FromRgb(251, 191, 36)), // Желтый
            GameState.Sunk => new SolidColorBrush(Color.FromRgb(220, 38, 38)), // Темно-красный
            GameState.Miss => new SolidColorBrush(Color.FromRgb(147, 197, 253)), // Светло-синий
            GameState.YourTurn => new SolidColorBrush(Color.FromRgb(34, 197, 94)), // Зеленый
            GameState.OpponentTurn => new SolidColorBrush(Color.FromRgb(156, 163, 175)), // Серый
            _ => new SolidColorBrush(Color.FromRgb(229, 231, 235)) // Светло-серый
        };

        // Цвет текста статуса (публичное свойство)
        public IBrush StatusForeground => (State == GameState.YouWin || State == GameState.YouLose || 
                                           State == GameState.Hit || State == GameState.Sunk || 
                                           State == GameState.YourTurn) 
            ? Brushes.White : Brushes.Black;

        // Название комнаты (публичное свойство)
        public string? RoomName { get; }
        // Собственное поле игрока (публичное свойство)
        public Models.Board Own { get; } = new Models.Board(10);
        // Поле противника (публичное свойство)
        public Models.Board Enemy { get; } = new Models.Board(10);
        // Менеджер размещения кораблей (публичное свойство)
        public ShipPlacementManager ShipManager { get; } = new ShipPlacementManager();

        // Подсчет оставшихся кораблей для отображения (публичные свойства)
        public int OwnRemainingShips => Own.RemainingShipsCount();
        public int EnemyRemainingShips => Enemy.RemainingShipsCount();

        private bool _shipsPlaced = false;
        // Флаг размещения кораблей (публичное свойство)
        public bool ShipsPlaced
        {
            get => _shipsPlaced;
            set { _shipsPlaced = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowPlacement)); OnPropertyChanged(nameof(ShowGame)); }
        }

        // Показать экран размещения кораблей (публичное свойство)
        public bool ShowPlacement => !_shipsPlaced;
        // Показать экран игры (публичное свойство)
        public bool ShowGame => _shipsPlaced;

        // Событие запроса выхода из игры (публичное событие)
        public event Action? ExitRequested;

        // Команды управления игрой (публичные свойства)
        public ICommand? ShootCommand { get; private set; }
        public ICommand StartGameCommand { get; private set; }
        public ICommand SelectShipCommand { get; private set; }
        public ICommand RotateShipCommand { get; private set; }
        public ICommand RemoveShipCommand { get; private set; }

        private int? _selectedShipSize;
        // Выбранный размер корабля (публичное свойство)
        public int? SelectedShipSize
        {
            get => _selectedShipSize;
            set { _selectedShipSize = value; OnPropertyChanged(); }
        }

        private bool _isHorizontal = true;
        // Горизонтальная ориентация корабля (публичное свойство)
        public bool IsHorizontal
        {
            get => _isHorizontal;
            set { _isHorizontal = value; OnPropertyChanged(); }
        }

        private readonly Services.INetworkService? _networkService;
        private string? _roomId;
        private bool _isDemoMode = false; // Флаг режима демонстрации

        // Конструктор модели представления игры (публичный)
        public GameViewModel(Models.Room? room = null, Services.INetworkService? networkService = null)
        {
            _networkService = networkService;
            RoomName = room?.Name;
            _roomId = room?.Id;

            // Проверяем доступность сервера
            if (_networkService != null && !_networkService.IsConnected)
            {
                _isDemoMode = true;
                System.Diagnostics.Debug.WriteLine("[GameVM] Demo mode enabled - server is not available");
            }

            // Подписываемся на сетевые события
            if (_networkService != null)
            {
                _networkService.ShootResultReceived += OnShootResultReceived;
                _networkService.OpponentShootReceived += OnOpponentShootReceived;
                _networkService.GameStateChanged += OnGameStateChanged;                _networkService.GameStarted += OnGameStarted;            }
            
            // В демо-режиме тоже нужно размещать корабли, поэтому начинаем с ожидания
            if (RoomName != null)
            {
                State = GameState.WaitingForOpponent;
            }

            // В демо-режиме тоже нужно размещать корабли вручную
            ShipsPlaced = false;

            // Инициализация команд
            ShootCommand = new Utils.RelayCommand(o => {
                if (o is Models.BoardCell cell) _ = ShootAt(cell);
            });

            StartGameCommand = new Utils.RelayCommand(_ =>
            {
                if (ShipManager.AllShipsPlaced)
                {
                    ShipsPlaced = true;
                    
                    // Отправляем расстановку кораблей на сервер
                    _ = SendShipPlacementAsync();
                    
                    // Состояние изменится через OnGameStateChanged от сервера
                }
            });

            SelectShipCommand = new Utils.RelayCommand(o =>
            {
                if (int.TryParse(o?.ToString(), out int size))
                {
                    SelectedShipSize = size;
                }
            });

            RotateShipCommand = new Utils.RelayCommand(_ =>
            {
                IsHorizontal = !IsHorizontal;
            });

            RemoveShipCommand = new Utils.RelayCommand(o =>
            {
                if (o is Models.BoardCell cell)
                {
                    RemoveShip(cell.Row, cell.Col);
                }
            });
        }

        // Поставить корабль (публичный метод)
        public bool PlaceShip(int r, int c, int size, bool horizontal)
        {
            if (!ShipManager.CanPlaceShip(size)) return false;
            if (Own.PlaceShip(r, c, size, horizontal))
            {
                ShipManager.PlaceShip(size);
                // Уведомляем об изменении ShipManager для обновления UI счетчиков
                OnPropertyChanged(nameof(ShipManager));
                OnPropertyChanged(nameof(StartGameCommand));
                return true;
            }
            return false;
        }

        // Разместить корабль на ячейке (публичный метод)
        public void PlaceShipAtCell(Models.BoardCell cell)
        {
            if (SelectedShipSize.HasValue)
            {
                PlaceShip(cell.Row, cell.Col, SelectedShipSize.Value, IsHorizontal);
            }
        }

        // Удалить корабль (публичный метод)
        public void RemoveShip(int r, int c)
        {
            var ships = Own.GetShips();
            foreach (var ship in ships)
            {
                var cell = ship.FirstOrDefault(ce => ce.Row == r && ce.Col == c);
                if (cell != null)
                {
                    Own.RemoveShip(r, c);
                    ShipManager.RemoveShip(ship.Count);
                    // Уведомляем об изменении ShipManager для обновления UI счетчиков
                    OnPropertyChanged(nameof(ShipManager));
                    OnPropertyChanged(nameof(StartGameCommand));
                    break;
                }
            }
        }

        // Обработать выстрел (публичный метод)
        public async System.Threading.Tasks.Task ShootAt(Models.BoardCell cell)
        {
            if (cell == null) return;
            if (cell.IsRevealed) return;
            // Проверяем, что сейчас наш ход
            if (State != GameState.YourTurn) return;

            // В режиме демонстрации показываем ошибку
            if (_isDemoMode)
            {
                await ShowDemoWarningDialog();
                return;
            }

            // Отправляем выстрел через сетевой сервис
            if (_networkService != null && _roomId != null && _networkService.IsConnected)
            {
                await _networkService.SendShootAsync(cell.Row, cell.Col, _roomId);
                // Результат придет через OnShootResultReceived
                return;
            }

            // Если нет подключения к серверу - ошибка
            System.Diagnostics.Debug.WriteLine("Not connected to game server");
            await ShowDemoWarningDialog();
        }

        // Обработать таймаут (публичный метод)
        public void HandleTimeExpired()
        {
            if (State == GameState.YourTurn && ShipsPlaced)
            {
                System.Diagnostics.Debug.WriteLine("Time expired - switching to opponent turn");
                State = GameState.OpponentTurn;
            }
        }

        // Обработка выстрела противника (публичный метод)
        public bool? HandleOpponentShot(int row, int col)
        {
            if (State == GameState.YouWin || State == GameState.YouLose)
                return null;

            var result = Own.ShootAt(row, col);
            if (result == true)
            {
                // Противник попал - проверяем потоплен ли корабль
                var sunk = Own.IsShipSunkAt(row, col);
                if (sunk == true)
                {
                    // Помечаем все ячейки потопленного корабля
                    var ships = Own.GetShips();
                    foreach (var ship in ships)
                    {
                        var cell = Own.GetCell(row, col);
                        if (cell != null && ship.Contains(cell))
                        {
                            foreach (var shipCell in ship)
                            {
                                shipCell.IsSunk = true;
                            }
                            break;
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"Opponent sunk ship at {row},{col}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Opponent hit at {row},{col}");
                }

                // Проверяем поражение
                if (Own.AllShipsSunk())
                {
                    State = GameState.YouLose;
                    System.Diagnostics.Debug.WriteLine("All your ships sunk — you lose!");
                    ShowDefeatDialog();
                    OnPropertyChanged(nameof(OwnRemainingShips));
                    OnPropertyChanged(nameof(EnemyRemainingShips));
                    return true;
                }

                OnPropertyChanged(nameof(OwnRemainingShips));
                return true;
            }
            else if (result == false)
            {
                System.Diagnostics.Debug.WriteLine($"Opponent miss at {row},{col}");
                OnPropertyChanged(nameof(OwnRemainingShips));
                return false;
            }

            return null;
        }

        // Показать диалог поражения (приватный метод)
        private async void ShowDefeatDialog()
        {
            await System.Threading.Tasks.Task.Delay(500);
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                var dialog = new Views.VictoryDialog(false);
                var parent = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow : null;
                if (parent != null)
                {
                    await dialog.ShowDialog(parent);
                }
            });
        }

        // Запросить выход из игры (публичный метод)
        public async void RequestExit()
        {
            // Отправляем сдачу на сервер, если подключены
            if (_networkService != null && _networkService.IsConnected && !string.IsNullOrEmpty(_roomId))
            {
                await _networkService.SendSurrenderAsync(_roomId);
            }
            
            ExitRequested?.Invoke();
        }

        // Показать диалог победы (приватный метод)
        private async void ShowVictoryDialog()
        {
            await System.Threading.Tasks.Task.Delay(500); // Небольшая задержка для визуального эффекта
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                var dialog = new Views.VictoryDialog(true);
                var parent = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow : null;
                if (parent != null)
                {
                    await dialog.ShowDialog(parent);
                }
            });
        }

        // Обработчик результата выстрела от сервера (приватный метод)
        private async void OnShootResultReceived(Models.ShootResultMessage message)
        {
            System.Diagnostics.Debug.WriteLine($"[GameViewModel.OnShootResultReceived] 🎯 Received shoot result: X={message.Row}, Y={message.Col}, IsHit={message.IsHit}, IsSunk={message.IsSunk}");
            
            var cell = Enemy.GetCell(message.Row, message.Col);
            if (cell == null || cell.IsRevealed)
            {
                System.Diagnostics.Debug.WriteLine($"[GameViewModel.OnShootResultReceived] ⚠️ Cell already revealed or null");
                return;
            }

            cell.IsRevealed = true;
            cell.IsHit = message.IsHit;
            
            if (message.IsHit && message.IsSunk)
            {
                var ships = Enemy.GetShips();
                foreach (var ship in ships)
                {
                    if (ship.Contains(cell))
                    {
                        foreach (var shipCell in ship)
                        {
                            shipCell.IsSunk = true;
                        }
                        break;
                    }
                }
                State = GameState.Sunk;
            }
            else if (message.IsHit)
            {
                State = GameState.Hit;
            }
            else
            {
                State = GameState.Miss;
            }

            if (message.IsGameOver)
            {
                if (message.IsWinner)
                {
                    State = GameState.YouWin;
                    ShowVictoryDialog();
                }
                else
                {
                    State = GameState.YouLose;
                    ShowDefeatDialog();
                }
            }
            else
            {
                await System.Threading.Tasks.Task.Delay(2000);
                if (State != GameState.YouWin && State != GameState.YouLose)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameViewModel.OnShootResultReceived] Transitioning to OpponentTurn");
                    State = GameState.OpponentTurn;
                }
            }

            OnPropertyChanged(nameof(EnemyRemainingShips));
        }

        // Обработчик выстрела противника от сервера (приватный метод)
        private async void OnOpponentShootReceived(Models.ShootMessage message)
        {
            System.Diagnostics.Debug.WriteLine($"[GameViewModel.OnOpponentShootReceived] 💥 Received opponent shoot: Row={message.Row}, Col={message.Col}, RoomId={message.RoomId}");
            
            if (message.RoomId != _roomId)
            {
                System.Diagnostics.Debug.WriteLine($"[GameViewModel.OnOpponentShootReceived] ⚠️ RoomId mismatch: message={message.RoomId}, current={_roomId}");
                return;
            }

            var result = HandleOpponentShot(message.Row, message.Col);
            if (result == true)
            {
                State = GameState.Hit;
            }
            else if (result == false)
            {
                State = GameState.Miss;
            }

            await System.Threading.Tasks.Task.Delay(2000);
            if (State != GameState.YouWin && State != GameState.YouLose)
            {
                System.Diagnostics.Debug.WriteLine($"[GameViewModel.OnOpponentShootReceived] Transitioning to YourTurn");
                State = GameState.YourTurn;
            }
        }

        // Обработчик изменения состояния игры от сервера (приватный метод)
        private void OnGameStateChanged(Models.GameStateMessage message)
        {
            // Если RoomId пустой — применяем состояние для текущей комнаты (сервер может не заполнять)
            System.Diagnostics.Debug.WriteLine($"[GameViewModel.OnGameStateChanged] 📨 Received: Type={message.Type}, State={message.State}, RoomId={message.RoomId}, _currentRoomId={_roomId}");
            
            if (string.IsNullOrEmpty(message.RoomId) || message.RoomId == _roomId)
            {
                System.Diagnostics.Debug.WriteLine($"[GameViewModel.OnGameStateChanged] ✅ Applying state {message.State} for room {_roomId}");
                State = message.State;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[GameViewModel.OnGameStateChanged] ⚠️ RoomId mismatch: message={message.RoomId}, current={_roomId}");
            }
        }

        private void OnGameStarted()
        {
            System.Diagnostics.Debug.WriteLine($"[GameViewModel] OnGameStarted event received for room {_roomId}");
            // По умолчанию ждём явного сообщения о ходе (your_turn)
            // Здесь можно обновить UI, если нужно
        }

        // Отправить размещение кораблей на сервер (приватный метод)
        private async System.Threading.Tasks.Task SendShipPlacementAsync()
        {
            if (_networkService == null || _roomId == null) return;

            try
            {
                // Собираем данные о размещенных кораблях
                var ships = new System.Collections.Generic.List<Models.ShipPlacementData>();
                
                // Получаем информацию о кораблях с доски
                for (int row = 0; row < Own.Size; row++)
                {
                    for (int col = 0; col < Own.Size; col++)
                    {
                        var cell = Own.GetCell(row, col);
                        if (cell != null && cell.HasShip && (col == 0 || Own.GetCell(row, col - 1) == null || !Own.GetCell(row, col - 1)!.HasShip))
                        {
                            // Найдем размер корабля
                            int shipSize = 1;
                            bool isHorizontal = true;
                            
                            // Проверяем горизонтальный размер
                            while (col + shipSize < Own.Size && Own.GetCell(row, col + shipSize) != null && Own.GetCell(row, col + shipSize)!.HasShip)
                            {
                                shipSize++;
                            }
                            
                            // Если горизонтальный размер = 1, проверяем вертикальный
                            if (shipSize == 1)
                            {
                                isHorizontal = false;
                                int vertSize = 1;
                                while (row + vertSize < Own.Size && Own.GetCell(row + vertSize, col) != null && Own.GetCell(row + vertSize, col)!.HasShip)
                                {
                                    vertSize++;
                                }
                                shipSize = vertSize;
                            }
                            
                            ships.Add(new Models.ShipPlacementData
                            {
                                Row = row,
                                Col = col,
                                Size = shipSize,
                                IsHorizontal = isHorizontal
                            });
                        }
                    }
                }

                // Отправляем на сервер
                if (!string.IsNullOrEmpty(_roomId))
                {
                    await _networkService.SendShipPlacementAsync(ships, _roomId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending ship placement: {ex.Message}");
            }
        }

        // Показать диалог демо-режима (приватный метод)
        private async System.Threading.Tasks.Task ShowDemoWarningDialog()
        {
            await System.Threading.Tasks.Task.Delay(500);
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                var parent = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow : null;
                if (parent != null)
                {
                    var dialog = new Views.ConfirmDialog(
                        "Сервер недоступен",
                        "❌ Сервер не работает.\n\nДля игры необходимо подключение к серверу.\n\nВы можете разместить корабли, но не сможете играть.\n\nВернуться в лобби?",
                        "Вернуться в лобби",
                        "Остаться"
                    );
                    var result = await dialog.ShowDialog<bool>(parent);
                    if (result)
                    {
                        RequestExit();
                    }
                }
            });
        }

        // Событие изменения свойства (публичное событие)
        public event PropertyChangedEventHandler? PropertyChanged;
        // Уведомление об изменении свойства (приватный метод)
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

// Управление игрой
using System;
using System.ComponentModel;
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

        // Конструктор модели представления игры (публичный)
        public GameViewModel(Models.Room? room = null, Services.INetworkService? networkService = null, bool? demoMode = null)
        {
            _networkService = networkService;
            RoomName = room?.Name;
            _roomId = room?.Name;
            
            // Подписываемся на сетевые события
            if (_networkService != null)
            {
                _networkService.ShootResultReceived += OnShootResultReceived;
                _networkService.OpponentShootReceived += OnOpponentShootReceived;
                _networkService.GameStateChanged += OnGameStateChanged;
            }
            
            // В демо-режиме тоже нужно размещать корабли, поэтому начинаем с ожидания
            if (RoomName != null)
            {
                State = GameState.WaitingForOpponent;
            }

            // Пример кораблей противника (для демо/тестов)
            var useDemo = demoMode ?? App.DemoMode;
            if (useDemo)
            {
                // Демо режим: размещаем несколько кораблей для интересной демонстрации
                Enemy.PlaceShip(0, 0);
                Enemy.PlaceShip(0, 1); // 2-палубный корабль
                Enemy.PlaceShip(2, 3);
                Enemy.PlaceShip(4, 4);
                Enemy.PlaceShip(4, 5); // Еще один 2-палубный корабль
                Enemy.PlaceShip(6, 7);
                Enemy.PlaceShip(8, 1);
                Enemy.PlaceShip(8, 2);
                Enemy.PlaceShip(8, 3); // 3-палубный корабль
            }
            else
            {
                // Режим тестирования: размещаем только 3 корабля для тестовых сценариев
                Enemy.PlaceShip(0, 0);
                Enemy.PlaceShip(0, 1); // 2-палубный корабль
                Enemy.PlaceShip(2, 3);
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
                    State = GameState.YourTurn;
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
            // В демо режиме разрешаем стрелять даже если не "Ваш ход"
            if (!App.DemoMode && State != GameState.YourTurn) return;

            // Отправляем выстрел через сеть, если есть подключение
            if (_networkService != null && _roomId != null && _networkService.IsConnected)
            {
                await _networkService.SendShootAsync(cell.Row, cell.Col, _roomId);
                // Результат придет через OnShootResultReceived
                return;
            }

            // Локальная обработка (демо режим)
            var result = Enemy.ShootAt(cell.Row, cell.Col);
            if (result == true)
            {
                // Попадание - проверяем потоплен ли корабль
                var sunk = Enemy.IsShipSunkAt(cell.Row, cell.Col);
                if (sunk == true)
                {
                    // Помечаем все ячейки потопленного корабля
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
                    Console.WriteLine($"Shot sunk ship at {cell.Row},{cell.Col}");
                }
                else
                {
                    State = GameState.Hit;
                    Console.WriteLine($"Shot hit at {cell.Row},{cell.Col}");
                }

                // Проверяем победу
                if (Enemy.AllShipsSunk())
                {
                    State = GameState.YouWin;
                    Console.WriteLine("All enemy ships sunk — you win!");
                    Console.WriteLine("DEBUG: State after AllShipsSunk set to " + State);
                    // Показываем диалог победы
                    ShowVictoryDialog();
                    // Убеждаемся, что выходим рано и не переходим к OpponentTurn после победы
                    return;
                }
            }
            else if (result == false)
            {
                State = GameState.Miss;
                Console.WriteLine($"Shot miss at {cell.Row},{cell.Col}");
            }
            else
            {
                Console.WriteLine($"Shot ignored at {cell.Row},{cell.Col} (already revealed or invalid)");
                return;
            }

            // Оставляем статус видимым дольше (2 секунды для Hit/Miss/Sunk)
            if (State == GameState.Hit || State == GameState.Miss || State == GameState.Sunk)
            {
                await System.Threading.Tasks.Task.Delay(2000); // 2 секунды для чтения статуса
            }

            // Двойная проверка: если мы стали YouWin/YouLose, не меняем состояние
            if (State == GameState.YouWin || State == GameState.YouLose)
            {
                // Обновляем счетчики и возвращаемся без переключения на противника
                OnPropertyChanged(nameof(OwnRemainingShips));
                OnPropertyChanged(nameof(EnemyRemainingShips));
                return;
            }

            // Всегда переходим к ходу противника после выстрела
            await System.Threading.Tasks.Task.Delay(500);
            State = GameState.OpponentTurn;

            // Обновляем счетчики кораблей
            OnPropertyChanged(nameof(OwnRemainingShips));
            OnPropertyChanged(nameof(EnemyRemainingShips));
        }

        // Обработать таймаут (публичный метод)
        public void HandleTimeExpired()
        {
            if (State == GameState.YourTurn && ShipsPlaced)
            {
                Console.WriteLine("Time expired - switching to opponent turn");
                State = GameState.OpponentTurn;
                
                // Симулируем выстрел противника
                if (App.DemoMode)
                {
                    _ = SimulateOpponentShot();
                }
            }
        }

        // Симуляция выстрела противника (приватный метод)
        private async System.Threading.Tasks.Task SimulateOpponentShot()
        {
            await System.Threading.Tasks.Task.Delay(1000);
            
            // Случайный выстрел противника
            var random = new Random();
            var row = random.Next(0, 10);
            var col = random.Next(0, 10);
            
            var cell = Own.GetCell(row, col);
            if (cell != null && !cell.IsRevealed)
            {
                HandleOpponentShot(row, col);
                
                // После выстрела противника возвращаем ход игроку
                await System.Threading.Tasks.Task.Delay(2000);
                if (State != GameState.YouWin && State != GameState.YouLose)
                {
                    State = GameState.YourTurn;
                }
            }
            else
            {
                // Если ячейка уже открыта, пробуем еще раз
                _ = SimulateOpponentShot();
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
                    Console.WriteLine($"Opponent sunk ship at {row},{col}");
                }
                else
                {
                    Console.WriteLine($"Opponent hit at {row},{col}");
                }

                // Проверяем поражение
                if (Own.AllShipsSunk())
                {
                    State = GameState.YouLose;
                    Console.WriteLine("All your ships sunk — you lose!");
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
                Console.WriteLine($"Opponent miss at {row},{col}");
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
        public void RequestExit() => ExitRequested?.Invoke();

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
            var cell = Enemy.GetCell(message.Row, message.Col);
            if (cell == null || cell.IsRevealed) return;

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
                    State = GameState.OpponentTurn;
                }
            }

            OnPropertyChanged(nameof(EnemyRemainingShips));
        }

        // Обработчик выстрела противника от сервера (приватный метод)
        private async void OnOpponentShootReceived(Models.ShootMessage message)
        {
            if (message.RoomId != _roomId) return;

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
                State = GameState.YourTurn;
            }
        }

        // Обработчик изменения состояния игры от сервера (приватный метод)
        private void OnGameStateChanged(Models.GameStateMessage message)
        {
            if (message.RoomId == _roomId)
            {
                State = message.State;
            }
        }

        // Событие изменения свойства (публичное событие)
        public event PropertyChangedEventHandler? PropertyChanged;
        // Уведомление об изменении свойства (приватный метод)
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
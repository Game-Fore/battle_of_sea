// Управление игрой
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media;
using BattleOfSea.Models;

namespace BattleOfSea.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged
    {
        private GameState _state = GameState.WaitingForOpponent;

        public GameState State
        {
            get => _state;
            set
            {
                if ((_state == GameState.YouWin || _state == GameState.YouLose) && value != _state)
                {
                    if (value != GameState.YouWin && value != GameState.YouLose)
                        return;
                }

                _state = value;
                Console.WriteLine($"[GameViewModel] State changed to: {value}");
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusBackground));
                OnPropertyChanged(nameof(StatusForeground));
            }
        }

        public string StatusText => State switch
        {
            GameState.WaitingForOpponent => "⏳ Ожидание соперника...",
            GameState.ReadyToStart => "✅ Соперник найден! Можно играть",
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

        public IBrush StatusBackground => State switch
        {
            GameState.YouWin => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
            GameState.YouLose => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            GameState.Hit => new SolidColorBrush(Color.FromRgb(251, 191, 36)),
            GameState.Sunk => new SolidColorBrush(Color.FromRgb(220, 38, 38)),
            GameState.Miss => new SolidColorBrush(Color.FromRgb(147, 197, 253)),
            GameState.YourTurn => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
            GameState.ReadyToStart => new SolidColorBrush(Color.FromRgb(59, 130, 246)),
            GameState.OpponentTurn => new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            _ => new SolidColorBrush(Color.FromRgb(229, 231, 235))
        };

        public IBrush StatusForeground =>
            (State == GameState.YouWin ||
             State == GameState.YouLose ||
             State == GameState.Hit ||
             State == GameState.Sunk ||
             State == GameState.YourTurn ||
             State == GameState.ReadyToStart)
                ? Brushes.White
                : Brushes.Black;

        public string? RoomName { get; }
        public Board Own { get; } = new Board(10);
        public Board Enemy { get; } = new Board(10);
        public ShipPlacementManager ShipManager { get; } = new ShipPlacementManager();

        public int OwnRemainingShips => Own.RemainingShipsCount();
        public int EnemyRemainingShips => Enemy.RemainingShipsCount();

        private bool _shipsPlaced;
        public bool ShipsPlaced
        {
            get => _shipsPlaced;
            set
            {
                _shipsPlaced = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowPlacement));
                OnPropertyChanged(nameof(ShowGame));
            }
        }

        public bool ShowPlacement => !ShipsPlaced;
        public bool ShowGame => ShipsPlaced;

        public event Action? ExitRequested;

        public ICommand ShootCommand { get; }
        public ICommand ReadyCommand { get; }
        public ICommand StartGameCommand { get; }
        public ICommand SelectShipCommand { get; }
        public ICommand RotateShipCommand { get; }
        public ICommand RemoveShipCommand { get; }

        private int? _selectedShipSize;
        public int? SelectedShipSize
        {
            get => _selectedShipSize;
            set { _selectedShipSize = value; OnPropertyChanged(); }
        }

        private bool _isHorizontal = true;
        public bool IsHorizontal
        {
            get => _isHorizontal;
            set { _isHorizontal = value; OnPropertyChanged(); }
        }

        private readonly Services.INetworkService? _networkService;
        private readonly string? _roomId;

        public GameViewModel(Room? room, Services.INetworkService? networkService)
        {
            _networkService = networkService;
            RoomName = room?.Name;
            _roomId = room?.Id;

            Console.WriteLine($"[GameVM] RoomId = '{_roomId}', RoomName = '{RoomName}'");

            ShipsPlaced = false;

            if (_networkService != null)
            {
                _networkService.ShootResultReceived += OnShootResultReceived;
                _networkService.OpponentShootReceived += OnOpponentShootReceived;
                _networkService.GameStateChanged += OnGameStateChanged;
            }

            ShootCommand = new Utils.RelayCommand(o =>
            {
                if (o is BoardCell cell)
                    _ = ShootAt(cell);
            });

            ReadyCommand = new Utils.RelayCommand(_ =>
            {
                _ = SendPlayerReadyAsync();
            });

            StartGameCommand = new Utils.RelayCommand(_ =>
            {
                if (ShipManager.AllShipsPlaced)
                {
                    ShipsPlaced = true;
                    _ = SendShipPlacementAsync();
                }
            });

            SelectShipCommand = new Utils.RelayCommand(o =>
            {
                if (int.TryParse(o?.ToString(), out int size))
                    SelectedShipSize = size;
            });

            RotateShipCommand = new Utils.RelayCommand(_ =>
            {
                IsHorizontal = !IsHorizontal;
            });

            RemoveShipCommand = new Utils.RelayCommand(o =>
            {
                if (o is BoardCell cell)
                    RemoveShip(cell.Row, cell.Col);
            });
        }

        public bool PlaceShip(int r, int c, int size, bool horizontal)
        {
            if (!ShipManager.CanPlaceShip(size)) return false;

            if (Own.PlaceShip(r, c, size, horizontal))
            {
                ShipManager.PlaceShip(size);
                OnPropertyChanged(nameof(ShipManager));
                return true;
            }

            return false;
        }

        public void PlaceShipAtCell(BoardCell cell)
        {
            if (SelectedShipSize.HasValue)
                PlaceShip(cell.Row, cell.Col, SelectedShipSize.Value, IsHorizontal);
        }

        public void RemoveShip(int r, int c)
        {
            var ships = Own.GetShips();
            foreach (var ship in ships)
            {
                var cell = ship.Find(x => x.Row == r && x.Col == c);
                if (cell != null)
                {
                    Own.RemoveShip(r, c);
                    ShipManager.RemoveShip(ship.Count);
                    OnPropertyChanged(nameof(ShipManager));
                    break;
                }
            }
        }

        public async System.Threading.Tasks.Task ShootAt(BoardCell cell)
        {
            if (cell.IsRevealed) return;
            if (State != GameState.YourTurn) return;
            if (_networkService == null || _roomId == null) return;

            await _networkService.SendShootAsync(cell.Row, cell.Col, _roomId);
        }

        public void RequestExit()
        {
            ExitRequested?.Invoke();
        }

        private async void OnShootResultReceived(ShootResultMessage message)
        {
            var cell = Enemy.GetCell(message.Row, message.Col);
            if (cell == null || cell.IsRevealed) return;

            cell.IsRevealed = true;
            cell.IsHit = message.IsHit;

            State = message.IsHit
                ? (message.IsSunk ? GameState.Sunk : GameState.Hit)
                : GameState.Miss;

            if (message.IsGameOver)
            {
                State = message.IsWinner ? GameState.YouWin : GameState.YouLose;
            }
            else
            {
                await System.Threading.Tasks.Task.Delay(1500);
                State = GameState.OpponentTurn;
            }

            OnPropertyChanged(nameof(EnemyRemainingShips));
        }

        private async void OnOpponentShootReceived(ShootMessage message)
        {
            if (message.RoomId != _roomId) return;

            Own.ShootAt(message.Row, message.Col);

            await System.Threading.Tasks.Task.Delay(1500);
            State = GameState.YourTurn;

            OnPropertyChanged(nameof(OwnRemainingShips));
        }

        private void OnGameStateChanged(GameStateMessage message)
        {
            if (!string.IsNullOrEmpty(message.RoomId) && message.RoomId != _roomId)
                return;

            if (Enum.TryParse<GameState>(message.State, true, out var newState))
            {
                Console.WriteLine($"[GameVM] Server state → {newState}");
                State = newState;
            }
        }

        private async System.Threading.Tasks.Task SendPlayerReadyAsync()
        {
            if (_networkService == null || _roomId == null) return;
            await _networkService.SendPlayerReadyAsync(_roomId);
        }

        private async System.Threading.Tasks.Task SendShipPlacementAsync()
        {
            if (_networkService == null || _roomId == null) return;

            var ships = new System.Collections.Generic.List<ShipPlacementData>();

            for (int row = 0; row < Own.Size; row++)
            {
                for (int col = 0; col < Own.Size; col++)
                {
                    var cell = Own.GetCell(row, col);
                    if (cell == null || !cell.HasShip) continue;

                    if (col > 0 && Own.GetCell(row, col - 1)?.HasShip == true)
                        continue;

                    int size = 1;
                    bool horizontal = true;

                    while (col + size < Own.Size && Own.GetCell(row, col + size)?.HasShip == true)
                        size++;

                    if (size == 1)
                    {
                        horizontal = false;
                        int v = 1;
                        while (row + v < Own.Size && Own.GetCell(row + v, col)?.HasShip == true)
                            v++;
                        size = v;
                    }

                    ships.Add(new ShipPlacementData
                    {
                        Row = row,
                        Col = col,
                        Size = size,
                        IsHorizontal = horizontal
                    });
                }
            }

            await _networkService.SendShipPlacementAsync(ships, _roomId);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

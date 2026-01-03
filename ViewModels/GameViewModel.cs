using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media;
using BattleOfSea.Models;
using BattleOfSea;

namespace BattleOfSea.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged
    {
        private GameState _state = GameState.WaitingForOpponent;
        public GameState State
        {
            get => _state;
            set { _state = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); 
                  OnPropertyChanged(nameof(StatusBackground)); OnPropertyChanged(nameof(StatusForeground)); }
        }

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

        public IBrush StatusBackground => State switch
        {
            GameState.YouWin => new SolidColorBrush(Color.FromRgb(34, 197, 94)), // Green
            GameState.YouLose => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // Red
            GameState.Hit => new SolidColorBrush(Color.FromRgb(251, 191, 36)), // Yellow
            GameState.Sunk => new SolidColorBrush(Color.FromRgb(220, 38, 38)), // Dark Red
            GameState.Miss => new SolidColorBrush(Color.FromRgb(147, 197, 253)), // Light Blue
            GameState.YourTurn => new SolidColorBrush(Color.FromRgb(34, 197, 94)), // Green
            GameState.OpponentTurn => new SolidColorBrush(Color.FromRgb(156, 163, 175)), // Gray
            _ => new SolidColorBrush(Color.FromRgb(229, 231, 235)) // Light Gray
        };

        public IBrush StatusForeground => (State == GameState.YouWin || State == GameState.YouLose || 
                                           State == GameState.Hit || State == GameState.Sunk || 
                                           State == GameState.YourTurn) 
            ? Brushes.White : Brushes.Black;

        public string? RoomName { get; }
        public Models.Board Own { get; } = new Models.Board(10);
        public Models.Board Enemy { get; } = new Models.Board(10);
        public ShipPlacementManager ShipManager { get; } = new ShipPlacementManager();

        private bool _shipsPlaced = false;
        public bool ShipsPlaced
        {
            get => _shipsPlaced;
            set { _shipsPlaced = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowPlacement)); OnPropertyChanged(nameof(ShowGame)); }
        }

        public bool ShowPlacement => !_shipsPlaced;
        public bool ShowGame => _shipsPlaced;

        public event Action? ExitRequested;

        public ICommand? ShootCommand { get; private set; }
        public ICommand StartGameCommand { get; private set; }
        public ICommand SelectShipCommand { get; private set; }
        public ICommand RotateShipCommand { get; private set; }
        public ICommand RemoveShipCommand { get; private set; }

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

        public GameViewModel(Models.Room? room = null)
        {
            RoomName = room?.Name;
            // In demo mode, start with YourTurn so user can immediately interact
            if (App.DemoMode)
            {
                State = GameState.YourTurn;
            }
            else if (RoomName != null)
            {
                State = GameState.WaitingForOpponent;
            }

            // Example enemy ships (for demo/test) -- in real game these come from server
            // Place several ships for a more interesting demo
            Enemy.PlaceShip(0, 0);
            Enemy.PlaceShip(0, 1); // 2-cell ship
            Enemy.PlaceShip(2, 3);
            Enemy.PlaceShip(4, 4);
            Enemy.PlaceShip(4, 5); // Another 2-cell ship
            Enemy.PlaceShip(6, 7);
            Enemy.PlaceShip(8, 1);
            Enemy.PlaceShip(8, 2);
            Enemy.PlaceShip(8, 3); // 3-cell ship

            // In demo mode, auto-place ships for quick testing
            if (App.DemoMode)
            {
                // Place demo ships
                Own.PlaceShip(1, 1);
                Own.PlaceShip(1, 2);
                Own.PlaceShip(3, 5);
                Own.PlaceShip(5, 2);
                Own.PlaceShip(5, 3);
                Own.PlaceShip(7, 6);
                Own.PlaceShip(7, 7);
                Own.PlaceShip(9, 4);
                ShipsPlaced = true;
            }
            else
            {
                // In real game, user must place ships
                ShipsPlaced = false;
            }

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

        public bool PlaceShip(int r, int c, int size, bool horizontal)
        {
            if (!ShipManager.CanPlaceShip(size)) return false;
            if (Own.PlaceShip(r, c, size, horizontal))
            {
                ShipManager.PlaceShip(size);
                OnPropertyChanged(nameof(StartGameCommand));
                return true;
            }
            return false;
        }

        public void PlaceShipAtCell(Models.BoardCell cell)
        {
            if (SelectedShipSize.HasValue)
            {
                PlaceShip(cell.Row, cell.Col, SelectedShipSize.Value, IsHorizontal);
            }
        }

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
                    OnPropertyChanged(nameof(StartGameCommand));
                    break;
                }
            }
        }

        public async System.Threading.Tasks.Task ShootAt(Models.BoardCell cell)
        {
            if (cell == null) return;
            if (cell.IsRevealed) return;
            // In demo mode, allow shooting even if not explicitly "YourTurn"
            if (!App.DemoMode && State != GameState.YourTurn) return;

            var result = Enemy.ShootAt(cell.Row, cell.Col);
            if (result == true)
            {
                // Hit - check if this sank a ship
                var sunk = Enemy.IsShipSunkAt(cell.Row, cell.Col);
                if (sunk == true)
                {
                    // Mark all cells of the sunk ship as sunk
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

                // Check for victory
                if (Enemy.AllShipsSunk())
                {
                    State = GameState.YouWin;
                    Console.WriteLine("All enemy ships sunk — you win!");
                    // Show victory dialog
                    ShowVictoryDialog();
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

            // Keep status visible longer so user can read it (2 seconds for Hit/Miss/Sunk)
            if (State == GameState.Hit || State == GameState.Miss || State == GameState.Sunk)
            {
                await System.Threading.Tasks.Task.Delay(2000); // 2 seconds to read the status
            }

            // In demo mode, immediately return to YourTurn so user can keep playing
            if (App.DemoMode)
            {
                if (State != GameState.YouWin)
                    State = GameState.YourTurn;
            }
            else
            {
                // Simple opponent response simulation: wait then set to opponent turn
                await System.Threading.Tasks.Task.Delay(500);
                // only change to opponent turn if we haven't already won
                if (State != GameState.YouWin)
                    State = GameState.OpponentTurn;
            }
        }

        public void RequestExit() => ExitRequested?.Invoke();

        private async void ShowVictoryDialog()
        {
            await System.Threading.Tasks.Task.Delay(500); // Small delay for visual effect
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

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BattleOfSea.Models;

namespace BattleOfSea.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged
    {
        private GameState _state = GameState.WaitingForOpponent;
        public GameState State
        {
            get => _state;
            set { _state = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
        }

        public string StatusText => State switch
        {
            GameState.WaitingForOpponent => "Ожидание соперника...",
            GameState.YourTurn => "Ваш ход",
            GameState.OpponentTurn => "Ход соперника",
            GameState.YouWin => "Вы победили!",
            GameState.YouLose => "Вы проиграли.",
            GameState.OpponentSurrender => "Соперник сдался.",
            GameState.Hit => "Попал",
            GameState.Miss => "Промах",
            GameState.Sunk => "Убил",
            _ => string.Empty
        };

        public string? RoomName { get; }
        public Models.Board Own { get; } = new Models.Board(10);
        public Models.Board Enemy { get; } = new Models.Board(10);

        public event Action? ExitRequested;

        public ICommand? ShootCommand { get; private set; }

        public GameViewModel(Models.Room? room = null)
        {
            RoomName = room?.Name;
            if (RoomName != null)
                State = GameState.WaitingForOpponent;

            // Example enemy ships (for demo/test) -- in real game these come from server
            Enemy.PlaceShip(0, 0);
            Enemy.PlaceShip(0, 1);
            Enemy.PlaceShip(2, 3);

            ShootCommand = new Utils.RelayCommand(o => {
                if (o is Models.BoardCell cell) _ = ShootAt(cell);
            });
        }

        public async System.Threading.Tasks.Task ShootAt(Models.BoardCell cell)
        {
            if (cell == null) return;
            if (cell.IsRevealed) return;

            var result = Enemy.ShootAt(cell.Row, cell.Col);
            if (result == true)
            {
                State = GameState.Hit;
                Console.WriteLine($"Shot hit at {cell.Row},{cell.Col}");
            }
            else if (result == false)
            {
                State = GameState.Miss;
                Console.WriteLine($"Shot miss at {cell.Row},{cell.Col}");
            }
            else
            {
                Console.WriteLine($"Shot ignored at {cell.Row},{cell.Col} (already revealed or invalid)");
            }

            // Simple opponent response simulation: wait then set to opponent turn
            await System.Threading.Tasks.Task.Delay(200);
            State = GameState.OpponentTurn;
        }

        public void RequestExit() => ExitRequested?.Invoke();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

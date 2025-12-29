using System.ComponentModel;
using System.Runtime.CompilerServices;
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
        public event Action? ExitRequested;

        public GameViewModel(Models.Room? room = null)
        {
            RoomName = room?.Name;
            if (RoomName != null)
                State = GameState.WaitingForOpponent;
        }

        public void RequestExit() => ExitRequested?.Invoke();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

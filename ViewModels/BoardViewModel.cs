// Представление поля
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace BattleOfSea.ViewModels
{
    public class BoardViewModel : INotifyPropertyChanged
    {
        public Models.Board Own { get; } = new Models.Board(10);
        public Models.Board Enemy { get; } = new Models.Board(10);

        public BoardViewModel()
        {
            // Example ships on own board (static for now)
            Own.PlaceShip(0, 0);
            Own.PlaceShip(0, 1);
            Own.PlaceShip(2, 3);
        }

        public void EnemyCellClick(Models.BoardCell cell)
        {
            Console.WriteLine($"Enemy cell clicked: {cell.Row},{cell.Col}");
            // For demo: reveal cell and randomly decide hit/miss
            cell.IsRevealed = true;
            cell.IsHit = new Random().Next(0, 4) == 0; // ~25% hit chance as demo
            Console.WriteLine(cell.IsHit ? "Попал!" : "Промах");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
